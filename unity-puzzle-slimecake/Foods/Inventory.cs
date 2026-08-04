#nullable enable
using System;
using System.Collections.Generic;
using SlimeCake.Core;
using SlimeCake.Entities.Foods;
using UnityEngine;

/// <summary>
/// Stack based food inventory.
/// Pushes and pops food items and tracks slot.
/// 
/// Inventory events trigger HUD animation & logic.
/// </summary>

namespace SlimeCake.Entities.Player
{
    [DisallowMultipleComponent]
    public sealed class Inventory : MonoBehaviour, IInventory
    {
        [SerializeField] private int initialMaxSlots = 1;
        [SerializeField] private int hardSlotCap = 10;

        private readonly List<FoodData> items = new();
        private int maxSlots;

        public int Count => this.items.Count;

        public int MaxSlots
        {
            get => this.maxSlots;
            set
            {
                var clamped = Mathf.Clamp(value, 0, this.hardSlotCap);
                if (clamped == this.maxSlots) return;
                var delta = clamped - this.maxSlots;
                this.maxSlots = clamped;

                if (delta > 0)
                {
                    for (int i = 0; i < delta; i++) this.SlotAdded?.Invoke();
                }
                else
                {
                    // Slots removed
                    // if there's a food at the removed slot, also remove it
                    var slotsRemoved = -delta;
                    for (int i = 0; i < slotsRemoved; i++)
                    {
                        if (this.items.Count > this.maxSlots)
                        {
                            var food = this.items[0];
                            this.items.RemoveAt(0);
                            this.FoodConsumed?.Invoke(food);
                        }
                        this.SlotRemoved?.Invoke();
                    }
                }

                this.Changed?.Invoke();
            }
        }

        public FoodData? Top => this.items.Count > 0 ? this.items[^1] : null;

        public bool CanPush => this.items.Count < this.maxSlots;

        public IReadOnlyList<FoodData> Items => this.items;

        // Generic event for player, level stats, etc.
        // Fires once per inventory change below.
        public event Action? Changed;

        // Events for HUD animation subscribes
        public event Action<FoodData>? FoodAdded; // pushed
        public event Action<FoodData>? FoodConsumed; // Pop, Clear, MaxSlots--
        public event Action<FoodData>? FoodMutated; // Food changed

        // For more complicated foods (destroying multiple food), needed for special animations
        public event Action<FoodData, FoodData>? FoodChainConsumed;
        public event Action? SlotAdded; // MaxSlots++
        public event Action? SlotRemoved; // MaxSlots--
        public event Action? Reversed; // Reverse()

        // For Undo feature
        public event Action? Restored;

        private void Awake()
        {
            this.maxSlots = Mathf.Clamp(this.initialMaxSlots, 0, this.hardSlotCap);
        }

        public bool TryPush(FoodData food)
        {
            if (!this.CanPush) return false;
            this.items.Add(food);
            this.FoodAdded?.Invoke(food);
            this.Changed?.Invoke();
            return true;
        }

        public FoodData? Pop()
        {
            if (this.items.Count == 0) return null;
            var food = this.items[^1];
            this.items.RemoveAt(this.items.Count - 1);
            this.FoodConsumed?.Invoke(food);
            this.Changed?.Invoke();
            return food;
        }

        public bool TopMatches(int foodId) => this.Top != null && this.Top.FoodId == foodId;

        public bool TryConsumeTop(int foodId, out FoodData? consumed)
        {
            if (!this.TopMatches(foodId))
            {
                consumed = null;
                return false;
            }
            consumed = this.Pop();
            return true;
        }

        // Try remove food by matching type instead
        public bool TryRemoveByReference(FoodData food)
        {
            var idx = this.items.IndexOf(food);
            if (idx < 0) return false;
            var removed = this.items[idx];
            this.items.RemoveAt(idx);
            this.FoodConsumed?.Invoke(removed);
            this.Changed?.Invoke();
            return true;
        }

        public void Reverse()
        {
            if (this.items.Count <= 1) return;
            this.items.Reverse();
            this.Reversed?.Invoke();
            this.Changed?.Invoke();
        }

        // Food that may not change anything but still triggers
        public void NotifyMutated(FoodData food)
        {
            this.FoodMutated?.Invoke(food);
            this.Changed?.Invoke();
        }

        // "pop self + pop next" for Pineapple.
        public bool TryPopWithChain()
        {
            if (this.items.Count == 0) return false;
            var primary = this.items[^1];
            this.items.RemoveAt(this.items.Count - 1);

            if (this.items.Count == 0)
            {
                this.FoodConsumed?.Invoke(primary);
            }
            else
            {
                var followup = this.items[^1];
                this.items.RemoveAt(this.items.Count - 1);
                this.FoodChainConsumed?.Invoke(primary, followup);
            }
            this.Changed?.Invoke();
            return true;
        }

        public void Clear()
        {
            if (this.items.Count == 0) return;

            while (this.items.Count > 0)
            {
                var food = this.items[^1];
                this.items.RemoveAt(this.items.Count - 1);
                this.FoodConsumed?.Invoke(food);
            }
            this.Changed?.Invoke();
        }

        // From Undo
        // Replace inventory instantly (no normal UI or animation calls)
        public void RestoreFromSnapshot(IList<FoodData> targetItems, int targetMaxSlots)
        {
            this.maxSlots = Mathf.Clamp(targetMaxSlots, 0, this.hardSlotCap);
            this.items.Clear();
            foreach (var item in targetItems)
            {
                if (item == null) continue;
                if (this.items.Count >= this.maxSlots) break;
                this.items.Add(item);
            }
            this.Restored?.Invoke();
            this.Changed?.Invoke();
        }
    }
}
