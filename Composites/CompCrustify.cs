using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using System.Collections;

namespace Progrimage.Composites
{
    internal class CompCrustify : ICompositeAction
    {
        public Action? DisposalDelegate { get; private set; }
        public Composite Composite { get; private set; }
        public int2 Pos { get; set; }
        private int _seed = Math2.RandomInt(0, 65536);
        private int _level = 75;
        private const int MAX_QUALITY = 75;
        private bool _preserveAlpha = true;

        #region Public Methods
        public void Init(Composite composite)
        {
            Composite = composite;
            composite.Name = "Crustify";
        }

        public IEnumerator Run(PositionedImage<Argb32> result)
        {
            if (result.Image is null) yield break;

            bool applyAlpha = false;
            byte[,] alpha;
            if (_preserveAlpha)
            {
                alpha = new byte[result.Width, result.Height];

                for (int y = 0; y < result.Height; y++)
                {
                    Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
                    for (int x = 0; x < result.Width; x++)
                    {
                        applyAlpha |= row[x].A != byte.MaxValue;
                        alpha[x, y] = row[x].A;
                    }
                }
            }
            else alpha = null;

            Random rand = new Random(_seed);
            List<int> qualities = new List<int>();
            if (_level == 1) qualities.Add(1);
            else
            {
                for (int i = 0; i < _level; i++)
                {
                    qualities.Add(i * (MAX_QUALITY - 1) / (_level - 1) + 1);
                }
            }

            for (int i = 0; i < _level; i++)
            {
                MemoryStream stream = new MemoryStream();
                int index = rand.Next(qualities.Count);
                result.Image.SaveAsJpeg(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = qualities[index]
                });
                qualities.RemoveAt(index);
                stream.Position = 0;
                result.Image.Dispose();
                result.Image = SixLabors.ImageSharp.Image.Load<Argb32>(stream);
                stream.Dispose();
            }

            if (applyAlpha)
            {
                for (int y = 0; y < result.Height; y++)
                {
                    Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
                    for (int x = 0; x < result.Width; x++)
                    {
                        row[x].A = alpha![x, y];
                    }
                }
            }

            Composite.Changed();
        }

        public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragInt("Seed", ref _seed, 1, 0)) ((ICompositeAction)this).Rerun();

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderInt("Level", ref _level, 1, MAX_QUALITY - 1)) ((ICompositeAction)this).Rerun();

            ImGui.SameLine();
            if (ImGui.Checkbox("Preserve Alpha", ref _preserveAlpha)) ((ICompositeAction)this).Rerun();
        }
        #endregion
    }
}
