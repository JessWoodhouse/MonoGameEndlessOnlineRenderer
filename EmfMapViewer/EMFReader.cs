using System;
using System.Collections.Generic;
using System.IO;
using Moffat.EndlessOnline.SDK.Data;

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
        public List<List<GFXRow>> GfxRows { get; } = new(9);

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
            var hdr = reader.GetBytes(0x2E);
            if (hdr.Length < 3 || hdr[0] != (byte)'E' || hdr[1] != (byte)'M' || hdr[2] != (byte)'F')
                throw new InvalidOperationException("Invalid EMF file");

            var headerReader = new EoReader(hdr);
            Width = headerReader.Slice(0x25).GetChar();
            Height = headerReader.Slice(0x26).GetChar();
            FillTile = headerReader.Slice(0x27).GetShort();

            int n = reader.GetChar();
            reader.GetBytes(n * 8);
            
            n = reader.GetChar();
            reader.GetBytes(n * 4);
            
            n = reader.GetChar();
            reader.GetBytes(n * 12);

            int rows = reader.GetChar();
            for (int i = 0; i < rows; i++)
            {
                int y = reader.GetChar();
                int count = reader.GetChar();
                reader.GetBytes(count * 2);
            }

            rows = reader.GetChar();
            for (int i = 0; i < rows; i++)
            {
                int y = reader.GetChar();
                int count = reader.GetChar();
                reader.GetBytes(count * 8);
            }

            for (int layer = 0; layer < 9; layer++)
            {
                var layerRows = new List<GFXRow>();
                int count = reader.GetChar();
                
                for (int r = 0; r < count; r++)
                {
                    int y = reader.GetChar();
                    int c = reader.GetChar();
                    var tiles = new List<GFX>(c);
                    
                    for (int t = 0; t < c; t++)
                    {
                        int x = reader.GetChar();
                        int tileId = reader.GetShort();
                        tiles.Add(new GFX(x, tileId));
                    }
                    layerRows.Add(new GFXRow(y, tiles));
                }
                GfxRows.Add(layerRows);
            }
        }
    }
}