using ImGuiNET;
using Progrimage;
using Progrimage.ImGuiComponents;
using Progrimage.Utils;
using System.Numerics;
using System.Xml;

namespace ProgrimageImGui.Windows
{
    public static class StyleEditor
    {
        #region
        public static string ThemeName = "Default";

        private const string PALETTE_NAME = "Style Editor";
        private const string THEME_DIR = "Themes\\";
        private static List<ComparableImGuiCol> _items = new();
        private static bool _first = true;

        // Struct for sorting alphabetically
        struct ComparableImGuiCol : IComparable
        {
            public string Name;
            public Enum Enum;

            public ComparableImGuiCol(string name, Enum colEnum)
            {
                Name = name;
                Enum = colEnum;
                if (colEnum is ImGuiCol)
                {
                    bool lastWasNotCapital = false;
                    int i = 1;
                    while (i < Name.Length)
                    {
                        char c = Name[i];
                        bool capital = c >= 'A' && c <= 'Z';
                        if (!capital) lastWasNotCapital = true;
                        else if (lastWasNotCapital)
                        {
                            lastWasNotCapital = false;
                            Name = Name[..i] + ' ' + Name[i..];
                            i++;
                        }
                        i++;
                    }
                }
            }

            public int CompareTo(object? obj)
            {
                if (obj is ComparableImGuiCol b)
                    return Name.CompareTo(b.Name);
                return 0;
            }
        }
        #endregion

        #region Public Methods
        public static void Init()
        {
            _items.Clear();

            // Populate _items
            for (int i = 0; i < (int)CustomColor.COUNT; i++)
                _items.Add(new ComparableImGuiCol(ColorManager.CustomColorNames[i], (CustomColor)i));
            for (int i = 0; i < (int)ImGuiCol.COUNT; i++)
            {
                ImGuiCol col = (ImGuiCol)i;
                string name = ImGui.GetStyleColorName(col) ?? "";
                _items.Add(new ComparableImGuiCol(name, col));
            }
            //

            _items.Sort();
            Load(ThemeName);
        }

        public static void Draw(ref bool open)
        {
            if (_first)
            {
                _first = false;
                var size = new Vector2(200, 300);
                ImGui.SetNextWindowSize(size);
                ImGui.SetNextWindowPos((new Vector2(MainWindow.Width, MainWindow.Height) - size) / 2);
            }

            if (ImGui.Begin("Style Editor", ref open, ImGuiWindowFlags.MenuBar))
            {
                ImGui.BeginMenuBar();
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Save")) Save(ThemeName); // Save style
                    if (ImGui.MenuItem("Load")) Load(ThemeName); // Load style
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();

                foreach (ComparableImGuiCol item in _items)
                {
                    string name = item.Name;
                    Enum Enum = item.Enum;
                    int id;

                    if (Enum is ImGuiCol col)
                    {
                        id = (int)col;
                        ImGui.PushID(id + ID.THEME_COLOR_START);
                        Vector4 color = ColorManager.ImGuiColorsRGB[id];
                        ColorPicker.Draw(PALETTE_NAME, ref color, name, id);
                        if (color != ColorManager.ImGuiColorsRGB[id])
                            ColorManager.SetImGuiColor(id, color);
                    }
                    else
                    {
                        CustomColor customCol = (CustomColor)Enum;
                        int index = (int)customCol;
                        id = index + (int)ImGuiCol.COUNT;
                        ImGui.PushID(id + ID.THEME_COLOR_START);
                        Vector4 color = ColorManager.CustomColorsRGB[index];
                        ColorPicker.Draw(PALETTE_NAME, ref color, name, id);
                        if (color != ColorManager.CustomColorsRGB[index])
                            ColorManager.SetCustomColor(index, color);
                    }
                    ImGui.SameLine();
                    ImGui.TextUnformatted(name);
                    ImGui.PopID();
                }
                ImGui.End();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Saves the current theme settings.
        /// </summary>
        /// <param name="theme"></param>
        /// <returns></returns>
        private static string GetThemeFilePath(string theme)
        {
            string fileName = theme;
            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c.ToString(), "");
            fileName += ".xml";
            return THEME_DIR + fileName;
        }

        private static void Save(string theme)
        {
            string path = GetThemeFilePath(theme);
            Directory.CreateDirectory(THEME_DIR);
            if (File.Exists(path)) File.Delete(path);

            using XmlWriter writer = XmlWriter.Create(path);
            writer.WriteStartElement("root");
            writer.WriteElementString("name", theme);

            writer.WriteStartElement("colors");
            foreach (ComparableImGuiCol item in _items)
            {
                Vector4 color = ColorManager.GetRGB(item.Enum);
                writer.WriteElementString("i", $"{item.Name},{color.X},{color.Y},{color.Z},{color.W}");
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        /// <summary>
        /// Loads theme settings from a file
        /// </summary>
        /// <param name="theme"></param>
        private static async void Load(string theme)
        {
            string path = GetThemeFilePath(theme);
            if (!File.Exists(path)) return;

            Dictionary<string, Enum> dict = new();

            foreach (ComparableImGuiCol item in _items)
                dict[item.Name] = item.Enum;

            // Read
            using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { Async = true });
            string lastStart = "";
            while (await reader.ReadAsync())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        lastStart = reader.Name;
                        break;
                    case XmlNodeType.Text:
                        switch (lastStart)
                        {
                            case "name":
                                // Theme Name
                                ThemeName = await reader.GetValueAsync();
                                break;
                            case "i":
                                // Color pair
                                {
                                    string[] arr = (await reader.GetValueAsync()).Split(',');
                                    if (dict.TryGetValue(arr[0], out Enum colEnum))
                                    {
                                        Vector4 color = new(
                                            float.Parse(arr[1]),
                                            float.Parse(arr[2]),
                                            float.Parse(arr[3]),
                                            float.Parse(arr[4])
                                        );

                                        ColorManager.SetColor(colEnum, color);
                                    }
                                }
                                break;
                        }
                        break;
                        //case XmlNodeType.EndElement:
                        //    Console.WriteLine("End Element {0}", reader.Name);
                        //    break;
                }
            }
        }
        #endregion
    }
}
