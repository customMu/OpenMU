// <copyright file="IAdditionalItemDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called after the regular drop of a killed monster was generated.
/// It can add additional items which are dropped independently of the regular drop item groups.
/// </summary>
[Guid("5B0C2E47-3F55-4B8E-9A1D-6E2C4F7A9B31")]
[PlugInPoint("Additional item drops", "Plugins which add additional items to the drop of a killed monster, independently of the regular drop item groups.")]
public interface IAdditionalItemDropPlugIn
{
    /// <summary>
    /// Adds additional items to <see cref="AdditionalItemDropArgs.Items"/>.
    /// </summary>
    /// <param name="args">The arguments.</param>
    ValueTask AddItemDropsAsync(AdditionalItemDropArgs args);
}
