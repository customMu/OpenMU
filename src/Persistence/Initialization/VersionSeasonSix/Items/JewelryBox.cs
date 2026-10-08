// <copyright file="JewelryBox.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The Jewelry Box (14/170), a box of this server: the reward of the last kill quests.
/// Dropped on the ground, +1 gives one excellent ring, +2 one excellent pendant.
/// The client has its own model for it (MODEL_JEWELRY_BOX).
/// </summary>
internal static class JewelryBox
{
    /// <summary>
    /// The item group of the Jewelry Box.
    /// </summary>
    internal const byte Group = 14;

    /// <summary>
    /// The item number of the Jewelry Box.
    /// </summary>
    internal const short Number = 170;

    /// <summary>
    /// The item level of the box with a ring.
    /// </summary>
    internal const byte RingLevel = 1;

    /// <summary>
    /// The item level of the box with a pendant.
    /// </summary>
    internal const byte PendantLevel = 2;

    private static readonly short[] Rings = [8, 9, 21, 22, 23, 24]; // Ice, Poison, Fire, Earth, Wind, Magic

    private static readonly short[] Pendants = [12, 13, 25, 26, 27, 28]; // Lighting, Fire, Ice, Wind, Water, Ability

    /// <summary>
    /// Creates the Jewelry Box, or sets its drops again when it exists.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Configure(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Items.FirstOrDefault(i => i.Group == Group && i.Number == Number) is not { } box)
        {
            box = context.CreateNew<ItemDefinition>();
            gameConfiguration.Items.Add(box);
            box.Group = Group;
            box.Number = Number;
            box.Name = "Jewelry Box";
            box.Width = 1;
            box.Height = 1;
            box.Durability = 1;
            box.DropsFromMonsters = false;
            box.SetGuid(box.Group, box.Number);
        }

        box.MaximumItemLevel = PendantLevel;
        foreach (var group in box.DropItems.ToList())
        {
            box.DropItems.Remove(group);
        }

        AddGroup(context, gameConfiguration, box, RingLevel, "Jewelry Box +1 (excellent ring)", Rings);
        AddGroup(context, gameConfiguration, box, PendantLevel, "Jewelry Box +2 (excellent pendant)", Pendants);
    }

    private static void AddGroup(IContext context, GameConfiguration gameConfiguration, ItemDefinition box, byte level, string description, short[] numbers)
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
