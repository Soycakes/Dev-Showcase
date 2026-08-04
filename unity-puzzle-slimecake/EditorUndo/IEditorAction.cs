#nullable enable
using SlimeCake.Systems;

namespace SlimeCake.LevelEditor.Undo
{
    /// <summary>
    /// Interface for level editor actions that can be applied and undone.
    /// </summary>
    public interface IEditorAction
    {
        string Description { get; }
        bool Apply(LevelManager manager, LevelBuilder builder);
        void Undo(LevelManager manager, LevelBuilder builder);
    }
}
