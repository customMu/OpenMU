// <copyright file="ResetEventTier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.ResetEvents;

/// <summary>
/// A level of Blood Castle or Devil Square by the resets of the character: the strength of its monsters and its rewards.
/// </summary>
public class ResetEventTier
{
    /// <summary>
    /// Gets or sets the level of the event (Blood Castle 1-8, Devil Square 1-7).
    /// </summary>
    [Display(Name = "Level")]
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the minimum resets; the level is valid up to the minimum resets of the next level minus one.
    /// </summary>
    [Display(Name = "Minimum resets")]
    public int MinimumResets { get; set; }

    /// <summary>
    /// Gets or sets the level of the regular monsters of the reset ladder whose strength the monsters of the event get.
    /// </summary>
    [Display(Name = "Monster level", Description = "The monsters get the strength of the regular monsters of this level (median of the monsters of the hunting maps within +-15 levels); the differences between the monsters of the event stay.")]
    public int MonsterLevel { get; set; }

    /// <summary>
    /// Gets or sets the factor of the health.
    /// </summary>
    [Display(Name = "Health factor")]
    public float HealthFactor { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the factor of the damage.
    /// </summary>
    [Display(Name = "Damage factor")]
    public float DamageFactor { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the factor of the defense.
    /// </summary>
    [Display(Name = "Defense factor")]
    public float DefenseFactor { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the money reward.
    /// </summary>
    [Display(Name = "Money", Description = "Blood Castle: every finisher (the one who delivered the weapon x1.5). Devil Square: the 1st place, the others a part of it.")]
    public int Money { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of chaos (1.5 = one and a 50 % chance for a second one).
    /// </summary>
    [Display(Name = "Jewels of Chaos", Description = "The jewels of the winner (Blood Castle: who delivered the weapon; Devil Square: 1st place). 1.5 = one and a 50 % chance for a second one.")]
    public float ChaosJewels { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of bless.
    /// </summary>
    [Display(Name = "Jewels of Bless")]
    public float BlessJewels { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of soul.
    /// </summary>
    [Display(Name = "Jewels of Soul")]
    public float SoulJewels { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of life.
    /// </summary>
    [Display(Name = "Jewels of Life")]
    public float LifeJewels { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of creation.
    /// </summary>
    [Display(Name = "Jewels of Creation")]
    public float CreationJewels { get; set; }

    /// <summary>
    /// Gets or sets the number of jewels of guardian.
    /// </summary>
    [Display(Name = "Jewels of Guardian")]
    public float GuardianJewels { get; set; }

    /// <summary>
    /// Gets or sets the chance of a chaos weapon (+1 to +5, option +4 or +8) for the winner.
    /// </summary>
    [Display(Name = "Chaos weapon chance", Description = "0.2 = 20 %: Chaos Dragon Axe, Chaos Nature Bow or Chaos Lightning Staff for the first wings.")]
    public float ChaosWeaponChance { get; set; }

    /// <summary>
    /// Gets or sets the chance of a Loch's Feather for the winner.
    /// </summary>
    [Display(Name = "Feather chance", Description = "0.01 = 1 %: Loch's Feather for the second wings.")]
    public float FeatherChance { get; set; }

    /// <summary>
    /// Gets or sets the chance of a Crest of Monarch for the winner.
    /// </summary>
    [Display(Name = "Crest of Monarch chance", Description = "0.01 = 1 %: Crest of Monarch for the cape of the Dark Lord.")]
    public float CrestChance { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"Level {this.Level}: {this.MinimumResets} resets";
}
