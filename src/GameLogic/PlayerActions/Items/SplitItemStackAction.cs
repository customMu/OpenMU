// <copyright file="SplitItemStackAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Action to split a part of a stackable item (e.g. jewels) into a new stack in a free inventory slot.
/// </summary>
public class SplitItemStackAction
{
    /// <summary>
    /// The minimum time between two splits of the same player. There is no general rate limiting
    /// for player requests, so this action protects itself against spamming.
    /// </summary>
    private const long MinimumIntervalMilliseconds = 250;

    private static readonly ConditionalWeakTable<Player, StrongBox<long>> LastSplitTimes = new();

    /// <summary>
    /// Splits <paramref name="amount"/> pieces from the stack in the given inventory slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The inventory slot of the stack which should be split.</param>
    /// <param name="amount">The amount of pieces which should be moved to the new stack.</param>
    /// <param name="group">The item group the client expects in the slot. It guards against a slot which changed in the meantime.</param>
    /// <param name="number">The item number the client expects in the slot.</param>
    public async ValueTask SplitAsync(Player player, byte slot, byte amount, byte group, short number)
    {
        if (player.SelectedCharacter is null || player.Inventory is null)
        {
            return;
        }

        // Only in the plain "entered world" state: no trade, no npc dialog (vault, chaos machine, ...) and no open personal store.
        if (player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || (player.ShopStorage?.StoreOpen ?? false)
            || IsRateLimited(player))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.SplitStackBusy)).ConfigureAwait(false);
            return;
        }

        if (slot <= InventoryConstants.LastEquippableItemSlotIndex || slot >= InventoryConstants.FirstStoreItemSlotIndex)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.SplitStackInvalid)).ConfigureAwait(false);
            return;
        }

        var item = player.Inventory.GetItem(slot);
        if (item?.Definition is null
            || !item.IsStackable()
            || item.Definition.Group != group
            || item.Definition.Number != number
            || amount == 0
            || amount >= item.Durability)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.SplitStackInvalid)).ConfigureAwait(false);
            return;
        }

        // We check the space with a temporary item, to not leave a persistent item behind if there's no space.
        var probe = new TemporaryItem
        {
            Definition = item.Definition,
            Level = item.Level,
            Durability = amount,
        };

        var freeSlot = player.Inventory.CheckInvSpace(probe);
        if (freeSlot is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryNotEnoughSpace)).ConfigureAwait(false);
            return;
        }

        var newItem = probe.MakePersistent(player.PersistenceContext);
        newItem.ItemSlot = freeSlot.Value;
        item.Durability -= amount;
        if (!await player.Inventory.AddItemAsync(freeSlot.Value, newItem).ConfigureAwait(false))
        {
            // Roll back, so no pieces get lost.
            item.Durability += amount;
            await player.PersistenceContext.DeleteAsync(newItem).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.SplitStackInvalid)).ConfigureAwait(false);
            return;
        }

        player.Logger.LogInformation(
            "Split stack of {item}: moved {amount} pieces from slot {slot} to slot {newSlot}, {remaining} pieces remain.",
            item.Definition.Name,
            amount,
            slot,
            freeSlot.Value,
            item.Durability);

        await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(item, false)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(newItem)).ConfigureAwait(false);
    }

    private static bool IsRateLimited(Player player)
    {
        var lastTime = LastSplitTimes.GetOrCreateValue(player);
        var now = Environment.TickCount64;
        if (now - lastTime.Value < MinimumIntervalMilliseconds)
        {
            return true;
        }

        lastTime.Value = now;
        return false;
    }
}
