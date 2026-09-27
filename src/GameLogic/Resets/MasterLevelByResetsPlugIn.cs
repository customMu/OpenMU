// <copyright file="MasterLevelByResetsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Limits the master level by the reset count of the character: master experience is only gained
/// while the master level is below the maximum for the current reset count, see <see cref="MasterLevelByResetsConfiguration"/>.
/// When this plugin is deactivated, only the maximum master level of the game configuration applies.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.MasterLevelByResetsPlugIn_Name), Description = nameof(PlugInResources.MasterLevelByResetsPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("2559656C-95B5-4E63-9694-E6FB22E89AA2")]
public class MasterLevelByResetsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<MasterLevelByResetsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public MasterLevelByResetsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MasterLevelByResetsConfiguration();
}
