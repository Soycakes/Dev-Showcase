#nullable enable
using System.Collections.Generic;
using SlimeCake.Core;
using SlimeCake.LevelEditor.Selection;
using SlimeCake.LevelEditor.Tools;
using SlimeCake.Systems;
using UnityEngine;

namespace SlimeCake.LevelEditor.Undo
{
    /// <summary>
    /// Level editor actions for tiles, items, tools, and selections.
    /// Tons of boilerplate incoming...
    /// </summary>
    public sealed class PlaceTileAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private readonly int tileId;
        private readonly int extraData;
        private bool applied;

        public PlaceTileAction(Vector2Int gridPos, int tileId, int extraData)
        {
            this.gridPos = gridPos;
            this.tileId = tileId;
            this.extraData = extraData;
        }

        public string Description => "Place tile";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!builder.PlaceTile(this.gridPos, this.tileId, this.extraData)) return false;
            manager.LiveData.tiles.Add(new TilePlacement
            {
                tileId = this.tileId,
                gridPos = this.gridPos,
                extraData = this.extraData
            });
            manager.MarkDirty();
            this.applied = true;
            return true;
        }

        // Undo placement only if it succeeded.
        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            builder.RemoveTile(this.gridPos);
            manager.LiveData.tiles.RemoveAll(t => t.gridPos == this.gridPos);
            manager.MarkDirty();
        }
    }

    public sealed class RemoveTileAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private bool wasFound;
        private int removedIndex;
        private TilePlacement removed;

        public RemoveTileAction(Vector2Int gridPos) { this.gridPos = gridPos; }

        public string Description => "Remove tile";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var idx = manager.LiveData.tiles.FindIndex(t => t.gridPos == this.gridPos);
            if (idx < 0) return false;
            this.wasFound = true;
            this.removedIndex = idx;
            this.removed = manager.LiveData.tiles[idx];
            builder.RemoveTile(this.gridPos);
            manager.LiveData.tiles.RemoveAt(idx);
            manager.MarkDirty();
            return true;
        }

        // Reinsert at original index to keep list order.
        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.wasFound) return;
            builder.PlaceTile(this.removed.gridPos, this.removed.tileId, this.removed.extraData);
            var insertAt = Mathf.Min(this.removedIndex, manager.LiveData.tiles.Count);
            manager.LiveData.tiles.Insert(insertAt, this.removed);
            manager.MarkDirty();
        }
    }

    public sealed class PlaceFoodAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private readonly int foodId;
        private readonly int extraData;
        private bool applied;

        public PlaceFoodAction(Vector2Int gridPos, int foodId, int extraData)
        {
            this.gridPos = gridPos;
            this.foodId = foodId;
            this.extraData = extraData;
        }

        public string Description => "Place food";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!builder.PlaceFood(this.gridPos, this.foodId)) return false;
            manager.LiveData.foods.Add(new FoodPlacement
            {
                foodId = this.foodId,
                gridPos = this.gridPos,
                extraData = this.extraData
            });
            manager.MarkDirty();
            this.applied = true;
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            builder.RemoveFood(this.gridPos);
            manager.LiveData.foods.RemoveAll(f => f.gridPos == this.gridPos);
            manager.MarkDirty();
        }
    }

    public sealed class RemoveFoodAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private bool wasFound;
        private int removedIndex;
        private FoodPlacement removed;

        public RemoveFoodAction(Vector2Int gridPos) { this.gridPos = gridPos; }

        public string Description => "Remove food";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var idx = manager.LiveData.foods.FindIndex(f => f.gridPos == this.gridPos);
            if (idx < 0) return false;
            this.wasFound = true;
            this.removedIndex = idx;
            this.removed = manager.LiveData.foods[idx];
            builder.RemoveFood(this.gridPos);
            manager.LiveData.foods.RemoveAt(idx);
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.wasFound) return;
            builder.PlaceFood(this.removed.gridPos, this.removed.foodId);
            var insertAt = Mathf.Min(this.removedIndex, manager.LiveData.foods.Count);
            manager.LiveData.foods.Insert(insertAt, this.removed);
            manager.MarkDirty();
        }
    }

    public sealed class PlaceHazardAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private readonly int hazardId;
        private readonly GridDirection facing;
        private readonly int extraData;
        private bool applied;

        public PlaceHazardAction(Vector2Int gridPos, int hazardId, GridDirection facing, int extraData)
        {
            this.gridPos = gridPos;
            this.hazardId = hazardId;
            this.facing = facing;
            this.extraData = extraData;
        }

        public string Description => "Place hazard";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!builder.PlaceHazard(this.gridPos, this.hazardId, this.facing, this.extraData)) return false;
            manager.LiveData.hazards.Add(new HazardPlacement
            {
                hazardId = this.hazardId,
                gridPos = this.gridPos,
                facing = this.facing,
                extraData = this.extraData
            });
            manager.MarkDirty();
            this.applied = true;
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            builder.RemoveHazard(this.gridPos);
            manager.LiveData.hazards.RemoveAll(h => h.gridPos == this.gridPos);
            manager.MarkDirty();
        }
    }

    public sealed class RemoveHazardAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private bool wasFound;
        private int removedIndex;
        private HazardPlacement removed;

        public RemoveHazardAction(Vector2Int gridPos) { this.gridPos = gridPos; }

        public string Description => "Remove hazard";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var idx = manager.LiveData.hazards.FindIndex(h => h.gridPos == this.gridPos);
            if (idx < 0) return false;
            this.wasFound = true;
            this.removedIndex = idx;
            this.removed = manager.LiveData.hazards[idx];
            builder.RemoveHazard(this.gridPos);
            manager.LiveData.hazards.RemoveAt(idx);
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.wasFound) return;
            builder.PlaceHazard(this.removed.gridPos, this.removed.hazardId, this.removed.facing, this.removed.extraData);
            var insertAt = Mathf.Min(this.removedIndex, manager.LiveData.hazards.Count);
            manager.LiveData.hazards.Insert(insertAt, this.removed);
            manager.MarkDirty();
        }
    }

    public sealed class PlacePlatformAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private readonly int platformId;
        private readonly GridDirection facing;
        private readonly int extraData;
        private bool applied;

        public PlacePlatformAction(Vector2Int gridPos, int platformId, GridDirection facing, int extraData)
        {
            this.gridPos = gridPos;
            this.platformId = platformId;
            this.facing = facing;
            this.extraData = extraData;
        }

        public string Description => "Place platform";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!builder.PlacePlatform(this.gridPos, this.platformId, this.facing, this.extraData)) return false;
            manager.LiveData.platforms.Add(new PlatformPlacement
            {
                platformId = this.platformId,
                gridPos = this.gridPos,
                facing = this.facing,
                extraData = this.extraData
            });
            manager.MarkDirty();
            this.applied = true;
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            builder.RemovePlatform(this.gridPos);
            manager.LiveData.platforms.RemoveAll(p => p.gridPos == this.gridPos);
            manager.MarkDirty();
        }
    }

    public sealed class RemovePlatformAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private bool wasFound;
        private int removedIndex;
        private PlatformPlacement removed;

        public RemovePlatformAction(Vector2Int gridPos) { this.gridPos = gridPos; }

        public string Description => "Remove platform";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var idx = manager.LiveData.platforms.FindIndex(p => p.gridPos == this.gridPos);
            if (idx < 0) return false;
            this.wasFound = true;
            this.removedIndex = idx;
            this.removed = manager.LiveData.platforms[idx];
            builder.RemovePlatform(this.gridPos);
            manager.LiveData.platforms.RemoveAt(idx);
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.wasFound) return;
            builder.PlacePlatform(this.removed.gridPos, this.removed.platformId, this.removed.facing, this.removed.extraData);
            var insertAt = Mathf.Min(this.removedIndex, manager.LiveData.platforms.Count);
            manager.LiveData.platforms.Insert(insertAt, this.removed);
            manager.MarkDirty();
        }
    }

    public sealed class PlaceKeystoneAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private readonly int linkedFoodId;
        private readonly int extraData;
        private bool applied;

        public PlaceKeystoneAction(Vector2Int gridPos, int linkedFoodId, int extraData)
        {
            this.gridPos = gridPos;
            this.linkedFoodId = linkedFoodId;
            this.extraData = extraData;
        }

        public string Description => "Place keystone";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!builder.PlaceKeystone(this.gridPos, this.linkedFoodId, this.extraData)) return false;
            manager.LiveData.keystones.Add(new KeystonePlacement
            {
                requiredFoodId = this.linkedFoodId,
                gridPos = this.gridPos,
                extraData = this.extraData
            });
            manager.MarkDirty();
            this.applied = true;
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            builder.RemoveKeystone(this.gridPos);
            manager.LiveData.keystones.RemoveAll(k => k.gridPos == this.gridPos);
            manager.MarkDirty();
        }
    }

    public sealed class RemoveKeystoneAction : IEditorAction
    {
        private readonly Vector2Int gridPos;
        private bool wasFound;
        private int removedIndex;
        private KeystonePlacement removed;

        public RemoveKeystoneAction(Vector2Int gridPos) { this.gridPos = gridPos; }

        public string Description => "Remove keystone";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var idx = manager.LiveData.keystones.FindIndex(k => k.gridPos == this.gridPos);
            if (idx < 0) return false;
            this.wasFound = true;
            this.removedIndex = idx;
            this.removed = manager.LiveData.keystones[idx];
            builder.RemoveKeystone(this.gridPos);
            manager.LiveData.keystones.RemoveAt(idx);
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.wasFound) return;
            builder.PlaceKeystone(this.removed.gridPos, this.removed.requiredFoodId, this.removed.extraData);
            var insertAt = Mathf.Min(this.removedIndex, manager.LiveData.keystones.Count);
            manager.LiveData.keystones.Insert(insertAt, this.removed);
            manager.MarkDirty();
        }
    }

    public sealed class SetCakeAction : IEditorAction
    {
        private readonly Vector2Int newPos;
        private bool prevHadCake;
        private Vector2Int prevPos;

        public SetCakeAction(Vector2Int newPos) { this.newPos = newPos; }

        public string Description => "Set cake";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            this.prevHadCake = manager.LiveData.hasCake;
            this.prevPos = manager.LiveData.cakePosition;
            if (this.prevHadCake && this.prevPos == this.newPos) return false;
            builder.SetCake(this.newPos);
            manager.LiveData.hasCake = true;
            manager.LiveData.cakePosition = this.newPos;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (this.prevHadCake)
            {
                builder.SetCake(this.prevPos);
                manager.LiveData.hasCake = true;
                manager.LiveData.cakePosition = this.prevPos;
            }
            else
            {
                builder.RemoveCake();
                manager.LiveData.hasCake = false;
            }
            manager.MarkDirty();
        }
    }

    public sealed class RemoveCakeAction : IEditorAction
    {
        private bool prevHadCake;
        private Vector2Int prevPos;

        public string Description => "Remove cake";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!manager.LiveData.hasCake) return false;
            this.prevHadCake = true;
            this.prevPos = manager.LiveData.cakePosition;
            builder.RemoveCake();
            manager.LiveData.hasCake = false;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.prevHadCake) return;
            builder.SetCake(this.prevPos);
            manager.LiveData.hasCake = true;
            manager.LiveData.cakePosition = this.prevPos;
            manager.MarkDirty();
        }
    }

    public sealed class SetPlayerSpawnAction : IEditorAction
    {
        private readonly Vector2Int newPos;
        private bool prevHadSpawn;
        private Vector2Int prevPos;

        public SetPlayerSpawnAction(Vector2Int newPos) { this.newPos = newPos; }

        public string Description => "Set player spawn";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            this.prevHadSpawn = manager.LiveData.hasPlayerSpawn;
            this.prevPos = manager.LiveData.playerSpawn;
            if (this.prevHadSpawn && this.prevPos == this.newPos) return false;
            builder.SetPlayerSpawn(this.newPos);
            manager.LiveData.hasPlayerSpawn = true;
            manager.LiveData.playerSpawn = this.newPos;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (this.prevHadSpawn)
            {
                builder.SetPlayerSpawn(this.prevPos);
                manager.LiveData.hasPlayerSpawn = true;
                manager.LiveData.playerSpawn = this.prevPos;
            }
            else
            {
                builder.RemovePlayerSpawn();
                manager.LiveData.hasPlayerSpawn = false;
            }
            manager.MarkDirty();
        }
    }

    public sealed class RemovePlayerSpawnAction : IEditorAction
    {
        private bool prevHadSpawn;
        private Vector2Int prevPos;

        public string Description => "Remove player spawn";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (!manager.LiveData.hasPlayerSpawn) return false;
            this.prevHadSpawn = true;
            this.prevPos = manager.LiveData.playerSpawn;
            builder.RemovePlayerSpawn();
            manager.LiveData.hasPlayerSpawn = false;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.prevHadSpawn) return;
            builder.SetPlayerSpawn(this.prevPos);
            manager.LiveData.hasPlayerSpawn = true;
            manager.LiveData.playerSpawn = this.prevPos;
            manager.MarkDirty();
        }
    }

    public sealed class SetCameraFrameAction : IEditorAction
    {
        private readonly Vector2 beforeCenter;
        private readonly float beforeHeight;
        private readonly Vector2 afterCenter;
        private readonly float afterHeight;

        public SetCameraFrameAction(Vector2 beforeCenter, float beforeHeight, Vector2 afterCenter, float afterHeight)
        {
            this.beforeCenter = beforeCenter;
            this.beforeHeight = beforeHeight;
            this.afterCenter = afterCenter;
            this.afterHeight = afterHeight;
        }

        public string Description => "Set camera frame";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var data = manager.LiveData;
            if (Mathf.Approximately(data.cameraCenter.x, this.afterCenter.x)
                && Mathf.Approximately(data.cameraCenter.y, this.afterCenter.y)
                && Mathf.Approximately(data.cameraHeight, this.afterHeight))
            {
                return false;
            }
            data.cameraCenter = this.afterCenter;
            data.cameraHeight = this.afterHeight;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            var data = manager.LiveData;
            data.cameraCenter = this.beforeCenter;
            data.cameraHeight = this.beforeHeight;
            manager.MarkDirty();
        }
    }

    public sealed class SetBackgroundAction : IEditorAction
    {
        private readonly int before;
        private readonly int after;

        public SetBackgroundAction(int before, int after) { this.before = before; this.after = after; }

        public string Description => "Set background";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var data = manager.LiveData;
            if (data.backgroundId == this.after) return false;
            data.backgroundId = this.after;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            manager.LiveData.backgroundId = this.before;
            manager.MarkDirty();
        }
    }

    public sealed class SetBackgroundOffsetAction : IEditorAction
    {
        private readonly Vector2 before;
        private readonly Vector2 after;

        public SetBackgroundOffsetAction(Vector2 before, Vector2 after) { this.before = before; this.after = after; }

        public string Description => "Set background offset";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            var data = manager.LiveData;
            if (Mathf.Approximately(data.backgroundOffset.x, this.after.x)
                && Mathf.Approximately(data.backgroundOffset.y, this.after.y))
            {
                return false;
            }
            data.backgroundOffset = this.after;
            manager.MarkDirty();
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            manager.LiveData.backgroundOffset = this.before;
            manager.MarkDirty();
        }
    }

    // Revert tool switch on undo.
    public sealed class ChangeToolAction : IEditorAction
    {
        private readonly EditorController editor;
        private readonly ToolKind from;
        private readonly ToolKind to;
        private bool applied;

        public ChangeToolAction(EditorController editor, ToolKind from, ToolKind to)
        {
            this.editor = editor;
            this.from = from;
            this.to = to;
        }

        public string Description => $"Change tool {this.from} to {this.to}";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (this.editor.ToolManager == null) return false;
            if (this.editor.ToolManager.Active.Kind == this.to) return false;
            this.editor.ActivateToolSilent(this.to);
            this.applied = true;
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            if (!this.applied) return;
            this.editor.ActivateToolSilent(this.from);
        }
    }

    // Track selection changes.
    public sealed class ChangeSelectionAction : IEditorAction
    {
        private readonly EditorSelection selection;
        private readonly HashSet<Vector2Int> before;
        private readonly HashSet<Vector2Int> after;

        public ChangeSelectionAction(EditorSelection selection, IEnumerable<Vector2Int> before, IEnumerable<Vector2Int> after)
        {
            this.selection = selection;
            this.before = new HashSet<Vector2Int>(before);
            this.after = new HashSet<Vector2Int>(after);
        }

        public string Description => "Change selection";

        public bool Apply(LevelManager manager, LevelBuilder builder)
        {
            if (this.SelectionMatches(this.after)) return false;
            this.selection.Set(this.after, additive: false);
            return true;
        }

        public void Undo(LevelManager manager, LevelBuilder builder)
        {
            this.selection.Set(this.before, additive: false);
        }

        private bool SelectionMatches(HashSet<Vector2Int> target)
        {
            if (this.selection.Count != target.Count) return false;
            foreach (var c in this.selection.Cells)
            {
                if (!target.Contains(c)) return false;
            }
            return true;
        }
    }
}
