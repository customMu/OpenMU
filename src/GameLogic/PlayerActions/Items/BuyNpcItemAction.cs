// <copyright file="BuyNpcItemAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using MUnique.OpenMU.GameLogic.CastleSiege;
using MUnique.OpenMU.GameLogic.KundunSymbols;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Action to buy items from a Monster merchant.
/// </summary>
public class BuyNpcItemAction
{
    private readonly CastleSiegeTaxProvider _castleSiegeTaxProvider = new();
    private readonly ItemPriceCalculator _priceCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuyNpcItemAction"/> class.
    /// </summary>
    public BuyNpcItemAction()
    {
        this._priceCalculator = new ItemPriceCalculator();
    }

    /// <summary>
    /// Buys the item of the specified slot from the <see cref="Player.OpenedNpc"/> merchant store.
    /// </summary>
    /// <param name="player">The player who buys the item.</param>
    /// <param name="slot">The slot of the item.</param>
    public async ValueTask BuyItemAsync(Player player, byte slot)
    {
        if (player.OpenedNpc is null)
        {
            await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
            return;
        }

        var npcDefinition = player.OpenedNpc.Definition;
        if (npcDefinition?.MerchantStore is null || npcDefinition.MerchantStore.Items.Count == 0)
        {
            await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
            return;
        }

        var storeItem = npcDefinition.MerchantStore.Items.FirstOrDefault(i => i.ItemSlot == slot);
        if (storeItem is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemUnknown)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
            return;
        }

        var symbolShop = player.GameContext.FeaturePlugIns.GetPlugIn<KundunSymbolsPlugIn>() is { } symbols && symbols.IsSymbolShop(npcDefinition)
            ? symbols
            : null;
        if (symbolShop is not null && symbolShop.GetConfiguration().GetPrice(storeItem) is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunSymbolsItemNotForSale)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
            return;
        }

        // Inventory Update:
        if (storeItem.IsStackable() && player.Inventory!.Items.FirstOrDefault(item => storeItem.CanCompletelyStackOn(item)) is { } targetItem)
        {
            if (!await this.CheckMoneyAsync(player, storeItem, symbolShop).ConfigureAwait(false))
            {
                await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
                return;
            }

            targetItem.Durability += storeItem.Durability;
            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(targetItem, false)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
        }
        else
        {
            var toSlot = player.Inventory!.CheckInvSpace(storeItem);
            if (toSlot is null)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryFull)).ConfigureAwait(false);
                await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
                return;
            }

            if (!await this.CheckMoneyAsync(player, storeItem, symbolShop).ConfigureAwait(false))
            {
                if (symbolShop is null)
                {
                    await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnoughMoney)).ConfigureAwait(false);
                }

                await player.InvokeViewPlugInAsync<IBuyNpcItemFailedPlugIn>(p => p.BuyNpcItemFailedAsync()).ConfigureAwait(false);
                return;
            }

            var newItem = player.PersistenceContext.CreateNew<Item>();
            newItem.AssignValues(storeItem);
            newItem.ItemSlot = (byte)toSlot;
            await player.InvokeViewPlugInAsync<INpcItemBoughtPlugIn>(p => p.NpcItemBoughtAsync(newItem)).ConfigureAwait(false);
            await player.Inventory.AddItemAsync(newItem).ConfigureAwait(false);
            player.GameContext.PlugInManager.GetPlugInPoint<IItemBoughtFromMerchantPlugIn>()?.ItemBought(player, newItem, storeItem, player.OpenedNpc);
        }

        await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
    }

    private ValueTask<bool> CheckMoneyAsync(Player player, Item item, KundunSymbolsPlugIn? symbolShop)
    {
        if (symbolShop is not null)
        {
            // The symbol shop sells for Symbols of Kundun instead of zen.
            return symbolShop.TrySpendAsync(player, symbolShop.GetConfiguration().GetPrice(item) ?? int.MaxValue);
        }

        var price = this._priceCalculator.CalculateFinalBuyingPrice(item);
        return this._castleSiegeTaxProvider.TryPayStoreCostAsync(player, price);
    }
}
