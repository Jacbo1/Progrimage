﻿using NewMath;
using NLua;

namespace Progrimage.LuaDefs
{
    public class LuaLayer
	{
		internal LuaImage LuaImage;
		internal Layer Layer;

		#region Properties
		public string name
		{
			get => Layer.Name;
			set => Layer.Name = value;
		}

		public LuaImage image
		{
			get => LuaImage;
			set
			{
				LuaImage = value;
				Layer.Image = LuaImage.Image;
			}
		}

		public LuaTable pos
		{
			get => image.pos;
			set => image.pos = value;
		}

		public LuaTable size
		{
			get => image.size;
			set => image.size = value;
		}

		public int width
		{
			get => image.width;
			set => image.width = value;
		}

		public int height
		{
			get => image.height;
			set => image.height = value;
        }

        public int x
        {
            get => image.x;
            set => image.x = value;
        }

        public int y
        {
            get => image.y;
            set => image.y = value;
        }
        #endregion

        #region Constructors
        public LuaLayer(Layer layer)
		{
			Layer = layer;
			image = new(layer);
		}

		public LuaLayer(int width, int height)
		{
			Layer = new Layer(Program.ActiveInstance, new int2(width, height));
			image = new(Layer);
			Program.ActiveInstance.LayerManager.Add(Layer);
		}
		#endregion

		#region Public Methods
		public void update()
		{
			Layer.Changed();
		}

		public void setActive()
		{
			Program.ActiveInstance.ActiveLayer = Layer;
		}

		public void dispose()
		{
			Layer?.Dispose();
			LuaImage?.dispose();
			Layer = null;
			LuaImage = null;
		}

		public static bool operator ==(LuaLayer a, LuaLayer b) => a.Layer == b.Layer;
		public static bool operator !=(LuaLayer a, LuaLayer b) => a.Layer != b.Layer;
		#endregion
	}
}
