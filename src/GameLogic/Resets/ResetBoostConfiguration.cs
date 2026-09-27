// <copyright file="ResetBoostConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for the <see cref="ResetBoostPlugIn"/>.
/// </summary>
public class ResetBoostConfiguration
{
    /// <summary>
    /// Gets or sets the percentage of maximum health and mana which each reset adds.
    /// </summary>
    [Display(Name = "HP/MP % per reset", Description = "Each reset multiplies maximum health and mana by (1 + resets * this / 100). Keep in sync with the client (GameLogic/ResetBoost.h).")]
    public float HealthAndManaPercentPerReset { get; set; } = 0.4f;

    /// <summary>
    /// Gets or sets the percentage of damage and defense which each reset adds.
    /// </summary>
    [Display(Name = "Damage/defense % per reset", Description = "Each reset multiplies the dealt damage and the defense by (1 + resets * this / 100). Keep in sync with the client (GameLogic/ResetBoost.h).")]
    public float DamageAndDefensePercentPerReset { get; set; } = 0.3f;
}
