// <copyright file="SetGuardConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// Configuration of the <see cref="SetGuardPlugIn"/>.
/// </summary>
public class SetGuardConfiguration
{
    /// <summary>
    /// Gets or sets the armor sets. The pieces of a set share the item number in the groups 7 (helm) to 11 (boots).
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Sets", Description = "Armor sets by item number (the same number in the groups 7 to 11) and their rank. Sets which are not listed give no Set Guard.")]
    public ICollection<SetGuardSet> Sets { get; set; } = CreateDefaultSets();

    /// <summary>
    /// Gets or sets the damage decrease of a complete set per rank.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Ranks", Description = "Damage decrease in percent of a complete set of the rank, before the enhancement steps. The client shows the same values (GameLogic/Items/SetGuard.h), keep them in sync.")]
    public ICollection<SetGuardRank> Ranks { get; set; } = CreateDefaultRanks();

    /// <summary>
    /// Gets or sets the item levels of the enhancement steps.
    /// </summary>
    [Display(Name = "Step levels", Description = "Comma separated item levels. Every step which the lowest level of the set reaches adds 'Step bonus (%)' of the rank value.")]
    public string StepLevels { get; set; } = "5,9,11,13,15";

    /// <summary>
    /// Gets or sets the bonus per reached step, in percent of the rank value.
    /// </summary>
    [Display(Name = "Step bonus (%)", Description = "Bonus per reached enhancement step, in percent of the rank value. 10 means x1.5 with all five steps.")]
    public float StepBonusPercent { get; set; } = 10f;

    /// <summary>
    /// Gets or sets a value indicating whether a complete set of a class which can't wear one of its pieces (the helm of
    /// the Magic Gladiator, the gloves of the Rage Fighter) gives the defense of that piece, at the lowest level of the set.
    /// </summary>
    [Display(Name = "Compensate missing pieces", Description = "A complete set of a class which can't wear one of its pieces (MG: helm, RF: gloves) gives the base defense of that piece at the lowest level of the set - the sets which the class shares with other classes are as strong as for them.")]
    public bool CompensateMissingPieces { get; set; } = true;

    private static ICollection<SetGuardSet> CreateDefaultSets() => new List<SetGuardSet>
    {
        new() { Name = "Pad", ItemNumber = 2, Rank = 1 },
        new() { Name = "Leather", ItemNumber = 5, Rank = 1 },
        new() { Name = "Vine", ItemNumber = 10, Rank = 1 },
        new() { Name = "Mistery", ItemNumber = 39, Rank = 1 },
        new() { Name = "Bone", ItemNumber = 4, Rank = 2 },
        new() { Name = "Scale", ItemNumber = 6, Rank = 2 },
        new() { Name = "Silk", ItemNumber = 11, Rank = 2 },
        new() { Name = "Sphinx", ItemNumber = 7, Rank = 3 },
        new() { Name = "Brass", ItemNumber = 8, Rank = 3 },
        new() { Name = "Wind", ItemNumber = 12, Rank = 3 },
        new() { Name = "Legendary", ItemNumber = 3, Rank = 4 },
        new() { Name = "Plate", ItemNumber = 9, Rank = 4 },
        new() { Name = "Spirit", ItemNumber = 13, Rank = 4 },
        new() { Name = "Light Plate", ItemNumber = 25, Rank = 4 },
        new() { Name = "Red Wing", ItemNumber = 40, Rank = 4 },
        new() { Name = "Eclipse", ItemNumber = 35, Rank = 5 },
        new() { Name = "Dragon", ItemNumber = 1, Rank = 5 },
        new() { Name = "Ashcrow", ItemNumber = 34, Rank = 5 },
        new() { Name = "Guardian", ItemNumber = 14, Rank = 5 },
        new() { Name = "Iris", ItemNumber = 36, Rank = 5 },
        new() { Name = "Adamantine", ItemNumber = 26, Rank = 5 },
        new() { Name = "Ancient", ItemNumber = 41, Rank = 5 },
        new() { Name = "Sacred", ItemNumber = 59, Rank = 5 },
        new() { Name = "Grand Soul", ItemNumber = 18, Rank = 6 },
        new() { Name = "Black Dragon", ItemNumber = 16, Rank = 6 },
        new() { Name = "Divine", ItemNumber = 19, Rank = 6 },
        new() { Name = "Storm Crow", ItemNumber = 15, Rank = 6 },
        new() { Name = "Dark Steel", ItemNumber = 27, Rank = 6 },
        new() { Name = "Black Rose", ItemNumber = 42, Rank = 6 },
        new() { Name = "Storm Hard", ItemNumber = 60, Rank = 6 },
        new() { Name = "Dark Soul", ItemNumber = 22, Rank = 7 },
        new() { Name = "Dark Phoenix", ItemNumber = 17, Rank = 7 },
        new() { Name = "Great Dragon", ItemNumber = 21, Rank = 7 },
        new() { Name = "Red Spirit", ItemNumber = 24, Rank = 7 },
        new() { Name = "Valiant", ItemNumber = 37, Rank = 7 },
        new() { Name = "Thunder Hawk", ItemNumber = 20, Rank = 7 },
        new() { Name = "Hurricane", ItemNumber = 23, Rank = 7 },
        new() { Name = "Glorious", ItemNumber = 38, Rank = 7 },
        new() { Name = "Dark Master", ItemNumber = 28, Rank = 7 },
        new() { Name = "Lilium", ItemNumber = 44, Rank = 7 },
        new() { Name = "Piercing", ItemNumber = 61, Rank = 7 },
        new() { Name = "Venom Mist", ItemNumber = 30, Rank = 8 },
        new() { Name = "Dragon Knight", ItemNumber = 29, Rank = 8 },
        new() { Name = "Sylphid Ray", ItemNumber = 31, Rank = 8 },
        new() { Name = "Volcano", ItemNumber = 32, Rank = 8 },
        new() { Name = "Sunlight", ItemNumber = 33, Rank = 8 },
        new() { Name = "Aura", ItemNumber = 43, Rank = 8 },
        new() { Name = "Phoenix Soul", ItemNumber = 73, Rank = 8 },
    };

    private static ICollection<SetGuardRank> CreateDefaultRanks() => new List<SetGuardRank>
    {
        new() { Rank = 1, Percent = 3.0f },
        new() { Rank = 2, Percent = 3.3f },
        new() { Rank = 3, Percent = 4.2f },
        new() { Rank = 4, Percent = 6.3f },
        new() { Rank = 5, Percent = 9.7f },
        new() { Rank = 6, Percent = 14.9f },
        new() { Rank = 7, Percent = 20.7f },
        new() { Rank = 8, Percent = 28.6f },
    };

    /// <summary>
    /// An armor set.
    /// </summary>
    public class SetGuardSet
    {
        /// <summary>
        /// Gets or sets the name, just for the overview.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the item number of the set pieces (groups 7 to 11).
        /// </summary>
        [Display(Name = "Item number")]
        public short ItemNumber { get; set; }

        /// <summary>
        /// Gets or sets the rank (1 to 8).
        /// </summary>
        public int Rank { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Name} ({this.ItemNumber}): rank {this.Rank}";
    }

    /// <summary>
    /// The damage decrease of a rank.
    /// </summary>
    public class SetGuardRank
    {
        /// <summary>
        /// Gets or sets the rank.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Gets or sets the damage decrease in percent.
        /// </summary>
        [Display(Name = "Damage decrease (%)")]
        public float Percent { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"Rank {this.Rank}: {this.Percent}%";
    }
}
