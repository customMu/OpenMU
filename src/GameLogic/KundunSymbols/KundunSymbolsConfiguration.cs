// <copyright file="KundunSymbolsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.KundunSymbols;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="KundunSymbolsPlugIn"/>.
/// </summary>
public class KundunSymbolsConfiguration
{
    /// <summary>
    /// Gets or sets the number of the npc whose merchant store sells for symbols instead of zen.
    /// </summary>
    [Display(Name = "Symbol shop NPC number", Description = "The merchant store of this npc (by default Delgado in Lorencia) sells for Symbols of Kundun instead of zen. The items of the store are edited at the npc (merchant store), the prices here.")]
    public short ShopNpcNumber { get; set; } = 478;

    /// <summary>
    /// Gets or sets the prices of the symbol shop.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Prices", Description = "Prices in Symbols of Kundun by item group, number and level (-1 = any level). The most specific entry counts. Items without a price can't be bought.")]
    public ICollection<KundunSymbolPrice> Prices { get; set; } = CreateDefaultPrices();

    /// <summary>
    /// Gets the price of the item in symbols.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The price per piece, or <c>null</c>, if the item can't be bought.</returns>
    public int? GetPrice(Item item)
    {
        if (item.Definition is not { } definition)
        {
            return null;
        }

        var entry = this.Prices
            .Where(p => p.Group == definition.Group && p.Number == definition.Number && (p.Level < 0 || p.Level == item.Level))
            .OrderByDescending(p => p.Level)
            .FirstOrDefault();
        return entry is { Price: > 0 } ? entry.Price : null;
    }

    private static List<KundunSymbolPrice> CreateDefaultPrices() =>
    [
        new() { Name = "Jewel of Bless", Group = 14, Number = 13, Level = -1, Price = 20 },
        new() { Name = "Jewel of Soul", Group = 14, Number = 14, Level = -1, Price = 30 },
        new() { Name = "Jewel of Chaos", Group = 12, Number = 15, Level = -1, Price = 15 },
        new() { Name = "Jewel of Life", Group = 14, Number = 16, Level = -1, Price = 60 },
        new() { Name = "Jewel of Creation", Group = 14, Number = 22, Level = -1, Price = 80 },
        new() { Name = "Box of Kundun +1", Group = 14, Number = 11, Level = 8, Price = 50 },
        new() { Name = "Box of Kundun +2", Group = 14, Number = 11, Level = 9, Price = 100 },
        new() { Name = "Box of Kundun +3", Group = 14, Number = 11, Level = 10, Price = 175 },
        new() { Name = "Box of Kundun +4", Group = 14, Number = 11, Level = 11, Price = 275 },
        new() { Name = "Box of Kundun +5", Group = 14, Number = 11, Level = 12, Price = 400 },
    ];
}
