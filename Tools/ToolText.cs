using ImGuiNET;
using NewMath;
using Progrimage.Composites;
using Progrimage.DrawingShapes;
using Progrimage.ImGuiComponents;
using Progrimage.Utils;
using Color = SixLabors.ImageSharp.Color;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using SystemFonts = SixLabors.Fonts.SystemFonts;

namespace Progrimage.Tools
{
	public class ToolText : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Text";
        public CompText? CompText;

        // Private fields
        private int2 _corner, _resizeStartMin, _resizeStartMax, _resizeStartOffset;
        private ResizeDir? _resizeDir;
		private DrawingShapeCollection? _overlayShapeSet;
        private int _textPos, _cursorX;
        private double _cursorChangeTime;
        private bool _cursorBlinkOn, _shouldUpdateCursor, _holdCursorX;
        private int _prevFontSize = 50;
        private string _prevFontName = "Arial";
        private Color _prevColor = Color.Red;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }

		internal Color Color
		{
			set
            {
                _prevColor = value;
                if (CompText != null) CompText.Color = value;
            }
		}
		#endregion

		#region Constructor
		public ToolText()
        {
            Icon = new(@"Assets\Textures\Tools\text.png", Defs.TOOL_ICON_SIZE, true);
        }
		#endregion

		#region Public Methods
        public void SetCompText(CompText? comp)
        {
            if (comp is null && CompText is not null)
            {
                // Clear
                CompText = null;
				_resizeDir = null;
				_overlayShapeSet?.Dispose();
				_overlayShapeSet = null;
				return;
            }

            if (CompText == comp) return;

            // Set to existing text box
			CompText = comp;
			_overlayShapeSet?.Dispose();
			_overlayShapeSet = null;
            _textPos = CompText.Text.Length;

			Layer layer = Program.ActiveInstance.ActiveLayer!;
			_corner = CompText.MinBound + layer.Pos;
			var box = new DrawingBoundingBoxDots(CompText.MinBound, CompText.MaxBound, Color.Black);
			if (layer.Image.Image is null)
            {
				box.Min -= _corner;
				box.Max -= _corner;
			}

            var cursor = new DrawingRect(Color.Black, double2.Zero, new double2(2, CompText.Font.Size))
			{
				Hidden = true,
				ScaleThickness = false
			};
			_overlayShapeSet?.Dispose();
			_overlayShapeSet = new(Program.ActiveInstance.ActiveLayer!, box, cursor);
			_overlayShapeSet.AttachedToLayer = true;
			if (layer.Image.Image is null) _overlayShapeSet.Pos = _corner;
			Program.ActiveInstance.ActiveLayer!.RenderOverlayShapes.Add(_overlayShapeSet);
			Program.ActiveInstance.Changed();
			_cursorBlinkOn = true;
			_cursorChangeTime = 0;
			_shouldUpdateCursor = true;
			_holdCursorX = false;

            box.GetResizeDir(true, true, _overlayShapeSet);
			_resizeDir = null;
		}
		#endregion

		#region ITool Methods
		public void OnSelect(Instance instance)
        {
			CompText = null;
            _resizeDir = null;
            MainWindow.FontPicked += FontPicked; 
		}

        public void OnDeselect()
        {
			MainWindow.FontPicked -= FontPicked;
            CompText = null;
        }

        public void OnLayerDeselect(Layer _)
        {
			CompText = null;
			_resizeDir = null;
			_overlayShapeSet?.Dispose();
            _overlayShapeSet = null;
        }

        public void Update(float deltaTime)
        {
            if (CompText is null) return; // No CompText

            if (ImGuiRenderer.TextInput.Count != 0)
            {
                bool changed = false;
                _cursorChangeTime = 0;
                _cursorBlinkOn = true;
                _shouldUpdateCursor = true;
                for (int i = 0; i < ImGuiRenderer.TextInput.Count; i++)
                {
                    var input = ImGuiRenderer.TextInput[i];
                    switch (input.Key)
                    {
                        case Keys.Left:
                            _textPos = Math.Max(0, _textPos - 1);
                            CompText.LastMoveDir = -1;
							_holdCursorX = false;
                            break;
                        case Keys.Right:
                            _textPos = Math.Min(CompText.Text.Length, _textPos + 1);
							CompText.LastMoveDir = 1;
							_holdCursorX = false;
                            break;
                        case Keys.Up:
                            {
                                int lineNum = CompText.GetLineIndex(_textPos);
                                bool indexShifted = CompText.IsIndexShifted(_textPos);
								if (indexShifted && _textPos != 0) lineNum++;
                                if (lineNum == 0) break; // On first line

                                if (!_holdCursorX)
                                {
                                    // Get current x coordinate of cursor
                                    _holdCursorX = true;
                                    _cursorX = CompText.GetCursorPos(_textPos).x;
                                }

                                lineNum--;
                                int minDist = indexShifted ? _cursorX : int.MaxValue;
								_textPos = indexShifted ? -1 : 0;
								for (int j = 0; j < CompText.LetterBoxes[lineNum].Count; j++)
                                {
                                    int dist = Math.Abs(CompText.GetCursorPos(j, true, lineNum).x - _cursorX);
                                    if (dist > minDist) break;

                                    minDist = dist;
                                    _textPos++;
                                }

                                // Adjust for previous lines
                                for (int j = 0; j < lineNum; j++)
                                    _textPos += CompText.LetterBoxes[j].Count;
                            }
                            break;
                        case Keys.Down:
                            {
								int lineNum = CompText.GetLineIndex(_textPos);
								bool indexShifted = CompText.IsIndexShifted(_textPos);
								if (indexShifted && _textPos != 0) lineNum++;
								if (lineNum + 1 == CompText.LetterBoxes.Count) break; // On last line

								if (!_holdCursorX)
								{
									// Get current x coordinate of cursor
									_holdCursorX = true;
									_cursorX = CompText.GetCursorPos(_textPos).x;
								}

								lineNum++;
								int minDist = indexShifted ? _cursorX : int.MaxValue;
								_textPos = indexShifted ? -1 : 0;
								for (int j = 0; j < CompText.LetterBoxes[lineNum].Count; j++)
								{
									int dist = Math.Abs(CompText.GetCursorPos(j, true, lineNum).x - _cursorX);
									if (dist > minDist) break;

									minDist = dist;
									_textPos++;
								}

								// Adjust for previous lines
								for (int j = 0; j < lineNum; j++)
									_textPos += CompText.LetterBoxes[j].Count;
                            }
                            break;
                        case Keys.Home:
                            _holdCursorX = false;
							if (input.CtrlDown)
                            {
								// Move to start of text
								_textPos = 0;
                            }
                            else
                            {
								// Move to start of the line
								int lineNum = CompText.GetLineIndex(_textPos);
                                if (_textPos != 0 && CompText.IsIndexShifted(_textPos)) lineNum++;

								if (lineNum == 0)
                                {
                                    // On first line
                                    _textPos = 0;
									CompText.LastMoveDir = -1;
									break;
								}

								_textPos = 0;
                                for (int j = 0; j < lineNum; j++)
                                    _textPos += CompText.LetterBoxes[j].Count;
                            }
							CompText.LastMoveDir = -1;
							break;
                        case Keys.End:
                            _holdCursorX = false;
							if (input.CtrlDown)
                            {
                                // Move to end of text
                                _textPos = CompText.Text.Length;
                            }
                            else
                            {
								// Move to end of the line
								int lineNum = CompText.GetLineIndex(_textPos);
								if (_textPos != 0 && CompText.IsIndexShifted(_textPos)) lineNum++;
								_textPos = CompText.LetterBoxes[lineNum].Count;
								for (int j = 0; j < lineNum; j++)
									_textPos += CompText.LetterBoxes[j].Count;
							}
							CompText.LastMoveDir = 1;
							break;
                        default:
                            if (input.Char is null && input.Text == "") break;
                            // Type or paste

                            _holdCursorX = false;

                            if (input.Char == '\b')
                            {
								// Backspace
								CompText.LastMoveDir = -1;
								if (_textPos == 0) break; // At start of string
                                if (_textPos == CompText.Text.Length) CompText.Text = CompText.Text[..^1]; // At end of string
                                else CompText.Text = CompText.Text[..(_textPos - 1)] + CompText.Text[_textPos..]; // In middle of string
                                _textPos--;
                                changed = true;
								break;
                            }

                            if (input.Char == 127)
                            {
                                // Delete
                                if (_textPos == CompText.Text.Length) break; // At end of string
                                if (_textPos == 0) CompText.Text = CompText.Text[1..]; // At start of string
                                else CompText.Text = CompText.Text[.._textPos] + CompText.Text[(_textPos + 1)..]; // In middle of string
                                changed = true;
								break;
                            }

							// Type char or paste text
							CompText.LastMoveDir = 1;
							string text;
                            if (input.Char is null) text = input.Text;
                            else text = input.Char.ToString()!;

                            if (_textPos == 0) CompText.Text = text + CompText.Text; // At start of strng
                            else if (_textPos == CompText.Text.Length) CompText.Text += text; // At end of string
                            else CompText.Text = CompText.Text[.._textPos] + text + CompText.Text[_textPos..]; // In middle of string
                            _textPos += text.Length;
                            changed = true;
                            break;
                    }
                }
                
                if (changed) CompText.Changed();
            }
            else _cursorChangeTime += deltaTime;

            if (_cursorChangeTime >= Defs.TYPING_CURSOR_FLASH_INTERVAL)
            {
                _cursorChangeTime %= Defs.TYPING_CURSOR_FLASH_INTERVAL;
                _cursorBlinkOn = !_cursorBlinkOn;
                _shouldUpdateCursor = true;
            }

            if (!_shouldUpdateCursor) return;

            // Update cursor
            _shouldUpdateCursor = false;
            DrawingRect cursor = (DrawingRect)_overlayShapeSet!.Shapes[1];
            cursor.Hidden = !_cursorBlinkOn;
            if (_cursorBlinkOn) cursor.Pos = CompText.Composite.Layer.Image.Image is null ? int2.Zero : CompText.GetCursorPos(_textPos, false);
            _overlayShapeSet.Shapes[1] = cursor;
            Program.ActiveInstance.OverlayChanged();
        }

        public void OnMouseDownCanvas(int2 pos)
        {
			_resizeDir = (_overlayShapeSet?.Shapes[0] as DrawingBoundingBoxDots?)?.GetResizeDir(true, true, _overlayShapeSet);
            if (_resizeDir is not null)
            {
                // Resize or move text tool
                _resizeStartMin = CompText!.MinBound + CompText.Composite.Layer.Pos;
                _resizeStartMax = CompText!.MaxBound + CompText.Composite.Layer.Pos;
                _resizeStartOffset = pos - _resizeStartMin;
                return;
            }

            // Create new text box
            _corner = pos;
			Layer layer = Program.ActiveInstance.ActiveLayer!;
            int2 shiftedCorner = _corner - layer.Pos;
			var box = new DrawingBoundingBoxDots(shiftedCorner, shiftedCorner, Color.Black);
            var cursor = new DrawingRect(Color.Black, double2.Zero, new double2(2, _prevFontSize))
            {
                Hidden = true,
                ScaleThickness = false
            };
            _overlayShapeSet?.Dispose();
			_overlayShapeSet = new(Program.ActiveInstance.ActiveLayer!, box, cursor);
            _overlayShapeSet.AttachedToLayer = true;
            if (layer.Image.Image is null) _overlayShapeSet.Pos = pos;
            Program.ActiveInstance.ActiveLayer!.RenderOverlayShapes.Add(_overlayShapeSet);
            var comp = new CompText(shiftedCorner - layer.Pos, shiftedCorner - layer.Pos, SystemFonts.CreateFont(_prevFontName, _prevFontSize), _prevColor);
            CompText = comp;
            Program.ActiveInstance.ActiveLayer!.AddComposite(new Composite(Program.ActiveInstance.ActiveLayer!, comp));
            _textPos = comp.Text.Length;
            DrawOverlay(pos);
            _cursorBlinkOn = true;
            _cursorChangeTime = 0;
            _shouldUpdateCursor = true;
            _holdCursorX = false;
        }

        public void OnMouseMoveCanvas(int2 pos)
        {
            if (MainWindow.IsDragging)
            {
                if (_resizeDir is not null)
                {
                    // Move or resize text box
                    if (_overlayShapeSet is null || CompText is null) return;
                    DrawingBoundingBoxDots box = (DrawingBoundingBoxDots)_overlayShapeSet.Shapes[0];
                    box.Resize((ResizeDir)_resizeDir, _resizeStartMin, _resizeStartMax, _resizeStartOffset, _overlayShapeSet);
                    _overlayShapeSet.Shapes[0] = box;
                    CompText.MinBound = box.Min + (int2)_overlayShapeSet.Pos;
                    CompText.MaxBound = box.Max + (int2)_overlayShapeSet.Pos;
                    CompText.Changed();
                    Program.ActiveInstance.OverlayChanged();
                    return;
                }

                // Drag new text box
                DrawOverlay(pos);
                return;
            }

            // Set cursor
            (_overlayShapeSet?.Shapes[0] as DrawingBoundingBoxDots?)?.GetResizeDir(true, true, _overlayShapeSet);
		}

        public void OnMouseUp(int2 _, int2 pos)
        {
            _resizeDir = null;
			(_overlayShapeSet?.Shapes[0] as DrawingBoundingBoxDots?)?.GetResizeDir(true, true, _overlayShapeSet);
		}

        public void DrawQuickActionsToolbar()
        {
			// Color picker
			ImGui.PushID(ID.TOOL_COLOR_PICKER);
			if (ColorPicker.Draw("tool", ref _prevColor, "", ID.TOOL_COLOR_PICKER) && CompText is not null)
            {
                CompText.Color = _prevColor;
                CompText.Changed();
            }
			ImGui.PopID();
			ImGui.SameLine();

			if (ImGui.Button("Font")) MainWindow.FontPickerPopup.ShouldShow = true;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            if (ImGui.DragInt("Font Size", ref _prevFontSize, 0.5f, 1) && CompText is not null)
            {
                if (_overlayShapeSet is not null)
                {
					DrawingRect cursor = (DrawingRect)_overlayShapeSet.Shapes[1];
                    cursor.Size.y = _prevFontSize;
                    _overlayShapeSet.Shapes[1] = cursor;
				}
                _holdCursorX = false;
                _shouldUpdateCursor = true;
                CompText.Font = SystemFonts.CreateFont(CompText.Font.Name, _prevFontSize);
                CompText.Changed();
            }

        }
        #endregion

        #region Private Methods
        private void FontPicked(object _, EventArgs _2)
        {
            _prevFontName = MainWindow.SelectedFont;
            if (CompText is not null)
            {
				_holdCursorX = false;
				_shouldUpdateCursor = true;
				CompText.Font = SystemFonts.CreateFont(_prevFontName, CompText.Font.Size);
                CompText.Changed();
            }
        }

        private void DrawOverlay(int2 pos)
        {
            if (Program.ActiveInstance.ActiveLayer is not Layer layer) return;

            var box = (DrawingBoundingBoxDots)_overlayShapeSet!.Shapes[0];
            box.Min = CompText!.MinBound = Math2.Min(pos, _corner) - layer.Pos;
            box.Max = CompText!.MaxBound = Math2.Max(pos, _corner) - layer.Pos;
            box.Min -= (int2)_overlayShapeSet.Pos;
            box.Max -= (int2)_overlayShapeSet.Pos;
            _overlayShapeSet.Shapes[0] = box;
            Program.ActiveInstance.OverlayChanged();

            _cursorBlinkOn = true;
            _cursorChangeTime = 0;
            _shouldUpdateCursor = true;
            CompText.Changed();
        }
        #endregion
    }
}
