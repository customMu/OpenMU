// <copyright file="KalimaInstanceInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// What the gatekeeper of the Kalima instance shows to a player before entering.
/// </summary>
/// <param name="Tiers">The tiers with their reset ranges.</param>
/// <param name="Resets">The resets of the player.</param>
/// <param name="TierLevel">The tier which the player can enter by its resets; 0 if none.</param>
/// <param name="EntriesLeft">The entries which are left today.</param>
/// <param name="EntriesPerDay">The entries per day.</param>
/// <param name="TimeUntilReset">The time until the daily entries are reset.</param>
/// <param name="CanReenter">Whether the player can go back into the running instance of its party for free.</param>
public sealed record KalimaInstanceInfo(
    IReadOnlyList<(byte Level, int MinimumResets, int? MaximumResets)> Tiers,
    int Resets,
    byte TierLevel,
    int EntriesLeft,
    int EntriesPerDay,
    TimeSpan TimeUntilReset,
    bool CanReenter);
