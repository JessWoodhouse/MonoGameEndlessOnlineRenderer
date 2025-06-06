using System;
using System.Collections.Generic;
using System.IO;
using Moffat.EndlessOnline.SDK.Data;
using Moffat.EndlessOnline.SDK.Protocol.Map;

namespace EmfMapViewer
{
    public class GFX
    {
        public int X { get; }
        public int Tile { get; }
        public GFX(int x, int tile) { X = x; Tile = tile; }
    }

    public class GFXRow
    {
        public int Y { get; }
        public List<GFX> Tiles { get; }
        public GFXRow(int y, List<GFX> tiles) { Y = y; Tiles = tiles; }
    }

    public class EMFReader
    {
        private readonly string _filename;
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FillTile { get; private set; }
        public List<List<GFXRow>> GfxRows { get; set; } = new(9);

        public EMFReader(string filename) => _filename = filename;

        public bool Load()
        {
            try
            {
                var data = File.ReadAllBytes(_filename);
                var reader = new EoReader(data);
                Deserialize(reader);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Deserialize(EoReader reader)
        {
            var emf = new Emf();
            emf.Deserialize(reader);

            Width = emf.Width;
            Height = emf.Height;
            FillTile = emf.FillTile;

            GfxRows = [.. emf.GraphicLayers.Select((layer) =>
                layer.GraphicRows.Select((row) =>
                    new GFXRow(row.Y, [.. row.Tiles.Select((tile) => new GFX(tile.X, tile.Graphic)
                    )])
                ).ToList()
            )];
        }
    }
}