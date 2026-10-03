// <copyright file="KundunSymbolsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.KundunSymbols;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Symbols of Kundun are a currency: picked up symbols are added to a counter of the character
/// (like zen, without a stack limit) instead of the inventory, so they can't be traded.
/// They are spent in the symbol shop (by default Delgado).
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.KundunSymbolsPlugIn_Name), Description = nameof(PlugInResources.KundunSymbolsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("B83E5D27-9A41-4C6F-A2D8-1E7C4B9F3A56")]
public class KundunSymbolsPlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn, IItemMovedPlugIn, ISupportCustomConfiguration<KundunSymbolsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public KundunSymbolsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new KundunSymbolsConfiguration();

    /// <summary>
    /// Gets the configuration, or the default one, if none is set.
    /// </summary>
    /// <returns>The configuration.</returns>
    public KundunSymbolsConfiguration GetConfiguration() => this.Configuration ??= new KundunSymbolsConfiguration();

    /// <summary>
    /// Determines whether the currency is available, i.e. its attribute is part of the configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns><c>true</c>, if the currency is available.</returns>
    public bool IsAvailable(GameConfiguration gameConfiguration) => gameConfiguration.Attributes.Any(a => a.Id == Stats.KundunSymbols.Id);

    /// <summary>
    /// Gets the symbol balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The balance.</returns>
    public int GetBalance(Player player) => (int)player.GetStoredStatValue(Stats.KundunSymbols);

    /// <summary>
    /// Determines whether the npc is the symbol shop.
    /// </summary>
    /// <param name="npc">The npc definition.</param>
    /// <returns><c>true</c>, if the npc sells for symbols.</returns>
    public bool IsSymbolShop(MonsterDefinition? npc) => npc is not null && npc.Number == this.GetConfiguration().ShopNpcNumber;

    /// <summary>
    /// Adds symbols to the balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns><c>true</c>, if the symbols have been added.</returns>
    public async ValueTask<bool> TryAddAsync(Player player, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        var balance = this.GetBalance(player) + amount;
        if (!player.TrySetStoredStatValue(Stats.KundunSymbols, balance))
        {
            return false;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunSymbolsCollectedFormat), amount, balance).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IKundunSymbolsViewPlugIn>(p => p.ShowBalanceAsync(balance)).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Tries to spend symbols of the balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns><c>true</c>, if the symbols have been spent.</returns>
    public async ValueTask<bool> TrySpendAsync(Player player, int amount)
    {
        var balance = this.GetBalance(player);
        if (amount < 0 || balance < amount)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunSymbolsNotEnoughFormat), amount, balance).ConfigureAwait(false);
            return false;
        }

        if (!player.TrySetStoredStatValue(Stats.KundunSymbols, balance - amount))
        {
            return false;
        }

        await player.InvokeViewPlugInAsync<IKundunSymbolsViewPlugIn>(p => p.ShowBalanceAsync(balance - amount)).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Collects the symbols of the item into the balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The symbol item.</param>
    /// <returns><c>true</c>, if the symbols have been collected.</returns>
    public ValueTask<bool> TryCollectAsync(Player player, Item item)
    {
        if (!item.IsSymbolOfKundun())
        {
            return ValueTask.FromResult(false);
        }

        return this.TryAddAsync(player, Math.Max(1, (int)item.Durability));
    }

    /// <summary>
    /// Sends the prices of the symbol shop to the player, after it opened the merchant store of an npc.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="npc">The npc definition.</param>
    public async ValueTask MerchantStoreOpenedAsync(Player player, MonsterDefinition npc)
    {
        if (!this.IsSymbolShop(npc) || npc.MerchantStore is not { } store)
        {
            await player.InvokeViewPlugInAsync<IKundunSymbolsViewPlugIn>(p => p.ShowShopPricesAsync([])).ConfigureAwait(false);
            return;
        }

        var configuration = this.GetConfiguration();
        var prices = store.Items
            .Select(item => (Slot: item.ItemSlot, Price: configuration.GetPrice(item) ?? 0))
            .ToList();
        await player.InvokeViewPlugInAsync<IKundunSymbolsViewPlugIn>(p => p.ShowShopPricesAsync(prices)).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunSymbolsBalanceFormat), this.GetBalance(player)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld || previousState != PlayerState.CharacterSelection)
        {
            return;
        }

        if (!this.IsAvailable(player.GameContext.Configuration))
        {
            return;
        }

        // Symbols which are still items (e.g. from before the currency was introduced) are collected.
        var symbols = player.Inventory?.Items.Where(item => item.IsSymbolOfKundun()).ToList() ?? [];
        foreach (var symbol in symbols)
        {
            await this.CollectInventoryItemAsync(player, symbol).ConfigureAwait(false);
        }

        var balance = this.GetBalance(player);
        await player.InvokeViewPlugInAsync<IKundunSymbolsViewPlugIn>(p => p.ShowBalanceAsync(balance)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ItemMovedAsync(Player player, Item item)
    {
        // e.g. a symbol which was taken out of the vault.
        if (item.IsSymbolOfKundun() && player.Inventory?.Items.Contains(item) is true && this.IsAvailable(player.GameContext.Configuration))
        {
            await this.CollectInventoryItemAsync(player, item).ConfigureAwait(false);
        }
    }

    private async ValueTask CollectInventoryItemAsync(Player player, Item item)
    {
        if (await this.TryCollectAsync(player, item).ConfigureAwait(false))
        {
            await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
        }
    }
}
