namespace Progrimage.Undo
{
    public static class UndoManager
    {
        public static long UndoHistoryMaxMemory = 100l * 1024l * 1024l;
        private static List<IUndoAction> _undoHistory = new();
        private static int _undoIndex;

        private static void ClearHistory()
        {
            _undoHistory.Clear();
            _undoIndex = 0;
        }

        public static void AddUndo(IUndoAction action)
        {
			DropRedos(_undoIndex);
			_undoIndex++;
            _undoHistory.Add(action);
        }

        public static void DropRedos(int index)
        {
            if (index < 0 || index >= _undoHistory.Count) return;
            _undoHistory.RemoveRange(index, _undoHistory.Count - index);
        }

        public static void Undo()
        {
            if (_undoIndex <= 0) return; // Nothing to undo
            _undoIndex--;
            _undoHistory[_undoIndex].Undo();
		}

        public static void Redo()
        {
            if (_undoIndex >= _undoHistory.Count) return; // Nothing to redo
            _undoHistory[_undoIndex].Redo();
			_undoIndex++;
		}

        public static void RemoveUndo(IUndoAction action)
        {
            action.Dispose();
            int index = _undoHistory.IndexOf(action);
            if (index == -1) return;
            _undoHistory.RemoveAt(index);
            if (index <= _undoIndex) _undoIndex--;
        }

        public static void RemoveUndos(IUndoAction[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                RemoveUndo(actions[i]);
            }
        }
    }
}
