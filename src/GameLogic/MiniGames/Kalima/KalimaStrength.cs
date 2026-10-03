// <copyright file="KalimaStrength.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The average strength of the regular monsters of a Kalima instance tier.
/// </summary>
/// <param name="Level">The monster level.</param>
/// <param name="Health">The maximum health.</param>
/// <param name="Damage">The maximum base damage.</param>
/// <param name="Defense">The defense.</param>
public sealed record KalimaStrength(float Level, float Health, float Damage, float Defense);
