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
        Assert.That(configuration.GetEchoSkill(14, 42), Is.Null); // Jewel of Illusion
        Assert.That(configuration.GetEchoSkill(13, 173), Is.Null);
        Assert.That(configuration.Shop.Count(e => configuration.GetEchoSkill(e.ItemGroup, e.ItemNumber) is not null), Is.EqualTo(22));
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
        Assert.That(price7.Select(p => (p.ItemNumber, p.MinimumAmount)), Is.EqualTo(new[] { ((short)42, 2), ((short)43, 2) }));
        Assert.That(rank8, Is.EqualTo(8));
        Assert.That(price8.Select(p => (p.ItemNumber, p.MinimumAmount)), Is.EqualTo(new[] { ((short)42, 3), ((short)43, 3), ((short)44, 1) }));
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

    private static Item CreateItem(byte group, short number)
    {
        return new Item { Definition = new ItemDefinition { Group = group, Number = number } };
    }
}
