using NewMath;
using Color = SixLabors.ImageSharp.Color;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Advanced;
using SixLabors.Fonts;
using SystemFonts = SixLabors.Fonts.SystemFonts;
using SizeF = SixLabors.ImageSharp.SizeF;
using PointF = SixLabors.ImageSharp.PointF;

namespace Progrimage.Utils
{
	internal static class IconGenerator
    {
        public static int2 size = new int2(256);
        public static Color color = Color.White;
        public static string TexturesDir = @"Assets\Textures\";
        public static string ToolsDir = TexturesDir + @"Tools\";
        public static string OutputDir = @"Generated\";

        public static void MakeRectTool()
        {
			//using (var img = new Image<A8>(size.X, size.Y))
			using var img = new Image<La16>(size.X, size.Y);
			var _thickness = 25;
			int2 size_ = size * 3 / 4;
			int2 pos = (size - size_) / 2;
			img.Mutate(i =>
			{
				i.Clear(new Color(new Argb32(0, 0, 0, 0)));
				i.Fill(color, new Rectangle(pos.X, pos.Y, size_.X, _thickness)); // Top
				i.Fill(color, new Rectangle(pos.X, pos.Y + size_.Y - _thickness, size_.X, _thickness)); // Bottom
				i.Fill(color, new Rectangle(pos.X, pos.Y + _thickness, _thickness, size_.Y - _thickness * 2)); // Left
				i.Fill(color, new Rectangle(pos.X + size_.X - _thickness, pos.Y + _thickness, _thickness, size_.Y - _thickness * 2)); // Right
			});
			Directory.CreateDirectory(OutputDir);
			//Util.SaveA8Image(img, OutputDir + "rect2.png", false);
			img.SaveAsPng(OutputDir + "rect.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
		}

        public static void MakeOvalTool()
        {
            using var img = new Image<La16>(size.X, size.Y);
            var _thickness = 25;
            int2 size_ = size * 3 / 4;
            int2 pos = (size - size_) / 2;
            var center = size / 2.0;
            img.Mutate(i =>
            {
                i.Draw(color, _thickness, new EllipsePolygon((float)center.X, (float)center.Y, size.X - _thickness, size.Y - _thickness));
            });
            Directory.CreateDirectory(OutputDir);
            img.SaveAsPng(OutputDir + "oval.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
        }

        //public static void MakeMaqueSelect()
        //{
        //    using (var src = Image.Load<Argb32>(ToolsDir + "marque_select.png"))
        //    {
        //        using (var img = new Image<La16>(src.Width, src.Height))
        //        {
        //            img.ProcessPixelRows(img_ =>
        //            {
        //                for (int y = 0; y < img.Height; y++)
        //                {
        //                    var rows = src.DangerousGetPixelRowMemory(y).ToArray();
        //                    var rowi = img_.GetRowSpan(y);
        //                    for (int x = 0; x < rowi.Length; x++)
        //                    {
        //                        rowi[x].L = rows[x].R;
        //                        rowi[x].A = rowi[x].L;
        //                    }
        //                }
        //            });
        //            Directory.CreateDirectory(OutputDir);
        //            img.SaveAsPng(OutputDir + "marque_select.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
        //        }
        //    }
        //}

        public static void MakeAddScriptTool()
        {
            using (var img = new Image<Argb32>(size.X, size.Y))
            {
                var font = SystemFonts.CreateFont("Consolas", size.Y);

                //Console.WriteLine("Size: " + size.Y);
                //Console.WriteLine("Size 2: " + font.Size);
                //Console.WriteLine("Line height: " + (font.FontMetrics.LineHeight * font.Size / font.FontMetrics.UnitsPerEm));
                //return;

                var textOptions = new TextOptions(font);
                //{
                //    HorizontalAlignment = SixLabors.Fonts.HorizontalAlignment.Center,
                //    VerticalAlignment = VerticalAlignment.Center,
                //    Origin = size / 2
                //};
                var glyphs = TextBuilder.GenerateGlyphs("</>", textOptions);
                glyphs = glyphs.Scale(size.X / glyphs.Bounds.Width);
                glyphs = glyphs.Translate(((size - new double2(glyphs.Bounds.Width, glyphs.Bounds.Height)) / 2 - new double2(glyphs.Bounds.X, glyphs.Bounds.Y)).ToPointF());
                img.Mutate(i =>
                {
                    i.Fill(color, glyphs);
                    //i.DrawText(textOptions, "</>", color);
                    //i.Fill(color, new Rectangle(0, (size.Y - thickness) / 2, size.X, thickness));
                    //i.Fill(color, new Rectangle((size.X - thickness) / 2, 0, thickness, size.Y));
                });
                Directory.CreateDirectory(OutputDir);
                img.SaveAsPng(OutputDir + "add_script_tool.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression, BitDepth = PngBitDepth.Bit1 });
            }
        }

        public static void MakeEyeIcon()
        {
            //using var img = new Image<La16>(size.X, size.Y);
            //var color = Color.White;
            //var thickness = 25;
            //int2 size_ = size * 3 / 4;
            //int2 pos = (size - size_) / 2;
            //var center = size / 2.0;
            //double startAng = Math.Asin(0.7) * 180 / Math.PI;
            //double arcLength = 180 - startAng;
            //img.Mutate(op =>
            //{
            //    op.Draw(Color.White, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)(0.15 * size.Y)), new SizeF((float)center.X, (float)center.X), 0, (float)startAng, (float)arcLength)));
            //    op.Draw(Color.Gray, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)((1 - 0.15) * size.Y)), new SizeF((float)center.X, (float)center.X), 0, (float)(startAng + 180), (float)arcLength)));
            //    op.Fill(color, new EllipsePolygon((float)center.X, (float)center.Y, size.X * 0.1f, size.Y * 0.1f));
            //});
            //Directory.CreateDirectory(OutputDir);
            //img.SaveAsPng(OutputDir + "eye_open.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });

            var color = Color.White;
            var thickness = 25 * 0.5f;
            int2 size_ = size * 3 / 4;
            int2 pos = (size - size_) / 2;
            var center = size / 2.0;
            double startAng = Math.Asin(0.7) * 180 / Math.PI;
            double arcLength = 180 - 2 * startAng;
            double yShift = 0.35 * thickness;

            using (var img = new Image<La16>(size.X, size.Y))
            {
                img.Mutate(op =>
                {
                    op.Draw(color, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)(0.15 * size.Y - yShift)), new SizeF((float)center.X, (float)center.Y), 0, (float)startAng, (float)arcLength)));
                    op.Draw(color, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)((1 - 0.15) * size.Y + yShift)), new SizeF((float)center.X, (float)center.Y), 0, (float)(startAng + 180), (float)arcLength)));
                    op.Fill(color, new EllipsePolygon((float)center.X, (float)center.Y, size.X * 0.15f, size.Y * 0.15f));
                    op.Crop(new Rectangle(32, 32, 192, 192));
    
                });
                Directory.CreateDirectory(OutputDir);
                img.SaveAsPng(OutputDir + "visible.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
            }

            using (var img = new Image<La16>(size.X, size.Y))
            {
                color = Color.LightGray;
                const double MULT = 0.75;
                double xOffset = size.X * (1 - MULT) * 2 / 3;
                img.Mutate(op =>
                {
                    op.Clear(Color.Transparent);
                    op.Draw(color, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)(0.15 * size.Y - yShift)), new SizeF((float)center.X, (float)center.Y), 0, (float)startAng, (float)arcLength)));
                    op.Draw(color, thickness, new SixLabors.ImageSharp.Drawing.Path(new ArcLineSegment(new PointF((float)center.X, (float)((1 - 0.15) * size.Y + yShift)), new SizeF((float)center.X, (float)center.Y), 0, (float)(startAng + 180), (float)arcLength)));
                    op.Fill(color, new EllipsePolygon((float)center.X, (float)center.Y, size.X * 0.15f, size.Y * 0.15f));
                    op.DrawLines(color, thickness, new PointF((float)xOffset, (float)(center.Y + MULT * 0.35 * size.Y)), new PointF(size.X - (float)xOffset, (float)(center.Y - MULT * 0.35 * size.Y)));
                    op.Crop(new Rectangle(32, 32, 192, 192));
                });
                img.SaveAsPng(OutputDir + "not_visible.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
            }
        }

        public static void MakeCircleBrush()
        {
            const int RESCALE = 8;
            int2 newSize = size * RESCALE;
            double2 center = (newSize - 1) * 0.5;
            double r2 = center.X * center.X;

            using var img = new Image<L8>(newSize.X, newSize.Y);
            for (int y = 0; y < newSize.Y; y++)
            {
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < newSize.X; x++)
                {
                    row[x].PackedValue = new int2(x, y).DistanceSqr(center) > r2 ? (byte)0 : byte.MaxValue;
                }
            }

            img.Mutate(op => op.Resize(size.X, size.Y, KnownResamplers.Box));
            img.SaveAsPng(OutputDir + "brush_circle.png", new PngEncoder() { ColorType = PngColorType.Grayscale, CompressionLevel = PngCompressionLevel.BestCompression });
        }

        public static void MakeDeleteIcon()
        {
			using var img = new Image<La16>(size.X, size.Y);
            double thickness = size.Min * 0.1;
            double distMult = (size.Min * 0.5 * Math.Sqrt(2) - thickness * 0.5) / size.Length();
            double2 center = (size - 1) * 0.5;
            double2 diag = size * distMult;
            double2[] corners =
            {
                center - diag,
                center + diag,
                center + diag * new double2(-1, 1),
                center + diag * new double2(1, -1)
            };
            img.Mutate(op =>
            {
                op.DrawLines(Color.White, (float)thickness, corners[0].ToPointF(), corners[1].ToPointF());
                op.DrawLines(Color.White, (float)thickness, corners[2].ToPointF(), corners[3].ToPointF());
            });
			img.SaveAsPng(OutputDir + "delete.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
		}

        public static void MakeAddIcon()
        {
			using var img = new Image<La16>(size.X, size.Y);
            float thickness = size.Min * 0.1f;
            double2 center = (size - 1) * 0.5;
            double2[] points =
            {
                new(0, center.Y),
                new(size.X, center.Y),
                new(center.X, 0),
                new(center.X, size.Y)
            };
            img.Mutate(op =>
            {
                op.DrawLines(Color.White, thickness, points[0].ToPointF(), points[1].ToPointF());
                op.DrawLines(Color.White, thickness, points[2].ToPointF(), points[3].ToPointF());
            });
			img.SaveAsPng(OutputDir + "add.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
		}

        public static void MakeLineToolIcon()
        {
			using var img = new Image<La16>(size.X, size.Y);
			double thickness = size.Min * 0.1;
			double2 center = (size - 1) * 0.5;
			double2 distMult = (size * 0.5 * Math.Sqrt(2) - thickness * 0.5) / size.Length();
			double2 diag = size * distMult;
            diag.X = -diag.X;

			img.Mutate(op =>
			{
				op.DrawLines(color, (float)thickness, (center + diag).ToPointF(), (center - diag).ToPointF());
			});
			img.SaveAsPng(OutputDir + "line.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
		}

        public static void MakeCropIcon()
        {
			using var img = new Image<La16>(size.X, size.Y);
            int2 length = size / 2;
            int2 thickness = size / 10;
            int2 horizontal = new int2(length.X, thickness.Y);
            int2 vertical = new int2(thickness.X, length.Y);
			img.Mutate(i =>
			{
				i.Clear(new Color(new Argb32(0, 0, 0, 0)));
				i.Fill(color, Rectangle(0, horizontal));
				i.Fill(color, Rectangle(0, vertical));
				i.Fill(color, Rectangle(size - horizontal, horizontal));
				i.Fill(color, Rectangle(size - vertical, vertical));
			});
			img.SaveAsPng(OutputDir + "crop.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
		}

        public static void MakeTextIcon()
        {
			using var img = new Image<La16>(size.X, size.Y);
			var font = SystemFonts.CreateFont("Times New Roman", size.Y);
			var textOptions = new TextOptions(font);
			var glyphs = TextBuilder.GenerateGlyphs("A", textOptions);
			glyphs = glyphs.Scale(size.X / glyphs.Bounds.Width);
			glyphs = glyphs.Translate(((size - new double2(glyphs.Bounds.Width, glyphs.Bounds.Height)) / 2 - new double2(glyphs.Bounds.X, glyphs.Bounds.Y)).ToPointF());
			img.Mutate(i =>
			{
				i.Fill(color, glyphs);
			});
			Directory.CreateDirectory(OutputDir);
			img.SaveAsPng(OutputDir + "text.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression});
		}

        public static void MakeQuadraticCurveIcon()
        {
            using var img = new Image<La16>(size.X, size.Y);
            img.Mutate(i =>
            {
				float thickness = size.Min * 0.1f;
				i.Clear(new Color(new Argb32(0, 0, 0, 0)));

                double2 padding = thickness;
                double2 a = padding;
                double2 b = new(size.X - padding.X - 1, (size.Y - 1) * 0.5);
                double2 c = new(padding.X, size.Y - 1 - padding.Y);
                double dashLength = a.Distance(b) / 10;
                double gapLength = dashLength;

				i.Draw(new Color(new Rgb24(169, 169, 169)), thickness, new PathBuilder().AddQuadraticBezier(a.ToPointF(), b.ToPointF(), c.ToPointF()).Build());

                float lineThickness = thickness * 0.5f;
				DrawDashedLine(i, Color.White, a, b, dashLength, gapLength, lineThickness);
                DrawDashedLine(i, Color.White, b, c, dashLength, gapLength, lineThickness);

                float radius = thickness * 0.75f;
                i.Fill(Color.White, new EllipsePolygon(a.ToPointF(), radius));
                i.Fill(Color.White, new EllipsePolygon(b.ToPointF(), radius));
                i.Fill(Color.White, new EllipsePolygon(c.ToPointF(), radius));
            });
            img.SaveAsPng(OutputDir + "quadratic_curve.png", new PngEncoder() { ColorType = PngColorType.GrayscaleWithAlpha, CompressionLevel = PngCompressionLevel.BestCompression });
        }

public static void MakeCubicCurveIcon()
{
    using var img = new Image<La16>(size.X, size.Y);
    img.Mutate(i =>
    {
        float thickness = size.Min * 0.1f;
        i.Clear(new Color(new Argb32(0, 0, 0, 0)));

        double2 padding = thickness;
        double left = padding.X;
        double right = size.X - padding.X - 1;
        double2 a = padding;
        double2 b = new(right, (size.Y - 1) / 3.0);
        double2 c = new(left, (size.Y - 1) * 2 / 3.0);
        double2 d = new(right, size.Y - 1 - padding.Y);
        double dashLength = a.Distance(b) / 10;
        double gapLength = dashLength;

        i.Draw(new Color(new Rgb24(169, 169, 169)), thickness, new PathBuilder().AddCubicBezier(a.ToPointF(), b.ToPointF(), c.ToPointF(), d.ToPointF()).Build());

        float lineThickness = thickness * 0.5f;
        DrawDashedLine(i, Color.White, a, b, dashLength, gapLength, lineThickness);
        DrawDashedLine(i, Color.White, b, c, dashLength, gapLength, lineThickness);
        DrawDashedLine(i, Color.White, c, d, dashLength, gapLength, lineThickness);

        float radius = thickness * 0.75f;
        i.Fill(Color.White, new EllipsePolygon(a.ToPointF(), radius));
        i.Fill(Color.White, new EllipsePolygon(b.ToPointF(), radius));
        i.Fill(Color.White, new EllipsePolygon(c.ToPointF(), radius));
        i.Fill(Color.White, new EllipsePolygon(d.ToPointF(), radius));
    });
    img.SaveAsPng(OutputDir + "cubic_curve.png", new PngEncoder()
    {
        ColorType = PngColorType.GrayscaleWithAlpha,
        CompressionLevel = PngCompressionLevel.BestCompression 
    });
}

        private static void DrawDashedLine(IImageProcessingContext context, Color color, double2 start, double2 stop, double dashLength, double gapLength, float thickness/*, double offset = 0*/)
        {
            double2 dir = stop - start;
            double pathLength = dir.Length();
            dir /= pathLength;
            double2 dashDelta = dir * dashLength;
            double2 gapDelta = dir * gapLength;
            //         double shift = Math.Floor(offset / (dashLength + gapLength));
            //double length = shift * (dashLength + gapLength);
            //         double2 pos = start + shift * dashDelta + shift * gapDelta;
            //         if (offset - length >= dashLength)
            //         {
            //             length += dashLength;
            //             pos += dashDelta;

            //             if (offset - length > 0)
            //             {
            //		pos += dashDelta * (offset - length) / dashLength;
            //		length = offset;
            //	}
            //         }
            //         else if (offset - length > 0)
            //         {
            //             pos += dashDelta * (offset - length) / dashLength;
            //             length = offset;
            //         }

            double length = 0;
            double2 pos = start;
            while (length <= pathLength - dashLength)
            {
                double2 next = pos + dashDelta;
                context.DrawLines(color, thickness, new PointF((float)pos.X, (float)pos.Y), new PointF((float)next.X, (float)next.Y));
                pos = next + gapDelta;
                length += dashLength + gapLength;
            }

            if (length >= pathLength) return;

            double2 next_ = pos + dashDelta * (pathLength - length) / dashLength;
            context.DrawLines(color, thickness, new PointF((float)pos.X, (float)pos.Y), new PointF((float)next_.X, (float)next_.Y));
        }

		private static Rectangle Rectangle(int2 pos, int2 size) => new(pos.X, pos.Y, size.X, size.Y);
		private static Rectangle Rectangle(int x, int y, int2 size) => new(x, y, size.X, size.Y);
		private static Rectangle Rectangle(int2 pos, int width, int height) => new(pos.X, pos.Y, width, height);
	}
}
