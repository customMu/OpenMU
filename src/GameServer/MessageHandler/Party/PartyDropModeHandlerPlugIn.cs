// <copyright file="PartyDropModeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Party;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;
using MUnique.OpenMU.GameServer.RemoteView.MiniGames;
using MUnique.OpenMU.GameServer.RemoteView.Party;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for the custom packets of this server with the head code 0xFB, which the game client of this server sends:
/// <list type="bullet">
///   <item>Party drop mode: C1 05 FB 03 [mode], mode 0 = free, 1 = random, 2 = in turn.</item>
///   <item>Fingerprint of the computer: C1 14 FB 20 [16 bytes] (for the rankings).</item>
/// </list>
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartyDropModeHandlerPlugIn_Name), Description = nameof(PlugInResources.PartyDropModeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("8C3F1A62-5D7E-4B19-A4E8-2F6B9D0C7E31")]
internal class PartyDropModeHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <summary>
    /// The sub code of the fingerprint of the computer of the player (C1 14 FB 20 [16 bytes]).
    /// </summary>
    internal const byte HardwareIdSubCode = 0x20;

    private readonly PartyDropModeAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => PartyDropModeViewPlugIn.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        // All custom packets of the head code 0xFB come here, as there is one handler per head code.
        if (packet.Length >= 20 && packet.Span[3] == HardwareIdSubCode)
        {
            var id = Convert.ToHexString(packet.Span.Slice(4, 16));
            if (player.HardwareId != id)
            {
                player.HardwareId = id;
                player.Logger.LogInformation("Hardware id of {account}: {id}", player.Account?.LoginName, id);
            }

            return;
        }

        if (packet.Length >= 4 && packet.Span[3] == KalimaInstanceViewPlugIn.EnterRequestSubCode)
        {
            if (player.GameContext.FeaturePlugIns.GetPlugIn<KalimaInstancePlugIn>() is { } kalima)
            {
                await kalima.EnterFromDialogAsync(player).ConfigureAwait(false);
            }

            return;
        }

        if (packet.Length >= 4 && packet.Span[3] == KalimaInstanceViewPlugIn.ChamberEnterRequestSubCode)
        {
            if (player.GameContext.FeaturePlugIns.GetPlugIn<KundunChamberPlugIn>() is { } chamber)
            {
                await chamber.EnterFromDialogAsync(player).ConfigureAwait(false);
            }

            return;
        }

        if (packet.Length >= 4 && packet.Span[3] == RemoteView.Quest.KillQuestViewPlugIn.ClaimRequestSubCode)
        {
            if (player.GameContext.FeaturePlugIns.GetPlugIn<GameLogic.PlugIns.KillQuests.KillQuestsPlugIn>() is { } quests)
            {
                await quests.ClaimRewardAsync(player).ConfigureAwait(false);
            }

            return;
        }

        if (packet.Length >= 5 && packet.Span[3] == PartyDropModeViewPlugIn.VoteAnswerSubCode)
        {
            if (player.Party is { } party)
            {
                await party.AnswerDropModeVoteAsync(player, packet.Span[4] != 0).ConfigureAwait(false);
            }

            return;
        }

        if (packet.Length < 5 || packet.Span[3] != PartyDropModeViewPlugIn.DropModeSubCode)
        {
            return;
        }

        await this._action.SetDropModeAsync(player, (PartyDropMode)packet.Span[4]).ConfigureAwait(false);
    }
}
