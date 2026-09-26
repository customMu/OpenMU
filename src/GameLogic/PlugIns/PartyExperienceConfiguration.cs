// <copyright file="PartyExperienceConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration for the <see cref="PartyExperienceFeaturePlugIn"/>.
/// Every party member which receives experience gets the experience he would get for the kill
/// when playing solo (calculated with his own level, rates and resets), multiplied by the share
/// of the tier which matches the number of receiving members.
/// </summary>
public class PartyExperienceConfiguration
{
    /// <summary>
    /// Gets or sets the shares per number of receiving party members.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Shares", Description = "Share of the solo experience which each receiving member gets, by the number of receiving members (the killer and the members in his view range). The tier with the highest member count which is not above the actual count is used. Without a matching tier, each member gets 1 / count.")]
    public ICollection<PartyExperienceShare> Shares { get; set; } = CreateDefaultShares();

    /// <summary>
    /// Gets the share of the solo experience for the specified number of receiving members.
    /// </summary>
    /// <param name="memberCount">The number of receiving members.</param>
    /// <returns>The share of the solo experience which each member gets.</returns>
    public double GetShare(int memberCount)
    {
        if (memberCount <= 1)
        {
            return 1.0;
        }

        PartyExperienceShare? matchingTier = null;
        foreach (var tier in this.Shares)
        {
            if (tier.MemberCount <= memberCount
                && (matchingTier is null || tier.MemberCount > matchingTier.MemberCount))
            {
                matchingTier = tier;
            }
        }

        return matchingTier is null ? 1.0 / memberCount : Math.Max(matchingTier.Share, 0f);
    }

    private static List<PartyExperienceShare> CreateDefaultShares() =>
    [
        new() { MemberCount = 2, Share = 0.90f },
        new() { MemberCount = 3, Share = 0.80f },
        new() { MemberCount = 4, Share = 0.70f },
        new() { MemberCount = 5, Share = 0.60f },
    ];

    /// <summary>
    /// Defines the share of the solo experience from a number of receiving members on.
    /// </summary>
    public class PartyExperienceShare
    {
        /// <summary>
        /// Gets or sets the number of receiving members at which this tier applies.
        /// </summary>
        [Display(Name = "Member count")]
        public int MemberCount { get; set; }

        /// <summary>
        /// Gets or sets the share of the solo experience which each member gets, e.g. 0.6 = 60 %.
        /// </summary>
        [Display(Name = "Share of solo experience")]
        public float Share { get; set; } = 1f;
    }
}
