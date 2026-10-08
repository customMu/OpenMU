// <copyright file="RankingStatisticsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CastleSiege;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Counts the player versus player statistics of the characters for the rankings of the site (stored stat attributes):
/// all player kills (murders, never reset like the PK count), killed player killers, guild war kills, Castle Siege kills,
/// won and lost duels. Kills between two characters of the same player don't count: the same account, the same computer
/// (the fingerprint of the game client), the same IP address, bots and (configurable) offline leveling characters.
/// </summary>
[PlugIn]
[Display(Name = "Ranking statistics", Description = "Counts player kills (never reset), PK hunting, guild war and Castle Siege kills and duels for the rankings of the site; kills between the same computer, IP address or account don't count.")]
[Guid("8E3B1D57-6A29-4C84-B0F5-2D7C9E4A1B63")]
public class RankingStatisticsPlugIn : IFeaturePlugIn, IAttackableGotKilledPlugIn, ISupportCustomConfiguration<RankingStatisticsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public RankingStatisticsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new RankingStatisticsConfiguration();

    /// <summary>
    /// Counts a murder (a player kill with a PK penalty); called where the PK count of the killer grows.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="killed">The killed player.</param>
    public static void OnPlayerMurdered(Player killer, Player killed)
    {
        if (GetPlugIn(killer) is { } plugIn && plugIn.Counts(killer, killed))
        {
            Increment(killer, Stats.RankingPlayerKills);
        }
    }

    /// <summary>
    /// Counts a finished duel.
    /// </summary>
    /// <param name="winner">The winner.</param>
    /// <param name="loser">The loser.</param>
    public static void OnDuelFinished(Player winner, Player loser)
    {
        if (GetPlugIn(winner) is { } plugIn && plugIn.Counts(winner, loser))
        {
            Increment(winner, Stats.RankingDuelWins);
            Increment(loser, Stats.RankingDuelLosses);
        }
    }

    /// <inheritdoc />
    public ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not Player victim || GetKillingPlayer(killer) is not { } player || !this.Counts(player, victim))
        {
            return ValueTask.CompletedTask;
        }

        if (player.DuelRoom is { State: DuelState.DuelStarted } && victim.DuelRoom == player.DuelRoom)
        {
            // duels count as won or lost duels when they end
            return ValueTask.CompletedTask;
        }

        if (player.GuildWarContext?.Score is { } score && score == victim.GuildWarContext?.Score)
        {
            Increment(player, Stats.RankingGuildWarKills);
        }
        else if (CastleSiegeContextResolver.GetContext(player) is { IsEventRunning: true } siege
                 && player.CurrentMap?.Definition.Number == siege.Configuration.CastleSiegeMapDefinition?.Number)
        {
            Increment(player, Stats.RankingCastleSiegeKills);
        }
        else if (player.CurrentMiniGame?.AllowPlayerKilling is true)
        {
            // Chaos Castle: the mini game ranking counts it
        }
        else if (victim.SelectedCharacter?.State >= HeroState.PlayerKiller1stStage)
        {
            Increment(player, Stats.RankingPlayerKillerKills);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Determines whether a kill (or duel) between two characters counts for the rankings.
    /// </summary>
    /// <param name="killer">The killer or winner.</param>
    /// <param name="killed">The killed player or loser.</param>
    /// <returns><c>true</c>, if it counts.</returns>
    internal bool Counts(Player killer, Player killed)
    {
        var configuration = this.Configuration ??= new RankingStatisticsConfiguration();
        if (killer == killed || killer.Account is not { } killerAccount || killed.Account is not { } killedAccount)
        {
            return false;
        }

        if (killerAccount == killedAccount || killerAccount.IsBot || killedAccount.IsBot)
        {
            return false;
        }

        if (!configuration.CountOfflineCharacters && (killer is OfflinePlayer || killed is OfflinePlayer))
        {
            return false;
        }

        if (configuration.IgnoreSameComputer && killer.HardwareId is { Length: > 0 } killerId && killerId == killed.HardwareId)
        {
            killer.Logger.LogInformation("Ranking statistics: the kill of {killed} by {killer} doesn't count, both play on the same computer.", killed.Name, killer.Name);
            return false;
        }

        if (configuration.IgnoreSameIpAddress
            && (killer as IHasIpAddress)?.IpAddress is { Length: > 0 } killerIp
            && killerIp == (killed as IHasIpAddress)?.IpAddress)
        {
            killer.Logger.LogInformation("Ranking statistics: the kill of {killed} by {killer} doesn't count, both play from {ip}.", killed.Name, killer.Name, killerIp);
            return false;
        }

        return true;
    }

    private static RankingStatisticsPlugIn? GetPlugIn(Player player) => player.GameContext?.FeaturePlugIns.GetPlugIn<RankingStatisticsPlugIn>();

    private static Player? GetKillingPlayer(IAttacker? killer) => killer switch
    {
        Player player => player,
        Monster { SummonedBy: { } owner } => owner,
        _ => null,
    };

    private static void Increment(Player player, AttributeDefinition statistic)
    {
        player.TrySetStoredStatValue(statistic, player.GetStoredStatValue(statistic) + 1);
    }
}

/// <summary>
/// The configuration of the <see cref="RankingStatisticsPlugIn"/>.
/// </summary>
public class RankingStatisticsConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether kills between players with the same IP address don't count.
    /// </summary>
    [Display(Name = "Ignore the same IP address", Description = "Kills and duels between two players with the same IP address don't count (one person with two characters).")]
    public bool IgnoreSameIpAddress { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether kills between players on the same computer don't count.
    /// </summary>
    [Display(Name = "Ignore the same computer", Description = "Kills and duels between two players on the same computer (the fingerprint which the game client sends) don't count.")]
    public bool IgnoreSameComputer { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether kills of and by offline leveling characters count.
    /// </summary>
    [Display(Name = "Count offline characters", Description = "Kills of and by offline leveling characters count; off by default, they can't defend themselves.")]
    public bool CountOfflineCharacters { get; set; }
}
