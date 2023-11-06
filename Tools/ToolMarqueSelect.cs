using NewMath;
using Progrimage.Utils;
using Progrimage.Selectors;

namespace Progrimage.Tools
{
    public class ToolMarqueSelect : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Marque Select";

        // Private fields
        private int2 _corner, _resizeStartMin, _resizeStartMax;
        private ResizeDir _resizeDir;
        private bool _resizing;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolMarqueSelect()
        {
            Icon = new(@"Assets\Textures\Tools\marque_select.png", Defs.TOOL_ICON_SIZE, true);
        }
        #endregion

        #region ITool Methods
        public void OnDeselect()
        {
            if (Program.ActiveInstance.Selection is null) return;
            Program.ActiveInstance.Selection.DrawBoundaryDots = false;
            Program.ActiveInstance.OverlayChanged();
        }

        public void OnMouseDownCanvas(int2 pos)
        {
            ISelector? selection = Program.ActiveInstance.Selection;
            ResizeDir? dir = selection?.GetResizeDir();
            _resizing = dir is not null;

            if (selection is not null) selection.DrawBoundaryDots = false;

            if (_resizing)
            {
                // Resizing
                _resizeDir = (ResizeDir)dir!;
                _resizeStartMin = selection!.Min;
                _resizeStartMax = selection!.Max;
                return;
            }

            // Not resizing
            Program.ActiveInstance.ClearSelection();
            pos = Math2.Clamp(pos, 0, Program.ActiveInstance.CanvasSize - 1);
            Program.ActiveInstance.Selection = new MarqueSelection(pos, pos, Program.ActiveInstance.ActiveLayer!);
            _corner = pos;
            Program.ActiveInstance.OverlayChanged();
        }

        public void OnMouseMoveCanvas(int2 pos)
        {
            ISelector? selection = Program.ActiveInstance.Selection;
            if (selection is null) return;

            pos = Math2.Clamp(pos, 0, Program.ActiveInstance.CanvasSize - 1);

            if (MainWindow.IsDragging)
            {
                // Mouse down
                Program.ActiveInstance.OverlayChanged();

                if (_resizing)
                {
                    // Resizing selection
                    Resize();
                    return;
                }

                // Not resizing
                selection!.Min = Math2.Min(pos, _corner);
                selection!.Max = Math2.Max(pos, _corner);
                return;
            }

            // Mouse not down
            selection.GetResizeDir();
        }

        public void OnMouseMoveScreen(int2 _)
        {
			ISelector? selection = Program.ActiveInstance.Selection;
			if (MainWindow.IsDragging || selection is null) return;
            selection.GetResizeDir();
		}

        public void OnMouseUp(int2 _, int2 _2)
        {
            Program.ActiveInstance.Selection?.GetResizeDir();
        }
        #endregion

        #region Private methods
        private void Resize()
        {
            if (Program.ActiveInstance.Selection is not ISelector selection) return; // No selection

            int2 mousePos = MainWindow.MousePosCanvas;
            switch (_resizeDir)
            {
                case ResizeDir.Up:
                    // Drag top middle
                    selection.Min = new int2(selection.Min.X, Math.Min(_resizeStartMax.Y, mousePos.Y));
                    selection.Max = new int2(selection.Max.X, Math.Max(_resizeStartMax.Y, mousePos.Y));
                    break;
                case ResizeDir.Down:
                    // Drag bottom middle
                    selection.Min = new int2(selection.Min.X, Math.Min(_resizeStartMin.Y, mousePos.Y));
                    selection.Max = new int2(selection.Max.X, Math.Max(_resizeStartMin.Y, mousePos.Y));
                    break;
                case ResizeDir.Left:
                    // Drag left middle
                    selection.Min = new int2(Math.Min(_resizeStartMax.X, mousePos.X), selection.Min.Y);
                    selection.Max = new int2(Math.Max(_resizeStartMax.X, mousePos.X), selection.Max.Y);
                    break;
                case ResizeDir.Right:
                    // Drag right middle
                    selection.Min = new int2(Math.Min(_resizeStartMin.X, mousePos.X), selection.Min.Y);
                    selection.Max = new int2(Math.Max(_resizeStartMin.X, mousePos.X), selection.Max.Y);
                    break;
                case ResizeDir.UpLeft:
                    // Drag top left
                    selection.Min = Math2.Min(_resizeStartMax, mousePos);
                    selection.Max = Math2.Max(_resizeStartMax, mousePos);
                    break;
                case ResizeDir.UpRight:
                    // Drag top right
                    selection.Min = new int2(Math.Min(_resizeStartMin.X, mousePos.X), Math.Min(_resizeStartMax.Y, mousePos.Y));
                    selection.Max = new int2(Math.Max(_resizeStartMin.X, mousePos.X), Math.Max(_resizeStartMax.Y, mousePos.Y));
                    break;
                case ResizeDir.DownLeft:
                    // Drag bottom left
                    selection.Min = new int2(Math.Min(_resizeStartMax.X, mousePos.X), Math.Min(_resizeStartMin.Y, mousePos.Y));
                    selection.Max = new int2(Math.Max(_resizeStartMax.X, mousePos.X), Math.Max(_resizeStartMin.Y, mousePos.Y));
                    break;
                case ResizeDir.DownRight:
                    // Drag bottom right
                    selection.Min = Math2.Min(_resizeStartMin, mousePos);
                    selection.Max = Math2.Max(_resizeStartMin, mousePos);
                    break;
            }
        }
        #endregion
    }
}
