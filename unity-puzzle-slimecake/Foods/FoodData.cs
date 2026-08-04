#nullable enable
using SlimeCake.Core;
using SlimeCake.Systems;
using UnityEngine;

/// <summary>
/// Abstract class for all food ScriptableObjects
/// pickup, use, & various hooks
/// </summary>

namespace SlimeCake.Entities.Foods
{
    public abstract class FoodData : ScriptableObject
    {
        [SerializeField] private int foodId;
        [SerializeField] private string displayName = "";
        [SerializeField, TextArea(3, 10)] private string description = "";
        [SerializeField] private Sprite? sprite;
        [SerializeField] private bool hologram;
        [SerializeField] private SoundId useSound = SoundId.None;
        [SerializeField] private SoundId pickupSound = SoundId.None;

        public int FoodId => this.foodId;
        public string DisplayName => this.displayName;
        public string Description => this.description;
        public Sprite? Sprite => this.sprite;
        public bool IsHologram => this.hologram;
        public SoundId UseSound => this.useSound;
        public SoundId PickupSound => this.pickupSound;

        public virtual bool GoesToInventory => false;

        public abstract bool CanPickUp(IInventory inventory);
        public abstract FoodPickupResult OnPickUp(IPlayer player, IInventory inventory);

        public virtual bool CanUse(IPlayer player, IInventory inventory) => false;
        public virtual void OnUse(IPlayer player, IInventory inventory) { }

        // Special case for Onion like foods
        public virtual bool CanInterceptPickup(FoodData incoming, IInventory inventory) => false;
        public virtual void OnInterceptPickup(FoodData incoming, IInventory inventory) { }
    }
}
