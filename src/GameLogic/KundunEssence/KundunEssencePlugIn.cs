// <copyright file="KundunEssencePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.KundunEssence;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Kundun Essence is the currency of the Kalima instance. It's a counter of the character
/// (like zen, without a stack limit), so it can't be traded. It's spent in the essence shop (by default Delgado).
/// </summary>
/// <remarks>
/// The essence uses the attribute of the former currency 'Symbols of Kundun', so the balances are kept 1:1.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.KundunEssencePlugIn_Name), Description = nameof(PlugInResources.KundunEssencePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("B83E5D27-9A41-4C6F-A2D8-1E7C4B9F3A56")]
public class KundunEssencePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<KundunEssenceConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public KundunEssenceConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new KundunEssenceConfiguration();

    /// <summary>
    /// Gets the configuration, or the default one, if none is set.
    /// </summary>
    /// <returns>The configuration.</returns>
    public KundunEssenceConfiguration GetConfiguration() => this.Configuration ??= new KundunEssenceConfiguration();

    /// <summary>
    /// Determines whether the currency is available, i.e. its attribute is part of the configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns><c>true</c>, if the currency is available.</returns>
    public bool IsAvailable(GameConfiguration gameConfiguration) => gameConfiguration.Attributes.Any(a => a.Id == Stats.KundunEssence.Id);

    /// <summary>
    /// Gets the essence balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The balance.</returns>
    public int GetBalance(Player player) => (int)player.GetStoredStatValue(Stats.KundunEssence);

    /// <summary>
    /// Determines whether the npc is the essence shop.
    /// </summary>
    /// <param name="npc">The npc definition.</param>
    /// <returns><c>true</c>, if the npc sells for essence.</returns>
    public bool IsEssenceShop(MonsterDefinition? npc) => npc is not null && npc.Number == this.GetConfiguration().ShopNpcNumber;

    /// <summary>
    /// Adds essence to the balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns><c>true</c>, if the essence has been added.</returns>
    public async ValueTask<bool> TryAddAsync(Player player, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        var balance = this.GetBalance(player) + amount;
        if (!player.TrySetStoredStatValue(Stats.KundunEssence, balance))
        {
            return false;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunEssenceCollectedFormat), amount, balance).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IKundunEssenceViewPlugIn>(p => p.ShowBalanceAsync(balance)).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Tries to spend essence of the balance of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns><c>true</c>, if the essence has been spent.</returns>
    public async ValueTask<bool> TrySpendAsync(Player player, int amount)
    {
        var balance = this.GetBalance(player);
        if (amount < 0 || balance < amount)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunEssenceNotEnoughFormat), amount, balance).ConfigureAwait(false);
            return false;
        }

        if (!player.TrySetStoredStatValue(Stats.KundunEssence, balance - amount))
        {
            return false;
        }

        await player.InvokeViewPlugInAsync<IKundunEssenceViewPlugIn>(p => p.ShowBalanceAsync(balance - amount)).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Prepares an item which was bought in the essence shop, e.g. binds a lost map to the character.
    /// </summary>
    /// <param name="item">The bought item.</param>
    public void PrepareBoughtItem(Item item)
    {
        if (this.GetConfiguration().BindBoughtLostMaps)
        {
            item.TryBindLostMap();
        }
    }

    /// <summary>
    /// Sends the prices of the essence shop to the player, after it opened the merchant store of an npc.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="npc">The npc definition.</param>
    public async ValueTask MerchantStoreOpenedAsync(Player player, MonsterDefinition npc)
    {
        if (!this.IsEssenceShop(npc) || npc.MerchantStore is not { } store)
        {
            await player.InvokeViewPlugInAsync<IKundunEssenceViewPlugIn>(p => p.ShowShopPricesAsync([])).ConfigureAwait(false);
            return;
        }

        var configuration = this.GetConfiguration();
        var prices = store.Items
            .Select(item => (Slot: item.ItemSlot, Price: configuration.GetPrice(item) ?? 0))
            .ToList();
        await player.InvokeViewPlugInAsync<IKundunEssenceViewPlugIn>(p => p.ShowShopPricesAsync(prices)).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunEssenceBalanceFormat), this.GetBalance(player)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld
            || previousState != PlayerState.CharacterSelection
            || !this.IsAvailable(player.GameContext.Configuration))
        {
            return;
        }

        var balance = this.GetBalance(player);
        await player.InvokeViewPlugInAsync<IKundunEssenceViewPlugIn>(p => p.ShowBalanceAsync(balance)).ConfigureAwait(false);
    }
}
