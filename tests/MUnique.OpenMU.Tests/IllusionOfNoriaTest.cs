// <copyright file="IllusionOfNoriaTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

/// <summary>
/// Tests the configuration of the Illusion of Noria (09.10.2026): the Echoes, the price of the reset of the harmony
/// option, the monsters of the daily quest, the day of the daily quest and the map which keeps its respawn delay.
/// </summary>
[TestFixture]
public class IllusionOfNoriaTest
{
    /// <summary>
    /// The Echoes map their items to the skills.
    /// </summary>
    [Test]
    public void EchoSkills()
    {
        var configuration = new IllusionOfNoriaConfiguration();
        Assert.That(configuration.GetEchoSkill(14, 173), Is.EqualTo(41)); // Twisting Slash
        Assert.That(configuration.GetEchoSkill(14, 194), Is.EqualTo(260)); // Killing Blow
        Assert.That(configuration.GetEchoSkill(14, 195), Is.Null); // Jewel of Illusion
        Assert.That(configuration.GetEchoSkill(13, 173), Is.Null);
        Assert.That(configuration.GetEchoItemNumbers(), Has.Count.EqualTo(22));
    }

    /// <summary>
    /// The shop of the warden and the drops (09.10.2026): a Lesser Mirage Stone for 5 shards (2 per daily quest), the
    /// Veil Ward and the Blessing of the Veil for 5 each; the monsters drop Lesser 1 %, Greater 0.01 %, a random Echo
    /// 0.05 %, a Jewel of Illusion 0.01 %, a rank 7-8 weapon 0.001 %; the boss 1-3 Echoes, 0-2 Jewels of Illusion (70 %)
    /// and a weapon (1 %); the mixes: 10 shards to add the option, an Echo from 1 Jewel of Illusion + 20 shards.
    /// </summary>
    [Test]
    public void ShopAndDrops()
    {
        var configuration = new IllusionOfNoriaConfiguration();
        Assert.That(configuration.LesserStonePrice, Is.EqualTo(5));
        Assert.That((configuration.WardPrice, configuration.WardDuration, configuration.WardEffectNumber), Is.EqualTo((5, TimeSpan.FromMinutes(30), (short)189)));
        Assert.That((configuration.BlessingPrice, configuration.BlessingDuration, configuration.BlessingEffectNumber, configuration.BlessingDamagePercent), Is.EqualTo((5, TimeSpan.FromHours(1), (short)185, 50)));
        Assert.That(configuration.DailyShards / configuration.LesserStonePrice, Is.EqualTo(2));
        Assert.That(configuration.MonsterLesserStoneChancePercent, Is.EqualTo(1));
        Assert.That(configuration.MonsterGreaterStoneChancePercent, Is.EqualTo(0.01));
        Assert.That(configuration.MonsterEchoChancePercent, Is.EqualTo(0.05));
        Assert.That(configuration.MonsterWeaponChancePercent, Is.EqualTo(0.001));
        Assert.That(configuration.BossWeaponChancePercent, Is.EqualTo(1));
        Assert.That(configuration.BossDrops.Single(d => d.ItemNumber == -1).MaximumAmount, Is.EqualTo(3));
        var jewels = configuration.BossDrops.Single(d => d.ItemNumber == 195);
        Assert.That((jewels.MinimumAmount, jewels.MaximumAmount, jewels.ChancePercent), Is.EqualTo((0, 2, 70.0)));
        Assert.That((configuration.AddOptionShards, configuration.EchoJewels, configuration.EchoShards), Is.EqualTo((10, 1, 20)));
    }

    /// <summary>
    /// The reset of a rank 8 weapon costs more.
    /// </summary>
    [Test]
    public void ResetPrice()
    {
        var configuration = new IllusionOfNoriaConfiguration();
        var (rank7, price7) = configuration.GetResetPrice(0, 17); // Dark Breaker
        var (rank8, price8) = configuration.GetResetPrice(0, 20); // Knight Blade
        Assert.That(rank7, Is.EqualTo(7));
        Assert.That(price7.Select(p => (p.ItemNumber, p.MinimumAmount)), Is.EqualTo(new[] { ((short)195, 1), ((short)196, 4), ((short)197, 1), ((short)172, 10) }));
        Assert.That(rank8, Is.EqualTo(8));
        Assert.That(price8.Select(p => (p.ItemNumber, p.MinimumAmount)), Is.EqualTo(new[] { ((short)195, 2), ((short)196, 10), ((short)197, 2), ((short)172, 20) }));
    }

    /// <summary>
    /// The monsters of the daily quest and the day which starts at the reset hour.
    /// </summary>
    [Test]
    public void DailyQuest()
    {
        var configuration = new IllusionOfNoriaConfiguration();
        Assert.That(configuration.GetMonsterNumbers(), Is.EqualTo(new short[] { 710, 711, 712, 713, 714, 715, 716, 717 }));
        var before = configuration.GetDayNumber(new DateTime(2026, 10, 9, 5, 59, 0));
        var after = configuration.GetDayNumber(new DateTime(2026, 10, 9, 6, 0, 0));
        Assert.That(after, Is.EqualTo(before + 1));
        Assert.That(configuration.GetTimeUntilNextDay(new DateTime(2026, 10, 9, 5, 0, 0)), Is.EqualTo(TimeSpan.FromHours(1)));
    }

    /// <summary>
    /// The monsters of the illusion keep their 5 seconds, the others get the random time.
    /// </summary>
    [Test]
    public void RandomRespawnExcludesTheIllusion()
    {
        var configuration = new RandomRespawnConfiguration();
        Assert.That(configuration.IsExcluded(82), Is.True);
        Assert.That(configuration.IsExcluded(3), Is.False);
    }

    /// <summary>
    /// An Echo finds the option of its skill on the weapon (for the class family), and no option on other weapons.
    /// </summary>
    [Test]
    public void EchoOption()
    {
        var weapons = new List<WeaponFixSkills>
        {
            new() { Group = 0, Number = 17, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 },
            new() { Group = 5, Number = 11, ClassFamily = 12, Skill1 = 9, Skill2 = 237, Skill3 = 8 },
            new() { Group = 5, Number = 11, ClassFamily = 0, Skill1 = 9, Skill2 = 38, Skill3 = 39 },
        };
        var deathStab = new EchoOfIllusionConsumeHandler(43, 4, weapons);
        Assert.That(deathStab.GetOptionNumber(CreateItem(0, 17)), Is.EqualTo(12));
        Assert.That(deathStab.GetOptionNumber(CreateItem(5, 11)), Is.Null);
        var decay = new EchoOfIllusionConsumeHandler(38, 0, weapons);
        Assert.That(decay.GetOptionNumber(CreateItem(5, 11)), Is.EqualTo(12)); // the DW options of the Staff of Kundun
        var decayMg = new EchoOfIllusionConsumeHandler(38, 12, weapons);
        Assert.That(decayMg.GetOptionNumber(CreateItem(5, 11)), Is.Null); // the MG has Gigantic Storm instead
    }

    /// <summary>
    /// An Echo gives a random one of 4 options (09.10.2026): the fix time of its skill, HP steal, MP steal or Double
    /// damage; their values count only from the weapon in the left hand.
    /// </summary>
    [Test]
    public async ValueTask EchoGivesOneOfFourOptionsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = new MUnique.OpenMU.Persistence.BasicModel.ItemOptionDefinition();
        foreach (var number in new[] { 11, 12, 13, 14, 15, 16 })
        {
            var option = new MUnique.OpenMU.Persistence.BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.SkillFixOption, Number = number };
            var value = new MUnique.OpenMU.Persistence.BasicModel.PowerUpDefinitionValue { ConstantValue = { Value = number / 100f } };
            option.LevelDependentOptions.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemOptionOfLevel
            {
                Level = 1,
                RequiredItemLevel = 10,
                PowerUpDefinition = new MUnique.OpenMU.Persistence.BasicModel.PowerUpDefinition { Boost = value },
            });
            definition.PossibleOptions.Add(option);
        }

        var darkBreaker = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 0, Number = 17, Durability = 60 };
        darkBreaker.PossibleItemOptions.Add(definition);
        var weapons = new List<WeaponFixSkills> { new() { Group = 0, Number = 17, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 } };
        var deathStab = new EchoOfIllusionConsumeHandler(43, 4, weapons);
        var seen = new HashSet<int>();
        for (var i = 0; i < 200; i++)
        {
            var weapon = new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = darkBreaker, Level = 10, Durability = 60 };
            Assert.That(deathStab.ApplyTo(weapon, player.PersistenceContext), Is.True);
            seen.Add(weapon.ItemOptions.Single().ItemOption!.Number);
        }

        Assert.That(seen, Is.EquivalentTo(new[] { 12, 14, 15, 16 }), "Death Stab or one of the 3 extra options");

        var hpWeapon = new MUnique.OpenMU.Persistence.BasicModel.Item { Definition = darkBreaker, Level = 10, Durability = 60 };
        hpWeapon.ItemOptions.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemOptionLink { ItemOption = definition.PossibleOptions.Single(o => o.Number == 14), Level = 1 });
        hpWeapon.ItemSlot = MUnique.OpenMU.DataModel.InventoryConstants.RightHandSlot;
        await player.Inventory!.AddItemAsync(MUnique.OpenMU.DataModel.InventoryConstants.RightHandSlot, hpWeapon).ConfigureAwait(false);
        Assert.That(IllusionWeaponOptions.GetValue(player, IllusionWeaponOptions.HealthSteal), Is.Zero, "right hand");
        await player.Inventory!.RemoveItemAsync(hpWeapon).ConfigureAwait(false);
        hpWeapon.ItemSlot = MUnique.OpenMU.DataModel.InventoryConstants.LeftHandSlot;
        await player.Inventory!.AddItemAsync(MUnique.OpenMU.DataModel.InventoryConstants.LeftHandSlot, hpWeapon).ConfigureAwait(false);
        Assert.That(IllusionWeaponOptions.GetValue(player, IllusionWeaponOptions.HealthSteal), Is.EqualTo(0.14).Within(0.0001), "left hand");
        Assert.That(IllusionWeaponOptions.GetValue(player, IllusionWeaponOptions.DoubleDamage), Is.Zero);
    }

    /// <summary>
    /// A dropped rank 7-8 weapon: +10, one of its skill fix options at level 1-10, luck and skill by chance, no excellent.
    /// </summary>
    [Test]
    public void RandomWeapon()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var fixOption = new MUnique.OpenMU.Persistence.BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.SkillFixOption, Number = 12 };
        foreach (var level in Enumerable.Range(1, 10))
        {
            fixOption.LevelDependentOptions.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemOptionOfLevel { Level = level });
        }

        var luck = new MUnique.OpenMU.Persistence.BasicModel.IncreasableItemOption { OptionType = ItemOptionTypes.Luck };
        var definitions = new MUnique.OpenMU.Persistence.BasicModel.ItemOptionDefinition();
        definitions.PossibleOptions.Add(fixOption);
        definitions.PossibleOptions.Add(luck);
        var darkBreaker = new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 0, Number = 17, Durability = 60 };
        darkBreaker.PossibleItemOptions.Add(definitions);
        gameContext.Configuration.Items.Add(darkBreaker);
        gameContext.Configuration.Items.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 0, Number = 18, Durability = 60 }); // without the options

        var plugIn = new IllusionOfNoriaPlugIn();
        var weapons = new List<WeaponFixSkills>
        {
            new() { Group = 0, Number = 17, ClassFamily = 4, Skill1 = 41, Skill2 = 43, Skill3 = 232 },
            new() { Group = 0, Number = 17, ClassFamily = 12, Skill1 = 56, Skill2 = 55, Skill3 = 41 },
            new() { Group = 0, Number = 18, ClassFamily = 4, Skill1 = 41, Skill2 = 43, Skill3 = 232 },
        };
        var withLuck = 0;
        for (var i = 0; i < 400; i++)
        {
            var weapon = plugIn.CreateRandomWeapon(gameContext, weapons);
            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon!.Definition, Is.SameAs(darkBreaker));
            Assert.That(weapon.Level, Is.EqualTo(10));
            var option = weapon.ItemOptions.Single(o => o.ItemOption == fixOption);
            Assert.That(option.Level, Is.InRange(1, 10));
            withLuck += weapon.ItemOptions.Any(o => o.ItemOption == luck) ? 1 : 0;
        }

        Assert.That(withLuck, Is.InRange(140, 260), "luck 50 %");
    }

    /// <summary>
    /// The boss drops every piece on its own (no stacks).
    /// </summary>
    [Test]
    public void BossDropsSinglePieces()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        foreach (var number in Enumerable.Range(173, 25))
        {
            gameContext.Configuration.Items.Add(new MUnique.OpenMU.Persistence.BasicModel.ItemDefinition { Group = 14, Number = (short)number, Durability = 255 });
        }

        var stones = IllusionOfNoriaPlugIn.CreateRewardItems(gameContext, [new IllusionRewardItem { ItemGroup = 14, ItemNumber = 196, MinimumAmount = 7, MaximumAmount = 7 }], singlePieces: true);
        Assert.That(stones, Has.Count.EqualTo(7));
        Assert.That(stones.All(i => i.Durability == 1), Is.True);

        var stack = IllusionOfNoriaPlugIn.CreateRewardItems(gameContext, [new IllusionRewardItem { ItemGroup = 14, ItemNumber = 196, MinimumAmount = 7, MaximumAmount = 7 }]);
        Assert.That(stack.Single().Durability, Is.EqualTo(7), "the shards of the daily quest still come as a stack");

        var plugIn = new IllusionOfNoriaPlugIn();
        var echoes = plugIn.CreateRandomEchoes(gameContext, 3);
        Assert.That(echoes, Has.Count.EqualTo(3));
        Assert.That(echoes.All(i => i.Definition!.Number is >= 173 and <= 194 && i.Durability == 1), Is.True);
    }

    private static Item CreateItem(byte group, short number)
    {
        return new Item { Definition = new ItemDefinition { Group = group, Number = number } };
    }
}
