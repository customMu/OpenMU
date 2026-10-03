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
    /// Gets or sets the health multiplier of the monsters for a single player.
    /// </summary>
    [Display(Name = "Health multiplier")]
    public float HealthMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the defense multiplier of the monsters for a single player.
    /// </summary>
    [Display(Name = "Defense multiplier")]
    public float DefenseMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the damage multiplier of the monsters for a single player.
    /// </summary>
    [Display(Name = "Damage multiplier")]
    public float DamageMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the item drop multiplier for a single player.
    /// </summary>
    [Display(Name = "Drop multiplier")]
    public float DropMultiplier { get; set; } = 1.0f;

    /// <inheritdoc />
    public override string ToString() => $"Kalima {this.Level}: {this.MinimumResets} resets";
}
