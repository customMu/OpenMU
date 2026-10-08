// <copyright file="JewelryBox.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The Jewelry Box (14/200), an item of this server: dropped on the ground, +1 gives one excellent ring,
/// +2 one excellent pendant. The client adds it to its item table (GameLogic/Items/JewelryBox in MuMain).
/// </summary>
internal static class JewelryBox
{
    /// <summary>
    /// The item group of the box.
    /// </summary>
    internal const byte Group = 14;

    /// <summary>
    /// The item number of the box.
    /// </summary>
    internal const short Number = 200;

    /// <summary>
    /// The level of the box which gives a ring.
    /// </summary>
    internal const byte RingLevel = 1;

    /// <summary>
    /// The level of the box which gives a pendant.
    /// </summary>
    internal const byte PendantLevel = 2;

    /// <summary>
    /// Adds the Jewelry Box, or sets the drops of an existing one again.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void Configure(IContext context, GameConfiguration gameConfiguration)
    {
        var box = gameConfiguration.Items.FirstOrDefault(i => i.Group == Group && i.Number == Number) ?? CreateItem(context, gameConfiguration);
        box.MaximumItemLevel = PendantLevel;
        foreach (var group in box.DropItems.Where(g => g.SourceItemLevel is RingLevel or PendantLevel).ToList())
        {
            box.DropItems.Remove(group);
        }

        QuestRewardBoxes.AddGroup(context, gameConfiguration, box, RingLevel, "Jewelry Box +1 (excellent ring)", QuestRewardBoxes.Rings);
        QuestRewardBoxes.AddGroup(context, gameConfiguration, box, PendantLevel, "Jewelry Box +2 (excellent pendant)", QuestRewardBoxes.Pendants);
    }

    private static ItemDefinition CreateItem(IContext context, GameConfiguration gameConfiguration)
    {
        var item = context.CreateNew<ItemDefinition>();
        item.Group = Group;
        item.Number = Number;
        item.Name = "Jewelry Box";
        item.Width = 1;
        item.Height = 1;
        item.Durability = 1;
        item.DropsFromMonsters = false;
        item.SetGuid(item.Group, item.Number);
        gameConfiguration.Items.Add(item);
        return item;
    }
}
