#nullable enable
using System.Collections.Generic;
using SlimeCake.Core;
using UnityEngine;

namespace SlimeCake.Entities.Player
{
    /// <summary>
    /// Snapshot of player and world state for undo.
    /// </summary>
    public sealed class GameplaySnapshot
    {
        public Vector2 PlayerPosition;
        public Vector2Int SafeSpotGrid;
        public Vector2Int? LastSafeAtSave;
        public Facing PlayerFacing;
        public int InventoryMaxSlots;
        public List<InventoryItemSnapshot> InventoryItems = new();
        public List<FoodInWorldSnapshot> FoodsInWorld = new();
        public List<KeystoneSnapshot> KeystonesInWorld = new();
        public LevelRunStats? StatsSnapshot;
    }

    public struct KeystoneSnapshot
    {
        public int LinkedFoodId;
        public int ExtraData;
        public Vector2Int GridPos;
    }

    public struct InventoryItemSnapshot
    {
        public int FoodId;
        public bool IsClone;
        public int UsesRemaining;
    }

    public struct FoodInWorldSnapshot
    {
        public int FoodId;
        public Vector2Int GridPos;
    }
}
