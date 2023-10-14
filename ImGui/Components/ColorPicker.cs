using ImGuiNET;
using Progrimage.Utils;
using System.Numerics;
using System.Threading.Channels;
using Color = SixLabors.ImageSharp.Color;

namespace Progrimage.ImGuiComponents
{
    class Palette
    {
        public static int _id;

        public int ID { get; private set; }
        public Vector4[] Colors;
        public Dictionary<int, bool> ShouldPushColor = new();

        public Palette()
        {
            _id++;
            ID = _id;
            Colors = new Vector4[32];
            for (int n = 0; n < Colors.Length; n++)
            {
                ImGui.ColorConvertHSVtoRGB(n / (float)Colors.Length, 1, 1,
                    out Colors[n].X, out Colors[n].Y, out Colors[n].Z);
                Colors[n].W = 1; // Alpha
            }
        }

        public void PushColor(Vector4 color)
        {
            for (int i = Colors.Length - 1; i > 0; i--)
                Colors[i] = Colors[i - 1];
            Colors[0] = color;
        }
    }

    internal static class ColorPicker
    {
        private static bool _suppressNextOpen;
        public static bool IsOpen { get; private set; }

		public static void SuppressNextOpen()
        {
            if (IsOpen) _suppressNextOpen = true;
        }

        private static Dictionary<string, Palette> _palettes = new();

        #region Public Methods
        public static bool Draw(string paletteName, ref Color color, string name = "", int id = 0)
        {
            var vector = color.ToVector4();
            bool changed = Draw(paletteName, ref vector, name, id);
			if (changed) color = new Color(vector);
            return changed;
        }

        public static bool Draw(string paletteName, ref Vector4 color, string name = "", int id = 0)
        {
            bool changed = false;
            if (!_palettes.TryGetValue(paletteName, out Palette palette))
            {
                // Create new palette with default colors
                _palettes[paletteName] = palette = new Palette();
            }

            Vector4 backup_color = color;
            IsOpen = ImGui.ColorButton("Color##3b", color, ImGuiColorEditFlags.AlphaPreviewHalf);
            if (Util.DragDropColor() is Vector4 col)
            {
                color = col;
                changed = true;
            }

            name = "Color Picker" + (name == "" ? "" : " - " + name);
            if (IsOpen) ImGui.OpenPopup(name);
			if (_suppressNextOpen)
			{
				_suppressNextOpen = false;
				IsOpen = false;
			}
            else IsOpen = ImGui.BeginPopup(name);
			if (IsOpen)
            {
                ImGui.Text(name);
                ImGui.Separator();
                Vector4 oldColor = color;
                ImGui.ColorPicker4("##picker", ref color, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);
                if (oldColor != color)
                {
                    palette.ShouldPushColor[id] = true;
                    changed = true;
                }
                ImGui.SameLine();

                ImGui.BeginGroup(); // Lock X position

                // Previews
                ImGui.Text("Current");
                ImGui.ColorButton("##current", color, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(60, 40));
                ImGui.Text("Previous");
                if (ImGui.ColorButton("##previous", backup_color, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(60, 40)))
                {
                    color = backup_color;
                    changed = true;
                }

                ImGui.Separator();

                // Palette
                ImGui.Text("Palette");
                for (int n = 0; n < palette.Colors.Length; n++)
                {
                    ImGui.PushID(n + ID.PALETTE_START);
                    if ((n % 8) != 0) ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.Y);

                    ImGuiColorEditFlags palette_button_flags = ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip;
                    if (ImGui.ColorButton("##palette", palette.Colors[n], palette_button_flags, new Vector2(20, 20)))
                    {
                        color = palette.Colors[n];
                        changed = true;
                    }

                    if (Util.DragDropColor() is Vector4 col2)
                        palette.Colors[n] = col2;

                    ImGui.PopID();
                }

                ImGui.EndGroup();
                ImGui.EndPopup();

                return changed;
            }

            if (!palette.ShouldPushColor.TryGetValue(id, out bool shouldPush) || !shouldPush) return changed;

            palette.ShouldPushColor.Remove(id);
            palette.PushColor(color);

            return changed;
        }

        public static void PushColorToPalette(string paletteName, Argb32 color) => PushColorToPalette(paletteName, new Vector4(color.R, color.G, color.B, color.A) / 255f);

        public static void PushColorToPalette(string paletteName, Color color) => PushColorToPalette(paletteName, color.ToVector4());

        public static void PushColorToPalette(string paletteName, Vector4 color)
        {
            if (!_palettes.TryGetValue(paletteName, out Palette? palette)) return;
			palette.PushColor(color);
		}
        #endregion
    }
}
