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
/// Sends the drop mode of the party with a custom packet of this server: C1 05 FB 03 [mode],
/// mode 0 = free, 1 = random, 2 = in turn. The game client of this server sends the same packet to change it.
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

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyDropModeViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public PartyDropModeViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowDropModeAsync(PartyDropMode mode)
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
            span[3] = DropModeSubCode;
            span[4] = (byte)mode;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
