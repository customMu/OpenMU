// <copyright file="KundunEssenceConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.KundunEssence;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="KundunEssencePlugIn"/>.
/// </summary>
public class KundunEssenceConfiguration
{
    /// <summary>
    /// Gets or sets the number of the npc whose merchant store sells for essence instead of zen.
    /// </summary>
    [Display(Name = "Essence shop NPC number", Description = "The merchant store of this npc (by default Delgado in Lorencia) sells for Kundun Essence instead of zen. The items of the store are edited at the npc (merchant store), the prices here.")]
    public short ShopNpcNumber { get; set; } = 478;

    /// <summary>
    /// Gets or sets a value indicating whether lost maps which are bought in the essence shop are bound to the character.
    /// </summary>
    [Display(Name = "Bind bought lost maps", Description = "Lost maps which are bought for essence are bound to the character: they can't be traded, sold or put into the vault. By default, they are tradable like the dropped ones; only lost maps of a donation are bound.")]
    public bool BindBoughtLostMaps { get; set; }

    /// <summary>
    /// Gets or sets the prices of the essence shop.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Prices", Description = "Prices in Kundun Essence by item group, number and level (-1 = any level). The most specific entry counts. Items without a price can't be bought.")]
    public ICollection<KundunEssencePrice> Prices { get; set; } = CreateDefaultPrices();

    /// <summary>
    /// Gets the price of the item in essence.
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
            .Where(p => p.Group == definition.Group && p.Number == definition.Number && (p.Level < 0 || p.Level == item.Level)
                        && (p.Amount <= 0 || p.Amount == (int)item.Durability))
            .OrderByDescending(p => p.Level)
            .ThenByDescending(p => p.Amount)
            .FirstOrDefault();
        return entry is { Price: > 0 } ? entry.Price : null;
    }

    private static List<KundunEssencePrice> CreateDefaultPrices() =>
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
        .. CreateLostMapPrices(),
        .. CreateLargePotionPrices(),
    ];

    /// <summary>
    /// The large healing and mana potions are only sold here, in stacks of 50, 100 and 255; the bigger stacks are cheaper per potion.
    /// The regular stores sell the small and medium potions.
    /// </summary>
    /// <returns>The prices of the large potions.</returns>
    private static IEnumerable<KundunEssencePrice> CreateLargePotionPrices() =>
        from potion in new[] { (Name: "Large Healing Potion", Number: (short)3), (Name: "Large Mana Potion", Number: (short)6) }
        from stack in new[] { (Amount: 50, Price: 5), (Amount: 100, Price: 9), (Amount: 255, Price: 20) }
        select new KundunEssencePrice { Name = $"{potion.Name} x{stack.Amount}", Group = 14, Number = potion.Number, Level = -1, Amount = stack.Amount, Price = stack.Price };

    /// <summary>
    /// The lost map +N (the entry fee of the chamber of Kundun N) costs 25 x N essence, which is about
    /// 5 days of the Kalima N instance with 5 x N essence for the Illusion of Kundun.
    /// </summary>
    /// <returns>The prices of the lost maps.</returns>
    private static IEnumerable<KundunEssencePrice> CreateLostMapPrices() =>
        Enumerable.Range(1, 7).Select(level => new KundunEssencePrice { Name = $"Lost Map +{level}", Group = 14, Number = 28, Level = level, Price = 25 * level });
}
