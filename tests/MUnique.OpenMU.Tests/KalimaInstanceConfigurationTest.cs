// <copyright file="KalimaInstanceConfigurationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Tests for <see cref="KalimaInstanceConfiguration"/> and <see cref="KundunEssenceConfiguration"/>.
/// </summary>
[TestFixture]
public class KalimaInstanceConfigurationTest
{
    /// <summary>
    /// A new day starts at the daily reset hour, the time before still belongs to the previous day.
    /// </summary>
    [Test]
    public void DayStartsAtResetHour()
    {
        var configuration = new KalimaInstanceConfiguration { DailyResetHour = 6, TimeZoneId = "UTC" };
        var beforeReset = configuration.GetDayNumber(new DateTime(2026, 10, 3, 5, 59, 0, DateTimeKind.Utc));
        var atReset = configuration.GetDayNumber(new DateTime(2026, 10, 3, 6, 0, 0, DateTimeKind.Utc));
        var evening = configuration.GetDayNumber(new DateTime(2026, 10, 3, 23, 0, 0, DateTimeKind.Utc));
        var nextMorning = configuration.GetDayNumber(new DateTime(2026, 10, 4, 5, 0, 0, DateTimeKind.Utc));

        Assert.That(atReset, Is.EqualTo(beforeReset + 1));
        Assert.That(evening, Is.EqualTo(atReset));
        Assert.That(nextMorning, Is.EqualTo(atReset));
    }

    /// <summary>
    /// The remaining time until the next reset.
    /// </summary>
    [Test]
    public void TimeUntilNextReset()
    {
        var configuration = new KalimaInstanceConfiguration { DailyResetHour = 6, TimeZoneId = "UTC" };
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 3, 5, 30, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 3, 6, 0, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromHours(24)));
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 3, 20, 0, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromHours(10)));
    }

    /// <summary>
    /// The tiers have strict reset ranges: 5-9, 10-14, 15-21, 22-29, 30-37, 38-44 and 45+.
    /// </summary>
    [TestCase(0, null, null)]
    [TestCase(4, null, null)]
    [TestCase(5, 1, "5-9")]
    [TestCase(9, 1, "5-9")]
    [TestCase(10, 2, "10-14")]
    [TestCase(21, 3, "15-21")]
    [TestCase(22, 4, "22-29")]
    [TestCase(37, 5, "30-37")]
    [TestCase(44, 6, "38-44")]
    [TestCase(45, 7, "45+")]
    [TestCase(200, 7, "45+")]
    public void TierByResets(int resets, int? expectedLevel, string? expectedRange)
    {
        var configuration = new KalimaInstanceConfiguration();
        var tier = configuration.GetTierByResets(resets);
        Assert.That(tier?.Level, Is.EqualTo(expectedLevel));
        Assert.That(tier is null ? null : configuration.GetResetRangeText(tier), Is.EqualTo(expectedRange));
    }

    /// <summary>
    /// The most specific price entry counts, items without a price are not for sale.
    /// </summary>
    [Test]
    public void EssenceShopPrices()
    {
        var configuration = new KundunEssenceConfiguration();
        var box = new ItemDefinition { Group = 14, Number = 11 };
        var bless = new ItemDefinition { Group = 14, Number = 13 };
        var unknown = new ItemDefinition { Group = 0, Number = 0 };

        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 8 }), Is.EqualTo(50));
        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 12 }), Is.EqualTo(400));
        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 0 }), Is.Null);
        Assert.That(configuration.GetPrice(new Item { Definition = bless }), Is.EqualTo(20));
        Assert.That(configuration.GetPrice(new Item { Definition = unknown }), Is.Null);
    }

    /// <summary>
    /// The lost map +N costs 25 x N essence.
    /// </summary>
    /// <param name="level">The level of the lost map.</param>
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(7)]
    public void LostMapPrices(int level)
    {
        var configuration = new KundunEssenceConfiguration();
        var lostMap = new ItemDefinition { Group = 14, Number = 28 };
        Assert.That(configuration.GetPrice(new Item { Definition = lostMap, Level = (byte)level }), Is.EqualTo(25 * level));
    }

    /// <summary>
    /// A lost map which is bought in the essence shop is bound to the character, if it's configured so.
    /// </summary>
    [Test]
    public void BoughtLostMapIsBound()
    {
        Assert.That(new KundunEssenceConfiguration().BindBoughtLostMaps, Is.False, "by default, bought lost maps are tradable");
        var plugIn = new KundunEssencePlugIn { Configuration = new KundunEssenceConfiguration { BindBoughtLostMaps = true } };
        var lostMap = new Item { Definition = new ItemDefinition { Group = 14, Number = 28 }, Level = 3 };
        var jewel = new Item { Definition = new ItemDefinition { Group = 14, Number = 13 } };
        Assert.That(lostMap.IsBoundToCharacter(), Is.False);

        plugIn.PrepareBoughtItem(lostMap);
        plugIn.PrepareBoughtItem(jewel);

        Assert.That(lostMap.IsBoundToCharacter(), Is.True);
        Assert.That(jewel.IsBoundToCharacter(), Is.False);
    }
}
