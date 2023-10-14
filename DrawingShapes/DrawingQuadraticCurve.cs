using NewMath;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using PointF = SixLabors.ImageSharp.PointF;
using Color = SixLabors.ImageSharp.Color;
using Progrimage.Utils;

namespace Progrimage.DrawingShapes
{
	internal struct DrawingQuadraticCurve : IShape
	{
		public float Thickness = 5;
		public Color Color = Color.Black;
		public double2[] Points = new double2[3];
		public double2 Pos
		{
			get => Math2.Min(Math2.Min(Points[0], Points[1]), Points[2]) - Thickness;
			set
			{
				double2 delta = value - Pos;
				Points[0] += delta;
				Points[1] += delta;
				Points[2] += delta;
			}
		}
		public bool Hidden { get; set; }

		public DrawingQuadraticCurve()
		{
			Hidden = false;
		}

		public DrawingQuadraticCurve(Color color, float thickness, double2 p1, double2 p2, double2 p3)
		{
			Hidden = false;
			Color = color;
			Thickness = thickness;
			Points[0] = p1;
			Points[1] = p2;
			Points[2] = p3;
		}

		public void Draw(IImageProcessingContext context)
		{
			PointF a = new PointF((float)Points[0].x, (float)Points[0].y);
			PointF b = new PointF((float)Points[1].x, (float)Points[1].y);
			PointF c = new PointF((float)Points[2].x, (float)Points[2].y);
			context.Draw(Color, Thickness, new PathBuilder().AddQuadraticBezier(a, b, c).Build());
		}

		public void DrawToRender(IImageProcessingContext context)
		{
			double2 p0 = Util.CanvasToRenderDouble(Points[0]);
			double2 p1 = Util.CanvasToRenderDouble(Points[1]);
			double2 p2 = Util.CanvasToRenderDouble(Points[2]);
			PointF a = new PointF((float)p0.x, (float)p0.y);
			PointF b = new PointF((float)p1.x, (float)p1.y);
			PointF c = new PointF((float)p2.x, (float)p2.y);
			context.Draw(Color, Thickness, new PathBuilder().AddQuadraticBezier(a, b, c).Build());
		}

		public SixLabors.ImageSharp.Rectangle GetBounds()
		{
			(int2 pos, int2 size) = GetSizeAndMin();
			return new SixLabors.ImageSharp.Rectangle(pos.x, pos.y, size.x, size.y);
		}

		public int2 GetSize() => GetSizeAndMin().Item2;

		private (int2, int2) GetSizeAndMin()
		{
			PointF a = new PointF((float)Points[0].x, (float)Points[0].y);
			PointF b = new PointF((float)Points[1].x, (float)Points[1].y);
			PointF c = new PointF((float)Points[2].x, (float)Points[2].y);
			var bounds = new PathBuilder().AddQuadraticBezier(a, b, c).Build().Bounds;

			int2 min = Math2.FloorToInt(new double2(bounds.X, bounds.Y) - Thickness);
			int2 max = Math2.CeilingToInt(new double2(bounds.X + bounds.Width, bounds.Y + bounds.Height) + Thickness);
			return (min, max - min);
		}
	}
}
