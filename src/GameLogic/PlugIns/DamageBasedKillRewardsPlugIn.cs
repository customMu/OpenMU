// <copyright file="DamageBasedKillRewardsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Distributes the rewards of a killed monster by the damage which was dealt to it, instead of giving
/// everything to the player who landed the last hit:
/// The damage of a party counts as the damage of one group. The experience is split between the groups
/// in proportion to their damage (inside a party, the party experience rules apply to its part).
/// The group with the highest damage gets the item and money drop.
/// When this plugin is deactivated, the player with the last hit (and his party) gets everything.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.DamageBasedKillRewardsPlugIn_Name), Description = nameof(PlugInResources.DamageBasedKillRewardsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("57B189F6-FF0D-4669-8622-7A755EACF0BC")]
public class DamageBasedKillRewardsPlugIn : IFeaturePlugIn
{
}
