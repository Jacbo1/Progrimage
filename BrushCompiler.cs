using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using Image = SixLabors.ImageSharp.Image;

namespace Progrimage
{
    internal class BrushCompiler
	{
		public volatile int Progress;
		private volatile float[] _pixels;
		private double _stepSize = Defs.BRUSH_STROKE_STEP;
		private int2 _imageSize;
		private int _angleSteps;
		private string _fileName;

		#region Constructor
		public BrushCompiler(float[] image, int2 size, double scale, int angleSteps, string fileName)
		{
			_pixels = image;
			_imageSize = size;
			_angleSteps = angleSteps;
			_fileName = fileName;
			if (scale != 1) Scale(scale);
		}
		#endregion

		#region Public Static Methods
		public static BrushCompiler? Compile(Image image, double scale, int angleSteps, string fileName)
		{
			float[]? pixels = image switch
			{
				Image<L8> L8 => ToArray(L8),
				Image<L16> L16 => ToArray(L16),
				Image<Argb32> Argb32 => ToArray(Argb32),
				_ => null
			};
			if (pixels is null) return null; // Unsupported format

			BrushCompiler compiler = new(pixels, new int2(image.Width, image.Height), scale, angleSteps, fileName);
			var task = Task.Factory.StartNew(compiler.Run);
			task.Wait();
			return compiler;
		}
		#endregion

		#region Private Methods
		private void Run()
		{
			Console.WriteLine($"Started compiling \"{_fileName}\"");
			// Draw brush strokes at different angles to a canvas and find the minimum
			// accumulation across them to use as a normalizing value
			object lockObject = new object();
			Progress = 0;
			float[] minValues = new float[_imageSize.x * _imageSize.y];
			for (int i = 0; i < minValues.Length; i++)
				minValues[i] = float.MaxValue;
			double cornerAng = _imageSize.GetAngle();
			double2 halfSize = _imageSize * 0.5;
			int stepsCompleted = 0;
			Parallel.For(0, _angleSteps, angStep =>
			{
				float[] canvas = new float[_imageSize.x * _imageSize.y];
				double angle = Math.PI * 2 * angStep / _angleSteps - Math.PI;
				double2 dir = double2.FromAngle(angle);
				double mult;
				if (-cornerAng <= angle && angle <= cornerAng) mult = _imageSize.x / dir.x;
				else if (Math.PI - cornerAng <= angle || angle <= cornerAng - Math.PI) mult = -_imageSize.x / dir.x;
				else if (cornerAng <= angle && angle <= Math.PI - cornerAng) mult = _imageSize.y / dir.y;
				else mult = -_imageSize.y / dir.y;

				double2 start = halfSize + dir * mult;
				double2 end = halfSize + dir * -mult;
				double lerpStep = _stepSize / Math.Abs(mult * 2);

				start -= halfSize;
				end -= halfSize;

				for (double lerp = lerpStep; lerp < 1; lerp += lerpStep)
				{
					DrawBrushAt(canvas, start * (1 - lerp) + end * lerp);
				}

				lock (lockObject)
				{
					for (int i = 0; i < canvas.Length; i++)
					{
						if (canvas[i] != 0) minValues[i] = Math.Min(minValues[i], canvas[i]);
						canvas[i] = 0;
					}
					stepsCompleted++;
				}

				const int PROG_STEP = 5;
				int prog = (100 / PROG_STEP) * stepsCompleted / _angleSteps * PROG_STEP;
				if (Progress != prog)
				{
					Progress = prog;
					Console.WriteLine(prog + "%");
				}
			});

			using FileStream stream = File.Open(@"Assets\Textures\Brushes\" + _fileName + ".norm", FileMode.Create);
			using var zipStream = new GZipStream(stream, CompressionLevel.SmallestSize);
			zipStream.Write(BitConverter.GetBytes(_imageSize.x), 0, 4);
			zipStream.Write(BitConverter.GetBytes(_imageSize.y), 0, 4);
			for (int i = 0; i < minValues.Length; i++)
			{
				zipStream.Write(BitConverter.GetBytes((minValues[i] == 0 || minValues[i] == float.MaxValue) ? 1f : _pixels[i] / minValues[i]), 0, 4);
			}
			stream.Flush();
			Console.WriteLine($"Finished compiling \"{_fileName}\"");
		}

		private void DrawBrushAt(float[] maxValues, double2 pos)
		{
			int2 ipos = Math2.Round(pos);
			double2 posFrac = ipos - pos;
			double xAdd = posFrac.x - ipos.x;

			int2 min = Math2.Max(ipos - 1, 0);
			int2 max = Math2.Min(min + _imageSize, _imageSize - 1);

			for (int y = min.y; y <= max.y; y++)
			{
				int yw = y * _imageSize.x;
				double yPixel = y - ipos.y + posFrac.y;

				for (int x = min.x; x <= max.x; x++)
					maxValues[x + yw] += GetBrushPixel(x + xAdd, yPixel);
			}
		}

		private float GetBrushPixel(double x, double y)
		{
			// Corner pixel coordinates
			int x1 = (int)Math.Floor(x);
			int x2 = (int)Math.Ceiling(x);
			int y1 = (int)Math.Floor(y);
			int y2 = (int)Math.Ceiling(y);

			// Coordinates out of bounds
			bool x1_0 = x1 < 0 || x1 >= _imageSize.x;
			bool x2_0 = x2 < 0 || x2 >= _imageSize.x;
			bool y1_0 = y1 < 0 || y1 >= _imageSize.y;
			bool y2_0 = y2 < 0 || y2 >= _imageSize.y;

			// Get corner pixel values
			float pixel00 = (x1_0 || y1_0) ? 0f : _pixels[x1 + y1 * _imageSize.x];
			float pixel01 = (x1_0 || y2_0) ? 0f : _pixels[x1 + y2 * _imageSize.x];
			float pixel10 = (x2_0 || y1_0) ? 0f : _pixels[x2 + y1 * _imageSize.x];
			float pixel11 = (x2_0 || y2_0) ? 0f : _pixels[x2 + y2 * _imageSize.x];

			double xFrac = x % 1;
			double yFrac = y % 1;

			if (xFrac < 0) xFrac++;
			if (yFrac < 0) yFrac++;

			// Interpolate
			double top = pixel00 * (1 - xFrac) + pixel10 * xFrac;
			double bottom = pixel01 * (1 - xFrac) + pixel11 * xFrac;
			return (float)(top * (1 - yFrac) + bottom * yFrac);
		}

		private void Scale(double scale)
		{
			// Scale to fit in the new size but maintain aspect ratio
			int2 newSize = Math2.Round(this._imageSize * scale);
			double2 scale_ = newSize / (double2)this._imageSize;
			float[] scaledPixels = new float[newSize.x * newSize.y];

			// Downscale with a modified Box algorithm that treats edge pixels as non-full pixels for fractional coordinates
			// Upscale with bilinear sampling
			// Downscaling and upscaling are separated on each axis
			double2 boxSize1 = 1 / scale_ - 1;
			int2 unscaledSize1 = _imageSize - 1;

			Parallel.For(0, newSize.y, y =>
			{
				int yw = y * newSize.x;
				for (int x = 0; x < newSize.x; x++)
				{
					double xd = x / scale_.x; // x in the original image's scale
					double yd = y / scale_.y; // y in the original image's scale
					int x1 = Math.Min((int)Math.Floor(xd), unscaledSize1.x); // Left bound floored
					int y1 = Math.Min((int)Math.Floor(yd), unscaledSize1.y); // Top bound floord
					int x2, y2;
					double right, bottom;

					if (scale_.x > 1)
					{
						// Scale x up
						x2 = Math.Min((int)Math.Ceiling(xd), unscaledSize1.x); // Right bound ceiled
						right = xd == x2 ? 1 : (1 - x2 + xd); // Right color multiplier
					}
					else
					{
						// Scale x down
						double x_ = xd + boxSize1.x;
						x2 = Math.Min((int)Math.Ceiling(x_), unscaledSize1.x); // Right bound ceiled
						right = x_ == x2 ? 1 : (1 - x2 + x_); // Right color multiplier
					}

					if (scale_.y > 1)
					{
						// Scale y up
						y2 = Math.Min((int)Math.Ceiling(yd), unscaledSize1.y); // Bottom bound ceiled
						bottom = yd == y2 ? 1 : (1 - y2 + yd); // Bottom color multiplier
					}
					else
					{
						// Scale y down
						double y_ = yd + boxSize1.y;
						y2 = Math.Min((int)Math.Ceiling(y_), unscaledSize1.y); // Bottom bound ceiled
						bottom = y_ == y2 ? 1 : (1 - y2 + y_); // Bottom color multiplier
					}

					double left = xd == x1 ? 1 : (1 - xd + x1); // Left color multiplier
					double top = yd == y1 ? 1 : (1 - yd + y1); // Top color multiplier

					// Bilinear sample on the corners
					double pixel = _pixels[x1 + y1 * _imageSize.x] * left * top + // Top left pixel
						_pixels[x1 + y2 * _imageSize.x] * left * bottom +         // Bottom left pixel
						_pixels[x2 + y1 * _imageSize.x] * right * top +           // Top right pixel
						_pixels[x2 + y2 * _imageSize.x] * right * bottom;         // Bottom right pixel
					double weight = left * top + left * bottom + right * top + right * bottom;          // Sum up the weights

					// Box sample for downscaling
					// Sample full insides
					for (int y_ = y1 + 1; y_ < y2; y_++)
					{
						int yw_ = y_ * _imageSize.x;
						for (int x_ = x1 + 1; x_ < x2; x_++)
						{
							pixel += _pixels[x_ + yw_];
						}
					}
					weight += Math.Max(x2 - x1 - 1, 0) * Math.Max(y2 - y1 - 1, 0);

					// Sample top and bottom edge
					for (int x_ = x1 + 1; x_ < x2; x_++)
					{
						pixel += _pixels[x_ + y1 * _imageSize.x] * top +
							_pixels[x_ + y2 * _imageSize.x] * bottom;
					}
					weight += Math.Max(x2 - x1 - 1, 0) * (top + bottom);

					// Sample left and right edge
					for (int y_ = y1 + 1; y_ < y2; y_++)
					{
						pixel += _pixels[x1 + y_ * _imageSize.x] * left +
							_pixels[x2 + y_ * _imageSize.x] * right;
					}
					weight += Math.Max(y2 - y1 - 1, 0) * (left + right);

					scaledPixels[x + yw] = (float)Math.Min(Math.Max(pixel / weight, 0), 1);
				}
			});

			_imageSize = newSize;
			_pixels = scaledPixels;
		}
		#endregion

		#region Private Static Methods
		private static float[] ToArray(Image<L8> image)
		{
			// Copy image to array
			float[] pixels = new float[image.Width * image.Height];
			for (int y = 0; y < image.Height; y++)
			{
				int yw = y * image.Width;
				Span<L8> row = image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < row.Length; x++)
					pixels[x + yw] = row[x].PackedValue / 255f;
			}
			return pixels;
		}

		private static float[] ToArray(Image<L16> image)
		{
			// Copy image to array
			float[] pixels = new float[image.Width * image.Height];
			for (int y = 0; y < image.Height; y++)
			{
				int yw = y * image.Width;
				Span<L16> row = image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < row.Length; x++)
					pixels[x + yw] = row[x].PackedValue / 65535f;
			}
			return pixels;
		}

		private static float[] ToArray(Image<Argb32> image)
		{
			// Copy image to array
			float[] pixels = new float[image.Width * image.Height];
			for (int y = 0; y < image.Height; y++)
			{
				int yw = y * image.Width;
				Span<Argb32> row = image.DangerousGetPixelRowMemory(y).Span;
				for (int x = 0; x < row.Length; x++)
				{
					Argb32 pixel = row[x];
					pixels[x + yw] = (pixel.R + pixel.G + pixel.B) / 765f;
				}
			}
			return pixels;
		}
		#endregion
	}
}
