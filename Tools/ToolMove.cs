using ImGuiNET;
using NewMath;
using Progrimage.DrawingShapes;
using Progrimage.Selectors;
using Progrimage.Undo;
using Progrimage.Utils;
using SixLabors.Fonts.Unicode;

namespace Progrimage.Tools
{
    public class ToolMove : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Move";

        // Private fields
        private bool _movingSelection, _moving, _resizing, _resizeFlipH, _resizeFlipV, _prevResizeFlipH, _prevResizeFlipV;
        private Image<Argb32>? _sourceImage;
        private ResizeDir _resizeDir;
        private int2 _resizeStartMin, _resizeStartMax, _originalPos, _mouseDownPos;
        //private List<UndoAction> _undos = new();
        //private bool _changed;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolMove()
        {
            Icon = new(@"Assets\Textures\Tools\move.png", Defs.TOOL_ICON_SIZE, true);
        }
        #endregion

        #region ITool Methods
        public void OnDeselect()
        {
            //UndoManager.RemoveUndos(_undos.ToArray());
            //_undos.Clear();
            if (Program.ActiveInstance.Selection is not null)
            {
                Program.ActiveInstance.Selection.DrawImage();
                Program.ActiveInstance.Selection.DrawImageInOverlay = false;
                Program.ActiveInstance.Selection.GrabSource = true;
                Program.ActiveInstance.Selection.DrawBoundaryDots = false;
            }
            Apply();
            Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
            _resizeFlipH = false;
            _resizeFlipV = false;
        }

        public void OnSelect(Instance instance)
        {
            //_undos.Clear();
            if (instance.Selection is not null)
            {
                _sourceImage = instance.Selection.Image.Clone();
                instance.Selection.GrabSource = false;
                instance.Selection.DrawImageInOverlay = true;
                instance.Selection.ClearSelection();
            }
        }

        public void OnMouseMoveCanvas(int2 pos)
        {
            if (_resizing)
            {
                // Resize
                if (Program.IsShiftPressed)
                {
                    double2 dir = (_resizeStartMax - _resizeStartMin).Normalize();
                    bool skip = false;
                    switch (_resizeDir)
                    {
                        case ResizeDir.DownLeft:
                        case ResizeDir.UpRight: dir.x = -dir.x; break;
                        case ResizeDir.DownRight:
                        case ResizeDir.UpLeft: break;
                        default: skip = true; break;
                    }

                    if (!skip)
                    {
                        int2 delta = pos - _mouseDownPos;
                        pos = Math2.RoundToInt(_mouseDownPos + dir * Math2.Sign(delta / dir) * Math2.Abs(delta / dir).max);
                    }
                }

                Resize(pos);
                return;
            }

            if (MainWindow.MouseInCanvas) Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
			if (_moving)
            {
                // Move
                int2 lastMousePos = MainWindow.LastMousePosCanvas;
                if (Program.IsShiftPressed)
                {
                    if (Math.Abs(pos.x - _mouseDownPos.x) > Math.Abs(pos.y - _mouseDownPos.y)) pos.y = _mouseDownPos.y;
                    else pos.x = _mouseDownPos.x;

                    if (Math.Abs(lastMousePos.x - _mouseDownPos.x) > Math.Abs(lastMousePos.y - _mouseDownPos.y)) lastMousePos.y = _mouseDownPos.y;
                    else lastMousePos.x = _mouseDownPos.x;
                }

                if (_movingSelection)
                {
                    // Move selection
                    if (Program.ActiveInstance.Selection is null) return;
                    int2 delta = pos - lastMousePos;
                    Program.ActiveInstance.Selection!.Pos += delta;
                }
                else Program.ActiveInstance.ActiveLayer!.Pos += pos - lastMousePos;
                Program.ActiveInstance.Changed();
            }
        }

        public void OnMouseDownCanvas(int2 pos)
        {
            //_changed = false;
            if (Program.ActiveInstance.ActiveLayer is null || _moving || !MainWindow.IsDragging) return;
			_originalPos = Program.ActiveInstance.ActiveLayer.Pos;
            _mouseDownPos = pos;

            _moving = true;
            ResizeDir? dir = Program.ActiveInstance.Selection?.GetResizeDir(ImGuiMouseCursor.ResizeAll);
            _resizing = dir is not null;

            if (Program.ActiveInstance.Selection is not null)
            {
                Program.ActiveInstance.Selection.DrawBoundaryDots = false;
				Program.ActiveInstance.Selection.ClearSelection();
			}

            if (_resizing)
            {
                // Resize
                _resizeDir = (ResizeDir)dir!;
                ISelector selection = Program.ActiveInstance.Selection!;

                switch (_resizeDir)
                {
                    case ResizeDir.DownRight: _mouseDownPos = selection.Min; break;
                    case ResizeDir.DownLeft: _mouseDownPos = new int2(selection.Max.x, selection.Min.y); break;
                    case ResizeDir.UpLeft: _mouseDownPos = selection.Max; break;
                    case ResizeDir.UpRight: _mouseDownPos = new int2(selection.Min.x, selection.Max.y); break;
                }

                _resizeStartMin = selection.Min;
                _resizeStartMax = selection.Max;

                _prevResizeFlipH = _resizeFlipH;
                _prevResizeFlipV = _resizeFlipV;

                return;
            }

			// Move
			if (MainWindow.MouseInCanvas) Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
			else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
			if (Program.ActiveInstance.Selection is not null) _movingSelection = true; // Move the selection
            else _movingSelection = false; // Move the entire layer
		}

        public void OnMouseMoveScreen(int2 mousePos)
        {
            if (!_moving) Program.ActiveInstance.Selection?.GetResizeDir(ImGuiMouseCursor.ResizeAll);
        }

        public void OnMouseUp(int2 _, int2 _2)
        {
            //if (_changed)
            //{
            //    _changed = false;
            //    if (_resizing && Program.ActiveInstance.Selection is not null)
            //    {
            //        ISelector selection = Program.ActiveInstance.Selection!;
            //        int2 newMin = selection.Min;
            //        int2 newMax = selection.Max;
            //        bool newFlipH = _resizeFlipH;
            //        bool newFlipV = _resizeFlipV;

            //        Image<Argb32> copy = selection.Image.Clone();
            //        UndoAction undoAction = _undos[_undos.Count - 1];
            //        undoAction.RedoDelegate = () =>
            //        {
            //            selection.Image = copy;
            //            _resizeFlipH = newFlipH;
            //            _resizeFlipV = newFlipV;
            //            selection.Min = newMin;
            //            selection.Max = newMax;
            //            Program.ActiveInstance.Changed();
            //        };
            //        undoAction.Disposed += (o, e) =>
            //        {
            //            if (selection.Image != copy) copy.Dispose();
            //        };

            //        UndoManager.AddUndo(undoAction);
            //    }
            //}

			Layer? layer = Program.ActiveInstance.ActiveLayer;
            if (_moving && !_movingSelection && layer is not null)
            {
                int2 newPos = layer.Pos;
                int2 oldPos = _originalPos;
                UndoManager.AddUndo(new UndoAction(
                    () =>
                    {
                        layer.Pos = oldPos;
                        Program.ActiveInstance.Changed();
                    },
                    () =>
                    {
                        layer.Pos = newPos;
                        Program.ActiveInstance.Changed();
                    })
                );
			}
            Apply();
            Program.ActiveInstance.Selection?.GetResizeDir(ImGuiMouseCursor.ResizeAll);
        }

        public string[] DrawBottomBar()
        {
            if (!_moving) return new string[] { };
            if (_movingSelection) return new[] { $"Move: ({Program.ActiveInstance.Selection!.Pos.x}x, {Program.ActiveInstance.Selection!.Pos.y}y)" };
            if (Program.ActiveInstance.ActiveLayer is null) return new string[] { };
            return new[] { $"Move: ({Program.ActiveInstance.ActiveLayer.Pos.x}x, {Program.ActiveInstance.ActiveLayer.Pos.y}y)" };
        }
		#endregion

		#region Private Methods
		private void Apply()
        {
            _moving = false;
            _resizing = false;
            _movingSelection = false;
        }

        private void Resize(int2 mousePos)
        {
            if (Program.ActiveInstance.Selection is null) return; // No selection

            ISelector selection = Program.ActiveInstance.Selection!;

            int2 oldMin = selection.Min;
            int2 oldMax = selection.Max;
            bool oldFlipH = _resizeFlipH;
            bool oldFlipV = _resizeFlipV;

            selection.DrawImageInOverlay = true;
            switch (_resizeDir)
            {
                case ResizeDir.Up:
                    // Drag top middle
                    _resizeFlipV = (mousePos.y > _resizeStartMax.y) ^ _prevResizeFlipV;
                    selection.Min = new int2(selection.Min.x, Math.Min(_resizeStartMax.y, mousePos.y));
                    selection.Max = new int2(selection.Max.x, Math.Max(_resizeStartMax.y, mousePos.y));
                    break;
                case ResizeDir.Down:
                    // Drag bottom middle
                    _resizeFlipV = (mousePos.y < _resizeStartMin.y) ^ _prevResizeFlipV;
                    selection.Min = new int2(selection.Min.x, Math.Min(_resizeStartMin.y, mousePos.y));
                    selection.Max = new int2(selection.Max.x, Math.Max(_resizeStartMin.y, mousePos.y));
                    break;
                case ResizeDir.Left:
                    // Drag left middle
                    _resizeFlipH = (mousePos.x > _resizeStartMax.x) ^ _prevResizeFlipH;
                    selection.Min = new int2(Math.Min(_resizeStartMax.x, mousePos.x), selection.Min.y);
                    selection.Max = new int2(Math.Max(_resizeStartMax.x, mousePos.x), selection.Max.y);
                    break;
                case ResizeDir.Right:
                    // Drag right middle
                    _resizeFlipH = (mousePos.x < _resizeStartMin.x) ^ _prevResizeFlipH;
                    selection.Min = new int2(Math.Min(_resizeStartMin.x, mousePos.x), selection.Min.y);
                    selection.Max = new int2(Math.Max(_resizeStartMin.x, mousePos.x), selection.Max.y);
                    break;
                case ResizeDir.UpLeft:
                    // Drag top left
                    _resizeFlipH = (mousePos.x > _resizeStartMax.x) ^ _prevResizeFlipH;
                    _resizeFlipV = (mousePos.y > _resizeStartMax.y) ^ _prevResizeFlipV;
                    selection.Min = Math2.Min(_resizeStartMax, mousePos);
                    selection.Max = Math2.Max(_resizeStartMax, mousePos);
                    break;
                case ResizeDir.UpRight:
                    // Drag top right
                    _resizeFlipH = (mousePos.x < _resizeStartMin.x) ^ _prevResizeFlipH;
                    _resizeFlipV = (mousePos.y > _resizeStartMax.y) ^ _prevResizeFlipV;
                    selection.Min = new int2(Math.Min(_resizeStartMin.x, mousePos.x), Math.Min(_resizeStartMax.y, mousePos.y));
                    selection.Max = new int2(Math.Max(_resizeStartMin.x, mousePos.x), Math.Max(_resizeStartMax.y, mousePos.y));
                    break;
                case ResizeDir.DownLeft:
                    // Drag bottom left
                    _resizeFlipH = (mousePos.x > _resizeStartMax.x) ^ _prevResizeFlipH;
                    _resizeFlipV = (mousePos.y < _resizeStartMin.y) ^ _prevResizeFlipV;
                    selection.Min = new int2(Math.Min(_resizeStartMax.x, mousePos.x), Math.Min(_resizeStartMin.y, mousePos.y));
                    selection.Max = new int2(Math.Max(_resizeStartMax.x, mousePos.x), Math.Max(_resizeStartMin.y, mousePos.y));
                    break;
                case ResizeDir.DownRight:
                    // Drag bottom right
                    _resizeFlipH = (mousePos.x < _resizeStartMin.x) ^ _prevResizeFlipH;
                    _resizeFlipV = (mousePos.y < _resizeStartMin.y) ^ _prevResizeFlipV;
                    selection.Min = Math2.Min(_resizeStartMin, mousePos);
                    selection.Max = Math2.Max(_resizeStartMin, mousePos);
                    break;
            }

            int2 size = selection.Max - selection.Min + 1;
            Image<Argb32> img = _sourceImage!.Clone();

            //if (!_changed)
            //{
            //    _changed = true;
            //    Image<Argb32> copy = img.Clone();
            //    UndoAction undo = new UndoAction(() =>
            //    {
            //        selection.Image = copy;
            //        _resizeFlipH = oldFlipH;
            //        _resizeFlipV = oldFlipV;
            //        selection.Min = oldMin;
            //        selection.Max = oldMax;
            //        Program.ActiveInstance.Changed();
            //    }, () => { });
            //    undo.Disposed += (o, e) =>
            //    {
            //        if (selection.Image != copy) copy.Dispose();
            //    };
            //    _undos.Add(undo);
            //}

            img.Mutate(i =>
            {
                i.Resize(size.x, size.y, KnownResamplers.Triangle);
                if (_resizeFlipH) i.Flip(FlipMode.Horizontal); // Flip horizontal
                if (_resizeFlipV) i.Flip(FlipMode.Vertical); // Flip vertical
            });
            selection.Image = img;
        }
        #endregion
    }
}
