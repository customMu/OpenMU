// <copyright file="WarpResetRequirementsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Requires a number of resets for single entries of the warp list (e.g. the later floors of Lost Tower
/// or Aida 2), in addition to the reset requirement of the whole map. The maps stay open: a player can
/// still walk into a later part of a map, the strong monsters there are the limit.
/// The game client shows the same values in the warp list (GameLogic/Travel/TravelRequirements.h).
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.WarpResetRequirementsPlugIn_Name), Description = nameof(PlugInResources.WarpResetRequirementsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A4F0C2E7-6B1D-4E93-8C5A-2D7F9B3E1C68")]
public class WarpResetRequirementsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<WarpResetRequirementsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public WarpResetRequirementsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new WarpResetRequirementsConfiguration();

    /// <summary>
    /// Gets the required resets of the warp list entry.
    /// </summary>
    /// <param name="warpIndex">The index of the warp list entry.</param>
    /// <returns>The required resets; 0 if the entry has no requirement.</returns>
    public int GetRequiredResets(int warpIndex)
    {
        var configuration = this.Configuration ??= (WarpResetRequirementsConfiguration)this.CreateDefaultConfig();
        return configuration.Warps.FirstOrDefault(w => w.WarpIndex == warpIndex)?.MinimumResets ?? 0;
    }
}
