using ImageSharpExtensions;
using NewMath;
using Progrimage.DrawingShapes;
using Progrimage.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.Selectors
{
    public class MarqueSelection : ISelector
    {
        #region Fields
        private Image<Argb32>? _image;
        private bool _grabSource = true,
            _drawImageInOverlay = false,
            _drawBoundaryDots = false;
        private int2 _min, _max;
        #endregion

        #region Properties
        public override int2 Min
        {
            get => _min;
            set
            {
                int2 val = Math2.Clamp(value, 0, Program.ActiveInstance.CanvasSize - 1);
                if (val == _min) return;
                _min = val;
                UpdateOutline();
            }
        }
        
        public override int2 Max
        {
            get => _max;
            set
            {
                int2 val = Math2.Clamp(value, 0, Program.ActiveInstance.CanvasSize - 1);
                if (val == _max) return;
                _max = val;
                UpdateOutline();
            }
        }
        
        public override int2 Pos
        {
            get => Min;
            set
            {
                if (value == _min) return;
                _max += value - _min;
                _min = value;
                UpdateOutline();
            }
        }
        
        public override bool DrawImageInOverlay
        {
            get => _drawImageInOverlay;
            set
            {
                if (_drawImageInOverlay == value) return; // Same value
                _drawImageInOverlay = value;

                if (value) Overlay.Shapes.Add(new OverlayImage(Image, int2.Zero)); // Add selection image to overlay
                else Overlay.Shapes.RemoveAt(0); // Remove selection image from overlay
                Program.ActiveInstance.Changed();
            }
        }

        /// <summary>
        /// Should the selection image copy from the Layer
        /// </summary>
        public override bool GrabSource
        {
            get => _grabSource;
            set
            {
                if (!value && _grabSource) _ = Image; // Update image
                _grabSource = value;
            }
        }

        public override Image<Argb32> Image
        {
            get
            {
                if (!_grabSource) return _image!;

                int2 size = _max - _min + 1;
				_image?.Dispose();
                if (_layer.Image.Image is null)
                {
                    _image = new Image<Argb32>(size.x, size.y);
                    return _image; // Layer does not overlap with selection
                }

                // Copy Layer selection to _image
                _image = _layer.Image.Image.GetSubimage(_min - _layer.Pos, size);
                return _image;
            }
            set
            {
                _grabSource = value == null;
                _image = value;
                if (!_drawImageInOverlay) return;

                OverlayImage overlay = (OverlayImage)Overlay.Shapes[0];
                overlay.Image = Image;
                Overlay.Shapes[0] = overlay;
                Program.ActiveInstance.Changed();
            }
        }

        public override bool DrawBoundaryDots
        {
            get => _drawBoundaryDots;
            set
            {
                if (_drawBoundaryDots == value) return;
                _drawBoundaryDots = value;
                Program.ActiveInstance.Changed();
            }
        }
        #endregion

        #region Constructors
        public MarqueSelection(int2 min, int2 max, Layer layer)
        {
            _layer = layer;
            _max = max;
            _min = min;

            // Init Outline
            Overlay = new(Program.ActiveInstance.ActiveLayer!);
            Program.ActiveInstance.ActiveLayer!.OverlayShapes.Add(Overlay);

            UpdateOutline();
        }
        #endregion

        #region Public Methods
        public override void ClearSelection(Layer layer)
        {
            if (layer.Image.Image is null) return; // No texture

            Rectangle region = new Rectangle(_min.x, _min.y, _max.x - _min.x + 1, _max.y - _min.y + 1);
            Rectangle bounds = new Rectangle(layer.Pos.x, layer.Pos.y, layer.Size.x, layer.Size.y);

            if (!region.IntersectsWith(bounds))
                return; // Layer does not overlap with selection

            region = Util.Clamp(region, bounds);
            region.Offset(-layer.Pos.x, -layer.Pos.y);

            // Draw the Layer selection to _image
            layer.Image.Mutate(i => i.Clear(Color.Transparent, region));
            layer.Changed();
        }

        public override void DrawImage(Layer layer) => layer.Image.DrawOver(new PositionedImage<Argb32>(Min, Image), true);

        public override void Dispose()
        {
            DrawImageInOverlay = false;
            Overlay?.Dispose();
            Overlay = null;
            _image?.Dispose();
            _image = null;
            Program.ActiveInstance.Changed();
        }

        public override void DrawOutline(IImageProcessingContext image)
        {
            double2 min = Util.CanvasToRenderDouble(_min);
            if (_min == int2.Zero) min = min + 1;
            double2 max = Util.CanvasToRenderDouble(_max + 1);
            if (_max == Program.ActiveInstance.CanvasSize - 1) max = max - 1;
            new DrawingRect(Color.Red, min, max - min, 1).Draw(image);
        }

        public override Image<Argb32> GetImageFromRender()
        {
            using PositionedImage<Argb32> src = Program.ActiveInstance.LayerManager.Merge();
            return src.Image!.GetSubimage(_min, _max - _min + 1);
        }
        #endregion

        #region Private Methods
        private void UpdateOutline()
        {
            Overlay.Pos = _min;
            Program.ActiveInstance.Changed();
        }
        #endregion
    }
}
