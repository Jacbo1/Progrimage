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

            Parallel.For(0, result.Image.Height, y =>
            {
                Span<Argb32> row = result.Image.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < result.Image.Width; x++)
                {
                    ref Argb32 pixel = ref row[x];
                    pixel.R = (byte)Math.Round(row[x].R * (1 - _color.W + _color.X * _color.W), MidpointRounding.AwayFromZero);
                    pixel.G = (byte)Math.Round(row[x].G * (1 - _color.W + _color.Y * _color.W), MidpointRounding.AwayFromZero);
                    pixel.B = (byte)Math.Round(row[x].B * (1 - _color.W + _color.Z * _color.W), MidpointRounding.AwayFromZero);
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
        }
        #endregion
    }
}
