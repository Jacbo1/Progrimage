using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.Selectors;
using Progrimage.Undo;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        private int2 _resizeStartMin, _resizeStartMax, _originalPos;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolMove()
        {
            Icon = new(@"Assets\Textures\Tools\move.png", Defs.TOOL_ICON_SIZE);
        }
        #endregion

        #region ITool Methods
        public void OnDeselect()
        {
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
                Resize(pos);
                return;
            }

            if (MainWindow.MouseInCanvas) Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
            else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
			if (_moving)
            {
                // Move
                if (_movingSelection)
                {
                    // Move selection
                    if (Program.ActiveInstance.Selection is null) return;
                    var delta = pos - JobQueue.State.LastMousePosCanvas;
                    Program.ActiveInstance.Selection!.Pos += delta;
                }
                else Program.ActiveInstance.ActiveLayer!.Pos += pos - JobQueue.State.LastMousePosCanvas;
                Program.ActiveInstance.Changed = true;
            }
        }

        public void OnMouseDownCanvas(int2 pos)
        {
            if (Program.ActiveInstance.ActiveLayer is null || _moving || !MainWindow.IsDragging) return;
			_originalPos = Program.ActiveInstance.ActiveLayer.Pos;

			_moving = true;
            ResizeDir? dir = Program.ActiveInstance.Selection?.GetResizeDir(ImGuiMouseCursor.ResizeAll);
            _resizing = dir is not null;

            if (Program.ActiveInstance.Selection is not null)
                Program.ActiveInstance.Selection.DrawBoundaryDots = false;

            if (_resizing)
            {
                // Resize
                _resizeDir = (ResizeDir)dir!;
                ISelector selection = Program.ActiveInstance.Selection!;

                _resizeStartMin = selection.Min;
                _resizeStartMax = selection.Max;

                _prevResizeFlipH = _resizeFlipH;
                _prevResizeFlipV = _resizeFlipV;

                return;
            }

			// Move
			if (MainWindow.MouseInCanvas) Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
			else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
			if (Program.ActiveInstance.Selection is not null)
                _movingSelection = true; // Move the selection
            else _movingSelection = false; // Move the entire layer
		}

        public void OnMouseMoveScreen(int2 mousePos)
        {
            if (!_moving) Program.ActiveInstance.Selection?.GetResizeDir(ImGuiMouseCursor.ResizeAll);
        }

        public void OnMouseUp(int2 _, int2 _2)
        {
			Layer? layer = Program.ActiveInstance.ActiveLayer;
            if (_moving && !_movingSelection && layer is not null)
            {
                int2 newPos = layer.Pos;
                UndoManager.AddUndo(new UndoAction(
                    () =>
                    {
                        layer.Pos = _originalPos;
                        Program.ActiveInstance.Changed = true;
                    },
                    () =>
                    {
                        layer.Pos = newPos;
                        Program.ActiveInstance.Changed = true;
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
