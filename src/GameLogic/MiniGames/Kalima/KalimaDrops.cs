// <copyright file="KalimaDrops.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The drops of the Kalima instance and of the chamber of Kundun: jewels by the weights of the tier, the items of the ranks
/// of the tier (taken from the plugin 'Item drop by rank') and the weapons of the bosses.
/// </summary>
public static class KalimaDrops
{
    /// <summary>
    /// The item groups of the weapons (swords, axes, maces, spears, bows and crossbows, staffs).
    /// </summary>
    private const byte LastWeaponGroup = 5;

    private const byte BoxOfKundunGroup = 14;

    private const short BoxOfKundunNumber = 11;

    /// <summary>
    /// The item level of the Box of Kundun +1; up to +5 (level 12).
    /// </summary>
    private const int BoxOfKundunFirstLevel = 8;

    private static readonly short[] ExcellentRingNumbers = [8, 9, 21, 22, 23, 24];

    private static readonly short[] ExcellentPendantNumbers = [12, 13, 25, 26, 27, 28];

    /// <summary>
    /// Rolls the jewel of a killed monster.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <param name="items">The item definitions.</param>
    /// <returns>The jewel, or <c>null</c>.</returns>
    public static Item? RollJewel(KalimaInstanceTier tier, IEnumerable<ItemDefinition> items)
    {
        if (!Rand.NextRandomBool(Math.Clamp(tier.JewelChancePercent / 100.0, 0, 1)))
        {
            return null;
        }

        (byte Group, short Number, int Weight)[] jewels =
        [
            (12, 15, tier.ChaosWeight),
            (14, 13, tier.BlessWeight),
            (14, 14, tier.SoulWeight),
            (14, 16, tier.LifeWeight),
            (14, 22, tier.CreationWeight),
            (14, 31, tier.GuardianWeight),
        ];
        var index = RollWeighted(jewels.Select(j => j.Weight).ToList());
        if (index < 0)
        {
            return null;
        }

        var (group, number, _) = jewels[index];
        return items.FirstOrDefault(d => d.Group == group && d.Number == number) is { } definition
            ? new TemporaryItem { Definition = definition, Durability = 1 }
            : null;
    }

    /// <summary>
    /// Rolls an item of the ranks of the tier for a killed monster.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <param name="gameContext">The game context.</param>
    /// <param name="chanceMultiplier">The multiplier of the chances of the plugin 'Item drop by rank'.</param>
    /// <returns>The item, or <c>null</c>.</returns>
    public static Item? RollRankItem(KalimaInstanceTier tier, IGameContext gameContext, float chanceMultiplier)
    {
        var candidates = GetRankItems(tier, gameContext, weaponsOnly: false);
        var roll = Rand.NextDouble();
        foreach (var (definition, chance) in candidates)
        {
            var multiplied = chance * chanceMultiplier;
            if (roll < multiplied)
            {
                var rank = GetRankConfiguration(gameContext);
                return ItemDropByRankPlugIn.CreateItem(
                    definition,
                    (byte)Math.Clamp(tier.ItemLevel, 0, 15),
                    Rand.NextRandomBool((rank?.LuckChancePercent ?? 20f) / 100.0),
                    Rand.NextRandomBool((rank?.SkillChancePercent ?? 30f) / 100.0));
            }

            roll -= multiplied;
        }

        return null;
    }

    /// <summary>
    /// Creates a weapon of the ranks of the tier, all weapons are equally likely.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <param name="gameContext">The game context.</param>
    /// <param name="minimumLevel">The lowest level.</param>
    /// <param name="levelWeights">The weights of the levels, from the lowest level.</param>
    /// <param name="luckAndSkill">If set to <c>true</c>, the weapon has luck and its skill; otherwise by the chances of the plugin 'Item drop by rank'.</param>
    /// <returns>The weapon, or <c>null</c> if the ranks have none.</returns>
    public static Item? CreateWeapon(KalimaInstanceTier tier, IGameContext gameContext, int minimumLevel, IList<int> levelWeights, bool luckAndSkill)
    {
        var weapons = GetRankItems(tier, gameContext, weaponsOnly: true);
        if (weapons.Count == 0)
        {
            return null;
        }

        var definition = weapons[Rand.NextInt(0, weapons.Count)].Definition;
        var level = minimumLevel + Math.Max(0, RollWeighted(levelWeights));
        var rank = GetRankConfiguration(gameContext);
        return ItemDropByRankPlugIn.CreateItem(
            definition,
            (byte)Math.Clamp(level, 0, 15),
            luckAndSkill || Rand.NextRandomBool((rank?.LuckChancePercent ?? 20f) / 100.0),
            luckAndSkill || Rand.NextRandomBool((rank?.SkillChancePercent ?? 30f) / 100.0));
    }

    /// <summary>
    /// Creates a Box of Kundun for the item ranks of the tier: +1 for the ranks 1-2, +2 for 3-4, +3 for 5-6, +4 for 7 and +5 for 8.
    /// If the ranks of the tier need different boxes, one of them is chosen at random.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <param name="items">The item definitions.</param>
    /// <returns>The box, or <c>null</c> if the item is missing.</returns>
    public static Item? CreateBoxOfKundun(KalimaInstanceTier tier, IEnumerable<ItemDefinition> items)
    {
        if (items.FirstOrDefault(d => d is { Group: BoxOfKundunGroup, Number: BoxOfKundunNumber }) is not { } definition)
        {
            return null;
        }

        var grades = Enumerable.Range(Math.Clamp(tier.MinimumItemRank, 1, 8), Math.Max(1, tier.MaximumItemRank - tier.MinimumItemRank + 1))
            .Select(GetBoxGrade)
            .Distinct()
            .ToList();
        var grade = grades[Rand.NextInt(0, grades.Count)];
        return new TemporaryItem { Definition = definition, Level = (byte)(BoxOfKundunFirstLevel - 1 + grade), Durability = 1 };
    }

    /// <summary>
    /// Creates an excellent ring (Ice, Poison, Fire, Earth, Wind or Magic) with excellent options by the weights.
    /// </summary>
    /// <param name="items">The item definitions.</param>
    /// <param name="optionCountWeights">The weights of 1, 2, 3, ... excellent options.</param>
    /// <returns>The ring, or <c>null</c> if no ring is configured.</returns>
    public static Item? CreateExcellentRing(IEnumerable<ItemDefinition> items, IList<int> optionCountWeights) =>
        CreateExcellentJewelry(items, ExcellentRingNumbers, optionCountWeights);

    /// <summary>
    /// Creates an excellent pendant (Lighting, Fire, Ice, Wind, Water or Ability) with excellent options by the weights.
    /// </summary>
    /// <param name="items">The item definitions.</param>
    /// <param name="optionCountWeights">The weights of 1, 2, 3, ... excellent options.</param>
    /// <returns>The pendant, or <c>null</c> if no pendant is configured.</returns>
    public static Item? CreateExcellentPendant(IEnumerable<ItemDefinition> items, IList<int> optionCountWeights) =>
        CreateExcellentJewelry(items, ExcellentPendantNumbers, optionCountWeights);

    private static Item? CreateExcellentJewelry(IEnumerable<ItemDefinition> items, short[] numbers, IList<int> optionCountWeights)
    {
        var rings = items.Where(d => d.Group == 13 && numbers.Contains(d.Number)).ToList();
        if (rings.Count == 0)
        {
            return null;
        }

        var definition = rings[Rand.NextInt(0, rings.Count)];
        var item = new TemporaryItem { Definition = definition };
        var options = definition.PossibleItemOptions
            .SelectMany(o => o.PossibleOptions)
            .Where(o => object.Equals(o.OptionType, ItemOptionTypes.Excellent))
            .OrderBy(_ => Rand.NextInt(0, int.MaxValue))
            .Take(Math.Max(1, KalimaDrops.RollWeighted(optionCountWeights) + 1))
            .ToList();
        foreach (var option in options)
        {
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = option });
        }

        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        return item;
    }

    /// <summary>
    /// Rolls an index by the weights.
    /// </summary>
    /// <param name="weights">The weights.</param>
    /// <returns>The index, or -1 if all weights are 0.</returns>
    public static int RollWeighted(IList<int> weights)
    {
        var total = weights.Where(w => w > 0).Sum();
        if (total <= 0)
        {
            return -1;
        }

        var roll = Rand.NextInt(0, total);
        for (var i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0)
            {
                continue;
            }

            if (roll < weights[i])
            {
                return i;
            }

            roll -= weights[i];
        }

        return -1;
    }

    private static ItemDropByRankPlugIn? GetRankPlugIn(IGameContext gameContext) =>
        gameContext.PlugInManager.GetActivePlugInsOf<IAdditionalItemDropPlugIn>().OfType<ItemDropByRankPlugIn>().FirstOrDefault();

    private static ItemDropByRankConfiguration? GetRankConfiguration(IGameContext gameContext) => GetRankPlugIn(gameContext)?.Configuration;

    private static List<(ItemDefinition Definition, double Chance)> GetRankItems(KalimaInstanceTier tier, IGameContext gameContext, bool weaponsOnly)
    {
        if (GetRankPlugIn(gameContext) is not { } plugIn)
        {
            return [];
        }

        return plugIn.GetRankItems(gameContext.Configuration.Items, gameContext.LoggerFactory.CreateLogger(typeof(KalimaDrops)))
            .Where(i => i.Rank >= tier.MinimumItemRank && i.Rank <= tier.MaximumItemRank)
            .Where(i => !weaponsOnly || (i.Definition.Group <= LastWeaponGroup && !IsAmmunition(i.Definition)))
            .Select(i => (i.Definition, i.Chance))
            .ToList();
    }

    private static int GetBoxGrade(int rank) => rank switch
    {
        <= 2 => 1,
        <= 4 => 2,
        <= 6 => 3,
        7 => 4,
        _ => 5,
    };

    private static bool IsAmmunition(ItemDefinition definition) => definition.Group == 4 && definition.Number is 7 or 15;
}
