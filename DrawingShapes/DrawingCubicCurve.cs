﻿using NewMath;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using PointF = SixLabors.ImageSharp.PointF;
using Color = SixLabors.ImageSharp.Color;
using Progrimage.Utils;

namespace Progrimage.DrawingShapes
{
	internal struct DrawingCubicCurve : IShape
	{
		public float Thickness = 5;
		public Color Color = Color.Black;
		public double2[] Points = new double2[4];
		public double2 Pos
		{
			get => Math2.Min(Math2.Min(Math2.Min(Points[0], Points[1]), Points[2]), Points[3]) - Thickness;
			set
			{
				double2 delta = value - Pos;
				Points[0] += delta;
				Points[1] += delta;
				Points[2] += delta;
				Points[3] += delta;
			}
		}
		public bool Hidden { get; set; }

		public DrawingCubicCurve()
		{
			Hidden = false;
		}

		public DrawingCubicCurve(Color color, float thickness, double2 p1, double2 p2, double2 p3, double2 p4)
		{
			Hidden = false;
			Color = color;
			Thickness = thickness;
			Points[0] = p1;
			Points[1] = p2;
			Points[2] = p3;
			Points[3] = p4;
		}

		public void Draw(IImageProcessingContext context)
		{
			PointF a = new PointF((float)Points[0].X, (float)Points[0].Y);
			PointF b = new PointF((float)Points[1].X, (float)Points[1].Y);
			PointF c = new PointF((float)Points[2].X, (float)Points[2].Y);
			PointF d = new PointF((float)Points[3].X, (float)Points[3].Y);
			context.Draw(Color, Thickness, new PathBuilder().AddCubicBezier(a, b, c, d).Build());
		}

		public void DrawToRender(IImageProcessingContext context)
		{
			double2 p0 = Util.CanvasToRenderDouble(Points[0]);
			double2 p1 = Util.CanvasToRenderDouble(Points[1]);
			double2 p2 = Util.CanvasToRenderDouble(Points[2]);
			double2 p3 = Util.CanvasToRenderDouble(Points[3]);
			PointF a = new PointF((float)p0.X, (float)p0.Y);
			PointF b = new PointF((float)p1.X, (float)p1.Y);
			PointF c = new PointF((float)p2.X, (float)p2.Y);
			PointF d = new PointF((float)p3.X, (float)p3.Y);
			context.Draw(Color, Thickness, new PathBuilder().AddCubicBezier(a, b, c, d).Build());
		}

		public SixLabors.ImageSharp.Rectangle GetBounds()
		{
			(int2 pos, int2 size) = GetSizeAndMin();
			return new SixLabors.ImageSharp.Rectangle(pos.X, pos.Y, size.X, size.Y);
		}

		public int2 GetSize() => GetSizeAndMin().Item2;

		private (int2, int2) GetSizeAndMin()
		{
			PointF a = new PointF((float)Points[0].X, (float)Points[0].Y);
			PointF b = new PointF((float)Points[1].X, (float)Points[1].Y);
			PointF c = new PointF((float)Points[2].X, (float)Points[2].Y);
			PointF d = new PointF((float)Points[3].X, (float)Points[3].Y);
			var bounds = new PathBuilder().AddCubicBezier(a, b, c, d).Build().Bounds;

			int2 min = Math2.FloorToInt(new double2(bounds.X, bounds.Y) - Thickness);
			int2 max = Math2.CeilingToInt(new double2(bounds.X + bounds.Width, bounds.Y + bounds.Height) + Thickness);
			return (min, max - min);
		}
	}
}
