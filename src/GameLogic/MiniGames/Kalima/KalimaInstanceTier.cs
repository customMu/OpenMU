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
    /// Gets or sets the average level of the monsters.
    /// </summary>
    [Display(Name = "Monster level", Description = "Average level of the monsters, which counts for the experience and the attack and defense rates.")]
    public int MonsterLevel { get; set; }

    /// <summary>
    /// Gets or sets the average health of the monsters.
    /// </summary>
    [Display(Name = "Health", Description = "Average maximum health of the regular monsters.")]
    public int Health { get; set; }

    /// <summary>
    /// Gets or sets the average maximum damage of the monsters.
    /// </summary>
    [Display(Name = "Damage", Description = "Average maximum base damage of the monsters.")]
    public int Damage { get; set; }

    /// <summary>
    /// Gets or sets the average defense of the monsters.
    /// </summary>
    [Display(Name = "Defense", Description = "Average defense of the monsters.")]
    public int Defense { get; set; }

    /// <summary>
    /// Gets or sets the item drop multiplier.
    /// </summary>
    [Display(Name = "Drop multiplier", Description = "Item drop multiplier, 1.5 means one roll and a 50 % chance for a second one.")]
    public float DropMultiplier { get; set; } = 1.0f;

    /// <inheritdoc />
    public override string ToString() => $"Kalima {this.Level}: {this.MinimumResets} resets";
}
