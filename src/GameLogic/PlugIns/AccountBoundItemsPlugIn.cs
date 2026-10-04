// <copyright file="AccountBoundItemsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Items which are bound to the account (e.g. the large potions of the essence shop): they can be moved through
/// the vault to the other characters of the account, but can't be traded, put into the personal store, dropped
/// or taken out of the game to the market of the website. The size of a stack doesn't matter.
/// </summary>
[PlugIn]
[Display(Name = "Account bound items", Description = "Items (group/number) which stay on the account: vault yes; trade, personal store, dropping and the website market no.")]
[Guid("5E2A7C14-3B98-4D61-A0F7-9C4E1B2D8A35")]
public class AccountBoundItemsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<AccountBoundItemsConfiguration>, ISupportDefaultCustomConfiguration
{
    private string? _parsedText;
    private HashSet<(byte Group, short Number)> _items = [];

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public AccountBoundItemsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Determines whether the item is bound to the account in the game context.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item is bound to the account.</returns>
    public static bool IsAccountBound(IGameContext gameContext, Item item)
        => gameContext.FeaturePlugIns.GetPlugIn<AccountBoundItemsPlugIn>()?.IsAccountBound(item) == true;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new AccountBoundItemsConfiguration();

    /// <summary>
    /// Determines whether the item is bound to the account.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item is bound to the account.</returns>
    public bool IsAccountBound(Item item)
    {
        if (item.Definition is not { } definition)
        {
            return false;
        }

        var text = (this.Configuration ??= new AccountBoundItemsConfiguration()).Items ?? string.Empty;
        if (!ReferenceEquals(text, this._parsedText))
        {
            this._items = Parse(text);
            this._parsedText = text;
        }

        return this._items.Contains((definition.Group, definition.Number));
    }

    private static HashSet<(byte Group, short Number)> Parse(string text)
    {
        var items = new HashSet<(byte Group, short Number)>();
        foreach (var entry in text.Split([';', ',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split('/');
            if (parts.Length == 2 && byte.TryParse(parts[0], out var group) && short.TryParse(parts[1], out var number))
            {
                items.Add((group, number));
            }
        }

        return items;
    }
}

/// <summary>
/// The configuration of the <see cref="AccountBoundItemsPlugIn"/>.
/// </summary>
public class AccountBoundItemsConfiguration
{
    /// <summary>
    /// Gets or sets the account bound items as "group/number", separated by ';'.
    /// </summary>
    [Display(Name = "Items", Description = "Items as group/number, separated by ';' (default: 14/3 large healing potion; 14/6 large mana potion).")]
    public string Items { get; set; } = "14/3; 14/6";
}

/// <summary>
/// Keeps the account bound items out of the trade and the personal store; the vault is allowed.
/// </summary>
[PlugIn]
[Display(Name = "Account bound items: moving", Description = "Account bound items can't be put into the trade or the personal store.")]
[Guid("9C61D2E8-4A37-4F05-B8E2-71D3A5C9F046")]
public class AccountBoundItemMovingPlugIn : IItemMovingPlugIn
{
    /// <inheritdoc />
    public void ItemMoving(Player player, Item item, Storages targetStorage, int slot, CancelEventArgs eventArgs)
    {
        if (targetStorage is not (Storages.Trade or Storages.PersonalStore)
            || !AccountBoundItemsPlugIn.IsAccountBound(player.GameContext, item))
        {
            return;
        }

        eventArgs.Cancel = true;
        _ = player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemIsBoundToAccount)).AsTask();
    }
}

/// <summary>
/// Account bound items can't be dropped to the ground.
/// </summary>
[PlugIn]
[Display(Name = "Account bound items: dropping", Description = "Account bound items can't be dropped to the ground.")]
[Guid("2B8F4E61-7D05-4C9A-93E1-6A0C5F7B2D18")]
public class AccountBoundItemDropPlugIn : IItemDropPlugIn
{
    /// <inheritdoc />
    public async ValueTask HandleItemDropAsync(Player player, Item item, Point target, IItemDropPlugIn.ItemDropArguments cancelArgs)
    {
        if (cancelArgs.WasHandled || !AccountBoundItemsPlugIn.IsAccountBound(player.GameContext, item))
        {
            return;
        }

        cancelArgs.WasHandled = true;
        cancelArgs.Success = false;
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemIsBoundToAccount)).ConfigureAwait(false);
    }
}
