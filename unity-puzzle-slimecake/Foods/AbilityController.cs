#nullable enable
using System;
using SlimeCake.Entities.Foods;
using SlimeCake.Systems;
using UnityEngine;

/// <summary>
/// Handles the player's food ability
/// Includes input buffer for cases where food can't be used yet
/// </summary>

namespace SlimeCake.Entities.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(SlimeController))]
    public sealed class AbilityController : MonoBehaviour
    {
        [SerializeField] private SoundId useFailSound = SoundId.ItemUseFail;

        private Inventory inventory = null!;
        private SlimeController player = null!;
        private SmartUndoController? smartUndo;
        private bool subscribed;
        // Food use buffer
        private bool bufferedUse;

        // Action when food is actually consumed by player input
        public event Action<FoodData>? FoodUsedByPlayer;

        private void Awake()
        {
            this.inventory = this.GetComponent<Inventory>();
            this.player = this.GetComponent<SlimeController>();
            this.smartUndo = this.GetComponent<SmartUndoController>();

            this.player.DashEnded += this.OnDashEnded;
        }

        private void Start()
        {
            this.Subscribe();
        }

        private void OnDestroy()
        {
            this.Unsubscribe();
            this.player.DashEnded -= this.OnDashEnded;
        }

        private void Subscribe()
        {
            if (this.subscribed) return;
            if (InputManager.Instance == null)
            {
                Debug.LogError(
                    $"AbilityController on '{this.gameObject.name}': InputManager.Instance is null at Start. " +
                    "Make sure an InputManager component exists in the scene before the player spawns.",
                    this);
                return;
            }
            InputManager.Instance.AbilityPressed += this.OnAbilityPressed;
            this.subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!this.subscribed) return;
            if (InputManager.Instance != null)
            {
                InputManager.Instance.AbilityPressed -= this.OnAbilityPressed;
            }
            this.subscribed = false;
        }

        private void OnAbilityPressed()
        {
            if (this.player.IsDead || this.player.IsFrozen) return;
            if (this.player.IsAwaitingFirstLand) return;
            // No food can be used mid dash
            if (this.player.IsDashing)
            {
                this.bufferedUse = true;
                return;
            }
            this.TryUseTopFood();
        }

        // Fired when dash ends.
        private void OnDashEnded(bool completed)
        {
            var wasBuffered = this.bufferedUse;
            this.bufferedUse = false;
            if (wasBuffered && completed) this.TryUseTopFood();
        }

        private void TryUseTopFood()
        {
            var top = this.inventory.Top;
            if (top == null) return;
            if (!top.CanUse(this.player, this.inventory))
            {
                AudioManager.Instance?.PlaySound(this.useFailSound);
                return;
            }
            this.smartUndo?.OnFoodEat();
            LevelRunTracker.Instance?.RecordUse(top);
            LevelRunTracker.Instance?.NotifyPlayerActed();
            top.OnUse(this.player, this.inventory);
            this.FoodUsedByPlayer?.Invoke(top);
            AudioManager.Instance?.PlaySound(top.UseSound);
        }
    }
}
