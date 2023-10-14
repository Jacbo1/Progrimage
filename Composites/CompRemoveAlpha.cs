using ImageSharpExtensions;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompRemoveAlpha : ICompositeAction
	{
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
					row[x].A = byte.MaxValue;
			}

			Composite.Changed();
		}
		#endregion
	}
}
