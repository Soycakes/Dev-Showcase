#nullable enable
using System;
using SlimeCake.Core;
using SlimeCake.Entities.Foods;
using SlimeCake.Entities.Foods.Variants;
using SlimeCake.Systems;
using SlimeCake.UI;
using UnityEngine;

/// <summary>
/// Controller for player and picking up food from the world.
/// Handles special cases of foods that are used on pickup or intercepts.
/// </summary>

namespace SlimeCake.Entities.Player
{
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(SlimeController))]
    public sealed class FoodPickupController : MonoBehaviour
    {
        [SerializeField] private SoundId defaultPickupSound = SoundId.PlayerPickup;

        private Inventory inventory = null!;
        private SlimeController player = null!;
        private PlayerLocator? gridTracker;
        private SmartUndoController? smartUndo;
        private SlimeView? slimeView;
        private Vector2Int? processedCell;

        // Special food pickup case
        public event Action<FoodData>? FoodConsumedOnPickup;

        private void Awake()
        {
            this.inventory = this.GetComponent<Inventory>();
            this.player = this.GetComponent<SlimeController>();
            this.gridTracker = this.GetComponent<PlayerLocator>();
            this.smartUndo = this.GetComponent<SmartUndoController>();
            this.slimeView = this.GetComponentInChildren<SlimeView>();
        }

        private void FixedUpdate()
        {
            if (this.player.IsDead) return;
            if (this.player.IsFrozen) return;

            var cell = this.GetCurrentCell();

            if (this.processedCell.HasValue && this.processedCell.Value != cell)
            {
                this.processedCell = null;
            }

            var entity = FoodWorldRegistry.GetAt(cell);
            if (entity == null || entity.Data == null)
            {
                this.processedCell = null;
                return;
            }

            if (this.processedCell.HasValue && this.processedCell.Value == cell)
            {
                return;
            }

            var data = entity.Data;

            // Special case for Onion (intercept food touched)
            var items = this.inventory.Items;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item == null) continue;
                if (!item.CanInterceptPickup(data, this.inventory)) continue;
                this.smartUndo?.OnFoodPickup();
                item.OnInterceptPickup(data, this.inventory);
                AudioManager.Instance?.PlaySound(data.PickupSound != SoundId.None ? data.PickupSound : this.defaultPickupSound);
                entity.RemoveFromWorld();
                // Slime intercepts, eat anim plays
                this.FoodConsumedOnPickup?.Invoke(data);
                this.processedCell = null;
                return;
            }

            if (!data.CanPickUp(this.inventory)) return;

            this.smartUndo?.OnFoodPickup();
            LevelRunTracker.Instance?.RecordPickup(data);

            // First pickup for food unlocks
            var pm = PlayerProgressManager.Instance;
            var unlockKey = $"food:{data.FoodId}";
            bool firstTime = pm != null && !pm.HasSeenUnlock(unlockKey);
            pm?.MarkUnlockSeen(unlockKey);

            // Special case for Watermelon
            FoodData? chainedNext = null;
            if (data is WatermelonFood w && w.MelonSlice != null && pm != null)
            {
                var sliceKey = $"food:{w.MelonSlice.FoodId}";
                if (!pm.HasSeenUnlock(sliceKey))
                {
                    pm.MarkUnlockSeen(sliceKey);
                    chainedNext = w.MelonSlice;
                }
            }

            if (SceneLoader.Instance?.CurrentScene == GameScene.Level
                && FoodIntroPopup.Instance != null)
            {
                if (firstTime) FoodIntroPopup.Instance.Show(data, chainedNext);
                else if (chainedNext != null) FoodIntroPopup.Instance.Show(chainedNext);
            }
            var sourcePos = entity.transform.position;
            var result = data.OnPickUp(this.player, this.inventory);
            switch (result)
            {
                case FoodPickupResult.AddedToInventory:
                    AudioManager.Instance?.PlaySound(data.PickupSound != SoundId.None ? data.PickupSound : this.defaultPickupSound);
                    this.slimeView?.PlayPickupDrag(data, sourcePos, replacesOverlay: true);
                    entity.RemoveFromWorld();
                    this.processedCell = null;
                    break;
                case FoodPickupResult.Consumed:
                    AudioManager.Instance?.PlaySound(data.PickupSound != SoundId.None ? data.PickupSound : this.defaultPickupSound);
                    this.slimeView?.PlayPickupDrag(data, sourcePos, replacesOverlay: false);
                    entity.RemoveFromWorld();
                    // Immediate-effect food (Mushroom / Ginseng / Eggplant / Dragonfruit /
                    // ...) eaten on contact - eat anim plays.
                    this.FoodConsumedOnPickup?.Invoke(data);
                    this.processedCell = null;
                    break;
                case FoodPickupResult.NotDestroyed:
                    AudioManager.Instance?.PlaySound(data.PickupSound != SoundId.None ? data.PickupSound : this.defaultPickupSound);
                    this.processedCell = cell;
                    break;
                case FoodPickupResult.NotPickedUp:
                    return;
            }
        }

        private Vector2Int GetCurrentCell()
        {
            return this.gridTracker != null
                ? this.gridTracker.CurrentCell
                : GridUtil.WorldToCell(this.transform.position);
        }
    }
}
