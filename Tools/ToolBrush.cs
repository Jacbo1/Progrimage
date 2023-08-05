using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.DrawingShapes;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;
using Progrimage.Utils;

namespace Progrimage.Tools
{
    public class ToolBrush : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Brush";
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolBrush()
        {
            Icon = new(@"Assets\Textures\Tools\brush.png", Defs.TOOL_ICON_SIZE, true);
        }
        #endregion

        #region ITool Methods
        public void Update(float _)
        {
            double2 cursorPos;
            if (Program.ActiveInstance.BrushSettings.IsPencil) cursorPos = MainWindow.MousePosCanvas + (Program.ActiveInstance.BrushSettings.Size % 2 == 1 ? 0.5 : 0);
            else cursorPos = MainWindow.MousePosCanvasDouble;
            Program.ActiveInstance.Draw(new DrawingCircleCursor(cursorPos, Program.ActiveInstance.BrushSettings.Size));
        }

        public void OnLayerDeselect(Layer layer)
        {
			Program.ActiveInstance.Stroke.WaitForJobs();
		}

        public void OnSelect(Instance instance)
        {
            instance.BrushMode = BrushMode.Brush;
            instance.Stroke.BrushState = instance.BrushSettings;
            instance.Stroke.SetBrushTexture(instance.BrushSettings.Path);
        }

        public void DrawQuickActionsToolbar()
        {
            var instance = Program.ActiveInstance;

            // Color picker
            ImGui.PushID(ID.TOOL_COLOR_PICKER);
            var color = instance.BrushSettings.Color;
            ColorPicker.Draw("tool", ref color, "", ID.TOOL_COLOR_PICKER);
            if (color != instance.BrushSettings.Color)
            {
				instance.BrushSettings.Color = color;
				instance.Stroke.BrushState = instance.BrushSettings;
            }
            ImGui.PopID();
            ImGui.SameLine();

            // Brush size
            int size = instance.BrushSettings.Size;
            ImGui.SetNextItemWidth(100);
            ImGui.DragInt("Brush size", ref size, 1, 1);
            size = Math.Max(size, 1);
            if (size != instance.BrushSettings.Size)
            {
				instance.BrushSettings.Size = size;
				instance.Stroke.BrushState = instance.BrushSettings;
            }

			// Pencil checkbox
			ImGui.SameLine();
			bool isPencil = instance.BrushSettings.IsPencil;
			if (ImGui.Checkbox("Pencil ", ref isPencil))
			{
				instance.BrushSettings.IsPencil = isPencil;
				instance.Stroke.BrushState = instance.BrushSettings;
			}
		}

        public void OnMouseDownCanvas(int2 pos)
        {
            if (!MainWindow.IsDragging) return;
            Program.ActiveInstance.Stroke.Layer = Program.ActiveInstance.ActiveLayer;
			Program.ActiveInstance.Stroke.BeginStroke(MainWindow.MousePosCanvasDouble);
            JobQueue.Queue.Add(new CoroutineJob(Program.ActiveInstance.Changed));
        }

        public void OnMouseMoveCanvasDouble(double2 pos)
        {
            if (!MainWindow.IsDragging) return;
            Program.ActiveInstance.Stroke.ContinueStroke(pos);
            JobQueue.Queue.Add(new CoroutineJob(Program.ActiveInstance.Changed));
        }

        public void OnMouseUp(int2 _, int2 _2)
        {
            Stroke stroke = Program.ActiveInstance.Stroke;
            if (stroke.Layer is null) return;
            JobQueue.Queue.Add(new CoroutineJob(() => UndoManager.AddUndo(new UndoRegion(stroke.Layer, stroke.Min, stroke.Size))));
            stroke.QueueDraw(stroke.Layer.Image, true);
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                stroke.Layer.Changed();
                stroke.Layer = null;
                stroke.QueueClear();
            }));
        }
        #endregion
    }
}