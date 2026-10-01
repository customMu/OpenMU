// <copyright file="WarpResetRequirementsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="WarpResetRequirementsPlugIn"/>.
/// </summary>
public class WarpResetRequirementsConfiguration
{
    /// <summary>
    /// Gets or sets the required resets per warp list entry.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Warps", Description = "Required resets per entry of the warp list (index of the warp info). Entries without a row need only the requirement of their map.")]
    public ICollection<WarpResetRequirement> Warps { get; set; } = CreateDefaultWarps();

    private static ICollection<WarpResetRequirement> CreateDefaultWarps() => new List<WarpResetRequirement>
    {
        new() { Name = "Dungeon2", WarpIndex = 9, MinimumResets = 1 },
        new() { Name = "Dungeon3", WarpIndex = 10, MinimumResets = 2 },
        new() { Name = "Atlans", WarpIndex = 11, MinimumResets = 1 },
        new() { Name = "Atlans2", WarpIndex = 12, MinimumResets = 3 },
        new() { Name = "Atlans3", WarpIndex = 13, MinimumResets = 4 },
        new() { Name = "LostTower", WarpIndex = 14, MinimumResets = 2 },
        new() { Name = "LostTower2", WarpIndex = 15, MinimumResets = 2 },
        new() { Name = "LostTower3", WarpIndex = 16, MinimumResets = 3 },
        new() { Name = "LostTower4", WarpIndex = 17, MinimumResets = 3 },
        new() { Name = "LostTower5", WarpIndex = 18, MinimumResets = 4 },
        new() { Name = "LostTower6", WarpIndex = 19, MinimumResets = 4 },
        new() { Name = "LostTower7", WarpIndex = 20, MinimumResets = 5 },
        new() { Name = "Tarkan", WarpIndex = 21, MinimumResets = 6 },
        new() { Name = "Tarkan2", WarpIndex = 22, MinimumResets = 7 },
        new() { Name = "Icarus", WarpIndex = 23, MinimumResets = 8 },
        new() { Name = "Aida1", WarpIndex = 25, MinimumResets = 7 },
        new() { Name = "Aida2", WarpIndex = 27, MinimumResets = 15 },
        new() { Name = "KanturuRuins1", WarpIndex = 28, MinimumResets = 9 },
        new() { Name = "KanturuRuins2", WarpIndex = 29, MinimumResets = 10 },
        new() { Name = "KanturuRuins3", WarpIndex = 45, MinimumResets = 25 },
        new() { Name = "Karutan1", WarpIndex = 46, MinimumResets = 10 },
        new() { Name = "Karutan2", WarpIndex = 47, MinimumResets = 20 },
        new() { Name = "Raklion", WarpIndex = 34, MinimumResets = 30 },
        new() { Name = "LaCleon", WarpIndex = 48, MinimumResets = 30 },
        new() { Name = "KanturuRelics", WarpIndex = 30, MinimumResets = 50 },
        new() { Name = "PeaceSwamp", WarpIndex = 33, MinimumResets = 50 },
        new() { Name = "Vulcanus", WarpIndex = 42, MinimumResets = 50 },
    };

    /// <summary>
    /// The required resets of a warp list entry.
    /// </summary>
    public class WarpResetRequirement
    {
        /// <summary>
        /// Gets or sets the name (only for the admin panel).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the index of the warp list entry.
        /// </summary>
        [Display(Name = "Warp index")]
        public int WarpIndex { get; set; }

        /// <summary>
        /// Gets or sets the minimum resets.
        /// </summary>
        [Display(Name = "Minimum resets")]
        public int MinimumResets { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Name} ({this.WarpIndex}): {this.MinimumResets} resets";
    }
}
