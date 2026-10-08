// <copyright file="SpeedHackCheckEventArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Event arguments for speed hack cheat checks.
/// </summary>
public class SpeedHackCheckEventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether a cheat was detected.
    /// </summary>
    public bool IsCheatDetected { get; set; }

    /// <summary>
    /// Gets the skill of an attack check, or <c>null</c> for a normal attack: the allowed time between two actions
    /// depends on it (plugin "Skill cast time").
    /// </summary>
    public DataModel.Configuration.Skill? Skill { get; init; }
}
