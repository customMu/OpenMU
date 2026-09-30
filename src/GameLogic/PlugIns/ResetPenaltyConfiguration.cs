// <copyright file="ResetPenaltyConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration for the <see cref="ResetPenaltyPlugIn"/>.
/// Monsters are grouped into tiers by their level. A character whose reset count reached the
/// "full penalty from reset" of a tier gets the regular (level based) experience penalty and a
/// reduced drop for the monsters of this tier; everybody else gets the experience without any
/// level penalty.
/// </summary>
public class ResetPenaltyConfiguration
{
    /// <summary>
    /// Gets or sets the chance that a penalized kill still drops anything.
    /// </summary>
    [Display(Name = "Drop chance when penalized", Description = "Chance between 0 and 1 that a penalized kill still drops its regular drop (items, money, jewels, event and quest items). 0.05 means 5 %. The drop is penalized if any receiver of the drop group reached the penalty of the monster's tier.")]
    public float DropChanceWhenPenalized { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets a value indicating whether the level penalty uses the level without the master level.
    /// </summary>
    [Display(Name = "Penalty by level without master level", Description = "If checked, the regular experience penalty is calculated from the character level only. Otherwise the master level is added, as in the original formula.")]
    public bool UseLevelWithoutMasterLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets the penalty tiers.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Penalty tiers", Description = "A monster belongs to the tier with the lowest maximum monster level which is not lower than its level. Full penalty from reset = 0 means no penalty for this tier. Monsters above all tiers are never penalized.")]
    public ICollection<ResetPenaltyTier> Tiers { get; set; } = CreateDefaultTiers();

    /// <summary>
    /// Determines whether a character with the specified reset count is penalized for a monster of the specified level.
    /// </summary>
    /// <param name="resetCount">The reset count of the character.</param>
    /// <param name="monsterLevel">The level of the monster.</param>
    /// <returns><c>true</c>, if the penalty applies.</returns>
    public bool IsPenalized(int resetCount, int monsterLevel)
    {
        ResetPenaltyTier? matchingTier = null;
        foreach (var tier in this.Tiers)
        {
            if (monsterLevel <= tier.MaximumMonsterLevel
                && (matchingTier is null || tier.MaximumMonsterLevel < matchingTier.MaximumMonsterLevel))
            {
                matchingTier = tier;
            }
        }

        return matchingTier is { FullPenaltyFromReset: > 0 } && resetCount >= matchingTier.FullPenaltyFromReset;
    }

    private static List<ResetPenaltyTier> CreateDefaultTiers() =>
    [
        new() { MaximumMonsterLevel = 10, FullPenaltyFromReset = 1 },
        new() { MaximumMonsterLevel = 18, FullPenaltyFromReset = 1 },
        new() { MaximumMonsterLevel = 40, FullPenaltyFromReset = 6 },
        new() { MaximumMonsterLevel = 55, FullPenaltyFromReset = 6 },
        new() { MaximumMonsterLevel = 71, FullPenaltyFromReset = 10 },
        new() { MaximumMonsterLevel = 90, FullPenaltyFromReset = 15 },
        new() { MaximumMonsterLevel = 108, FullPenaltyFromReset = 30 },
    ];

    /// <summary>
    /// A tier of monsters, defined by their maximum level.
    /// </summary>
    public class ResetPenaltyTier
    {
        /// <summary>
        /// Gets or sets the maximum monster level of this tier.
        /// </summary>
        [Display(Name = "Maximum monster level")]
        public int MaximumMonsterLevel { get; set; }

        /// <summary>
        /// Gets or sets the reset count from which on the penalty applies.
        /// </summary>
        [Display(Name = "Full penalty from reset", Description = "0 = never.")]
        public int FullPenaltyFromReset { get; set; }
    }
}
