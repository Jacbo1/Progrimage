using System.Numerics;

namespace Progrimage
{
    public struct BrushState
    {
        public BrushPath Path;
        public int Size;
        public Vector4 Color;
        public bool IsPencil;

        public BrushState(BrushPath path, int size, Vector4 color, bool isPencil)
        {
            Path = path;
            Size = size;
            Color = color;
            IsPencil = isPencil;
        }
    }
}
