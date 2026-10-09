// <copyright file="ItemChatCommandAmountTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;

/// <summary>
/// The amount of the GM command /item (09.10.2026): <c>/item 14 14 0 255</c> gives a stack of 255.
/// </summary>
[TestFixture]
public class ItemChatCommandAmountTest
{
    private static readonly ItemDefinition Jewel = new() { Group = 14, Number = 14, Durability = 255 };

    /// <summary>
    /// The 4th number of a stackable item is the amount; more than a stack makes several stacks.
    /// </summary>
    [TestCase("/item 14 14 0 255", new[] { 255 })]
    [TestCase("/item 14 14", new[] { 1 })]
    [TestCase("/item 14 14 0 30", new[] { 30 })]
    [TestCase("/item group=14 number=14 amount=300", new[] { 255, 45 })]
    public async ValueTask StackableAmountAsync(string command, int[] stacks)
    {
        var arguments = await command.TryParseArgumentsAsync<ItemChatCommandArgs>(null).ConfigureAwait(false);
        Assert.That(arguments, Is.Not.Null);
        var items = ItemChatCommandPlugIn.CreateItems(Jewel, arguments!);
        Assert.That(items.Select(i => (int)i.Durability), Is.EqualTo(stacks));
    }

    /// <summary>
    /// A not stackable item keeps the 4th number as excellent options and makes copies only with amount=.
    /// </summary>
    [Test]
    public async ValueTask CopiesOfOtherItemsAsync()
    {
        var sword = new ItemDefinition { Group = 0, Number = 1, Durability = 20, Width = 1, Height = 3, ItemSlot = new MUnique.OpenMU.Persistence.BasicModel.ItemSlotType() };
        sword.ItemSlot.ItemSlots.Add(0);
        var arguments = await "/item group=0 number=1 amount=3".TryParseArgumentsAsync<ItemChatCommandArgs>(null).ConfigureAwait(false);
        var items = ItemChatCommandPlugIn.CreateItems(sword, arguments!);
        Assert.That(items, Has.Count.EqualTo(3));
        Assert.That(items.All(i => i.Durability == 20), Is.True);
    }
}
