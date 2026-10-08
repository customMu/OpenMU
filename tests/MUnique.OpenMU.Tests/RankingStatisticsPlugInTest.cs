// <copyright file="RankingStatisticsPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the anti farming rules of the <see cref="RankingStatisticsPlugIn"/>: kills between two characters of the same
/// person (same account, same IP address) and of bots don't count.
/// </summary>
[TestFixture]
public class RankingStatisticsPlugInTest
{
    private GameContext _gameContext = null!;

    /// <summary>
    /// Sets up a game context.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        var dummyPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        this._gameContext = (GameContext)dummyPlayer.GameContext;
    }

    /// <summary>
    /// Two players with own accounts and IP addresses count.
    /// </summary>
    [Test]
    public void DifferentPlayersCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        Assert.That(plugIn.Counts(this.CreatePlayer("a", "10.0.0.1"), this.CreatePlayer("b", "10.0.0.2")), Is.True);
    }

    /// <summary>
    /// Two players with the same IP address don't count.
    /// </summary>
    [Test]
    public void SameIpAddressDoesNotCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        Assert.That(plugIn.Counts(this.CreatePlayer("a", "10.0.0.1"), this.CreatePlayer("b", "10.0.0.1")), Is.False);
    }

    /// <summary>
    /// The same IP address counts when the check is switched off.
    /// </summary>
    [Test]
    public void SameIpAddressCountsWhenSwitchedOff()
    {
        var plugIn = new RankingStatisticsPlugIn { Configuration = new RankingStatisticsConfiguration { IgnoreSameIpAddress = false } };
        Assert.That(plugIn.Counts(this.CreatePlayer("a", "10.0.0.1"), this.CreatePlayer("b", "10.0.0.1")), Is.True);
    }

    /// <summary>
    /// Two players on the same computer (the same fingerprint, other IP addresses - e.g. a VPN) don't count.
    /// </summary>
    [Test]
    public void SameComputerDoesNotCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        var killer = this.CreatePlayer("a", "10.0.0.1");
        var killed = this.CreatePlayer("b", "10.0.0.2");
        killer.HardwareId = killed.HardwareId = "0123456789ABCDEF0123456789ABCDEF";
        Assert.That(plugIn.Counts(killer, killed), Is.False);
    }

    /// <summary>
    /// Other computers count, also when only one of them sent a fingerprint.
    /// </summary>
    [Test]
    public void OtherComputersCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        var killer = this.CreatePlayer("a", "10.0.0.1");
        var killed = this.CreatePlayer("b", "10.0.0.2");
        killer.HardwareId = "0123456789ABCDEF0123456789ABCDEF";
        Assert.That(plugIn.Counts(killer, killed), Is.True);
        killed.HardwareId = "FEDCBA9876543210FEDCBA9876543210";
        Assert.That(plugIn.Counts(killer, killed), Is.True);
    }

    /// <summary>
    /// Two characters of the same account don't count.
    /// </summary>
    [Test]
    public void SameAccountDoesNotCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        var killer = this.CreatePlayer("a", "10.0.0.1");
        var killed = this.CreatePlayer("b", "10.0.0.2");
        killed.Account = killer.Account;
        Assert.That(plugIn.Counts(killer, killed), Is.False);
    }

    /// <summary>
    /// Bots don't count.
    /// </summary>
    [Test]
    public void BotDoesNotCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        var bot = this.CreatePlayer("bot", "10.0.0.2");
        bot.Account!.IsBot = true;
        Assert.That(plugIn.Counts(this.CreatePlayer("a", "10.0.0.1"), bot), Is.False);
    }

    /// <summary>
    /// A player without an account (not logged in) doesn't count.
    /// </summary>
    [Test]
    public void WithoutAccountDoesNotCount()
    {
        var plugIn = new RankingStatisticsPlugIn();
        var killed = this.CreatePlayer("b", "10.0.0.2");
        killed.Account = null;
        Assert.That(plugIn.Counts(this.CreatePlayer("a", "10.0.0.1"), killed), Is.False);
    }

    private TestPlayer CreatePlayer(string login, string ip)
    {
        var player = new TestPlayer(this._gameContext, ip);
        player.Account = new Account { LoginName = login };
        return player;
    }

    private sealed class TestPlayer : Player, IHasIpAddress
    {
        public TestPlayer(IGameContext gameContext, string ipAddress)
            : base(gameContext)
        {
            this.IpAddress = ipAddress;
        }

        public string? IpAddress { get; }

        protected override ICustomPlugInContainer<GameLogic.Views.IViewPlugIn> CreateViewPlugInContainer() => new MockViewPlugInContainer();
    }
}
