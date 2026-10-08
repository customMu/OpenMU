// <copyright file="MuHelperConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MuHelper;

/// <summary>
/// Configuration for the <see cref="MuHelper"/>.
/// </summary>
public class MuHelperConfiguration
{
    /// <summary>
    /// Gets or sets the minimum character level.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MuHelperConfiguration_MinLevel_Name))]
    public int MinLevel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum character level.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MuHelperConfiguration_MaxLevel_Name))]
    public int MaxLevel { get; set; } = 400;
}