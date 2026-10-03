// <copyright file="PartyDropModeAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Party;

/// <summary>
/// Action to change the drop mode of the party, which only the party master may do.
/// </summary>
public class PartyDropModeAction
{
    /// <summary>
    /// Sets the drop mode of the party of the player.
    /// </summary>
    /// <param name="player">The player who requests the change.</param>
    /// <param name="mode">The new drop mode.</param>
    public async ValueTask SetDropModeAsync(Player player, PartyDropMode mode)
    {
        if (player.Party is not { } party)
        {
            return;
        }

        if (!Enum.IsDefined(mode))
        {
            player.Logger.LogWarning("{player} requested the unknown drop mode {mode}.", player, mode);
            return;
        }

        if (!await party.TrySetDropModeAsync(player, mode).ConfigureAwait(false))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PartyDropModeOnlyMaster)).ConfigureAwait(false);
            return;
        }

        var messageKey = mode switch
        {
            PartyDropMode.Random => nameof(PlayerMessage.PartyDropModeRandomFormat),
            PartyDropMode.RoundRobin => nameof(PlayerMessage.PartyDropModeRoundRobinFormat),
            _ => nameof(PlayerMessage.PartyDropModeFreeFormat),
        };
        foreach (var member in party.PartyList.OfType<Player>())
        {
            await member.ShowLocalizedBlueMessageAsync(messageKey, player.Name).ConfigureAwait(false);
        }
    }
}
