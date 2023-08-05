using ImageSharpExtensions;
using ImGuiNET;
using NewMath;
using Progrimage.CoroutineUtils;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;
using Progrimage.Utils;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.Tools
{
    public class ToolFill : ITool
	{
		#region Fields
		// Public fields
		public const string CONST_NAME = "Fill";

		// Private fields
		private Color _color = Color.Red;
		private bool _erase, _sampleAllLayers, _replacePixels;
		private bool _contiguous = true;
		private float _threshold = 0.1f;
		#endregion

		#region Properties
		public string Name => CONST_NAME;
		public TexPair Icon { get; private set; }
		#endregion

		#region Constructor
		public ToolFill()
		{
			Icon = new(@"Assets\Textures\Tools\fill.png", Defs.TOOL_ICON_SIZE, true);
		}
		#endregion

		#region ITool Methods
		public void OnMouseDownCanvas(int2 pos)
		{
			JobQueue.Queue.Add(new CoroutineJob(Run(pos)));
		}

		public void DrawQuickActionsToolbar()
		{
			// Color picker
			ImGui.PushID(ID.TOOL_COLOR_PICKER);
			ColorPicker.Draw("tool", ref _color, "", ID.TOOL_COLOR_PICKER);
			ImGui.PopID();
			ImGui.SameLine();

			ImGui.Checkbox("Contiguous", ref _contiguous);
			ImGui.SameLine();

			ImGui.Checkbox("Sample all layers", ref _sampleAllLayers);
			ImGui.SameLine();

			if (!_replacePixels)
			{
				ImGui.Checkbox("Erase", ref _erase);
				ImGui.SameLine();
			}

			ImGui.Checkbox("Replace pixels", ref _replacePixels);
			ImGui.SameLine();

			ImGui.SetNextItemWidth(100);
			ImGui.SliderFloat("Threshold", ref _threshold, 0, 1);
		}
		#endregion

		#region Private Methods
		private IEnumerator<bool> Run(int2 pos)
		{
			Layer? layer = Program.ActiveInstance.ActiveLayer;
			if (layer is not null) UndoManager.AddUndo(new UndoRegion(layer, layer.Pos, layer.Size));
			PositionedImage<Argb32> sampleImage;
			Argb32 baseColor = new Argb32(0, 0, 0, 0);
			bool sourceIsNew = layer is null;
			PositionedImage<Argb32> source = sourceIsNew ? new(int2.Zero, Program.ActiveInstance.CanvasSize) : layer!.Image;
			if (_sampleAllLayers)
			{
				int2 pos_, size;
				if (source.Image is null)
				{
					pos_ = int2.Zero;
					size = Program.ActiveInstance.CanvasSize;
				}
				else
				{
					pos_ = Math2.Min(source.Pos, 0);
					size = Math2.Max(source.Pos + source.Size, Program.ActiveInstance.CanvasSize) - pos_;
				}
				sampleImage = Program.ActiveInstance.LayerManager.Merge(pos_, size);
			}
			else if (sourceIsNew) sampleImage = new();
			else
			{
				sampleImage = layer!.Image;
				sampleImage.ExpandToContain(int2.Zero, Program.ActiveInstance.CanvasSize);
			}
			source.ExpandToContain(sampleImage.Pos, sampleImage.Size);

			if (sampleImage.Image is not null && pos >= sampleImage.Pos && pos < sampleImage.Pos + sampleImage.Size)
			{
				// Sample image
				int2 relative = pos - sampleImage.Pos;
				baseColor = sampleImage.Image[relative.x, relative.y];
			}

			double threshold = 260100.0 * _threshold * _threshold;
			Argb32 color = _color.ToPixel<Argb32>();
			byte iAlpha = (byte)(byte.MaxValue - color.A);

			if (sampleImage.Image is null)
			{
				// Not sampling all layers so sampleImage is the active layer's image or a new image
				// Simple fill because the image was null
				if (_erase && !_replacePixels) yield break;

				source.ExpandToContain(int2.Zero, Program.ActiveInstance.CanvasSize);
				DrawingOptions options = new DrawingOptions()
				{
					GraphicsOptions =
					{
						AlphaCompositionMode = _replacePixels ? PixelAlphaCompositionMode.Src : PixelAlphaCompositionMode.SrcOver
					}
				};
				source.Mutate(op => op.Fill(options, _color, new Rectangle(0, 0, sampleImage.Width, sampleImage.Height)));

				if (sourceIsNew) Program.ActiveInstance.CreateLayer(source);

                layer?.Changed();
                yield break;
			}
			else
			{
				// Flood fill
				bool[,] _checked = new bool[sampleImage.Width, sampleImage.Height];
				Stack<int2> _checkPoints = new();
				if (_contiguous) _checkPoints.Push(pos - sampleImage.Pos);
				else
				{
					for (int x = 0; x < sampleImage.Width; x++)
					{
						for (int y = 0; y < sampleImage.Height; y++)
						{
							_checkPoints.Push(new int2(x, y));
						}
					}
				}

				int xMax = sampleImage.Image.Width - 1;
				int yMax = sampleImage.Image.Height - 1;
				int yieldCheck = 1;
				while (_checkPoints.Any())
				{
					// Check for yield
					yieldCheck--;
					if (yieldCheck == 0)
					{
						Program.ActiveInstance.Changed();
						yieldCheck = 100;
						if (JobQueue.ShouldYield) yield return true;
					}

					// Check pixel
					pos = _checkPoints.Pop();
					if (_checked[pos.x, pos.y]) continue; // Already checked this point
					_checked[pos.x, pos.y] = true;
					Argb32 pixel = sampleImage.Image[pos.x, pos.y];
					int r = pixel.R - baseColor.R;
					int g = pixel.G - baseColor.G;
					int b = pixel.B - baseColor.B;
					int a = pixel.A - baseColor.A;
					if (r * r + g * g + b * b + a * a > threshold) continue; // Difference over threshold

					// Apply change
					int2 sourcePos = pos + sampleImage.Pos - source.Pos;
					pixel = source.Image![pos.x, pos.y];
					if (_replacePixels) pixel = color;
					else if (_erase)
					{
						// Erase
						pixel.A = (byte)(pixel.A * iAlpha / byte.MaxValue);
					}
					else
					{
						// Blend colors
						pixel = Util.Blend(pixel, color);
					}

					source.Image![pos.x, pos.y] = pixel;

					if (!_contiguous) continue;

					// Push neighboring pixels
					if (pos.x > 0) _checkPoints.Push(new int2(pos.x - 1, pos.y));
                    if (pos.x < xMax) _checkPoints.Push(new int2(pos.x + 1, pos.y));
                    if (pos.y > 0) _checkPoints.Push(new int2(pos.x, pos.y - 1));
					if (pos.y < yMax) _checkPoints.Push(new int2(pos.x, pos.y + 1));
				}
			}

			Program.ActiveInstance.Changed();
			layer?.Changed();
			if (!_sampleAllLayers) yield break;

			sampleImage.Dispose();

			if (sourceIsNew)
			{
				// Create new layer
				Program.ActiveInstance.CreateLayer(source);
				yield break;
			}
		}
		#endregion
	}
}
