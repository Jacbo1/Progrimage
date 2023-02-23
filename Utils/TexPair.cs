using Microsoft.Xna.Framework.Graphics;
using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
            Util.DrawImageToTexture2D(Texture, image);
        }

        public TexPair()
        {
            Texture = null;
            _ptr = IntPtr.Zero;
        }

        public TexPair(string path, bool convertToSRGB = false)
        {
            if (!File.Exists(path))
            {
                // File not found
				Texture = null;
				_ptr = IntPtr.Zero;
                return;
			}

            if (convertToSRGB)
            {
                using var img = Image.Load<Argb32>(path);
                Texture = new Texture2D(MainWindow.GraphicsDevice, img.Width, img.Height, false, SurfaceFormat.Color);
                Util.DrawImageToTexture2D(Texture, img);
            }
            else Texture = Texture2D.FromFile(MainWindow.GraphicsDevice, path);
            _ptr = MainWindow.ImGuiRenderer.BindTexture(Texture);
        }

        public TexPair(string path, int2 size)
        {
            if (!File.Exists(path))
            {
                // File not found
                Texture = null;
                _ptr = IntPtr.Zero;
                return;
            }

            using var img = Util.HighQualityDownscale(Image.Load<Argb32>(path), size, true);
            Texture = new Texture2D(MainWindow.GraphicsDevice, img.Width, img.Height, false, SurfaceFormat.Color);
            Util.DrawImageToTexture2D(Texture, img);
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
