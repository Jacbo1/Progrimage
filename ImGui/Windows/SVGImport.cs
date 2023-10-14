using CalculatorLibrary;
using ImGuiNET;
using NewMath;
using Progrimage;
using System.Numerics;
using Progrimage.Utils;
using LockedBitmapLibrary;
using Color = Microsoft.Xna.Framework.Color;

namespace ProgrimageImGui.Windows
{
	internal static class SVGImport
	{
		public static bool Show;
		private static bool _wasShowing;
		private static bool _maintainAspectRatio = true;
		private static string _widthInput = "", _heightInput = "";
		private static bool _hasThumbnail;
		private static string _path = "";
		private const int THUMBNAIL_SIZE = 100;
		private static TexPair _texture;
		private static double _svgWidth, _svgHeight;

		public static void TryShowWindow(ref bool mouseOverCanvasWindow)
		{
			if (_hasThumbnail == null) return;
			if (!Show)
			{
				_wasShowing = false;
				return;
			}

			if (!ImGui.Begin("Import SVG", ref Show, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)) return;

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
			if (Calculator.TryCalculateDouble(_widthInput, out double temp)) width = (int)(temp + 0.5);
			if (Calculator.TryCalculateDouble(_heightInput, out temp)) height = (int)(temp + 0.5);

			if (_maintainAspectRatio)
			{
				if (oldWidthString != _widthInput && width is not null)
				{
					height = (int)(width.Value / _svgWidth * _svgHeight + 0.5);
					_heightInput = ((int)height).ToString();
				}

				if (oldHeightString != _heightInput && height is not null)
				{
					width = (int)(height.Value / _svgHeight * _svgWidth + 0.5);
					_widthInput = ((int)width).ToString();
				}
			}

			int2? newSize = null;
			if (width is not null && height is not null) newSize = new int2((int)width, (int)height);

			bool valid = newSize is not null;
			if (valid)
			{
				int2 size = (int2)newSize;
				ImGui.Text($"({_svgWidth}x{_svgHeight}) => ({size.x}x{size.y})");
			}
			else ImGui.Text("Cannot apply");

			float windowWidth = ImGui.GetWindowWidth();
			windowWidth -= MainWindow.Style.ItemSpacing.X;

			ImGui.BeginDisabled(!valid);
			if (ImGui.Button("Import", new Vector2(windowWidth * 0.5f, itemHeight)) && newSize is not null)
			{
				int2 size = (int2)newSize;
				using Bitmap? bmp = Util.LoadSVG(_path, size.x, size.y, false);
				if (bmp != null)
				{
					Console.WriteLine("a");
					Program.ActiveInstance.CreateLayer(Util.BitmapToImage(bmp));
					Console.WriteLine("b");
				}
				Show = false;
			}
			ImGui.EndDisabled();

			ImGui.SameLine();
			if (ImGui.Button("Cancel", new Vector2(windowWidth * 0.5f, itemHeight)))
				Show = false;

			ImGui.Indent((ImGui.GetWindowWidth() - _texture.Size.x) / 2);
			ImGui.Image(_texture, _texture.Size);

			mouseOverCanvasWindow &= !ImGui.IsWindowHovered();
			ImGui.End();

			_wasShowing = true;
		}

		public static void SetPath(string path)
		{
			_path = path;
			_texture.Dispose();
			_hasThumbnail = false;
			if (!path.EndsWith(".svg") || !File.Exists(path)) return;

			Bitmap? image = Util.LoadSVG(path, THUMBNAIL_SIZE, THUMBNAIL_SIZE, true, out _svgWidth, out _svgHeight);
			if (image is null) return;

			_widthInput = _svgWidth.ToString();
			_heightInput = _svgHeight.ToString();
			_texture.Size = image.Size;
			_hasThumbnail = true;

			// Draw bitmap to texture
			Color[] pixels = new Color[_texture.Size.x * _texture.Size.y];
			LockedBitmap bitmap = new LockedBitmap(image);
			using LockedBitmap opaque = new LockedBitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			const int CHECKER_SIZE = 4;
			for (int x = 0; x < (int)Math.Ceiling(opaque.Width / (double)CHECKER_SIZE); x++)
			{
				int x1 = x * CHECKER_SIZE;
				for (int y = (x % 2) * CHECKER_SIZE; y < opaque.Height; y += CHECKER_SIZE * 2)
				{
					opaque.FillRectangle(System.Drawing.Color.LightGray, x1, y, CHECKER_SIZE, CHECKER_SIZE);
				}

				for (int y = ((x + 1) % 2) * CHECKER_SIZE; y < opaque.Height; y += CHECKER_SIZE * 2)
				{
					opaque.FillRectangle(SimpleColor.White, x1, y, CHECKER_SIZE, CHECKER_SIZE);
					opaque.GetPixel(x1, y);
				}
			}
			opaque.DrawImage(bitmap, 0, 0);
			bitmap.Dispose();

			byte[] imageData = new byte[opaque.Width * opaque.Height * 4];
			unsafe
			{
				for (int i = 0; i < imageData.Length; i += 4)
				{
					imageData[i] = opaque.Ptr[i + 2];
					imageData[i + 1] = opaque.Ptr[i + 1];
					imageData[i + 2] = opaque.Ptr[i];
					imageData[i + 3] = opaque.Ptr[i + 3];
				}
			}
			//unsafe
			//{
			//	Marshal.Copy((nint)opaque.Ptr, imageData, 0, imageData.Length);
			//}

			_texture.Texture.SetData(imageData);
		}
	}
}
