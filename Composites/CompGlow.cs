using ImGuiNET;
using Progrimage.Utils;
using System.Collections;
using Progrimage.CoroutineUtils;
using ImageSharpExtensions;
using NewMath;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Progrimage.Composites
{
	public class CompGlow : ICompositeAction
    {
        private int _iterations = 5, _maxIterations = 1, _blurRadius = 8;

		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region ICompositeAction Methods
		public void Init(Composite composite)
        {
            Composite = composite;
            composite.Name = "Glow";
        }

        public IEnumerator Run(PositionedImage<Argb32> result)
        {
            DisposalDelegate = null;
            _maxIterations = 0;
            int w = result.Size.X / 2;
            int h = result.Size.Y / 2;
            while (w != 0 && h != 0)
            {
                _maxIterations++;
                w /= 2;
                h /= 2;
            }
            int iterations = Math.Min(_iterations, _maxIterations);

            if (iterations == 0 || result.Image is null) yield break;

            using (Image<Argb32> src = result.Image!.Clone())
            {
                Image<Argb32> last = result.Image;
                Image<Argb32>[] images = new Image<Argb32>[iterations];

                DisposalDelegate = () =>
                {
                    foreach (var img in images)
                        img?.Dispose();
                    src.Dispose();
                };

                int2[] sizes = new int2[iterations + 1];
                float _blurSigma = _blurRadius / 3f;
                sizes[0] = result.Size;

                if (JobQueue.ShouldYield) yield return true;

                // Downscale
                for (int i = 0; i < iterations; i++)
                {
                    var img = last.Clone();
                    int2 size = sizes[i] / 2;
                    sizes[i + 1] = size;
                    img.Mutate(x => x.Resize(size.X, size.Y, KnownResamplers.Box));
                    images[i] = img;
                    last = img;

                    if (JobQueue.ShouldYield) yield return true;
                }
                
                // Upscale, blur, and merge
                for (int i = iterations - 1; i >= 0; i--)
                {
                    Image<Argb32> cur = images[i];
                    int2 nextSize = Math2.Round(cur.Size() * sizes[i] / (double2)sizes[i + 1]);
                    int2 nextSizePlusBlur = nextSize + _blurRadius * 2;
                    Image<Argb32> next = images[Math.Max(i - 1, 0)];

                    // Scale up
                    cur.Mutate(x => x.Resize(nextSize.X, nextSize.Y));

                    // Blur with padding for the blur to extend onto
                    Image<Argb32> temp = new(nextSizePlusBlur.X, nextSizePlusBlur.Y);
                    temp.DrawReplace(cur, _blurRadius);
                    temp.Mutate(x => x.GaussianBlur(_blurSigma));
                    cur.Dispose();
                    images[i] = null;

                    if (i == 0)
                    {
                        // Draw to output
                        int2 center = (nextSizePlusBlur - result.Size) / 2;
                        result.Pos -= center;
                        result.Dispose();
                        result.Image = temp;
                        result.Image.DrawOver(src, center);

                        Composite.Changed();
					}
                    else
                    {
                        // Draw to next image and make it bigger
                        temp.DrawOver(next, (nextSizePlusBlur - next.Size()) / 2);
                        next.Dispose();
                        images[i - 1] = temp;
                    }

                    if (JobQueue.ShouldYield) yield return true;
                }
            }
            DisposalDelegate = null;
        }

        public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
        {
            // Calculate maximum amount of iterations
            _maxIterations = 0;
            int w = result.Size.X / 2;
            int h = result.Size.Y / 2;
            while (w != 0 && h != 0)
            {
                _maxIterations++;
                w /= 2;
                h /= 2;
            }
            //_maxIterations = Math.Min(_maxIterations, 15);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("Iterations", ref _iterations, 0, _maxIterations))
                ((ICompositeAction)this).Rerun();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragInt("Radius", ref _blurRadius, 1, 1))
            {
                _blurRadius = Math.Max(_blurRadius, 1);
                ((ICompositeAction)this).Rerun();
            }
        }
        #endregion
    }
}
