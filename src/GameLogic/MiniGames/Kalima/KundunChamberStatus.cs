// <copyright file="KundunChamberStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The state of a fight in the chamber of Kundun, shown as a banner at the top of the screen.
/// </summary>
/// <param name="Level">The level of the chamber.</param>
/// <param name="Phase">The number of the started phases (0 = none yet).</param>
/// <param name="PhaseCount">The number of phases.</param>
/// <param name="HealthPercent">The health of Kundun in percent.</param>
/// <param name="Illusions">The number of living Illusions of the current phase.</param>
/// <param name="IsShielded">A value indicating whether Kundun is invulnerable, because Illusions are alive.</param>
/// <param name="IsDefeated">A value indicating whether Kundun is defeated.</param>
/// <param name="SecondsLeft">The seconds until the chamber closes.</param>
/// <param name="LastHealPercent">What the last phase healed Kundun, in percent of his maximum health.</param>
/// <param name="LastDefensePercent">What the last phase added to his defense, in percent.</param>
/// <param name="LastDamagePercent">What the last phase added to his damage, in percent.</param>
public sealed record KundunChamberStatus(
    byte Level,
    byte Phase,
    byte PhaseCount,
    byte HealthPercent,
    byte Illusions,
    bool IsShielded,
    bool IsDefeated,
    ushort SecondsLeft,
    byte LastHealPercent,
    byte LastDefensePercent,
    byte LastDamagePercent);
