using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using Progrimage.ImGuiComponents;
using SixLabors.ImageSharp.Advanced;
using System.Collections;
using System.Numerics;

namespace Progrimage.Composites
{
    internal class CompTint : ICompositeAction
    {
        private float _strength = 1;
        private Vector4 _color = Vector4.One;
        public Action? DisposalDelegate { get; private set; }
        public Composite Composite { get; private set; }
        public int2 Pos { get; set; }

        #region Public Methods
        public void Init(Composite composite)
        {
            Composite = composite;
            composite.Name = "Tint";
        }

        public IEnumerator Run(PositionedImage<Argb32> result)
        {
            if (result.Image is null) yield break;
            Vector4 premult = _color * _strength * _color.W;

            Parallel.For(0, result.Image.Height, y =>
            {
                Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < result.Image.Width; x++)
                {
                    ref Argb32 pixel = ref row[x];
                    pixel.R = (byte)Math.Round(row[x].R * premult.X, MidpointRounding.AwayFromZero);
                    pixel.G = (byte)Math.Round(row[x].G * premult.Y, MidpointRounding.AwayFromZero);
                    pixel.B = (byte)Math.Round(row[x].B * premult.Z, MidpointRounding.AwayFromZero);
                }
            });

            Composite.Changed();
        }

        public void DrawQuickActionsToolbar(PositionedImage<Argb32> result)
        {
            ImGui.PushID(ID.COMPOSITE_COLOR_PICKER);
            if (ColorPicker.Draw("CompTint", ref _color, "Tint Color", ID.COMPOSITE_COLOR_PICKER))
                ((ICompositeAction)this).Rerun();
            ImGui.PopID();
            ImGui.SameLine();

            ImGui.SetNextItemWidth(100);
            if (ImGui.SliderFloat("Strength", ref _strength, 0, 1))
                ((ICompositeAction)this).Rerun();
        }
        #endregion
    }
}
