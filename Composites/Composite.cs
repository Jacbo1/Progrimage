using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using NewMath;
using Progrimage.Utils;
using System.Numerics;
using SixLabors.ImageSharp.Processing;
using System.Collections;
using Progrimage.CoroutineUtils;
using ImageSharpExtensions;

namespace Progrimage.Composites
{
    public class Composite : IUsesToolbar, IDisposable
    {
        public string Name { get; set; }
        public TexPair Thumbnail;
        public Vector2 ThumbnailSize;
        public Layer Layer;
        public ICompositeAction CompositeAction;
        public bool Processing { get; private set; }
        public bool Hidden;

        private bool _shouldUpdateThumbnail;

        #region Constructor
        public Composite(Layer layer, ICompositeAction action)
        {
            Name = "Composite";
            Layer = layer;
            CompositeAction = action;
            CompositeAction.Init(this);
            _shouldUpdateThumbnail = true;
            UpdateThumbnail();
            Program.ActiveInstance.ActiveComposite = this;
        }

        public Composite(ICompositeAction action)
        {
            Name = "Composite";
            Layer = Program.ActiveInstance.ActiveLayer!;
            CompositeAction = action;
            CompositeAction.Init(this);
            _shouldUpdateThumbnail = true;
            UpdateThumbnail();
            Program.ActiveInstance.ActiveComposite = this;
        }
        #endregion

        #region Public Methods
        public void Rerun()
        {
            JobQueue.Queue.Add(new CoroutineJob(Layer.Changed));
        }

        public IEnumerator Update()
        {
            // This should run every time the layer updates
            Processing = true;
            var enumerator = CompositeAction.Run(Layer.CompositeResult);
            while (enumerator.MoveNext()) yield return true;

            Processing = false;
            UpdateThumbnail();
        }

        public void Changed()
        {
            _shouldUpdateThumbnail = true;
            Program.ActiveInstance.Changed();
        }

        public void UpdateThumbnail()
        {
            if (!_shouldUpdateThumbnail) return;
            _shouldUpdateThumbnail = false;

            int2 maxSize = (int2)(MainWindow.LayerThumbnailSize * MainWindow.UIScale);

            if (Layer.CompositeResult.Image is null)
            {
                // Null texture
                using Image<Rgb24> temp = Util.GetTransparencyChecker(maxSize.x, maxSize.y, (int)(4 * MainWindow.UIScale));
                ThumbnailSize = maxSize;
                Thumbnail.Size = maxSize;
                Util.DrawImageToTexture2D(Thumbnail.Texture, temp);
                return;
            }

            // Has texture
            int2 thumbnailSize = Util.ScaleToFit(Layer.CompositeResult.Size, maxSize, true);
            ThumbnailSize = thumbnailSize;
            Thumbnail.Size = thumbnailSize;

            using (Image<Rgb24> temp = Util.GetTransparencyChecker(thumbnailSize.x, thumbnailSize.y, (int)(4 * MainWindow.UIScale)))
            {
                using Image<Argb32> img = Layer.CompositeResult.Image.Clone();
                img.Mutate(x => x.Resize(thumbnailSize.x, thumbnailSize.y));
                int2 pos = (maxSize - thumbnailSize) / 2;
                temp.DrawOver(img, pos);
                Util.DrawImageToTexture2D(Thumbnail.Texture, temp);
            }
        }

        public void DrawQuickActionsToolbar() => CompositeAction.DrawQuickActionsToolbar(Layer.CompositeResult);

        public void Dispose()
        {
            if (Program.ActiveInstance.ActiveComposite == this)
                Program.ActiveInstance.ActiveComposite = null;
			Layer.RemoveComposite(this);
            Thumbnail.Dispose();
        }
        #endregion
    }
}
