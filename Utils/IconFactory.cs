// Source: https://stackoverflow.com/a/32530019

using System.Drawing.Imaging;
using Image = System.Drawing.Image;

namespace Progrimage.Utils
{
	/// <summary>
	/// Provides methods for creating icons.
	/// </summary>
	public static class IconFactory
	{
		private static readonly ImageCodecInfo? _pngCodecInfo;
		#region constants

		/// <summary>
		/// Represents the max allowed width of an icon.
		/// </summary>
		public const int MaxIconWidth = 256;

		/// <summary>
		/// Represents the max allowed height of an icon.
		/// </summary>
		public const int MaxIconHeight = 256;

		private const ushort HeaderReserved = 0;
		private const ushort HeaderIconType = 1;
		private const byte HeaderLength = 6;

		private const byte EntryReserved = 0;
		private const byte EntryLength = 16;

		private const byte PngColorsInPalette = 0;
		private const ushort PngColorPlanes = 1;

		#endregion

		#region methods
		static IconFactory()
		{
			_pngCodecInfo = ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Png.Guid);
		}

		/// <summary>
		/// Saves the specified <see cref="Bitmap"/> objects as a single 
		/// icon into the output stream.
		/// </summary>
		/// <param name="images">The bitmaps to save as an icon.</param>
		/// <param name="path">The output file path.</param>
		/// <remarks>
		/// The expected input for the <paramref name="images"/> parameter are 
		/// portable network graphic files that have a <see cref="Image.PixelFormat"/> 
		/// of <see cref="PixelFormat.Format32bppArgb"/> and where the
		/// width is less than or equal to <see cref="IconFactory.MaxIconWidth"/> and the 
		/// height is less than or equal to <see cref="MaxIconHeight"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Occurs if any of the input images do 
		/// not follow the required image format. See remarks for details.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Occurs if any of the arguments are null.
		/// </exception>
		public static void SaveAsIcon(Bitmap image, string path)
		{
			int[] sizes = { 16, 32, 48, 64, 128, 256 };
			var bitmaps = new Bitmap[sizes.Length];

			for (int i = 0; i < sizes.Length; i++)
			{
				int size = sizes[i];
				var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);

				using var g = Graphics.FromImage(bitmap);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

				int newWidth = size;
				int newHeight = size;

				double scalex = image.Width / (double)newWidth;
				double scaley = image.Height / (double)newHeight;
				if (scalex > scaley) newHeight = Math.Max(1, (int)(image.Height / scalex + 0.5));
				else newWidth = Math.Max(1, (int)(image.Width / scaley + 0.5));
				g.DrawImage(image, 0, 0, newWidth, newHeight);
				bitmaps[i] = bitmap;
			}

			SaveAsIcon(bitmaps, path);

			foreach (var bitmap in bitmaps)
			{
				bitmap.Dispose();
			}
		}
		/// <summary>
		/// Saves the specified <see cref="Bitmap"/> objects as a single 
		/// icon into the output stream.
		/// </summary>
		/// <param name="images">The bitmaps to save as an icon.</param>
		/// <param name="path">The output file path.</param>
		/// <remarks>
		/// The expected input for the <paramref name="images"/> parameter are 
		/// portable network graphic files that have a <see cref="Image.PixelFormat"/> 
		/// of <see cref="PixelFormat.Format32bppArgb"/> and where the
		/// width is less than or equal to <see cref="IconFactory.MaxIconWidth"/> and the 
		/// height is less than or equal to <see cref="MaxIconHeight"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Occurs if any of the input images do 
		/// not follow the required image format. See remarks for details.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Occurs if any of the arguments are null.
		/// </exception>
		public static void SaveAsIcon(IEnumerable<Bitmap> images, string path)
		{
			using var stream = File.OpenWrite(path);
			SaveAsIcon(images, stream);
		}

		/// <summary>
		/// Saves the specified <see cref="Bitmap"/> objects as a single 
		/// icon into the output stream.
		/// </summary>
		/// <param name="images">The bitmaps to save as an icon.</param>
		/// <param name="stream">The output stream.</param>
		/// <remarks>
		/// The expected input for the <paramref name="images"/> parameter are 
		/// portable network graphic files that have a <see cref="Image.PixelFormat"/> 
		/// of <see cref="PixelFormat.Format32bppArgb"/> and where the
		/// width is less than or equal to <see cref="IconFactory.MaxIconWidth"/> and the 
		/// height is less than or equal to <see cref="MaxIconHeight"/>.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Occurs if any of the input images do 
		/// not follow the required image format. See remarks for details.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Occurs if any of the arguments are null.
		/// </exception>
		public static void SaveAsIcon(IEnumerable<Bitmap> images, Stream stream)
		{
			ArgumentNullException.ThrowIfNull(images);
			ArgumentNullException.ThrowIfNull(stream);

			// validates the pngs
			ThrowForInvalidPngs(images);

			Bitmap[] orderedImages = images.OrderBy(i => i.Width).ThenBy(i => i.Height).ToArray();

			using var writer = new BinaryWriter(stream);

			// write the header
			writer.Write(HeaderReserved);
			writer.Write(HeaderIconType);
			writer.Write((ushort)orderedImages.Length);

			// save the image buffers and offsets
			Dictionary<uint, byte[]> buffers = new();

			// tracks the length of the buffers as the iterations occur
			// and adds that to the offset of the entries
			uint lengthSum = 0;
			uint baseOffset = (uint)(HeaderLength + EntryLength * orderedImages.Length);

			for (int i = 0; i < orderedImages.Length; i++)
			{
				Bitmap image = orderedImages[i];

				// creates a byte array from an image
				byte[] buffer = CreateImageBuffer(image);

				// calculates what the offset of this image will be
				// in the stream
				uint offset = (baseOffset + lengthSum);

				// writes the image entry
				writer.Write(GetIconWidth(image));
				writer.Write(GetIconHeight(image));
				writer.Write(PngColorsInPalette);
				writer.Write(EntryReserved);
				writer.Write(PngColorPlanes);
				writer.Write((ushort)Image.GetPixelFormatSize(image.PixelFormat));
				writer.Write((uint)buffer.Length);
				writer.Write(offset);

				lengthSum += (uint)buffer.Length;

				// adds the buffer to be written at the offset
				buffers.Add(offset, buffer);
			}

			// writes the buffers for each image
			foreach (var kvp in buffers)
			{
				// seeks to the specified offset required for the image buffer
				writer.BaseStream.Seek(kvp.Key, SeekOrigin.Begin);

				// writes the buffer
				writer.Write(kvp.Value);
			}

		}

		private static void ThrowForInvalidPngs(IEnumerable<Bitmap> images)
		{
			foreach (var image in images)
			{
				if (image.PixelFormat != PixelFormat.Format32bppArgb)
				{
					throw new InvalidOperationException
						(string.Format("Required pixel format is PixelFormat.{0}.",
									   PixelFormat.Format32bppArgb.ToString()));
				}

				if (image.Width > MaxIconWidth || image.Height > MaxIconHeight)
				{
					throw new InvalidOperationException(string.Format("Dimensions must be less than or equal to {0}x{1}", MaxIconWidth, MaxIconHeight));
				}
			}
		}

		private static byte GetIconHeight(Bitmap image)
		{
			return image.Height == MaxIconHeight ? (byte)0 : (byte)image.Height;
		}

		private static byte GetIconWidth(Bitmap image)
		{
			return image.Width == MaxIconWidth ? (byte)0 : (byte)image.Width;
		}

		private static byte[] CreateImageBuffer(Bitmap image)
		{
			//using var stream = new MemoryStream();
			//image.Save(stream, ImageFormat.Png);
			//return stream.ToArray();

			EncoderParameters encoderParams = new(1);
			encoderParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32);
			using var stream = new MemoryStream();
			image.Save(stream, _pngCodecInfo, encoderParams);
			return stream.ToArray();
		}
		#endregion
	}
}
