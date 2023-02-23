namespace Progrimage.Undo
{
	public interface IRedoAction : IDisposable
	{
		public void Undo();
		public void Redo();
		public long MemorySize { get; }
	}
}
