// <copyright file="ResetExperienceRateConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration for the <see cref="ResetExperienceRatePlugIn"/>.
/// The experience of a kill is multiplied by the rate of the tier with the highest
/// <see cref="ExperienceRateTier.MinimumResetCount"/> which the character has reached.
/// </summary>
public class ResetExperienceRateConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the rate also applies to master experience.
    /// </summary>
    [Display(Name = "Apply to master experience", Description = "If checked, the tier rate multiplies master experience, too. Otherwise only normal experience is affected.")]
    public bool ApplyToMasterExperience { get; set; }

    /// <summary>
    /// Gets or sets the rate tiers.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Rate tiers", Description = "The rate of the tier with the highest minimum reset count which the character has reached is used. Without a matching tier the rate is 1. The rate is applied on top of the server experience rate, so set the server rate (game configuration) to 1.")]
    public ICollection<ExperienceRateTier> Tiers { get; set; } = CreateDefaultTiers();

    /// <summary>
    /// Gets the rate for the specified reset count.
    /// </summary>
    /// <param name="resetCount">The reset count of the character.</param>
    /// <returns>The experience rate of the matching tier, or 1 if no tier matches.</returns>
    public float GetRate(int resetCount)
    {
        ExperienceRateTier? matchingTier = null;
        foreach (var tier in this.Tiers)
        {
            if (tier.MinimumResetCount <= resetCount
                && (matchingTier is null || tier.MinimumResetCount > matchingTier.MinimumResetCount))
            {
                matchingTier = tier;
            }
        }

        return matchingTier is null ? 1f : Math.Max(matchingTier.Rate, 0f);
    }

    private static List<ExperienceRateTier> CreateDefaultTiers() =>
    [
        new() { MinimumResetCount = 0, Rate = 30f },
        new() { MinimumResetCount = 2, Rate = 27f },
        new() { MinimumResetCount = 3, Rate = 24f },
        new() { MinimumResetCount = 4, Rate = 21f },
        new() { MinimumResetCount = 5, Rate = 18f },
        new() { MinimumResetCount = 6, Rate = 15f },
        new() { MinimumResetCount = 7, Rate = 13f },
        new() { MinimumResetCount = 8, Rate = 11f },
        new() { MinimumResetCount = 9, Rate = 9f },
        new() { MinimumResetCount = 10, Rate = 6f },
        new() { MinimumResetCount = 20, Rate = 4f },
        new() { MinimumResetCount = 30, Rate = 2f },
        new() { MinimumResetCount = 40, Rate = 1.5f },
        new() { MinimumResetCount = 46, Rate = 1f },
    ];

    /// <summary>
    /// Defines the experience rate from a reset count on.
    /// </summary>
    public class ExperienceRateTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count at which this tier applies.
        /// </summary>
        [Display(Name = "Minimum reset count")]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the experience rate of this tier.
        /// </summary>
        [Display(Name = "Rate")]
        public float Rate { get; set; } = 1f;
    }
}
