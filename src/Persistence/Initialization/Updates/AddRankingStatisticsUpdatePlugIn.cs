// <copyright file="AddRankingStatisticsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the stat attributes of the ranking statistics (player kills, PK hunting, guild war and Castle Siege kills, duels),
/// which the plugin "Ranking statistics" counts and the site shows in its rankings.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("C5A2E871-3D9B-4F46-8E1C-6B0D7A4F2E95")]
public class AddRankingStatisticsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Add ranking statistics";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds the character counters of the rankings: player kills (never reset), PK hunting, guild war and Castle Siege kills, won and lost duels.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddRankingStatistics;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 8, 20, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingPlayerKills);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingPlayerKillerKills);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingGuildWarKills);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingCastleSiegeKills);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingDuelWins);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.RankingDuelLosses);
        return ValueTask.CompletedTask;
    }
}
