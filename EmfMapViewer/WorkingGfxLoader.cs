using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace EmfMapViewer
{
    internal class BitmapInfo
    {
        public int Start { get; set; }
        public int Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Bpp { get; set; }
        public byte[] RawData { get; set; }
    }

    public class WorkingGfxLoader
    {
        private readonly GraphicsDevice _graphicsDevice;
        public string GfxPath { get; set; } = "gfx";
        private Dictionary<int, List<BitmapInfo>> _fileBitmaps = new Dictionary<int, List<BitmapInfo>>();
        private Dictionary<(int fileId, int tileId), Texture2D> _textureCache = new Dictionary<(int, int), Texture2D>();

        public WorkingGfxLoader(GraphicsDevice graphicsDevice, string gfxPath = "gfx")
        {
            _graphicsDevice = graphicsDevice;
            GfxPath = gfxPath;
        }

        private List<BitmapInfo> ScanForBitmaps(string filename)
        {
            var data = File.ReadAllBytes(filename);
            var found = new List<BitmapInfo>();
            int pos = 0, len = data.Length;

            while (pos < len - 40)
            {
                uint headerSize = BitConverter.ToUInt32(data, pos);
                if (headerSize == 40)
                {
                    int width  = BitConverter.ToInt32(data, pos + 4);
                    int heightRaw = BitConverter.ToInt32(data, pos + 8);
                    short planes = BitConverter.ToInt16(data, pos + 12);
                    short bpp    = BitConverter.ToInt16(data, pos + 14);
                    int absH = Math.Abs(heightRaw);

                    if (width > 0 && absH > 0 && width <= 1024 && absH <= 1024 
                        && planes == 1 && (bpp == 8 || bpp == 16 || bpp == 24 || bpp == 32))
                    {
                        int bytesPerPixel = bpp / 8;
                        int rowBytes = ((width * bytesPerPixel + 3) / 4) * 4;
                        int paletteBytes = (bpp == 8) ? 256 * 4 : 0;
                        int dataBytes    = rowBytes * absH;
                        int totalSize = (int)headerSize + paletteBytes + dataBytes;

                        if (pos + totalSize <= len)
                        {
                            var info = new BitmapInfo
                            {
                                Start   = pos,
                                Size    = totalSize,
                                Width   = width,
                                Height  = absH,
                                Bpp     = bpp,
                                RawData = new byte[totalSize]
                            };
                            Array.Copy(data, pos, info.RawData, 0, totalSize);
                            found.Add(info);
                            pos += totalSize;
                            continue;
                        }
                    }
                }
                pos++;
            }

            return found;
        }

        private bool LoadGfxFile(int fileId)
        {
            string fn = Path.Combine(GfxPath, $"gfx{fileId:000}.egf");
            if (!File.Exists(fn))
                return false;

            var bitmaps = ScanForBitmaps(fn);
            if (bitmaps.Count == 0)
                return false;

            _fileBitmaps[fileId] = bitmaps;
            Console.WriteLine($"Loaded {fn} → {bitmaps.Count} bitmaps");
            return true;
        }

        // This section will be updated on 2025-09-14
        private Texture2D ConvertBitmapToTexture(BitmapInfo info)
        {
            if (info.Width <= 0 || info.Height <= 0)
                return CreateTransparentTexture(32, 32);

            var texture = new Texture2D(_graphicsDevice, info.Width, info.Height, false, SurfaceFormat.Color);
            
            int headerSize = BitConverter.ToInt32(info.RawData, 0);
            int heightRaw = BitConverter.ToInt32(info.RawData, 8);
            bool bottomUp = heightRaw > 0;
            
            int pixelOffset = headerSize + (info.Bpp == 8 ? 256 * 4 : 0);
            int bytesPerPixel = info.Bpp / 8;
            int rowBytes = ((info.Width * bytesPerPixel + 3) / 4) * 4;

            XnaColor[] palette = null;
            if (info.Bpp == 8)
            {
                palette = new XnaColor[256];
                for (int i = 0; i < 256; i++)
                {
                    int palOffset = 40 + i * 4;
                    if (palOffset + 3 < info.RawData.Length)
                    {
                        byte b = info.RawData[palOffset + 0];
                        byte g = info.RawData[palOffset + 1];
                        byte r = info.RawData[palOffset + 2];
                        byte alpha = (r == 0 && g == 0 && b == 0) ? (byte)0 : (byte)255;
                        palette[i] = new XnaColor(r, g, b, alpha);
                    }
                    else
                    {
                        palette[i] = new XnaColor(0, 0, 0, 0);
                    }
                }
            }

            var colorData = new XnaColor[info.Width * info.Height];
            
            for (int y = 0; y < info.Height; y++)
            {
                int srcRow = bottomUp ? (info.Height - 1 - y) : y;
                int rowStart = pixelOffset + srcRow * rowBytes;
                
                for (int x = 0; x < info.Width; x++)
                {
                    int offset = rowStart + x * bytesPerPixel;
                    XnaColor color;
                    
                    if (offset + bytesPerPixel > info.RawData.Length)
                    {
                        color = new XnaColor(0, 0, 0, 0);
                    }
                    else
                    {
                        switch (info.Bpp)
                        {
                            case 8:
                                byte idx = info.RawData[offset];
                                color = idx < palette.Length ? palette[idx] : new XnaColor(0, 0, 0, 0);
                                break;
                                
                            case 16:
                                ushort pixel = BitConverter.ToUInt16(info.RawData, offset);
                                byte r16 = (byte)(((pixel >> 10) & 0x1F) * 255 / 31);
                                byte g16 = (byte)(((pixel >> 5) & 0x1F) * 255 / 31);
                                byte b16 = (byte)((pixel & 0x1F) * 255 / 31);
                                byte a16 = (r16 == 0 && g16 == 0 && b16 == 0) ? (byte)0 : (byte)255;
                                color = new XnaColor(r16, g16, b16, a16);
                                break;
                                
                            case 24:
                                byte b24 = info.RawData[offset + 0];
                                byte g24 = info.RawData[offset + 1];
                                byte r24 = info.RawData[offset + 2];
                                byte a24 = (r24 == 0 && g24 == 0 && b24 == 0) ? (byte)0 : (byte)255;
                                color = new XnaColor(r24, g24, b24, a24);
                                break;
                                
                            case 32:
                                byte b32 = info.RawData[offset + 0];
                                byte g32 = info.RawData[offset + 1];
                                byte r32 = info.RawData[offset + 2];
                                byte a32 = info.RawData[offset + 3];
                                color = new XnaColor(r32, g32, b32, a32);
                                break;
                                
                            default:
                                color = new XnaColor(0, 0, 0, 0);
                                break;
                        }
                    }
                    
                    colorData[y * info.Width + x] = color;
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        private Texture2D CreateTransparentTexture(int width, int height)
        {
            var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            var colorData = new XnaColor[width * height];
            for (int i = 0; i < colorData.Length; i++)
                colorData[i] = new XnaColor(0, 0, 0, 0);
            texture.SetData(colorData);
            return texture;
        }

        public Texture2D LoadTile(int fileId, int tileId)
        {
            var key = (fileId, tileId);
            if (_textureCache.ContainsKey(key))
                return _textureCache[key];

            if (tileId <= 0)
            {
                _textureCache[key] = null;
                return null;
            }

            if (!_fileBitmaps.ContainsKey(fileId))
            {
                if (!LoadGfxFile(fileId))
                {
                    _textureCache[key] = null;
                    return null;
                }
            }

            var list = _fileBitmaps[fileId];
            int idx;
            if (fileId == 3)
                idx = tileId + 1;
            else
                idx = tileId;

            if (idx < 1 || idx >= list.Count - (fileId == 3 ? 1 : 0))
            {
                _textureCache[key] = null;
                return null;
            }

            var info = list[idx];
            var tex = ConvertBitmapToTexture(info);
            _textureCache[key] = tex;
            return tex;
        }

        public void ClearCache()
        {
            foreach (var kv in _textureCache)
            {
                kv.Value?.Dispose();
            }
            _textureCache.Clear();
        }
    }
}