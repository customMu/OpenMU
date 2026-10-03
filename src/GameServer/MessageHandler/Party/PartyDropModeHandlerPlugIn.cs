// <copyright file="PartyDropModeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Party;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Party;
using MUnique.OpenMU.GameServer.RemoteView.Party;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for the custom packets of this server with the head code 0xFB, which the game client of this server sends:
/// <list type="bullet">
///   <item>Party drop mode: C1 05 FB 03 [mode], mode 0 = free, 1 = random, 2 = in turn.</item>
/// </list>
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartyDropModeHandlerPlugIn_Name), Description = nameof(PlugInResources.PartyDropModeHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("8C3F1A62-5D7E-4B19-A4E8-2F6B9D0C7E31")]
internal class PartyDropModeHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly PartyDropModeAction _action = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => false;

    /// <inheritdoc/>
    public byte Key => PartyDropModeViewPlugIn.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 5 || packet.Span[3] != PartyDropModeViewPlugIn.DropModeSubCode)
        {
            return;
        }

        await this._action.SetDropModeAsync(player, (PartyDropMode)packet.Span[4]).ConfigureAwait(false);
    }
}
