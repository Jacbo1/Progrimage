using CalculatorLibrary;
using ImGuiNET;
using NewMath;
using Progrimage;
using Progrimage.Undo;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace ProgrimageImGui.Windows
{
	internal static class ResizeLayer
	{
		public static bool Show;
		private static bool _wasShowing, _maintainAspectRatio;
		private static string _widthInput = "", _heightInput = "";

		public static void TryShowResizeLayerWindow(ref bool mouseOverCanvasWindow)
		{
			if (Program.ActiveInstance.ActiveLayer is not Layer layer) return; // No active layer
			if (!Show)
			{
				_wasShowing = false;
				return;
			}

			if (!ImGui.Begin("Resize Layer", ref Show, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
				return;

			if (!_wasShowing)
			{
				_widthInput = layer.Width.ToString();
				_heightInput = layer.Height.ToString();
			}

			const int TARGET_TEXT_WIDTH = 50;
			ImGui.Text("Width:");
			ImGui.SameLine();
			ImGui.Indent(TARGET_TEXT_WIDTH);
			ImGui.SetNextItemWidth(100);
			string oldWidthString = _widthInput;
			ImGui.InputText("px ", ref _widthInput, 10, ImGuiInputTextFlags.CharsDecimal);
			_widthInput.Replace(".", "");
			ImGui.Unindent(TARGET_TEXT_WIDTH);

			ImGui.Text("Height:");
			ImGui.SameLine();
			ImGui.Indent(TARGET_TEXT_WIDTH);
			ImGui.SetNextItemWidth(100);
			string oldHeightString = _heightInput;
			ImGui.InputText("px", ref _heightInput, 10, ImGuiInputTextFlags.CharsDecimal);
			float itemHeight = ImGui.GetItemRectSize().Y;
			_heightInput.Replace(".", "");
			ImGui.Unindent(TARGET_TEXT_WIDTH);

			ImGui.Checkbox("Maintain aspect ratio", ref _maintainAspectRatio);

			int? width = null, height = null;
			if (Calculator.TryCalculateDouble(_widthInput, out double temp)) width = (int)Math.Round(temp, MidpointRounding.AwayFromZero);
			if (Calculator.TryCalculateDouble(_heightInput, out temp)) height = (int)Math.Round(temp, MidpointRounding.AwayFromZero);

			if (_maintainAspectRatio)
			{
				if (oldWidthString != _widthInput && width is not null)
				{
					height = (int)Math.Round((int)width / (double)layer.Width * layer.Height, MidpointRounding.AwayFromZero);
					_heightInput = ((int)height).ToString();
				}

				if (oldHeightString != _heightInput && height is not null)
				{
					width = (int)Math.Round((int)height / (double)layer.Height * layer.Width, MidpointRounding.AwayFromZero);
					_widthInput = ((int)width).ToString();
				}
			}

			int2? newSize = null;
			if (width is not null && height is not null)
				newSize = new int2((int)width, (int)height);

			bool valid = newSize is not null;
			if (valid)
			{
				int2 size = (int2)newSize;
				ImGui.Text($"({layer.Width}, {layer.Height}) => ({size.x}, {size.y})");
			}
			else ImGui.Text("Cannot apply");

			float windowWidth = ImGui.GetWindowWidth();
			windowWidth -= MainWindow.Style.ItemSpacing.X;

			ImGui.BeginDisabled(!valid);
			if (ImGui.Button("Apply", new Vector2(windowWidth * 0.5f, itemHeight)) && newSize is not null)
			{
				UndoManager.AddUndo(new UndoImagePatch(layer, layer.Pos, layer.Size));
				int2 size = (int2)newSize;
				if (layer.Image.Image is null) layer.Size = size;
				else layer.Image.Mutate(op => op.Resize(size.x, size.y));
				layer.Changed();
				Show = false;
			}
			ImGui.EndDisabled();

			ImGui.SameLine();
			if (ImGui.Button("Cancel", new Vector2(windowWidth * 0.5f, itemHeight)))
				Show = false;

			mouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.End();

			_wasShowing = true;
		}
	}
}
