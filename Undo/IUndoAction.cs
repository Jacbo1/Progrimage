﻿namespace Progrimage.Undo
{
	public interface IUndoAction : IDisposable
	{
		public void Undo();
		public void Redo();
		public long MemorySize { get; }
	}
}
