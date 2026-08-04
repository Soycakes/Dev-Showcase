#nullable enable
using System;
using SlimeCake.Core;
using UnityEngine;

/// <summary>
/// Food that gives double jump multiple times, destroying when out of charges.
/// 
/// Each food has it's own counter.
/// </summary>

namespace SlimeCake.Entities.Foods.Variants
{
    [CreateAssetMenu(fileName = "MultiUseJumpFood", menuName = "SlimeCake/Foods/Multi-Use Jump Food")]
    public class MultiUseJumpFood : FoodData, IMultiUseFood
    {
        [SerializeField] private float jumpHeight = 1f;
        [SerializeField] private int totalUses = 3;
        [SerializeField] private bool requireGrounded;

        [NonSerialized] private int usesRemaining = -1;

        public int UsesRemaining => this.usesRemaining;
        public int TotalUses => this.totalUses;

        public void SetUsesRemaining(int value) => this.usesRemaining = Mathf.Max(0, value);

        public override bool GoesToInventory => true;

        public override bool CanPickUp(IInventory inventory) => inventory.CanPush;

        public override FoodPickupResult OnPickUp(IPlayer player, IInventory inventory)
        {
            var instance = Instantiate(this);
            instance.usesRemaining = instance.totalUses;
            if (!inventory.TryPush(instance))
            {
                Destroy(instance);
                return FoodPickupResult.NotPickedUp;
            }
            return FoodPickupResult.AddedToInventory;
        }

        public override bool CanUse(IPlayer player, IInventory inventory)
        {
            return !this.requireGrounded || player.IsGrounded;
        }

        public override void OnUse(IPlayer player, IInventory inventory)
        {
            this.usesRemaining = Mathf.Max(0, this.usesRemaining - 1);
            player.Jump(this.jumpHeight);

            if (this.usesRemaining <= 0)
            {
                // Last use, destroy
                inventory.Pop();
                Destroy(this);
            }
            else
            {
                // DON'T destroy, but send an event for animation/etc
                inventory.NotifyMutated(this);
            }
        }
    }
}
