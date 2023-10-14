using ImageSharpExtensions;
using NewMath;
using Progrimage.CoroutineUtils;
using SixLabors.ImageSharp.Advanced;
using System.Numerics;

namespace Progrimage
{
	public class Stroke
    {
        #region Fields
        // Private
        private List<double2> _points = new();
        // Mask is _mask[x + y * width]
        private float[] _mask, _brushTexture, _unscaledBrushTexture, _normTexture, _unscaledNormTexture;
        private int2 _maskMinBound, _maskMaxBound, _targetBrushSize, _brushSize, _unscaledBrushSize, _maskSize, _unscaledNormSize;
        private double2 _lastPointDrawnPos;
        private float _normMult;
        private double _brushStep;
        private bool _wasSinglePoint = true, _wasPencil = false;
        private int _lastPointDrawn, _queuedJobs;
        private BrushPath? _currentBrushPath;
        private BrushState _brushState;
        private Layer? _layer;
        #endregion

        #region Properties
        public int2 Min
        {
            get => _maskMinBound;
        }

        public int2 Max
        {
            get => _maskMaxBound;
        }

        public int2 Size
        {
            get => _maskSize;
        }

        public Layer? Layer
        {
            get => _layer;
            set
            {
                _queuedJobs++;
                JobQueue.Queue.Add(new CoroutineJob(() =>
                {
                    if (_layer == value) return; // Same layer
                    _layer = value;
					_queuedJobs--;
				}));
            }
        }

        public double BrushStep
        {
            get => _brushStep;
            set
            {
                _queuedJobs++;
                JobQueue.Queue.Add(new CoroutineJob(BrushStepChanged(value)));
            }
        }

        public BrushState BrushState
        {
            get => _brushState;
            set
            {
                _queuedJobs++;
                JobQueue.Queue.Add(new CoroutineJob(() =>
                {
                    _brushState = value;
                    BrushStateChanged();
					_queuedJobs--;
				}));
            }
        }
        #endregion

        #region Public Methods
        public void WaitForJobs()
        {
            if (_queuedJobs <= 0) return; // No jobs

            JobQueue.UnlimitedTime = true;
            JobQueue.Work();
            while (_queuedJobs > 0)
            {
                Thread.Sleep(1);
                JobQueue.Work();
            }
			JobQueue.UnlimitedTime = false;
		}

        public void Clear()
        {
            _mask = null;
        }

        public void QueueClear()
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                _mask = null;
				_queuedJobs--;
			}));
        }

        public void QueueDraw(PositionedImage<Argb32> image, bool expand = false)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                Draw(image, expand);
				_queuedJobs--;
			}));
        }

        public void Draw(PositionedImage<Argb32> image, bool expand = false)
        {
            if (expand) image.ExpandToContain(_maskMinBound, _maskSize);
            else if (image.Image is null || !ISEUtils.Overlaps(image.Pos, image.Size, _maskMinBound, _maskSize)) return; // Does not overlap

            int2 imageOffset = Math2.Max(_maskMinBound - image.Pos, 0);
            int2 strokeOffset = Math2.Max(image.Pos - _maskMinBound, 0);
            int2 overlapMin = Math2.Max(image.Pos, _maskMinBound);
            int2 overlapMax = Math2.Min(image.Pos + image.Size - 1, _maskMaxBound);
            int2 overlapSize = overlapMax - overlapMin + 1;
            Vector4 brushColor = _brushState.Color;
            brushColor.X *= 255;
            brushColor.Y *= 255;
            brushColor.Z *= 255;
            Rgb24 solidColor = new Rgb24(
                (byte)Math.Round(brushColor.X, MidpointRounding.AwayFromZero),
                (byte)Math.Round(brushColor.Y, MidpointRounding.AwayFromZero),
                (byte)Math.Round(brushColor.Z, MidpointRounding.AwayFromZero)
            );
            float brushAlpha = _brushState.Color.W;

            // Multithreading this outer loop causes weird glitches in the result
            for (int y = 0; y < overlapSize.y; y++)
            {
                Span<Argb32> row = image.Image!.DangerousGetPixelRowMemory(y + imageOffset.y).Span;
                int yw = (y + strokeOffset.y) * _maskSize.x;

                for (int x = 0; x < overlapSize.x; x++)
                {
                    brushColor.W = Math.Min(_mask[x + strokeOffset.x + yw] * brushAlpha, 1);

                    ref Argb32 pixel = ref row[x + imageOffset.x];

                    if (brushColor.W == 0) continue; // Second color is invisible

                    if (pixel.A == 0)
                    {
                        // Layer pixel is fully transparent
                        pixel.R = solidColor.R;
                        pixel.G = solidColor.G;
                        pixel.B = solidColor.B;
                        pixel.A = (byte)Math.Round(255 * brushColor.W, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        // Layer pixel is semi-transparent or opaque
                        float aAlpha = pixel.A / 255f;
                        float alpha1 = 1 - brushColor.W;
                        float denom = brushColor.W + aAlpha * alpha1;
                        if (denom == 0) return;
                        float alphaMult = aAlpha * alpha1;

                        pixel.R = (byte)Math.Round((brushColor.X * brushColor.W + pixel.R * alphaMult) / denom, MidpointRounding.AwayFromZero);
                        pixel.G = (byte)Math.Round((brushColor.Y * brushColor.W + pixel.G * alphaMult) / denom, MidpointRounding.AwayFromZero);
                        pixel.B = (byte)Math.Round((brushColor.Z * brushColor.W + pixel.B * alphaMult) / denom, MidpointRounding.AwayFromZero);
                        pixel.A = (byte)Math.Round(255 * brushColor.W + pixel.A * alpha1, MidpointRounding.AwayFromZero);
                    }
                }
            }
        }

        public void QueueErase(PositionedImage<Argb32> image)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                Erase(image);
				_queuedJobs--;
			}));
        }

        public void Erase(PositionedImage<Argb32> image)
        {
            if (image.Image is null || !ISEUtils.Overlaps(image.Pos, image.Size, _maskMinBound, _maskSize)) return; // Does not overlap

            int2 imageOffset = Math2.Max(_maskMinBound - image.Pos, 0);
            int2 strokeOffset = Math2.Max(image.Pos - _maskMinBound, 0);
            int2 overlapMin = Math2.Max(image.Pos, _maskMinBound);
            int2 overlapMax = Math2.Min(image.Pos + image.Size - 1, _maskMaxBound);
            int2 overlapSize = overlapMax - overlapMin + 1;
            for (int y = 0; y < overlapSize.y; y++)
            {
                Span<Argb32> row = image.Image.DangerousGetPixelRowMemory(y + imageOffset.y).Span;
                int yw = (y + strokeOffset.y) * _maskSize.x;

                for (int x = 0; x < overlapSize.x; x++)
                {
                    ref var pixel = ref row[x + imageOffset.x];
					float stroke = Math.Max(1 - _mask[x + strokeOffset.x + yw] * _brushState.Color.W, 0);
					pixel.A = (byte)Math.Round(pixel.A * stroke, MidpointRounding.AwayFromZero);
                }
            }
        }

        public void BrushStateChanged()
        {
            if (_brushState.IsPencil)
            {
                if (_wasPencil && _targetBrushSize == _brushState.Size) return;
                SetBrushPencilTexture(_brushState.Size);
                return;
            }

            if (_targetBrushSize == _brushState.Size && !_wasPencil) return;
            _wasPencil = false;
			_targetBrushSize = _brushState.Size;
            _brushTexture = ScaleTexture(_unscaledBrushTexture, _unscaledBrushSize, _targetBrushSize, false, out _brushSize);
            _normTexture = ScaleTexture(_unscaledNormTexture, _unscaledNormSize, _brushSize, true, out int2 _);
            _normMult = (_unscaledNormSize.x / (float)_brushSize.x + _unscaledNormSize.y / (float)_brushSize.y) * 0.5f;
		}

        public void SetBrushTexture(BrushPath path)
        {
            if (path is BrushPath<L8> L8) SetBrushTexture(L8);
            else if (path is BrushPath<L16> L16) SetBrushTexture(L16);
        }

        public void SetBrushTexture(BrushPath<L8> path)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                if (_currentBrushPath is BrushPath<L8> path2 && path2 == path) return; // Same brush
                _unscaledNormTexture = path.LoadNorm(out _unscaledNormSize);
                SetBrushTexture(path.Load());
                _currentBrushPath = path;
				JobQueue.Queue.Add(new CoroutineJob(path.Dispose));
				_queuedJobs--;
			}));
        }

        public void SetBrushTexture(BrushPath<L16> path)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                if (_currentBrushPath is BrushPath<L16> path2 && path2 == path) return; // Same brush
				_unscaledNormTexture = path.LoadNorm(out _unscaledNormSize);
				SetBrushTexture(path.Load());
                _currentBrushPath = path;
				JobQueue.Queue.Add(new CoroutineJob(path.Dispose));
				_queuedJobs--;
			}));
        }

        public void BeginStroke(double2 pos)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(BeginStrokeEnum(pos)));
        }

        public void ContinueStroke(double2 pos)
        {
            _queuedJobs++;
            JobQueue.Queue.Add(new CoroutineJob(ContinueStrokeEnum(pos)));
        }
		#endregion

		#region Private Methods
        private void SetBrushPencilTexture(int size)
        {
            _wasPencil = true;
            if (_brushSize != size) _brushTexture = new float[size * size];
            _targetBrushSize = _brushSize = size;

            int center = size - 1;
            int radiusSqr = size * size;
            for (int y = 0; y < size; y++)
            {
                int yw = y * size;
                for (int x = 0; x < size; x++)
                {
                    int x1 = x * 2 - center;
                    int y1 = y * 2 - center;
                    _brushTexture[x + yw] = (x1 * x1 + y1 * y1 <= radiusSqr) ? 1 : 0;
                }
            }
        }

		private void SetBrushTexture(Image<L8> image)
		{
			_queuedJobs++;
			JobQueue.Queue.Add(new CoroutineJob(() =>
            {
                _unscaledBrushSize = new int2(image.Width, image.Height);
                _unscaledBrushTexture = new float[_unscaledBrushSize.x * _unscaledBrushSize.y];

                // Copy texture to array
                for (int y = 0; y < image.Height; y++)
                {
                    int yw = y * _unscaledBrushSize.x;
                    Span<L8> row = image.DangerousGetPixelRowMemory(y).Span;
                    for (int x = 0; x < row.Length; x++)
                        _unscaledBrushTexture[x + yw] = row[x].PackedValue / 255f;
                }

                _brushTexture = ScaleTexture(_unscaledBrushTexture, _unscaledBrushSize, _targetBrushSize, false, out _brushSize);
                _normTexture = ScaleTexture(_unscaledNormTexture, _unscaledNormSize, _brushSize, true, out int2 _);
				_normMult = (_unscaledNormSize.x / (float)_brushSize.x + _unscaledNormSize.y / (float)_brushSize.y) * 0.5f;
				_currentBrushPath = null;
				_queuedJobs--;
			}));
		}

		private void SetBrushTexture(Image<L16> image)
		{
			_queuedJobs++;
			JobQueue.Queue.Add(new CoroutineJob(() =>
			{
				_unscaledBrushSize = new int2(image.Width, image.Height);
				_unscaledBrushTexture = new float[_unscaledBrushSize.x * _unscaledBrushSize.y];

                // Copy texture to array
                for (int y = 0; y < image.Height; y++)
                {
                    int yw = y * _unscaledBrushSize.x;
                    Span<L16> row = image.DangerousGetPixelRowMemory(y).Span;
                    for (int x = 0; x < row.Length; x++)
                        _unscaledBrushTexture[x + yw] = row[x].PackedValue / 65535f;
                }

				_brushTexture = ScaleTexture(_unscaledBrushTexture, _unscaledBrushSize, _targetBrushSize, false, out _brushSize);
				_normTexture = ScaleTexture(_unscaledNormTexture, _unscaledNormSize, _brushSize, true, out int2 _);
				_normMult = (_unscaledNormSize.x / (float)_brushSize.x + _unscaledNormSize.y / (float)_brushSize.y) * 0.5f;
				_currentBrushPath = null;
				_queuedJobs--;
			}));
		}

		private IEnumerator<bool> BrushStepChanged(double brushStep)
        {
            if (_brushStep == brushStep) yield break; // Same step size
            _brushStep = brushStep;
            var recalc = RecalculateStroke();
            while (recalc.MoveNext()) yield return true;
			_queuedJobs--;
		}

        private IEnumerator<bool> RecalculateStroke()
        {
            RecalculateBounds(out _maskMinBound, out _maskMaxBound);
            _maskSize = _maskMaxBound - _maskMinBound + 1;
            _mask = new float[_maskSize.x * _maskSize.y];
            _lastPointDrawn = -1;

			_queuedJobs++;
			var draw = DrawStroke();
            while (draw.MoveNext()) yield return true;
			_queuedJobs--;
		}

        private IEnumerator<bool> ContinueStrokeEnum(double2 pos)
        {
            if (_points.Count != 0 &&
                (_brushState.IsPencil && (int2)pos == (int2)_lastPointDrawnPos) ||
                (!_brushState.IsPencil && _points[_points.Count - 1].DistanceSqr(pos) < _brushStep * _brushStep))
            {
				// Too close to last point
				_queuedJobs--;
				yield break;
            }

			_points.Add(pos);
            double2 strokeHalfSize = _brushSize * 0.5;
            int2 min = Math2.Min(_maskMinBound, Math2.FloorToInt(pos - strokeHalfSize));
            int2 max = Math2.Max(_maskMaxBound, Math2.CeilingToInt(pos + strokeHalfSize));
            GrowMask(min, max);

			_queuedJobs++;
			var draw = DrawStroke();
            while (draw.MoveNext()) yield return true;
			_queuedJobs--;
		}

        private IEnumerator<bool> BeginStrokeEnum(double2 pos)
        {
            _lastPointDrawn = -1;
            _points.Clear();
            _points.Add(pos);
            double2 strokeHalfSize = _brushSize * 0.5;
            _maskMinBound = Math2.FloorToInt(pos - strokeHalfSize);
            _maskMaxBound = Math2.CeilingToInt(pos + strokeHalfSize);
			_maskSize = _maskMaxBound - _maskMinBound + 1;
            _mask = new float[_maskSize.x * _maskSize.y];

			_queuedJobs++;
			var draw = DrawStroke();
            while (draw.MoveNext()) yield return true;
			_queuedJobs--;
		}

        private IEnumerator<bool> DrawStroke()
        {
            if (_lastPointDrawn + 1 >= _points.Count)
            {
				// No points to draw
				_queuedJobs--;
				yield break;
            }

            double2 brushHalfSize = _brushSize * 0.5;

			if (_points.Count == 1)
            {
                // Draw single point
                double2 point = _points[0];
                if (_brushState.IsPencil) point = (int2)point;
                DrawBrushAsSinglePoint(point - brushHalfSize);
                _wasSinglePoint = true;
				_queuedJobs--;
				yield break;
            }

            if (_wasSinglePoint && _points.Count != 1)
            {
				// Clear mask
                _mask = new float[_maskSize.x * _maskSize.y];
                _lastPointDrawn = 0;
                _wasSinglePoint = false;
            }

            _lastPointDrawn = Math.Max(_lastPointDrawn, 0);

            // Draw stroke
            if (_brushState.IsPencil)
            {
                // Draw as pencil
                for (; _lastPointDrawn + 1 < _points.Count; _lastPointDrawn++)
                {
                    int2 last = (int2)_points[_lastPointDrawn];
                    int2 next = (int2)_points[_lastPointDrawn + 1];

                    bool skipFirst = _lastPointDrawn != 0;
                    if (last.x == next.x)
                    {
                        // Vertical line
                        for (int y = Math.Min(last.y, next.y) + (skipFirst ? 1 : 0); y <= Math.Max(last.y, next.y); y++)
                        {
                            DrawBrushAt(new double2(last.x, y) - brushHalfSize);
                            if (JobQueue.ShouldYield) yield return true;
                        }
                        continue;
                    }

                    if (last.y == next.y)
                    {
                        // Horizontal line
                        for (int x = Math.Min(last.x, next.x) + (skipFirst ? 1 : 0); x <= Math.Max(last.x, next.x); x++)
                        {
                            DrawBrushAt(new double2(x, last.y) - brushHalfSize);
                            if (JobQueue.ShouldYield) yield return true;
                        }
						continue;
					}

                    // Diagonal line
                    double dx = (last.x - next.x) / (double)(last.y - next.y);
                    double dy = (last.y - next.y) / (double)(last.x - next.x);
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        // Go along x
                        if (last.x > next.x) (last, next) = (next, last);

                        for (int x = last.x + (skipFirst ? 1 : 0); x <= next.x; x++)
                        {
                            DrawBrushAt(new double2(x, Math.Round(dy * (x - last.x) + last.y, MidpointRounding.AwayFromZero)) - brushHalfSize);
                            if (JobQueue.ShouldYield) yield return true;
                        }
                    }
                    else
                    {
                        // Go along y
                        if (last.y > next.y) (last, next) = (next, last);

                        for (int y = last.y + (skipFirst ? 1 : 0); y <= next.y; y++)
                        {
                            DrawBrushAt(new double2(Math.Round(dx * (y - last.y) + last.x, MidpointRounding.AwayFromZero), y) - brushHalfSize);
                            if (JobQueue.ShouldYield) yield return true;
                        }
                    }
                }

				_lastPointDrawnPos = _points[_points.Count - 1];

				_queuedJobs--;
                yield break;
            }

            // Draw as smooth stroke
            for (; _lastPointDrawn + 1 < _points.Count; _lastPointDrawn++)
            {
                double2 last = _points[_lastPointDrawn];
                double2 next = _points[_lastPointDrawn + 1];
                double2 delta = next - last;
                double length = delta.Length();
                if (length < _brushStep) continue;
                double step = _brushStep / length;
                double stepShift = 0;

                if (_lastPointDrawn != 0)
                {
                    // Move the start along the line from A to B
                    // Keeping it a fixed distance from the last drawn point
                    double2 deltaLast = _lastPointDrawnPos - last;
                    double2 nDelta = delta / length;
                    double2 perp = new(nDelta.y, -nDelta.x);
                    double perpDist = deltaLast.Dot(perp);
                    stepShift = (Math.Sqrt(_brushStep * _brushStep - perpDist * perpDist) + deltaLast.Dot(nDelta)) / length;
                }

                // Lerp along line segment in fixed distance steps
                double2 nextLast = _lastPointDrawnPos - brushHalfSize;
                last -= brushHalfSize;
                next -= brushHalfSize;
				for (double lerp = stepShift; lerp <= 1; lerp += step)
                {
                    nextLast = last * (1 - lerp) + next * lerp;
                    DrawBrushAt(nextLast);
                    if (JobQueue.ShouldYield) yield return true;
                }

                _lastPointDrawnPos = nextLast + brushHalfSize;
            }

			_queuedJobs--;
		}

		private float[] ScaleTexture(float[] texture, int2 currentSize, int2 targetSize, bool exactSize, out int2 newSize)
        {
            if (targetSize.x < 1 || targetSize.y < 1 || targetSize == currentSize)
            {
                newSize = currentSize;
                return texture;
            }

            // Scale to fit in the new size but maintain aspect ratio
            newSize = targetSize;
            double2 scale;
            if (!exactSize)
            {
                // Scale to fit and maintain aspect ratio
                scale = currentSize / (double2)targetSize;
                if (scale.x > scale.y) newSize.y = (int)Math.Round(currentSize.y / scale.x, MidpointRounding.AwayFromZero);
                else newSize.x = (int)Math.Round(currentSize.x / scale.y, MidpointRounding.AwayFromZero);
            }
            scale = newSize / (double2)currentSize;
            float[] newTexture = new float[newSize.x * newSize.y];

            // Downscale with a modified Box algorithm that treats edge pixels as non-full pixels for fractional coordinates
            // Upscale with bilinear sampling
            // Downscaling and upscaling are separated on each axis
            double2 boxSize1 = 1 / scale - 1;
            int2 unscaledSize1 = currentSize - 1;

            int2 size = newSize; // Can't use out variables in Parallel.For()
			Parallel.For(0, newSize.y, y =>
            {
                int yw = y * size.x;
                for (int x = 0; x < size.x; x++)
                {
                    double xd = x / scale.x; // x in the original image's scale
                    double yd = y / scale.y; // y in the original image's scale
                    int x1 = Math.Min((int)Math.Floor(xd), unscaledSize1.x); // Left bound floored
                    int y1 = Math.Min((int)Math.Floor(yd), unscaledSize1.y); // Top bound floord
                    int x2, y2;
                    double right, bottom;

                    if (scale.x > 1)
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

                    if (scale.y > 1)
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
                    double pixel = texture[x1 + y1 * currentSize.x] * left * top + // Top left pixel
						texture[x1 + y2 * currentSize.x] * left * bottom +         // Bottom left pixel
						texture[x2 + y1 * currentSize.x] * right * top +           // Top right pixel
						texture[x2 + y2 * currentSize.x] * right * bottom;         // Bottom right pixel
                    double weight = left * top + left * bottom + right * top + right * bottom;          // Sum up the weights

                    // Box sample for downscaling
                    // Sample full insides
                    for (int y_ = y1 + 1; y_ < y2; y_++)
                    {
                        int yw_ = y_ * currentSize.x;
                        for (int x_ = x1 + 1; x_ < x2; x_++)
                        {
                            pixel += texture[x_ + yw_];
                        }
                    }
                    weight += Math.Max(x2 - x1 - 1, 0) * Math.Max(y2 - y1 - 1, 0);

                    // Sample top and bottom edge
                    for (int x_ = x1 + 1; x_ < x2; x_++)
                    {
                        pixel += texture[x_ + y1 * currentSize.x] * top +
							texture[x_ + y2 * currentSize.x] * bottom;
                    }
                    weight += Math.Max(x2 - x1 - 1, 0) * (top + bottom);

                    // Sample left and right edge
                    for (int y_ = y1 + 1; y_ < y2; y_++)
                    {
                        pixel += texture[x1 + y_ * currentSize.x] * left +
							texture[x2 + y_ * currentSize.x] * right;
                    }
                    weight += Math.Max(y2 - y1 - 1, 0) * (left + right);

                    newTexture[x + yw] = (float)Math.Min(Math.Max(pixel / weight, 0), 1);
                }
            });

            return newTexture;
        }

        private void DrawBrushAt(double2 pos)
        {
            int2 ipos = Math2.Round(pos);
            double2 posFrac = ipos - pos;
            ipos -= _maskMinBound;
			double xAdd = posFrac.x - ipos.x;

			int2 min = Math2.Max(ipos - 1, 0);
            int2 max = Math2.Min(min + _brushSize, _maskSize - 1);

            for (int y = min.y; y <= max.y; y++)
            {
                int yw = y * _maskSize.x;
                double yPixel = y - ipos.y + posFrac.y;

                for (int x = min.x; x <= max.x; x++)
                {
                    if (_brushState.IsPencil) _mask[x + yw] = Math.Max(_mask[x + yw], GetPixel(x - ipos.x, y - ipos.y));
					else _mask[x + yw] += GetPixel(x + xAdd, yPixel, true);
                }
            }
        }

        private void DrawBrushAsSinglePoint(double2 pos)
        {
			int2 ipos = Math2.Round(pos);
			double2 posFrac = ipos - pos;
			ipos -= _maskMinBound;
            double xAdd = posFrac.x - ipos.x;

			int2 min = Math2.Max(ipos - 1, 0);
			int2 max = Math2.Min(min + _brushSize, _maskSize - 1);

            for (int y = min.y; y <= max.y; y++)
            {
                int yw = y * _maskSize.x;
                double yPixel = y - ipos.y + posFrac.y;

                for (int x = min.x; x <= max.x; x++)
                {
                    if (_brushState.IsPencil) _mask[x + yw] = Math.Max(_mask[x + yw], GetPixel(x - ipos.x, y - ipos.y));
                    else _mask[x + yw] += GetPixel(x + xAdd, yPixel, false);
                }
            }
		}

        private float GetPixel(double x, double y, bool normalize = true)
        {
            if (_brushState.IsPencil)
            {
                int ix = (int)x;
                int iy = (int)y;
                if (ix < 0 || iy < 0 || ix >= _brushSize.x || iy >= _brushSize.y) return 0;
                return _brushTexture[ix + iy * _brushSize.x];
            }

            // Corner pixel coordinates
            int x1 = (int)Math.Floor(x);
            int x2 = (int)Math.Ceiling(x);
            int y1 = (int)Math.Floor(y);
            int y2 = (int)Math.Ceiling(y);

			double xFrac = x % 1;
			double yFrac = y % 1;

			if (xFrac < 0) xFrac++;
			if (yFrac < 0) yFrac++;

			// Coordinates out of bounds
			bool x1_0 = x1 < 0 || x1 >= _brushSize.x;
            bool x2_0 = x2 < 0 || x2 >= _brushSize.x;
            bool y1_0 = y1 < 0 || y1 >= _brushSize.y;
            bool y2_0 = y2 < 0 || y2 >= _brushSize.y;

            // Get corner pixel values
            float pixel00 = (x1_0 || y1_0) ? 0f : _brushTexture[x1 + y1 * _brushSize.x];
            float pixel01 = (x1_0 || y2_0) ? 0f : _brushTexture[x1 + y2 * _brushSize.x];
            float pixel10 = (x2_0 || y1_0) ? 0f : _brushTexture[x2 + y1 * _brushSize.x];
            float pixel11 = (x2_0 || y2_0) ? 0f : _brushTexture[x2 + y2 * _brushSize.x];

            // Interpolate
            double top = pixel00 * (1 - xFrac) + pixel10 * xFrac;
            double bottom = pixel01 * (1 - xFrac) + pixel11 * xFrac;
            double pixel = top * (1 - yFrac) + bottom * yFrac;

            if (!normalize) return (float)pixel;

			// Get normalizer pixel values
            pixel00 = (x1_0 || y1_0) ? 0f : _normTexture[x1 + y1 * _brushSize.x];
            pixel01 = (x1_0 || y2_0) ? 0f : _normTexture[x1 + y2 * _brushSize.x];
            pixel10 = (x2_0 || y1_0) ? 0f : _normTexture[x2 + y1 * _brushSize.x];
            pixel11 = (x2_0 || y2_0) ? 0f : _normTexture[x2 + y2 * _brushSize.x];

            // Interpolate
            top = pixel00 * (1 - xFrac) + pixel10 * xFrac;
            bottom = pixel01 * (1 - xFrac) + pixel11 * xFrac;
            double normPixel = top * (1 - yFrac) + bottom * yFrac;

			return (float)(pixel * normPixel) * _normMult;
        }

        private void RecalculateBounds(out int2 min, out int2 max)
        {
            if (_points.Count == 0)
            {
                // No points
                min = int2.Zero;
                max = int2.Zero;
                return;
            }

            min = int.MaxValue;
            max = int.MinValue;
            double2 strokeHalfSize = _brushSize * 0.5;

            for (int i = 0; i < _points.Count; i++)
            {
                double2 point = _points[i];
                min = Math2.Min(min, Math2.FloorToInt(point - strokeHalfSize));
                max = Math2.Max(max, Math2.CeilingToInt(point + strokeHalfSize));
            }
        }

        private void GrowMask(int2 newMin, int2 newMax)
        {
            if (newMin == _maskMinBound && newMax == _maskMaxBound) return; // Don't need to resize

            int2 newSize = newMax - newMin + 1;
            int2 curOffset = Math2.Max(newMin - _maskMinBound, 0);
            int2 newOffset = Math2.Max(_maskMinBound - newMin, 0);
            int2 overlapMin = Math2.Max(newMin, _maskMinBound);
            int2 overlapMax = Math2.Min(newMax, _maskMaxBound);
            int2 overlapSize = overlapMax - overlapMin + 1;

            float[] newMask = new float[newSize.x * newSize.y];

            // Copy mask data to new mask
            for (int y = 0; y < overlapSize.y; y++)
                Array.Copy(_mask, curOffset.x + (y + curOffset.y) * _maskSize.x, newMask, newOffset.x + (y + newOffset.y) * newSize.x, overlapSize.x);

            _maskMinBound = newMin;
            _maskMaxBound = newMax;
            _maskSize = newSize;
			_mask = newMask;
        }
        #endregion
    }
}
