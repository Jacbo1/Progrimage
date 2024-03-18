using ImageSharpExtensions;
using Microsoft.Xna.Framework.Graphics;
using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Progrimage.Utils
{
	public struct TexPair : IDisposable
    {
        public Texture2D Texture;
        private IntPtr _ptr;

        public IntPtr Ptr
        {
            get => IsValid() ? _ptr : Util.GetDefaultIconTexture().Ptr;
        }

        public int2 Size
        {
            get
            {
                if (IsValid()) return new int2(Texture.Width, Texture.Height);
                return int2.Zero;
                
            }
            set
            {
                if (IsValid())
                {
                    if (Size == value) return;
                    Dispose();
                    this = MainWindow.CreateTexture(value);
                    return;
                }

                this = MainWindow.CreateTexture(value);
            }
        }

        public TexPair(Texture2D texture, IntPtr ptr)
        {
            Texture = texture;
            _ptr = ptr;
        }

        public TexPair(Texture2D texture)
        {
            Texture = texture;
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
        }

        public TexPair(Image<Rgb24> image)
        {
            Texture = MainWindow.CreateTexture(new int2(image.Width, image.Height))!;
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
            Util.DrawImageToTexture2D(Texture, image);
        }

        public TexPair(Image<Argb32> image)
        {
            Texture = new Texture2D(MainWindow.GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
            Util.DrawImageToTexture2DFixSrgb(Texture, image);
        }

        public TexPair()
        {
            Texture = null;
            _ptr = IntPtr.Zero;
        }

        public TexPair(string path, bool hasAlpha = false)
        {
            if (!File.Exists(path))
            {
                // File not found
				Texture = null;
				_ptr = IntPtr.Zero;
                return;
			}

            if (hasAlpha)
            {
                using var img = Image.Load<Argb32>(path);
                Texture = new Texture2D(MainWindow.GraphicsDevice, img.Width, img.Height, false, SurfaceFormat.Color);
                Util.DrawImageToTexture2DFixSrgb(Texture, img);
            }
            else Texture = Texture2D.FromFile(MainWindow.GraphicsDevice, path);
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
        }

        public TexPair(string path, int2 size, bool hasAlpha = false)
        {
            if (!File.Exists(path))
            {
                // File not found
                Texture = null;
                _ptr = IntPtr.Zero;
                return;
            }

            Image<Argb32> img = Image.Load<Argb32>(path);
			ISEUtils.HighQualityDownscale(ref img, Util.ScaleToFit(new int2(img.Width, img.Height), size), true);
            Image<Argb32> padded = new Image<Argb32>(size.X, size.Y);
            padded.DrawOver(img, new int2((size.X - img.Width) / 2, (size.Y - img.Height) / 2));
            img.Dispose();
            Texture = new Texture2D(MainWindow.GraphicsDevice, padded.Width, padded.Height, false, SurfaceFormat.Color);
            if (hasAlpha) Util.DrawImageToTexture2DFixSrgb(Texture, padded);
            else Util.DrawImageToTexture2D(Texture, padded);
			padded.Dispose();
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
        }

        public void Rebind()
        {
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
        }

        public void Dispose()
        {
            if (IsValid()) Texture?.Dispose();
        }

        public bool IsValid()
        {
            return Texture != null && !Texture.IsDisposed;
        }

        public static implicit operator Texture2D?(TexPair t) => t.Texture;

        public static implicit operator IntPtr(TexPair t)
        {
            return t.IsValid() ? t._ptr : Util.GetDefaultIconTexture().Ptr;
        }
    }
}
