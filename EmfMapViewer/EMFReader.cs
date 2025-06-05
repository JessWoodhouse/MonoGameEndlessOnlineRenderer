using System;
using System.Collections.Generic;
using System.IO;

namespace EmfMapViewer
{
    public class GFX
    {
        public int X { get; set; }
        public int Tile { get; set; }

        public GFX(int x, int tile)
        {
            X = x;
            Tile = tile;
        }
    }

    public class GFXRow
    {
        public int Y { get; set; }
        public List<GFX> Tiles { get; set; }

        public GFXRow(int y, List<GFX> tiles)
        {
            Y = y;
            Tiles = tiles;
        }
    }

    public class EMFReader
    {
        private readonly string _filename;
        private byte[] _data;
        private int _pos;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FillTile { get; private set; }
        public List<List<GFXRow>> GfxRows { get; private set; }

        public EMFReader(string filename)
        {
            _filename = filename;
            GfxRows = new List<List<GFXRow>>(9);
        }

        private int Eon1(int b)
        {
            if (b == 0 || b == 254) b = 1;
            return b - 1;
        }

        private int Eon2(int b1, int b2)
        {
            if (b1 == 0 || b1 == 254) b1 = 1;
            if (b2 == 0 || b2 == 254) b2 = 1;
            return (b2 - 1) * 253 + (b1 - 1);
        }

        private byte[] ReadBytes(int n)
        {
            var chunk = new byte[n];
            Array.Copy(_data, _pos, chunk, 0, n);
            _pos += n;
            return chunk;
        }

        public bool Load()
        {
            try
            {
                _data = File.ReadAllBytes(_filename);
                _pos = 0;

                var hdr = ReadBytes(0x2E);
                if (hdr.Length < 3 || hdr[0] != (byte)'E' || hdr[1] != (byte)'M' || hdr[2] != (byte)'F')
                    return false;

                Width    = Eon1(hdr[0x25]);
                Height   = Eon1(hdr[0x26]);
                FillTile = Eon2(hdr[0x27], hdr[0x28]);

                _pos = 0x2E;

                int npcCount = Eon1(_data[_pos]);
                _pos += 1 + npcCount * 8;

                int u1Count = Eon1(_data[_pos]);
                _pos += 1 + u1Count * 4;

                int chestCount = Eon1(_data[_pos]);
                _pos += 1 + chestCount * 12;

                int tileRowCount = Eon1(_data[_pos]);
                _pos += 1;
                for (int i = 0; i < tileRowCount; i++)
                {
                    byte[] rh = ReadBytes(2);
                    int cnt = Eon1(rh[1]);
                    _pos += cnt * 2;
                }

                int warpRowCount = Eon1(_data[_pos]);
                _pos += 1;
                for (int i = 0; i < warpRowCount; i++)
                {
                    byte[] rh = ReadBytes(2);
                    int cnt = Eon1(rh[1]);
                    _pos += cnt * 8;
                }

                for (int layer = 0; layer < 9; layer++)
                {
                    var layerRows = new List<GFXRow>();
                    int outerSize = Eon1(_data[_pos]);
                    _pos += 1;

                    for (int r = 0; r < outerSize; r++)
                    {
                        byte[] rh = ReadBytes(2);
                        int rowY = Eon1(rh[0]);
                        int cnt  = Eon1(rh[1]);
                        var tiles = new List<GFX>(cnt);

                        for (int t = 0; t < cnt; t++)
                        {
                            byte[] gb = ReadBytes(3);
                            int gx  = Eon1(gb[0]);
                            int tid = Eon2(gb[1], gb[2]);
                            tiles.Add(new GFX(gx, tid));
                        }

                        layerRows.Add(new GFXRow(rowY, tiles));
                    }

                    GfxRows.Add(layerRows);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading EMF file {_filename}: {ex.Message}");
                return false;
            }
        }
    }
}