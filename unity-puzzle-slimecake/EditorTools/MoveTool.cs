#nullable enable
using System.Collections.Generic;
using SlimeCake.Core;
using SlimeCake.LevelEditor.Undo;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCake.LevelEditor.Tools
{
    /// <summary>
    /// Handles dragging selections in the grid, updating ghost renders, and committing transactions.
    /// </summary>
    public sealed class MoveTool : ITool
    {
        public ToolKind Kind => ToolKind.Move;
        public Texture2D? Cursor { get; set; }
        public Vector2 CursorHotspot { get; set; }
        public string HintText => "LMB drag move (selection or single cell), Esc cancel";

        private GameObject? ghostRoot;
        private readonly List<EntityRecord> records = new();
        private readonly HashSet<Vector2Int> capturedCells = new();
        private Vector2Int? anchorCell;
        private Vector2Int currentEnd;
        private bool dragging;

        public void OnEnter(ToolContext context) { }
        public void OnExit(ToolContext context) => this.CancelDrag(context);
        public void OnCancel(ToolContext context) => this.CancelDrag(context);
        public void OnDelete(ToolContext context) { }

        public void Tick(ToolContext context)
        {
            if (Mouse.current == null) return;
            if (context.Brush.PickerOpen) { context.Ghost.Hide(); return; }

            context.Ghost.Hide();

            var lmb = Mouse.current.leftButton;

            if (!this.dragging
                && lmb.wasPressedThisFrame
                && !context.IsPointerOverUI()
                && context.Viewport.TryGetMouseCell(out var startCell))
            {
                this.StartDrag(context, startCell);
            }

            if (this.dragging && context.Viewport.TryGetMouseCell(out var hover))
            {
                this.currentEnd = hover;
                this.UpdateGhostPosition(context);
            }

            if (lmb.wasReleasedThisFrame && this.dragging)
            {
                this.CommitMove(context);
            }
        }

        private void StartDrag(ToolContext context, Vector2Int anchor)
        {
            this.anchorCell = anchor;
            this.currentEnd = anchor;

            this.capturedCells.Clear();
            if (!context.Selection.IsEmpty)
            {
                foreach (var cell in context.Selection.Cells) this.capturedCells.Add(cell);
            }
            else
            {
                this.capturedCells.Add(anchor);
            }

            this.records.Clear();
            this.records.AddRange(CaptureEntityRecords(context, this.capturedCells));

            // Create sprites for the drag preview.
            this.ghostRoot = new GameObject("MoveGhost");
            this.ghostRoot.transform.position = Vector3.zero;
            foreach (var cell in this.capturedCells)
            {
                foreach (var source in context.Builder.GetSpriteRenderersAt(cell))
                {
                    if (source.sprite == null) continue;

                    var ghostGo = new GameObject($"GhostSR_{cell.x}_{cell.y}");
                    ghostGo.transform.SetParent(this.ghostRoot.transform, worldPositionStays: false);
                    ghostGo.transform.localPosition = source.transform.position;
                    ghostGo.transform.rotation = source.transform.rotation;
                    ghostGo.transform.localScale = source.transform.lossyScale;

                    var spriteRenderer = ghostGo.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = source.sprite;
                    spriteRenderer.flipX = source.flipX;
                    spriteRenderer.flipY = source.flipY;
                    spriteRenderer.sortingLayerID = source.sortingLayerID;
                    spriteRenderer.sortingOrder = source.sortingOrder + 1000;
                    var color = source.color;
                    color.a = 0.5f;
                    spriteRenderer.color = color;
                }
            }

            context.Builder.SetCellsVisible(this.capturedCells, visible: false);
            this.dragging = true;
        }

        private void UpdateGhostPosition(ToolContext context)
        {
            if (this.ghostRoot == null || !this.anchorCell.HasValue) return;
            var offsetCells = new Vector2Int(
                this.currentEnd.x - this.anchorCell.Value.x,
                this.currentEnd.y - this.anchorCell.Value.y);
            this.ghostRoot.transform.position = new Vector3(offsetCells.x, offsetCells.y, 0f);
            context.MarchingAnts?.SetTemporaryOffset(offsetCells);
        }

        private void CommitMove(ToolContext context)
        {
            if (!this.dragging || !this.anchorCell.HasValue)
            {
                this.CleanupAfterDrag();
                return;
            }

            var offset = new Vector2Int(
                this.currentEnd.x - this.anchorCell.Value.x,
                this.currentEnd.y - this.anchorCell.Value.y);

            // Restore visibility and reset outline offsets.
            context.Builder.SetCellsVisible(this.capturedCells, visible: true);
            context.MarchingAnts?.SetTemporaryOffset(Vector2Int.zero);

            if (offset == Vector2Int.zero)
            {
                this.CleanupAfterDrag();
                return;
            }

            // Remove from source, clear destination, and place moved entities.
            CompositeAction? stroke = new();

            // Remove old items at source.
            foreach (var sourceCell in this.capturedCells)
            {
                context.Editor.EmitRemoveActionsAt(sourceCell, stroke);
            }

            // Clear destination cells while keeping cake and spawn.
            var destCells = new HashSet<Vector2Int>();
            foreach (var cell in this.capturedCells) destCells.Add(cell + offset);
            foreach (var dest in destCells)
            {
                if (this.capturedCells.Contains(dest)) continue;
                context.Editor.EmitRemoveActionsAt(dest, stroke, keepCakeAndSpawn: true);
            }

            foreach (var record in this.records)
            {
                this.EmitPlaceAction(context, record, record.Cell + offset, stroke);
            }

            // Update selection to the new position.
            var beforeSelection = new HashSet<Vector2Int>(context.Selection.Cells);
            if (!context.Selection.IsEmpty)
            {
                var moved = new List<Vector2Int>(context.Selection.Count);
                foreach (var cell in context.Selection.Cells) moved.Add(cell + offset);
                context.Selection.Set(moved, additive: false);
                var afterSelection = new HashSet<Vector2Int>(moved);
                stroke.Add(new ChangeSelectionAction(context.Selection, beforeSelection, afterSelection));
            }

            context.Editor.FinishStroke(ref stroke);

            this.CleanupAfterDrag();
        }

        private void EmitPlaceAction(ToolContext context, EntityRecord record, Vector2Int newCell, CompositeAction stroke)
        {
            switch (record.Kind)
            {
                case EntityKind.Tile:
                    context.Editor.DispatchAction(new PlaceTileAction(newCell, record.Id, record.ExtraData), stroke);
                    break;
                case EntityKind.Food:
                    context.Editor.DispatchAction(new PlaceFoodAction(newCell, record.Id, record.ExtraData), stroke);
                    break;
                case EntityKind.Hazard:
                    context.Editor.DispatchAction(new PlaceHazardAction(newCell, record.Id, record.Facing, record.ExtraData), stroke);
                    break;
                case EntityKind.Platform:
                    context.Editor.DispatchAction(new PlacePlatformAction(newCell, record.Id, record.Facing, record.ExtraData), stroke);
                    break;
                case EntityKind.Keystone:
                    context.Editor.DispatchAction(new PlaceKeystoneAction(newCell, record.Id, record.ExtraData), stroke);
                    break;
                case EntityKind.Cake:
                    context.Editor.DispatchAction(new SetCakeAction(newCell), stroke);
                    break;
                case EntityKind.PlayerSpawn:
                    context.Editor.DispatchAction(new SetPlayerSpawnAction(newCell), stroke);
                    break;
            }
        }

        private void CancelDrag(ToolContext context)
        {
            if (this.dragging)
            {
                context.Builder.SetCellsVisible(this.capturedCells, visible: true);
                context.MarchingAnts?.SetTemporaryOffset(Vector2Int.zero);
            }
            this.CleanupAfterDrag();
            context.Ghost.Hide();
        }

        private void CleanupAfterDrag()
        {
            if (this.ghostRoot != null) Object.Destroy(this.ghostRoot);
            this.ghostRoot = null;
            this.records.Clear();
            this.capturedCells.Clear();
            this.anchorCell = null;
            this.dragging = false;
        }

        // Scans each entity list once and tests against the cells
        private static List<EntityRecord> CaptureEntityRecords(ToolContext context, HashSet<Vector2Int> cells)
        {
            var result = new List<EntityRecord>();
            var data = context.Level.LiveData;

            foreach (var tile in data.tiles)
                if (cells.Contains(tile.gridPos))
                    result.Add(new EntityRecord(EntityKind.Tile, tile.gridPos, tile.tileId, GridDirection.Up, tile.extraData));

            foreach (var food in data.foods)
                if (cells.Contains(food.gridPos))
                    result.Add(new EntityRecord(EntityKind.Food, food.gridPos, food.foodId, GridDirection.Up, food.extraData));

            foreach (var hazard in data.hazards)
                if (cells.Contains(hazard.gridPos))
                    result.Add(new EntityRecord(EntityKind.Hazard, hazard.gridPos, hazard.hazardId, hazard.facing, hazard.extraData));

            foreach (var platform in data.platforms)
                if (cells.Contains(platform.gridPos))
                    result.Add(new EntityRecord(EntityKind.Platform, platform.gridPos, platform.platformId, platform.facing, platform.extraData));

            foreach (var keystone in data.keystones)
                if (cells.Contains(keystone.gridPos))
                    result.Add(new EntityRecord(EntityKind.Keystone, keystone.gridPos, keystone.requiredFoodId, GridDirection.Up, keystone.extraData));

            if (data.hasCake && cells.Contains(data.cakePosition))
                result.Add(new EntityRecord(EntityKind.Cake, data.cakePosition, 0, GridDirection.Up, 0));
            if (data.hasPlayerSpawn && cells.Contains(data.playerSpawn))
                result.Add(new EntityRecord(EntityKind.PlayerSpawn, data.playerSpawn, 0, GridDirection.Up, 0));

            return result;
        }

        private enum EntityKind { Tile, Food, Hazard, Platform, Keystone, Cake, PlayerSpawn }

        private readonly struct EntityRecord
        {
            public readonly EntityKind Kind;
            public readonly Vector2Int Cell;
            public readonly int Id;
            public readonly GridDirection Facing;
            public readonly int ExtraData;

            public EntityRecord(EntityKind kind, Vector2Int cell, int id, GridDirection facing, int extraData)
            {
                this.Kind = kind; this.Cell = cell; this.Id = id; this.Facing = facing; this.ExtraData = extraData;
            }
        }
    }
}
