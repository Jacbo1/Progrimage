using ImGuiNET;
using NewMath;
using Progrimage.ImGuiComponents;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Progrimage.Tools
{
	public class ToolPipette : ITool
	{
		#region Fields
		// Public fields
		public const string CONST_NAME = "Pipette";
		private bool _sampleAllLayers = true;
		#endregion

		#region Properties
		public string Name => CONST_NAME;
		public TexPair Icon { get; private set; }
		#endregion

		#region Constructor
		public ToolPipette()
		{
			Icon = new(@"Assets\Textures\Tools\pipette.png", Defs.TOOL_ICON_SIZE, true);
		}
		#endregion

		#region ITool Methods
		public void DrawQuickActionsToolbar()
		{
			ImGui.Checkbox("Sample all layers ", ref _sampleAllLayers);
		}

		public void OnMouseDownCanvas(int2 pos)
		{
			if (!MainWindow.IsDragging) return;
			Argb32 color;
			int2 mousePos = MainWindow.MousePosCanvas;
			Instance instance = Program.ActiveInstance;
			if (_sampleAllLayers)
			{
				Image<Argb32> image = instance.RenderToImage();
				if (mousePos.X < 0 || mousePos.Y < 0 || mousePos.X >= image.Width || mousePos.Y >= image.Height) return;
				color = image[mousePos.X, mousePos.Y];
				image.Dispose();
			}
			else
			{
				if (instance.ActiveLayer?.Image?.Image == null) return;
				Layer layer = instance.ActiveLayer;
				Image<Argb32> image = layer.Image.Image!;
				mousePos -= layer.Pos;
				if (mousePos.X < 0 || mousePos.Y < 0 || mousePos.X >= image.Width || mousePos.Y >= image.Height) color = new Argb32(0, 0, 0, 0);
				else color = image[mousePos.X, mousePos.Y];
			}

			ColorPicker.PushColorToPalette("tool", color);
			instance.GetTool<ToolFill>()!.Color = color;
			instance.GetTool<ToolCubicCurve>()!.Color = color;
			instance.GetTool<ToolQuadraticCurve>()!.Color = color;
			instance.GetTool<ToolLine>()!.Color = color;
			instance.GetTool<ToolRect>()!.Color = color;
			instance.GetTool<ToolOval>()!.Color = color;
			instance.GetTool<ToolText>()!.Color = color;
			instance.BrushSettings.Color = new System.Numerics.Vector4(color.R, color.G, color.B, color.A) / 255f;
			instance.Stroke.BrushState = instance.BrushSettings;
		}
		#endregion
	}
}
