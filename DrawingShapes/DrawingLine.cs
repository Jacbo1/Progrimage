using NewMath;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;

namespace Progrimage.DrawingShapes
{
    public struct DrawingLine : IShape
    {
        public double Thickness;
        public double2 Start, Stop;
        public bool ScaleThickness = true;
        public Color Color;
        public bool Hidden { get; set; }

        public double2 Pos
        {
            get => Math2.Min(Start, Stop);
            set
            {
                double2 delta = value - Math2.Min(Start, Stop);
                Start += delta;
                Stop += delta;
            }
        }

        public DrawingLine(Color color, double2 start, double2 stop, double thickness)
        {
            Hidden = false;
            Start = start;
            Stop = stop;
            Thickness = thickness;
            Color = color;
        }

        public void Draw(IImageProcessingContext context)
        {
            context.DrawLines(Color, (float)Thickness, new PointF((float)Start.x, (float)Start.y), new PointF((float)Stop.x, (float)Stop.y));
        }

        public void DrawToRender(IImageProcessingContext context)
        {
            double2 start = Util.CanvasToRenderDouble(Start);
            double2 stop = Util.CanvasToRenderDouble(Stop);
            float thickness = (float)(ScaleThickness ? Thickness * Program.ActiveInstance.Zoom : Thickness);
            context.DrawLines(Color, thickness, new PointF((float)start.x, (float)start.y), new PointF((float)stop.x, (float)stop.y));
        }

        public SixLabors.ImageSharp.Rectangle GetBounds()
        {
            double2 delta = Stop - Start;
            double2 side = new double2(-delta.y, delta.x);
            side *= Thickness / (2 * side.Length()); // Normalize and scale

            double2 a = Start + side;
            double2 b = Start - side;
            double2 c = Stop + side;
            double2 d = Stop - side;

            double2 min = Math2.Min(Math2.Min(Math2.Min(a, b), c), d);
            double2 max = Math2.Max(Math2.Max(Math2.Max(a, b), c), d);

            int2 imin = Math2.FloorToInt(min);
            int2 size = Math2.CeilingToInt(max - min) + 1;

            return new SixLabors.ImageSharp.Rectangle(imin.x, imin.y, size.x, size.y);
        }

        public int2 GetSize() => GetBounds().Size();
    }
}
