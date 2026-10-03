// <copyright file="PartyDropMode.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Defines who of a party may pick up the items which dropped for a kill of a party member.
/// The party master chooses the mode.
/// </summary>
public enum PartyDropMode : byte
{
    /// <summary>
    /// Every member of the party may pick up the items (who comes first).
    /// </summary>
    Free = 0,

    /// <summary>
    /// Each item belongs to a random member near the killed monster.
    /// </summary>
    Random = 1,

    /// <summary>
    /// The items belong to the members near the killed monster in turn.
    /// </summary>
    RoundRobin = 2,
}
