// <copyright file="InvasionConfigurationDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// Provides ready-to-use default configurations for the built-in invasion types.
/// </summary>
internal static class InvasionConfigurationDefaults
{
    /// <summary>
    /// Gets the default configuration for the Golden Invasion event.
    /// </summary>
    public static PeriodicInvasionConfiguration Golden => new()
    {
        TaskDuration = TimeSpan.FromMinutes(30),
        PreStartMessageDelay = TimeSpan.FromSeconds(3),
        StartMessage = "[{mapName}] Golden invasion!",
        EndMessage = "[{mapName}] Golden invasion has ended.",
        Timetable = PeriodicTaskConfiguration.GenerateTimeSequence(TimeSpan.FromHours(4)).ToList(),
        OneRandomMap = true,
        Mobs =
        [
            new(InvasionMonsters.GoldenBudgeDragon, 3, [InvasionMaps.Lorencia], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenGoblin, 3, [InvasionMaps.Noria], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenSoldier, 3, [InvasionMaps.Devias], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenTitan, 3, [InvasionMaps.Devias], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenVepar, 3, [InvasionMaps.Atlans], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenLizardKing, 3, [InvasionMaps.Atlans], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenWheel, 3, [InvasionMaps.Tarkan], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenTantallos, 3, [InvasionMaps.Tarkan], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
            new(InvasionMonsters.GoldenDragon, 3, [InvasionMaps.Lorencia, InvasionMaps.Noria, InvasionMaps.Devias], SpawnMapStrategy.RandomMap) { MinimumCount = 1 },
        ],
    };

    /// <summary>
    /// Gets the default configuration for the Red Dragon Invasion event.
    /// </summary>
    public static PeriodicInvasionConfiguration RedDragon => new()
    {
        TaskDuration = TimeSpan.FromMinutes(30),
        PreStartMessageDelay = TimeSpan.FromSeconds(3),
        StartMessage = "[{mapName}] Red Dragon invasion!",
        EndMessage = "[{mapName}] Red Dragon invasion has ended.",
        Timetable = PeriodicTaskConfiguration.GenerateTimeSequence(TimeSpan.FromHours(6), new TimeOnly(2, 0)).ToList(),
        Mobs =
        [
            new(InvasionMonsters.RedDragon, 5, [InvasionMaps.Lorencia, InvasionMaps.Noria, InvasionMaps.Devias], SpawnMapStrategy.RandomMap),
        ],
    };

    /// <summary>
    /// Gets the default configuration for the White Wizard Invasion event.
    /// </summary>
    public static PeriodicInvasionConfiguration WhiteWizard => new()
    {
        TaskDuration = TimeSpan.FromMinutes(30),
        PreStartMessageDelay = TimeSpan.FromSeconds(3),
        StartMessage = "[{mapName}] White Wizard corps invasion!",
        EndMessage = "[{mapName}] White Wizard corps invasion has ended.",
        ForceSingleMap = true,
        Timetable = PeriodicTaskConfiguration.GenerateTimeSequence(TimeSpan.FromHours(2), new TimeOnly(12, 0), new TimeOnly(23, 0)).ToList(),
        Mobs =
        [
            new(InvasionMonsters.WhiteWizard, 1, [InvasionMaps.Lorencia, InvasionMaps.Noria, InvasionMaps.Devias], SpawnMapStrategy.RandomMap, announceDeath: true),
            new(InvasionMonsters.DestructiveOgreSoldier, 15, [], SpawnMapStrategy.RandomMap),
            new(InvasionMonsters.DestructiveOgreArcher, 10, [], SpawnMapStrategy.RandomMap),
        ],
    };
}