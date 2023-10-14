using CalculatorLibrary;
using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using Progrimage;
using System.Numerics;

namespace ProgrimageImGui.Windows
{
	internal static class ResizeImage
	{
		public static bool Show;
		private static bool _wasShowing, _maintainAspectRatio;
		private static string _widthInput = "", _heightInput = "";

		public static void TryShowWindow(ref bool mouseOverCanvasWindow)
		{
			if (!Show)
			{
				_wasShowing = false;
				return;
			}

			if (!ImGui.Begin("Resize Image", ref Show, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) return;

			if (!_wasShowing)
			{
				_widthInput = Program.ActiveInstance.CanvasSize.x.ToString();
				_heightInput = Program.ActiveInstance.CanvasSize.y.ToString();
			}

			const int TARGET_TEXT_WIDTH = 50;
			ImGui.Text("Width:");
			ImGui.SameLine();
			ImGui.Indent(TARGET_TEXT_WIDTH);
			ImGui.SetNextItemWidth(100);
			string oldWidthString = _widthInput;
			ImGui.InputText("px ", ref _widthInput, 100, ImGuiInputTextFlags.CharsDecimal);
			_widthInput.Replace(".", "");
			ImGui.Unindent(TARGET_TEXT_WIDTH);

			ImGui.Text("Height:");
			ImGui.SameLine();
			ImGui.Indent(TARGET_TEXT_WIDTH);
			ImGui.SetNextItemWidth(100);
			string oldHeightString = _heightInput;
			ImGui.InputText("px", ref _heightInput, 100, ImGuiInputTextFlags.CharsDecimal);
			float itemHeight = ImGui.GetItemRectSize().Y;
			_heightInput.Replace(".", "");
			ImGui.Unindent(TARGET_TEXT_WIDTH);

			ImGui.Checkbox("Maintain aspect ratio", ref _maintainAspectRatio);

			int? width = null, height = null;
			if (Calculator.TryCalculateDouble(_widthInput, out double temp)) width = (int)Math.Round(temp, MidpointRounding.AwayFromZero);
			if (Calculator.TryCalculateDouble(_heightInput, out temp)) height = (int)Math.Round(temp, MidpointRounding.AwayFromZero);

			if (_maintainAspectRatio)
			{
				int2 canvasSize = Program.ActiveInstance.CanvasSize;
				if (oldWidthString != _widthInput && width is not null)
				{
					height = (int)Math.Round((int)width / (double)canvasSize.x * canvasSize.y, MidpointRounding.AwayFromZero);
					_heightInput = ((int)height).ToString();
				}

				if (oldHeightString != _heightInput && height is not null)
				{
					width = (int)Math.Round((int)height / (double)canvasSize.y * canvasSize.x, MidpointRounding.AwayFromZero);
					_widthInput = ((int)width).ToString();
				}
			}

			int2? newSize = null;
			if (width is not null && height is not null)
				newSize = new int2((int)width, (int)height);

			if (newSize is not null)
			{
				int2 canvasSize = Program.ActiveInstance.CanvasSize;
				int2 size = (int2)newSize;
				ImGui.Text($"({canvasSize.x}, {canvasSize.y}) => ({size.x}, {size.y})");
			}
			else ImGui.Text("Cannot apply");

			float windowWidth = ImGui.GetWindowWidth();
			windowWidth -= MainWindow.Style.ItemSpacing.X;

			if (ImGui.Button("Apply", new Vector2(windowWidth * 0.5f, itemHeight)) && newSize is not null)
			{
				var instance = Program.ActiveInstance;
				int2 size = (int2)newSize;
				double2 scale = size / (double2)Program.ActiveInstance.CanvasSize;
				instance.CanvasSize = (int2)newSize;
				var layers = instance.LayerManager.Layers;
				for (int i = 0; i < layers.Count; i++)
				{
					Layer layer = layers[i];
					int2 layerSize = Math2.Round(layer.Size * scale);
					if (layerSize <= layer.Size)
					{
						// High quality proprietary downscale
						if (layer.Image.Image is not null)
							ISEUtils.HighQualityDownscale(ref layer.Image.Image, layerSize);
					}
					else
					{
						// Default scaling
						layer.Image.Mutate(op => op.Resize(layerSize.x, layerSize.y));
					}
					layer.Pos = Math2.Round(layer.Pos * scale);
					layer.Changed();
				}
				Show = false;
			}

			ImGui.SameLine();
			if (ImGui.Button("Cancel", new Vector2(windowWidth * 0.5f, itemHeight)))
				Show = false;

			mouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.End();

			_wasShowing = true;
		}
	}
}
