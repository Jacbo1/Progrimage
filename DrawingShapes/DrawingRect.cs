using NewMath;
using SixLabors.ImageSharp.Drawing.Processing;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using RectangleF = SixLabors.ImageSharp.RectangleF;
using Size = SixLabors.ImageSharp.Size;
using Progrimage.Utils;

namespace Progrimage.DrawingShapes
{
	public struct DrawingRect : IShape
    {
        public Color Color;
        public double Thickness;
        public double2 Size;
        public double2 Pos { get; set; }
        public bool Hidden { get; set; }
        public bool Fill, ExtendInwards, ScaleThickness = true, Transparent = false;

        /// <summary>
        /// Creates a filled rectangle struct for drawing.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        public DrawingRect(Color color, double2 pos, double2 size)
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
        /// Creates an outlined rectangle struct for drawing.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="thickness"></param>
        /// <param name="extendInwards"></param>
        public DrawingRect(Color color, double2 pos, double2 size, double thickness, bool extendInwards = true)
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
        /// Draws the rectangle.
        /// </summary>
        /// <param name="context"></param>
        public void Draw(IImageProcessingContext context)
        {
            if (Fill)
            {
                // Filled rectangle
                context.Fill(Color, new RectangleF((float)Pos.X, (float)Pos.Y, (float)Size.X, (float)Size.Y));
                return;
            }

            // Outlined rectangle
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                context.Fill(Color, new RectangleF((float)Pos.X, (float)Pos.Y, (float)Size.X, (float)Thickness)); // Top
                context.Fill(Color, new RectangleF((float)Pos.X, (float)(Pos.Y + Size.Y - Thickness), (float)Size.X, (float)Thickness)); // Bottom
                context.Fill(Color, new RectangleF((float)Pos.X, (float)(Pos.Y + Thickness), (float)Thickness, (float)(Size.Y - Thickness * 2))); // Left
                context.Fill(Color, new RectangleF((float)(Pos.X + Size.X - Thickness), (float)(Pos.Y + Thickness), (float)Thickness, (float)(Size.Y - Thickness * 2))); // Right
                return;
            }

            // Extend outwards so the outline makes it bigger
            double2 pos2 = Pos - Thickness;
            double2 size2 = Size + Thickness * 2;
            context.Fill(Color, new RectangleF((float)pos2.X, (float)pos2.Y, (float)size2.X, (float)Thickness)); // Top
            context.Fill(Color, new RectangleF((float)pos2.X, (float)(Pos.Y + Size.Y), (float)size2.X, (float)Thickness)); // Bottom
            context.Fill(Color, new RectangleF((float)pos2.X, (float)Pos.Y, (float)Thickness, (float)Size.Y)); // Left
            context.Fill(Color, new RectangleF((float)(Pos.X + Size.X), (float)Pos.Y, (float)Thickness, (float)Size.Y)); // Right
        }

        /// <summary>
        /// Gets the bounds of the rectangle.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetBounds()
        {
            return (Fill || ExtendInwards) ? new Rectangle((int)Pos.X, (int)Pos.Y, (int)Size.X, (int)Size.Y) : new Rectangle((int)(Pos.X - Thickness), (int)(Pos.Y - Thickness), (int)(Size.X + Thickness * 2), (int)(Size.Y + Thickness * 2));
        }

        /// <summary>
        /// Gets the size of the rectangle including the outlin
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
            if (Fill)
            {
                // Filled rectangle
                context.Fill(Color, new RectangleF((float)pos.X, (float)pos.Y, (float)size.X, (float)size.Y));
                return;
            }

            double thickness = ScaleThickness ? Math.Max(0, Thickness * Program.ActiveInstance.Zoom) : Thickness;

            // Outlined rectangle
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                context.Fill(Color, new RectangleF((float)pos.X, (float)pos.Y, (float)size.X, (float)thickness)); // Top
                context.Fill(Color, new RectangleF((float)pos.X, (float)(pos.Y + size.Y - thickness), (float)size.X, (float)thickness)); // Bottom
                context.Fill(Color, new RectangleF((float)pos.X, (float)(pos.Y + thickness), (float)thickness, (float)(size.Y - thickness * 2))); // Left
                context.Fill(Color, new RectangleF((float)(pos.X + size.X - thickness), (float)(pos.Y + thickness), (float)thickness, (float)(size.Y - thickness * 2))); // Right
                return;
            }

            // Extend outwards so the outline makes it bigger
            double2 pos2 = pos - thickness;
            double2 size2 = size + thickness * 2;
            context.Fill(Color, new RectangleF((float)pos2.X, (float)pos2.Y, (float)size2.X, (float)thickness)); // Top
            context.Fill(Color, new RectangleF((float)pos2.X, (float)(pos.Y + size.Y), (float)size2.X, (float)thickness)); // Bottom
            context.Fill(Color, new RectangleF((float)pos2.X, (float)pos.Y, (float)thickness, (float)size.Y)); // Left
            context.Fill(Color, new RectangleF((float)(pos.X + size.X), (float)pos.Y, (float)thickness, (float)size.Y)); // Right
        }
    }
}
