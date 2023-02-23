//using System.Runtime.CompilerServices;

//namespace Progrimage.Utils
//{
//	internal static class IDManager
//	{
//		private static Queue<int> _availableIDs = new();
//		private static int _counter;
//		private static Dictionary<string, int> _ids = new();
//		private static Dictionary<object, int> _objectIDs = new();

//		public static int GetID([CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
//		{
//			string key = path + line;
//			if (_ids.TryGetValue(key, out int value))
//				return value;

//			int newID = GetNewID();
//			_ids.Add(key, newID);
//			return newID;
//		}

//		public static int GetID(int identifier, [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
//		{
//			string key = path + line + "_" + identifier;
//			if (_ids.TryGetValue(key, out int value))
//				return value;

//			int newID = GetNewID();
//			_ids.Add(key, newID);
//			return newID;
//		}

//		public static int GetID(object o)
//		{
//			if (_objectIDs.TryGetValue(o, out int value))
//				return value;

//			int newID = GetNewID();
//			_objectIDs.Add(o, newID);
//			return newID;
//		}

//		private static int GetNewID()
//		{
//			if (_availableIDs.TryDequeue(out int value))
//				return value;

//			_counter++;
//			return _counter;
//		}
//	}
//}
