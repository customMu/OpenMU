// <copyright file="MasterLevelByResetsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration for the <see cref="MasterLevelByResetsPlugIn"/>.
/// Every reset raises the maximum master level by <see cref="MasterLevelTier.LevelsPerReset"/> of the tier
/// with the highest <see cref="MasterLevelTier.MinimumResetCount"/> which the reset has reached.
/// Resets below the lowest tier don't raise it, so the master level is locked until then.
/// </summary>
public class MasterLevelByResetsConfiguration
{
    /// <summary>
    /// Gets or sets the tiers.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Tiers", Description = "Each reset raises the maximum master level by the levels of the tier with the highest minimum reset count which this reset has reached. Resets below the lowest tier don't raise it. The maximum master level of the game configuration still applies.")]
    public ICollection<MasterLevelTier> Tiers { get; set; } = CreateDefaultTiers();

    /// <summary>
    /// Gets the maximum master level for the specified reset count.
    /// </summary>
    /// <param name="resetCount">The reset count of the character.</param>
    /// <returns>The sum of the levels of all resets up to the reset count.</returns>
    public int GetMaximumMasterLevel(int resetCount)
    {
        var total = 0;
        for (var reset = 1; reset <= resetCount; reset++)
        {
            MasterLevelTier? matchingTier = null;
            foreach (var tier in this.Tiers)
            {
                if (tier.MinimumResetCount <= reset
                    && (matchingTier is null || tier.MinimumResetCount > matchingTier.MinimumResetCount))
                {
                    matchingTier = tier;
                }
            }

            total += Math.Max(matchingTier?.LevelsPerReset ?? 0, 0);
        }

        return total;
    }

    /// <summary>
    /// Gets the reset count at which the master level is unlocked, i.e. the lowest tier with levels.
    /// </summary>
    /// <returns>The reset count, or <c>null</c> if no tier has levels.</returns>
    public int? GetUnlockResetCount()
    {
        int? result = null;
        foreach (var tier in this.Tiers)
        {
            if (tier.LevelsPerReset > 0 && (result is null || tier.MinimumResetCount < result))
            {
                result = tier.MinimumResetCount;
            }
        }

        return result;
    }

    private static List<MasterLevelTier> CreateDefaultTiers() =>
    [
        new() { MinimumResetCount = 20, LevelsPerReset = 3 },
        new() { MinimumResetCount = 24, LevelsPerReset = 4 },
        new() { MinimumResetCount = 28, LevelsPerReset = 5 },
        new() { MinimumResetCount = 32, LevelsPerReset = 6 },
        new() { MinimumResetCount = 36, LevelsPerReset = 7 },
        new() { MinimumResetCount = 40, LevelsPerReset = 8 },
        new() { MinimumResetCount = 43, LevelsPerReset = 9 },
        new() { MinimumResetCount = 47, LevelsPerReset = 10 },
    ];

    /// <summary>
    /// Defines by how many levels a reset raises the maximum master level, from a reset count on.
    /// </summary>
    public class MasterLevelTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count at which this tier applies.
        /// </summary>
        [Display(Name = "Minimum reset count")]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the number of master levels which each reset of this tier adds to the maximum.
        /// </summary>
        [Display(Name = "Master levels per reset")]
        public int LevelsPerReset { get; set; }
    }
}
