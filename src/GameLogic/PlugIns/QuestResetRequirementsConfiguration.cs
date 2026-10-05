// <copyright file="QuestResetRequirementsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="QuestResetRequirementsPlugIn"/>.
/// </summary>
public class QuestResetRequirementsConfiguration
{
    /// <summary>
    /// Gets or sets the required resets per quest.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Quests", Description = "Required resets per quest (group and number of the quest definition). The legacy class change quests are in group 0.")]
    public ICollection<QuestResetRequirement> Quests { get; set; } = CreateDefaultQuests();

    private static ICollection<QuestResetRequirement> CreateDefaultQuests() => new List<QuestResetRequirement>
    {
        new() { Name = "Find the 'Scroll of Emperor' (Sevina)", QuestGroup = 0, QuestNumber = 0, MinimumResets = 1 },
        new() { Name = "Treasures of MU (Sevina)", QuestGroup = 0, QuestNumber = 1, MinimumResets = 1 },
        new() { Name = "Gain Hero Status (Marlon)", QuestGroup = 0, QuestNumber = 2, MinimumResets = 3 },
        new() { Name = "Secret of the 'Dark Stone' (Marlon)", QuestGroup = 0, QuestNumber = 3, MinimumResets = 3 },
    };

    /// <summary>
    /// The required resets of a quest.
    /// </summary>
    public class QuestResetRequirement
    {
        /// <summary>
        /// Gets or sets the name (only for the admin panel).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group of the quest.
        /// </summary>
        [Display(Name = "Quest group")]
        public short QuestGroup { get; set; }

        /// <summary>
        /// Gets or sets the number of the quest.
        /// </summary>
        [Display(Name = "Quest number")]
        public short QuestNumber { get; set; }

        /// <summary>
        /// Gets or sets the minimum resets.
        /// </summary>
        [Display(Name = "Minimum resets")]
        public int MinimumResets { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Name} ({this.QuestGroup}/{this.QuestNumber}): {this.MinimumResets} resets";
    }
}
