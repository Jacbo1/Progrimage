using NewMath;
using Progrimage.Utils;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Size = SixLabors.ImageSharp.Size;

namespace Progrimage.DrawingShapes
{
    public struct DrawingOval : IShape
    {
        public Color Color;
        public double Thickness;
        public double2 Size;
        public double2 Pos { get; set; }
        public bool Hidden { get; set; }
        public bool Fill, ExtendInwards, ScaleThickness = true;

        /// <summary>
        /// Creates a filled oval struct for drawing.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        public DrawingOval(Color color, double2 pos, double2 size)
        {
            Color = color;
            Thickness = 0;
            Pos = pos;
            Size = size;
            Fill = true;
            ExtendInwards = true;
            Hidden = false;
        }

        /// <summary>
        /// Creates an outlined oval struct for drawing.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="thickness"></param>
        /// <param name="extendInwards"></param>
        public DrawingOval(Color color, double2 pos, double2 size, double thickness, bool extendInwards = true)
        {
            Color = color;
            Thickness = thickness;
            Pos = pos;
            Size = size;
            Fill = false;
            ExtendInwards = extendInwards;
            Hidden = false;
        }

        /// <summary>
        /// Draws the oval.
        /// </summary>
        /// <param name="context"></param>
        public void Draw(IImageProcessingContext context)
        {
            float centerx = (float)(Pos.x + Size.x * 0.5);
            float centery = (float)(Pos.y + Size.y * 0.5);

            if (Fill)
            {
                // Filled oval
                context.Fill(Color, new EllipsePolygon(centerx, centery, (float)Size.x, (float)Size.y));
                return;
            }

            // Outlined oval
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                if (Size.x - Thickness >= 1 && Size.y - Thickness >= 1)
                    context.Draw(Color, (float)Thickness, new EllipsePolygon(centerx, centery, (float)(Size.x - Thickness), (float)(Size.y - Thickness)));
                return;
            }

            // Extend outwards so the outline makes it bigger
            context.Draw(Color, (float)Thickness, new EllipsePolygon(centerx, centery, (float)Size.x, (float)Size.y));
        }

        /// <summary>
        /// Gets the bounds of the oval.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetBounds()
        {
            return (Fill || ExtendInwards) ? new Rectangle((int)Pos.x, (int)Pos.y, (int)Size.x, (int)Size.y) : new Rectangle((int)(Pos.x - Thickness), (int)(Pos.y - Thickness), (int)(Size.x + Thickness * 2), (int)(Size.y + Thickness * 2));
        }

        /// <summary>
        /// Gets the size of the oval including the outline.
        /// </summary>
        /// <returns></returns>
        public int2 GetSize()
        {
            return (int2)((Fill || ExtendInwards) ? Size : Size + Thickness * 2);
        }

        public void DrawToRender(IImageProcessingContext context)
        {
            double2 pos = Util.CanvasToRenderDouble(Pos);
            double2 size = Size * Program.ActiveInstance.Zoom;
            float centerx = (float)(pos.x + size.x * 0.5);
            float centery = (float)(pos.y + size.y * 0.5);

            if (Fill)
            {
                // Filled oval
                context.Fill(Color, new EllipsePolygon(centerx, centery, (float)size.x, (float)size.y));
                return;
            }

            float thickness = (float)(ScaleThickness ? Math.Max(0, Thickness * Program.ActiveInstance.Zoom) : Thickness);

            // Outlined oval
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                if (size.x - thickness >= 1 && size.y - thickness >= 1)
                    context.Draw(Color, thickness, new EllipsePolygon(centerx, centery, (float)(size.x - thickness), (float)(size.y - thickness)));
                return;
            }

            // Extend outwards so the outline makes it bigger
            context.Draw(Color, thickness, new EllipsePolygon(centerx, centery, (float)size.x, (float)size.y));
        }
    }
}
