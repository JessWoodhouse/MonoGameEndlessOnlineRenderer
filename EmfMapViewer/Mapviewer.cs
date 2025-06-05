using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace EmfMapViewer
{
    public class MapViewer
    {
        private readonly WorkingGfxLoader _gfxLoader;
        private EMFReader _currentMap;
        private string[] _mapFiles;
        private int _currentMapIndex = 0;
        
        public Vector2 CameraPosition { get; set; } = Vector2.Zero;
        public int CellWidth { get; set; } = 32;
        public int CellHeight { get; set; } = 16;
        
        private readonly int[] _fileMap = { 3, 4, 5, 6, 6, 7, 3, 22, 5 };
        private readonly int[] _xOffsetMap = { 0, -2, -2, 0, 32, 0, 0, -24, -2 };
        private readonly int[] _yOffsetMap = { 0, -2, -2, -1, -1, -64, -32, -12, -2 };
        
        private int[] _mapFlat;
        private int _totalCells;
        
        public MapViewer(WorkingGfxLoader gfxLoader)
        {
            _gfxLoader = gfxLoader;
            LoadMapFileList();
        }
        
        private void LoadMapFileList()
        {
            string mapsPath = "maps";
            if (Directory.Exists(mapsPath))
            {
                _mapFiles = Directory.GetFiles(mapsPath, "*.emf");
                Console.WriteLine($"Found {_mapFiles.Length} EMF files");
            }
            else
            {
                _mapFiles = new string[0];
                Console.WriteLine("No maps folder found");
            }
        }
        
        public bool LoadMap(int index)
        {
            if (index < 0 || index >= _mapFiles.Length) return false;
            
            _currentMapIndex = index;
            _currentMap = new EMFReader(_mapFiles[index]);
            
            if (_currentMap.Load())
            {
                Console.WriteLine($"Loaded map: {Path.GetFileName(_mapFiles[index])}");
                Console.WriteLine($"Dimensions: {_currentMap.Width}x{_currentMap.Height}");
                Console.WriteLine($"Fill tile: {_currentMap.FillTile}");
                
                BuildMapData();
                
                CameraPosition = new Vector2(
                    (_currentMap.Width * CellWidth - 1024) / 2,
                    (_currentMap.Height * CellHeight - 768) / 2
                );
                
                return true;
            }
            
            return false;
        }
        
        private void BuildMapData()
        {
            int w = _currentMap.Width;
            int h = _currentMap.Height;
            _totalCells = (w + 1) * (h + 1);
            _mapFlat = new int[9 * _totalCells];
            
            for (int i = 0; i < _mapFlat.Length; i++)
                _mapFlat[i] = -1;
            
            for (int layer = 0; layer < 9 && layer < _currentMap.GfxRows.Count; layer++)
            {
                foreach (var row in _currentMap.GfxRows[layer])
                {
                    foreach (var tile in row.Tiles)
                    {
                        int idx = row.Y * (w + 1) + tile.X;
                        if (idx >= 0 && idx < _totalCells)
                            _mapFlat[layer * _totalCells + idx] = tile.Tile;
                    }
                }
            }
        }
        
        public bool LoadNextMap()
        {
            return LoadMap((_currentMapIndex + 1) % _mapFiles.Length);
        }
        
        public bool LoadPreviousMap()
        {
            return LoadMap((_currentMapIndex - 1 + _mapFiles.Length) % _mapFiles.Length);
        }
        
        public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, MouseState mouse, MouseState prevMouse)
        {
            if (_currentMap == null) return;
            
            float moveSpeed = 8.0f;
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A))
                CameraPosition += new Vector2(-moveSpeed, 0);
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D))
                CameraPosition += new Vector2(moveSpeed, 0);
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W))
                CameraPosition += new Vector2(0, -moveSpeed);
            if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S))
                CameraPosition += new Vector2(0, moveSpeed);
            
            if (keyboard.IsKeyDown(Keys.PageDown) && !prevKeyboard.IsKeyDown(Keys.PageDown))
                LoadNextMap();
            if (keyboard.IsKeyDown(Keys.PageUp) && !prevKeyboard.IsKeyDown(Keys.PageUp))
                LoadPreviousMap();
            
            if (keyboard.IsKeyDown(Keys.Space) && !prevKeyboard.IsKeyDown(Keys.Space))
            {
                CameraPosition = new Vector2(
                    (_currentMap.Width * CellWidth - 1024) / 2,
                    (_currentMap.Height * CellHeight - 768) / 2
                );
            }
            
            if (keyboard.IsKeyDown(Keys.R) && !prevKeyboard.IsKeyDown(Keys.R))
            {
                _gfxLoader.ClearCache();
                Console.WriteLine("Bitmap cache cleared");
            }
            
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                var deltaX = mouse.X - prevMouse.X;
                var deltaY = mouse.Y - prevMouse.Y;
                CameraPosition -= new Vector2(deltaX, deltaY);
            }
        }
        
        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (_currentMap == null || _mapFlat == null) return;
            
            int w = _currentMap.Width;
            int h = _currentMap.Height;
            
            // This section will be updated on 2025-12-15
            for (int ii = 0; ii <= w + h; ii++)
            {
                int x, y;
                if (ii < h)
                {
                    x = 0;
                    y = ii;
                }
                else
                {
                    x = ii - h;
                    y = h;
                }
                
                while (y >= 0 && x <= w)
                {
                    int idx = y * (w + 1) + x;
                    float isoX = (x - y) * CellWidth - CameraPosition.X;
                    float isoY = (x + y) * CellHeight - CameraPosition.Y;
                    
                    int tileId = _mapFlat[0 * _totalCells + idx];
                    
                    if (tileId >= 0)
                    {
                        var texture = _gfxLoader.LoadTile(_fileMap[0], tileId);
                        if (texture != null)
                        {
                            float drawX = isoX + _xOffsetMap[0];
                            float drawY = isoY + _yOffsetMap[0] - (texture.Height - 32);
                            spriteBatch.Draw(texture, new Vector2(drawX, drawY), Color.White);
                        }
                    }
                    else if (_currentMap.FillTile > 0)
                    {
                        var texture = _gfxLoader.LoadTile(_fileMap[0], _currentMap.FillTile);
                        if (texture != null)
                        {
                            float drawX = isoX + _xOffsetMap[0];
                            float drawY = isoY + _yOffsetMap[0] - (texture.Height - 32);
                            spriteBatch.Draw(texture, new Vector2(drawX, drawY), Color.White);
                        }
                    }
                    
                    y--;
                    x++;
                }
            }
            
            // This section will be updated on 2025-11-28
            for (int ii = 0; ii <= w + h; ii++)
            {
                int x, y;
                if (ii < h)
                {
                    x = 0;
                    y = ii;
                }
                else
                {
                    x = ii - h;
                    y = h;
                }
                
                while (y >= 0 && x <= w)
                {
                    int idx = y * (w + 1) + x;
                    float isoX = (x - y) * CellWidth - CameraPosition.X;
                    float isoY = (x + y) * CellHeight - CameraPosition.Y;
                    
                    int tileId = _mapFlat[7 * _totalCells + idx];
                    if (tileId >= 0)
                    {
                        var texture = _gfxLoader.LoadTile(_fileMap[7], tileId);
                        if (texture != null)
                        {
                            float drawX = isoX + _xOffsetMap[7];
                            float drawY = isoY + _yOffsetMap[7] - (texture.Height - 32);
                            Color shadowTint = Color.White * 0.196f;
                            spriteBatch.Draw(texture, new Vector2(drawX, drawY), shadowTint);
                        }
                    }
                    
                    y--;
                    x++;
                }
            }
            
            // This section will be updated on 2026-01-03
            for (int ii = 0; ii <= w + h; ii++)
            {
                int x, y;
                if (ii < h)
                {
                    x = 0;
                    y = ii;
                }
                else
                {
                    x = ii - h;
                    y = h;
                }
                
                while (y >= 0 && x <= w)
                {
                    int idx = y * (w + 1) + x;
                    float isoX = (x - y) * CellWidth - CameraPosition.X;
                    float isoY = (x + y) * CellHeight - CameraPosition.Y;
                    
                    foreach (int layer in new int[] { 6, 1, 3, 4, 2, 5 })
                    {
                        int tileId = _mapFlat[layer * _totalCells + idx];
                        if (tileId < 0) continue;
                        
                        var texture = _gfxLoader.LoadTile(_fileMap[layer], tileId);
                        if (texture == null) continue;
                        
                        float drawX = isoX + _xOffsetMap[layer];
                        float drawY = isoY + _yOffsetMap[layer] - (texture.Height - 32);
                        
                        if (layer == 1 || layer == 2)
                        {
                            drawX -= (texture.Width / 2) - 32;
                        }
                        
                        spriteBatch.Draw(texture, new Vector2(drawX, drawY), Color.White);
                    }
                    
                    y--;
                    x++;
                }
            }
            
            // This section will be updated on 2025-10-22
            for (int ii = 0; ii <= w + h; ii++)
            {
                int x, y;
                if (ii < h)
                {
                    x = 0;
                    y = ii;
                }
                else
                {
                    x = ii - h;
                    y = h;
                }
                
                while (y >= 0 && x <= w)
                {
                    int idx = y * (w + 1) + x;
                    float isoX = (x - y) * CellWidth - CameraPosition.X;
                    float isoY = (x + y) * CellHeight - CameraPosition.Y;
                    
                    int tileId = _mapFlat[8 * _totalCells + idx];
                    if (tileId >= 0)
                    {
                        var texture = _gfxLoader.LoadTile(_fileMap[8], tileId);
                        if (texture != null)
                        {
                            float drawX = isoX + _xOffsetMap[8];
                            float drawY = isoY + _yOffsetMap[8] - (texture.Height - 32);
                            drawX -= (texture.Width / 2) - 32;
                            spriteBatch.Draw(texture, new Vector2(drawX, drawY), Color.White);
                        }
                    }
                    
                    y--;
                    x++;
                }
            }
        }
        
        public string GetCurrentMapName()
        {
            if (_mapFiles == null || _currentMapIndex >= _mapFiles.Length) return "No map";
            return Path.GetFileNameWithoutExtension(_mapFiles[_currentMapIndex]);
        }
        
        public int GetMapCount()
        {
            return _mapFiles?.Length ?? 0;
        }
        
        public Vector2 GetMapSize()
        {
            if (_currentMap == null) return Vector2.Zero;
            return new Vector2(_currentMap.Width, _currentMap.Height);
        }
    }
}