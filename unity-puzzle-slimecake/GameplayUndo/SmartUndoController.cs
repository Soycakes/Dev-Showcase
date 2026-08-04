#nullable enable
using System;
using System.Collections.Generic;
using SlimeCake.Core;
using SlimeCake.Entities.Foods;
using SlimeCake.Entities.Foods.Variants;
using SlimeCake.Entities.World;
using SlimeCake.Systems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeCake.Entities.Player
{
    /// <summary>
    /// Captures and restores gameplay state for undo. Handles physics and items.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-150)]
    public sealed class SmartUndoController : MonoBehaviour
    {
        [SerializeField] private SlimeController slime = null!;
        [SerializeField] private Inventory inventory = null!;
        [SerializeField] private FoodRegistry foodRegistry = null!;

        // Tracks cell safety and history
        // 
        // OnFoodEat : Saves current cell if grounded, else last safe cell.
        // OnPlayerKilled/OnFoodPickup : Saves at last safe cell.
        // Clears lastSafeCell after a save to group actions in the same cell.
        private readonly Stack<GameplaySnapshot> history = new();
        private GameplaySnapshot? firstSafeSpot;
        private bool winLocked;

        private Vector2Int? lastTrackedCell;
        private bool lastTrackedCellWasGrounded;
        private Vector2Int? lastSafeCell;
        // Tracks the last saved cell to prevent duplicate saves.
        private Vector2Int? lastSavedCell;

        // Dashing players are not grounded.
        private bool IsSafelyGrounded => this.slime.IsGrounded && !this.slime.IsDashing;

        public int HistoryCount => this.history.Count;
        public bool WinLocked => this.winLocked;

        // Debug readonly accessors
        public bool HasFallback => this.firstSafeSpot != null;
        // Returns the next cell to restore on undo.
        public Vector2Int? NextUndoCell =>
            this.history.Count > 0 ? this.history.Peek().SafeSpotGrid
            : this.firstSafeSpot != null ? this.firstSafeSpot.SafeSpotGrid
            : (Vector2Int?)null;
        public Vector2Int? LastSavedCell => this.lastSavedCell;
        public Vector2Int? LastSafeCell => this.lastSafeCell;
        public Vector2Int? LastTrackedCell => this.lastTrackedCell;
        public bool LastTrackedGrounded => this.lastTrackedCellWasGrounded;

        public event Action? UndoApplied;

        private void Awake()
        {
            if (this.slime == null) this.slime = this.GetComponent<SlimeController>();
            if (this.inventory == null) this.inventory = this.GetComponent<Inventory>();
            if (this.foodRegistry == null) Debug.LogError("SmartUndoController: foodRegistry not assigned.", this);
        }

        private void OnEnable()
        {
            CakeEntity.Reached += this.OnCakeReached;
            if (this.slime != null) this.slime.Killed += this.OnPlayerKilled;
        }

        private void OnDisable()
        {
            CakeEntity.Reached -= this.OnCakeReached;
            if (this.slime != null) this.slime.Killed -= this.OnPlayerKilled;
        }

        private void FixedUpdate()
        {
            // Sync tracking before actions run.
            this.UpdateTracking();

            if (this.firstSafeSpot == null && this.IsSafelyGrounded)
            {
                var cell = GridUtil.WorldToCell((Vector2)this.transform.position);
                this.firstSafeSpot = this.BuildSnapshot(cell);
            }
        }

        private void Update()
        {
            if (this.slime.IsFrozen) return;
            if (this.slime.IsAwaitingFirstLand) return;
            if (InputManager.Instance != null && InputManager.Instance.UndoPressed)
            {
                this.Undo();
            }
        }

        private void LateUpdate()
        {
            if (!UI.DebugHUD.IsActive) return;
            if (this.winLocked) return;

            var cell = this.NextUndoCell;
            if (cell.HasValue)
            {
                UI.DebugHUD.DrawCellOutline(cell.Value, new Color(0.2f, 0.55f, 1f));
            }
        }

        public void OnFoodEat()
        {
            if (this.firstSafeSpot == null) return;

            this.UpdateTracking();
            var nowCell = GridUtil.WorldToCell((Vector2)this.transform.position);

            // Save at current cell if grounded since entry, else last safe cell.
            if (this.lastTrackedCellWasGrounded)
            {
                // Save state before changes.
                this.PushAt(nowCell);
            }
            else
            {
                // Save at last safe cell if falling or dashing.
                this.SaveAtLast(nowCell);
            }
        }

        public void OnFoodPickup()
        {
            if (this.firstSafeSpot == null) return;

            this.UpdateTracking();
            var nowCell = GridUtil.WorldToCell((Vector2)this.transform.position);
            this.SaveAtLast(nowCell);
        }

        // Roll back to the last safe cell on death.
        private void OnPlayerKilled() => this.OnFoodPickup();

        private void SaveAtLast(Vector2Int nowCell)
        {
            // Avoid saving if last safe cell matches current.
            var lsc = this.lastSafeCell;
            if (lsc.HasValue && lsc.Value == nowCell)
            {
                lsc = null;
            }

            // Check if floor below has crumbled.
            if (lsc.HasValue && !this.IsBelowLastSafe(lsc.Value))
            {
                lsc = null;
            }

            if (lsc == null)
            {
                // No safe cell found. Reset safe cell and return.
                this.lastSafeCell = null;
                return;
            }

            this.PushAt(lsc.Value);
        }

        private void PushAt(Vector2Int cell)
        {
            var snap = this.BuildSnapshot(cell);
            this.history.Push(snap);
            this.lastSafeCell = null;
            this.lastSavedCell = cell;
        }

        private bool IsBelowLastSafe(Vector2Int cell)
        {
            var below = new Vector2Int(cell.x, cell.y - 1);
            if (KeystoneWorldRegistry.Entries.TryGetValue(below, out var ke) && ke != null)
            {
                return ke.State == KeystoneState.Active;
            }
            return true;
        }

        private void UpdateTracking()
        {
            var nowCell = GridUtil.WorldToCell((Vector2)this.transform.position);
            var nowGrounded = this.IsSafelyGrounded;

            if (this.lastTrackedCell == null)
            {
                this.lastTrackedCell = nowCell;
                this.lastTrackedCellWasGrounded = nowGrounded;
                return;
            }

            if (nowCell != this.lastTrackedCell.Value)
            {
                // Update safe cell on cell change if grounded, unless already saved.
                if (this.lastTrackedCellWasGrounded && this.lastTrackedCell != this.lastSavedCell)
                {
                    this.lastSafeCell = this.lastTrackedCell;
                }
                this.lastTrackedCell = nowCell;
                this.lastTrackedCellWasGrounded = nowGrounded;
            }
            else if (nowGrounded)
            {
                // Grounded state remains active within the same cell.
                this.lastTrackedCellWasGrounded = true;
            }
        }

        public void Undo()
        {
            if (this.winLocked) return;

            GameplaySnapshot snapshot;
            if (this.history.Count > 0)
            {
                snapshot = this.history.Pop();
            }
            else if (this.firstSafeSpot != null)
            {
                snapshot = this.firstSafeSpot;
            }
            else
            {
                return;
            }

            this.RestoreSnapshot(snapshot);
            this.UndoApplied?.Invoke();
        }

        private void OnCakeReached()
        {
            this.winLocked = true;
        }

        private GameplaySnapshot BuildSnapshot(Vector2Int cell)
        {
            var snap = new GameplaySnapshot
            {
                PlayerPosition = new Vector2(cell.x, cell.y),
                SafeSpotGrid = cell,
                LastSafeAtSave = this.lastSafeCell,
                PlayerFacing = this.slime.Facing,
                InventoryMaxSlots = this.inventory.MaxSlots,
            };

            foreach (var item in this.inventory.Items)
            {
                if (item == null) continue;
                var template = this.foodRegistry.Get(item.FoodId);
                var isClone = template != item;
                var uses = (item is MultiUseJumpFood mu) ? mu.UsesRemaining : 0;
                snap.InventoryItems.Add(new InventoryItemSnapshot
                {
                    FoodId = item.FoodId,
                    IsClone = isClone,
                    UsesRemaining = uses
                });
            }

            foreach (var pair in FoodWorldRegistry.Entries)
            {
                var fe = pair.Value;
                if (fe == null || fe.Data == null) continue;
                snap.FoodsInWorld.Add(new FoodInWorldSnapshot
                {
                    FoodId = fe.Data.FoodId,
                    GridPos = pair.Key
                });
            }

            foreach (var pair in KeystoneWorldRegistry.Entries)
            {
                var ke = pair.Value;
                if (ke == null) continue;
                // Restore active keystones only.
                if (ke.State != KeystoneState.Active) continue;
                snap.KeystonesInWorld.Add(new KeystoneSnapshot
                {
                    LinkedFoodId = ke.LinkedFoodId,
                    ExtraData = ke.ExtraData,
                    GridPos = pair.Key
                });
            }

            if (LevelRunTracker.Instance != null)
            {
                snap.StatsSnapshot = LevelRunTracker.Instance.Stats.Clone();
            }

            return snap;
        }

        private void RestoreSnapshot(GameplaySnapshot snap)
        {
            // Teleport and reset player state to prevent clipping.
            this.slime.Teleport(snap.PlayerPosition);
            this.slime.SetFacing(snap.PlayerFacing);
            this.slime.ClearDeath();

            // Reset tracking variables.
            this.lastTrackedCell = snap.SafeSpotGrid;
            this.lastTrackedCellWasGrounded = true;
            this.lastSafeCell = snap.LastSafeAtSave;
            this.lastSavedCell = null;

            this.RestoreInventory(snap);
            this.RestoreFoodsInWorld(snap);
            this.RestoreKeystonesInWorld(snap);
            this.RestoreStats(snap);
        }

        private void RestoreStats(GameplaySnapshot snap)
        {
            if (snap.StatsSnapshot == null) return;
            LevelRunTracker.Instance?.RestoreStatsFromSnapshot(snap.StatsSnapshot);
        }

        private void RestoreInventory(GameplaySnapshot snap)
        {
            // Track clones to destroy to prevent memory leaks.
            var oldClones = new List<FoodData>();
            foreach (var existing in this.inventory.Items)
            {
                if (existing == null) continue;
                var existingTemplate = this.foodRegistry.Get(existing.FoodId);
                if (existingTemplate != existing) oldClones.Add(existing);
            }

            // Restore inventory in one call to avoid redundant HUD updates.
            var targetItems = new List<FoodData>(snap.InventoryItems.Count);
            foreach (var item in snap.InventoryItems)
            {
                var template = this.foodRegistry.Get(item.FoodId);
                if (template == null)
                {
                    Debug.LogWarning($"SmartUndoController: foodId {item.FoodId} not in registry on restore.");
                    continue;
                }

                FoodData toPush;
                if (item.IsClone && template is MultiUseJumpFood multiUseTemplate)
                {
                    var clone = Instantiate(multiUseTemplate);
                    clone.SetUsesRemaining(item.UsesRemaining);
                    toPush = clone;
                }
                else
                {
                    toPush = template;
                }
                targetItems.Add(toPush);
            }

            this.inventory.RestoreFromSnapshot(targetItems, snap.InventoryMaxSlots);

            foreach (var clone in oldClones) Destroy(clone);
        }

        private void RestoreFoodsInWorld(GameplaySnapshot snap)
        {
            var builder = LevelBuilder.Instance;
            if (builder == null)
            {
                Debug.LogError("SmartUndoController: LevelBuilder.Instance is null on restore. Foods cannot be respawned.");
                return;
            }

            builder.ClearAllFoods();
            foreach (var f in snap.FoodsInWorld)
            {
                builder.PlaceFood(f.GridPos, f.FoodId);
            }
        }

        private void RestoreKeystonesInWorld(GameplaySnapshot snap)
        {
            var builder = LevelBuilder.Instance;
            if (builder == null) return;

            builder.ClearAllKeystones();
            foreach (var k in snap.KeystonesInWorld)
            {
                builder.PlaceKeystone(k.GridPos, k.LinkedFoodId, k.ExtraData);
            }
        }
    }
}
