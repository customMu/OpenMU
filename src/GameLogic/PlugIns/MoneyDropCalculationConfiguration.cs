// <copyright file="MoneyDropCalculationConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration for the <see cref="MoneyDropCalculationPlugIn"/>.
/// The amount of money per kill is calculated as follows:
/// <c>((LevelFactor * level * (level + LevelOffset) + HealthFactor * maxHealth) * levelPenalty + MinimumMoney)
/// * GlobalMultiplier * MapMultiplier * MonsterMultiplier * random(1 +- RandomVariance)</c>.
/// </summary>
public class MoneyDropCalculationConfiguration
{
    /// <summary>
    /// Gets or sets the money per level factor. The default is the shape of the vanilla base experience formula.
    /// </summary>
    [Display(Name = "Level factor", Description = "Factor of the level part: LevelFactor * level * (level + LevelOffset). 0 disables the level part.")]
    public float LevelFactor { get; set; } = 1.25f / 3f;

    /// <summary>
    /// Gets or sets the level offset.
    /// </summary>
    [Display(Name = "Level offset", Description = "The offset which is added to the monster level in the level part.")]
    public float LevelOffset { get; set; } = 25f;

    /// <summary>
    /// Gets or sets the health factor.
    /// </summary>
    [Display(Name = "Health factor", Description = "Money per point of maximum health of the monster. Makes 'fat' monsters and bosses drop more. 0 disables it.")]
    public float HealthFactor { get; set; }

    /// <summary>
    /// Gets or sets the minimum money.
    /// </summary>
    [Display(Name = "Minimum money", Description = "Flat amount which is added to every drop (before the multipliers).")]
    public float MinimumMoney { get; set; } = 7f;

    /// <summary>
    /// Gets or sets the global multiplier.
    /// </summary>
    [Display(Name = "Global multiplier", Description = "Multiplier for all money drops. This is the main knob for balancing the economy.")]
    public float GlobalMultiplier { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the random variance.
    /// </summary>
    [Display(Name = "Random variance", Description = "Random deviation of the amount, e.g. 0.1 = +-10%. 0 = no randomness. Values are limited to 0..1.")]
    public float RandomVariance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the amount is reduced when the killer is much stronger than the monster.
    /// </summary>
    [Display(Name = "Reduce for high level killers", Description = "If enabled and the killer level is more than 10 above the monster level, the level and health part is scaled down like the experience.")]
    public bool ReduceForHighLevelKillers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the money is split equally in a party.
    /// </summary>
    [Display(Name = "Split equally in party", Description = "If enabled, the total amount is split equally between the players which take part in the kill. Otherwise it is split proportionally to their experience.")]
    public bool SplitEquallyInParty { get; set; } = true;

    /// <summary>
    /// Gets or sets the multipliers per map.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Map multipliers", Description = "Money multipliers for specific maps. Maps which are not listed use 1.")]
    public ICollection<MapMoneyMultiplier> MapMultipliers { get; set; } = new List<MapMoneyMultiplier>();

    /// <summary>
    /// Gets or sets the multipliers per monster.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Monster multipliers", Description = "Money multipliers for specific monsters (e.g. bosses). Monsters which are not listed use 1.")]
    public ICollection<MonsterMoneyMultiplier> MonsterMultipliers { get; set; } = new List<MonsterMoneyMultiplier>();

    /// <summary>
    /// Gets or sets the reset tiers for outleveled monsters.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Reset tiers (outleveled monsters)", Description = "The tier with the highest 'Minimum reset count' not above the highest reset count of the participants applies: monsters below its minimum level drop money x 'Outleveled multiplier'. Empty = off.")]
    public ICollection<ResetMonsterLevelTier> ResetTiers { get; set; } = new List<ResetMonsterLevelTier>();

    /// <summary>
    /// Gets or sets the multiplier for outleveled monsters.
    /// </summary>
    [Display(Name = "Outleveled multiplier", Description = "Money multiplier for monsters below the minimum level of the reset tier (see 'Reset tiers').")]
    public float OutleveledMultiplier { get; set; } = 0.1f;

    /// <summary>
    /// Defines the minimum monster level for full money from a reset count on.
    /// </summary>
    public class ResetMonsterLevelTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count.
        /// </summary>
        [Display(Name = "Minimum reset count")]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the minimum monster level.
        /// </summary>
        [Display(Name = "Minimum monster level")]
        public int MinimumMonsterLevel { get; set; }
    }

    /// <summary>
    /// Defines a money multiplier for a map.
    /// </summary>
    public class MapMoneyMultiplier
    {
        /// <summary>
        /// Gets or sets the number of the map.
        /// </summary>
        [Display(Name = "Map number", Description = "The map number as shown in the map definition (e.g. 0 = Lorencia).")]
        public short MapNumber { get; set; }

        /// <summary>
        /// Gets or sets the multiplier.
        /// </summary>
        [Display(Name = "Multiplier")]
        public float Multiplier { get; set; } = 1f;
    }

    /// <summary>
    /// Defines a money multiplier for a monster.
    /// </summary>
    public class MonsterMoneyMultiplier
    {
        /// <summary>
        /// Gets or sets the number of the monster.
        /// </summary>
        [Display(Name = "Monster number", Description = "The monster number as shown in the monster definition.")]
        public short MonsterNumber { get; set; }

        /// <summary>
        /// Gets or sets the multiplier.
        /// </summary>
        [Display(Name = "Multiplier")]
        public float Multiplier { get; set; } = 1f;
    }
}
