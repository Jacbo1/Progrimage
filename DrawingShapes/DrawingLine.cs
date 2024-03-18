using NewMath;
using Progrimage.Utils;
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
            context.DrawLine(Color, (float)Thickness, new PointF((float)Start.X, (float)Start.Y), new PointF((float)Stop.X, (float)Stop.Y));
        }

        public void DrawToRender(IImageProcessingContext context)
        {
            double2 start = Util.CanvasToRenderDouble(Start);
            double2 stop = Util.CanvasToRenderDouble(Stop);
            float thickness = (float)(ScaleThickness ? Thickness * Program.ActiveInstance.Zoom : Thickness);
            context.DrawLine(Color, thickness, new PointF((float)start.X, (float)start.Y), new PointF((float)stop.X, (float)stop.Y));
        }

        public SixLabors.ImageSharp.Rectangle GetBounds()
        {
            double2 delta = Stop - Start;
            double2 side = new double2(-delta.Y, delta.X);
            side *= Thickness / (2 * side.Length()); // Normalize and scale

            double2 a = Start + side;
            double2 b = Start - side;
            double2 c = Stop + side;
            double2 d = Stop - side;

            double2 min = Math2.Min(Math2.Min(Math2.Min(a, b), c), d);
            double2 max = Math2.Max(Math2.Max(Math2.Max(a, b), c), d);

            int2 imin = Math2.Floor(min);
            int2 size = Math2.Ceiling(max - min) + 1;

            return new SixLabors.ImageSharp.Rectangle(imin.X, imin.Y, size.X, size.Y);
        }

        public int2 GetSize() => GetBounds().Size();
    }
}
