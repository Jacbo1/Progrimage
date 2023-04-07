using ImGuiNET;
using NewMath;
using Progrimage.DrawingShapes;
using Progrimage.Selectors;
using Progrimage.Utils;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;

namespace Progrimage.Tools
{
    internal class ToolCrop : ITool
	{
		#region Fields
		// Public fields
		public const string CONST_NAME = "Crop";

		// Private
		private DrawingShapeCollection? _overlay;
		private static readonly Color _color = new Color(new Argb32(0, 0, 0, 100));
		private ToolMarqueSelect _toolMarqueSelect;
		#endregion

		#region Properties
		public string Name => CONST_NAME;
		public TexPair Icon { get; private set; }
		#endregion

		#region Constructor
		public ToolCrop()
		{
			Icon = new(@"Assets\Textures\Tools\crop.png", Defs.TOOL_ICON_SIZE, true);
		}
		#endregion

		#region ITool Methods
		public void OnLayerDeselect(Layer layer)
		{
			_overlay?.Dispose();
			_overlay = null;
			Program.ActiveInstance.OverlayChanged = true;
		}

		public void OnLayerSelect(Layer layer)
		{
			_overlay = new DrawingShapeCollection(layer, 0, new DrawingRect(_color, 0, 0), new DrawingRect(_color, 0, 0), new DrawingRect(_color, 0, 0), new DrawingRect(_color, 0, 0));
			layer.RenderOverlayShapes.Add(_overlay);
			DrawOverlay();
		}

		public void OnDeselect()
		{
			_toolMarqueSelect.OnDeselect();
			Program.ActiveInstance.ClearSelection();
			Util.SetMouseCursor(ImGuiMouseCursor.Arrow);
		}

		public void OnSelect(Instance instance)
		{
			_toolMarqueSelect = instance.GetTool<ToolMarqueSelect>()!;

			int2 minBound = new int2(int.MaxValue, 0);
			int2 maxBound = new int2(int.MinValue, Program.ActiveInstance.CanvasSize.y - 1);

			// Autocrop based on transparency
			using var img = Program.ActiveInstance.LayerManager.Merge().Image;
			bool getY = true;
			for (int y = 0; y < img.Height; y++)
			{
				var row = img.DangerousGetPixelRowMemory(y).Span;

				// Get min x
				for (int x = 0; x < (getY ? img.Width : Math.Min(minBound.x, img.Width)); x++)
				{
					if (row[x].A == 0) continue;

					if (getY)
					{
						minBound.y = y;
						getY = false;
					}
					minBound.x = Math.Min(minBound.x, x);
					break;
				}

				// Get max x
				for (int x = img.Width - 1; x > Math.Max(0, maxBound.x); x--)
				{
					if (row[x].A == 0) continue;

					maxBound.x = Math.Max(maxBound.x, x);
					break;
				}
			}

			// Get max y
			for (int y = img.Height - 1; y >= minBound.y; y--)
			{
				var row = img.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < img.Width; x++)
				{
					if (row[x].A == 0) continue;
					maxBound.y = y;
					goto MAX_Y_END;
				}
			}
		MAX_Y_END:

			if (minBound.x != int.MaxValue)
			{
				// Auto-cropped
				bool oldDragging = MainWindow.IsDragging;
				MainWindow.IsDragging = true;
				_toolMarqueSelect.OnMouseDownCanvas(minBound);
				_toolMarqueSelect.OnMouseMoveCanvas(maxBound);
				MainWindow.IsDragging = oldDragging;
				_toolMarqueSelect.OnMouseUp(int2.Zero, int2.Zero);
				DrawOverlay();
			}
		}

		public void OnMouseDownCanvas(int2 pos)
		{
			_toolMarqueSelect.OnMouseDownCanvas(pos);
			if (!MainWindow.IsDragging) return;
			DrawOverlay();
		}

		public void OnMouseMoveCanvas(int2 pos)
		{
			_toolMarqueSelect.OnMouseMoveCanvas(pos);
			if (!MainWindow.IsDragging) return;
			DrawOverlay();
		}

		public void OnMouseMoveScreen(int2 pos) => _toolMarqueSelect.OnMouseMoveScreen(pos);
		public void OnMouseUp(int2 _, int2 _2) => _toolMarqueSelect.OnMouseUp(_, _2);

		public void EnterPressed()
		{
			// Apply
			if (Program.ActiveInstance.Selection is not MarqueSelection selection) return;
			List<Layer> layers = Program.ActiveInstance.LayerManager.Layers;
			for (int i = 0; i < layers.Count; i++)
			{
				bool wasNotNull = layers[i].Image is not null;
				if (wasNotNull) layers[i].TextureExpanded(layers[i].Pos - selection.Min);
				int2 oldPos = layers[i].Pos;
				layers[i].Image.Crop(selection.Min, selection.Max - selection.Min + 1);
				if (wasNotNull && layers[i].Image is null)
					layers[i].TextureExpanded(-oldPos);
				layers[i].Pos -= selection.Min;
				layers[i].Changed();
			}
			Program.ActiveInstance.CanvasSize = selection.Max - selection.Min + 1;
			Program.ActiveInstance.ClearSelection();
			DrawOverlay();
		}

		public void EscapePressed()
		{
			// Clear
			DrawOverlay();
		}
		#endregion

		#region Private Methods
		private void DrawOverlay()
		{
			if (_overlay is null) return;
			if (Program.ActiveInstance.Selection is not MarqueSelection selection)
			{
				// Clear
				_overlay.Hidden = true;
				Program.ActiveInstance.OverlayChanged = true;
				return;
			}
			_overlay.Hidden = false;

			var rect = (DrawingRect)_overlay.Shapes[0];
			rect.Pos = double2.Zero;
			rect.Size = new double2(selection.Min.x, Program.ActiveInstance.CanvasSize.y);
			_overlay.Shapes[0] = rect;

			rect = (DrawingRect)_overlay.Shapes[1];
			rect.Pos = new double2(selection.Max.x + 1, 0);
			rect.Size = new double2(Program.ActiveInstance.CanvasSize.x - selection.Max.x - 1, Program.ActiveInstance.CanvasSize.y);
			_overlay.Shapes[1] = rect;

			rect = (DrawingRect)_overlay.Shapes[2];
			rect.Pos = new double2(selection.Min.x, 0);
			rect.Size = new double2(selection.Max.x - selection.Min.x + 1, selection.Min.y);
			_overlay.Shapes[2] = rect;

			rect = (DrawingRect)_overlay.Shapes[3];
			rect.Pos = new double2(selection.Min.x, selection.Max.y + 1);
			rect.Size = new double2(selection.Max.x - selection.Min.x + 1, Program.ActiveInstance.CanvasSize.y - selection.Max.y - 1);
			_overlay.Shapes[3] = rect;

			Program.ActiveInstance.OverlayChanged = true;
		}
		#endregion
	}
}
