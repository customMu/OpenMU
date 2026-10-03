// <copyright file="KalimaInstanceTier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// A tier (Kalima map) of the Kalima instance.
/// </summary>
public class KalimaInstanceTier
{
    /// <summary>
    /// Gets or sets the level of the tier, which is the <see cref="MiniGameDefinition.GameLevel"/> of the mini game (Kalima 1 to 7).
    /// </summary>
    [Display(Name = "Level", Description = "The Kalima level (1-7), the game level of the mini game definition of the type 'Kalima instance'.")]
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the minimum resets which every member needs for this tier.
    /// </summary>
    [Display(Name = "Minimum resets")]
    public int MinimumResets { get; set; }

    /// <summary>
    /// Gets or sets the number of the reference map, whose monsters define the strength of the tier.
    /// </summary>
    [Display(Name = "Reference map", Description = "Number of the map of the reset ladder whose monsters define the strength (the median of its monsters, as stored in the database).")]
    public short ReferenceMapNumber { get; set; }

    /// <summary>
    /// Gets or sets the factor of the monster level relative to the reference map.
    /// </summary>
    [Display(Name = "Level factor", Description = "Monster level = level of the reference map x this factor (1.1 = a bit stronger).")]
    public float LevelFactor { get; set; } = 1.1f;

    /// <summary>
    /// Gets or sets the factor of the health relative to the reference map.
    /// </summary>
    [Display(Name = "Health factor")]
    public float HealthFactor { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the factor of the damage relative to the reference map.
    /// </summary>
    [Display(Name = "Damage factor")]
    public float DamageFactor { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the factor of the defense relative to the reference map.
    /// </summary>
    [Display(Name = "Defense factor", Description = "The defense is subtracted from the damage, so a high factor makes weaker players useless.")]
    public float DefenseFactor { get; set; } = 1.1f;

    /// <summary>
    /// Gets or sets the item drop multiplier.
    /// </summary>
    [Display(Name = "Drop multiplier", Description = "Item drop multiplier, 1.5 means one roll and a 50 % chance for a second one.")]
    public float DropMultiplier { get; set; } = 1.0f;

    /// <inheritdoc />
    public override string ToString() => $"Kalima {this.Level}: {this.MinimumResets} resets";
}
