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
using MUnique.OpenMU.GameLogic.Attributes;
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

    /// <summary>
    /// One speed curve for all: the offsets of attack and magic speed are equal in agility (0.03 and 0.025 per agility
    /// + 35 of a median weapon), every listed skill has a fix time.
    /// </summary>
    [Test]
    public void SpeedCurveOffsetsAreEqualInAgility()
    {
        var config = new SkillCastTimeConfiguration();
        Assert.That((config.AttackSpeedCurveOffset + 35) / 0.03, Is.EqualTo((config.MagicSpeedCurveOffset + 35) / 0.025).Within(1));
        Assert.That(config.SpeedCurves, Is.Not.Empty);
        foreach (var curve in config.SpeedCurves)
        {
            Assert.That(config.FixTimes.Any(f => f.SkillNumber == curve.SkillNumber), $"fix time of skill {curve.SkillNumber}");
        }
    }

    /// <summary>
    /// At the maximum attack speed (510) and magic speed (430) the animation is at least 25 % shorter than the fix time:
    /// every class can use the full harmony option (-25 %).
    /// </summary>
    [Test]
    public void TheSpeedMaximumsAllowTheFullOption()
    {
        var config = new SkillCastTimeConfiguration();
        var attack = (config.AttackSpeedCurveOffset + config.AttackSpeedAtFix) / (config.AttackSpeedCurveOffset + Stats.AttackSpeed.MaximumValue!.Value);
        var magic = (config.MagicSpeedCurveOffset + config.MagicSpeedAtFix) / (config.MagicSpeedCurveOffset + Stats.MagicSpeed.MaximumValue!.Value);
        Assert.That((attack, magic), Is.EqualTo((0.75f, 0.75f)).Using<(float, float)>((x, y) => Math.Abs(x.Item1 - y.Item1) < 0.01f && Math.Abs(x.Item2 - y.Item2) < 0.01f));
        Assert.That(attack, Is.LessThanOrEqualTo(0.75f));
        Assert.That(magic, Is.LessThanOrEqualTo(0.75f));
    }

    /// <summary>
    /// The harmony option lowers the cast time only as far as the speed of the player reaches: at the fix speed the
    /// animation is still the fix time, at the maximum speed the full option counts.
    /// </summary>
    [Test]
    public async ValueTask OptionNeedsSpeedAboveTheFixAsync()
    {
        var plugIn = CreatePlugIn(withSpeedCurves: true);
        var config = plugIn.Configuration!;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player, Weapon(0, 17, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(config.AttackSpeedAtFix), Stats.AttackSpeed);
        var speed = player.Attributes[Stats.AttackSpeed];
        Assert.That(speed, Is.GreaterThanOrEqualTo(config.AttackSpeedAtFix));

        var animation = TwistingFix * (config.AttackSpeedCurveOffset + config.AttackSpeedAtFix) / (config.AttackSpeedCurveOffset + speed);
        var expected = Math.Max(TwistingFix * 0.75, animation * config.SpeedCheckShare);
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(expected).Within(1));

        player.Attributes.AddElement(new ConstantElement(1000), Stats.AttackSpeed);
        Assert.That(player.Attributes[Stats.AttackSpeed], Is.EqualTo(510), "the maximum attack speed");
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(TwistingFix * 0.75).Within(1));
    }

    /// <summary>
    /// Below the fix speed a skill casts no faster than its animation at the speed of the player (times the share);
    /// at the fix speed and above it's the fix time.
    /// </summary>
    [Test]
    public async ValueTask SlowPlayerCastsAtTheAnimationTimeOfItsSpeedAsync()
    {
        var plugIn = CreatePlugIn(withSpeedCurves: true);
        var config = plugIn.Configuration!;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(35), Stats.AttackSpeed);
        var speed = player.Attributes[Stats.AttackSpeed];
        Assert.That(speed, Is.LessThan(config.AttackSpeedAtFix));

        // one curve for all: fix x (offset + fix speed) / (offset + speed)
        var expected = TwistingFix * (config.AttackSpeedCurveOffset + config.AttackSpeedAtFix) / (config.AttackSpeedCurveOffset + speed) * config.SpeedCheckShare;
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(expected).Within(1));
        Assert.That(expected, Is.GreaterThan(TwistingFix * 2), "a slow DK is much slower than the fix");

        player.Attributes.AddElement(new ConstantElement(config.AttackSpeedAtFix), Stats.AttackSpeed);
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(TwistingFix), "at the fix speed");
    }

    /// <summary>
    /// The spells of the wizard follow the magic speed, not the attack speed.
    /// </summary>
    [Test]
    public async ValueTask SpellsFollowTheMagicSpeedAsync()
    {
        const short evilSpirit = 9;
        var plugIn = CreatePlugIn(withSpeedCurves: true);
        var config = plugIn.Configuration!;
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(1000), Stats.AttackSpeed);
        player.Attributes.AddElement(new ConstantElement(50), Stats.MagicSpeed);
        var speed = player.Attributes[Stats.MagicSpeed];

        var expected = 279 * (config.MagicSpeedCurveOffset + config.MagicSpeedAtFix) / (config.MagicSpeedCurveOffset + speed) * config.SpeedCheckShare;
        Assert.That(plugIn.GetCastTime(player, Skill(evilSpirit)).TotalMilliseconds, Is.EqualTo(expected).Within(1));
    }

    /// <summary>
    /// Earthshake on the Dark Horse (the DL command build) follows the attack speed: a slow player casts it no faster
    /// than its animation at its speed, at the fix speed it's the fix time (268 ms, before 08.10.2026 fixed 1 467 ms).
    /// </summary>
    [Test]
    public async ValueTask EarthshakeFollowsTheAttackSpeedAsync()
    {
        const short earthshake = 62;
        var plugIn = CreatePlugIn(withSpeedCurves: true);
        var config = plugIn.Configuration!;
        var fix = config.FixTimes.First(f => f.SkillNumber == earthshake).Milliseconds;
        Assert.That(fix, Is.EqualTo(268));
        Assert.That(config.SpeedCurves.First(c => c.SkillNumber == earthshake).Speed, Is.EqualTo(SkillSpeedStat.AttackSpeed));

        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(100), Stats.AttackSpeed);
        player.Attributes.AddElement(new ConstantElement(1000), Stats.MagicSpeed);
        var speed = player.Attributes[Stats.AttackSpeed];
        var expected = fix * (config.AttackSpeedCurveOffset + config.AttackSpeedAtFix) / (config.AttackSpeedCurveOffset + speed) * config.SpeedCheckShare;
        Assert.That(plugIn.GetCastTime(player, Skill(earthshake)).TotalMilliseconds, Is.EqualTo(expected).Within(1));

        player.Attributes.AddElement(new ConstantElement(config.AttackSpeedAtFix), Stats.AttackSpeed);
        Assert.That(plugIn.GetCastTime(player, Skill(earthshake)).TotalMilliseconds, Is.EqualTo(fix), "at the fix speed");
    }

    /// <summary>
    /// The option lowers the fix time, but not the animation time of a slow player.
    /// </summary>
    [Test]
    public async ValueTask OptionDoesNotHelpBelowTheFixSpeedAsync()
    {
        var plugIn = CreatePlugIn(withSpeedCurves: true);
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await EquipAsync(player, Weapon(0, 17, optionNumber: 11, level: 13, value: 0.25f)).ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(35), Stats.AttackSpeed);
        var animation = plugIn.GetAnimationMilliseconds(player, TwistingSlash, TwistingFix) * plugIn.Configuration!.SpeedCheckShare;
        Assert.That(plugIn.GetCastTime(player, Skill(TwistingSlash)).TotalMilliseconds, Is.EqualTo(animation).Within(1));
    }

    /// <summary>
    /// Normal attacks have a minimum time too, and share it with the skills (one action after the other).
    /// </summary>
    [Test]
    public async ValueTask NormalAttacksAreCheckedAsync()
    {
        var plugIn = CreatePlugIn();
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        Assert.That(plugIn.TryNormalAttack(player), Is.True);
        Assert.That(plugIn.TryNormalAttack(player), Is.False);
        Assert.That(plugIn.TryCast(player, Skill(TwistingSlash)), Is.False, "the skill waits for the normal attack");

        var notChecked = CreatePlugIn();
        notChecked.Configuration!.NormalAttackMilliseconds = 0;
        Assert.That(notChecked.TryNormalAttack(player), Is.True);
        Assert.That(notChecked.TryNormalAttack(player), Is.True);
    }

    private static SkillCastTimePlugIn CreatePlugIn(bool withSpeedCurves = false)
    {
        var configuration = new SkillCastTimeConfiguration { ToleranceMilliseconds = 0 };
        if (!withSpeedCurves)
        {
            // the player of the tests has no attack speed: only the fix times
            configuration.SpeedCurves = new List<SkillSpeedCurve>();
        }

        return new SkillCastTimePlugIn { Configuration = configuration };
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
