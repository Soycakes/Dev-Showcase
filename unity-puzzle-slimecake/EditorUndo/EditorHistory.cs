#nullable enable
using System.Collections.Generic;
using SlimeCake.Systems;

namespace SlimeCake.LevelEditor.Undo
{
    /// <summary>
    /// Manages undo and redo history for the level editor.
    /// </summary>
    public sealed class EditorHistory
    {
        private readonly LinkedList<IEditorAction> undoList = new();
        private readonly Stack<IEditorAction> redoStack = new();

        private readonly LevelManager manager;
        private readonly LevelBuilder builder;
        private readonly int maxSize;

        public int UndoCount => this.undoList.Count;
        public int RedoCount => this.redoStack.Count;
        public bool CanUndo => this.undoList.Count > 0;
        public bool CanRedo => this.redoStack.Count > 0;

        public EditorHistory(LevelManager manager, LevelBuilder builder, int maxSize = 200)
        {
            this.manager = manager;
            this.builder = builder;
            this.maxSize = maxSize;
        }

        public void RecordApplied(IEditorAction action)
        {
            // Do not record history during playtesting.
            if (this.manager.CurrentMode == LevelManager.Mode.Play) return;
            this.undoList.AddLast(action);
            this.redoStack.Clear();
            this.TrimToMax();
        }

        public bool Undo()
        {
            if (this.undoList.Count == 0) return false;
            var action = this.undoList.Last!.Value;
            this.undoList.RemoveLast();
            action.Undo(this.manager, this.builder);
            this.redoStack.Push(action);
            return true;
        }

        public bool Redo()
        {
            if (this.redoStack.Count == 0) return false;
            var action = this.redoStack.Pop();
            action.Apply(this.manager, this.builder);
            this.undoList.AddLast(action);
            return true;
        }

        public void Clear()
        {
            this.undoList.Clear();
            this.redoStack.Clear();
        }

        private void TrimToMax()
        {
            while (this.undoList.Count > this.maxSize)
            {
                this.undoList.RemoveFirst();
            }
        }
    }
}
