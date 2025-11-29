using ImageSharpExtensions;
using ImGuiNET;
using Jacbo.Math2;
using LimParallel;
using Progrimage.CoroutineUtils;
using Progrimage.ImGuiComponents;
using Progrimage.Undo;
using Progrimage.Utils;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using static Progrimage.Utils.FilePicker;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.Tools
{
	public class ToolFill : ITool
	{
		private enum FillLineDirection : byte
		{
			Up,
			Down,
			UpDown
		}

		private struct FillLine(int x, int y, int width, bool isMovingDown, int parentWidth = -1)
        {
			public int X = x;
			public int Y = y;
			public int Width = width;
			public bool IsMovingDown = isMovingDown;
        }

        #region Fields
        // Public fields
        public const string CONST_NAME = "Fill";

		// Private fields
		internal Color Color = Color.Red;
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
			ColorPicker.Draw("tool", ref Color, "", ID.TOOL_COLOR_PICKER);
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
				baseColor = sampleImage.Image[relative.X, relative.Y];
			}

			double threshold = 260100.0 * _threshold * _threshold;
			Argb32 color = Color.ToPixel<Argb32>();
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
				source.Mutate(op => op.Fill(options, Color, new Rectangle(0, 0, sampleImage.Width, sampleImage.Height)));

				if (sourceIsNew) Program.ActiveInstance.CreateLayer(source);

				layer?.Changed();
				yield break;
			}
			else if (_contiguous)
			{
				var coroutine = FloodFill(sampleImage, source, baseColor, pos - sampleImage.Pos);
				while (coroutine.MoveNext()) yield return coroutine.Current;
			}
			else FillNonContiguous(sampleImage, source, baseColor);

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

		private IEnumerator<bool> FloodFill(PositionedImage<Argb32> sampleImage, PositionedImage<Argb32> source, Argb32 baseColor, int2 startingPos)
		{
			if (source.Image == null || sampleImage.Image == null) yield break;

			int2 sourceOffset = sampleImage.X - source.X;
			double threshold = 260100.0 * _threshold * _threshold;
			Argb32 color = Color.ToPixel<Argb32>();
			byte iAlpha = (byte)(byte.MaxValue - color.A);

			bool IsPixelValid(in Argb32 pixel)
			{
				int r = pixel.R - baseColor.R;
				int g = pixel.G - baseColor.G;
				int b = pixel.B - baseColor.B;
				int a = pixel.A - baseColor.A;
				return r * r + g * g + b * b + a * a <= threshold;
			}

			if (!IsPixelValid(source.Image[startingPos.X, startingPos.Y])) yield break; // Invalid starting pixel

			// Prevent repeatedly drawing the same pixels
			HashSet<int2> lineStarts = [];

			List<FillLine> createdLines = [];
			void CreateLines(in Span<Argb32> sampleRow, in Span<Argb32> srcRow, int x, int y, int parentWidth, bool isMovingDown, bool clear = true)
			{
				int parentX = x;
				int parentRight = x + parentWidth;
				if (clear) createdLines.Clear();

				int createdLinesInitialIndex = createdLines.Count;

				if (parentWidth != -1)
				{
					// Find line start
					bool valid = false;
					for (int x1 = x; x1 < parentRight; x1++)
					{
						if (IsPixelValid(sampleRow[x1]))
						{
							x = x1;
							valid = true;
							break;
						}
					}
					if (!valid) return;
				}

				FillLine line = new()
				{
					X = x,
					Y = y,
					IsMovingDown = isMovingDown
				};

				bool repeat = true;
				while (repeat)
				{
					x = line.X;
					repeat = false;

					// Get leftmost bound
					for (int x1 = x; x1 >= 0; x1--)
					{
						if (IsPixelValid(sampleRow[x1])) line.X = x1;
						else break;
					}

					line.Width = x - line.X + 1;

					// Get rightmost bound
					for (int x1 = x + 1; x1 < sampleImage.Width; x1++)
					{
						if (IsPixelValid(sampleRow[x1])) line.Width++;
						else break;
					}

					int lineRight = line.X + line.Width;

					if (lineStarts.Add(new(line.X, line.Y)))
					{
						// Add line to output
						createdLines.Add(line);

						// Draw line
						int stopX = line.X + line.Width + sourceOffset.X;
						for (int x1 = line.X + sourceOffset.X; x1 < stopX; x1++)
						{
							ref var srcPixel = ref srcRow[x1];

							// Apply change
							if (_replacePixels) srcPixel = color;
							else if (_erase)
							{
								// Erase
								srcPixel.A = (byte)(srcPixel.A * iAlpha / byte.MaxValue);
							}
							else
							{
								// Blend colors
								srcPixel = Util.Blend(srcPixel, color);
							}
						}
					}

                    // Try to create more lines
                    if (parentWidth != -1 && lineRight < parentRight)
                    {
                        // New line ends more to the left than its parent so scan border to the right
                        for (int x1 = line.X + line.Width + 1; x1 < parentRight; x1++)
                        {
                            if (IsPixelValid(sampleRow[x1]))
                            {
                                // Found new region on other side of border connected to parent line
                                line.X = x1;
                                repeat = true;
                                break;
                            }
                        }
                    }
                }

				if (parentWidth == -1 || createdLinesInitialIndex >= createdLines.Count) return;

				// Look for overhangs
				var leftLine = createdLines[createdLinesInitialIndex];
				int leftX = leftLine.X;
				int leftExtent = parentX - leftX;

				var rightLine = createdLines[^1];
				int rightX = parentX + parentWidth;
				int rightExtent = rightLine.X + rightLine.Width - rightX;

				Span<Argb32> prevSampleRow = default;
				Span<Argb32> prevSrcRow = default;

				if (leftExtent > 0)
				{
					// Potential left overhang
					if (isMovingDown) y--;
					else y++;
					prevSampleRow = sampleImage.Image!.DangerousGetPixelRowMemory(y).Span;
					prevSrcRow = source.Image!.DangerousGetPixelRowMemory(y + sourceOffset.Y).Span;
					CreateLines(prevSampleRow, prevSrcRow, leftX, y, leftExtent, !isMovingDown, false);
				}

				if (rightExtent > 0)
				{
					// Potential right overhang
					if (leftExtent <= 0)
					{
						if (isMovingDown) y--;
						else y++;
						prevSampleRow = sampleImage.Image!.DangerousGetPixelRowMemory(y).Span;
						prevSrcRow = source.Image!.DangerousGetPixelRowMemory(y + sourceOffset.Y).Span;
					}
					CreateLines(prevSampleRow, prevSrcRow, rightX, y, rightExtent, !isMovingDown, false);
				}
			}

			Span<Argb32> sampleRow = sampleImage.Image!.DangerousGetPixelRowMemory(startingPos.Y).Span;
			Span<Argb32> srcRow = source.Image!.DangerousGetPixelRowMemory(startingPos.Y + sourceOffset.Y).Span;

			// Push first lines
			Stack<FillLine> lines = new();
			CreateLines(sampleRow, srcRow, startingPos.X, startingPos.Y, -1, false);
			FillLine line, newLine;
			for (int i = 0; i < createdLines.Count; i++)
			{
				line = createdLines[i];
				line.IsMovingDown = true;
				lines.Push(line);
				line.IsMovingDown = false;
				lines.Push(line);
			}

			int yieldCheck = 1;
			while (lines.Count != 0)
			{
				// Check for yield
				yieldCheck--;
				if (yieldCheck < 1)
				{
					Program.ActiveInstance.Changed();
					yieldCheck = 100;
					if (JobQueue.ShouldYield) yield return true;
				}

				line = lines.Pop();
				int y;
				if (line.IsMovingDown)
				{
					// Move down
					y = line.Y + 1;
					if (y >= sampleImage.Height) continue;
				}
				else
				{
					// Move up
					y = line.Y - 1;
					if (y < 0) continue;
				}

				sampleRow = sampleImage.Image!.DangerousGetPixelRowMemory(y).Span;
				srcRow = source.Image!.DangerousGetPixelRowMemory(y + sourceOffset.Y).Span;
				int lineRight = line.X + line.Width;
				CreateLines(sampleRow, srcRow, line.X, y, line.Width, line.IsMovingDown);
				for (int i = 0; i < createdLines.Count; i++)
					lines.Push(createdLines[i]);
			}
		}

		private void FillNonContiguous(PositionedImage<Argb32> sampleImage, PositionedImage<Argb32> source, Argb32 baseColor)
		{
			if (source.Image == null || sampleImage.Image == null) return;

			int2 sourceOffset = sampleImage.X - source.X;
			double threshold = 260100.0 * _threshold * _threshold;
			Argb32 color = Color.ToPixel<Argb32>();
			byte iAlpha = (byte)(byte.MaxValue - color.A);
			LimitedParallel.For(0, sampleImage.Height, y =>
			{
				var srcRow = source.Image.DangerousGetPixelRowMemory(y + sourceOffset.Y).Span;
				var sampleRow = sampleImage.Image.DangerousGetPixelRowMemory(y).Span;

				for (int x = 0; x < sampleImage.Width; x++)
				{
					var samplePixel = sampleRow[x];

					int r = samplePixel.R - baseColor.R;
					int g = samplePixel.G - baseColor.G;
					int b = samplePixel.B - baseColor.B;
					int a = samplePixel.A - baseColor.A;
					if (r * r + g * g + b * b + a * a > threshold) continue; // Difference over threshold

					ref var srcPixel = ref srcRow[x + sourceOffset.X];

					// Apply change
					if (_replacePixels) srcPixel = color;
					else if (_erase)
					{
						// Erase
						srcPixel.A = (byte)(srcPixel.A * iAlpha / byte.MaxValue);
					}
					else
					{
						// Blend colors
						srcPixel = Util.Blend(srcPixel, color);
					}
				}
			});
		}
		#endregion
	}
}
