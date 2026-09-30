// <copyright file="JewelDropConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="JewelDropPlugIn"/>.
/// </summary>
public class JewelDropConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether jewels drop in mini games (Blood Castle, Devil Square, Chaos Castle etc.).
    /// </summary>
    [Display(Name = "Apply in mini games", Description = "If enabled, jewels also drop from monsters in mini games (Blood Castle, Devil Square, Chaos Castle, ...). Mini games have their own rewards, so this is off by default.")]
    public bool ApplyInMiniGames { get; set; }

    /// <summary>
    /// Gets or sets the jewels which can drop.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Jewels", Description = "Every jewel is rolled independently on each kill. Chance per kill = 'Chance per kill (%)' * tier multiplier * map multiplier * monster multiplier.")]
    public ICollection<JewelDropEntry> Jewels { get; set; } = CreateDefaultJewels();

    /// <summary>
    /// Gets or sets the tiers by monster level.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Tiers by monster level", Description = "The tier with the highest 'Minimum monster level' which is not above the monster level applies. Monsters below the lowest tier drop no jewels. An empty list means multiplier 1 for all monsters.")]
    public ICollection<JewelDropTier> Tiers { get; set; } = CreateDefaultTiers();

    /// <summary>
    /// Gets or sets the multipliers for specific maps.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Map multipliers", Description = "Multipliers for specific maps. Maps which are not listed use 1. Use 0 to disable jewel drops on a map.")]
    public ICollection<JewelDropMapMultiplier> MapMultipliers { get; set; } = new List<JewelDropMapMultiplier>();

    /// <summary>
    /// Gets or sets the multipliers for specific monsters.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Monster multipliers", Description = "Multipliers for specific monsters. Monsters which are not listed use 1. Use 0 to disable jewel drops of a monster.")]
    public ICollection<JewelDropMonsterMultiplier> MonsterMultipliers { get; set; } = new List<JewelDropMonsterMultiplier>();

    private static ICollection<JewelDropEntry> CreateDefaultJewels() => new List<JewelDropEntry>
    {
        new() { Name = "Jewel of Bless", ItemGroup = 14, ItemNumber = 13, ChancePercent = 0.07f },
        new() { Name = "Jewel of Soul", ItemGroup = 14, ItemNumber = 14, ChancePercent = 0.04f },
        new() { Name = "Jewel of Chaos", ItemGroup = 12, ItemNumber = 15, ChancePercent = 0.05f },
        new() { Name = "Jewel of Life", ItemGroup = 14, ItemNumber = 16, ChancePercent = 0.015f, MinimumMonsterLevel = 50 },
        new() { Name = "Jewel of Creation", ItemGroup = 14, ItemNumber = 22, ChancePercent = 0.015f, MinimumMonsterLevel = 50 },
        new() { Name = "Jewel of Guardian", ItemGroup = 14, ItemNumber = 31, ChancePercent = 0f, MinimumMonsterLevel = 80 },
        new() { Name = "Gemstone", ItemGroup = 14, ItemNumber = 41, ChancePercent = 0f, MinimumMonsterLevel = 80 },
        new() { Name = "Jewel of Harmony", ItemGroup = 14, ItemNumber = 42, ChancePercent = 0f, MinimumMonsterLevel = 80 },
        new() { Name = "Lower refine stone", ItemGroup = 14, ItemNumber = 43, ChancePercent = 0f, MinimumMonsterLevel = 80 },
        new() { Name = "Higher refine stone", ItemGroup = 14, ItemNumber = 44, ChancePercent = 0f, MinimumMonsterLevel = 80 },
    };

    private static ICollection<JewelDropTier> CreateDefaultTiers() => new List<JewelDropTier>
    {
        new() { MinimumMonsterLevel = 1, Multiplier = 0.25f },
        new() { MinimumMonsterLevel = 20, Multiplier = 1f },
        new() { MinimumMonsterLevel = 50, Multiplier = 1.5f },
        new() { MinimumMonsterLevel = 80, Multiplier = 2.5f },
        new() { MinimumMonsterLevel = 110, Multiplier = 4f },
    };

    /// <summary>
    /// A jewel which can drop.
    /// </summary>
    public class JewelDropEntry
    {
        /// <summary>
        /// Gets or sets the name, only for the overview.
        /// </summary>
        [Display(Name = "Name", Description = "Only for the overview; the item is identified by group and number.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item group.
        /// </summary>
        [Display(Name = "Item group")]
        public short ItemGroup { get; set; }

        /// <summary>
        /// Gets or sets the item number.
        /// </summary>
        [Display(Name = "Item number")]
        public short ItemNumber { get; set; }

        /// <summary>
        /// Gets or sets the chance per kill in percent at multiplier 1.
        /// </summary>
        [Display(Name = "Chance per kill (%)", Description = "Chance per kill in percent at multiplier 1 (e.g. 0.07 = one piece per ~1430 kills). 0 disables this jewel.")]
        public float ChancePercent { get; set; }

        /// <summary>
        /// Gets or sets the minimum monster level.
        /// </summary>
        [Display(Name = "Minimum monster level")]
        public int MinimumMonsterLevel { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum monster level.
        /// </summary>
        [Display(Name = "Maximum monster level")]
        public int MaximumMonsterLevel { get; set; } = 255;
    }

    /// <summary>
    /// A tier by monster level.
    /// </summary>
    public class JewelDropTier
    {
        /// <summary>
        /// Gets or sets the minimum monster level of this tier.
        /// </summary>
        [Display(Name = "Minimum monster level")]
        public int MinimumMonsterLevel { get; set; }

        /// <summary>
        /// Gets or sets the multiplier.
        /// </summary>
        [Display(Name = "Multiplier")]
        public float Multiplier { get; set; } = 1f;
    }

    /// <summary>
    /// A multiplier for a map.
    /// </summary>
    public class JewelDropMapMultiplier
    {
        /// <summary>
        /// Gets or sets the map number.
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
    /// A multiplier for a monster.
    /// </summary>
    public class JewelDropMonsterMultiplier
    {
        /// <summary>
        /// Gets or sets the monster number.
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
