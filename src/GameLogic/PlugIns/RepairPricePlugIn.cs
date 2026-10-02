// <copyright file="RepairPricePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces the repair price, which originally depends on the item value (and makes wings very expensive),
/// by a flat price per missing durability point. Dark Horse, Dark Raven and ammunition keep the original price.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.RepairPricePlugIn_Name), Description = nameof(PlugInResources.RepairPricePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("7B3E2C51-9A4D-4F86-B1C2-5D8E6F0A3B97")]
public class RepairPricePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<RepairPriceConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public RepairPriceConfiguration? Configuration { get; set; }

    /// <summary>
    /// Determines whether the wings price applies to the item. Must match the client (GameLogic/Items/RepairPrice.h).
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <returns><c>true</c> for wings and capes.</returns>
    public static bool IsWingsPrice(ItemDefinition definition) => definition.Group == 12 || (definition.Group == 13 && definition.Number == 30);

    /// <inheritdoc />
    public object CreateDefaultConfig() => new RepairPriceConfiguration();

    /// <summary>
    /// Calculates the repair price.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="npcDiscount">If set to <c>true</c>, the item is repaired through an NPC.</param>
    /// <returns>The price, or <c>null</c> if the original price applies to this item.</returns>
    public long? CalculateRepairPrice(Item item, bool npcDiscount)
    {
        if (item.Definition is not { } definition
            || definition.IsAmmunition
            || item.IsTrainablePet())
        {
            return null;
        }

        var maximumDurability = item.GetMaximumDurabilityOfOnePiece();
        var missing = maximumDurability - item.Durability();
        if (missing <= 0)
        {
            return 0;
        }

        var configuration = this.Configuration ??= (RepairPriceConfiguration)this.CreateDefaultConfig();
        var pricePerDurability = IsWingsPrice(definition) ? configuration.WingsPricePerDurability : configuration.PricePerDurability;
        double price = (double)pricePerDurability * missing;
        if (item.Durability() == 0)
        {
            price *= configuration.BrokenItemMultiplier;
        }

        if (!npcDiscount)
        {
            price *= configuration.SelfRepairMultiplier;
        }

        return RoundPrice((long)price);
    }

    private static long RoundPrice(long price)
    {
        if (price >= 1000)
        {
            return price / 100 * 100;
        }

        if (price >= 100)
        {
            return price / 10 * 10;
        }

        return price;
    }
}
