using ImGuiNET;
using System.Numerics;

namespace Progrimage.Utils
{
    public static class ColorManager
    {
        public static Vector4[] ImGuiColorsRGB;
        public static Vector4[] CustomColorsRGB;
        public static Vector4[] CustomColorsSRGB;
        public static string[] CustomColorNames;

        /// <summary>
        /// Initializes the color manager
        /// </summary>
        public static void Init()
        {
            int count = (int)ImGuiCol.COUNT;
            ImGuiColorsRGB = new Vector4[count];
            for (int i = 0; i < count; i++) SetImGuiColor(i, MainWindow.Style.Colors[i]);

            count = (int)CustomColor.COUNT;
            CustomColorsRGB = new Vector4[count];
            CustomColorsSRGB = new Vector4[count];
            CustomColorNames = new string[count];

            CustomColorNames[(int)CustomColor.QuickActionsToolbar] = "Quick Actions Toolbar";
            CustomColorNames[(int)CustomColor.ToolPanel] = "Tool Panel";
            CustomColorNames[(int)CustomColor.ViewportBackground] = "Viewport Background";
            CustomColorNames[(int)CustomColor.ButtonText] = "Button Text";
            CustomColorNames[(int)CustomColor.LayerList] = "Layer List";
            CustomColorNames[(int)CustomColor.LayerListText] = "Layer List Text";
            CustomColorNames[(int)CustomColor.QuickbarSeparator] = "Quickbar Separator";
            CustomColorNames[(int)CustomColor.SelectedButtonBG] = "Selected Button Background";
            CustomColorNames[(int)CustomColor.UnselectedButtonBG] = "Unselected Button Background";
            CustomColorNames[(int)CustomColor.BottomBar] = "Bottom Bar";
			CustomColorNames[(int)CustomColor.FontPickerItem] = "Font Picker Item";
			CustomColorNames[(int)CustomColor.FontPickerHover] = "Font Picker Hovered Item";
			CustomColorNames[(int)CustomColor.FontPickerSeparator] = "Font Picker Separator";
			CustomColorNames[(int)CustomColor.FontPickerText] = "Font Picker Text";

			for (int i = 0; i < count; i++) SetCustomColor(i, new Vector4(1, 1, 1, 1));
        }

        /// <summary>
        /// Updates an ImGui style color
        /// </summary>
        /// <param name="colorIndex">Index of the ImGui color</param>
        /// <param name="color">Linear RGB color</param>
        public static void SetImGuiColor(int colorIndex, Vector4 color)
        {
            ImGuiColorsRGB[colorIndex] = color;
            MainWindow.Style.Colors[colorIndex] = RGBtoSRGB(color);
        }

        /// <summary>
        /// Updates a custom style color
        /// </summary>
        /// <param name="colorIndex">Index of the custom color</param>
        /// <param name="color">Linear RGB color</param>
        public static void SetCustomColor(int colorIndex, Vector4 color)
        {
            CustomColorsRGB[colorIndex] = color;
            CustomColorsSRGB[colorIndex] = RGBtoSRGB(color);
        }

        /// <summary>
        /// Updates an ImGui style color
        /// </summary>
        /// <param name="colEnum">ImGuiCol enum</param>
        /// <param name="color">Linear RGB color</param>
        public static void SetColor(ImGuiCol colEnum, Vector4 color) => SetImGuiColor((int)colEnum, color);

        /// <summary>
        /// Updates an custom style color
        /// </summary>
        /// <param name="colEnum">CustomColor enum</param>
        /// <param name="color">Linear RGB color</param>
        public static void SetColor(CustomColor colEnum, Vector4 color) => SetCustomColor((int)colEnum, color);

        /// <summary>
        /// Updates an ImGui or custom style color
        /// </summary>
        /// <param name="colEnum">ImGuiCol or CustomColor enum</param>
        /// <param name="color">Linear RGB color</param>
        public static void SetColor(Enum colEnum, Vector4 color)
        {
            if (colEnum is ImGuiCol col) SetImGuiColor((int)col, color);
            else SetCustomColor((int)(CustomColor)colEnum, color);
        }

        /// <summary>
        /// Gets an ImGui style color in linear RGB
        /// </summary>
        /// <param name="colEnum">ImGuiCol enum</param>
        /// <returns>Linear RGB color</returns>
        public static Vector4 GetRGB(ImGuiCol colEnum) => ImGuiColorsRGB[(int)colEnum];

        /// <summary>
        /// Gets an custom style color in linear RGB
        /// </summary>
        /// <param name="colEnum">CustomColor enum</param>
        /// <returns>Linear RGB color</returns>
        public static Vector4 GetRGB(CustomColor colEnum) => CustomColorsRGB[(int)colEnum];

        /// <summary>
        /// Gets an ImGui or custom style color in linear RGB
        /// </summary>
        /// <param name="colEnum">ImGuiCol or CustomColor enum</param>
        /// <returns>Linear RGB color</returns>
        public static Vector4 GetRGB(Enum colEnum)
        {
            if (colEnum is ImGuiCol col) return ImGuiColorsRGB[(int)col];
            return CustomColorsRGB[(int)(CustomColor)colEnum];
        }

        /// <summary>
        /// Gets an ImGui style color in sRGB
        /// </summary>
        /// <param name="colEnum">ImGuiCol enum</param>
        /// <returns>sRGB color</returns>
        public static Vector4 GetSRGB(ImGuiCol colEnum) => MainWindow.Style.Colors[(int)colEnum];

        /// <summary>
        /// Gets an custom style color in sRGB
        /// </summary>
        /// <param name="colEnum">CustomColor enum</param>
        /// <returns>sRGB color</returns>
        public static Vector4 GetSRGB(CustomColor colEnum) => CustomColorsSRGB[(int)colEnum];

        /// <summary>
        /// Gets an ImGui or custom style color in sRGB
        /// </summary>
        /// <param name="colEnum">ImGuiCol or CustomColor enum</param>
        /// <returns>sRGB color</returns>
        public static Vector4 GetSRGB(Enum colEnum)
        {
            if (colEnum is ImGuiCol col) return MainWindow.Style.Colors[(int)col];
            return CustomColorsSRGB[(int)(CustomColor)colEnum];
        }

        /// <summary>
        ///  Converts linear RGB to sRGB.
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns>sRGB color</returns>
        public static Vector4 RGBtoSRGB(Vector4 rgb)
        {
            return new Vector4(
                (float)Math.Pow(rgb.X, 2.2),
                (float)Math.Pow(rgb.Y, 2.2),
                (float)Math.Pow(rgb.Z, 2.2),
                rgb.W);
        }
    }
}
