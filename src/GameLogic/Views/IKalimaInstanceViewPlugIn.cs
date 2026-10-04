// <copyright file="IKalimaInstanceViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Interface of a view which shows the entry dialog of the Kalima instance: the tiers by resets and the entries left.
/// When the client has no such view, talking to the gatekeeper enters the instance at once.
/// Inside the instance, it shows the spots with living monsters on the map.
/// </summary>
public interface IKalimaInstanceViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the entry dialog.
    /// </summary>
    /// <param name="info">The information about the instance for the player.</param>
    ValueTask ShowEntryDialogAsync(KalimaInstanceInfo info);

    /// <summary>
    /// Shows the entry dialog of the chamber of Kundun: the levels by resets, the lost map which is needed and the entries left.
    /// </summary>
    /// <param name="info">The information about the chamber for the player.</param>
    /// <returns>The task.</returns>
    ValueTask ShowChamberEntryDialogAsync(KundunChamberInfo info);

    /// <summary>
    /// Shows the spots which still have living monsters: the ones of the current pack, or the one of the boss.
    /// </summary>
    /// <param name="spots">The spots; empty when no monster is left.</param>
    ValueTask ShowSpotsAsync(IReadOnlyList<Point> spots);

    /// <summary>
    /// Shows the progress of the instance: the killed packs and the state of the boss.
    /// </summary>
    /// <param name="clearedPacks">The number of killed packs.</param>
    /// <param name="packCount">The number of packs; 0 to hide the progress.</param>
    /// <param name="bossState">The state of the boss: 0 = not yet, 1 = alive, 2 = defeated.</param>
    /// <returns>The task.</returns>
    ValueTask ShowProgressAsync(int clearedPacks, int packCount, byte bossState);

    /// <summary>
    /// Closes everything outside of the arena on the map of the player, until the map changes.
    /// </summary>
    /// <param name="center">The center of the arena.</param>
    /// <param name="radius">The radius of the arena in fields.</param>
    /// <returns>The task.</returns>
    ValueTask ShowArenaAsync(Point center, float radius);
}
