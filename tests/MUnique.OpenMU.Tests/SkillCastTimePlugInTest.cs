// <copyright file="SkillCastTimePlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests for the <see cref="SkillCastTimePlugIn"/>: the fix time of the skills and the harmony options which lower it.
/// </summary>
[TestFixture]
public class SkillCastTimePlugInTest
{
    private const short TwistingSlash = 41;
    private const short DeathStab = 43;
    private const int TwistingFix = 329;

    /// <summary>
    /// The default configuration has the generated tables: Twisting Slash 329 ms, a rank 7 DK weapon with 3 skills.
    /// </summary>
    [Test]
    public void DefaultConfigurationHasTheGeneratedTables()
    {
        var config = new SkillCastTimeConfiguration();
        Assert.That(config.FixTimes.First(f => f.SkillNumber == TwistingSlash).Milliseconds, Is.EqualTo(TwistingFix));
        var darkBreaker = config.Weapons.First(w => w.Group == 0 && w.Number == 17);
        Assert.That((darkBreaker.Skill1, darkBreaker.Skill2, darkBreaker.Skill3), Is.EqualTo((TwistingSlash, DeathStab, (short)232)));
        Assert.That(config.Weapons.Count(w => w.Group == 5 && w.Number == 11), Is.EqualTo(2), "Staff of Kundun for DW and MG");
    }

    /// <summary>
    /// The plugin point works through the plugin manager (its generated proxy): the server registers the plugin at start.
    /// </summary>
    [Test]
    public async ValueTask PlugInPointWorksThroughThePlugInManagerAsync()
    {
        var manager = new PlugInManager(null, new NullLoggerFactory(), null, null);
        Assert.DoesNotThrow(() => manager.RegisterPlugIn<ISkillCastTimeCheckPlugIn, SkillCastTimePlugIn>());
        manager.RegisterPlugInAtPlugInPoint<ISkillCastTimeCheckPlugIn>(CreatePlugIn());
        var point = manager.GetPlugInPoint<ISkillCastTimeCheckPlugIn>();
        Assert.That(point, Is.Not.Null);

        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var first = new CancelEventArgs();
        point!.CheckCast(player, Skill(TwistingSlash), first);
        var second = new CancelEventArgs();
        point.CheckCast(player, Skill(TwistingSlash), second);
        Assert.That((first.Cancel, second.Cancel), Is.EqualTo((false, true)));
    }

    /// <summary>
    /// A second cast before the fix time is refused, after it it is allowed.
    /// </summary>
    [Test]
    public async ValueTask CastBeforeTheFixTimeIsRefusedAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var skill = Skill(TwistingSlash);

        Assert.That(plugIn.TryCast(player, skill), Is.True);
        Assert.That(plugIn.TryCast(player, skill), Is.False);
        await Task.Delay(TwistingFix + 60).ConfigureAwait(false);
        Assert.That(plugIn.TryCast(player, skill), Is.True);
    }

    /// <summary>
    /// A skill without a fix time is never refused.
    /// </summary>
    [Test]
    public async ValueTask SkillWithoutFixTimeIsNotCheckedAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var skill = Skill(999);
        for (var i = 0; i < 5; i++)
        {
            Assert.That(plugIn.TryCast(player, skill), Is.True);
        }
    }

    /// <summary>
    /// The harmony option 11 of a Dark Breaker at level 13 (-25 %) lowers the fix time of Twisting Slash, not of Death Stab.
    /// </summary>
    [Test]
    public async ValueTask HarmonyOptionLowersTheFixTimeOfItsSkillAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player, Weapon(0, 17, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);

        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(TwistingFix * 0.75).Within(0.5));
        Assert.That(plugIn.GetCastTime(player, Skill(DeathStab)).TotalMilliseconds, Is.EqualTo(176));
    }

    /// <summary>
    /// A weapon which is not in the list gives nothing, and the minimum cast time holds whatever the option.
    /// </summary>
    [Test]
    public async ValueTask OtherWeaponAndMinimumCastTimeAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player, Weapon(0, 5, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(TwistingFix));

        var strong = CreatePlugIn();
        var player2 = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player2, Weapon(0, 17, optionNumber: 12, level: 13, value: 0.8f)).ConfigureAwait(false);
        Assert.That(strong.GetCastTime(player2, Skill(DeathStab)).TotalMilliseconds, Is.EqualTo(120), "minimum cast time");
    }

    /// <summary>
    /// A master skill (e.g. the strengthened Twisting Slash) has the fix time and the option of its base skill.
    /// </summary>
    [Test]
    public async ValueTask MasterSkillUsesItsBaseSkillAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player, Weapon(0, 17, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);
        var master = Skill(330);
        var masterDefinition = new Mock<MasterSkillDefinition>();
        masterDefinition.SetupAllProperties();
        masterDefinition.Object.ReplacedSkill = Skill(TwistingSlash);
        master.MasterDefinition = masterDefinition.Object;

        Assert.That(plugIn.GetCastTime(player, master).TotalMilliseconds, Is.EqualTo(TwistingFix * 0.75).Within(0.5));
    }

    /// <summary>
    /// The option counts x0.6 for the elf.
    /// </summary>
    [Test]
    public async ValueTask ElfGetsAWeakerOptionAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.CharacterClass!.Number = 8; // Fairy Elf
        await EquipAsync(player, Weapon(4, 21, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);
        var multiShot = plugIn.Configuration!.FixTimes.First(f => f.SkillNumber == 235).Milliseconds;
        Assert.That(plugIn.GetCastTime(player, Skill(235)).TotalMilliseconds, Is.EqualTo(multiShot * (1 - (0.25 * 0.6))).Within(0.5));
    }

    private static SkillCastTimePlugIn CreatePlugIn()
    {
        return new SkillCastTimePlugIn { Configuration = new SkillCastTimeConfiguration { ToleranceMilliseconds = 0 } };
    }

    private static Skill Skill(short number)
    {
        var skill = new Mock<Skill>();
        skill.SetupAllProperties();
        skill.Object.Number = number;
        return skill.Object;
    }

    private static async ValueTask EquipAsync(Player player, Item item)
    {
        item.ItemSlot = InventoryConstants.RightHandSlot;
        await player.Inventory!.AddItemAsync(InventoryConstants.RightHandSlot, item).ConfigureAwait(false);
    }

    private static Item Weapon(byte group, short number, int optionNumber, int level, float value)
    {
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Object.Group = group;
        definition.Object.Number = number;
        definition.Object.Durability = 100;
        definition.Setup(d => d.BasePowerUpAttributes).Returns(new List<ItemBasePowerUpDefinition>());
        definition.Setup(d => d.PossibleItemSetGroups).Returns(new List<ItemSetGroup>());
        definition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        definition.Setup(d => d.Requirements).Returns(new List<AttributeRequirement>());

        var option = new Mock<IncreasableItemOption>();
        option.SetupAllProperties();
        option.Object.OptionType = ItemOptionTypes.HarmonyOption;
        option.Object.Number = optionNumber;
        var levelOption = new ItemOptionOfLevel
        {
            Level = level,
            PowerUpDefinition = new PowerUpDefinition { Boost = new TestPowerUpDefinitionValue(new SimpleElement { Value = value }) },
        };
        option.Setup(o => o.LevelDependentOptions).Returns(new List<ItemOptionOfLevel> { levelOption });

        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Object.Definition = definition.Object;
        item.Object.Durability = 100;
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink> { new() { ItemOption = option.Object, Level = level } });
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        return item.Object;
    }

    private sealed class TestPowerUpDefinitionValue : PowerUpDefinitionValue
    {
        public TestPowerUpDefinitionValue(SimpleElement constantValue)
        {
            this.ConstantValue = constantValue;
        }
    }
}
