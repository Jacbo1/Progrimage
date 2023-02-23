using ImGuiNET;
using NewMath;
using Color = SixLabors.ImageSharp.Color;
using System.Numerics;
using Progrimage.Utils;
using Progrimage.DrawingShapes;
using Progrimage.CoroutineUtils;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;

namespace Progrimage.Tools
{
    public class ToolOval : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Oval";

        // Private fields
        private int2 _corner;
        private Vector4 _colorVec = Color.Black.ToVector4();
        private DrawingOval _drawingOval = new(Color.Black, int2.Zero, int2.Zero, 5, true);
        private DrawingShapeCollection _overlayShapeSet;
        private bool _wasShiftPressed, _startCenter;
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolOval()
        {
            Icon = new(@"Assets\Textures\Tools\oval.png", Defs.TOOL_ICON_SIZE);
        }
        #endregion

        #region ITool Methods
        public void Update(float _)
        {
			if (_wasShiftPressed == Program.IsShiftPressed) return;
			_wasShiftPressed = Program.IsShiftPressed;
            if (MainWindow.IsDragging) DrawOverlay(MainWindow.MousePosCanvas);
		}

        public void OnMouseDownCanvas(int2 pos)
        {
            _startCenter = Program.IsCtrlPressed;
			_corner = pos;
            _drawingOval.Pos = int2.Zero;
            _drawingOval.Size = int2.Zero;
            _overlayShapeSet = new(Program.ActiveInstance.ActiveLayer!, _drawingOval);
            Program.ActiveInstance.ActiveLayer!.OverlayShapes.Add(_overlayShapeSet);
            DrawOverlay(pos);
        }

        public void OnMouseMoveCanvas(int2 pos)
        {
            if (JobQueue.State.IsMouseDown && JobQueue.State.MouseDownStartInCanvas)
                DrawOverlay(pos);
        }

        public void OnMouseUp(int2 _, int2 pos)
        {
            _overlayShapeSet?.Dispose();
            if (!JobQueue.State.PostMouseDownStartInCanvas || Program.ActiveInstance.ActiveLayer is not Layer layer) return;
            var bounds = _drawingOval.GetBounds();
			UndoManager.AddUndo(new UndoImagePatch(layer, bounds));
			layer.Image.ExpandToContain(bounds);
			_drawingOval.Pos -= layer.Pos;
            ((IShape)_drawingOval).Draw(layer);
        }

        public void DrawQuickActionsToolbar()
        {
            // Color picker
            ImGui.PushID(ID.TOOL_COLOR_PICKER);
            Vector4 color = _colorVec;
            ColorPicker.Draw("tool", ref color, "", ID.TOOL_COLOR_PICKER);
            if (color != _colorVec)
            {
                _colorVec = color;
                _drawingOval.Color = new Color(_colorVec);
            }
            ImGui.PopID();
            ImGui.SameLine();

            // Filled checkbox
            ImGui.Checkbox("Filled", ref _drawingOval.Fill);

            if (_drawingOval.Fill) return; // Already done

            // Outline
            // Extend inwards or outwards
            ImGui.SameLine();
            ImGui.Checkbox("Extend outline inwards", ref _drawingOval.ExtendInwards);

            // Thickness slider
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);

            int temp = (int)_drawingOval.Thickness;
            ImGui.DragInt("Thickness", ref temp, 1, 1);
            _drawingOval.Thickness = Math.Max(temp, 1);
        }
        #endregion

        #region Private Methods
        private void DrawOverlay(int2 pos)
        {
            if (Program.ActiveInstance.ActiveLayer is null) return;

			if (Program.IsShiftPressed)
			{
				// Make circle
				if (Math.Abs(pos.x - _corner.x) > Math.Abs(pos.y - _corner.y))
					pos.y = _corner.y + Math.Abs(pos.x - _corner.x) * Math.Sign(pos.y - _corner.y);
				else pos.x = _corner.x + Math.Abs(pos.y - _corner.y) * Math.Sign(pos.x - _corner.x);
			}

            int2 min = Math2.Min(pos, _corner);
            int2 max = Math2.Max(pos, _corner);
            int2 size = max - min + 1;
            if (_startCenter)
            {
                _drawingOval.Pos = min - size;
				size *= 2;
			}
            else _drawingOval.Pos = min;
			_drawingOval.Size = size;
            _overlayShapeSet.Shapes[0] = _drawingOval;
            Program.ActiveInstance.Changed = true;
        }
        #endregion
    }
}
