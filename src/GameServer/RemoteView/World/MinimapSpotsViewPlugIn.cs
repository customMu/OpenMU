// <copyright file="MinimapSpotsViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.World;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the monster spots of the map for the minimap (custom packet <c>C2 [size] FB 06</c>):
/// <c>[count u16 LE] count × {[x] [y] [level u16 LE] [monster count] [flags: 1 = boss] [name length] [name UTF-8]}</c>.
/// </summary>
[PlugIn]
[Display(Name = "Minimap spots view", Description = "Sends the monster spots of the map to the minimap of the custom client.")]
[Guid("5E1B8D27-9C43-4A6F-B0D2-7A3E6C9F1B85")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class MinimapSpotsViewPlugIn : IMinimapSpotsViewPlugIn
{
    /// <summary>
    /// The sub code of the packet.
    /// </summary>
    public const byte SpotsSubCode = 0x06;

    private const int MaximumNameLength = 32;

    private const int MaximumPacketSize = 0xFFFF;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinimapSpotsViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public MinimapSpotsViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowSpotsAsync(IReadOnlyList<MinimapSpot> spots)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var entries = new List<(MinimapSpot Spot, byte[] Name)>();
        const int entryHeader = 7;   // x, y, level (u16), monsters, flags (1 = boss), name length
        var size = 7;
        foreach (var spot in spots)
        {
            var name = Encoding.UTF8.GetBytes(spot.Name);
            if (name.Length > MaximumNameLength)
            {
                name = name[..MaximumNameLength];
            }

            if (size + entryHeader + name.Length > MaximumPacketSize)
            {
                break;
            }

            entries.Add((spot, name));
            size += entryHeader + name.Length;
        }

        int Write()
        {
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC2;
            BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)size);
            span[3] = Inventory.KundunEssenceViewPlugIn.Code;
            span[4] = SpotsSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[5..], (ushort)entries.Count);
            var offset = 7;
            foreach (var (spot, name) in entries)
            {
                span[offset] = spot.X;
                span[offset + 1] = spot.Y;
                BinaryPrimitives.WriteUInt16LittleEndian(span[(offset + 2)..], (ushort)Math.Clamp(spot.Level, 0, ushort.MaxValue));
                span[offset + 4] = (byte)Math.Clamp(spot.Count, 0, byte.MaxValue);
                span[offset + 5] = spot.IsBoss ? (byte)1 : (byte)0;
                span[offset + 6] = (byte)name.Length;
                name.CopyTo(span[(offset + entryHeader)..]);
                offset += entryHeader + name.Length;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
