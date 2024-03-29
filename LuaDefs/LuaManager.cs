﻿using NewMath;
using NLua;
using Progrimage.CoroutineUtils;
using Progrimage.Utils;
using System.Numerics;

namespace Progrimage.LuaDefs
{
    public class LuaManager : IDisposable
    {
        private unsafe struct FuncCallItem
		{
            public string FuncName;
            public object[] Args;
            public Action? Callback;

			public FuncCallItem(string funcName, object[] args)
			{
				FuncName = funcName;
				Args = args;
                Callback = null;
			}

			public FuncCallItem(string funcName, Action callback, object[] args) : this(funcName, args)
            {
                Callback = callback;
            }
		}

        public static LuaManager Current;
		public string? Error;
		public Lua? Lua;
        private LuaFunction _createVector2Func, _createVector3Func, _createVector4Func, _callTimersFunc;
        private LuaFunction? _currentCoroutine;
		private Queue<FuncCallItem> _funcCallQueue = new();

        #region Constructors
        public LuaManager()
        {
            Init();
        }
        #endregion

        #region Public Methods
        public bool CallFunction(string funcName, params object[] args)
        {
            if (Error is not null) return true;
            _funcCallQueue.Enqueue(new FuncCallItem(funcName, args));
            return !ProcessFuncCallQueue();
		}

        public bool CallFunction(string funcName, Action callback, params object[] args)
        {
            if (Error is not null) return true;
            _funcCallQueue.Enqueue(new FuncCallItem(funcName, callback, args));
            return !ProcessFuncCallQueue();
		}

		/// <summary>
		/// Resumes the active coroutine if there is one
		/// </summary>
		/// <returns>True if processing a coroutine</returns>
		public bool TryResumeCoroutine()
        {
			if (Error is not null || Lua is null || JobQueue.ShouldYield) return true;
			if (_currentCoroutine is null) return false;
            FuncCallItem item;
			try
            {
				Lua["timer.calltime"] = Util.Time / 1000.0;
				object[] result = _currentCoroutine.Call();
                if (result.Length == 0 || result[0] is not bool || !(bool)result[0]) return true;
                _currentCoroutine = null;
			}
			catch (Exception e)
			{
                Error ??= e.Message;
			}
			if (_funcCallQueue.TryDequeue(out item)) item.Callback?.Invoke();
			return false;
        }

		/// <summary>
		/// Processes the function call queue.
		/// </summary>
		/// <returns>True if processing queue or coroutine</returns>
		public bool ProcessFuncCallQueue()
        {
            if (Lua is null || _currentCoroutine is not null) return true;
			while (Error is null && _currentCoroutine is null && !JobQueue.ShouldYield && _funcCallQueue.TryPeek(out FuncCallItem item))
            {
				try
                {
                    if (Lua[item.FuncName] as LuaFunction is not null)
                    {
                        Lua["timer.calltime"] = Util.Time / 1000.0;
						_currentCoroutine = (LuaFunction)Lua.DoString("return coroutine.wrap(function(...) " + item.FuncName + "(...) return true end)").First();
                        object[] result = _currentCoroutine.Call(item.Args);
                        if (result.Length == 0 || result[0] is not bool || !(bool)result[0]) return true;
                        _currentCoroutine = null;
                    }
				}
                catch (Exception e)
                {
                    Error ??= e.Message;
                }
                item.Callback?.Invoke();
				_funcCallQueue.Dequeue();
			}
			return _currentCoroutine is null || Error is not null || !_funcCallQueue.Any();
		}

        public void InitLuaValues()
        {
            Lua!["timer.deltaTime"] = MainWindow.IO.DeltaTime;
			double curtime = Util.Time / 1000.0;
			Lua["timer.frameTime"] = curtime;
			Lua["timer.calltime"] = curtime;
			Lua.DoString("timer.clockFrameTime = os.clock()");
		}

		public void Dispose()
        {
			MainWindow.OnPreUpdate -= PreUpdate;
			Lua?.Dispose();
            Lua = null;
        }
        #endregion

        #region Private Methods
        private void PreUpdate(object _, EventArgs _2)
        {
            if (Error is not null || Lua is null) return;
            try
            {
                Lua["timer.frameTime"] = MainWindow.IO.DeltaTime;
                Lua["timer.frameStartTime"] = Util.Time / 1000.0;
                Lua.DoString("timer.frameStartTimeClock = os.clock()");
                _callTimersFunc.Call();
                TryResumeCoroutine();
                ProcessFuncCallQueue();
            }
            catch (Exception e)
            {
                Error ??= e.Message;
            }
        }

        private void Init()
        {
            Lua?.Dispose();
			Lua = new Lua();
            Current = this;
            Lua.DoFile(@"LuaDefs\Index.lua");
            _callTimersFunc = (LuaFunction)Lua.DoFile(@"LuaDefs\Timer.lua")[0];
			_createVector2Func = (LuaFunction)Lua["vec2"];
			_createVector3Func = (LuaFunction)Lua["vec3"];
			_createVector4Func = (LuaFunction)Lua["vec4"];
            Lua["input.getMousePosCanvas"] = () => CreateVector2(MainWindow.MousePosCanvasDouble);
            Lua["input.getMousePosScreen"] = () => CreateVector2(MainWindow.MousePosScreen);
            Lua["input.isMouseDown"] = () => MainWindow.IsDragging;
            Lua["input.isMouse2Down"] = () => MainWindow.IsDragging2;
            Lua["input.isMouseInCanvas"] = () => MainWindow.MouseInCanvas;
            Lua["render.getActiveLayer"] = () => Program.ActiveInstance?.ActiveLuaLayer;
            Lua["render.getStrokeColor"] = () => CreateVector4(Program.ActiveInstance.Stroke.BrushState.Color * 255);
            Lua["render.setStrokeColor"] = (Action<LuaTable>)(color =>
            {
				Vector4 vec = ToColor(color) / 255;
				BrushState brushState = Program.ActiveInstance.Stroke.BrushState;
				brushState.Color = vec;
				Program.ActiveInstance.Stroke.BrushState = brushState;
				Program.ActiveInstance.Stroke.BrushStateChanged();
			});
            Lua["render.setStrokeSize"] = (Action<int>)(brushSize =>
            {
				BrushState brushState = Program.ActiveInstance.Stroke.BrushState;
				brushState.Size = brushSize;
				Program.ActiveInstance.Stroke.BrushState = brushState;
				Program.ActiveInstance.Stroke.BrushStateChanged();
			});
            Lua["render.getStrokeSize"] = () => Program.ActiveInstance.Stroke.Size;
            Lua["render.endStroke"] = (Action<bool>) (erase =>
            {
                if (Program.ActiveInstance.ActiveLayer == null) return;
                if (erase) Program.ActiveInstance?.Stroke.Erase(Program.ActiveInstance.ActiveLayer.Image);
				else Program.ActiveInstance?.Stroke.Draw(Program.ActiveInstance.ActiveLayer.Image, true);
            });
            Lua["render.setActiveLayer"] = (Action<LuaLayer>) (layer => Program.ActiveInstance.ActiveLayer = layer.Layer);
            Lua["render.beginStroke"] = (Action<LuaTable>) (pos => Program.ActiveInstance?.ActiveLayer?.BrushDown(ToDouble2(pos)));
            Lua["render.continueStroke"] = (Action<LuaTable>) (pos => Program.ActiveInstance?.ActiveLayer?.MoveBrush(ToDouble2(pos)));
            Lua["render.createLayer"] = (Func<int, int, LuaLayer>) ((w, h) => new LuaLayer(w, h));
            Lua["render.getCanvasPos"] = () => CreateVector2(MainWindow.CanvasOriginDouble);
            Lua["render.getCanvasSize"] = () => CreateVector2(Program.ActiveInstance.CanvasSize);
            Lua["render.getZoom"] = () => CreateVector2(Program.ActiveInstance.Zoom);
            Lua["render.createImage"] = (Func<int, int, LuaImage>) ((w, h) => new LuaImage(0, 0, w, h));
            Lua["render.update"] = () => Program.ActiveInstance.Changed();

            Lua["util.createUndoAction"] = (Func<LuaFunction, LuaFunction, LuaUndoAction>) ((undo, redo) => new LuaUndoAction(undo, redo));
            Lua["util.createUndoRegion"] = (Func<LuaLayer, LuaTable, LuaTable, LuaUndoRegion>) ((layer, pos, size) => new LuaUndoRegion(layer.Layer, ToInt2(pos), ToInt2(size)));

            InitLuaValues();
			MainWindow.OnPreUpdate += PreUpdate!;
		}

        #region Vector
        public LuaTable CreateVector2(double2 n)
        {
            return (LuaTable)_createVector2Func?.Call(n.X, n.Y)?.First();
		}

        public LuaTable CreateVector2(double x, double y)
        {
            return (LuaTable)_createVector2Func?.Call(x, y)?.First();
		}

        public LuaTable CreateVector3(double3 n)
        {
            return (LuaTable)_createVector3Func?.Call(n.X, n.Y, n.Z)?.First();
		}

        public LuaTable CreateVector3(double x, double y, double z)
        {
            return (LuaTable)_createVector3Func?.Call(x, y, z)?.First();
		}

        public LuaTable CreateVector4(double4 n)
        {
            return (LuaTable)_createVector4Func?.Call(n.X, n.Y, n.Z, n.W)?.First();
		}

        public LuaTable CreateVector4(double x, double y, double z, double w)
        {
            return (LuaTable)_createVector4Func?.Call(x, y, z, w)?.First();
		}

        public static double4 ToColor(LuaTable color)
        {
            double4 c = default;
            var values = color.Values.GetEnumerator();
            if (!values.MoveNext()) return c;

            // R
            c.X = Util.ToDouble(values.Current);
            if (values.MoveNext())
            {
                // G
                c.Y = Util.ToDouble(values.Current);
                if (values.MoveNext())
                {
                    // B
                    c.Z = Util.ToDouble(values.Current);
                    c.W = values.MoveNext() ? Util.ToDouble(values.Current) : 255;
                }
                else c.W = 255; // No B
            }
            else
            {
                // No G
                c.Y = c.Z = c.X;
                c.W = 255;
            }

            return c;
        }

        public static double2 ToDouble2(LuaTable vec)
        {
            double2 v = default;
            var values = vec.Values.GetEnumerator();
            if (!values.MoveNext()) return v; // No X

            v.X = Util.ToDouble(values.Current); // X
            if (values.MoveNext()) v.Y = Util.ToDouble(values.Current); // Y
            else v.Y = v.X; // No Y

            return v;
        }

        public static int2 ToInt2(LuaTable vec)
        {
            int2 v = default;
            var values = vec.Values.GetEnumerator();
            if (!values.MoveNext()) return v; // No X

            v.X = Util.ToInt(values.Current); // X
            if (values.MoveNext()) v.Y = Util.ToInt(values.Current); // Y
            else v.Y = v.X; // No Y

            return v;
        }
        #endregion
        #endregion
    }
}
