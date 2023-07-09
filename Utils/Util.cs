using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using NewMath;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;
using System.Runtime.InteropServices;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;
using XnaColor = Microsoft.Xna.Framework.Color;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using Point = SixLabors.ImageSharp.Point;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using Size = SixLabors.ImageSharp.Size;
using PointF = SixLabors.ImageSharp.PointF;
using System.Drawing.Imaging;
using ImageSharpExtensions;

namespace Progrimage.Utils
{
    public static class Util
    {
        private static int _windowID = 0;
        private static Image<Argb32>? _defaultIconImg, _defaultTextureImg;
        private static TexPair _defaultIconTex, _defaultTextureTex;
        private static ImGuiMouseCursor _currentCusor = ImGuiMouseCursor.Arrow;
        private static ImageCodecInfo? _codecInfo;

        public static long Time
        {
            get => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        /// <summary>
        /// Converts a screen position to a canvas position
        /// </summary>
        /// <param name="pos">Screen position</param>
        /// <returns>Canvas position</returns>
        public static int2 ScreenToCanvas(int2 pos) => (int2)((pos - MainWindow.CanvasOriginDouble) / Program.ActiveInstance.Zoom);
        public static double2 ScreenToCanvasDouble(int2 pos) => (pos - MainWindow.CanvasOriginDouble) / Program.ActiveInstance.Zoom;

        /// <summary>
        /// Converts a canvas position to a screen position
        /// </summary>
        /// <param name="pos">Canvas position</param>
        /// <returns>Screen position</returns>
        public static int2 CanvasToScreen(int2 pos) => Math2.RoundToInt((pos + MainWindow.CanvasOriginDouble) * Program.ActiveInstance.Zoom);
        public static double2 CanvasToScreenDouble(int2 pos) => (pos + MainWindow.CanvasOriginDouble) * Program.ActiveInstance.Zoom;
        public static double2 CanvasToScreenDouble(double2 pos) => (pos + MainWindow.CanvasOriginDouble) * Program.ActiveInstance.Zoom;

        /// <summary>
        /// Converts a canvas position to a position on the rendered image
        /// </summary>
        /// <param name="pos">Canvas position</param>
        /// <returns>Rendered position</returns>
        public static int2 CanvasToRender(double2 pos) => (int2)((pos - Program.ActiveInstance.RT_pixMin) * Program.ActiveInstance.Zoom);

		public static double2 CanvasToRenderDouble(double2 pos) => (pos - Program.ActiveInstance.RT_pixMin) * Program.ActiveInstance.Zoom;

        /// <summary>
        /// Gets a unique window ID for ImGui
        /// </summary>
        /// <returns>A unique identifier as a string</returns>
        public static string GetUniqueWindowID()
        {
            _windowID = (_windowID + 1) % int.MaxValue;
            return _windowID.ToString();
        }

        /// <summary>
        /// Allows dragging and dropping a color onto the component
        /// </summary>
        /// <returns>A Vector4 if a color was dropped, else null</returns>
        public static Vector4? DragDropColor()
        {
            if (!ImGui.BeginDragDropTarget()) return null;

            Vector4? col = null;
            unsafe
            {
                ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("_COL3F");

                if (payload.NativePtr != null)
                    col = new Vector4(Marshal.PtrToStructure<Vector3>(payload.Data), 1);
                else
                {
                    payload = ImGui.AcceptDragDropPayload("_COL4F");

                    if (payload.NativePtr != null)
                        col = Marshal.PtrToStructure<Vector4>(payload.Data);
                }
            }
            ImGui.EndDragDropTarget();

            return col;
        }

        /// <summary>
        /// Checks whether the user is dragging a color to drop it somewhere
        /// </summary>
        /// <returns>True if dragging</returns>
        public static bool IsDraggingColor()
        {
            unsafe
            {
                ImGuiPayloadPtr payload = ImGui.GetDragDropPayload();
                if (payload.NativePtr != null)
                    return payload.IsDataType("_COL3F") || payload.IsDataType("_COL4F");
            }
            return false;
        }

        public static Argb32 Blend(Argb32 source, Argb32 over)
        {
            if (over.A == 0) return source; // Overlay pixel is fully transparent

            byte iOverlayAlpha = (byte)(255 - over.A);
            byte alpha = (byte)(over.A + source.A * iOverlayAlpha / 255);

            if (source.A == 0)
            {
                // Overlay is the only source of color and will darken if not handled separately
                source.R = over.R;
                source.G = over.G;
                source.B = over.B;
            }
            else
            {
                // Blend colors
                source.R = (byte)((over.R * over.A + source.R * source.A * iOverlayAlpha / 255) / alpha);
                source.G = (byte)((over.G * over.A + source.G * source.A * iOverlayAlpha / 255) / alpha);
                source.B = (byte)((over.B * over.A + source.B * source.A * iOverlayAlpha / 255) / alpha);
            }

            source.A = alpha;

            return source;
        }

        ///// <summary>
        ///// Alpha blend 2 colors
        ///// </summary>
        ///// <param name="a">Source color</param>
        ///// <param name="b">Overlay color</param>
        ///// <returns>Blended color</returns>
        //public static int4 BlendColors(int4 a, int4 b)
        //{
        //    if (b.w == 255 || a.w == 0) return b; // No need to blend alpha

        //    double aAlpha = a.w / 255.0;
        //    double bAlpha = b.w / 255.0;
        //    double alpha1 = 1 - bAlpha;
        //    double denom = bAlpha + aAlpha * alpha1;
        //    if (denom == 0) return a;
        //    double alphaMult = aAlpha * alpha1;
        //    return new int4(
        //        (int)Math.Round((b.x * bAlpha + a.x * alphaMult) / denom, MidpointRounding.AwayFromZero),
        //        (int)Math.Round((b.y * bAlpha + a.y * alphaMult) / denom, MidpointRounding.AwayFromZero),
        //        (int)Math.Round((b.z * bAlpha + a.z * alphaMult) / denom, MidpointRounding.AwayFromZero),
        //        (int)Math.Round(b.w + a.w * alpha1, MidpointRounding.AwayFromZero));
        //}

        public static int4 BlendColorsErase(int4 a, int4 b)
        {
            if (b.w == 0) return a;

            double srcAlpha = a.w / 255.0;
            int colorAlpha = 255 - b.w;
            double alpha1 = 1 - srcAlpha;
            double denom = srcAlpha + colorAlpha / 255.0 * alpha1;
            if (denom == 0) return a;

            double alphaMult = colorAlpha * alpha1;
            return new int4(
                (int)Math.Round((b.x * alphaMult + a.x * srcAlpha) / denom, MidpointRounding.AwayFromZero),
                (int)Math.Round((b.y * alphaMult + a.y * srcAlpha) / denom, MidpointRounding.AwayFromZero),
                (int)Math.Round((b.z * alphaMult + a.z * srcAlpha) / denom, MidpointRounding.AwayFromZero),
                (int)Math.Round(colorAlpha * alpha1 + a.w, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Draws an Image to a Texture2D
        /// </summary>
        /// <param name="tex">Destination texture</param>
        /// <param name="img">Source image</param>
        public static void DrawImageToTexture2D(Texture2D tex, Image<Rgb24> img)
        {
            uint[] pixels = new uint[tex.Width * tex.Height];

            Parallel.For(0, Math.Min(img.Height, tex.Height), y =>
            {
                int j = y * tex.Width;
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < Math.Min(row.Length, tex.Width); x++)
                {
                    var pixel = row[x];
                    pixels[x + j] = (uint)((pixel.B << 16) + (pixel.G << 8) + pixel.R);
                }
            });
            tex.SetData(pixels);
        }

        /// <summary>
        /// Draws an Image to a Texture2D
        /// </summary>
        /// <param name="tex">Destination texture</param>
        /// <param name="img">Source image</param>
        public static void DrawImageToTexture2DAsRGB24(Texture2D tex, Image<Argb32> img)
        {
            uint[] pixels = new uint[tex.Width * tex.Height];

            Parallel.For(0, Math.Min(img.Height, tex.Height), y =>
            {
                int j = y * tex.Width;
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < Math.Min(row.Length, tex.Width); x++)
                {
                    var pixel = row[x];
                    pixels[x + j] = (uint)((pixel.B << 16) + (pixel.G << 8) + pixel.R);
                }
            });
            tex.SetData(pixels);
        }

        /// <summary>
        /// Draws an Image to a Texture2D while supporting alpha
        /// </summary>
        /// <param name="tex">Destination texture (must be SurfaceColor.Color)</param>
        /// <param name="img">Source image</param>
        public static void DrawImageToTexture2D(Texture2D tex, Image<Argb32> img)
        {
            XnaColor[] pixels = new XnaColor[tex.Width * tex.Height];

            Parallel.For(0, Math.Min(img.Height, tex.Height), y =>
            {
                int j = y * img.Width;
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < Math.Min(row.Length, tex.Width); x++)
                {
                    var pixel = row[x];
                    float r = (float)Math.Pow(pixel.R / 255.0, 2.2);
                    float g = (float)Math.Pow(pixel.G / 255.0, 2.2);
                    float b = (float)Math.Pow(pixel.B / 255.0, 2.2);
                    pixels[x + j] = XnaColor.FromNonPremultiplied(new XnaVector4(r, g, b, pixel.A / 255f));
                }
            });
            tex.SetData(pixels);
        }

        /// <summary>
        /// Generates a checkered white and gray background used to indicate transparency
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="checkerSize"></param>
        /// <returns></returns>
        public static Image<Rgb24> GetTransparencyChecker(int width, int height, int checkerSize)
        {
            Image<Rgb24> img = new(width, height, Color.White);
            img.Mutate(i =>
            {
                for (int x = 0; x < (int)Math.Ceiling(width / (double)checkerSize); x++)
                {
                    int x1 = x * checkerSize;
                    for (int y = (x % 2) * checkerSize; y < height; y += checkerSize * 2)
                        i.Fill(Color.LightGray, new Rectangle(x1, y, checkerSize, checkerSize));
                }
            });
            return img;
        }

        public static float GetSliderHeight()
        {
            return MainWindow.Style.FrameBorderSize + MainWindow.Style.FramePadding.Y * 2 + ImGui.GetFontSize();
        }

        /// <summary>
        /// Gets a path to open a file load dialog to
        /// </summary>
        /// <returns>File or directory path</returns>
        public static string? GetLoadPath()
        {
            return Program.ActiveInstance?.LastLoadPath ?? MainWindow.LastLoadPath;
        }

        /// <summary>
        /// Gets a path to open a file save dialog to
        /// </summary>
        /// <returns>File or directory path</returns>
        public static string? GetSavePath()
        {
            return Program.ActiveInstance?.LastSavePath ?? MainWindow.LastSavePath;
        }

        /// <summary>
        /// Sets a default path for opening file load dialog to
        /// </summary>
        /// <param name="path"></param>
        public static void SetLastLoadPath(string path)
        {
            if (Program.ActiveInstance != null)
                Program.ActiveInstance.LastLoadPath = path;
            MainWindow.LastLoadPath = path;
        }

        /// <summary>
        /// Sets a default path for opening file save dialog to
        /// </summary>
        /// <param name="path"></param>
        public static void SetLastSavePath(string path)
        {
            if (Program.ActiveInstance != null)
                Program.ActiveInstance.LastSavePath = path;
            MainWindow.LastSavePath = path;
        }

        /// <summary>
        /// Scales a size to fit within the size of the target size without exceeding
        /// </summary>
        /// <param name="size">Source Size</param>
        /// <param name="target">Target Size</param>
        /// <returns></returns>
        public static int2 ScaleToFit(int2 size, int2 target, bool scaleUp = true)
        {
            if (!scaleUp && size.x <= target.x && size.y <= target.y) return size;

            // Resize
            double xscale = size.x / (double)target.x;
            double yscale = size.y / (double)target.y;
            if (xscale > yscale) return new int2(target.x, (int)Math.Round(size.y / xscale, MidpointRounding.AwayFromZero));
            else return new int2((int)Math.Round(size.x / yscale, MidpointRounding.AwayFromZero), target.y);
        }

        /// <summary>
        /// Loads an image saved as L8 into a TexPair
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>The image</returns>
        public static TexPair LoadA8Texture(string path)
        {
            using var img = Image.Load<L8>(path);
            TexPair pair = MainWindow.CreateTexture(img.Width, img.Height);
            Texture2D tex = pair.Texture;
            XnaColor[] pixels = new XnaColor[tex.Width * tex.Height];
            Parallel.For(0, img.Height, y =>
            {
                int j = y * tex.Width;
                var row = img.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < row.Length; x++)
                {
                    byte value = row[x].PackedValue;
                    ref XnaColor pixel = ref pixels[x + j];
                    pixel.A = value;
                    pixel.R = byte.MaxValue;
                    pixel.G = byte.MaxValue;
                    pixel.B = byte.MaxValue;
                }
            });
            tex.SetData(pixels);
            return pair;
        }

        public static void SaveA8Image(Image<A8> img, string path, bool binary = false)
        {
            using var dest = new Image<L8>(img.Width, img.Height);
            for (int y = 0; y < img.Height; y++)
            {
                var srcRow = img.DangerousGetPixelRowMemory(y).Span;
                var destRow = dest.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < destRow.Length; x++)
                    destRow[x].PackedValue = srcRow[x].PackedValue;
            }
            dest.SaveAsPng(path, new PngEncoder() { ColorType = PngColorType.Grayscale, CompressionLevel = PngCompressionLevel.BestCompression, BitDepth = binary ? PngBitDepth.Bit1 : PngBitDepth.Bit8 });
        }

        public static Rectangle Clamp(Rectangle rect, Rectangle bounds)
        {
            int x = Math.Max(rect.X, bounds.X);
            int y = Math.Max(rect.Y, bounds.Y);
            return new Rectangle(
                x, y,
                Math.Min(rect.X + rect.Width, bounds.X + bounds.Width) - x,
                Math.Min(rect.Y + rect.Height, bounds.Y + bounds.Height) - y
            );
        }

        public static void ClearAndSetSize<TPixel>(ref Image<TPixel>? img, int2 size, Color? color = null) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (img is null)
            {
                img = new(size.x, size.y);
                if (color is null) return;
				img.Mutate(i => i.Clear((Color)color));
				return;
            }

            if (img.Width != size.x || img.Height != size.y)
            {
                img.Dispose();
                img = new(size.x, size.y);
				if (color is null) return;
				img.Mutate(i => i.Clear((Color)color));
				return;
            }

            img.Mutate(i => i.Clear(color ?? Color.Transparent));
        }

        public static Image<Argb32> GetDefaultIconImage()
        {
            _defaultIconImg ??= Image.Load<Argb32>(@"Assets\Textures\Icons\default.png");
            return _defaultIconImg;
        }

        public static Image<Argb32> GetDefaultTextureImage()
        {
            _defaultTextureImg ??= Image.Load<Argb32>(@"Assets\Textures\default.png");
            return _defaultTextureImg;
        }

        public static TexPair GetDefaultIconTexture()
        {
            if (!_defaultIconTex.IsValid())
                _defaultIconTex = new(@"Assets\Textures\Icons\default.png", true);
            return _defaultIconTex;
        }

        public static TexPair GetDefaultTexture()
        {
            if (!_defaultTextureTex.IsValid())
                _defaultTextureTex = new(@"Assets\Textures\default.png", true);
            return _defaultTextureTex;
        }

        public static bool Overlaps(this IImageProcessingContext context, int2 pos, int2 size)
        {
            Size contextSize = context.GetCurrentSize();
            return pos + size > int2.Zero  && pos.x < contextSize.Width && pos.y < contextSize.Height;
        }

        public static bool Overlaps(this IImageProcessingContext context, int2 pos1, int2 pos2, int2 size)
        {
            Size contextSize = context.GetCurrentSize();
            return pos1 < pos2 + size && pos2.x < pos1.x + contextSize.Width && pos2.y < pos1.y + contextSize.Height;
        }

        public static bool Overlaps(this IImageProcessingContext context, int2 pos, Image img)
        {
            Size contextSize = context.GetCurrentSize();
            return pos.x + img.Width > 0 && pos.y + img.Height > 0 && pos.x < contextSize.Width && pos.y < contextSize.Height;
        }

        public static bool Overlaps(this Image img, int2 pos, int2 size)
        {
            return pos + size > int2.Zero  && pos.x < img.Width && pos.y < img.Height;
        }

        public static bool Overlaps(this Image target, int2 pos, Image img)
        {
            return pos.x + img.Width > 0 && pos.y + img.Height > 0 && pos.x < target.Width && pos.y < target.Height;
        }

        public static void SetMouseCursor() => SetMouseCursor(_currentCusor);

        public static void SetMouseCursor(ImGuiMouseCursor cursor)
        {
            _currentCusor = cursor;
            if (cursor == ImGuiMouseCursor.Arrow || cursor == ImGuiMouseCursor.None)
            {
                ImGui.GetIO().MouseDrawCursor = false;
                return;
            }

            ImGui.GetIO().MouseDrawCursor = true;
            ImGui.SetMouseCursor(cursor);
        }

        /// <summary>
        /// Copies the given image to the clipboard as PNG, DIB and standard Bitmap format.
        /// </summary>
        /// <param name="image">Image to put on the clipboard.</param>
        /// <param name="imageNoTr">Optional specifically nontransparent version of the image to put on the clipboard.</param>
        /// <param name="data">Clipboard data object to put the image into. Might already contain other stuff. Leave null to create a new one.</param>
        public static void SetClipboardImage(Bitmap image)
        {
            Clipboard.SetImage(image);

            //if (_codecInfo is null)
            //{
            //    ImageCodecInfo[] encoders = ImageCodecInfo.GetImageEncoders();
            //    for (int t = 0; t < encoders.Length; t++)
            //    {
            //        if (encoders[t].FormatID == ImageFormat.Png.Guid)
            //            _codecInfo = encoders[t];
            //    }
            //}

            //MemoryStream ms = new MemoryStream();
            //image.Save(ms, _codecInfo!, null);
            //DataObject dataObject = new DataObject();
            //dataObject.SetData("PNG", false, ms);
            //Clipboard.Clear();
            //Clipboard.SetDataObject(dataObject, false);
            //Console.WriteLine("a");


            //Clipboard.Clear();
            //DataObject data = new DataObject();
            //using (MemoryStream pngMemStream = new MemoryStream())
            //using (MemoryStream dibMemStream = new MemoryStream())
            //{
            //    // As standard bitmap, without transparency support
            //    //data.SetData(DataFormats.Bitmap, true, image);
            //    // As PNG. Gimp will prefer this over the other two.
            //    image.Save(pngMemStream, ImageFormat.Png);
            //    data.SetData("PNG", false, pngMemStream);
            //    //// As DIB. This is (wrongly) accepted as ARGB by many applications.
            //    //byte[] dibData = ConvertToDib(image);
            //    //dibMemStream.Write(dibData, 0, dibData.Length);
            //    //data.SetData(DataFormats.Dib, false, dibMemStream);
            //    // The 'copy=true' argument means the MemoryStreams can be safely disposed after the operation.

            //    Clipboard.SetDataObject(data, true);
            //}
        }

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of type BITFIELDS.
        /// This is (wrongly) accepted by many applications as containing transparency,
        /// so I'm abusing it for that.
        /// </summary>
        /// <param name="image">Image to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        private static byte[] ConvertToDib(Bitmap image)
        {
            byte[] bm32bData;
            int width = image.Width;
            int height = image.Height;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new System.Drawing.Rectangle(0, 0, bm32b.Width, bm32b.Height));
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);

                bm32bData = GetImageData(bm32b);
            }
            // BITMAPINFOHEADER struct for DIB.
            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            WriteIntToByteArray(fullImage, 0x00, 4, true, (uint)hdrSize);
            //Int32 biWidth;
            WriteIntToByteArray(fullImage, 0x04, 4, true, (uint)width);
            //Int32 biHeight;
            WriteIntToByteArray(fullImage, 0x08, 4, true, (uint)height);
            //Int16 biPlanes;
            WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            WriteIntToByteArray(fullImage, 0x14, 4, true, (uint)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        /// <summary>
        /// Gets the raw bytes from an image.
        /// </summary>
        /// <param name="sourceImage">The image to get the bytes from.</param>
        /// <param name="stride">Stride of the retrieved image data.</param>
        /// <returns>The raw bytes of the image</returns>
        private static byte[] GetImageData(Bitmap sourceImage)
        {
            BitmapData sourceData = sourceImage.LockBits(new System.Drawing.Rectangle(0, 0, sourceImage.Width, sourceImage.Height), ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            int stride = sourceData.Stride;
            byte[] data = new byte[stride * sourceImage.Height];
            Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        private static void WriteIntToByteArray(byte[] data, int startIndex, int bytes, bool littleEndian, uint value)
        {
            int lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (int index = 0; index < bytes; index++)
            {
                int offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (byte)(value >> (8 * index) & 0xFF);
            }
        }

        private static uint ReadIntFromByteArray(byte[] data, int startIndex, int bytes, bool littleEndian)
        {
            int lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            uint value = 0;
            for (int index = 0; index < bytes; index++)
            {
                int offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (uint)(data[offs] << (8 * index));
            }
            return value;
        }

        public static void DrawImageSafe(this IImageProcessingContext context, Image img, int2 pos)
        {
            if (!context.Overlaps(pos, img)) return;
            context.DrawImage(img, new Point(pos.x, pos.y), 1);
        }

        /// <summary>
        /// Extends an image to contain a region
        /// </summary>
        /// <param name="image">The image to extend</param>
        /// <param name="imagePos">The position of the image</param>
        /// <param name="imageSize">The size of the image</param>
        /// <param name="regionPos">The position of the region</param>
        /// <param name="regionSize">The size of the region</param>
        /// <returns>True if extended, false otherwise</returns>
        public static bool ExtendToContain(ref Image<Argb32>? image, ref int2 imagePos, ref int2 imageSize, int2 regionPos, int2 regionSize)
        {
            if (image is null)
            {
                // Image is null
                image = new(regionSize.x, regionSize.y);
                imagePos = regionPos;
                imageSize = regionSize;
                return true;
            }

            if (ISEUtils.RectContains(imagePos, imageSize, regionPos, regionSize)) return false; // Region within bounds

            // Extend Image
            imageSize = Math2.Max(regionPos + regionSize, imageSize) - Math2.Min(regionPos, imagePos);
            int2 offset = Math2.Max(regionPos - imagePos, 0);

            var img = image.GetSubimage(offset, imageSize);
            image.Dispose();
            image = img;
            imagePos -= offset;

            return true;
        }

        /// <summary>
        /// Extends an image to contain a region
        /// </summary>
        /// <param name="image">The image to extend</param>
        /// <param name="regionPos">The position of the region</param>
        /// <param name="regionSize">The size of the region</param>
        /// <returns>True if extended, false otherwise</returns>
        public static bool ExtendToContain(ref Image<Argb32>? image, int2 regionPos, int2 regionSize)
        {
            int2 pos = int2.Zero;
            int2 size = new int2(image?.Width ?? 0, image?.Height ?? 0);
            return ExtendToContain(ref image, ref pos, ref size, regionPos, regionSize);
        }

		public static string RemoveInvalidChars(string filename)
		{
            char[] arr = Path.GetInvalidFileNameChars();
			for (int i = 0; i < arr.Length; i++)
                filename.Replace(arr[i].ToString(), "");

            arr = Path.GetInvalidPathChars();
			for (int i = 0; i < arr.Length; i++)
                filename.Replace(arr[i].ToString(), "");

			return filename;
		}

		public static int ToInt(object o)
        {
            return o switch
            {
                byte b => b,
                ushort u => u,
                int i => i,
                long l => (int)l,
                double d => (int)d,
                _ => 0
            };
        }

        public static double ToDouble(object o)
        {
            return o switch
            {
				byte b => b,
				ushort u => u,
				int i => i,
                long l => l,
                double d => d,
                _ => 0
            };
        }

        public static int2 Size(this Image img) => new int2(img.Width, img.Height);

        public static Size ToSize(this int2 n) => new Size(n.x, n.y);

        public static Point ToPoint(this int2 n) => new Point(n.x, n.y);

        public static PointF ToPointF(this double2 n) => new PointF((float)n.x, (float)n.y);

        public static int2 ToInt2(this Vector2 v) => new int2((int)Math.Round(v.X, MidpointRounding.AwayFromZero), (int)Math.Round(v.Y, MidpointRounding.AwayFromZero));

        public static Vector4 ToVector4(this Color c)
        {
            var pixel = c.ToPixel<RgbaVector>();
            return new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
        }

        public static int2 Min(this Rectangle rect) => new int2(rect.X, rect.Y);

        public static int2 Max(this Rectangle rect) => new int2(rect.X + rect.Width - 1, rect.Y + rect.Height - 1);

        public static int2 Size(this Rectangle rect) => new int2(rect.Width, rect.Height);
	}
}
