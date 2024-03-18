using NewMath;

namespace Progrimage
{
    public static class ID
    {
        public const int PALETTE_START = 0;
        public const int TOOL_COLOR_PICKER = 40;
        public const int COMPOSITE_COLOR_PICKER = 41;
        public const int THEME_COLOR_START = 42;
        //public const int LAYER_START = THEME_COLOR_START + (int)ImGuiNET.ImGuiCol.COUNT + (int)CustomColor.COUNT;
    }

    public static class Defs
    {
        public const string LAYER_PAYLOAD = "LAYER";
        public const string COMPOSITE_PAYLOAD = "COMP";
        public const string EXPORT_FILE_FILTER_ANY = "|*.png;*.jpg;*.jpeg;*.bmp;*.ico;*.tga";
        public const string EXPORT_FILE_FILTER_FULL = EXPORT_FILE_FILTER_ANY + "|PNG (*.png)|*.png|JPG (*.jpg, *.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|ICO (*.ico)|*.ico|TGA (*.tga)|*.tga";
        public const string IMPORT_FILE_FILTER_ANY = "|*.png;*.jpg;*.jpeg;*.svg;*.webp;*.bmp;*.ico;*.pbm;*.tif;*.tiff;*.tga;*.dds;*.gif";
        public const string IMPORT_FILE_FILTER_FULL = IMPORT_FILE_FILTER_ANY + "|PNG (*.png)|*.png|JPG (*.jpg, *.jpeg)|*.jpg;*.jpeg|SVG (*.svg)|*.svg|WebP (*.webp)|*.webp|BMP (*.bmp)|*.bmp|ICO (*.ico)|*.ico|TIFF (*.tif, *.tiff)|*.tif;*.tiff|TGA (*.tga)|*.tga|DDS (*.dds)|*.dds|GIF (*.gif)|*.gif";
        public const string FILE_FILTER_LUA = "|*.lua;*.xml";
        public const string LUA_BASE_PATH = @"Lua\";
        public const string LUA_TOOL_PATH = @"Tools\";
        public const string LUA_COMPOSITE_PATH = @"Composites\";
        public const string LUA_LAYER_GEN_PATH = @"Layer Generators\";
        public const int TOOL_ICON_SIZE = 30;
        public const int CURSOR_CHANGE_RADIUS = 16;
        public const int CURSOR_CHANGE_RADIUS_SQR = CURSOR_CHANGE_RADIUS * CURSOR_CHANGE_RADIUS;
        public const double BRUSH_STROKE_STEP = 1;
        public const double TYPING_CURSOR_FLASH_INTERVAL = 1000;
        public static readonly int2 LayerThumbnailSize = 40;
        public static readonly int2 LayerButtonSize = 16;
        public static readonly int2 DotOuterSize = 10;
        public static readonly int2 DotInnerSize = 6;
    }
}
