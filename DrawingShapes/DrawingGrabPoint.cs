using NewMath;
using Progrimage.Utils;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.DrawingShapes
{
	internal struct DrawingGrabPoint : IShape
	{
		public double2 Pos { get; set; }
		public bool Hidden { get; set; }

		public void Draw(IImageProcessingContext context)
		{
			throw new NotImplementedException();
		}

		public void DrawToRender(IImageProcessingContext context)
		{
			// Move dot
			double2 pos = Util.CanvasToRenderDouble(Pos);
			new DrawingOval(Color.Gray, pos - Defs.DotOuterSize / 2, Defs.DotOuterSize).Draw(context);
			new DrawingOval(Color.White, pos - Defs.DotInnerSize / 2, Defs.DotInnerSize).Draw(context);
		}

		public Rectangle GetBounds()
		{
			int2 size = Defs.DotOuterSize / 2;
			int2 min = Math2.Floor(Pos - size);
			int2 max = Math2.Ceiling(Pos + size);
			size = max - min + 1;
			return new Rectangle(min.X, min.Y, size.X, size.Y);
		}

		public int2 GetSize()
		{
			int2 size = Defs.DotOuterSize / 2;
			int2 min = Math2.Floor(Pos - size);
			int2 max = Math2.Ceiling(Pos + size);
			return max - min + 1;
		}

		public double2? CheckProximity()
		{
			if (MainWindow.MousePosCanvasDouble.DistanceSqr(Pos) > Defs.CURSOR_CHANGE_RADIUS_SQR) return null;
			return Pos - MainWindow.MousePosCanvasDouble;
		}
	}
}
