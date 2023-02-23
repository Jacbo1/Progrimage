using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NewMath;
using Progrimage.Utils;
using Progrimage.Windows;
using IS = SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Num = System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
using Progrimage.Tools;
using Color = Microsoft.Xna.Framework.Color;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Progrimage.Composites;
using Progrimage.CoroutineUtils;
using Image = SixLabors.ImageSharp.Image;
using ProgrimageImGui.Windows;
using SystemFonts = SixLabors.Fonts.SystemFonts;
using FontFamily = SixLabors.Fonts.FontFamily;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using ProgrimageImGui;
using System.Buffers;
using Progrimage.LuaDefs;

namespace Progrimage
{
    /// <summary>
    /// Simple FNA + ImGui example
    /// </summary>
    public class MainWindow : Game
    {
        private enum Events
        {
            OnMouseDown,
            OnMouseDownCanvas,
            OnMouseUp,
            OnMouseMoveScreen,
            OnMouseMoveCanvas,
            OnMouseMoveCanvasDouble,
            OnMouseEnterCanvas,
            OnMouseExitConvas
        }

        private struct LuaCompositePath
        {
            public string Path;
            public bool IsDirectory;
            public List<LuaCompositePath>? FileTree;

            public LuaCompositePath(string path, bool isDirectory)
            {
                Path = System.IO.Path.GetFileNameWithoutExtension(path);
                IsDirectory = isDirectory;
                if (!IsDirectory)
                {
                    // Single file
                    FileTree = null;
                    return;
                }

                // Directory
                FileTree = new();

                string[] dirs = Directory.GetDirectories(path);
                for (int i = 0; i < dirs.Length; i++)
                    FileTree.Add(new LuaCompositePath(dirs[i], true));

                string[] files = Directory.GetFiles(path, "*.lua");
                for (int i = 0; i < files.Length; i++)
                    FileTree.Add(new LuaCompositePath(files[i], false));
            }

            public static void PopulateFromRoot(string path, ref List<LuaCompositePath> fileTree)
            {
                fileTree.Clear();
				string[] dirs = Directory.GetDirectories(path);
				for (int i = 0; i < dirs.Length; i++)
					fileTree.Add(new LuaCompositePath(dirs[i], true));

				string[] files = Directory.GetFiles(path, "*.lua");
				for (int i = 0; i < files.Length; i++)
					fileTree.Add(new LuaCompositePath(files[i], false));
			}

            public void CreateMenus(Layer layer, string relPath = "")
            {
                if (!IsDirectory)
                {
					if (ImGui.MenuItem(Path)) layer.AddComposite(new Composite(layer, new CompLua(relPath, Path)));
                    return;
				}

                if (!ImGui.BeginMenu(Path)) return;
                relPath += Path + "\\";
                for (int i = 0; i < FileTree.Count; i++)
                    FileTree[i].CreateMenus(layer, relPath);
				ImGui.EndMenu();
			}
        }

        #region Fields
        // Public static
        public static int Width, Height;
        public static ImGuiStylePtr Style;
        public static ImGuiRenderer ImGuiRenderer;
        public static GraphicsDevice GraphicsDevice;
        public static EventHandler MouseUp, OnDraw, OnPreUpdate, FontPicked;
        public static bool IsMouseDown, MouseDownStartInCanvas, PostMouseDownStartInCanvas, MouseInCanvas, IsMouseDownCanvas, MouseDownCanvasChanged, IsDragging, IsWindowActive;
        public static bool MouseOverCanvasWindow;
		public static int2 LayerThumbnailSize = Defs.LayerThumbnailSize;
        public static int2 MousePosCanvas, LastMousePosCanvas, MousePosScreen, LastMousePosScreen, CanvasMin, CanvasMax, CanvasOrigin;
        public static double2 MousePosCanvasDouble, LastMousePosCanvasDouble, CanvasOriginDouble;
        public static ImGuiIOPtr IO;
        public static string? LastLoadPath, LastSavePath;
        public static Vector2 ToolIconSize = new(Defs.TOOL_ICON_SIZE, Defs.TOOL_ICON_SIZE);
        public static string SelectedFont = "Arial";
        public static Popup FontPickerPopup, CreateLuaCompPopup;

		// Private static
		private static double _uiScale = 1;
        private static float _thumbnailUpdateInterval = 1000;
        private static float _thumbnailUpdateTimer;
        private static int _quickBarHeight = 20;
        private static int _fontPickerScroll = 0;
        private static TexPair CanvasTexture, VisibleIcon, NotVisibleIcon, DeleteIcon, AddIcon;
        private static bool _scannedLuaComposites;
        private static (int, int) _draggedComposite;
        private static List<LuaCompositePath> _luaComposites = new();
		private static TexPair _fontPickerTexture;
		private static Image<Rgb24>? _fontPickerImage;
        private static string _luaCompositeInputName;

		// Private
		private GraphicsDeviceManager _graphics;
        private string[]? _launchFiles;
		#endregion

		#region Properties
		public static double UIScale
        {
            get => _uiScale;
            set
            {
                _uiScale = value;
            }
        }
        #endregion

        public MainWindow()
        {
            Activated += (o, e) => IsWindowActive = true;
            Deactivated += (o, e) => IsWindowActive = false;
            Window.FileDrop += Window_FileDrop;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.PreferMultiSampling = true;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Window.AllowUserResizing = true;
            Window.AllowAltF4 = true;

            IsMouseVisible = true;

            FontPickerPopup = new Popup("Font Picker", () =>
            {
                _fontPickerImage?.Dispose();
                _fontPickerImage = null;
                _fontPickerTexture.Dispose();
            }, DrawFontPicker);

            CreateLuaCompPopup = new Popup("Create Lua Composite", DrawCreateLuaCompositePopup)
            {
                OpenAction = () => { _luaCompositeInputName = ""; }
            };
		}

        public void Run(string[] files)
        {
            _launchFiles = files;
			Run();
		}

		private void Window_FileDrop(object? sender, FileDropEventArgs e)
		{
			foreach (string file in e.Files)
            {
                try
                {
                    Program.ActiveInstance.CreateLayer(Image.Load<Argb32>(file));
                }
                catch { }
            }
		}

        protected override void Initialize()
        {
            ImGuiRenderer = new ImGuiRenderer(this);
            ImGuiRenderer.RebuildFontAtlas();

            base.Initialize();
            GraphicsDevice = base.GraphicsDevice;

            Style = ImGui.GetStyle();
            Style.WindowPadding = Vector2.Zero;

            ColorManager.Init();
            StyleEditor.Init();

            IO = ImGui.GetIO();

            VisibleIcon = new(@"Assets\Textures\Icons\visible.png", Defs.LayerButtonSize);
            NotVisibleIcon = new(@"Assets\Textures\Icons\not_visible.png", Defs.LayerButtonSize);
            DeleteIcon = new(@"Assets\Textures\Icons\delete.png", Defs.LayerButtonSize);
            AddIcon = new(@"Assets\Textures\Icons\add.png", Defs.LayerButtonSize);

            Program.ActiveInstance = new Instance(new int2(512, 512));
            Program.ActiveInstance.CreateLayer();

            Window.ClientSizeChanged += (o, e) =>
            {
                var bounds = Window.ClientBounds;
                Width = bounds.Width;
                Height = bounds.Height;
                Program.ActiveInstance.Changed = true;
                Program.ActiveInstance.Zoom = Program.ActiveInstance.Zoom;
            };

            {
                var bounds = Window.ClientBounds;
                Width = bounds.Width;
                Height = bounds.Height;
                Program.ActiveInstance.Changed = true;
                Program.ActiveInstance.Zoom = Program.ActiveInstance.Zoom;
            }

            if (_launchFiles is not null)
            {
                foreach (string file in _launchFiles)
                {
                    try
                    {
                        Program.ActiveInstance.CreateLayer(Image.Load<Argb32>(file));
                    }
                    catch { }
                }
                _launchFiles = null;
            }
		}

        protected override void LoadContent()
        {
            GraphicsDevice = base.GraphicsDevice;
            base.LoadContent();
        }

        protected override void Draw(GameTime gameTime)
        {
			base.GraphicsDevice.Clear(new Color(clear_color.X, clear_color.Y, clear_color.Z));

            // Call BeforeLayout first to set things up
            ImGuiRenderer.BeforeLayout(gameTime);

            // Draw our UI
            ImGuiLayout();

            // Call AfterLayout now to finish up and draw all the things
            ImGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }

        // Direct port of the example at https://github.com/ocornut/imgui/blob/master/examples/sdl_opengl2_example/main.cpp
        private bool show_test_window = false;
        private bool show_another_window = false;
        private bool show_style_editor = false;
        private Num.Vector3 clear_color = new Num.Vector3(114f / 255f, 144f / 255f, 154f / 255f);
        private byte[] _textBuffer = new byte[100];

        protected virtual void ImGuiLayout()
        {
            int2 oldCanvasMin = CanvasMin;
            int2 oldCanvasMax = CanvasMax;
            CanvasMax.y = Height - 1;
            JobQueue.UpdateTime();
            Util.SetMouseCursor();
            Queue<Events> eventHandlersTrigged = new();

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(Width, Height));
            ImGui.Begin("Main", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus);
            MousePosScreen = IO.MousePos.ToInt2();

            DrawMenuBar();
            HandleEvents(eventHandlersTrigged);
            DrawCanvas();
            DrawBottomBar();
            DrawToolPanel();
            DrawRightPanel();
            FontPickerPopup.Draw();
            CreateLuaCompPopup.Draw();
            ResizeCanvas.TryShowResizeCanvasWindow(ref MouseOverCanvasWindow);

            ImGui.End();

            // ImGui demo window for dev purposes
            if (show_test_window)
            {
                ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref show_test_window);
            }

            if (show_style_editor) StyleEditor.Draw(ref show_style_editor);

            // Event handlers
            if (MousePosScreen != LastMousePosScreen && IsWindowActive)
                eventHandlersTrigged.Enqueue(Events.OnMouseMoveScreen);

            // Run methods on active tool
            float timeDelta = ImGui.GetIO().Framerate;
            OnPreUpdate?.Invoke(this, EventArgs.Empty);
            if (eventHandlersTrigged.Contains(Events.OnMouseUp))
                MouseUp?.Invoke(this, EventArgs.Empty);

            foreach (var interactable in Program.ActiveInstance.GetInteractables())
            {
                foreach (var eventHandler in eventHandlersTrigged)
                {
                    switch (eventHandler)
                    {
                        case Events.OnMouseDown:
                            interactable.OnMouseDown(JobQueue.State.MousePosScreen, JobQueue.State.MousePosCanvas);
                            break;
                        case Events.OnMouseDownCanvas:
                            interactable.OnMouseDownCanvas(JobQueue.State.MousePosCanvas);
                            break;
                        case Events.OnMouseEnterCanvas:
                            interactable.OnMouseEnterCanvas(JobQueue.State.MousePosScreen, JobQueue.State.MousePosCanvas);
                            break;
                        case Events.OnMouseExitConvas:
                            interactable.OnMouseExitConvas(JobQueue.State.MousePosScreen, JobQueue.State.MousePosCanvas);
                            break;
                        case Events.OnMouseMoveCanvas:
                            interactable.OnMouseMoveCanvas(JobQueue.State.MousePosCanvas);
                            break;
                        case Events.OnMouseMoveCanvasDouble:
                            interactable.OnMouseMoveCanvasDouble(MousePosCanvasDouble);
                            break;
                        case Events.OnMouseMoveScreen:
                            interactable.OnMouseMoveScreen(JobQueue.State.MousePosScreen);
                            break;
                        case Events.OnMouseUp:
                            interactable.OnMouseUp(JobQueue.State.MousePosScreen, JobQueue.State.MousePosCanvas);
                            break;
                    }
                }
                interactable.Update(timeDelta);
            }
            OnDraw?.Invoke(this, EventArgs.Empty);

            JobQueue.Work();

            LastMousePosCanvas = MousePosCanvas;
            LastMousePosScreen = MousePosScreen;
            LastMousePosCanvasDouble = MousePosCanvasDouble;

            if (oldCanvasMin != CanvasMin || oldCanvasMax != CanvasMax)
                Program.ActiveInstance.Changed = true;

            Program.ActiveInstance.Init();
            ImGuiRenderer.TextInput.Clear();
        }
        
        public static TexPair CreateTexture(int width, int height, SurfaceFormat format = SurfaceFormat.ColorSRgb)
        {
            return new TexPair(new Texture2D(GraphicsDevice, width, height, false, format));
        }

        public static TexPair CreateTexture(int2 size, SurfaceFormat format = SurfaceFormat.ColorSRgb)
        {
            return new TexPair(new Texture2D(GraphicsDevice, size.x, size.y, false, format));
        }

        #region Private Methods
        private void DrawMenuBar()
        {
            // Create menu bars
            if (!ImGui.BeginMenuBar()) return;

            if (ImGui.BeginMenu("File"))
            {
                bool saveable = Program.ActiveInstance != null;
                if (ImGui.MenuItem("Save", saveable && Program.ActiveInstance?.LastSavePath != null))
                    Save(Util.GetSavePath()); // Save
                if (ImGui.MenuItem("Save As...", saveable))
                {
                    // Save as
                    SaveFileDialog picker = new SaveFileDialog();
                    if (Util.GetSavePath() is string s)
                        picker.InitialDirectory = s;
                    picker.Title = "Save As...";
                    picker.Filter = Defs.FILE_FILTER_FULL;
                    picker.AddExtension = true;
                    picker.OverwritePrompt = true;
                    if (picker.ShowDialog() == DialogResult.OK)
                        Save(picker.FileName);
                }

                if (ImGui.MenuItem("Load", Program.DEV_MODE)) // This currently shows the ImGui demo window. No way to save or load projects currently
                    show_test_window = !show_test_window; // Load editor file

                if (ImGui.MenuItem("Import"))
                {
                    // Import image
                    OpenFileDialog picker = new OpenFileDialog();
                    if (Util.GetLoadPath() is string s)
                        picker.InitialDirectory = s;
                    picker.Title = "Import file";
                    picker.Filter = Defs.FILE_FILTER_FULL;
                    if (picker.ShowDialog() == DialogResult.OK)
                    {
                        Program.ActiveInstance.CreateLayer(IS.Image.Load<Argb32>(picker.FileName));
                        Util.SetLastLoadPath(picker.FileName);
                    }
                }

                if (ImGui.MenuItem("Open Lua Folder"))
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", Directory.GetCurrentDirectory() + "\\" + Defs.LUA_BASE_PATH);
                    }
                    catch { }
				}

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Resize Canvas"))
                    ResizeCanvas.Show = true;
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Style Editor"))
                    show_style_editor = !show_style_editor; // Show style editor
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Layer", Program.ActiveInstance?.ActiveLayer is not null))
            {
                Layer layer = Program.ActiveInstance!.ActiveLayer!;

                if (ImGui.BeginMenu("Composites"))
                {
                    if (ImGui.MenuItem("Glow")) layer.AddComposite(new Composite(layer, new CompGlow()));
                    if (ImGui.MenuItem("HSV")) layer.AddComposite(new Composite(layer, new CompHSV()));
                    if (ImGui.MenuItem("HSL")) layer.AddComposite(new Composite(layer, new CompHSL()));
                    if (ImGui.MenuItem("Contrast")) layer.AddComposite(new Composite(layer, new CompContrast()));
                    if (ImGui.MenuItem("Invert")) layer.AddComposite(new Composite(layer, new CompInvert()));
                    if (ImGui.MenuItem("Grayscale")) layer.AddComposite(new Composite(layer, new CompGrayscale()));
                    if (ImGui.MenuItem("Remove Alpha")) layer.AddComposite(new Composite(layer, new CompRemoveAlpha()));
                    if (ImGui.MenuItem("Multiply Alpha")) layer.AddComposite(new Composite(layer, new CompAlphaMult()));

                    if (!_scannedLuaComposites && Directory.Exists(Defs.LUA_BASE_PATH + Defs.LUA_COMPOSITE_PATH))
                    {
                        // Repopulate scanned lua composites
                        _scannedLuaComposites = true;
                        LuaCompositePath.PopulateFromRoot(Defs.LUA_BASE_PATH + Defs.LUA_COMPOSITE_PATH, ref _luaComposites);
                    }

                    if (_scannedLuaComposites && _luaComposites.Count != 0)
                    {
                        ImGui.Separator();
                        if (ImGui.MenuItem("Create new Lua composite"))
                            CreateLuaCompPopup.Open();

                        ImGui.Separator();
                        for (int i = 0; i < _luaComposites.Count; i++)
                            _luaComposites[i].CreateMenus(layer);
                    }

                    ImGui.EndMenu();
                } else _scannedLuaComposites = false;
                ImGui.EndMenu();
			}
			else _scannedLuaComposites = false;
			ImGui.EndMenuBar();
        }

        private void DrawToolPanel()
        {
            var y = ImGui.GetFrameHeight() + _quickBarHeight - 1;

            ref var ChildBg = ref Style.Colors[(int)ImGuiCol.ChildBg];
            var childbg = ChildBg;
            ref var FramePadding = ref Style.FramePadding;
            var framePadding = FramePadding;
            FramePadding = Vector2.Zero;

            // Draw tool panel
            ChildBg = ColorManager.CustomColorsSRGB[(int)CustomColor.ToolPanel];

            //ToolIconSize = new Vector2(30, 30) * (float)_uiScale;

            ImGui.SetNextWindowPos(new Vector2(0, y));
            ImGui.BeginChild("tool panel", new Vector2(ToolIconSize.X, ImGui.GetWindowHeight() - y), false, ImGuiWindowFlags.NoBringToFrontOnFocus);
            var buttonColor = Style.Colors[(int)ImGuiCol.Button];
            var buttonColorHover = Style.Colors[(int)ImGuiCol.ButtonHovered];
            var textColor = Style.Colors[(int)ImGuiCol.Text];
            var red = new Vector4(1, 0, 0, 1);

            int toolCounter = 0;
            Action<ITool> Draw = tool =>
            {
                bool isActiveTool = false;
                if (tool == Program.ActiveInstance?.ActiveTool)
                {
                    // Set colors to indicate active tool
                    isActiveTool = true;
                    var col = Style.Colors[(int)ImGuiCol.ButtonActive];
                    Style.Colors[(int)ImGuiCol.Button] = col;
                    Style.Colors[(int)ImGuiCol.ButtonHovered] = col;
                }

                // Set active tool
                toolCounter++;
				if (ImGui.ImageButton(tool.ToString() + toolCounter, tool.Icon, ToolIconSize, Vector2.Zero, Vector2.One, ColorManager.GetSRGB(tool == Program.ActiveInstance?.ActiveTool ? CustomColor.SelectedButtonBG : CustomColor.UnselectedButtonBG)))
                    Program.ActiveInstance!.ActiveTool = tool;

                if (!isActiveTool) return;

                // Set colors back to normal
                Style.Colors[(int)ImGuiCol.Button] = buttonColor;
                Style.Colors[(int)ImGuiCol.ButtonHovered] = buttonColorHover;
            };

            // Draw default tool buttons
            foreach (ITool tool in Program.ActiveInstance.DefaultTools)
                Draw(tool);

            // Draw lua tool buttons
            int i = 0;
            var tools = Program.ActiveInstance.LuaTools;
            while (i < tools.Count)
            {
                ToolLua tool = tools[i];
                if (tool == null)
                {
                    tools.RemoveAt(i);
                    continue;
                }

                Draw(tool);

                // Draw error tooltip
                if (tool.Error is not null && ImGui.IsItemHovered())
                {
                    Style.Colors[(int)ImGuiCol.Text] = red;
                    ImGui.BeginTooltip();
					ImGui.PushTextWrapPos(Width - ImGui.GetMousePos().X);
					ImGui.TextUnformatted(tool.Error);
					ImGui.PopTextWrapPos();
					ImGui.EndTooltip();
                    Style.Colors[(int)ImGuiCol.Text] = textColor;
                }

                // Draw settings context menu
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Settings"))
                    {
                        // Open tool settings window
                        tool.OpenSettings();
                        ImGui.CloseCurrentPopup();
                    }

                    if (ImGui.MenuItem("Remove"))
                    {
						// Remove tool
						Program.ActiveInstance.ActiveTool = null;
						Program.ActiveInstance.LuaTools.Remove(tool);
						tool.Dispose();
						i--;
                    }

                    ImGui.EndPopup();
                }
                i++;
            }

            // Draw button to create new lua tool
            Draw(Program.ActiveInstance.ToolCreateScript);

            CanvasMin.x = (int)ImGui.GetWindowWidth();
			MouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.EndChild();

            // Quick Actions Toolbar
            ChildBg = ColorManager.CustomColorsSRGB[(int)CustomColor.QuickActionsToolbar];
            ImGui.SetNextWindowPos(new Vector2(0, _quickBarHeight - 1));
            ImGui.BeginChild("quick actions toolbar", new Vector2(ImGui.GetWindowWidth(), _quickBarHeight), false, ImGuiWindowFlags.NoBringToFrontOnFocus);
            //Program.ActiveInstance?.ActiveInteractive?.DrawQuickActionsToolbar();
            ref var TextColor = ref Style.Colors[(int)ImGuiCol.Text];
            var separatorColor = ColorManager.CustomColorsSRGB[(int)CustomColor.QuickbarSeparator];
            Queue<IUsesToolbar> toolbarUsers = Program.ActiveInstance.GetToolbarUsers();
            bool first = true;
            bool multi = toolbarUsers.Count > 1;

            foreach (var toolbarUser in toolbarUsers)
            {
                if (multi)
                {
                    if (!first) ImGui.SameLine();
                    first = false;
                    TextColor = separatorColor;
                    ImGui.Text(toolbarUser.Name);
                    TextColor = textColor;
                    ImGui.SameLine();
                }
                toolbarUser.DrawQuickActionsToolbar();
            }

            if (Program.DEV_MODE)
            {
                ImGui.SameLine();
                ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate, ImGui.GetIO().Framerate));
            }

            CanvasMin.y = _quickBarHeight + (int)ImGui.GetWindowHeight() + 1;
			MouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.EndChild();
            ChildBg = childbg;
            FramePadding = framePadding;
        }

        private void SetCanvasOrigin()
        {
			CanvasOriginDouble = (CanvasMax - CanvasMin) * 0.5 - (Program.ActiveInstance.CanvasSize * 0.5 - Program.ActiveInstance.Pos) * Program.ActiveInstance.Zoom + CanvasMin;
			CanvasOrigin = Math2.RoundToInt(CanvasOriginDouble);
        }

        private void HandleEvents(Queue<Events> eventHandlersTrigged)
        {
            bool mouseInCanvas = MouseOverCanvasWindow && MousePosScreen >= CanvasMin && MousePosScreen <= CanvasMax && IsWindowActive;
            Instance instance = Program.ActiveInstance;
            if (mouseInCanvas)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Middle)) // Pan canvas
                    instance.Pos += (MousePosScreen - LastMousePosScreen) / instance.Zoom;
                float scroll = IO.MouseWheel;
                if (scroll != 0)
                {
                    SetCanvasOrigin();
                    double2 oldPos = Util.ScreenToCanvasDouble(MousePosScreen);
                    instance.ZoomLerp = Math2.Clamp(instance.ZoomLerp + scroll * 0.025, 0, 1); // Zoom canvas
                    SetCanvasOrigin();
                    double2 newPos = Util.ScreenToCanvasDouble(MousePosScreen);
                    instance.Pos += newPos - oldPos;
                }
            }
            SetCanvasOrigin();
            
            // Mouse Events
            // Mouse in canvas
            MousePosCanvas = Util.ScreenToCanvas(MousePosScreen);
            MousePosCanvasDouble = Util.ScreenToCanvasDouble(MousePosScreen);
            //bool mouseInCanvas = MousePosCanvas >= 0 && MousePosCanvas < instance.CanvasSize && IsWindowActive;
            if (mouseInCanvas != MouseInCanvas)
            {
                MouseInCanvas = mouseInCanvas;
                if (mouseInCanvas) eventHandlersTrigged.Enqueue(Events.OnMouseEnterCanvas);
                else eventHandlersTrigged.Enqueue(Events.OnMouseExitConvas);
            }

            // Mouse down/up
            bool mouseDown = ImGui.IsMouseDown(ImGuiMouseButton.Left) && (mouseInCanvas || IsDragging);
            bool mouseDownChanged = mouseDown != IsMouseDown;
            if (mouseDownChanged)
            {
                IsMouseDown = mouseDown;
                if (mouseDown)
                {
                    MouseDownStartInCanvas = mouseInCanvas;
                    eventHandlersTrigged.Enqueue(Events.OnMouseDown);
                    if (mouseInCanvas) eventHandlersTrigged.Enqueue(Events.OnMouseDownCanvas);
                }
                else
                {
                    PostMouseDownStartInCanvas = MouseDownStartInCanvas;
                    MouseDownStartInCanvas = false;
                    eventHandlersTrigged.Enqueue(Events.OnMouseUp);
                }
            }


            // Mouse move canvas
            if (IsWindowActive)
            {
                if (MousePosCanvas != LastMousePosCanvas)
                    eventHandlersTrigged.Enqueue(Events.OnMouseMoveCanvas);
                if (MousePosCanvasDouble != LastMousePosCanvasDouble)
                    eventHandlersTrigged.Enqueue(Events.OnMouseMoveCanvasDouble);
            }

            bool b = MouseInCanvas && MouseDownStartInCanvas && IsMouseDown;
            MouseDownCanvasChanged = b != IsMouseDownCanvas;
            IsMouseDownCanvas = b;

            IsDragging = IsMouseDown && MouseDownStartInCanvas;
        }

        private void DrawCanvas()
        {
            Instance instance = Program.ActiveInstance;

            _thumbnailUpdateTimer += ImGui.GetIO().Framerate;
            if (_thumbnailUpdateTimer > _thumbnailUpdateInterval)
            {
                _thumbnailUpdateTimer = 0;
                foreach (Layer layer in Program.ActiveInstance.LayerManager.Layers)
                    JobQueue.Queue.Add(new CoroutineJob(layer.UpdateThumbnail));
            }

            // Render to Texture2D
            Program.ActiveInstance.RenderToTexture2D(ref CanvasTexture, out int2 offset, out int2 size);

			MouseOverCanvasWindow = ImGui.IsWindowHovered();
			ImGui.SetNextWindowPos(CanvasOrigin + offset);
            ImGui.SetNextWindowSize(size);
            Style.WindowBorderSize = 0;
            ImGui.Begin("canvas", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.ChildWindow);
            ImGui.Image(CanvasTexture, size);
            MouseOverCanvasWindow |= ImGui.IsWindowHovered();
			ImGui.End();
        }

        private void DrawRightPanel()
        {
            float y = ImGui.GetFrameHeight() + _quickBarHeight + 1;
            float height = ImGui.GetWindowHeight() - y;
            float panelWidth = (float)(150 * _uiScale);

            ref var ChildBg = ref Style.Colors[(int)ImGuiCol.ChildBg];
            var childbg = ChildBg;
            ChildBg = ColorManager.CustomColorsSRGB[(int)CustomColor.ToolPanel];

            CanvasMax.x = (int)(ImGui.GetWindowWidth() - panelWidth);
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetWindowWidth() - panelWidth, y));
            ImGui.BeginChild("right panel", new Vector2(panelWidth, height));

			// Draw layer list
			DrawLayerList(ref ChildBg);
			MouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.EndChild();

            ChildBg = childbg;
        }

        private void DrawLayerList(ref Vector4 childBg)
        {
            childBg = ColorManager.CustomColorsSRGB[(int)CustomColor.LayerList];

            ref var TextColor = ref Style.Colors[(int)ImGuiCol.Text];
            var textColor = TextColor;
            TextColor = ColorManager.CustomColorsSRGB[(int)CustomColor.LayerListText];

            ref var IndentSpacing = ref Style.IndentSpacing;
            var indentSpacing = IndentSpacing;
            IndentSpacing = 0;

            //ref var CellPadding = ref Style.CellPadding;
            //var cellPadding = CellPadding;
            //CellPadding.Y = 100;

            float thumbnailWidth = (float)(LayerThumbnailSize.x * _uiScale);
            float halfThumbnailWidth = thumbnailWidth * 0.5f;
            ImGui.BeginChild("layer list");

            // Draw top stuff
            if (ImGui.ImageButton("add layer", AddIcon.Ptr, Defs.LayerButtonSize))
                Program.ActiveInstance.CreateLayer(); // Clicked

            Vector2 itemSize = new Vector2(ImGui.GetWindowWidth(), LayerThumbnailSize.y);

			List<Layer> layers = Program.ActiveInstance.LayerManager.Layers;
            for (int i = 0; i < layers.Count; i++)
            {
                Layer layer = layers[i];
                string layerString = layer.ToString()! + i;
				ImGui.PushID(layerString);
                ImGui.BeginGroup();

                // Draw thumbnail
                float indent = (thumbnailWidth - layer.ThumbnailSize.X) * 0.5f;
                ImGui.Indent(indent);
                if (ImGui.ImageButton(layerString + "thumb", layer.ThumbnailTex, layer.ThumbnailSize, Vector2.Zero, Vector2.One, layer == Program.ActiveInstance.ActiveLayer ? Style.Colors[(int)ImGuiCol.ButtonActive] : Style.Colors[(int)ImGuiCol.Button]))
                {
                    if (Program.ActiveInstance.ActiveLayer != layer)
                        Program.ActiveInstance.ActiveComposite = null;
                    Program.ActiveInstance.ActiveLayer = layer;
                }
                ImGui.Indent(indent);
                ImGui.SameLine();
                ImGui.BeginGroup();

                // Draw layer name
                bool selected = layer == Program.ActiveInstance.ActiveLayer;
                if (ImGui.Selectable(layer.Name, selected))
                {
                    if (Program.ActiveInstance.ActiveLayer != layer)
                        Program.ActiveInstance.ActiveComposite = null;
                    Program.ActiveInstance.ActiveLayer = layer;
                }

                // Draw visibility button
                var framePadding = Style.FramePadding;
                Style.FramePadding = Vector2.Zero;
                if (ImGui.ImageButton(layerString + "vis",
                    (layer.Hidden ? NotVisibleIcon : VisibleIcon).Ptr,
                    Defs.LayerButtonSize,
                    Vector2.Zero, Vector2.One,
                    ColorManager.GetSRGB(layer.Hidden ? CustomColor.SelectedButtonBG : CustomColor.UnselectedButtonBG)))
                {
                    // Clicked
                    layer.Hidden = !layer.Hidden;
                    Program.ActiveInstance.Changed = true;
                }

                // Draw delete button
                ImGui.SameLine();
                if (ImGui.ImageButton(layerString + "del", DeleteIcon.Ptr, Defs.LayerButtonSize))
                {
                    if (i != 0 && i + 1 == layers.Count)
                        Program.ActiveInstance.ActiveLayer = layers[i - 1];
                    layer.Dispose(); // Clicked
                }

                Style.FramePadding = framePadding;
                ImGui.EndGroup();
				ImGui.EndGroup();

				// Drag and drop layers
				if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
				{
					unsafe
					{
						ImGui.SetDragDropPayload(Defs.LAYER_PAYLOAD, new IntPtr(&i), sizeof(int));
					}
					ImGui.EndDragDropSource();
				}
				if (ImGui.BeginDragDropTarget())
				{
					unsafe
					{
						ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload(Defs.LAYER_PAYLOAD);
						if (payload.NativePtr != null)
						{
							(layers[*(int*)payload.Data], layers[i]) = (layers[i], layers[*(int*)payload.Data]);
							Program.ActiveInstance.Changed = true;
						}
					}
					ImGui.EndDragDropTarget();
				}
				ImGui.PopID();

				// Draw composites
				if (layer.Composites is not null && ImGui.TreeNode("Composites"))
                {
                    ImGui.Indent(halfThumbnailWidth);

                    if (ImGui.Button("Apply Composites")) layer.ApplyComposites();
                    else
                    {
                        int j = 0;
                        while (layer.Composites is not null && j < layer.Composites.Count)
                        {
                            var comp = layer.Composites[j];
                            string compString = comp.ToString()! + j + "_" + i;
                            ImGui.PushID(compString + "ID");
                            ImGui.BeginGroup();
                            bool selectedComp = selected && Program.ActiveInstance.ActiveComposite == comp;
                            indent = (thumbnailWidth - comp.ThumbnailSize.X) * 0.5f;
                            ImGui.Indent(indent);
                            if (ImGui.ImageButton(compString, comp.Thumbnail, comp.ThumbnailSize, Vector2.Zero, Vector2.One, selectedComp ? Style.Colors[(int)ImGuiCol.ButtonActive] : Style.Colors[(int)ImGuiCol.Button]))
                            {
                                Program.ActiveInstance.ActiveLayer = layer;
                                Program.ActiveInstance.ActiveComposite = comp;
                            }

                            if (comp.CompositeAction is CompLua luaComp && luaComp.Error is not null && ImGui.IsItemHovered())
                            {
                                Style.Colors[(int)ImGuiCol.Text] = new Vector4(1, 0, 0, 1);
                                ImGui.BeginTooltip();
								ImGui.PushTextWrapPos(Width - ImGui.GetMousePos().X);
								ImGui.TextUnformatted(luaComp.Error);
								ImGui.PopTextWrapPos();
								ImGui.EndTooltip();
                                Style.Colors[(int)ImGuiCol.Text] = textColor;
                            }

                            ImGui.Indent(indent);

                            // Draw composite name
                            ImGui.SameLine();
                            ImGui.BeginGroup();
                            if (ImGui.Selectable(comp.Name, selectedComp))
                            {
                                Program.ActiveInstance.ActiveLayer = layer;
                                Program.ActiveInstance.ActiveComposite = comp;
                            }

                            // Draw visibility button
                            Style.FramePadding = Vector2.Zero;
                            if (ImGui.ImageButton(compString + "vis",
                                (comp.Hidden ? NotVisibleIcon : VisibleIcon).Ptr,
                                Defs.LayerButtonSize,
                                Vector2.Zero, Vector2.One,
                                ColorManager.GetSRGB(comp.Hidden ? CustomColor.SelectedButtonBG : CustomColor.UnselectedButtonBG)))
                            {
                                // Clicked
                                comp.Hidden = !comp.Hidden;
                                layer!.Changed();
                            }

                            // Draw delete button
                            ImGui.SameLine();
                            if (ImGui.ImageButton(compString + "del", DeleteIcon.Ptr, Defs.LayerButtonSize))
                            {
                                // Clicked
                                layer.RemoveComposite(comp);
                                i--;
                            }

                            Style.FramePadding = framePadding;

                            ImGui.EndGroup();
                            ImGui.EndGroup();

                            // Drag and drop composites
                            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoPreviewTooltip))
                            {
                                unsafe
                                {
                                    _draggedComposite.Item1 = i;
                                    _draggedComposite.Item2 = j;
                                    ImGui.SetDragDropPayload(Defs.COMPOSITE_PAYLOAD, new IntPtr(&i), sizeof(int));
                                }
                                ImGui.EndDragDropSource();
                            }
                            if (ImGui.BeginDragDropTarget())
                            {
                                unsafe
                                {
                                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload(Defs.COMPOSITE_PAYLOAD);
                                    if (payload.NativePtr != null)
                                    {
                                        (layers[_draggedComposite.Item1].Composites[_draggedComposite.Item2], layer.Composites[j]) = (layer.Composites[j], layers[_draggedComposite.Item1].Composites[_draggedComposite.Item2]);
                                        layer.Changed();
                                        if (i != _draggedComposite.Item1) layers[_draggedComposite.Item1].Changed();
                                        Program.ActiveInstance.Changed = true;
                                    }
                                }
                                ImGui.EndDragDropTarget();
                            }
                            ImGui.PopID();
                            j++;
                        }
                        ImGui.Unindent(halfThumbnailWidth);
                        ImGui.TreePop();
                    }
                }
            }
            ImGui.EndChild();

            TextColor = textColor;
            IndentSpacing = indentSpacing;
            //CellPadding = cellPadding;
        }

        private void DrawBottomBar()
        {
            float width = (float)((ImGui.GetWindowWidth() - 150 * _uiScale) - (30 * _uiScale));
            float height = ImGui.GetFontSize() * 1.5f;
            List<string> strings = new();
            strings.Add($"Cursor Pos: ({MousePosCanvas.x}x, {MousePosCanvas.y}y)");
            if (Program.ActiveInstance.Selection is not null)
            {
                var selection = Program.ActiveInstance.Selection;
				strings.Add($"Selection: [({selection.Pos.x}x, {selection.Pos.y}y) ({selection.Max.x - selection.Min.x + 1}w, {selection.Max.y - selection.Min.y + 1}h)]");
			}
			foreach (var interactable in Program.ActiveInstance.GetInteractables())
                strings.AddRange(interactable.DrawBottomBar());

            ref var ChildBg = ref Style.Colors[(int)ImGuiCol.ChildBg];
            var childBg = ChildBg;
            ChildBg = ColorManager.CustomColorsSRGB[(int)CustomColor.BottomBar];
            ImGui.SetNextWindowPos(new Vector2((float)(30 * _uiScale), ImGui.GetWindowHeight() - height));
			ImGui.BeginChild("bottom bar", new Vector2(width, height), false, ImGuiWindowFlags.NoBringToFrontOnFocus);
            float interval = width / Math.Max(1, strings.Count - 1);
            float x = 0;
            for (int i = 0; i < strings.Count; i++)
            {
                ImGui.SameLine();
                ImGui.Indent(interval * i - x);
                ImGui.Text(strings[i]);
                x += ImGui.CalcItemWidth();
            }

			MouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.EndChild();
            ChildBg = childBg;
		}

		private static void DrawFontPicker()
		{
			int2 mousePos = IsWindowActive ? (MousePosScreen - (int2)ImGui.GetWindowPos()) : -1;

			// Draw
			int2 selectorSize = new int2(300, Height / 2);
			Util.ClearAndSetSize(ref _fontPickerImage, selectorSize, new IS.Color(ColorManager.CustomColorsRGB[(int)CustomColor.FontPickerItem]));
			const int ITEM_HEIGHT = 30;
			const int ITEM_SPACING = 2;
			FontFamily[] families = SystemFonts.Collection.Families.ToArray();
			_fontPickerImage.Mutate(op =>
            {
                const float FONT_SIZE = ITEM_HEIGHT * 0.5f;
                const float ITEM_CENTER = ITEM_HEIGHT * 0.5f;
                const float TEXT_X = 5;
				Rectangle spacerRect = new Rectangle(0, 0, selectorSize.x, ITEM_SPACING);

				var separatorColor = new IS.Color(ColorManager.CustomColorsRGB[(int)CustomColor.FontPickerSeparator]);
				var textColor = new IS.Color(ColorManager.CustomColorsRGB[(int)CustomColor.FontPickerText]);

				for (int i = _fontPickerScroll / (ITEM_HEIGHT + ITEM_SPACING); i < families.Length; i++)
                {
                    int y = (ITEM_HEIGHT + ITEM_SPACING) * i - _fontPickerScroll;
                    if (y >= selectorSize.y) break;

                    FontFamily family = families[i];

					if (mousePos.x >= 0 && mousePos.x < selectorSize.x && mousePos.y >= y && mousePos.y < y + ITEM_HEIGHT)
					{
						// Hovering item;
						op.Fill(new IS.Color(ColorManager.CustomColorsRGB[(int)CustomColor.FontPickerHover]), new Rectangle(0, y, selectorSize.x, ITEM_HEIGHT));

						if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
						{
                            // Clicked
                            SelectedFont = family.Name;
                            FontPicked?.Invoke(null, EventArgs.Empty);
						}
					}

					TextOptions textOptions = new TextOptions(family.CreateFont(FONT_SIZE))
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new Vector2(TEXT_X, y + ITEM_CENTER)
                    };
					op.DrawText(textOptions, family.Name, textColor);
                    spacerRect.Y = y + ITEM_HEIGHT;
                    op.Fill(separatorColor, spacerRect);
                }
            });

			_fontPickerTexture.Size = selectorSize;
			Util.DrawImageToTexture2D(_fontPickerTexture!, _fontPickerImage!);
            ImGui.Image(_fontPickerTexture, selectorSize);

            if (ImGui.IsItemHovered()) _fontPickerScroll = Math.Clamp(_fontPickerScroll - (int)IO.MouseWheel * 35, 0, (families.Length - 1) * (ITEM_HEIGHT + ITEM_SPACING) + ITEM_HEIGHT - selectorSize.y);
		}

        private static void DrawCreateLuaCompositePopup()
        {
            if (ImGui.InputText("Name", ref _luaCompositeInputName, 200))
                Util.RemoveInvalidChars(_luaCompositeInputName);

			string path = Defs.LUA_COMPOSITE_PATH + _luaCompositeInputName;
			if (!_luaCompositeInputName.EndsWith(".lua")) path += ".lua";
            bool fileExists = File.Exists(Defs.LUA_BASE_PATH + path);

            ImGui.BeginDisabled(fileExists);
            if (ImGui.Button("Create") && !fileExists)
            {
                File.Create(Defs.LUA_BASE_PATH + path);
                LuaFileHandler.OpenLuaEditor(path);
                CreateLuaCompPopup.Close();
            }
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.DelayShort))
            {
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35);
				ImGui.TextUnformatted("File already exists");
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		public void Save(string path)
        {
			Image<Argb32> image = Program.ActiveInstance.LayerManager.Merge().Image!;
            switch (Path.GetExtension(path).ToLower())
            {
                case ".png":
                    image.SaveAsPngAsync(path);
                    break;
                case ".jpg":
                case ".jpeg":
                    image.SaveAsJpegAsync(path);
                    break;
                case ".bmp":
                    image.SaveAsBmpAsync(path);
                    break;
                case ".tga":
                    image.SaveAsTgaAsync(path);
                    break;
            }
            Util.SetLastSavePath(path);
        }
        #endregion
    }
}