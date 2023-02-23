using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.DrawingShapes;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;
using Progrimage.Utils;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Progrimage.Tools
{
	internal class ToolCubicCurve : ITool
	{
		#region Fields
		// Public fields
		public const string CONST_NAME = "Cubic Curve";

		// Private
		private DrawingGrabPoint[] _dragPoints = new DrawingGrabPoint[4];
		private DrawingCubicCurve _curve = new();
		private DrawingShapeCollection? _pointOverlay, _curveOverlay;
		private int _grabbedPointIndex = -1;
		private bool _curveReady;
		private double2 _grabOffset;
		#endregion

		#region Properties
		public string Name => CONST_NAME;
		public TexPair Icon { get; private set; }
		#endregion

		#region Constructor
		public ToolCubicCurve()
		{
			Icon = new(@"Assets\Textures\Tools\cubic_curve.png", Defs.TOOL_ICON_SIZE);
		}
		#endregion

		#region ITool Methods
		public void OnLayerDeselect(Layer layer)
		{
			_pointOverlay?.Dispose();
			_pointOverlay = null;
			_curveOverlay?.Dispose();
			_curveOverlay = null;
		}

		public void OnLayerSelect(Layer layer)
		{
			_pointOverlay = new(layer, 0, _dragPoints[0], _dragPoints[1], _dragPoints[2], _dragPoints[3]) { Hidden = true };
			_curveOverlay = new(layer, 0, _curve) { Hidden = true };
			layer.OverlayShapes.Add(_curveOverlay);
			layer.RenderOverlayShapes.Add(_pointOverlay);
			_curveReady = false;
		}

		public void DrawQuickActionsToolbar()
		{
			// Color picker
			ImGui.PushID(ID.TOOL_COLOR_PICKER);
			if (ColorPicker.Draw("tool", ref _curve.Color, "", ID.TOOL_COLOR_PICKER) && _curveOverlay is not null)
			{
				_curveOverlay.Shapes[0] = _curve;
				Program.ActiveInstance.Changed |= _curveReady;
			}
			ImGui.PopID();
			ImGui.SameLine();

			// Thickness
			ImGui.SetNextItemWidth(100);
			int temp = (int)_curve.Thickness;
			ImGui.DragInt("Thickness", ref temp, 1, 1);
			temp = Math.Max(temp, 1);
			if (temp != _curve.Thickness)
			{
				_curve.Thickness = temp;
				if (_curveOverlay is not null)
				{
					_curveOverlay.Shapes[0] = _curve;
					Program.ActiveInstance.Changed |= _curveReady;
				}
			}
		}

		public void OnMouseDownCanvas(int2 _)
		{
			if (!MainWindow.IsDragging) return;

			_pointOverlay!.Hidden = false;
			_curveOverlay!.Hidden = false;
			if (_curveReady)
			{
				// Try to grab a point
				var result = TryGetHoveredPoint();
				if (result is null) _grabbedPointIndex = -1; // No point
				else
				{
					// Point grabbed
					(_grabbedPointIndex, _grabOffset) = ((int, double2))result;
					Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
					return;
				}
			}

			// No current curve or did not grab a point
			_grabbedPointIndex = -1;
			for (int i = 0; i < 4; i++)
			{
				_curve.Points[i] = _dragPoints[i].Pos = MainWindow.MousePosCanvasDouble;
				_pointOverlay.Shapes[i] = _dragPoints[i];
			}
			_curveOverlay.Shapes[0] = _curve;
			_curveReady = true;
			Program.ActiveInstance.Changed = true;
		}

		public void OnMouseMoveCanvasDouble(double2 pos)
		{
			if (!MainWindow.IsDragging)
			{
				if (TryGetHoveredPoint() is not null)
					Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
				else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
				return;
			}

			if (_grabbedPointIndex == -1)
			{
				_curve.Points[1] = _dragPoints[1].Pos = Math2.Lerp(_dragPoints[0].Pos, pos, 1.0 / 3.0);
				_curve.Points[2] = _dragPoints[2].Pos = Math2.Lerp(_dragPoints[0].Pos, pos, 2.0 / 3.0);
				_curve.Points[3] = _dragPoints[3].Pos = pos;

				for (int i = 1; i < 4; i++)
					_pointOverlay!.Shapes[i] = _dragPoints[i];
				_curveOverlay!.Shapes[0] = _curve;

				Program.ActiveInstance.Changed = true;
				return;
			}

			_curve.Points[_grabbedPointIndex] = _dragPoints[_grabbedPointIndex].Pos = pos + _grabOffset;

			_pointOverlay!.Shapes[_grabbedPointIndex] = _dragPoints[_grabbedPointIndex];
			_curveOverlay!.Shapes[0] = _curve;

			Program.ActiveInstance.Changed = true;
		}

		public void OnMouseUp(int2 _, int2 _2)
		{
			if (_grabbedPointIndex == -1) return;
			_grabbedPointIndex = -1;
			if (TryGetHoveredPoint() is not null)
				Util.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
			else Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
		}

		public void EnterPressed()
		{
			if (!_curveReady || Program.ActiveInstance.ActiveLayer is not Layer layer) return;

			var bounds = _curve.GetBounds();
			UndoManager.AddUndo(new UndoImagePatch(layer, bounds));
			layer.Image.ExpandToContain(bounds);
			double2 curPos = _curve.Pos;
			_curve.Pos -= layer.Pos;
			layer.Image.Image.Mutate(_curve.Draw);
			_curve.Pos = curPos;

			_curveReady = false;
			_pointOverlay!.Hidden = true;
			_curveOverlay!.Hidden = true;
			Program.ActiveInstance.Changed = true;
		}

		public void EscapePressed()
		{
			if (_pointOverlay is not null) _pointOverlay.Hidden = true;
			if (_curveOverlay is not null) _curveOverlay!.Hidden = true;
			_curveReady = false;
			Program.ActiveInstance.Changed = true;
		}
		#endregion

		#region Private Methods
		private (int, double2)? TryGetHoveredPoint()
		{
			for (int i = 0; i < 4; i++)
			{
				double2? offset = _dragPoints[i].CheckProximity();
				if (offset is double2 d) return (i, d);
			}
			return null;
		}
		#endregion
	}
}
