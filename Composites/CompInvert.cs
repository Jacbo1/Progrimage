using ImageSharpExtensions;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
    internal class CompInvert : ICompositeAction
	{
		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Public Methods
		public void Init(Composite composite)
		{
			Composite = composite;
			composite.Name = "Invert";
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
					pixel.R = (byte)(byte.MaxValue - pixel.R);
					pixel.G = (byte)(byte.MaxValue - pixel.G);
					pixel.B = (byte)(byte.MaxValue - pixel.B);
				}
			});

			Composite.Changed();
		}
		#endregion
	}
}
