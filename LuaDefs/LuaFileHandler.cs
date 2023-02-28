using NewMath;
using NLua;
using NLua.Exceptions;
using Progrimage.Utils;
using System.Diagnostics;
using System.Timers;

namespace Progrimage.LuaDefs
{
	public abstract class LuaFileHandler : IDisposable
	{
		#region Fields
		// Public
		public string Name { get; set; }
		private string Subdirectory = "";

		// Protected
		protected LuaManager LuaManager;
		protected string? FileName;
		
		// Private
		private const int FILE_DELAY = 500;
		private FileSystemWatcher _watcher;
		private System.Timers.Timer _fileTimer;
		private bool _fileUpdating;
		private string DefaultName = "LUA DEFAULT";
		#endregion

		public string? Error
		{
			get => LuaManager?.Error;
		}

		#region Constructors
		public LuaFileHandler(string defaultName, string subdirectory)
		{
			DefaultName = defaultName;
			Subdirectory = subdirectory;
			Init();
		}

		public LuaFileHandler(string defaultName, string subdirectory, string name) : this(defaultName, subdirectory)
		{
			Load(name);
		}
		#endregion

		#region Public Methods
		public void Dispose()
		{
			LuaManager?.Dispose();
			LuaManager = null;
			_watcher?.Dispose();
			_watcher = null;
		}

		public static void OpenLuaEditor(string path)
		{
			path = Directory.GetCurrentDirectory() + '\\' + Defs.LUA_BASE_PATH + path;
			if (!File.Exists(path)) File.WriteAllText(path, "");

			try
			{
				string exePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Programs\Microsoft VS Code\Code.exe";
				if (File.Exists(exePath))
				{
					Process.Start(exePath, '"' + path + '"');
					return;
				}

				exePath = @"C:\Program Files (x86)\Notepad++.exe";
				if (File.Exists(exePath))
				{
					Process.Start(exePath, '"' + path + '"');
					return;
				}

				exePath = @"notepad.exe";
				Process.Start(exePath, '"' + path + '"');
			}
			catch { }
		}
		#endregion

		#region Private Methods
		private void Init()
		{
			if (LuaManager?.Error is not null) LuaManager.Error = null;
			Name = DefaultName;
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + '\\' + Defs.LUA_BASE_PATH + Subdirectory);
			_watcher?.Dispose();
			_watcher = new FileSystemWatcher(Directory.GetCurrentDirectory() + '\\' + Defs.LUA_BASE_PATH + Subdirectory);
			_watcher.Filter = GetFileName() + ".lua";
			_watcher.Changed += FileChanged;
			_watcher.Created += FileChanged;
			_watcher.EnableRaisingEvents = true;
			InitLua();
		}

		private void FileChanged(object _, EventArgs _2)
		{
			if (_fileUpdating && _fileTimer != null)
			{
				_fileTimer.Enabled = false;
				_fileTimer.Dispose();
			}

			_fileTimer = new(FILE_DELAY);
			_fileTimer.Elapsed += FileTimerElapsed;
			_fileTimer.AutoReset = false;
			_fileTimer.Enabled = true;
			_fileUpdating = true;
		}

		private void FileTimerElapsed(object _, ElapsedEventArgs _2)
		{
			_fileUpdating = false;
            _fileTimer.Dispose();
            InitLua();
		}

		private void InitLua()
		{
			LuaManager?.Dispose();
			LuaManager = new LuaManager();

			string path = Directory.GetCurrentDirectory() + '\\' + Defs.LUA_BASE_PATH + Subdirectory + GetFileName() + ".lua";
			if (!File.Exists(path))
			{
				LuaManager.Error = $"No lua file found for \"{Defs.LUA_BASE_PATH + Subdirectory + GetFileName() + ".lua\""}";
				return;
			}

			try
			{
				LuaManager.InitLuaValues();
				LuaPreInit();
				LuaManager.Lua!.DoFile(path);
				LuaPostInit();
			}
			catch (Exception e)
			{
				LuaManager.Error = e.Message;
            }
		}

		protected virtual void LuaPreInit() { }
		protected virtual void LuaPostInit() { }

		protected void SetFilename(string path)
		{
			FileName = path;
			_watcher.Filter = GetFileName() + ".lua";
			_watcher.NotifyFilter = NotifyFilters.LastWrite;
			InitLua();
		}

		protected string GetFileName()
		{
			bool renameFiles = false;
			string safeName = Util.RemoveInvalidChars(Name);
			string oldFile = FileName;
			if (FileName is not null && !FileName.StartsWith(safeName))
			{
				// Name has been changed
				renameFiles = true;
				FileName = null;
			}

			if (FileName is null)
			{
				FileName = safeName;
				int i = 1;
				while (File.Exists(FileName + ".xml") || File.Exists(FileName + ".lua"))
				{
					FileName = safeName + "_" + i;
					i++;
				}
				SetFilename(FileName);
			}

			if (!renameFiles) return FileName;

			// Rename file because the Name has changed
			string oldPath = Defs.LUA_BASE_PATH + Subdirectory + oldFile;
			string newPath = Defs.LUA_BASE_PATH + Subdirectory + FileName;
			if (File.Exists(oldPath + ".xml")) File.Move(oldPath + ".xml", newPath + ".xml");
			if (File.Exists(oldPath + ".lua")) File.Move(oldPath + ".lua", newPath + ".lua");
			InitLua();

			return FileName;
		}
		#endregion

		#region Abstract Methods
		protected abstract void Save();
		protected abstract void Load(string path);
		#endregion
	}
}
