using NewMath;
using Progrimage.Undo;

namespace Progrimage.LuaDefs
{
    public class LuaUndoRegion
    {
        private readonly UndoRegion _undoAction;

        public LuaUndoRegion(Layer layer, int2 pos, int2 size)
        {
            _undoAction = new UndoRegion(layer, pos, size);
            UndoManager.AddUndo(_undoAction);
        }

        public void remove()
        {
            UndoManager.RemoveUndo(_undoAction);
        }
    }
}
