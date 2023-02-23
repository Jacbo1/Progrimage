using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
using System.Security.Cryptography;
using Image = SixLabors.ImageSharp.Image;

namespace Progrimage
{
    public interface BrushPath : IDisposable
    {
    }

    public struct BrushPath<TPixel> : BrushPath where TPixel : unmanaged, IPixel<TPixel>
    {
        public string Path;
        private Image<TPixel>? _image = null;
        public string NormPath
        {
            get => System.IO.Path.GetDirectoryName(Path) + '\\' + System.IO.Path.GetFileNameWithoutExtension(Path) + ".norm";
		}

        public BrushPath(string path)
        {
            Path = path;
        }

        public Image<TPixel> Load()
        {
            _image ??= Image.Load<TPixel>(Path);
            return _image;
        }

        public float[] LoadNorm(out int2 size)
        {
            if (!File.Exists(NormPath))
            {
                // No normalization file
                size = int2.One;
                return new float[] { 1 };
            }

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (FileStream fileStream = File.OpenRead(NormPath))
                using (GZipStream zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    zipStream.CopyTo(stream);
                data = stream.ToArray();
            }

            // Read dimensions
            int width = BitConverter.ToInt32(data, 0);
            int height = BitConverter.ToInt32(data, 4);
            size = new int2(width, height);

			// Read pixels
			float[] arr = new float[width * height];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = BitConverter.ToSingle(data, (i + 2) * 4);
            return arr;
		}

        public void Dispose()
        {
			_image?.Dispose();
			_image = null;

		}

        public static bool operator ==(BrushPath<TPixel> a, BrushPath<TPixel> b) => a.Path == b.Path;
        public static bool operator !=(BrushPath<TPixel> a, BrushPath<TPixel> b) => a.Path != b.Path;
    }
}
