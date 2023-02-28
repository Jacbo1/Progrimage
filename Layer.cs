using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Progrimage.Utils;
using System.Numerics;
using Progrimage.DrawingShapes;
using Progrimage.Composites;
using Progrimage.CoroutineUtils;
using System.Collections;
using ImageSharpExtensions;

namespace Progrimage
{
    public class Layer : IDisposable
    {
        #region Fields
        // Public
        public PositionedImage<Argb32> Image = new();
        public PositionedImage<Argb32> CompositeResult = new();
        public string Name;
        public TexPair ThumbnailTex;
        public Vector2 ThumbnailSize;
        public bool ProcessingComposites { get; private set; }
        public bool Hidden;
        public List<DrawingShapeCollection> OverlayShapes = new();
        public List<DrawingShapeCollection> RenderOverlayShapes = new();
        public List<Composite>? Composites { get; private set; }
        public readonly JobIdentifier JobIdentifier = new(true);

        // Private static
        private static int _idCounter;

        // Private
        private int _myID;
        private Instance _instance;
        private double2 _brushPos;
        private Image<Argb32> _baseTexture;
        private Image<Rgb24> _thumbnail;
        private bool _shouldUpdateThumbnail = true;
        private PositionedImage<Argb32> _brushStroke = new();
		#endregion

		#region Properties
		public int2 Pos
        {
            get => Image.Pos;
            set => Image.Pos = value;
		}

		public int2 Size
		{
			get => Image.Size;
			set => Image.Size = value;
		}

		public int X
		{
			get => Image.X;
			set => Image.X = value;
		}

		public int Y
		{
			get => Image.Y;
			set => Image.Y = value;
		}

		public int Width
		{
			get => Image.Width;
		}

		public int Height
		{
			get => Image.Height;
		}
		#endregion

		#region Constructor
		public Layer(Instance myInstance, Image<Argb32>? src = null)
        {
            _myID = _idCounter;
            _idCounter++;
            _instance = myInstance;

            Name = "Layer " + _myID;

            Image.Image = src;
            Pos = (myInstance.CanvasSize - Size) / 2;
            Image.ExpandAction = TextureExpanded;
			Image.ImageCreatedAction = TextureCreated;

			UpdateThumbnail();
        }

        public Layer(Instance myInstance, int2 size)
        {
            _myID = _idCounter;
            _idCounter++;
            Name = "Layer " + _myID;
            _instance = myInstance;
            Size = size;
            Pos = (myInstance.CanvasSize - Size) / 2;
            UpdateThumbnail();
			Image.ExpandAction = TextureExpanded;
			Image.ImageCreatedAction = TextureCreated;
		}
        #endregion

        #region Public Methods
        public void BrushDown(double2 pos)
        {
            _brushPos = pos;
            DrawBrushLine(pos, pos);
        }

        public void MoveBrush(double2 pos)
        {
            if (pos.DistanceSqr(_brushPos) < Defs.BRUSH_STROKE_STEP * Defs.BRUSH_STROKE_STEP) return;
            DrawBrushLine(_brushPos, pos);
            _brushPos = pos;
        }

        public void DrawBrushLine(double2 start, double2 stop)
        {
            JobIdentifier.Cancel();
            CompositeResult.Dispose();
        }

        public void Changed()
        {
            ProcessingComposites = false;
            _shouldUpdateThumbnail = true;
            _instance.Changed = true;

            if (Composites is null) return;
            JobIdentifier.Cancel();
            JobQueue.Queue.Add(new CoroutineJob(ChangedEnum(), JobIdentifier, DisposeComposites, CompositesFirstDuplicateRun));
        }

        private void DisposeComposites()
        {
            if (Composites is null) return;
            foreach (Composite comp in Composites!)
                comp.CompositeAction.DisposalDelegate?.Invoke();
            _instance.Changed = true;
            ProcessingComposites = false;
        }

        private void CompositesFirstDuplicateRun()
        {
            foreach (Composite comp in Composites!)
                comp.CompositeAction.RunOnceFirstRepeat(CompositeResult);
            _instance.Changed = true;
        }

        private IEnumerator ChangedEnum()
        {
            ProcessingComposites = true;
            CompositeResult.Dispose();
            CompositeResult.Image = Image.Image?.Clone();
            CompositeResult.Pos = int2.Zero;

            if (Composites is not null)
            {

                foreach (Composite comp in Composites!)
                {
                    if (comp.Hidden) continue;
                    var enumerator = comp.Update();
                    while (enumerator.MoveNext()) yield return true;
                }
            }

            ProcessingComposites = false;
            _instance.Changed = true;
        }

        /// <summary>
        /// Updates the thumbnail in the layer list if it is appropriate.
        /// </summary>
        public void UpdateThumbnail()
        {
            if (!_shouldUpdateThumbnail) return;
            _shouldUpdateThumbnail = false;

            int2 maxSize = (int2)(MainWindow.LayerThumbnailSize * MainWindow.UIScale);
            int2 thumbnailSize = Util.ScaleToFit(Math2.Max(Size, 1), maxSize, true);
            ThumbnailSize = thumbnailSize;
            ThumbnailTex.Size = thumbnailSize;

            _thumbnail?.Dispose();
            _thumbnail = Util.GetTransparencyChecker(thumbnailSize.x, thumbnailSize.y, (int)(4 * MainWindow.UIScale));
            
            if (Image.Image is not null)
            {
                using var img = Image.Image.Clone();
                img.Mutate(x => x.Resize(thumbnailSize.x, thumbnailSize.y));
                _thumbnail.DrawOver(img, int2.Zero);
            }

            Util.DrawImageToTexture2D(ThumbnailTex!, _thumbnail);
        }

        public void Dispose()
        {
			Image.Dispose();
            _brushStroke.Dispose();
            CompositeResult.Dispose();
            _baseTexture?.Dispose();
            _baseTexture = null;
            _thumbnail?.Dispose();
            _thumbnail = null;
            ThumbnailTex.Dispose();
            _instance.LayerManager.Remove(this);
            _instance.Changed = true;
        }

        public void AddComposite(Composite comp)
        {
            Composites ??= new List<Composite>();
            Composites.Add(comp);
            Changed();
        }

        public void RemoveComposite(Composite comp)
        {
            if (Composites is null || !Composites.Remove(comp)) return;
            comp.Dispose();
            if (Composites.Count == 0)
            {
                Composites = null;
                CompositeResult.Dispose();
            }
            Changed();
        }

        public void ApplyComposites()
        {
            if (Composites is null || CompositeResult.Image is null) return;

            JobQueue.Queue.Add(new CoroutineJob(() =>
            {
				Image.Dispose();
				Image.Image = CompositeResult.Image.Clone();
				Pos += CompositeResult.Pos;
				CompositeResult.Dispose();
				while (Composites is not null && Composites.Count != 0) Composites[0].Dispose();
                Changed();
            }));
        }
		#endregion

		#region Private Methods
		public void TextureExpanded(int2 deltaPos)
		{
			if (deltaPos == int2.Zero) return;

			if (Composites is not null)
			{
				for (int i = 0; i < Composites.Count; i++)
				{
                    Composites[i].CompositeAction.Pos += deltaPos;
                }
			}

			if (OverlayShapes is not null)
			{
				for (int i = 0; i < OverlayShapes.Count; i++)
				{
					if (OverlayShapes[i].AttachedToLayer)
						OverlayShapes[i].Pos += deltaPos;
				}
			}

			if (RenderOverlayShapes is not null)
			{
				for (int i = 0; i < RenderOverlayShapes.Count; i++)
				{
					if (RenderOverlayShapes[i].AttachedToLayer)
						RenderOverlayShapes[i].Pos += deltaPos;
				}
			}
		}

		private void TextureCreated()
		{
            if (Composites is not null)
			{
				for (int i = 0; i < Composites.Count; i++)
				{
					Composites[i].CompositeAction.Pos -= Image.Pos;
				}
			}

			if (OverlayShapes is not null)
			{
				for (int i = 0; i < OverlayShapes.Count; i++)
				{
					if (OverlayShapes[i].AttachedToLayer)
						OverlayShapes[i].Pos -= Image.Pos;
				}
			}
            
			if (RenderOverlayShapes is not null)
			{
				for (int i = 0; i < RenderOverlayShapes.Count; i++)
				{
					if (RenderOverlayShapes[i].AttachedToLayer)
						RenderOverlayShapes[i].Pos -= Image.Pos;
				}
			}
		}

		/// <summary>
		/// Overlays the color onto the pixel
		/// </summary>
		/// <param name="src"></param>
		/// <param name="baseTex"></param>
		/// <param name="maskAlpha"></param>
		/// <param name="color"></param>
		/// <param name="maxValue"></param>
		private static void BlendPixel(ref Argb32 src, Argb32 baseTex, ushort maskAlpha, Vector4 color, ushort maxValue)
        {
            double colorAlpha = maskAlpha * (double)color.W / maxValue;
            double srcAlpha = baseTex.A / 255.0;
            double alpha1 = 1 - colorAlpha;
            double denom = colorAlpha + srcAlpha * alpha1;
            if (denom == 0) return;

            colorAlpha *= 255;
            double alphaMult = srcAlpha * alpha1;
            denom = 1.0 / denom;
            src.R = (byte)Math.Round((color.X * colorAlpha + baseTex.R * alphaMult) * denom, MidpointRounding.AwayFromZero);
            src.G = (byte)Math.Round((color.Y * colorAlpha + baseTex.G * alphaMult) * denom, MidpointRounding.AwayFromZero);
            src.B = (byte)Math.Round((color.Z * colorAlpha + baseTex.B * alphaMult) * denom, MidpointRounding.AwayFromZero);
            src.A = (byte)Math.Round(colorAlpha + baseTex.A * alpha1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Overlays the pixel onto the color
        /// </summary>
        /// <param name="src"></param>
        /// <param name="baseTex"></param>
        /// <param name="baseAlphaMult"></param>
        /// <param name="color"></param>
        private static void BlendPixelReverse(ref Argb32 src, Argb32 baseTex, double mask, Vector4 color)
        {
            double srcAlpha255 = baseTex.A * (1 - mask);
            double srcAlpha = srcAlpha255 / 255.0;
            double colorAlpha = color.W * mask;
            double alpha1 = 1 - srcAlpha;
            double denom = srcAlpha + colorAlpha * alpha1;
            if (denom == 0) return;

            colorAlpha *= 255;
            double alphaMult = colorAlpha * alpha1;
            denom = 1.0 / denom;
            src.R = (byte)Math.Round((color.X * alphaMult + baseTex.R * srcAlpha) * denom, MidpointRounding.AwayFromZero);
            src.G = (byte)Math.Round((color.Y * alphaMult + baseTex.G * srcAlpha) * denom, MidpointRounding.AwayFromZero);
            src.B = (byte)Math.Round((color.Z * alphaMult + baseTex.B * srcAlpha) * denom, MidpointRounding.AwayFromZero);
            src.A = (byte)Math.Round(colorAlpha * alpha1 + srcAlpha255, MidpointRounding.AwayFromZero);
        }
        #endregion
    }
}
