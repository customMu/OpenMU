// <copyright file="PlayerItemExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Extensions for the items of a <see cref="Player"/>.
/// </summary>
public static class PlayerItemExtensions
{
    /// <summary>
    /// The attributes which are compared with the stat requirements of items by the <see cref="ItemRequirementsByBaseStatsPlugIn"/>.
    /// </summary>
    private static readonly IDictionary<AttributeDefinition, AttributeDefinition> BaseStatOfTotalStat = new Dictionary<AttributeDefinition, AttributeDefinition>
    {
        { Stats.TotalStrength, Stats.BaseStrength },
        { Stats.TotalAgility, Stats.BaseAgility },
        { Stats.TotalVitality, Stats.BaseVitality },
        { Stats.TotalEnergy, Stats.BaseEnergy },
        { Stats.TotalLeadership, Stats.BaseLeadership },
    };

    /// <summary>
    /// Gets the attributes which, when they change, may change whether equipped items comply with their requirements.
    /// </summary>
    internal static IEnumerable<AttributeDefinition> RequirementRelevantStats { get; } =
    [
        Stats.BaseStrength,
        Stats.BaseAgility,
        Stats.BaseVitality,
        Stats.BaseEnergy,
        Stats.BaseLeadership,
        Stats.Level,
        Stats.Resets,
    ];

    /// <summary>
    /// Determines whether an equipped item gives its bonuses. That's always the case, unless the
    /// <see cref="ItemRequirementsByBaseStatsPlugIn"/> is active and the player doesn't comply with the requirements of the item.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The equipped item.</param>
    /// <returns><c>True</c>, if the item gives its bonuses.</returns>
    public static bool IsEquippedItemActive(this Player player, Item item)
    {
        return player.GameContext.FeaturePlugIns.GetPlugIn<ItemRequirementsByBaseStatsPlugIn>() is null
               || player.CompliesRequirements(item);
    }

    /// <summary>
    /// Determines whether the player holds a weapon which doesn't meet its requirements, see <see cref="ItemRequirementsByBaseStatsPlugIn"/>.
    /// Such a weapon can't be used for physical attack skills. Shields and ammunition don't count as weapons.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>True</c>, if the player holds a weapon which doesn't meet its requirements.</returns>
    public static bool HasInactiveHandWeapon(this Player player)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<ItemRequirementsByBaseStatsPlugIn>() is null
            || player.Inventory is not { } inventory)
        {
            return false;
        }

        return inventory.EquippedItems.Any(item =>
            (item.ItemSlot == InventoryConstants.LeftHandSlot || item.ItemSlot == InventoryConstants.RightHandSlot)
            && item.Definition is { IsAmmunition: false, Group: <= 5 }
            && !player.CompliesRequirements(item));
    }

    /// <summary>
    /// Determines whether the player complies with the requirements of the specified item.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    /// <returns><c>True</c>, if the player complies with the requirements of the specified item; Otherwise, <c>false</c>.</returns>
    public static bool CompliesRequirements(this Player player, Item item)
    {
        item.ThrowNotInitializedProperty(item.Definition is null, nameof(item.Definition));

        var byBaseStats = player.GameContext.FeaturePlugIns.GetPlugIn<ItemRequirementsByBaseStatsPlugIn>() is not null;

        // A character with at least one reset already reached every level requirement below the reset level.
        var ignoreLevelRequirement = byBaseStats && item.IsWearable() && player.Attributes![Stats.Resets] >= 1;

        foreach (var requirement in item.Definition.Requirements.Select(item.GetRequirement))
        {
            if (ignoreLevelRequirement && requirement.Attr == Stats.Level)
            {
                continue;
            }

            var attribute = byBaseStats && BaseStatOfTotalStat.TryGetValue(requirement.Attr, out var baseStat)
                ? baseStat
                : requirement.Attr;
            if (player.Attributes![attribute] < requirement.Value)
            {
                return false;
            }
        }

        return item.Definition.QualifiedCharacters.Contains(player.SelectedCharacter!.CharacterClass!);
    }

    /// <summary>
    /// Destroys an item of the <see cref="Player.Inventory"/>.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    public static async ValueTask DestroyInventoryItemAsync(this Player player, Item item)
    {
        await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(item.ItemSlot)).ConfigureAwait(false);
        player.GameContext.PlugInManager.GetPlugInPoint<IItemDestroyedPlugIn>()?.ItemDestroyed(item);
    }

    /// <summary>
    /// Logs the items of the vault of the account which have no item definition.
    /// </summary>
    /// <param name="player">The player.</param>
    internal static void LogInvalidVaultItems(this Player player)
    {
        var invalidItems = player.Account?.Vault?.Items.Where(i => i.Definition is null);
        if (invalidItems is null)
        {
            return;
        }

        foreach (var item in invalidItems)
        {
            player.Logger.LogWarning("Account {name} has item without definition in vault, Slot: {slot}, ID: {id}", player.Account?.LoginName, item.ItemSlot, item.GetId());
        }
    }

    /// <summary>
    /// Logs the items of the inventory of the selected character which have no item definition.
    /// </summary>
    /// <param name="player">The player.</param>
    internal static void LogInvalidInventoryItems(this Player player)
    {
        var invalidItems = player.SelectedCharacter?.Inventory?.Items.Where(i => i.Definition is null);
        if (invalidItems is null)
        {
            return;
        }

        foreach (var item in invalidItems)
        {
            player.Logger.LogWarning("Character {name} has item without definition in inventory, Slot: {slot}, ID: {id}", player.SelectedCharacter?.Name, item.ItemSlot, item.GetId());
        }
    }
}
