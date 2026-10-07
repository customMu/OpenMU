// <copyright file="ISkillCastTimeCheckPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin point interface which checks the minimum time between two skill casts of a player.
/// </summary>
[Guid("3E6A1D52-8B47-4C9F-A2D1-5F0B7C3E9A68")]
[PlugInPoint("Skill cast time check", "Is called before a player casts an attack skill; refuses casts faster than the fix time of the skill.")]
public interface ISkillCastTimeCheckPlugIn
{
    /// <summary>
    /// Checks if the player may cast the skill now and registers the cast.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skill">The skill.</param>
    /// <param name="eventArgs">Cancelled, if the cast comes too early and has to be ignored.</param>
    void CheckCast(Player player, Skill skill, CancelEventArgs eventArgs);
}
