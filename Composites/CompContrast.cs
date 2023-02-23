using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompContrast : ICompositeAction
	{
		public double Contrast = 1;

		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Public Methods
		public void Init(Composite composite)
		{
			Composite = composite;
			composite.Name = "Contrast";
		}

		public IEnumerator Run(PositionedImage<Argb32> result)
		{
			if (result.Image is null) yield break;

			Parallel.For(0, result.Image.Height, y =>
			{
				var row = result.Image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < result.Image.Width; x++)
				{
					row[x].R = (byte)Math.Clamp(Math.Round(127.5 + (row[x].R - 127.5) * Contrast, MidpointRounding.AwayFromZero), 0, 255);
					row[x].G = (byte)Math.Clamp(Math.Round(127.5 + (row[x].G - 127.5) * Contrast, MidpointRounding.AwayFromZero), 0, 255);
					row[x].B = (byte)Math.Clamp(Math.Round(127.5 + (row[x].B - 127.5) * Contrast, MidpointRounding.AwayFromZero), 0, 255);
				}
			});

			Composite.Changed();
		}

		public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			float temp = (float)Contrast;
			if (ImGui.SliderFloat("Contrast", ref temp, 0, 10))
			{
				Contrast = temp;
				((ICompositeAction)this).Rerun();
			}
		}
		#endregion
	}
}
