using NewMath;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.DrawingShapes
{
    public struct OverlayImage : IShape
    {
        public Image<Argb32> Image;
        public double2 Pos { get; set; }
        public bool Hidden { get; set; }

        public OverlayImage(Image<Argb32> image, int2 pos)
        {
            Image = image;
            Pos = pos;
            Hidden = false;
        }

        public void Draw(IImageProcessingContext context)
        {
            context.DrawImageSafe(Image, (int2)Pos);
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)Pos.x, (int)Pos.y, Image.Width, Image.Height);
        }

        public int2 GetSize()
        {
            return new int2(Image.Width, Image.Height);
        }

        public void DrawToRender(IImageProcessingContext context)
        {
            int2 pos = Util.CanvasToRender(Pos);
            int2 size = Math2.Max(1, Util.CanvasToRender(Pos + new int2(Image.Width, Image.Height)) - pos);
            using var temp = Image.Clone();
            temp.Mutate(op => op.Resize(size.x, size.y));
            context.DrawImageSafe(temp, pos);
        }
    }
}
