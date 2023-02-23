namespace Progrimage.Undo
{
	public class UndoAction : IRedoAction
	{
		private Action _undoAction, _redoAction;
		public long MemorySize { get; private set; }

		public UndoAction(Action undoAction, Action redoAction)
		{
			MemorySize = sizeof(long) + 50;
			_undoAction = undoAction;
			_redoAction = redoAction;
		}

		public void Undo() => _undoAction();

		public void Redo() => _redoAction();

		public void Dispose() { }
	}
}
