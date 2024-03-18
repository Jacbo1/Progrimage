using ImageSharpExtensions;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompGrayscale : ICompositeAction
	{
		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Public Methods
		public void Init(Composite composite)
		{
			Composite = composite;
			composite.Name = "Grayscale";
		}

		public IEnumerator Run(PositionedImage<Argb32> result)
		{
			if (result.Image is null) yield break;

			Parallel.For(0, result.Image.Height, y =>
			{
				Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < result.Image.Width; x++)
				{
					ref Argb32 pixel = ref row[x];
					byte col = (byte)((pixel.R + pixel.G + pixel.B) / 3);
					pixel.R = col;
					pixel.G = col;
					pixel.B = col;
				}
			});

			Composite.Changed();
		}
		#endregion
	}
}
