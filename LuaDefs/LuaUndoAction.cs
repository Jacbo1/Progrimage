using NLua;
using Progrimage.Undo;

namespace Progrimage.LuaDefs
{
    public class LuaUndoAction
    {
        private readonly UndoAction _undoAction;
        public LuaFunction redo, undo;

        public LuaUndoAction(LuaFunction undo, LuaFunction redo)
        {
            this.redo = redo;
            this.undo = undo;
            _undoAction = new UndoAction(() => this.undo?.Call(), () => this.redo?.Call());
            UndoManager.AddUndo(_undoAction);
        }

        public void remove()
        {
            UndoManager.RemoveUndo(_undoAction);
        }
    }
}
