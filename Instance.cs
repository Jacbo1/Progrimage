using ImageSharpExtensions;
using NewMath;
using Progrimage.Composites;
using Progrimage.DrawingShapes;
using Progrimage.LuaDefs;
using Progrimage.Selectors;
using Progrimage.Tools;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using Color = SixLabors.ImageSharp.Color;

namespace Progrimage
{
    public enum BrushMode
    {
        Brush,
        Pencil,
        Eraser
    }

    #region Structs
    //public struct Selection
    //{
    //    public ISelector Tool;

    //    public Selection(ISelector tool)
    //    {
    //        Tool = tool;
    //    }
    //}
    #endregion

    public class Instance
    {
        #region Fields
        // Public fields
        public LayerManager LayerManager;
        public string? LastSavePath, LastLoadPath;
        public Dictionary<string, ITool> Tools = new();
        public int MaxBrushSize = 500;
        public BrushMode BrushMode = BrushMode.Brush;
        public List<BrushPath> BrushTextures = new()
        {
            new BrushPath<L16>(@"Assets\Textures\Brushes\brush_regular.png"),
			new BrushPath<L16>(@"Assets\Textures\Brushes\brush_distance.png"),
			new BrushPath<L8>(@"Assets\Textures\Brushes\brush_circle.png")
        };
        public BrushState BrushSettings;
        public BrushState EraserSettings;
        public ISelector? Selection = null;
        private bool _changed = true, _overlayChanged;
        public List<ToolLua> LuaTools = new();
        public ITool[] DefaultTools = new ITool[]
        {
            new ToolBrush(),
            new ToolEraser(),
			new ToolFill(),
			new ToolMove(),
            new ToolMarqueSelect(),
            new ToolRect(),
            new ToolOval(),
            new ToolLine(),
            new ToolQuadraticCurve(),
            new ToolCubicCurve(),
            new ToolText(),
            new ToolCrop()
        };
        public ToolCreateScript ToolCreateScript = new();
        public LuaLayer? ActiveLuaLayer { get; private set; }
        private Composite? _activeComposite;
        public double2 RT_pixMin;
        public Stroke Stroke = new();
        public TextInputHandler TextInputHandler = new();

		// Private fields
		private Layer? _activeLayer;
        private ITool _activeTool;
        private int2 _canvasSize, _renderOffset, _renderSize;
        private Image<Argb32> _transparencyBg, _renderedImage;
        private double2 _pos;
        private double _zoomLerp, _zoom = 1;
        private bool _firstUpdate = true;
        private Queue<IShape> _overlayShapes = new();
        private int _checkerSize = 8;
		#endregion

		#region Properties
        public Composite? ActiveComposite
        {
            get => _activeComposite;
            set
            {
                if (value is null && _activeComposite?.CompositeAction is CompText && (_activeTool as ToolText)?.CompText == _activeComposite.CompositeAction)
                    ((ToolText)_activeTool).SetCompText(null);
                _activeComposite = value;
                if (value?.CompositeAction is not CompText compText || (ActiveTool as ToolText)?.CompText == compText) return;
                ToolText textTool = GetTool<ToolText>()!;
                ActiveTool = textTool;
                textTool.SetCompText(compText);
			}
        }

		public Layer? ActiveLayer
        {
            get => _activeLayer;
            set
            {
                if (_activeLayer is Layer layerHidden)
                    _activeTool?.OnLayerDeselect(layerHidden);
                _activeLayer = value;
                if (_activeLayer is null) ActiveLuaLayer = null;
                else
                {
                    ActiveLuaLayer = new LuaLayer(_activeLayer!);
                    _activeTool?.OnLayerSelect(_activeLayer);
                }
            }
        }

        public ITool ActiveTool
        {
            get => _activeTool;
            set
            {
                if (value is ToolCreateScript)
                {
                    // Shouldn't set this as the active tool
                    value.OnSelect(this);
                    return;
                }

                if (_activeLayer is not null) _activeTool?.OnLayerDeselect(_activeLayer);
                _activeTool?.OnDeselect();
                _activeTool = value;
                if (_activeTool == null) return;
                _activeTool.OnSelect(this);
                if (_activeLayer is Layer layerHidden)
                    _activeTool.OnLayerSelect(layerHidden);
            }
        }

        public int2 CanvasSize
        {
            get => _canvasSize;
            set
            {
                if (_canvasSize == value) return;

                _canvasSize = value;
                _transparencyBg?.Dispose();
                _checkerSize = (int)(8 * MainWindow.UIScale);
				_transparencyBg = Util.GetTransparencyChecker(_canvasSize.x + _checkerSize * 2, _canvasSize.y + _checkerSize * 2, _checkerSize).CloneAs<Argb32>();
                Zoom = _zoom;
				_changed = true;
            }
        }

        public double2 Pos
        {
            get => _pos;
            set
            {
                _pos = value;
                _changed = true;
            }
        }

        public double Zoom
        {
            get => _zoom;
            set
            {
                _zoom = value;
                _changed = true;

                GetZoomBounds(out _, out double minZoom, out double maxZoom);
                double minZoomLog = Math.Log(minZoom);
                _zoomLerp = (Math.Log(_zoom) - minZoomLog) / (Math.Log(maxZoom) - minZoomLog);
            }
        }

        public double ZoomLerp
        {
            get => _zoomLerp;
            set
            {
                _zoomLerp = value;
                GetZoomBounds(out _, out double minZoom, out double maxZoom);
                _zoom = Math.Exp(Math2.Lerp(Math.Log(minZoom), Math.Log(maxZoom), _zoomLerp));
                _changed = true;
            }
        }
        #endregion

        #region Constructors
        public Instance(int2 canvasSize)
        {
            Stroke.BrushStep = Defs.BRUSH_STROKE_STEP;
            BrushSettings = new BrushState(BrushTextures[0], 10, new Vector4(1, 0, 0, 1), false);
            EraserSettings = new BrushState(BrushTextures[0], 20, new Vector4(1, 1, 1, 1), false);
            CanvasSize = canvasSize;
            LayerManager = new LayerManager(this);
            ActiveTool = DefaultTools[0];
        }
        #endregion

        #region Public Methods
        public void Changed()
        {
            _changed = true;
        }

        public void OverlayChanged()
        {
            _overlayChanged = true;
        }

        public void Init()
        {
            if (!_firstUpdate) return;
            _firstUpdate = false;

            int2 maxSize = Math2.Max(MainWindow.CanvasMax - MainWindow.CanvasMin + 1, 1);
            if (CanvasSize > maxSize)
            {
                // Set initial zoom
                double ratio = Math.Min(maxSize.x / (double)CanvasSize.x, maxSize.y / (double)CanvasSize.y);
                Zoom = ratio * 0.85;
            }
            else Zoom = _zoom;
        }

        public ToolType? GetTool<ToolType>() where ToolType : ITool
		{
            for (int i = 0; i < DefaultTools.Length; i++)
            {
                if (DefaultTools[i] is ToolType tool)
                    return tool;
            }
            return default;
        }

        public Queue<IInteractable> GetInteractables()
        {
            Queue<IInteractable> queue = new();
            if (_activeTool != null) queue.Enqueue(_activeTool);
            return queue;
        }

        public Queue<IUsesToolbar> GetToolbarUsers()
        {
            Queue<IUsesToolbar> queue = new();
			if (_activeTool != null) queue.Enqueue(_activeTool);
            if (ActiveComposite is not null) queue.Enqueue(ActiveComposite);
            return queue;
        }

        public Layer CreateLayer(Image<Argb32> img)
        {
			if (LayerManager.Layers.Count == 0) CanvasSize = new int2(img.Width, img.Height);
			else if (LayerManager.Layers.Count == 1 && LayerManager.Layers[0].Image.Image is null)
            {
                LayerManager.Layers[0].Dispose();
                CanvasSize = new int2(img.Width, img.Height);
            }

            Layer layer = new Layer(this, img);
            LayerManager.Add(layer);
            ActiveLayer = layer;
            _changed = true;
            return layer;
        }

        public Layer CreateLayer(PositionedImage<Argb32> img)
        {
            Layer layer = CreateLayer(img.Image);
            layer.Pos = img.Pos;
            return layer;
        }

        public Layer CreateLayer()
        {
            Layer layer = new Layer(this);
            LayerManager.Add(layer);
            ActiveLayer = layer;
            _changed = true;
            return layer;
        }

        public void Draw(IShape shape)
        {
            _overlayShapes.Enqueue(shape);
            _overlayChanged = true;
        }

        public void RenderToTexture2D(ref TexPair tex, out int2 offset, out int2 size)
        {
            offset = _renderOffset;
            size = _renderSize;
            if (!_changed && !_overlayChanged) return; // Only re-render if there have been changes
            
            bool offscreen = false;
            if (_changed)
            {
                using (Image<Argb32> img = LayerManager.Merge()!)
                {
                    //Draw layer overlay shapes
                    img.Mutate(op =>
                    {
                        var layers = LayerManager.Layers;
                        for (int i = 0; i < layers.Count; i++)
                        {
                            var overlays = layers[i].OverlayShapes;
                            for (int j = 0; j < overlays.Count; j++)
                            {
                                overlays[j].Draw(op);
                            }
                        }
                    });

                    int2 checkerOffset = Math2.RoundToInt(MainWindow.CanvasOrigin / _zoom) % (_checkerSize * 2);
                    if (checkerOffset.x < 0) checkerOffset.x += _checkerSize * 2;
                    if (checkerOffset.y < 0) checkerOffset.y += _checkerSize * 2;
                    _renderedImage?.Dispose();
                    _renderedImage = _transparencyBg.GetSubimage(checkerOffset, _canvasSize);
                    _renderedImage.DrawOver(img, int2.Zero);
                }

				// Crop to screen bounds
				int2 min = Math2.Max(MainWindow.CanvasMin, MainWindow.CanvasOrigin);
				int2 max = Math2.Min(MainWindow.CanvasMax, MainWindow.CanvasOrigin + Math2.RoundToInt(_canvasSize * _zoom) - 1);
				int2 pixMinScaled = min - MainWindow.CanvasOrigin;
				int2 pixMaxScaled = max - MainWindow.CanvasOrigin;
                _renderOffset = pixMinScaled;
                _renderSize = pixMaxScaled - pixMinScaled + 1;
                if (_renderSize.x <= 0 || _renderSize.y <= 0)
                {
                    tex.Size = 1;
                    offscreen = true;
                }
                else
                {
					var img2 = ZoomScale(_renderedImage, pixMinScaled, pixMaxScaled);
                    _renderedImage.Dispose();
                    _renderedImage = img2;
                    tex.Size = _renderSize;
                }
            }

            _changed = false;
            _overlayChanged = false;

            using var temp = _renderedImage.Clone();
            temp.Mutate(i =>
            {
                // Draw layer overlay shapes
                foreach (Layer layer in LayerManager.Layers)
                {
                    if (layer.Hidden) continue;
                    foreach (var col in layer.RenderOverlayShapes)
                        col.DrawToRender(i);
                }

                // Draw overlay shapes
                while (_overlayShapes.TryDequeue(out IShape shape))
                    shape.DrawToRender(i, temp);

                // Draw selection stuff
                if (Selection is null) return;
                Selection.DrawOutline(i);

                if (!Selection.DrawBoundaryDots) return;
                // Draw boundary dots
                int2 min_ = Util.CanvasToRender(Selection.Min);
                int2 max_ = Util.CanvasToRender(Selection.Max + 1) - 1;
                int2 center = (max_ + min_) / 2;

                for (int j = 0; j < 8; j++)
                {
                    int2 point = int2.Zero;
                    switch (j)
                    {
                        case 0: point = new(center.x, min_.y); break;
                        case 1: point = new(center.x, max_.y); break;
                        case 2: point = new(min_.x, center.y); break;
                        case 3: point = new(max_.x, center.y); break;
                        case 4: point = min_; break;
                        case 5: point = new(max_.x, min_.y); break;
                        case 6: point = new(min_.x, max_.y); break;
                        case 7: point = max_; break;
                    }

                    // Move dots
                    new DrawingOval(Color.Gray, point - Defs.DotOuterSize / 2, Defs.DotOuterSize).Draw(i);
                    new DrawingOval(Color.White, point - Defs.DotInnerSize / 2, Defs.DotInnerSize).Draw(i);
                }
            });

            if (!offscreen) Util.DrawImageToTexture2DAsRGB24(tex!, temp);

            size = _renderSize;
            offset = _renderOffset;
            return;
        }

        public Image<Argb32> RenderToImage() => LayerManager.Merge();

        public void ClearSelection()
        {
            if (Selection is null) return;
            ActiveTool.PreSelectionChanged();
            Selection.Dispose();
            Selection = null;
            _overlayChanged = true;
        }

        public void GetZoomBounds(out double ratio, out double minZoom, out double maxZoom)
        {
            int2 maxSize = Math2.Max(MainWindow.CanvasMax - MainWindow.CanvasMin + 1, 1);
            ratio = Math.Min(maxSize.x / (double)CanvasSize.x, maxSize.y / (double)CanvasSize.y);
            minZoom = Math.Min(1, ratio * 0.1f);
            maxZoom = Math.Max(ratio * 2, 10);
        }
		#endregion

		#region Private Methods
		private Image<Argb32> ZoomScale(Image<Argb32> image, int2 scaledMin, int2 scaledMax)
		{
			// Downscale with a modified Box algorithm that treats edge pixels as non-full pixels for fractional coordinates
			// Upscale with nearest neighbor sampling
			// Downscaling and upscaling are separated on each axis

			int2 imageSize = new int2(image.Width, image.Height);
			int2 newSize = Math2.RoundToInt(imageSize * _zoom);
			double2 scale = newSize / (double2)imageSize;

			int2 croppedSize = scaledMax - scaledMin + 1;
            if (_zoom == 1)
            {
                // Don't scale
                RT_pixMin = scaledMin;
				return image.GetSubimage(scaledMin, croppedSize);
            }

			RT_pixMin = scaledMin / scale;
			Image<Argb32> scaledImage = new(croppedSize.x, croppedSize.y);
			if (_zoom > 1)
            {
                // Scale up using nearest neighbor
                Parallel.For(scaledMin.y, scaledMax.y + 1, y =>
                {
                    Span<Argb32> destRow = scaledImage.DangerousGetPixelRowMemory(y - scaledMin.y).Span;
                    Span<Argb32> srcRow = image.DangerousGetPixelRowMemory((int)(y / scale.y)).Span;
                    for (int x = scaledMin.x; x <= scaledMax.x; x++)
                        destRow[x - scaledMin.x] = srcRow[(int)(x / scale.x)];
                });
                return scaledImage;
            }

			// Scale down using modified box sampling
			double2 boxSize1 = 1 / scale - 1;
			int2 unscaledSize1 = imageSize - 1;
			Parallel.For(scaledMin.y, scaledMax.y + 1, y =>
            {
                Span<Argb32> destRow = scaledImage.DangerousGetPixelRowMemory(y - scaledMin.y).Span;
                double yd = y / scale.y; // y in the original image's scale
                int y1 = Math.Min((int)Math.Floor(yd), unscaledSize1.y); // Top bound floord
                double top = yd == y1 ? 1 : (1 - yd + y1); // Top color multiplier
                Span<Argb32> rowY1 = image.DangerousGetPixelRowMemory(y1).Span;

                double y_2 = yd + boxSize1.y;
                int y2 = Math.Min((int)Math.Ceiling(y_2), unscaledSize1.y); // Bottom bound ceiled
                double bottom = y_2 == y2 ? 1 : (1 - y2 + y_2); // Bottom color multiplier
                int innerHeight = Math.Max(y2 - y1 - 1, 0);
                Span<Argb32> rowY2 = image.DangerousGetPixelRowMemory(y2).Span;

                for (int x = scaledMin.x; x <= scaledMax.x; x++)
                {
                    double xd = x / scale.x; // x in the original image's scale
                    int x1 = Math.Min((int)Math.Floor(xd), unscaledSize1.x); // Left bound floored
                    double x_2 = xd + boxSize1.x;
                    int x2 = Math.Min((int)Math.Ceiling(x_2), unscaledSize1.x); // Right bound ceiled
                    double right = x_2 == x2 ? 1 : (1 - x2 + x_2); // Right color multiplier
                    double left = xd == x1 ? 1 : (1 - xd + x1); // Left color multiplier
					int innerWidth = Math.Max(x2 - x1 - 1, 0);

					// Bilinear sample on the corners
					double tlMult = top * left;
                    double trMult = top * right;
                    double blMult = bottom * left;
                    double brMult = bottom * right;
					double4 pixel = rowY1[x1].ToDouble4() * tlMult + // Top left pixel
                        rowY1[x2].ToDouble4() * trMult +            // Top right pixel
                        rowY2[x1].ToDouble4() * blMult +           // Bottom left pixel
                        rowY2[x2].ToDouble4() * brMult;           // Bottom right pixel

                    // Sample top and bottom edge
                    for (int x_ = x1 + 1; x_ < x2; x_++)
                    {
                        pixel += rowY1[x_].ToDouble4() * top +
                            rowY2[x_].ToDouble4() * bottom;
                    }

                    for (int y_ = y1 + 1; y_ < y2; y_++)
                    {
						// Sample full insides
						for (int x_ = x1 + 1; x_ < x2; x_++)
                            pixel += image[x_, y_].ToDouble4();

                        // Sample left and right edges
                        pixel += image[x1, y_].ToDouble4() * left +
                            image[x2, y_].ToDouble4() * right;
                    }

                    destRow[x - scaledMin.x] = (pixel / (tlMult + trMult + blMult + brMult + innerWidth * (top + bottom) + innerHeight * (left + right) + innerWidth * innerHeight)).ToArgb32();
                }
            });

            return scaledImage;
		}
		#endregion
	}
}
