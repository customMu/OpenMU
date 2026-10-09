// <copyright file="ItemLevelUpgradeCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.GameLogic.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// The Chaos Machine mixes +10..+15 (09.10.2026, wiki §7): the Jewels of Bless and Soul of the mix are multiplied by the
/// rank of the item - x1 (ranks 1-3), x2 (4-5), x3 (6-8), see <see cref="EnchantPriceRanks"/>. The client shows the
/// amounts in the Chaos Machine. Set as the handler of the craftings 3, 4, 22, 23, 49, 50 (Server/enchant-price.sql).
/// </summary>
public class ItemLevelUpgradeCrafting : SimpleItemCraftingHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemLevelUpgradeCrafting"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public ItemLevelUpgradeCrafting(SimpleCraftingSettings settings)
        : base(settings)
    {
    }

    /// <inheritdoc />
    public override CraftingResult? TryGetRequiredItems(Player player, out IList<CraftingRequiredItemLink> items, out byte successRate)
    {
        var storage = player.TemporaryStorage?.Items.ToList() ?? [];
        var item = FindItem(storage);
        player.Logger.LogInformation(
            "Chaos Machine +10..+15: item {item}, price rank {rank}, jewels x{multiplier}",
            item,
            EnchantPriceRanks.GetPriceRank(item?.Definition),
            EnchantPriceRanks.GetChaosMachineMultiplier(item?.Definition));
        return base.TryGetRequiredItems(player, out items, out successRate);
    }

    /// <inheritdoc />
    protected override int GetAmountMultiplier(ItemCraftingRequiredItem requiredItem, IList<Item> foundItems, IList<Item> storage)
    {
        // the Bless and Soul of the mix (by the found items: the possible items of the requirement are not reliable here)
        if (foundItems.Count == 0 || !foundItems.All(i => i.Definition is { Group: 14, Number: 13 or 14 }))
        {
            return 1;
        }

        return EnchantPriceRanks.GetChaosMachineMultiplier(FindItem(storage)?.Definition);
    }

    // the enchanted item: the one which is not a jewel
    private static Item? FindItem(IList<Item> storage)
    {
        return storage.FirstOrDefault(i => i.Definition is { } d && !(d.Group == 12 && d.Number == 15) && !(d.Group == 14 && d.Number is 13 or 14));
    }
}
