// <copyright file="IPartyDropModeViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Party;

/// <summary>
/// Interface of a view which shows the drop mode of the party.
/// </summary>
public interface IPartyDropModeViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the drop mode of the party.
    /// </summary>
    /// <param name="mode">The drop mode.</param>
    ValueTask ShowDropModeAsync(PartyDropMode mode);
}
