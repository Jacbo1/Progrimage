using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.DrawingShapes;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;
using Progrimage.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Color = SixLabors.ImageSharp.Color;

namespace Progrimage.Tools
{
	public class ToolLine : ITool
	{
		#region Fields
		// Public fields
		public const string CONST_NAME = "Line";

		// Private fields
		private Vector4 _colorVec = Color.Black.ToVector4();
		private DrawingLine _drawingLine = new(Color.Black, 0, 0, 5);
		private DrawingShapeCollection _overlayShapeSet;
		private bool _shiftPressed;
		#endregion

		#region Properties
		public string Name => CONST_NAME;
		public TexPair Icon { get; private set; }
		#endregion

		#region Constructor
		public ToolLine()
		{
			Icon = new(@"Assets\Textures\Tools\line.png", Defs.TOOL_ICON_SIZE);
		}
		#endregion

		#region ITool Methods
		public void Update(float _)
		{
			if (_shiftPressed == Program.IsShiftPressed) return;
			_shiftPressed = Program.IsShiftPressed;
			if (MainWindow.IsDragging) DrawOverlay(MainWindow.MousePosCanvas);
		}

		public void OnMouseDownCanvas(int2 pos)
		{
			_drawingLine.Start = pos;
			_drawingLine.Stop = pos;
			_overlayShapeSet = new(Program.ActiveInstance.ActiveLayer!, _drawingLine);
			Program.ActiveInstance.ActiveLayer!.OverlayShapes.Add(_overlayShapeSet);
			DrawOverlay(pos);
		}

		public void OnMouseMoveCanvas(int2 pos)
		{
			if (MainWindow.IsDragging) DrawOverlay(pos);
		}

		public void OnMouseUp(int2 _, int2 pos)
		{
			_overlayShapeSet?.Dispose();
			if (!MainWindow.PostMouseDownStartInCanvas || Program.ActiveInstance.ActiveLayer is not Layer layer) return;
			var bounds = _drawingLine.GetBounds();
			UndoManager.AddUndo(new UndoImagePatch(layer, bounds));
			layer.Image.ExpandToContain(bounds);
			_drawingLine.Pos -= layer.Pos;
			((IShape)_drawingLine).Draw(layer);
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
				_drawingLine.Color = new Color(_colorVec);
			}
			ImGui.PopID();
			ImGui.SameLine();

			// Thickness slider
			ImGui.SameLine();
			ImGui.SetNextItemWidth(100);
			int temp = (int)_drawingLine.Thickness;
			ImGui.DragInt("Thickness", ref temp, 1, 1);
			_drawingLine.Thickness = Math.Max(temp, 1);
		}
		#endregion

		#region Private Methods
		private void DrawOverlay(int2 pos)
		{
			if (Program.ActiveInstance.ActiveLayer is null) return;

			if (Program.IsShiftPressed)
			{
				double2 pos_ = pos;
				if (Math.Abs(pos.x - _drawingLine.Start.x) > Math.Abs(pos.y - _drawingLine.Start.y))
					pos_.y = _drawingLine.Start.y;
				else pos_.x = _drawingLine.Start.x;
				_drawingLine.Stop = pos_;
			}
			else _drawingLine.Stop = pos;
			_overlayShapeSet.Shapes[0] = _drawingLine;
			Program.ActiveInstance.Changed = true;
		}
		#endregion
	}
}
