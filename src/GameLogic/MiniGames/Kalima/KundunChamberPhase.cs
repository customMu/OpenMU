// <copyright file="KundunChamberPhase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// A phase of the fight against Kundun. When the health of Kundun reaches the threshold, it stops there,
/// Illusions of Kundun appear and Kundun is invulnerable until they are killed. The longer the players
/// need for the Illusions (after some free seconds), the more Kundun heals and the stronger he gets.
/// Each phase happens once; when Kundun heals above a threshold again, its phase doesn't repeat.
/// </summary>
public class KundunChamberPhase
{
    /// <summary>
    /// Gets or sets the health threshold of the phase, relative to the maximum health of Kundun.
    /// </summary>
    [Display(Name = "Health threshold", Description = "The phase starts when the health of Kundun reaches this part of his maximum health (0.75 = 75 %). The damage which would go below is cut.")]
    public float HealthThreshold { get; set; }

    /// <summary>
    /// Gets or sets the number of Illusions of Kundun which appear.
    /// </summary>
    [Display(Name = "Illusions")]
    public int IllusionCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the health of each Illusion, relative to the health of the Illusion of Kundun of the Kalima instance of the same level.
    /// </summary>
    [Display(Name = "Illusion health factor", Description = "Health of each Illusion relative to the Illusion of Kundun of the Kalima instance of the same level (0.5 = half).")]
    public float IllusionHealthFactor { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the damage of each Illusion, relative to the Illusion of Kundun of the Kalima instance of the same level.
    /// </summary>
    [Display(Name = "Illusion damage factor")]
    public float IllusionDamageFactor { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the healing of Kundun per second after the free seconds, relative to his maximum health.
    /// </summary>
    [Display(Name = "Heal per second", Description = "Kundun heals this part of his maximum health per second which the players needed for the Illusions after the free seconds (0.001 = 0.1 %).")]
    public float HealPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum healing of the phase, relative to the maximum health of Kundun.
    /// </summary>
    [Display(Name = "Maximum heal")]
    public float MaximumHeal { get; set; }

    /// <summary>
    /// Gets or sets the defense increase of Kundun per second after the free seconds.
    /// </summary>
    [Display(Name = "Defense per second", Description = "The defense of Kundun increases by this part per second (0.002 = 0.2 %), until the end of the fight.")]
    public float DefensePerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum defense increase of the phase.
    /// </summary>
    [Display(Name = "Maximum defense increase")]
    public float MaximumDefenseIncrease { get; set; }

    /// <summary>
    /// Gets or sets the damage increase of Kundun per second after the free seconds.
    /// </summary>
    [Display(Name = "Damage per second", Description = "The damage of Kundun increases by this part per second (0.003 = 0.3 %), until the end of the fight.")]
    public float DamagePerSecond { get; set; }

    /// <summary>
    /// Gets or sets the maximum damage increase of the phase.
    /// </summary>
    [Display(Name = "Maximum damage increase")]
    public float MaximumDamageIncrease { get; set; }

    /// <summary>
    /// Calculates the effects of the phase.
    /// </summary>
    /// <param name="penaltySeconds">The seconds which the players needed for the Illusions after the free seconds.</param>
    /// <returns>The heal (part of the maximum health), the defense increase and the damage increase.</returns>
    public (float Heal, float Defense, float Damage) CalculateEffects(double penaltySeconds)
    {
        var seconds = (float)Math.Max(0, penaltySeconds);
        return (
            Math.Min(seconds * this.HealPerSecond, this.MaximumHeal),
            Math.Min(seconds * this.DefensePerSecond, this.MaximumDefenseIncrease),
            Math.Min(seconds * this.DamagePerSecond, this.MaximumDamageIncrease));
    }

    /// <inheritdoc />
    public override string ToString() => $"{this.HealthThreshold:P0}: {this.IllusionCount} Illusion(s)";
}
