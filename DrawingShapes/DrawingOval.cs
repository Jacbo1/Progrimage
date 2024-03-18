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
            float centerx = (float)(Pos.X + Size.X * 0.5);
            float centery = (float)(Pos.Y + Size.Y * 0.5);

            if (Fill)
            {
                // Filled oval
                context.Fill(Color, new EllipsePolygon(centerx, centery, (float)Size.X, (float)Size.Y));
                return;
            }

            // Outlined oval
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                if (Size.X - Thickness >= 1 && Size.Y - Thickness >= 1)
                    context.Draw(Color, (float)Thickness, new EllipsePolygon(centerx, centery, (float)(Size.X - Thickness), (float)(Size.Y - Thickness)));
                return;
            }

            // Extend outwards so the outline makes it bigger
            context.Draw(Color, (float)Thickness, new EllipsePolygon(centerx, centery, (float)Size.X, (float)Size.Y));
        }

        /// <summary>
        /// Gets the bounds of the oval.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetBounds()
        {
            return (Fill || ExtendInwards) ? new Rectangle((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y) : new Rectangle((int)(Pos.X - Thickness), (int)(Pos.Y - Thickness), (int)(Size.X + Thickness * 2), (int)(Size.Y + Thickness * 2));
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
            float centerx = (float)(pos.X + size.X * 0.5);
            float centery = (float)(pos.Y + size.Y * 0.5);

            if (Fill)
            {
                // Filled oval
                context.Fill(Color, new EllipsePolygon(centerx, centery, (float)size.X, (float)size.Y));
                return;
            }

            float thickness = (float)(ScaleThickness ? Math.Max(0, Thickness * Program.ActiveInstance.Zoom) : Thickness);

            // Outlined oval
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                if (size.X - thickness >= 1 && size.Y - thickness >= 1)
                    context.Draw(Color, thickness, new EllipsePolygon(centerx, centery, (float)(size.X - thickness), (float)(size.Y - thickness)));
                return;
            }

            // Extend outwards so the outline makes it bigger
            context.Draw(Color, thickness, new EllipsePolygon(centerx, centery, (float)size.X, (float)size.Y));
        }
    }
}
