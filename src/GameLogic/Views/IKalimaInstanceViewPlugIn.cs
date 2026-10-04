// <copyright file="IKalimaInstanceViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

using MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// Interface of a view which shows the entry dialog of the Kalima instance: the tiers by resets and the entries left.
/// When the client has no such view, talking to the gatekeeper enters the instance at once.
/// </summary>
public interface IKalimaInstanceViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the entry dialog.
    /// </summary>
    /// <param name="info">The information about the instance for the player.</param>
    ValueTask ShowEntryDialogAsync(KalimaInstanceInfo info);
}
