// <copyright file="PartyExperienceFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes the experience distribution of parties: every receiving member gets his own solo experience
/// for the kill (own level, own penalty for too weak monsters, own rates) multiplied by a share which
/// depends on the number of receiving members. The levels of the other members don't matter.
/// When this plugin is deactivated, the default level based distribution is used.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartyExperienceFeaturePlugIn_Name), Description = nameof(PlugInResources.PartyExperienceFeaturePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("65A05907-3D74-4954-8369-EF37F7628590")]
public class PartyExperienceFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<PartyExperienceConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public PartyExperienceConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PartyExperienceConfiguration();
}
