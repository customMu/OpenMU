// <copyright file="PartyDropModeAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Party;

using MUnique.OpenMU.GameLogic.Views.Party;

/// <summary>
/// Action to choose the drop mode: without a party the mode of the parties which the player creates,
/// in a party a proposal of a change, to which all members must agree.
/// </summary>
public class PartyDropModeAction
{
    /// <summary>
    /// Chooses the drop mode (without a party) or proposes it to the party.
    /// </summary>
    /// <param name="player">The player who requests the change.</param>
    /// <param name="mode">The new drop mode.</param>
    public async ValueTask SetDropModeAsync(Player player, PartyDropMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            player.Logger.LogWarning("{player} requested the unknown drop mode {mode}.", player, mode);
            return;
        }

        if (player.Party is not { } party)
        {
            player.PreferredPartyDropMode = mode;
            await player.InvokeViewPlugInAsync<IPartyDropModeViewPlugIn>(p => p.ShowDropModeAsync(mode)).ConfigureAwait(false);
            return;
        }

        await party.StartDropModeVoteAsync(player, mode).ConfigureAwait(false);
    }
}
