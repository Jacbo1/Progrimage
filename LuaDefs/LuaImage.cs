using NewMath;
using NLua;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageSharpExtensions;
using SixLabors.ImageSharp.Advanced;
using KeraLua;
using SixLabors.ImageSharp.Drawing.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Progrimage.DrawingShapes;
using RectangleF = SixLabors.ImageSharp.RectangleF;
using SixLabors.ImageSharp.Drawing;

namespace Progrimage.LuaDefs
{
	public class LuaImage
	{
		#region Fields
		internal PositionedImage<Argb32> Image;
		#endregion

		#region Constructors
		public LuaImage()
		{
			Image = new();
		}

		public LuaImage(Layer layer)
		{
			Image = layer.Image;
		}

		public LuaImage(PositionedImage<Argb32> image)
		{
			Image = image;
		}

		public LuaImage(int x, int y, int width, int height)
		{
			Image = new(new int2(x, y), new int2(width, height));
		}
		#endregion

		#region Properties
		public LuaTable pos
		{
			get => LuaManager.Current.CreateVector2(Image.Pos);
			set => Image.Pos = LuaManager.ToInt2(value);
		}

		public LuaTable size
		{
			get => LuaManager.Current.CreateVector2(Image.Width, Image.Height);
			set => Image.Size = LuaManager.ToInt2(value);
		}

		public int width
		{
			get => Image.Width;
			set => Image.Size = new int2(value, Image.Height);
		}

		public int height
		{
			get => Image.Height;
			set => Image.Size = new int2(Image.Width, value);
		}

		public int x
		{
			get => Image.X;
			set => Image.X = value;
		}

		public int y
		{
			get => Image.Y;
			set => Image.Y = value;
		}
		#endregion

		#region Public Methods
		public LuaTable getPixels()
		{
			if (Image.Image is null) return null;

			string[] columns = new string[Image.Width];
			Parallel.For(0, Image.Width, x =>
			{
				string s = "{";
				for (int y = 0; y < Image.Height; y++)
				{
					if (y != 0) s += ",";
					Argb32 pixel = Image.Image![x, y];
					s += "vec4(" + pixel.R + "," + pixel.G + "," + pixel.B + "," + pixel.A + ")";
				}
				columns[x] = s + "}";
			});

			return (LuaTable)LuaManager.Current.Lua.DoString("return {" + string.Join(',', columns) + "}").First();
		}

		public void setPixels(LuaTable pixels)
		{
			if (Image.Image is null) return;
			var columnEnum = pixels.Values.GetEnumerator();
			int x = 0;
			while (columnEnum.MoveNext())
			{
				int y = 0;
				var pixelEnum = ((LuaTable)columnEnum.Current).Values.GetEnumerator();
				while (pixelEnum.MoveNext())
				{
					Image.Image[x, y] = LuaManager.ToColor((LuaTable)pixelEnum.Current).ToArgb32();
					y++;
				}
				x++;
			}
		}

		public LuaTable getPixel(int x, int y)
		{
			if (Image.Image is null) return null;
			var pixel = Image.Image![x - 1, y - 1];
			return LuaManager.Current.CreateVector4(pixel.R, pixel.G, pixel.B, pixel.A);
		}

		public LuaTable getPixel(LuaTable pos)
		{
			int2 ipos = LuaManager.ToInt2(pos);
			return getPixel(ipos.x, ipos.y);
		}

		public void setPixel(int x, int y, LuaTable pixel)
		{
			if (Image.Image is not null)
				Image.Image[x - 1, y - 1] = LuaManager.ToColor(pixel).ToArgb32();
		}

		public void setPixel(LuaTable pos, LuaTable pixel)
		{
			int2 ipos = LuaManager.ToInt2(pos);
			setPixel(ipos.x, ipos.y, pixel);
		}

		public LuaImage getSubimage(LuaTable pos, LuaTable size) => new LuaImage(Image.Image?.GetSubimage(LuaManager.ToInt2(pos), LuaManager.ToInt2(size)));
		public LuaImage getSubimage(int x, int y, int width, int height) => new LuaImage(Image.Image?.GetSubimage(new int2(x, y), new int2(width, height)));

		public void expandToContain(double x, double y, double width, double height)
		{
			double2 pos = new double2(x, y);
			int2 min = Math2.FloorToInt(pos);
			int2 size = Math2.CeilingToInt(pos + new double2(width, height)) + 1 - min;
			Image.ExpandToContain(min, size);
		}

		public void expandToContain(LuaTable pos, LuaTable size)
		{
			double2 dpos = LuaManager.ToDouble2(pos);
			double2 dsize = LuaManager.ToDouble2(size);
			expandToContain(dpos.x, dpos.y, dsize.x, dsize.y);
		}

		public void resize(int width, int height) => Image.Mutate(op => op.Resize(width, height));
		public void resize(LuaTable size)
		{
			int2 isize = LuaManager.ToInt2(size);
			resize(isize.x, isize.y);
		}

		public void resize(int width, int height, string sampler)
		{
			Image.Mutate(op => op.Resize(width, height, sampler switch
			{
				"bicubic" => KnownResamplers.Bicubic,
				"box" => KnownResamplers.Box,
				"catmullrom" => KnownResamplers.CatmullRom,
				"hermite" => KnownResamplers.Hermite,
				"lanczos2" => KnownResamplers.Lanczos2,
				"lanczos3" => KnownResamplers.Lanczos3,
				"lanczos5" => KnownResamplers.Lanczos5,
				"lanczos8" => KnownResamplers.Lanczos8,
				"mitchell" => KnownResamplers.MitchellNetravali,
				"nearestneighbor" => KnownResamplers.NearestNeighbor,
				"robidoux" => KnownResamplers.Robidoux,
				"robidouxsharp" => KnownResamplers.RobidouxSharp,
				"spline" => KnownResamplers.Spline,
				"triangle" => KnownResamplers.Triangle,
				"welch" => KnownResamplers.Welch,
				_ => throw new Exception("Unknown sampler \"" + sampler + "\"")
			}));
		}

		public void resize(LuaTable size, string sampler)
		{
			int2 isize = LuaManager.ToInt2(size);
			resize(isize.x, isize.y, sampler);
		}

		public void dispose() => Image.Dispose();

		public static bool operator ==(LuaImage a, LuaImage b) => a.Image == b.Image;
		public static bool operator !=(LuaImage a, LuaImage b) => a.Image != b.Image;

		#region Drawing
		public void drawOver(LuaImage overlay, bool expand = false) => Image.DrawOver(overlay.Image, expand);

		public void drawReplace(LuaImage overlay, bool expand = false) => Image.DrawReplace(overlay.Image, expand);

		public void drawMask(LuaImage overlay) => Image.DrawMask(overlay.Image);

		public void clear(Color color, int x, int y, int width, int height) => Image.Mutate(op => op.Clear(color, new Rectangle(x, y, width, height)));
		public void clear(LuaTable color, int x, int y, int width, int height) => clear(new Color(LuaManager.ToColor(color) / 255), x, y, width, height);
		public void clear(LuaTable color, LuaTable pos, LuaTable size)
		{
			int2 ipos = LuaManager.ToInt2(pos);
			int2 isize = LuaManager.ToInt2(size);
			clear(color, ipos.x, ipos.y, isize.x, isize.y);
		}
		public void clear(LuaTable color) => clear(color, 0, 0, Image.Width, Image.Height);
		public void clear() => clear(Color.Transparent, 0, 0, Image.Width, Image.Height);

		public void drawLine(LuaTable color, double thickness, double x1, double y1, double x2, double y2)
		{
			Image?.Mutate(op => op.DrawLines(
				new Color(LuaManager.ToColor(color) / 255),
				(float)thickness,
				new PointF((float)x1, (float)y1),
				new PointF((float)x2, (float)y2))
			);
		}

		public void drawLine(LuaTable color, double thickness, LuaTable start, LuaTable end)
		{
			double2 startPos = LuaManager.ToDouble2(start);
			double2 endPos = LuaManager.ToDouble2(end);
			drawLine(color, thickness, startPos.x, startPos.y, endPos.x, endPos.y);
		}

		public void drawRect(LuaTable color, double thickness, double x, double y, double width, double height)
		{
			Image.Mutate(new DrawingRect(new Color(LuaManager.ToColor(color) / 255), new double2(x, y), new double2(width, height), thickness, false).Draw);
		}

		public void drawRect(LuaTable color, double thickness, LuaTable pos, LuaTable size)
		{
			double2 dpos = LuaManager.ToDouble2(pos);
			double2 dsize = LuaManager.ToDouble2(size);
			drawRect(color, thickness, dpos.x, dpos.y, dsize.x, dsize.y);
		}

		public void fillRect(LuaTable color, double x, double y, double width, double height)
		{
			Image.Mutate(op => op.Fill(new Color(LuaManager.ToColor(color) / 255), new RectangleF((float)x, (float)y, (float)width, (float)height)));
		}

		public void fillRect(LuaTable color, LuaTable pos, LuaTable size)
		{
			double2 dpos = LuaManager.ToDouble2(pos);
			double2 dsize = LuaManager.ToDouble2(size);
			fillRect(color, dpos.x, dpos.y, dsize.x, dsize.y);
		}

		public void drawOval(LuaTable color, double thickness, double x, double y, double width, double height)
		{
			Image.Mutate(new DrawingOval(new Color(LuaManager.ToColor(color) / 255), new double2(x, y), new double2(width, height), thickness, false).Draw);
		}

		public void drawOval(LuaTable color, double thickness, LuaTable pos, LuaTable size)
		{
			double2 dpos = LuaManager.ToDouble2(pos);
			double2 dsize = LuaManager.ToDouble2(size);
			drawOval(color, thickness, dpos.x, dpos.y, dsize.x, dsize.y);
		}

		public void fillOval(LuaTable color, double x, double y, double width, double height)
		{
			Image.Mutate(op => op.Fill(new Color(LuaManager.ToColor(color) / 255), new EllipsePolygon((float)x, (float)y, (float)width, (float)height)));
		}

		public void fillOval(LuaTable color, LuaTable pos, LuaTable size)
		{
			double2 dpos = LuaManager.ToDouble2(pos);
			double2 dsize = LuaManager.ToDouble2(size);
			fillOval(color, dpos.x, dpos.y, dsize.x, dsize.y);
		}

		public void drawQuadraticCurve(LuaTable color, double thickness, LuaTable p1, LuaTable p2, LuaTable p3)
		{
			Image.Mutate(new DrawingQuadraticCurve(new Color(LuaManager.ToColor(color) / 255), (float)thickness, LuaManager.ToDouble2(p1), LuaManager.ToDouble2(p2), LuaManager.ToDouble2(p3)).Draw);
		}

		public void drawCubicCurve(LuaTable color, double thickness, LuaTable p1, LuaTable p2, LuaTable p3, LuaTable p4)
		{
			Image.Mutate(new DrawingCubicCurve(new Color(LuaManager.ToColor(color) / 255), (float)thickness, LuaManager.ToDouble2(p1), LuaManager.ToDouble2(p2), LuaManager.ToDouble2(p3), LuaManager.ToDouble2(p4)).Draw);
		}

		public void drawPolygon(LuaTable color, double thickness, LuaTable points)
		{
			var values = points.Values;
			var valEnum = values.GetEnumerator();
			PointF[] pointfs = new PointF[values.Count];
			for (int i = 0; i < pointfs.Length; i++)
			{
				valEnum.MoveNext();
				double2 point = LuaManager.ToDouble2((LuaTable)valEnum.Current);
				pointfs[i] = new PointF((float)point.x, (float)point.y);
			}
			Image.Mutate(op => op.DrawPolygon(new Color(LuaManager.ToColor(color) / 255), (float)thickness, pointfs));
		}

		public void fillPolygon(LuaTable color, LuaTable points)
		{
			var values = points.Values;
			var valEnum = values.GetEnumerator();
			PointF[] pointfs = new PointF[values.Count];
			for (int i = 0; i < pointfs.Length; i++)
			{
				valEnum.MoveNext();
				double2 point = LuaManager.ToDouble2((LuaTable)valEnum.Current);
				pointfs[i] = new PointF((float)point.x, (float)point.y);
			}
			Image.Mutate(op => op.FillPolygon(new Color(LuaManager.ToColor(color) / 255), pointfs));
		}
		#endregion
		#endregion
	}
}
