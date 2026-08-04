#nullable enable
using System.Collections.Generic;
using SlimeCake.Systems;

namespace SlimeCake.LevelEditor.Undo
{
    /// <summary>
    /// Combines multiple editor actions into one undo and redo transaction.
    /// </summary>
    public sealed class CompositeAction : IEditorAction
    {
        private readonly List<IEditorAction> actions = new();

        public string Description => $"Composite ({this.actions.Count})";
        public bool IsEmpty => this.actions.Count == 0;
        public int Count => this.actions.Count;

        public void Add(IEditorAction action) => this.actions.Add(action);

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var anyApplied = false;
            foreach (var action in this.actions)
            {
                if (action.Apply(manager, builder)) anyApplied = true;
            }
            return anyApplied;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            for (int i = this.actions.Count - 1; i >= 0; i--)
            {
                this.actions[i].Undo(manager, builder);
            }
        }
    }
}
