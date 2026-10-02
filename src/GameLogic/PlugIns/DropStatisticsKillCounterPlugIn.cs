// <copyright file="DropStatisticsKillCounterPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Counts the monster kills of the drop owner for the drop statistics (<c>/dropstats</c>).
/// It's called once per kill, independent of the other additional item drop plugins.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.DropStatisticsKillCounterPlugIn_Name), Description = nameof(PlugInResources.DropStatisticsKillCounterPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("9E4B7C21-5A3D-4F8E-B6C2-1D7A9E3F5B48")]
public class DropStatisticsKillCounterPlugIn : IAdditionalItemDropPlugIn
{
    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        if (!args.KilledObject.IsSummonedMonster
            && args.Monster.ObjectKind == NpcObjectKind.Monster
            && args.Killer.SelectedCharacter?.Name is { } characterName)
        {
            DropStatistics.AddKill(characterName);
        }

        return ValueTask.CompletedTask;
    }
}
