using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompAlphaMult : ICompositeAction
	{
		private float _multiplier = 1;
		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Public Methods
		public void Init(Composite composite)
		{
			Composite = composite;
			composite.Name = "Remove Alpha";
		}

		public IEnumerator Run(PositionedImage<Argb32> result)
		{
			if (result.Image is null) yield break;

			for (int y = 0; y < result.Image.Height; y++)
			{
				Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < result.Image.Width; x++)
					row[x].A = (byte)Math.Clamp(Math.Round(row[x].A * _multiplier, MidpointRounding.AwayFromZero), 0, 255);
			}

			Composite.Changed();
		}

		public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
		{
			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			if (ImGui.DragFloat("Alpha Multiplier", ref _multiplier, 0.01f, 0, 255))
				((ICompositeAction)this).Rerun();
		}
		#endregion
	}
}
