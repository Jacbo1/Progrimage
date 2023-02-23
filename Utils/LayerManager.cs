using Xna = Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Point = SixLabors.ImageSharp.Point;
using Color = SixLabors.ImageSharp.Color;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using static Progrimage.LuaDefs.LuaManager;
using NewMath;
using ImageSharpExtensions;
using Progrimage.LuaDefs;

namespace Progrimage.Utils
{
    public class LayerManager
    {
        public List<Layer> Layers { get; private set; }
        public List<LuaLayer> LuaLayers { get; private set; }
        private Instance _instance;

        public LayerManager(Instance instance)
        {
            Layers = new();
            LuaLayers = new();
            _instance = instance;
        }

        public PositionedImage<Argb32> Merge(List<Layer> layers, int x, int y, int width, int height)
        {
			PositionedImage<Argb32> image = new(new int2(x, y), new int2(width, height));

            for (int j = 0; j < layers.Count; j++)
            {
                Layer layer = layers[j];
                if (layer.Hidden) continue;

				PositionedImage<Argb32> tex;
                int2 pos;
                if (!layer.ProcessingComposites && layer.CompositeResult.Image is not null)
                    tex = new(layer.CompositeResult.RealPos + layer.Image.RealPos, layer.CompositeResult.Image);
                else tex = new(layer.Image.RealPos, layer.Image.Image);

                bool cloned = false;
                if (layer == Program.ActiveInstance.Stroke.Layer)
                {
                    // Draw brush stroke
                    cloned = true;
                    var img = tex.Clone();
                    if (Program.ActiveInstance.BrushMode == BrushMode.Eraser)
                        Program.ActiveInstance.Stroke.Erase(img);
                    else Program.ActiveInstance.Stroke.Draw(img, true);
                    tex = img;
                }

                if (tex.Image is not null)
                {
                    image.DrawOver(tex);
                    if (cloned) tex.Dispose();
                }
            }

            return image;
        }

        public PositionedImage<Argb32> Merge() => Merge(Layers, 0, 0, _instance.CanvasSize.x, _instance.CanvasSize.y);
        public PositionedImage<Argb32> Merge(int2 pos, int2 size) => Merge(Layers, pos.x, pos.y, size.x, size.y);

        public void DrawTexture2D(Xna.Texture2D tex, Image<Rgb24> background, List<Layer>? layers = null)
        {
            const int TILE_SIZE = 16;
            using var merged = Merge(layers ?? Layers, 0, 0, tex.Width, tex.Height);
            using var img = new Image<Rgb24>(tex.Width, tex.Height);
            Color c1 = Color.White;
            Color c2 = Color.LightGray;
            for (int x = 0; x < tex.Width / TILE_SIZE; x++)
            {
                for (int y = 0; y < tex.Height / TILE_SIZE; y++)
                {
                    img.Mutate(i => i.Fill((x + y) % 2 == 0 ? c1 : c2, new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE)));
                }
            }

            //img.Mutate(x => x.DrawImage(merged, 1));
            img.DrawOver(merged, int2.Zero);
            Util.DrawImageToTexture2D(tex, img);
        }

        public void Remove(Layer layer)
        {
            int i = Layers.IndexOf(layer);
            if (i == -1) return;

            Layers.RemoveAt(i);
            LuaLayers.RemoveAt(i);
        }

        public void Remove(LuaLayer layer)
        {
            int i = LuaLayers.IndexOf(layer);
            if (i == -1) return;

            Layers.RemoveAt(i);
            LuaLayers.RemoveAt(i);
        }

        public void RemoveAt(int i)
        {
            Layers.RemoveAt(i);
            LuaLayers.RemoveAt(i);
        }

        public void Add(Layer layer)
        {
            Layers.Add(layer);
            LuaLayers.Add(new LuaLayer(layer));
        }

        public void Insert(int i, Layer layer)
        {
            Layers.Insert(i, layer);
            LuaLayers.Insert(i, new LuaLayer(layer));
        }
    }
}
