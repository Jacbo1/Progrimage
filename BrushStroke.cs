using NewMath;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace Progrimage
{
    public class BrushTextureArray
    {
        public byte[,]? ByteData { get; private set; }
        public ushort[,]? ShortData { get; private set; }
        public ushort MaxValue { get; private set; }
        private int _size;

        public BrushTextureArray(BrushTexturePair tex, int brushSize)
        {
            _size = brushSize;
            if (tex.L8)
            {
                // L8
                ShortData = null;
                MaxValue = byte.MaxValue;
                ByteData = new byte[brushSize, brushSize];
                using var img = Image.Load<L8>(tex.Path);
                img.Mutate(x => x.Resize(brushSize, brushSize, KnownResamplers.Box));
                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < brushSize; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < Math.Min(row.Length, brushSize); x++)
                        {
                            ByteData[x, y] = row[x].PackedValue;
                        }
                    }
                });
            }
            else
            {
                // L16
                ShortData = new ushort[brushSize, brushSize];
                MaxValue = ushort.MaxValue;
                ByteData = null;
                using var img = Image.Load<L16>(tex.Path);
                img.Mutate(x => x.Resize(brushSize, brushSize, KnownResamplers.Box));
                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < brushSize; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < Math.Min(row.Length, brushSize); x++)
                        {
                            ShortData[x, y] = row[x].PackedValue;
                        }
                    }
                });
            }
        }

        public BrushTextureArray(int brushSize)
        {
            _size = brushSize;
            if (brushSize == 1)
            {
                // The radius check has an issue with a brush size of 1
                ByteData = new byte[1, 1];
                ByteData[0, 0] = byte.MaxValue;
                return;
            }

            double radius = brushSize * 0.5;
            double radiusSqr = radius * radius;
            MaxValue = byte.MaxValue;
            ByteData = new byte[brushSize, brushSize];
            for (int x = 0; x < brushSize; x++)
            {
                for (int y = 0; y < brushSize; y++)
                {
                    double x1 = x - radius;
                    double y1 = y - radius;
                    ByteData[x, y] = (x1 * x1 + y1 * y1 <= radiusSqr) ? byte.MaxValue : (byte)0;
                }
            }
        }

        /// <summary>
        /// Returns a bilinearly interpolated pixel
        /// </summary>
        /// <param name="x">X coordinate in the mask</param>
        /// <param name="y">Y coordinate in the mask</param>
        /// <returns>A bilinearly interpolated pixel value</returns>
        public ushort GetPixel(double x, double y)
        {
            // Corner pixel coordinates
            int x1 = (int)Math.Floor(x);
            int x2 = (int)Math.Ceiling(x);
            int y1 = (int)Math.Floor(y);
            int y2 = (int)Math.Ceiling(y);

            // Coordinates out of bounds
            bool x1_0 = x1 < 0 || x1 >= _size;
            bool x2_0 = x2 < 0 || x2 >= _size;
            bool y1_0 = y1 < 0 || y1 >= _size;
            bool y2_0 = y2 < 0 || y2 >= _size;

            // Get corner pixel values
            ushort pixel00 = (x1_0 || y1_0) ? (ushort)0 : (ByteData?[x1, y1] ?? ShortData![x1, y1]);
            ushort pixel01 = (x1_0 || y2_0) ? (ushort)0 : (ByteData?[x1, y2] ?? ShortData![x1, y2]);
            ushort pixel10 = (x2_0 || y1_0) ? (ushort)0 : (ByteData?[x2, y1] ?? ShortData![x2, y1]);
            ushort pixel11 = (x2_0 || y2_0) ? (ushort)0 : (ByteData?[x2, y2] ?? ShortData![x2, y2]);

            double xFrac = x % 1;
            double yFrac = y % 1;

            if (xFrac < 0) xFrac++;
            if (yFrac < 0) yFrac++;

            // Interpolate
            double top = pixel00 * (1 - xFrac) + pixel10 * xFrac;
            double bottom = pixel01 * (1 - xFrac) + pixel11 * xFrac;
            double pixel = top * (1 - yFrac) + bottom * yFrac;
            return (ushort)Math.Round(pixel, MidpointRounding.AwayFromZero);
        }
    }

    public struct BrushTexturePair
    {
        public string Path;
        public bool L8;

        public BrushTexturePair(string path, bool L8)
        {
            Path = path;
            this.L8 = L8;
        }
    }

    public class BrushStroke
    {
        public BrushTextureArray BrushTexture { get; private set; }
        public bool StrokeIs8Bit { get; private set; }
        public int2? StrokeMin, StrokeMax;
        public byte[,] StrokeMaskByte;
        public ushort[,] StrokeMaskShort;
        public int BrushSize;

        private BrushTexturePair _brushTexturePair;

        public BrushStroke(BrushTexturePair pair, int size)
        {
            StrokeIs8Bit = !pair.L8;
            SetTexture(pair, size);
        }

        public BrushStroke(int size)
        {
            SetPencil(size);
        }

        public void SetTexture(BrushTexturePair pair, int size)
        {
            BrushSize = size;
            _brushTexturePair = pair;
            BrushTexture = new BrushTextureArray(pair, size);

            if (StrokeIs8Bit == pair.L8) return;

            if (pair.L8)
            {
                StrokeMaskByte = new byte[1, 1];
                StrokeMaskShort = null;
            }
            else
            {
                StrokeMaskByte = null;
                StrokeMaskShort = new ushort[1, 1];
            }
            StrokeMin = null;
            StrokeMax = null;
            StrokeIs8Bit = pair.L8;
        }

        public void SetTexture(BrushTexturePair pair) => SetTexture(pair, BrushSize);

        public void SetSize(int size)
        {
            if (BrushSize == size) return;
            BrushSize = size;
            BrushTexture = new BrushTextureArray(_brushTexturePair, size);
        }

        public void SetPencil(int size)
        {
            BrushSize = size;
            BrushTexture = new BrushTextureArray(size);

            if (StrokeIs8Bit) return;

            StrokeMaskByte = new byte[1, 1];
            StrokeMaskShort = null;
            StrokeMin = null;
            StrokeMax = null;
            StrokeIs8Bit = true;
        }

        public void ClearMask()
        {
            if (StrokeIs8Bit)
            {
                StrokeMaskByte = new byte[1, 1];
                StrokeMaskShort = null;
            }
            else
            {
                StrokeMaskByte = null;
                StrokeMaskShort = new ushort[1, 1];
            }
            StrokeMin = null;
            StrokeMax = null;
        }
    }
}
