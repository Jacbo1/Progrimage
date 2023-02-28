using ImGuiNET;
using NewMath;
using Progrimage.LuaDefs;
using Progrimage.Utils;
using System;
using System.Diagnostics;
using System.Timers;
using System.Xml;

namespace Progrimage.Tools
{
    public class ToolLua : LuaFileHandler, ITool, IDisposable
    {
        #region Fields
        // Public
        public TexPair Icon { get; set; }

        // Private static
        private static bool _toolSettingsOpen, _firstDraw = true;

        // Private
        private bool _showSettings;
        private string? _iconPath;
        #endregion

        #region Constructor
        public ToolLua() : base("Lua Tool", Defs.LUA_TOOL_PATH) { }
        public ToolLua(string path) : base("Lua Tool", Defs.LUA_TOOL_PATH, path) { }
        #endregion

        #region ITool Methods
        //public void OnMouseDown(int2 mousePosScreen, int2 mousePosCanvas) => LuaManager.CallFunction("OnMouseDown", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouseDownCanvas(int2 mousePosCanvas) => LuaManager.CallFunction("OnMouseDown", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouseDown2Canvas() => LuaManager.CallFunction("OnMouse2Down", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouseUp(int2 mousePosScreen, int2 mousePosCanvas) => LuaManager.CallFunction("OnMouseUp", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouse2Up() => LuaManager.CallFunction("OnMouse2Up", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouseMoveScreen(int2 mousePos) => LuaManager.CallFunction("OnMouseMoveScreen", LuaManager.Current.CreateVector2(mousePos));
        public void OnMouseMoveCanvasDouble(double2 mousePos) => LuaManager.CallFunction("OnMouseMoveCanvas", LuaManager.Current.CreateVector2(mousePos));
        public void OnMouseEnterCanvas(int2 mousePosScreen, int2 mousePosCanvas) => LuaManager.CallFunction("OnMouseEnterCanvas", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnMouseExitConvas(int2 mousePosScreen, int2 mousePosCanvas) => LuaManager.CallFunction("OnMouseExitConvas", LuaManager.Current.CreateVector2(MainWindow.MousePosCanvasDouble));
        public void OnSelect(Instance instance)
        {
            if (LuaManager?.Lua is null) return;
            LuaManager.Lua["activeLayer"] = instance.ActiveLuaLayer;
            LuaManager.CallFunction("OnSelect");
        }
        public void OnDeselect() => LuaManager.CallFunction("OnDeselect");
        public void OnLayerSelect(Layer layer)
        {
			if (LuaManager?.Lua is null) return;
			LuaManager.Lua["activeLayer"] = layer;
            LuaManager.CallFunction("OnLayerSelect", new LuaLayer(layer));
        }
        public void OnLayerDeselect(Layer layer) => LuaManager.CallFunction("OnLayerDeselect", new LuaLayer(layer));
        //public void DrawQuickActionsToolbar() => LuaManager.CallFunction("DrawQuickActionsToolbar");
        public void Update(float deltaTime) => LuaManager.CallFunction("Update", deltaTime / 1000.0);
        #endregion

        #region Public Methods
        public void OpenSettings()
        {
            if (_showSettings || _toolSettingsOpen) return;
            _toolSettingsOpen = true;
            _showSettings = true;
            _firstDraw = true;
            MainWindow.OnDraw += OnDraw!;
        }

        public void OnDraw(object o, EventArgs e)
        {
            if (_firstDraw)
            {
                _firstDraw = false;
                //ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 200));
                ImGui.OpenPopup("Tool Settings");
            }

            //if (ImGui.Begin("Tool Settings", ref _showSettings, ImGuiWindowFlags.MenuBar))
            if (ImGui.BeginPopup("Tool Settings", ImGuiWindowFlags.MenuBar))
            {
                // Draw menu bar
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.MenuItem("Save")) Save(); // Save

                    if (ImGui.MenuItem("Load"))
                    {
                        // Import image
                        OpenFileDialog picker = new OpenFileDialog();
                        picker.InitialDirectory = Directory.GetCurrentDirectory() + "\\" + Defs.LUA_BASE_PATH + Defs.LUA_TOOL_PATH;
                        picker.Title = "Import file";
                        picker.Filter = Defs.FILE_FILTER_LUA;
                        if (picker.ShowDialog() == DialogResult.OK)
                            Load(picker.FileName);
                    }

                    if (ImGui.MenuItem("Edit")) OpenLuaEditor(Defs.LUA_TOOL_PATH + GetFileName() + ".lua");
                    ImGui.EndMenuBar();
                }

                //
                ImGui.Text("Name: " + Name);

                if (ImGui.ImageButton(ToString(), Icon, MainWindow.ToolIconSize))
                {
                    // Import image
                    OpenFileDialog picker = new OpenFileDialog();
                    picker.Title = "Set icon";
                    picker.Filter = Defs.FILE_FILTER_FULL;
                    if (picker.ShowDialog() == DialogResult.OK)
                    {
                        Icon.Dispose();
                        Icon = new(picker.FileName, true);
                        _iconPath = picker.FileName;
                    }
                }

                ImGui.SameLine();
                ImGui.Text("Icon");

                // Name
                string s = Name;
                if (ImGui.InputText("Name", ref s, 100)) Name = s;

                ImGui.EndPopup();
            }
            else _showSettings = false;

            if (_showSettings) return;

            _toolSettingsOpen = false;
            MainWindow.OnDraw -= OnDraw!;
        }
		#endregion

		#region Private Methods
		protected override void Save()
        {
            GetFileName();
            string path = Defs.LUA_BASE_PATH + Defs.LUA_TOOL_PATH + FileName;
            Directory.CreateDirectory(Defs.LUA_BASE_PATH + Defs.LUA_TOOL_PATH);
            if (File.Exists(path + ".xml")) File.Delete(path + ".xml");

            using XmlWriter writer = XmlWriter.Create(path + ".xml");
            writer.WriteStartElement("root");
            writer.WriteElementString("name", Name);
            if (_iconPath is not null) writer.WriteElementString("icon", _iconPath);
            writer.WriteEndElement();
        }

        protected override async void Load(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
            SetFilename(Name);
            string path_ = Path.GetDirectoryName(path) + '\\' + FileName;
            if (!File.Exists(path_ + ".xml")) return;

            // Read
            using XmlReader reader = XmlReader.Create(path_ + ".xml", new XmlReaderSettings { Async = true });
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
                                Name = reader.Value;
                                break;
                            case "icon":
                                _iconPath = reader.Value;
                                Icon.Dispose();
                                try
                                {
                                    Icon = new(_iconPath, true);
                                }
                                catch { }
                                break;
                        }
                        break;
                }
            }
        }
        #endregion
    }
}
