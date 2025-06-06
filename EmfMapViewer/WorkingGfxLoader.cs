using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PELoaderLib;

namespace EmfMapViewer
{
    public class WorkingGfxLoader : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        public string GfxPath { get; set; } = "gfx";
        private readonly Dictionary<int, IPEFile> _peFiles = new();
        private readonly Dictionary<(int fileId, int tileId), Texture2D> _textureCache = new();

        public WorkingGfxLoader(GraphicsDevice graphicsDevice, string gfxPath = "gfx")
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            GfxPath = gfxPath;
        }

        public Texture2D LoadTile(int fileId, int tileId)
        {
            var key = (fileId, tileId);
            if (_textureCache.TryGetValue(key, out var cachedTex))
                return cachedTex;

            if (tileId <= 0)
            {
                _textureCache[key] = null;
                return null;
            }

            if (!_peFiles.ContainsKey(fileId))
            {
                string filename = Path.Combine(GfxPath, $"gfx{fileId:000}.egf");
                if (!File.Exists(filename))
                {
                    _textureCache[key] = null;
                    return null;
                }
                try
                {
                    var pe = new PEFile(filename);
                    pe.Initialize();
                    _peFiles[fileId] = pe;
                }
                catch
                {
                    _textureCache[key] = null;
                    return null;
                }
            }

            int resourceId = 100 + tileId;

            ReadOnlyMemory<byte> bmpData;
            try
            {
                bmpData = _peFiles[fileId].GetEmbeddedBitmapResourceByID(resourceId);
            }
            catch
            {
                _textureCache[key] = null;
                return null;
            }

            if (bmpData.Length == 0)
            {
                _textureCache[key] = null;
                return null;
            }

            try
            {
                using var ms = new MemoryStream(bmpData.ToArray());
                Texture2D tex = Texture2D.FromStream(_graphicsDevice, ms);

                int width = tex.Width;
                int height = tex.Height;
                var pixels = new Color[width * height];
                tex.GetData(pixels);

                bool anyBlack = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color c = pixels[i];
                    if (c.R == 0 && c.G == 0 && c.B == 0 && c.A == 255)
                    {
                        pixels[i] = Color.Transparent;
                        anyBlack = true;
                    }
                }

                if (anyBlack)
                    tex.SetData(pixels);

                _textureCache[key] = tex;
                return tex;
            }
            catch
            {
                _textureCache[key] = null;
                return null;
            }
        }

        public void ClearCache()
        {
            foreach (var kv in _textureCache)
                kv.Value?.Dispose();
            _textureCache.Clear();

            foreach (var pe in _peFiles.Values)
                pe.Dispose();
            _peFiles.Clear();
        }

        public void Dispose()
        {
            ClearCache();
        }
    }
}
