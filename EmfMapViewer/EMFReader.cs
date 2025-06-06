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
        private EoReader _r;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FillTile { get; private set; }
        public List<List<GFXRow>> GfxRows { get; } = new(9);

        public EMFReader(string filename) => _filename = filename;

        int E1(int b) => (b = (b == 0 || b == 254 ? 1 : b)) - 1;
        int E2(int b1, int b2)
        {
            b1 = (b1 == 0 || b1 == 254 ? 1 : b1);
            b2 = (b2 == 0 || b2 == 254 ? 1 : b2);
            return (b2 - 1) * 253 + (b1 - 1);
        }

        public bool Load()
        {
            try
            {
                var data = File.ReadAllBytes(_filename);
                _r = new EoReader(data);
                var hdr = _r.GetBytes(0x2E);
                if (hdr.Length < 3 || hdr[0] != (byte)'E' || hdr[1] != (byte)'M' || hdr[2] != (byte)'F')
                    return false;

                Width    = E1(hdr[0x25]);
                Height   = E1(hdr[0x26]);
                FillTile = E2(hdr[0x27], hdr[0x28]);

                int n = E1(_r.GetByte()); _r.GetBytes(n * 8);
                n = E1(_r.GetByte()); _r.GetBytes(n * 4);
                n = E1(_r.GetByte()); _r.GetBytes(n * 12);

                int rows = E1(_r.GetByte());
                for (int i = 0; i < rows; i++)
                {
                    var rh = _r.GetBytes(2);
                    _r.GetBytes(E1(rh[1]) * 2);
                }

                rows = E1(_r.GetByte());
                for (int i = 0; i < rows; i++)
                {
                    var rh = _r.GetBytes(2);
                    _r.GetBytes(E1(rh[1]) * 8);
                }

                for (int layer = 0; layer < 9; layer++)
                {
                    var layerRows = new List<GFXRow>();
                    int count = E1(_r.GetByte());
                    for (int r = 0; r < count; r++)
                    {
                        var rh = _r.GetBytes(2);
                        int y = E1(rh[0]), c = E1(rh[1]);
                        var tiles = new List<GFX>(c);
                        for (int t = 0; t < c; t++)
                        {
                            var gb = _r.GetBytes(3);
                            tiles.Add(new GFX(E1(gb[0]), E2(gb[1], gb[2])));
                        }
                        layerRows.Add(new GFXRow(y, tiles));
                    }
                    GfxRows.Add(layerRows);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
