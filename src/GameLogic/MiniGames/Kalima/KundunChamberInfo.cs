// <copyright file="KundunChamberInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The information about the chamber of Kundun for a player, for the entry dialog of the keeper.
/// </summary>
/// <param name="Tiers">The levels with their reset ranges (the tiers of the Kalima instance).</param>
/// <param name="Resets">The resets of the player.</param>
/// <param name="TierLevel">The level which the player can enter by its resets; 0 if none.</param>
/// <param name="EntriesLeft">The entries which are left this week.</param>
/// <param name="EntriesPerWeek">The entries per week.</param>
/// <param name="TimeUntilReset">The time until the weekly entries are reset.</param>
/// <param name="CanReenter">Whether the player can go back into the running chamber of its party for free.</param>
/// <param name="HasLostMap">Whether the player has the Lost Map of its level, the entry fee.</param>
public sealed record KundunChamberInfo(
    IReadOnlyList<(byte Level, int MinimumResets, int? MaximumResets)> Tiers,
    int Resets,
    byte TierLevel,
    int EntriesLeft,
    int EntriesPerWeek,
    TimeSpan TimeUntilReset,
    bool CanReenter,
    bool HasLostMap);
