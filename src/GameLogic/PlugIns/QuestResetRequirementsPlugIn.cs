// <copyright file="QuestResetRequirementsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Requires a number of resets to start a quest, in addition to its character level (e.g. the first class change
/// quests of Sevina from 1 reset, the second class change quests of Marlon from 3 resets). The quest NPC tells
/// the player to come back when he is stronger; quests which are already active can be finished.
/// </summary>
[PlugIn]
[Display(Name = "Quest reset requirements", Description = "Required resets to start a quest (group and number), in addition to the character level. The quest NPC sends the player away until then.")]
[Guid("7C2E9A41-3B6D-4F85-9E1A-5D8B2C4F7A13")]
public class QuestResetRequirementsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<QuestResetRequirementsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public QuestResetRequirementsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new QuestResetRequirementsConfiguration();

    /// <summary>
    /// Gets the required resets of the quest.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <returns>The required resets; 0 if the quest has no requirement.</returns>
    public int GetRequiredResets(QuestDefinition quest)
    {
        var configuration = this.Configuration ??= (QuestResetRequirementsConfiguration)this.CreateDefaultConfig();
        return configuration.Quests.FirstOrDefault(q => q.QuestGroup == quest.Group && q.QuestNumber == quest.Number)?.MinimumResets ?? 0;
    }

    /// <summary>
    /// Gets the resets which the player is missing to start the quest.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="quest">The quest.</param>
    /// <returns>The required resets if the player has less, otherwise 0.</returns>
    public int GetMissingResets(Player player, QuestDefinition quest)
    {
        var required = this.GetRequiredResets(quest);
        return required > (player.Attributes?[Stats.Resets] ?? 0) ? required : 0;
    }
}
