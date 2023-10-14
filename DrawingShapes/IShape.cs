using ImageSharpExtensions;
using NewMath;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Progrimage.DrawingShapes
{
	public interface IShape
    {
        /// <summary>
        /// The position of the IShape.
        /// </summary>
        public double2 Pos { get; set; }

        public bool Hidden { get; set; }

        /// <summary>
        /// Draws the IShape to the ImageProcessingContext
        /// </summary>
        /// <param name="context"></param>
        public void DrawToRender(IImageProcessingContext context);

        public void DrawToRender(IImageProcessingContext context, Image<Argb32> image) => DrawToRender(context);

        /// <summary>
        /// Draws the IShape to the ImageProcessingContext.
        /// </summary>
        /// <param name="context"></param>
        public void Draw(IImageProcessingContext context);

        /// <summary>
        /// Draws the IShape to the Layer.
        /// </summary>
        /// <param name="layer"></param>
        public void Draw(Layer layer)
        {
            if (layer.Image.Image is null)
            {
                var bounds = GetBounds();
                layer.Image = new(new int2(bounds.X, bounds.Y), new int2(bounds.Width, bounds.Height));
            }
            layer.Image.Mutate(Draw);
            layer.Changed();
        }

        /// <summary>
        /// Draws the IShape to the image.
        /// </summary>
        /// <param name="img"></param>
        public void Draw(PositionedImage<Argb32> img) => img.Mutate(Draw);

        /// <summary>
        /// Gets the bounds of the IShape.
        /// </summary>
        /// <returns></returns>
        public abstract Rectangle GetBounds();

        /// <summary>
        /// Gets the size of the IShape.
        /// </summary>
        /// <returns></returns>
        public abstract int2 GetSize();
    }
}