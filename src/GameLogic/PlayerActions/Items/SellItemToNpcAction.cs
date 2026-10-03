// <copyright file="SellItemToNpcAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Action to sell an item to a npc merchant.
/// </summary>
public class SellItemToNpcAction
{
    /// <summary>
    /// The flat price which the player gets for any item (or stack of items) sold to an npc merchant.
    /// Zen is intended to be obtained through drops only, selling is just a way to get rid of items.
    /// The <see cref="ItemPriceCalculator"/> is intentionally not used here, because its selling price
    /// is also used by other features (e.g. success rates of the fenrir crafting).
    /// </summary>
    private const int FlatSellingPrice = 1;

    /// <summary>
    /// Sells the item of the specified slot to the npc merchant.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The slot.</param>
    /// <returns><c>True</c>, if the item was sold; otherwise, <c>false</c>.</returns>
    public async ValueTask<bool> SellItemAsync(Player player, byte slot)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        var item = player.Inventory?.GetItem(slot);
        if (item is null)
        {
            player.Logger.LogWarning("Player {0} requested to sell item at slot {1}, but item wasn't found.", player, slot);
            await player.InvokeViewPlugInAsync<IItemSoldToNpcPlugIn>(p => p.ItemSoldToNpcAsync(false)).ConfigureAwait(false);
            return false;
        }

        if (player.OpenedNpc?.Definition.MerchantStore is null)
        {
            player.Logger.LogWarning("Player {0} requested to sell item at slot {1} to an npc, but no npc merchant store is currently opened.", player, slot);
            await player.InvokeViewPlugInAsync<IItemSoldToNpcPlugIn>(p => p.ItemSoldToNpcAsync(false)).ConfigureAwait(false);
            return false;
        }

        if (item.Definition is null
            || (item.Definition.IsBoundToCharacter && (item.Definition.Durability == 0 || item.Durability > 0))
            || item.IsBoundToCharacter())
        {
            await player.InvokeViewPlugInAsync<IItemSoldToNpcPlugIn>(p => p.ItemSoldToNpcAsync(false)).ConfigureAwait(false);
            return false;
        }

        return await this.SellItemAsync(player, item).ConfigureAwait(false);
    }

    private async ValueTask<bool> SellItemAsync(Player player, Item item)
    {
        var sellingPrice = FlatSellingPrice;
        player.Logger.LogDebug("Flat selling price {0} for item {1}", sellingPrice, item);
        if (!player.TryAddMoney(sellingPrice))
        {
            // The money doesn't fit into the inventory anymore. Without the answer the request would
            // stay unanswered - the client keeps waiting, and the player gets no hint why nothing
            // happened. All other refusals above already report back this way.
            player.Logger.LogDebug("Item {0} not sold, the money of player {1} is at its maximum.", item, player);
            await player.InvokeViewPlugInAsync<IItemSoldToNpcPlugIn>(p => p.ItemSoldToNpcAsync(false)).ConfigureAwait(false);
            return false;
        }

        player.Logger.LogDebug("Sold Item {0} for price: {1}", item, sellingPrice);
        await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemSoldToNpcPlugIn>(p => p.ItemSoldToNpcAsync(true)).ConfigureAwait(false);

        player.GameContext.PlugInManager.GetPlugInPoint<IItemSoldToMerchantPlugIn>()?.ItemSold(player, item, player.OpenedNpc!);
        return true;
    }
}