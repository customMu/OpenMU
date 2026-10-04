// <copyright file="IKillQuestViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.PlugIns.KillQuests;

/// <summary>
/// Interface of a view which shows the current kill quest in the quest window of the client (key T).
/// Without such a view the player sees the quest only through the chat messages and /quest.
/// </summary>
public interface IKillQuestViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the state of the kill quests of the character.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <returns>The task.</returns>
    ValueTask ShowKillQuestAsync(KillQuestState state);
}
