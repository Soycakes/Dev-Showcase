using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace FactoryGame.Mixed
{
    /// <summary>
    /// Handles inventory transaction logic like splitting, swapping, and transferring items, similar to Minecraft's inventory system.
    /// </summary>
    public static class InventoryClickSolver
    {
        // Left click moves a full stack. Right click moves a partial stack.
        // Both clicks follow the decision tree below.
        public static void SolveClick(
            DynamicBuffer<InventoryElement> inv,
            ref PlayerHeldItem held,
            int slotIndex,
            InventoryClickType clickType,
            IInventoryValidator validator
        )
        {
            if (slotIndex < 0 || slotIndex >= inv.Length)
                return;

            bool isRightClick = clickType == InventoryClickType.RightClick;
            uint slotItemId = inv[slotIndex].ItemId;
            int slotCount = inv[slotIndex].Count;

            if (held.Count == 0) // Pick up
            {
                if (slotCount == 0 || slotItemId == 0)
                    return;
                if (!validator.CanExtract(slotIndex))
                    return;

                int take = isRightClick ? (slotCount + 1) / 2 : slotCount;
                int remaining = slotCount - take;

                held.ItemId = slotItemId;
                held.Count = take;
                inv[slotIndex] =
                    remaining <= 0
                        ? new InventoryElement { ItemId = 0, Count = 0 }
                        : new InventoryElement
                        {
                            ItemId = slotItemId,
                            Count = remaining,
                        };
            }
            else if (slotCount == 0) // Place
            {
                int place = isRightClick ? 1 : held.Count;
                if (!validator.CanInsert(slotIndex, held.ItemId, place))
                    return;

                inv[slotIndex] = new InventoryElement
                {
                    ItemId = held.ItemId,
                    Count = place,
                };
                held.Count -= place;
                if (held.Count <= 0)
                {
                    held.ItemId = 0;
                    held.Count = 0;
                }
            }
            else if (slotItemId == held.ItemId) // Stack items
            {
                int place = isRightClick ? 1 : held.Count;
                if (!validator.CanInsert(slotIndex, held.ItemId, place))
                    return;

                int maxStack = ItemDatabase.GetMaxStack(held.ItemId);
                int space = maxStack - slotCount;
                if (space <= 0)
                    return;

                int toAdd = math.min(place, space);
                var element = inv[slotIndex];
                element.Count += toAdd;
                inv[slotIndex] = element;

                held.Count -= toAdd;
                if (held.Count <= 0)
                {
                    held.ItemId = 0;
                    held.Count = 0;
                }
            }
            else // Swap items
            {
                if (
                    !validator.CanExtract(slotIndex)
                    || !validator.CanInsert(slotIndex, held.ItemId, held.Count)
                )
                    return;

                uint tempId = slotItemId;
                int tempCount = slotCount;

                inv[slotIndex] = new InventoryElement
                {
                    ItemId = held.ItemId,
                    Count = held.Count,
                };

                held.ItemId = tempId;
                held.Count = tempCount;
            }
        }

        // Handles shift transfers between player inventory and containers.
        public static void SolveShiftClick(
            DynamicBuffer<InventoryElement> sourceInv,
            DynamicBuffer<InventoryElement> destInv,
            int slotIndex,
            int destStart,
            int destEnd,
            IInventoryValidator destValidator,
            int countToMove
        )
        {
            if (slotIndex < 0 || slotIndex >= sourceInv.Length)
                return;

            uint itemId = sourceInv[slotIndex].ItemId;
            int count = sourceInv[slotIndex].Count;
            if (itemId == 0 || count <= 0)
                return;

            int toMove = math.min(count, countToMove);
            int maxStack = ItemDatabase.GetMaxStack(itemId);

            int moved = MergeThenFill(
                destInv,
                itemId,
                toMove,
                destStart,
                destEnd,
                maxStack,
                destValidator
            );

            if (moved > 0)
            {
                var sourceElement = sourceInv[slotIndex];
                sourceElement.Count -= moved;
                if (sourceElement.Count <= 0)
                {
                    sourceInv[slotIndex] = new InventoryElement
                    {
                        ItemId = 0,
                        Count = 0,
                    };
                }
                else
                {
                    sourceInv[slotIndex] = sourceElement;
                }
            }
        }

        // Handles drag events to split items across slots.
        public static void SolveDragPlacement(
            DynamicBuffer<InventoryElement> inv,
            ref PlayerHeldItem held,
            FixedList128Bytes<byte> slotIndices,
            IInventoryValidator validator
        )
        {
            if (held.Count <= 0 || held.ItemId == 0)
                return;
            int slotCount = slotIndices.Length;
            if (slotCount == 0)
                return;

            // Count valid target slots in the drag path
            int validSlotsCount = 0;
            for (int i = 0; i < slotCount; i++)
            {
                int slotIdx = slotIndices[i];
                if (slotIdx >= 0 && slotIdx < inv.Length)
                {
                    uint slotItemId = inv[slotIdx].ItemId;
                    if (slotItemId == 0 || slotItemId == held.ItemId)
                    {
                        if (validator.CanInsert(slotIdx, held.ItemId, 1))
                        {
                            validSlotsCount++;
                        }
                    }
                }
            }

            if (validSlotsCount == 0)
                return;

            int amountPerSlot = held.Count / validSlotsCount;

            int totalPlaced = 0;

            for (int i = 0; i < slotCount; i++)
            {
                int slotIdx = slotIndices[i];
                if (slotIdx < 0 || slotIdx >= inv.Length)
                    continue;

                uint slotItemId = inv[slotIdx].ItemId;
                if (slotItemId != 0 && slotItemId != held.ItemId)
                    continue;

                int originalCount = inv[slotIdx].Count;
                int maxStack = ItemDatabase.GetMaxStack(held.ItemId);
                int space = maxStack - originalCount;
                int placed = math.min(amountPerSlot, space);

                // Revalidate using the actual quantity to place in the slot.
                if (placed <= 0 || !validator.CanInsert(slotIdx, held.ItemId, placed))
                    continue;

                inv[slotIdx] = new InventoryElement
                {
                    ItemId = held.ItemId,
                    Count = originalCount + placed,
                };
                totalPlaced += placed;
            }

            held.Count -= totalPlaced;
            if (held.Count <= 0)
            {
                held.ItemId = 0;
                held.Count = 0;
            }
        }

        // Adds items to the inventory by stacking or using empty slots.
        // Returns the number of items successfully added.
        public static int TryGiveItem(
            DynamicBuffer<InventoryElement> inv,
            uint itemId,
            int quantity
        )
        {
            if (itemId == 0 || quantity <= 0)
                return 0;

            int maxStack = ItemDatabase.GetMaxStack(itemId);
            return MergeThenFill(
                inv,
                itemId,
                quantity,
                0,
                inv.Length - 1,
                maxStack,
                null
            );
        }

        // Merges items into matching stacks or empty slots within a range.
        private static int MergeThenFill(
            DynamicBuffer<InventoryElement> destInv,
            uint itemId,
            int amount,
            int start,
            int end,
            int maxStack,
            IInventoryValidator validator
        )
        {
            int remaining = amount;

            // Validate with the added count rather than the total remaining.
            for (int i = start; i <= end && i < destInv.Length && remaining > 0; i++)
            {
                if (destInv[i].ItemId != itemId)
                    continue;

                int space = maxStack - destInv[i].Count;
                if (space <= 0)
                    continue;

                int toAdd = math.min(remaining, space);
                if (validator != null && !validator.CanInsert(i, itemId, toAdd))
                    continue;

                var element = destInv[i];
                element.Count += toAdd;
                destInv[i] = element;
                remaining -= toAdd;
            }

            for (int i = start; i <= end && i < destInv.Length && remaining > 0; i++)
            {
                if (destInv[i].ItemId != 0)
                    continue;

                int toAdd = math.min(remaining, maxStack);
                if (validator != null && !validator.CanInsert(i, itemId, toAdd))
                    continue;

                destInv[i] = new InventoryElement { ItemId = itemId, Count = toAdd };
                remaining -= toAdd;
            }

            return amount - remaining;
        }
    }
}
