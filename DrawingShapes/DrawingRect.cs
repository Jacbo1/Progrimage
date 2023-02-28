using NewMath;
using SixLabors.ImageSharp.Processing;
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
                context.Fill(Color, new RectangleF((float)Pos.x, (float)Pos.y, (float)Size.x, (float)Size.y));
                return;
            }

            // Outlined rectangle
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                context.Fill(Color, new RectangleF((float)Pos.x, (float)Pos.y, (float)Size.x, (float)Thickness)); // Top
                context.Fill(Color, new RectangleF((float)Pos.x, (float)(Pos.y + Size.y - Thickness), (float)Size.x, (float)Thickness)); // Bottom
                context.Fill(Color, new RectangleF((float)Pos.x, (float)(Pos.y + Thickness), (float)Thickness, (float)(Size.y - Thickness * 2))); // Left
                context.Fill(Color, new RectangleF((float)(Pos.x + Size.x - Thickness), (float)(Pos.y + Thickness), (float)Thickness, (float)(Size.y - Thickness * 2))); // Right
                return;
            }

            // Extend outwards so the outline makes it bigger
            double2 pos2 = Pos - Thickness;
            double2 size2 = Size + Thickness * 2;
            context.Fill(Color, new RectangleF((float)pos2.x, (float)pos2.y, (float)size2.x, (float)Thickness)); // Top
            context.Fill(Color, new RectangleF((float)pos2.x, (float)(Pos.y + Size.y), (float)size2.x, (float)Thickness)); // Bottom
            context.Fill(Color, new RectangleF((float)pos2.x, (float)Pos.y, (float)Thickness, (float)Size.y)); // Left
            context.Fill(Color, new RectangleF((float)(Pos.x + Size.x), (float)Pos.y, (float)Thickness, (float)Size.y)); // Right
        }

        /// <summary>
        /// Gets the bounds of the rectangle.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetBounds()
        {
            return (Fill || ExtendInwards) ? new Rectangle((int)Pos.x, (int)Pos.y, (int)Size.x, (int)Size.y) : new Rectangle((int)(Pos.x - Thickness), (int)(Pos.y - Thickness), (int)(Size.x + Thickness * 2), (int)(Size.y + Thickness * 2));
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
                context.Fill(Color, new RectangleF((float)pos.x, (float)pos.y, (float)size.x, (float)size.y));
                return;
            }

            double thickness = ScaleThickness ? Math.Max(0, Thickness * Program.ActiveInstance.Zoom) : Thickness;

            // Outlined rectangle
            if (ExtendInwards)
            {
                // Extend inwards so the outline doesn't make it bigger
                context.Fill(Color, new RectangleF((float)pos.x, (float)pos.y, (float)size.x, (float)thickness)); // Top
                context.Fill(Color, new RectangleF((float)pos.x, (float)(pos.y + size.y - thickness), (float)size.x, (float)thickness)); // Bottom
                context.Fill(Color, new RectangleF((float)pos.x, (float)(pos.y + thickness), (float)thickness, (float)(size.y - thickness * 2))); // Left
                context.Fill(Color, new RectangleF((float)(pos.x + size.x - thickness), (float)(pos.y + thickness), (float)thickness, (float)(size.y - thickness * 2))); // Right
                return;
            }

            // Extend outwards so the outline makes it bigger
            double2 pos2 = pos - thickness;
            double2 size2 = size + thickness * 2;
            context.Fill(Color, new RectangleF((float)pos2.x, (float)pos2.y, (float)size2.x, (float)thickness)); // Top
            context.Fill(Color, new RectangleF((float)pos2.x, (float)(pos.y + size.y), (float)size2.x, (float)thickness)); // Bottom
            context.Fill(Color, new RectangleF((float)pos2.x, (float)pos.y, (float)thickness, (float)size.y)); // Left
            context.Fill(Color, new RectangleF((float)(pos.x + size.x), (float)pos.y, (float)thickness, (float)size.y)); // Right
        }
    }
}
