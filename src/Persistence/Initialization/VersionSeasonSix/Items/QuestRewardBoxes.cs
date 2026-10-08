// <copyright file="QuestRewardBoxes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The boxes of the last kill quests: the Box of Luck (14/11 +0) gives one excellent ring,
/// the Box of Heaven (14/11 +7) one excellent pendant, like the Box of Kundun.
/// </summary>
internal static class QuestRewardBoxes
{
    /// <summary>
    /// The item level of the Box of Luck.
    /// </summary>
    internal const byte BoxOfLuckLevel = 0;

    /// <summary>
    /// The item level of the Box of Heaven.
    /// </summary>
    internal const byte BoxOfHeavenLevel = 7;

    /// <summary>
    /// The numbers (group 13) of the rings which can be excellent: Ice, Poison, Fire, Earth, Wind, Magic.
    /// </summary>
    internal static readonly short[] Rings = [8, 9, 21, 22, 23, 24];

    /// <summary>
    /// The numbers (group 13) of the pendants which can be excellent: Lighting, Fire, Ice, Wind, Water, Ability.
    /// </summary>
    internal static readonly short[] Pendants = [12, 13, 25, 26, 27, 28];

    /// <summary>
    /// Replaces the drops of the Box of Luck and the Box of Heaven by the rings and the pendants.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Configure(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Items.FirstOrDefault(i => i.Group == 14 && i.Number == 11) is not { } box)
        {
            return;
        }

        foreach (var group in box.DropItems.Where(g => g.SourceItemLevel is BoxOfLuckLevel or BoxOfHeavenLevel).ToList())
        {
            box.DropItems.Remove(group);
        }

        AddGroup(context, gameConfiguration, box, BoxOfLuckLevel, "Box of Luck (excellent ring)", Rings);
        AddGroup(context, gameConfiguration, box, BoxOfHeavenLevel, "Box of Heaven (excellent pendant)", Pendants);
    }

    /// <summary>
    /// Adds a drop group of one excellent item out of the given jewelry (group 13) for one level of the box.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="box">The box.</param>
    /// <param name="level">The item level of the box.</param>
    /// <param name="description">The description of the drop group.</param>
    /// <param name="numbers">The numbers of the jewelry in group 13.</param>
    internal static void AddGroup(IContext context, GameConfiguration gameConfiguration, ItemDefinition box, byte level, string description, short[] numbers)
    {
        var group = context.CreateNew<ItemDropItemGroup>();
        group.SourceItemLevel = level;
        group.ItemType = SpecialItemType.Excellent;
        group.Chance = 1.0;
        group.Description = description;
        group.DropEffect = ItemDropEffect.FanfareSound;
        foreach (var number in numbers)
        {
            if (gameConfiguration.Items.FirstOrDefault(i => i.Group == 13 && i.Number == number) is { } item)
            {
                group.PossibleItems.Add(item);
            }
        }

        box.DropItems.Add(group);
    }
}
