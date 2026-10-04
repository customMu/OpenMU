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

    /// <summary>
    /// Gets or sets the chance per killed monster for a jewel, in percent.
    /// </summary>
    [Display(Name = "Jewel chance (%)", Description = "Chance per killed monster (not the boss) for one jewel; its kind by the weights below.")]
    public float JewelChancePercent { get; set; } = 2f;

    /// <summary>
    /// Gets or sets the weight of the Jewel of Chaos.
    /// </summary>
    [Display(Name = "Weight: Jewel of Chaos")]
    public int ChaosWeight { get; set; } = 40;

    /// <summary>
    /// Gets or sets the weight of the Jewel of Bless.
    /// </summary>
    [Display(Name = "Weight: Jewel of Bless")]
    public int BlessWeight { get; set; } = 35;

    /// <summary>
    /// Gets or sets the weight of the Jewel of Soul.
    /// </summary>
    [Display(Name = "Weight: Jewel of Soul")]
    public int SoulWeight { get; set; } = 20;

    /// <summary>
    /// Gets or sets the weight of the Jewel of Life.
    /// </summary>
    [Display(Name = "Weight: Jewel of Life")]
    public int LifeWeight { get; set; } = 5;

    /// <summary>
    /// Gets or sets the weight of the Jewel of Creation.
    /// </summary>
    [Display(Name = "Weight: Jewel of Creation")]
    public int CreationWeight { get; set; }

    /// <summary>
    /// Gets or sets the weight of the Jewel of Guardian.
    /// </summary>
    [Display(Name = "Weight: Jewel of Guardian")]
    public int GuardianWeight { get; set; }

    /// <summary>
    /// Gets or sets the lowest rank of the dropped items.
    /// </summary>
    [Display(Name = "Item rank from", Description = "The monsters drop the items of these ranks (plugin 'Item drop by rank'), with the chance of the plugin x the item chance multiplier.")]
    public int MinimumItemRank { get; set; } = 3;

    /// <summary>
    /// Gets or sets the highest rank of the dropped items.
    /// </summary>
    [Display(Name = "Item rank to")]
    public int MaximumItemRank { get; set; } = 4;

    /// <summary>
    /// Gets or sets the level of the items which the monsters drop.
    /// </summary>
    [Display(Name = "Item level", Description = "Level of the items which the monsters drop (e.g. +1 in Kalima 5). The weapons of the boss and of Kundun have their own levels.")]
    public int ItemLevel { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"Kalima {this.Level}: {this.MinimumResets} resets";
}
