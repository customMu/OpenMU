// <copyright file="EnchantPriceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.GameLogic.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

/// <summary>
/// The price of enchanting by rank (09.10.2026, wiki §7).
/// </summary>
[TestFixture]
public class EnchantPriceTest
{
    /// <summary>
    /// Jewels per use and levels per use of Bless and Soul, and the Chaos Machine multiplier.
    /// </summary>
    [TestCase(8, 2, false, 1, 3, 1)] // Pad Armor, rank 1: 1 Bless = +3
    [TestCase(8, 2, true, 1, 1, 1)] // Pad Armor: 1 Soul = +1
    [TestCase(8, 4, false, 1, 2, 1)] // Bone Armor, rank 2: 1 Bless = +2
    [TestCase(8, 7, false, 1, 1, 1)] // Sphinx Armor, rank 3
    [TestCase(8, 9, true, 2, 1, 2)] // Plate Armor, rank 4
    [TestCase(8, 1, false, 2, 1, 2)] // Dragon Armor: rank 5, the first set of the rank -> price of rank 4
    [TestCase(8, 26, false, 3, 1, 2)] // Adamantine Armor, rank 5
    [TestCase(8, 17, true, 4, 1, 3)] // Dark Phoenix Armor: rank 7, first set -> rank 6
    [TestCase(0, 17, false, 5, 1, 3)] // Dark Breaker, rank 7
    [TestCase(8, 29, true, 6, 1, 3)] // Dragon Knight Armor, rank 8
    [TestCase(6, 0, false, 1, 1, 1)] // Small Shield: no rank
    public void Steps(byte group, short number, bool soul, int jewels, int levels, int multiplier)
    {
        var definition = new ItemDefinition { Group = group, Number = number };
        Assert.That(EnchantPriceRanks.GetStep(definition, soul), Is.EqualTo((jewels, levels)));
        Assert.That(EnchantPriceRanks.GetChaosMachineMultiplier(definition), Is.EqualTo(multiplier));
    }

    /// <summary>
    /// The Chaos Machine mixes +10..+15 multiply only the Bless and Soul, by the rank of the item in the mix.
    /// </summary>
    [Test]
    public void ChaosMachineMultipliesBlessAndSoul()
    {
        var bless = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 14, Number = 13, Durability = 255 };
        var chaos = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 12, Number = 15, Durability = 255 };
        var armor = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 8, Number = 29, Durability = 50, ItemSlot = new MUnique.OpenMU.Persistence.BasicModel.ItemSlotType() }; // Dragon Knight, rank 8
        armor.ItemSlot.ItemSlots.Add(3);
        IList<Item> storage = [new Item { Definition = armor }, new Item { Definition = bless, Durability = 6 }, new Item { Definition = chaos, Durability = 1 }];
        var handler = new TestCrafting();
        Assert.That(handler.Multiplier(new MUnique.OpenMU.Persistence.BasicModel.ItemCraftingRequiredItem { PossibleItems = { bless } }, storage), Is.EqualTo(3));
        Assert.That(handler.Multiplier(new MUnique.OpenMU.Persistence.BasicModel.ItemCraftingRequiredItem { PossibleItems = { chaos } }, storage), Is.EqualTo(1));
    }

    /// <summary>
    /// The same with the real crafting +10 of the configuration (season 6 data).
    /// </summary>
    [Test]
    public async ValueTask ChaosMachineWithRealDataAsync()
    {
        var contextProvider = new MUnique.OpenMU.Persistence.InMemory.InMemoryPersistenceContextProvider();
        await new MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.DataInitialization(contextProvider, new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory()).CreateInitialDataAsync(3, true).ConfigureAwait(false);
        var config = (await contextProvider.CreateNewConfigurationContext().GetAsync<MUnique.OpenMU.DataModel.Configuration.GameConfiguration>().ConfigureAwait(false)).First();
        var crafting = config.Monsters.First(m => m.Number == 238).ItemCraftings.First(c => c.Number == 3);
        var bless = crafting.SimpleCraftingSettings!.RequiredItems.First(r => r.PossibleItems.Any(d => d.Group == 14 && d.Number == 13));
        var definition = config.Items.First(d => d.Group == 8 && d.Number == 26); // Adamantine Armor, rank 5
        IList<Item> storage = [new Item { Definition = definition, Level = 9 }, new Item { Definition = bless.PossibleItems.First(), Durability = 2 }];
        Assert.That(new TestCrafting().Multiplier(bless, storage), Is.EqualTo(2));
    }

    /// <summary>
    /// The whole check of the mix +10 (log 09.10.2026: a rank 5 armor +9 with 2 Bless, 2 Soul and a Chaos was
    /// "TooManyItems": the item was taken from the mix before the jewels, the multiplier fell back to 1).
    /// </summary>
    [Test]
    public async ValueTask ChaosMachineMixWithDoubledJewelsAsync()
    {
        var contextProvider = new MUnique.OpenMU.Persistence.InMemory.InMemoryPersistenceContextProvider();
        await new MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.DataInitialization(contextProvider, new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory()).CreateInitialDataAsync(3, true).ConfigureAwait(false);
        var config = (await contextProvider.CreateNewConfigurationContext().GetAsync<MUnique.OpenMU.DataModel.Configuration.GameConfiguration>().ConfigureAwait(false)).First();
        var crafting = config.Monsters.First(m => m.Number == 238).ItemCraftings.First(c => c.Number == 3);
        var handler = new ItemLevelUpgradeCrafting(crafting.SimpleCraftingSettings!);
        MUnique.OpenMU.DataModel.Configuration.Items.ItemDefinition Definition(byte group, short number) => config.Items.First(d => d.Group == group && d.Number == number);

        async ValueTask<MUnique.OpenMU.GameLogic.Views.NPC.CraftingResult?> CheckAsync(int jewels)
        {
            var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
            var storage = player.TemporaryStorage!;
            await storage.AddItemAsync(0, new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = Definition(8, 26), Level = 9, Durability = 50 }).ConfigureAwait(false); // Adamantine Armor, rank 5: x2
            await storage.AddItemAsync(2, new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = Definition(12, 15), Durability = 1 }).ConfigureAwait(false);

            // single jewels: the jewels of the seed data are not stackable (in the game they are stacks of the same count)
            for (byte i = 0; i < jewels; i++)
            {
                await storage.AddItemAsync((byte)(3 + i), new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = Definition(14, 13), Durability = 1 }).ConfigureAwait(false);
                await storage.AddItemAsync((byte)(24 + i), new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = Definition(14, 14), Durability = 1 }).ConfigureAwait(false);
            }

            return handler.TryGetRequiredItems(player, out _, out _);
        }

        Assert.That(await CheckAsync(2).ConfigureAwait(false), Is.Null);
        Assert.That(await CheckAsync(1).ConfigureAwait(false), Is.EqualTo(MUnique.OpenMU.GameLogic.Views.NPC.CraftingResult.LackingMixItems));
        Assert.That(await CheckAsync(3).ConfigureAwait(false), Is.EqualTo(MUnique.OpenMU.GameLogic.Views.NPC.CraftingResult.TooManyItems));
    }

    private sealed class TestCrafting : ItemLevelUpgradeCrafting
    {
        public TestCrafting()
            : base(new MUnique.OpenMU.Persistence.BasicModel.SimpleCraftingSettings())
        {
        }

        public int Multiplier(ItemCraftingRequiredItem requiredItem, IList<Item> storage) => this.GetAmountMultiplier(requiredItem, storage.Where(i => i.Definition is { } d && requiredItem.PossibleItems.Any(p => p.Group == d.Group && p.Number == d.Number)).ToList(), storage);
    }
}
