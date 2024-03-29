﻿using NewMath;
using Progrimage.Utils;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using Color = SixLabors.ImageSharp.Color;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace Progrimage.DrawingShapes
{
	public struct DrawingCircleCursor : IShape
	{
		public double2 Pos { get; set; }
		public double2 Size;
		public bool Hidden { get; set; }

		public DrawingCircleCursor(double2 pos, double2 size)
		{
			Pos = pos;
			Size = size;
			Hidden = false;
		}

		public void Draw(IImageProcessingContext context)
		{
			throw new NotImplementedException();
		}

		public void DrawToRender(IImageProcessingContext context)
		{
			throw new NotImplementedException();
		}

		public void DrawToRender(IImageProcessingContext context, Image<Argb32> image)
		{
			const float THICKNESS = 1.5f;
			double2 pos = Util.CanvasToRenderDouble(Pos);
			double2 size = Size * Program.ActiveInstance.Zoom;
			double2 radius = size * 0.5;
			int2 min = Math2.Floor(pos - radius);
			int2 max = Math2.Ceiling(pos + radius);
			int2 isize = max - min + 1;

			if (!Util.Overlaps(context, min, isize)) return; // Does not overlap

			using var tempImage = new Image<L8>(isize.X, isize.Y);
			if (size.X - THICKNESS <= 0 || size.Y - THICKNESS <= 0)
				tempImage.Mutate(op => op.Draw(Color.White, THICKNESS, new EllipsePolygon((float)(pos.X - min.X), (float)(pos.Y - min.Y), (float)size.X, (float)size.Y)));
			else tempImage.Mutate(op => op.Draw(Color.White, THICKNESS, new EllipsePolygon((float)(pos.X - min.X), (float)(pos.Y - min.Y), (float)(size.X - THICKNESS), (float)(size.Y - THICKNESS))));

			int2 imageSize = new int2(image.Width, image.Height);
			int2 imageOffset = Math2.Max(-min, 0);
			int2 strokeOffset = Math2.Max(min, 0);
			int2 overlapMin = Math2.Max(min, 0);
			int2 overlapMax = Math2.Min(max, imageSize - 1);
			int2 overlapSize = overlapMax - overlapMin + 1;
			for (int y = 0; y < overlapSize.Y; y++)
			{
				var imageRow = image.DangerousGetPixelRowMemory(y + strokeOffset.Y).Span;
				var tempRow = tempImage.DangerousGetPixelRowMemory(y + imageOffset.Y).Span;

				for (int x = 0; x < overlapSize.X; x++)
				{
					if (tempRow[x + imageOffset.X].PackedValue == 0) continue;
					const float BRIGHTNESS = 255 * 0.25f;
					float lerp = tempRow[x + imageOffset.X].PackedValue / 255f;
					float ilerp = 1 - lerp;
					ref var pixel = ref imageRow[x + strokeOffset.X];
					pixel.R = (byte)Math.Min(Math.Round(pixel.R * ilerp + (255 - pixel.R + BRIGHTNESS) * lerp), 255);
					pixel.G = (byte)Math.Min(Math.Round(pixel.G * ilerp + (255 - pixel.G + BRIGHTNESS) * lerp), 255);
					pixel.B = (byte)Math.Min(Math.Round(pixel.B * ilerp + (255 - pixel.B + BRIGHTNESS) * lerp), 255);
		}
			}
		}

		public SixLabors.ImageSharp.Rectangle GetBounds()
		{
			throw new NotImplementedException();
		}

		public int2 GetSize()
		{
			throw new NotImplementedException();
		}
	}
}
