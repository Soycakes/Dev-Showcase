#nullable enable
using SlimeCake.Core;
using UnityEngine;

/// <summary>
/// Inventory food that pops itself plus the item beneath it in the stack when used.
/// </summary>

namespace SlimeCake.Entities.Foods.Variants
{
    // Pops itself plus the food directly beneath it in the stack.
    // Can't use pineapple food byitself.
    [CreateAssetMenu(fileName = "PineappleFood", menuName = "SlimeCake/Foods/Pineapple Food")]
    public sealed class PineappleFood : FoodData
    {
        public override bool GoesToInventory => true;

        public override bool CanPickUp(IInventory inventory) => inventory.CanPush;

        public override FoodPickupResult OnPickUp(IPlayer player, IInventory inventory)
        {
            return inventory.TryPush(this)
                ? FoodPickupResult.AddedToInventory
                : FoodPickupResult.NotPickedUp;
        }

        public override bool CanUse(IPlayer player, IInventory inventory) => inventory.Count >= 2;

        public override void OnUse(IPlayer player, IInventory inventory)
        {
            inventory.TryPopWithChain();
        }
    }
}
