using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompHSV : ICompositeAction
	{
		public double H, S, V;

		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Public Methods
		public void Init(Composite composite)
		{
			Composite = composite;
			composite.Name = "HSV";
		}

		public IEnumerator Run(PositionedImage<Argb32> result)
		{
			if (result.Image is null) yield break;

			Parallel.For(0, result.Image.Height, y =>
			{
				var row = result.Image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < result.Image.Width; x++)
				{
					double3 hsv = RGBtoHSV(row[x]);
					hsv.X = (hsv.X + H) % 360;
					hsv.Y = Math.Clamp(hsv.Y + S, 0, 1);
					hsv.Z = Math.Clamp(hsv.Z + V, 0, 1);
					row[x] = HSVtoRGB(hsv, row[x].A);
				}
			});

			Composite.Changed();
		}

		public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			float temp = (float)H;
			if (ImGui.SliderFloat("H", ref temp, 0, 360))
			{
				H = temp;
				((ICompositeAction)this).Rerun();
			}

			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			temp = (float)S;
			if (ImGui.SliderFloat("S", ref temp, -1, 1))
			{
				S = temp;
				((ICompositeAction)this).Rerun();
			}

			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			temp = (float)V;
			if (ImGui.SliderFloat("V", ref temp, -1, 1))
			{
				V = temp;
				((ICompositeAction)this).Rerun();
			}
		}
		#endregion

		#region Private Methods
		private static double3 RGBtoHSV(Argb32 src)
		{
			double red = src.R / 255.0;
			double green = src.G / 255.0;
			double blue = src.B / 255.0;
			double V = Math.Max(Math.Max(red, green), blue);
			double delta = V - Math.Min(Math.Min(red, green), blue);

			double H;
			if (delta == 0) H = 0;
			else if (red == V) H = 60 * ((green - blue) / delta % 6);
			else if (green == V) H = 60 * (2 + (blue - red) / delta);
			else H = 60 * (4 + (red - green) / delta);

			double S = V == 0 ? 0 : delta / V;

			return new double3(H, S, V);
		}

		private static Argb32 HSVtoRGB(double3 src, byte alpha)
		{
			double c = src.Z * src.Y;
			double x = c * (1 - Math.Abs((src.X / 60 % 2) - 1));

			double3 rgb = Math2.Round(255 * (src.Z - c + ((int)(src.X / 60) switch
			{
				0 => new double3(c, x, 0),
				1 => new double3(x, c, 0),
				2 => new double3(0, c, x),
				3 => new double3(0, x, c),
				4 => new double3(x, 0, c),
				_ => new double3(c, 0, x)
			})));

			return new Argb32(
					(byte)Math.Clamp(rgb.X, 0, 255),
					(byte)Math.Clamp(rgb.Y, 0, 255),
					(byte)Math.Clamp(rgb.Z, 0, 255),
					alpha);
		}
		#endregion
	}
}
