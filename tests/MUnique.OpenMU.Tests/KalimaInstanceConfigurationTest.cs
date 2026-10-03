// <copyright file="KalimaInstanceConfigurationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.KundunSymbols;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// Tests for <see cref="KalimaInstanceConfiguration"/> and <see cref="KundunSymbolsConfiguration"/>.
/// </summary>
[TestFixture]
public class KalimaInstanceConfigurationTest
{
    /// <summary>
    /// The factor grows by (per player - 1) for each additional player, up to a full party.
    /// </summary>
    [TestCase(2.0f, 0, 1.0f)]
    [TestCase(2.0f, 1, 1.0f)]
    [TestCase(2.0f, 2, 2.0f)]
    [TestCase(2.0f, 5, 5.0f)]
    [TestCase(2.0f, 10, 5.0f)]
    [TestCase(1.6f, 2, 1.6f)]
    [TestCase(1.6f, 5, 3.4f)]
    [TestCase(2.1f, 2, 2.1f)]
    [TestCase(2.1f, 5, 5.4f)]
    public void PlayerFactor(float perPlayer, int players, float expected)
    {
        var configuration = new KalimaInstanceConfiguration();
        Assert.That(configuration.GetPlayerFactor(perPlayer, players), Is.EqualTo(expected).Within(0.0001f));
    }

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
    /// The highest tier which was reached by the resets is entered.
    /// </summary>
    [TestCase(0, null)]
    [TestCase(4, null)]
    [TestCase(5, 1)]
    [TestCase(14, 2)]
    [TestCase(22, 4)]
    [TestCase(49, 6)]
    [TestCase(50, 7)]
    [TestCase(200, 7)]
    public void HighestTier(int resets, int? expectedLevel)
    {
        var configuration = new KalimaInstanceConfiguration();
        Assert.That(configuration.GetHighestTier(resets)?.Level, Is.EqualTo(expectedLevel));
    }

    /// <summary>
    /// The most specific price entry counts, items without a price are not for sale.
    /// </summary>
    [Test]
    public void SymbolShopPrices()
    {
        var configuration = new KundunSymbolsConfiguration();
        var box = new ItemDefinition { Group = 14, Number = 11 };
        var bless = new ItemDefinition { Group = 14, Number = 13 };
        var unknown = new ItemDefinition { Group = 0, Number = 0 };

        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 8 }), Is.EqualTo(50));
        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 12 }), Is.EqualTo(400));
        Assert.That(configuration.GetPrice(new Item { Definition = box, Level = 0 }), Is.Null);
        Assert.That(configuration.GetPrice(new Item { Definition = bless }), Is.EqualTo(20));
        Assert.That(configuration.GetPrice(new Item { Definition = unknown }), Is.Null);
    }
}
