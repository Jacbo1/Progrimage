using DirectBitmapLibrary;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NewMath;
using Progrimage.ImGuiComponents;
using Progrimage.Selectors;
using Progrimage.Tools;
using Progrimage.Undo;
using Progrimage.Utils;
using ProgrimageImGui;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using Color = Microsoft.Xna.Framework.Color;
using Font = SixLabors.Fonts.Font;
using Image = SixLabors.ImageSharp.Image;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SystemFonts = SixLabors.Fonts.SystemFonts;

namespace Progrimage
{
    /// <summary>
    /// ImGui renderer for use with XNA-likes (FNA & MonoGame)
    /// </summary>
    public class ImGuiRenderer
    {
        public static List<TextInput> TextInput = new();
        public static Font FontToCheckValidChars = SystemFonts.CreateFont("Arial", 1);

		private Game _game;

        // Graphics
        private GraphicsDevice _graphicsDevice;

        private BasicEffect _effect;
        private RasterizerState _rasterizerState;

        private byte[] _vertexData;
        private VertexBuffer _vertexBuffer;
        private int _vertexBufferSize;

        private byte[] _indexData;
        private IndexBuffer _indexBuffer;
        private int _indexBufferSize;

        // Textures
        private Dictionary<IntPtr, Texture2D> _loadedTextures;

        private int _textureId;
        private IntPtr? _fontTextureId;

        // Input
        private int _scrollWheelValue;
        private int _horizontalScrollWheelValue;
        private readonly float WHEEL_DELTA = 120;
        private Keys[] _allKeys = Enum.GetValues<Keys>();

        public bool[] KeysDown;
        private bool _leftCtrlPressed, _rightCtrlPressed;
        private bool _leftShiftPressed, _rightShiftPressed;
        private bool _leftAltPressed, _rightAltPressed;

        public ImGuiRenderer(Game game)
        {
            KeysDown = new bool[Enum.GetValues(typeof(Keys)).Cast<int>().Max()];

            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            _game = game ?? throw new ArgumentNullException(nameof(game));
            _graphicsDevice = game.GraphicsDevice;

            _loadedTextures = new Dictionary<IntPtr, Texture2D>();

            _rasterizerState = new RasterizerState()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };

            SetupInput();
        }

        #region ImGuiRenderer

        /// <summary>
        /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
        /// </summary>
        public virtual unsafe void RebuildFontAtlas()
        {
            // Get font texture from ImGui
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            // Copy the data to a managed array
            var pixels = new byte[width * height * bytesPerPixel];
            unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

            // Create and register the texture as an XNA texture
            var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            tex2d.SetData(pixels);

            // Should a texture already have been build previously, unbind it first so it can be deallocated
            if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);

            // Bind the new texture to an ImGui-friendly id
            _fontTextureId = BindTexture(tex2d);

            // Let ImGui know where to find the texture
            io.Fonts.SetTexID(_fontTextureId.Value);
            io.Fonts.ClearTexData(); // Clears CPU side texture data
        }

        /// <summary>
        /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
        /// </summary>
        public virtual IntPtr BindTexture(Texture2D texture)
        {
            var id = new IntPtr(_textureId++);

            _loadedTextures.Add(id, texture);

            return id;
        }

        /// <summary>
        /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
        /// </summary>
        public virtual void UnbindTexture(IntPtr textureId)
        {
            _loadedTextures.Remove(textureId);
        }

        /// <summary>
        /// Sets up ImGui for a new frame, should be called at frame start
        /// </summary>
        public virtual void BeforeLayout(GameTime gameTime)
        {
            ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdateInput();

            ImGui.NewFrame();
        }

        /// <summary>
        /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, should be called after the UI is drawn using ImGui.** calls
        /// </summary>
        public virtual void AfterLayout()
        {
            ImGui.Render();

            unsafe { RenderDrawData(ImGui.GetDrawData()); }
        }

        #endregion ImGuiRenderer

        #region Setup & Update

        /// <summary>
        /// Maps ImGui keys to XNA keys. We use this later on to tell ImGui what keys were pressed
        /// </summary>
        protected virtual void SetupInput()
        {
            var io = ImGui.GetIO();

            _game.Window.KeyDown += (o, e) =>
            {
                switch (e.Key)
                {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Home:
                    case Keys.End:
                        TextInput.Add(new TextInput(e.Key, Program.IsCtrlPressed));
                        break;
                    case Keys.LeftControl: _leftCtrlPressed = Program.IsCtrlPressed = true; break;
                    case Keys.RightControl: _rightCtrlPressed = Program.IsCtrlPressed = true; break;
                    case Keys.LeftShift: _leftShiftPressed = Program.IsShiftPressed = true; break;
                    case Keys.RightShift: _rightShiftPressed = Program.IsShiftPressed = true; break;
                    case Keys.LeftAlt: _leftAltPressed = Program.IsAltPressed = true; break;
                    case Keys.RightAlt: _rightAltPressed = Program.IsAltPressed = true; break;
                    case Keys.Escape:
                        if (ColorPicker.IsOpen)
                        {
                            ColorPicker.SuppressNextOpen();
                            ImGui.CloseCurrentPopup();
                            break;
                        }

                        if (MainWindow.FontPickerPopup.IsOpen)
                        {
                            MainWindow.FontPickerPopup.SuppressNextOpen();
                            ImGui.CloseCurrentPopup();
                            break;
                        }

                        if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopupId))
                        {
                            ImGui.CloseCurrentPopup();
                            break;
                        }

                        Program.ActiveInstance.ClearSelection();
                        Program.ActiveInstance.ActiveTool.EscapePressed();
                        break;
                    case Keys.Enter:
                        Program.ActiveInstance.ActiveTool.EnterPressed();
                        break;
                    case Keys.A:
                        if (Program.ActiveInstance.ActiveTool is ToolText t && t.CompText is not null) break;
                        if (Program.IsCtrlPressed)
                        {
                            Program.ActiveInstance.ClearSelection();
                            var marqueTool = Program.ActiveInstance.GetTool<ToolMarqueSelect>();
                            bool oldDragging = MainWindow.IsDragging;
                            MainWindow.IsDragging = true;
                            marqueTool!.OnMouseDownCanvas(0);
                            marqueTool.OnMouseMoveCanvas(Program.ActiveInstance.CanvasSize - 1);
                            marqueTool.OnMouseUp(0, 0);
                            MainWindow.IsDragging = oldDragging;
                        }
                        else if (Program.ActiveInstance.ActiveLayer is Layer layer && layer.Size != int2.Zero)
                        {
                            Program.ActiveInstance.ClearSelection();
                            var marqueTool = Program.ActiveInstance.GetTool<ToolMarqueSelect>();
                            bool oldDragging = MainWindow.IsDragging;
                            MainWindow.IsDragging = true;
                            marqueTool!.OnMouseDownCanvas(layer.Pos);
                            marqueTool.OnMouseMoveCanvas(layer.Pos + layer.Size - 1);
                            marqueTool.OnMouseUp(0, 0);
                            MainWindow.IsDragging = oldDragging;
                        }
                        break;
                    case Keys.Z:
                        if (Program.IsCtrlPressed) UndoManager.Undo();
                        break;
                    case Keys.Y:
                        if (Program.IsCtrlPressed) UndoManager.Redo();
                        break;
                    case Keys.C:
                        if (!Program.IsCtrlPressed) return;
                        if (Program.ActiveInstance.ActiveTool is ToolText)
                        {
                            // Copy text
                            TextInput.Add(new TextInput(true));
                            return;
                        }
                        if (Program.ActiveInstance?.Selection is not ISelector selection) return;

                        // Copy selection to clipboard
                        using (Image<Argb32> copiedImage = Program.ActiveInstance.Selection.GetImageFromRender())
                        {
                            DataObject dataObject = new DataObject();

                            using MemoryStream pngStream = new MemoryStream();
                            copiedImage.SaveAsPng(pngStream, new PngEncoder()
                            {
                                CompressionLevel = PngCompressionLevel.BestCompression
                            });
                            dataObject.SetData("PNG", pngStream);

                            using MemoryStream bmpStream = new MemoryStream();
                            copiedImage.SaveAsBmp(bmpStream, new BmpEncoder()
                            {
                                SupportTransparency = true
                            });
                            dataObject.SetData("Bitmap", bmpStream);

                            using (DirectBitmap bitmap = new DirectBitmap(copiedImage.Width, copiedImage.Height))
                            {
                                Parallel.For(0, copiedImage.Height, y =>
                                {
                                    Span<Argb32> row = copiedImage.DangerousGetPixelRowMemory(y).Span;
                                    for (int x = 0; x < row.Length; x++)
                                    {
                                        Argb32 pixel = row[x];
                                        bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixel.R, pixel.G, pixel.B));
                                    }
                                });
                                dataObject.SetData(DataFormats.Bitmap, bitmap.Bitmap);
                            }
                            Clipboard.SetDataObject(dataObject, true);
                        }
                        break;
                    case Keys.V:
                        if (!Program.IsCtrlPressed) return;
                        if (Program.ActiveInstance.ActiveTool is ToolText && Clipboard.ContainsText())
                        {
                            // Copy text
                            TextInput.Add(new TextInput(Clipboard.GetText()));
                            return;
                        }

                        if (Clipboard.ContainsImage())
                        {
                            // Paste image in
                            IDataObject data = Clipboard.GetDataObject();
                            string[] formats = data.GetFormats();
                            Image<Argb32> img;
                            if (formats.Contains("PNG")) img = Image.Load<Argb32>((MemoryStream)data.GetData("PNG", true));
                            else if (formats.Contains("Bitmap")) img = Image.Load<Argb32>((MemoryStream)data.GetData("Bitmap", true));
                            else
                            {
                                using Bitmap src = (Bitmap)Clipboard.GetImage();
                                img = new Image<Argb32>(src.Width, src.Height);

                                for (int y = 0; y < src.Height; y++)
                                {
                                    Span<Argb32> row = img.DangerousGetPixelRowMemory(y).Span;
                                    for (int x = 0; x < row.Length; x++)
                                    {
                                        System.Drawing.Color srcPixel = src.GetPixel(x, y);
                                        row[x].A = 255;
                                        row[x].R = srcPixel.R;
                                        row[x].G = srcPixel.G;
                                        row[x].B = srcPixel.B;
                                    }
                                }
                            }

                            Program.ActiveInstance.CreateLayer(img);
                        }

                        if (Clipboard.ContainsFileDropList())
                        {
                            foreach (string file in Clipboard.GetFileDropList())
                            {
                                MainWindow.Self.FileDrop(file);
                            }
                        }
                        break;
                    case Keys.S:
                        if (!Program.IsCtrlPressed) return;
                        // Save
                        string? path = Util.GetSavePath();
                        if (path is null)
                        {
                            // Save as
                            MainWindow.Self.SaveAs();
                            return;
                        }

                        // Save
                        MainWindow.Self.Save(path);
                        break;
                }
            };

            _game.Window.KeyUp += (o, e) =>
            {
                switch (e.Key)
                {
                    case Keys.LeftControl:
                        _leftCtrlPressed = false;
                        Program.IsCtrlPressed = _rightCtrlPressed;
                        break;
                    case Keys.RightControl:
                        _rightCtrlPressed = false;
                        Program.IsCtrlPressed = _leftCtrlPressed;
                        break;

                    case Keys.LeftShift:
                        _leftShiftPressed = false;
                        Program.IsShiftPressed = _rightShiftPressed;
                        break;
                    case Keys.RightShift:
                        _rightShiftPressed = false;
                        Program.IsShiftPressed = _leftShiftPressed;
                        break;

                    case Keys.LeftAlt:
                        _leftAltPressed = false;
                        Program.IsAltPressed = _rightAltPressed;
                        break;
                    case Keys.RightAlt:
                        _rightAltPressed = false;
                        Program.IsAltPressed = _leftAltPressed;
                        break;
                }
            };

            _game.Window.TextInput += (s, a) =>
            {
                char c = a.Character;
                if (c == 13) c = '\n'; // Replace carriage return with line break
                if (FontToCheckValidChars.GetGlyphs(new SixLabors.Fonts.Unicode.CodePoint(c), ColorFontSupport.None).First().GlyphMetrics.GlyphType != GlyphType.Fallback)
                    TextInput.Add(new TextInput(c));

                if (a.Character == '\t') return;
                io.AddInputCharacter(a.Character);
            };

            ImGui.GetIO().Fonts.AddFontDefault();
        }

        /// <summary>
        /// Updates the <see cref="Effect" /> to the current matrices and texture
        /// </summary>
        protected virtual Effect UpdateEffect(Texture2D texture)
        {
            _effect = _effect ?? new BasicEffect(_graphicsDevice);

            var io = ImGui.GetIO();

            _effect.World = Matrix.Identity;
            _effect.View = Matrix.Identity;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            _effect.TextureEnabled = true;
            _effect.Texture = texture;
            _effect.VertexColorEnabled = true;

            return _effect;
        }

        /// <summary>
        /// Sends XNA input state to ImGui
        /// </summary>
        protected virtual void UpdateInput()
        {
            if (!_game.IsActive) return;

            var io = ImGui.GetIO();

            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            io.AddMousePosEvent(mouse.X, mouse.Y);
            io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
            io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

            io.AddMouseWheelEvent(
                (mouse.HorizontalScrollWheelValue - _horizontalScrollWheelValue) / WHEEL_DELTA,
                (mouse.ScrollWheelValue - _scrollWheelValue) / WHEEL_DELTA);
            _scrollWheelValue = mouse.ScrollWheelValue;
            _horizontalScrollWheelValue = mouse.HorizontalScrollWheelValue;

            foreach (var key in _allKeys)
            {
                if (TryMapKeys(key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
                }
            }

            io.DisplaySize = new System.Numerics.Vector2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
        }

        private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
        {
            //Special case not handed in the switch...
            //If the actual key we put in is "None", return none and true. 
            //otherwise, return none and false.
            if (key == Keys.None)
            {
                imguikey = ImGuiKey.None;
                return true;
            }

            imguikey = key switch
            {
                Keys.Back => ImGuiKey.Backspace,
                Keys.Tab => ImGuiKey.Tab,
                Keys.Enter => ImGuiKey.Enter,
                Keys.CapsLock => ImGuiKey.CapsLock,
                Keys.Escape => ImGuiKey.Escape,
                Keys.Space => ImGuiKey.Space,
                Keys.PageUp => ImGuiKey.PageUp,
                Keys.PageDown => ImGuiKey.PageDown,
                Keys.End => ImGuiKey.End,
                Keys.Home => ImGuiKey.Home,
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.PrintScreen => ImGuiKey.PrintScreen,
                Keys.Insert => ImGuiKey.Insert,
                Keys.Delete => ImGuiKey.Delete,
                >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
                >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
                >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
                Keys.Multiply => ImGuiKey.KeypadMultiply,
                Keys.Add => ImGuiKey.KeypadAdd,
                Keys.Subtract => ImGuiKey.KeypadSubtract,
                Keys.Decimal => ImGuiKey.KeypadDecimal,
                Keys.Divide => ImGuiKey.KeypadDivide,
                >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (key - Keys.F1),
                Keys.NumLock => ImGuiKey.NumLock,
                Keys.Scroll => ImGuiKey.ScrollLock,
                Keys.LeftShift or Keys.RightShift => ImGuiKey.ModShift,
                Keys.LeftControl or Keys.RightControl => ImGuiKey.ModCtrl,
                Keys.LeftAlt or Keys.RightAlt => ImGuiKey.ModAlt,
                Keys.OemSemicolon => ImGuiKey.Semicolon,
                Keys.OemPlus => ImGuiKey.Equal,
                Keys.OemComma => ImGuiKey.Comma,
                Keys.OemMinus => ImGuiKey.Minus,
                Keys.OemPeriod => ImGuiKey.Period,
                Keys.OemQuestion => ImGuiKey.Slash,
                Keys.OemTilde => ImGuiKey.GraveAccent,
                Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
                Keys.OemCloseBrackets => ImGuiKey.RightBracket,
                Keys.OemPipe => ImGuiKey.Backslash,
                Keys.OemQuotes => ImGuiKey.Apostrophe,
                _ => ImGuiKey.None,
            };

            return imguikey != ImGuiKey.None;
        }

        #endregion Setup & Update

        #region Internals

        /// <summary>
        /// Gets the geometry as set up by ImGui and sends it to the graphics device
        /// </summary>
        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
            var lastViewport = _graphicsDevice.Viewport;
            var lastScissorBox = _graphicsDevice.ScissorRectangle;

            _graphicsDevice.BlendFactor = Color.White;
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.RasterizerState = _rasterizerState;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            // Setup projection
            _graphicsDevice.Viewport = new Viewport(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);

            UpdateBuffers(drawData);

            RenderCommandLists(drawData);

            // Restore modified state
            _graphicsDevice.Viewport = lastViewport;
            _graphicsDevice.ScissorRectangle = lastScissorBox;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        {
            if (drawData.TotalVtxCount == 0)
            {
                return;
            }

            // Expand buffers if we need more room
            if (drawData.TotalVtxCount > _vertexBufferSize)
            {
                _vertexBuffer?.Dispose();

                _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
                _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
                _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
            }

            if (drawData.TotalIdxCount > _indexBufferSize)
            {
                _indexBuffer?.Dispose();

                _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
                _indexData = new byte[_indexBufferSize * sizeof(ushort)];
            }

            // Copy ImGui's vertices and indices to a set of managed byte arrays
            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

                fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
                fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
                {
                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            // Copy the managed byte arrays to the gpu vertex- and index buffers
            _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
            _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
        {
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdListsRange[n];

                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                {
                    ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                    if (drawCmd.ElemCount == 0)
                    {
                        continue;
                    }

                    if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
                    {
                        throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                    }

                    _graphicsDevice.ScissorRectangle = new Rectangle(
                        (int)drawCmd.ClipRect.X,
                        (int)drawCmd.ClipRect.Y,
                        (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                        (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                    );

                    var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                        _graphicsDevice.DrawIndexedPrimitives(
                            primitiveType: PrimitiveType.TriangleList,
                            baseVertex: (int)drawCmd.VtxOffset + vtxOffset,
                            minVertexIndex: 0,
                            numVertices: cmdList.VtxBuffer.Size,
                            startIndex: (int)drawCmd.IdxOffset + idxOffset,
                            primitiveCount: (int)drawCmd.ElemCount / 3
                        );
#pragma warning restore CS0618
                    }
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }
        }

        #endregion Internals
    }
}