using ImageSharpExtensions;
using NewMath;
using Progrimage.LuaDefs;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections;

namespace Progrimage.Composites
{
	internal class CompLua : LuaFileHandler, ICompositeAction, IDisposable
	{
		public Action? DisposalDelegate { get; private set; }
		public Composite Composite { get; private set; }
		public int2 Pos { get; set; }

		#region Constructors
		//public CompLua() : base("Lua Composite", Defs.LUA_COMPOSITE_PATH) { }

		public CompLua(string name) : base(name, Defs.LUA_COMPOSITE_PATH, name) { }

		public CompLua(string path, string name) : base(name, Defs.LUA_COMPOSITE_PATH + path, name) { }
		#endregion

		#region Public Methods
		public IEnumerator Run(PositionedImage<Argb32> result)
		{
			bool running = true;
			LuaManager.CallFunction("Run", () =>
			{
				running = false;
				Composite.Changed();
			}, new LuaImage(result));
			while (running && LuaManager.Error is null) yield return true;
		}

		public void Init(Composite composite)
		{
			Composite = composite;
			Composite.Name = LuaManager.Lua?["Name"] as string ?? Name;
		}

		protected override void Load(string path)
		{
			
		}

		protected override void Save()
		{
			
		}
		#endregion

		#region Private Methods


		#endregion
		protected override void LuaPreInit()
		{
			LuaManager.Lua!["Rerun"] = ((ICompositeAction)this).Rerun;
		}

		protected override void LuaPostInit()
		{
			if (Composite == null) return;
			Composite.Name = LuaManager.Lua?["Name"] as string ?? Name;
			((ICompositeAction)this).Rerun();
		}
	}
}
