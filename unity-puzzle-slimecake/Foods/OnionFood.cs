#nullable enable
using SlimeCake.Core;
using UnityEngine;

/// <summary>
/// Inventory food that consumes itself and any food the player touches in the world.
/// </summary>

namespace SlimeCake.Entities.Foods.Variants
{
    // Can't be eaten in inventory, BUT
    // On touching another food (in world),
    // Both onion & touched food is destroyed
    [CreateAssetMenu(fileName = "OnionFood", menuName = "SlimeCake/Foods/Onion Food")]
    public sealed class OnionFood : FoodData
    {
        public override bool GoesToInventory => true;

        public override bool CanPickUp(IInventory inventory) => inventory.CanPush;

        public override FoodPickupResult OnPickUp(IPlayer player, IInventory inventory)
        {
            return inventory.TryPush(this)
                ? FoodPickupResult.AddedToInventory
                : FoodPickupResult.NotPickedUp;
        }

        // True on EVERY intercepts on touching food
        // (Maybe gate some foods here later?)
        public override bool CanInterceptPickup(FoodData incoming, IInventory inventory) => true;

        public override void OnInterceptPickup(FoodData incoming, IInventory inventory)
        {
            inventory.TryRemoveByReference(this);
        }
    }
}
