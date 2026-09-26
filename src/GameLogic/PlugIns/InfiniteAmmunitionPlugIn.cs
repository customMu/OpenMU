// <copyright file="InfiniteAmmunitionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Arrows and bolts are not consumed anymore by attacks and skills, like with the Infinity Arrow skill.
/// The ammunition still has to be equipped. When this plugin is deactivated, the ammunition is consumed as usual.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.InfiniteAmmunitionPlugIn_Name), Description = nameof(PlugInResources.InfiniteAmmunitionPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("E9B5F879-2A38-4618-85AC-246C010D4A75")]
public class InfiniteAmmunitionPlugIn : IFeaturePlugIn
{
}
