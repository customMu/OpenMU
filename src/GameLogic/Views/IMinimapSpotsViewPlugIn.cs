// <copyright file="IMinimapSpotsViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// A monster spot for the minimap of the client.
/// </summary>
/// <param name="X">The x coordinate of the center.</param>
/// <param name="Y">The y coordinate of the center.</param>
/// <param name="Name">The name of the monster.</param>
/// <param name="Level">The level of the monster.</param>
/// <param name="Count">The number of monsters.</param>
public sealed record MinimapSpot(byte X, byte Y, string Name, int Level, int Count);

/// <summary>
/// Interface of a view which shows the monster spots of the current map on the minimap.
/// </summary>
public interface IMinimapSpotsViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the spots of the current map.
    /// </summary>
    /// <param name="spots">The spots.</param>
    ValueTask ShowSpotsAsync(IReadOnlyList<MinimapSpot> spots);
}
