using System.Diagnostics.Tracing;

namespace Progrimage.Undo
{
	public class UndoAction : IUndoAction
	{
		public Action UndoDelegate, RedoDelegate;
		public EventHandler Disposed;

		public long MemorySize { get; set; }

		public UndoAction(Action undoAction, Action redoAction)
		{
			MemorySize = sizeof(long) + 50;
			UndoDelegate = undoAction;
			RedoDelegate = redoAction;
		}

		public void Undo() => UndoDelegate();

		public void Redo() => RedoDelegate();

		public void Dispose() => Disposed?.Invoke(this, EventArgs.Empty);
	}
}
