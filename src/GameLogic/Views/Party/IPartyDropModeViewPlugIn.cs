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

    /// <summary>
    /// Shows the drop mode of the party to which the player is invited (sent before the invitation).
    /// </summary>
    /// <param name="mode">The drop mode of the party of the inviting player, or the one it chose for a new party.</param>
    ValueTask ShowInviteDropModeAsync(PartyDropMode mode);

    /// <summary>
    /// Asks the player to agree to the change of the drop mode of the party.
    /// </summary>
    /// <param name="initiatorName">The name of the member who proposed the change.</param>
    /// <param name="mode">The proposed drop mode.</param>
    ValueTask ShowDropModeVoteAsync(string initiatorName, PartyDropMode mode);
}
