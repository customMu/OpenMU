// <copyright file="SkillFixOptionTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

/// <summary>
/// Tests the skill fix option of the Illusion of Noria (09.10.2026): a separate option type, sent to the custom client
/// with the socket count 15 instead of the sockets; the stones which add and raise it.
/// </summary>
[TestFixture]
public class SkillFixOptionTest
{
    private GameConfiguration _gameConfiguration = null!;
    private IPersistenceContextProvider _contextProvider = null!;

    /// <summary>
    /// Sets up the configuration with the skill fix options on the Dark Breaker.
    /// </summary>
    [OneTimeSetUp]
    public async ValueTask SetupAsync()
    {
        this._contextProvider = new InMemoryPersistenceContextProvider();
        await new DataInitialization(this._contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(3, true).ConfigureAwait(false);
        this._gameConfiguration = (await this._contextProvider.CreateNewConfigurationContext().GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        using var context = this._contextProvider.CreateNewContext(this._gameConfiguration);
        var definition = context.CreateNew<ItemOptionDefinition>();
        definition.Name = "Skill Fix Options";
        for (short number = 11; number <= 13; number++)
        {
            var option = context.CreateNew<IncreasableItemOption>();
            option.OptionType = ItemOptionTypes.SkillFixOption;
            option.Number = number;
            definition.PossibleOptions.Add(option);
        }

        this.DarkBreaker.PossibleItemOptions.Add(definition);
    }

    private ItemDefinition DarkBreaker => this._gameConfiguration.Items.First(i => i.Group == 0 && i.Number == 17);

    /// <summary>
    /// The option goes to the client in the place of the sockets (count 15) and comes back with its number and level.
    /// </summary>
    [Test]
    public void SerializedInsteadOfSockets()
    {
        using var context = this._contextProvider.CreateNewContext(this._gameConfiguration);
        var item = this.CreateWeapon(context, 12, 7);
        var serializer = new ItemSerializerExtended();
        var array = new byte[serializer.NeededSpace];
        var length = serializer.SerializeItem(array, item);

        Assert.That(array[4] & 0x80, Is.EqualTo(0x80), "the socket flag");
        Assert.That(array[length - 2] & 0xF, Is.EqualTo(0xF), "the marker of the skill fix option");
        Assert.That(array[length - 1], Is.EqualTo((2 << 4) | 7), "option 12 (2), level 7");

        var deserialized = serializer.DeserializeItem(array, this._gameConfiguration, context);
        var link = deserialized.ItemOptions.Single(o => o.ItemOption?.OptionType == ItemOptionTypes.SkillFixOption);
        Assert.That(link.ItemOption!.Number, Is.EqualTo(12));
        Assert.That(link.Level, Is.EqualTo(7));
        Assert.That(deserialized.SocketCount, Is.Zero);
    }

    /// <summary>
    /// The skill fix option is not a harmony option: the jewel of harmony doesn't see it and the stones of the option
    /// don't see harmony options.
    /// </summary>
    [Test]
    public void SeparateFromHarmony()
    {
        Assert.That(ItemOptionTypes.SkillFixOption, Is.Not.EqualTo(ItemOptionTypes.HarmonyOption));
        Assert.That(new HarmonyJewelConsumeHandlerPlugIn().Configuration.OptionType, Is.EqualTo(ItemOptionTypes.HarmonyOption));
        Assert.That(new JewelOfIllusionConsumeHandlerPlugIn().Configuration.OptionType, Is.EqualTo(ItemOptionTypes.SkillFixOption));
        Assert.That(new LesserMirageStoneConsumeHandlerPlugIn().Configuration.OptionType, Is.EqualTo(ItemOptionTypes.SkillFixOption));
        Assert.That(new GreaterMirageStoneConsumeHandlerPlugIn().Configuration.OptionType, Is.EqualTo(ItemOptionTypes.SkillFixOption));
    }

    /// <summary>
    /// The harmony jewels are back as they were: 60 %, the refine stones 80 / 20 % with a fall back on a fail.
    /// </summary>
    [Test]
    public void HarmonyAsBefore()
    {
        Assert.That(new HarmonyJewelConsumeHandlerPlugIn().Configuration.SuccessChance, Is.EqualTo(0.6));
        Assert.That(new HigherRefineStoneConsumeHandlerPlugIn().Configuration.FailResult, Is.EqualTo(ItemUpgradeConsumeHandlerPlugIn.ItemFailResult.SetOptionToBaseLevel));
        Assert.That(new LowerRefineStoneConsumeHandlerPlugIn().Configuration.SuccessChance, Is.EqualTo(0.2));
    }

    /// <summary>
    /// The stones: the jewel adds (100 %), the Lesser stone falls back on a fail, the Greater one keeps the level; the
    /// chance by the level they go to.
    /// </summary>
    [TestCase(2, 1.0)]
    [TestCase(4, 0.7)]
    [TestCase(10, 0.1)]
    public void Stones(int targetLevel, double chance)
    {
        Assert.That(new JewelOfIllusionConsumeHandlerPlugIn().Configuration.SuccessChance, Is.EqualTo(1.0));
        Assert.That(new JewelOfIllusionConsumeHandlerPlugIn().Key, Is.EqualTo(new GameLogic.ItemIdentifier(195, 14)));
        Assert.That(new LesserMirageStoneConsumeHandlerPlugIn().Configuration.FailResult, Is.EqualTo(ItemUpgradeConsumeHandlerPlugIn.ItemFailResult.SetOptionToBaseLevel));
        Assert.That(new GreaterMirageStoneConsumeHandlerPlugIn().Configuration.FailResult, Is.EqualTo(ItemUpgradeConsumeHandlerPlugIn.ItemFailResult.None));
        Assert.That(new TestLesser().ChanceOf(targetLevel), Is.EqualTo(chance).Within(1e-9));
        Assert.That(new TestGreater().ChanceOf(targetLevel), Is.EqualTo(chance).Within(1e-9));
    }

    private Item CreateWeapon(IContext context, short optionNumber, int level)
    {
        var item = context.CreateNew<Item>();
        item.Definition = this.DarkBreaker;
        item.Level = 11;
        item.Durability = 100;
        var link = context.CreateNew<ItemOptionLink>();
        link.ItemOption = this.DarkBreaker.PossibleItemOptions.SelectMany(o => o.PossibleOptions)
            .First(o => o.OptionType == ItemOptionTypes.SkillFixOption && o.Number == optionNumber);
        link.Level = level;
        item.ItemOptions.Add(link);
        return item;
    }

    private sealed class TestLesser : LesserMirageStoneConsumeHandlerPlugIn
    {
        public double ChanceOf(int targetLevel) => this.GetUpgradeSuccessChance(targetLevel);
    }

    private sealed class TestGreater : GreaterMirageStoneConsumeHandlerPlugIn
    {
        public double ChanceOf(int targetLevel) => this.GetUpgradeSuccessChance(targetLevel);
    }
}
