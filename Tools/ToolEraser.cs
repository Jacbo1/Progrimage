using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.DrawingShapes;
using Progrimage.Undo;
using Progrimage.Utils;

namespace Progrimage.Tools
{
    public class ToolEraser : ITool
    {
        #region Fields
        // Public fields
        public const string CONST_NAME = "Eraser";
        #endregion

        #region Properties
        public string Name => CONST_NAME;
        public TexPair Icon { get; private set; }
        #endregion

        #region Constructor
        public ToolEraser()
        {
            Icon = new(@"Assets\Textures\Tools\eraser.png", Defs.TOOL_ICON_SIZE, true);
        }
		#endregion

		#region ITool Methods
		public void Update(float _)
		{
			double2 cursorPos;
			if (Program.ActiveInstance.EraserSettings.IsPencil) cursorPos = MainWindow.MousePosCanvas + (Program.ActiveInstance.EraserSettings.Size % 2 == 1 ? 0.5 : 0);
			else cursorPos = MainWindow.MousePosCanvasDouble;
			Program.ActiveInstance.Draw(new DrawingCircleCursor(cursorPos, Program.ActiveInstance.EraserSettings.Size));
		}

        public void OnLayerDeselect(Layer layer)
        {
			Program.ActiveInstance.Stroke.WaitForJobs();
        }

        public void OnSelect(Instance instance)
        {
            instance.BrushMode = BrushMode.Eraser;
			instance.Stroke.BrushState = instance.EraserSettings;
			instance.Stroke.SetBrushTexture(instance.EraserSettings.Path);
		}

        public void DrawQuickActionsToolbar()
        {
			var instance = Program.ActiveInstance;

			// Color picker
			var alpha = instance.EraserSettings.Color.W;
			ImGui.SetNextItemWidth(100);
			ImGui.SliderFloat("Opacity", ref alpha, 0, 1);
			if (alpha != instance.BrushSettings.Color.W)
			{
				instance.EraserSettings.Color.W = alpha;
				instance.Stroke.BrushState = instance.EraserSettings;
			}
            ImGui.SameLine();

            // Brush size
            int size = instance.EraserSettings.Size;
            ImGui.SetNextItemWidth(100);
            ImGui.DragInt("Eraser size", ref size, 1, 1);
            size = Math.Max(size, 1);
            if (size != instance.EraserSettings.Size)
            {
                instance.EraserSettings.Size = size;
                instance.Stroke.BrushState = instance.EraserSettings;
            }

			// Pencil checkbox
            ImGui.SameLine();
			bool isPencil = instance.EraserSettings.IsPencil;
            if (ImGui.Checkbox("Pencil ", ref isPencil))
            {
				instance.EraserSettings.IsPencil = isPencil;
				instance.Stroke.BrushState = instance.EraserSettings;
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
			stroke.QueueErase(stroke.Layer.Image);
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
