// <copyright file="ItemRequirementsByBaseStatsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the item requirements:
/// - The stat requirements (strength, agility, vitality, energy, leadership) are checked against the base stats
///   of the character (the distributed points), not against the total stats including the bonuses of items.
/// - The level requirement of wearable items (equipment, wings, pets) is ignored for characters with at least
///   one reset, because they already reached the reset level once.
/// - An equipped item gives no bonuses (and doesn't count for set bonuses) while its requirements are not met,
///   e.g. after a reset which reset the stats. It stays equipped and gives its bonuses again as soon as the
///   requirements are met again.
/// When this plugin is deactivated, the original behavior applies.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ItemRequirementsByBaseStatsPlugIn_Name), Description = nameof(PlugInResources.ItemRequirementsByBaseStatsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("44F39DA4-1B1E-41AB-9414-B8961DE57326")]
public class ItemRequirementsByBaseStatsPlugIn : IFeaturePlugIn
{
}
