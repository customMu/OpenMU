// <copyright file="RepairPriceConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for the <see cref="RepairPricePlugIn"/>.
/// The repair price is <c>PricePerDurability * missingDurability</c>, multiplied by
/// <see cref="SelfRepairMultiplier"/> without an NPC and by <see cref="BrokenItemMultiplier"/> at zero durability.
/// The client calculates the displayed price with the same values (GameLogic/Items/RepairPrice.h), keep them in sync.
/// </summary>
public class RepairPriceConfiguration
{
    /// <summary>
    /// Gets or sets the price per missing durability point of weapons, armor, shields, rings, pendants and pets in the pet slot.
    /// </summary>
    [Display(Name = "Price per durability", Description = "Price per missing durability point of weapons, armor, shields, rings, pendants and pets (except Dark Horse/Dark Raven).")]
    public int PricePerDurability { get; set; } = 50;

    /// <summary>
    /// Gets or sets the price per missing durability point of wings and capes.
    /// </summary>
    [Display(Name = "Wings price per durability", Description = "Price per missing durability point of wings and capes.")]
    public int WingsPricePerDurability { get; set; } = 100;

    /// <summary>
    /// Gets or sets the multiplier when repairing without an NPC (from the inventory).
    /// </summary>
    [Display(Name = "Self repair multiplier", Description = "Multiplier when the item is repaired from the inventory, without an NPC.")]
    public float SelfRepairMultiplier { get; set; } = 2.5f;

    /// <summary>
    /// Gets or sets the multiplier when the item has zero durability.
    /// </summary>
    [Display(Name = "Broken item multiplier", Description = "Multiplier when the durability of the item is zero.")]
    public float BrokenItemMultiplier { get; set; } = 1.4f;
}
