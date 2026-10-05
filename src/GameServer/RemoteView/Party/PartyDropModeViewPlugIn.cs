// <copyright file="PartyDropModeViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Party;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.Party;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the drop mode of the party with custom packets of this server (mode 0 = free, 1 = random, 2 = in turn):
/// <c>C1 05 FB 03 [mode]</c> - the drop mode of the party, or the chosen mode for new parties without a party;
/// the client sends the same packet to choose it (without a party) or to propose a change (vote in the party);
/// <c>C1 0F FB 0F [mode] [name of the initiator, 10 bytes]</c> - a member proposes a change, all must agree;
/// the client answers with <c>C1 05 FB 10 [0 = no, 1 = yes]</c>;
/// <c>C1 05 FB 11 [mode]</c> - the drop mode of the party to which the player is invited (before the invitation).
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartyDropModeViewPlugIn_Name), Description = nameof(PlugInResources.PartyDropModeViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("3E9D5B17-6A2C-4F80-B7D4-1C8E0A5F2B96")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class PartyDropModeViewPlugIn : IPartyDropModeViewPlugIn
{
    /// <summary>
    /// The head code of the custom packets of this server.
    /// </summary>
    public const byte Code = KundunEssenceViewPlugIn.Code;

    /// <summary>
    /// The sub code of the drop mode packet.
    /// </summary>
    public const byte DropModeSubCode = 0x03;

    /// <summary>
    /// The sub code of the proposal of a change of the drop mode.
    /// </summary>
    public const byte VoteSubCode = 0x0F;

    /// <summary>
    /// The sub code of the answer of the client to the proposal.
    /// </summary>
    public const byte VoteAnswerSubCode = 0x10;

    /// <summary>
    /// The sub code of the drop mode of the party to which the player is invited.
    /// </summary>
    public const byte InviteDropModeSubCode = 0x11;

    private const int NameLength = 10;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyDropModeViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public PartyDropModeViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public ValueTask ShowDropModeAsync(PartyDropMode mode) => this.SendModeAsync(DropModeSubCode, mode);

    /// <inheritdoc />
    public ValueTask ShowInviteDropModeAsync(PartyDropMode mode) => this.SendModeAsync(InviteDropModeSubCode, mode);

    /// <inheritdoc />
    public async ValueTask ShowDropModeVoteAsync(string initiatorName, PartyDropMode mode)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 5 + NameLength;
            var span = connection.Output.GetSpan(size)[..size];
            span.Clear();
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = VoteSubCode;
            span[4] = (byte)mode;
            var name = System.Text.Encoding.UTF8.GetBytes(initiatorName);
            name.AsSpan(0, Math.Min(name.Length, NameLength)).CopyTo(span[5..]);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private async ValueTask SendModeAsync(byte subCode, PartyDropMode mode)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 5;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = subCode;
            span[4] = (byte)mode;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
