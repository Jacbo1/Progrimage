using ImageSharpExtensions;
using NewMath;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace Progrimage.Undo
{
    public class UndoImagePatch: IRedoAction, IDisposable //where TPixel : unmanaged, IPixel<TPixel>
	{
		private Layer _layer;
		private PositionedImage<Argb32> _image;
		private PositionedImage<Argb32>? _redoImage;
		private int2 _layerPos, _layerSize;
		public long MemorySize { get; private set; }

		public UndoImagePatch(Layer layer, int2 pos, int2 size)
		{	
			//						  ref  int2s self
			MemorySize = sizeof(long) + 4 + 4*3 + 1;
			_layer = layer;
			_layerPos = layer.Pos;
			_layerSize = layer.Size;
			_image = layer.Image.GetPositionedSubimage(pos, size);
			if (_image.Image is null) return;
			MemorySize += _image.Image.Width * _image.Image.Height * System.Runtime.InteropServices.Marshal.SizeOf(_image.Image!.DangerousGetPixelRowMemory(0).Span[0]);
		}

		public UndoImagePatch(Layer layer, SixLabors.ImageSharp.Rectangle bounds)
		{	
			//						  ref  int2s self
			MemorySize = sizeof(long) + 4 + 4*3 + 1;
			_layer = layer;
			_layerPos = layer.Pos;
			_layerSize = layer.Size;
			_image = layer.Image.GetPositionedSubimage(new int2(bounds.X, bounds.Y), new int2(bounds.Width, bounds.Height));
			if (_image.Image is null) return;
			MemorySize += _image.Image.Width * _image.Image.Height * System.Runtime.InteropServices.Marshal.SizeOf(_image.Image!.DangerousGetPixelRowMemory(0).Span[0]);
		}

		public void Redo()
		{
			if (_redoImage is null) return;

			MemorySize -= 9; // Bytes used for RedoImage.Image ref and RedoImage.Pos
			if (_redoImage.Image is null)
			{
				_redoImage = null;
				return;
			}

			if (_layer is null)
			{
				Dispose();
				return;
			}

			_layer.Image.DrawReplace(_redoImage, true);
			MemorySize += _redoImage.Image.Width * _redoImage.Image.Height * System.Runtime.InteropServices.Marshal.SizeOf(_redoImage.Image!.DangerousGetPixelRowMemory(0).Span[0]);
			_redoImage.Dispose();
			_redoImage = null;
			_layer.Changed();
		}

		public void Undo()
		{
			if (_layer is null)
			{
				Dispose();
				return;
			}

			if (_image.Image is null)
			{
				// Make Layer.Image null
				_redoImage?.Dispose();
				_redoImage = _layer.Image.Clone();
				MemorySize += 9; // Bytes used for RedoImage.Image ref and RedoImage.Pos
				if (_redoImage.Image is not null)
					MemorySize += _redoImage.Image.Width * _redoImage.Image.Height * System.Runtime.InteropServices.Marshal.SizeOf(_redoImage.Image!.DangerousGetPixelRowMemory(0).Span[0]);
				_layer.Image.Dispose();
				_layer.Changed();
				return;
			}

			_redoImage?.Dispose();
			_redoImage = _layer.Image.GetPositionedSubimage(_image.Pos, _image.Size);
			MemorySize += 9; // Bytes used for RedoImage.Image ref and RedoImage.Pos
			if (_redoImage.Image is not null)
				MemorySize += _redoImage.Image.Width * _redoImage.Image.Height * System.Runtime.InteropServices.Marshal.SizeOf(_redoImage.Image!.DangerousGetPixelRowMemory(0).Span[0]);
			_layer.Image.Crop(_layerPos, _layerSize);
			_layer.Image.DrawReplace(_image);
			_layer.Changed();
		}

		public void Dispose()
		{
			_image.Dispose();
			_redoImage?.Dispose();
			_redoImage = null;
			MemorySize = sizeof(long) + 5 + 12 + 1;
		}
	}
}
