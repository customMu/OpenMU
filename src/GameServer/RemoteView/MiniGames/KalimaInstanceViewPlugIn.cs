// <copyright file="KalimaInstanceViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.MiniGames;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the entry dialog of the Kalima instance (custom packet <c>C2 [size] FB 04</c>):
/// <c>[resets u16 LE] [tier level] [entries left] [entries per day] [seconds until reset u32 LE] [can re-enter]
/// [tier count] tier count × {[level] [minimum resets u16 LE] [maximum resets u16 LE, 0xFFFF = open]}</c>.
/// The client answers with <c>C1 04 FB 05</c> to enter.
/// </summary>
[PlugIn]
[Display(Name = "Kalima instance view", Description = "Shows the entry dialog of the Kalima instance on the custom client.")]
[Guid("2B7E4C19-6A3F-4D85-9E21-C8F0A5D3B746")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class KalimaInstanceViewPlugIn : IKalimaInstanceViewPlugIn
{
    /// <summary>
    /// The sub code of the entry dialog.
    /// </summary>
    public const byte EntryDialogSubCode = 0x04;

    /// <summary>
    /// The sub code of the request to enter, sent by the client.
    /// </summary>
    public const byte EnterRequestSubCode = 0x05;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaInstanceViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public KalimaInstanceViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowEntryDialogAsync(KalimaInstanceInfo info)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(info.Tiers.Count, 20);

        int Write()
        {
            const int headerSize = 17;
            var size = headerSize + (count * 5);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC2;
            BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)size);
            span[3] = Inventory.KundunEssenceViewPlugIn.Code;
            span[4] = EntryDialogSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[5..], (ushort)Math.Clamp(info.Resets, 0, ushort.MaxValue));
            span[7] = info.TierLevel;
            span[8] = (byte)Math.Clamp(info.EntriesLeft, 0, byte.MaxValue);
            span[9] = (byte)Math.Clamp(info.EntriesPerDay, 0, byte.MaxValue);
            BinaryPrimitives.WriteUInt32LittleEndian(span[10..], (uint)Math.Max(0, info.TimeUntilReset.TotalSeconds));
            span[14] = info.CanReenter ? (byte)1 : (byte)0;
            span[15] = (byte)count;
            span[16] = 0;
            for (var i = 0; i < count; i++)
            {
                var (level, minimum, maximum) = info.Tiers[i];
                var entry = span.Slice(headerSize + (i * 5), 5);
                entry[0] = level;
                BinaryPrimitives.WriteUInt16LittleEndian(entry[1..], (ushort)Math.Clamp(minimum, 0, ushort.MaxValue - 1));
                BinaryPrimitives.WriteUInt16LittleEndian(entry[3..], maximum is { } max ? (ushort)Math.Clamp(max, 0, ushort.MaxValue - 1) : ushort.MaxValue);
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
