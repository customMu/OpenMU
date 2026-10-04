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
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the entry dialog of the Kalima instance (custom packet <c>C2 [size] FB 04</c>):
/// <c>[resets u16 LE] [tier level] [entries left] [entries per day] [seconds until reset u32 LE] [can re-enter]
/// [tier count] tier count × {[level] [minimum resets u16 LE] [maximum resets u16 LE, 0xFFFF = open]}</c>.
/// The client answers with <c>C1 04 FB 05</c> to enter.
/// Inside the instance, the spots with living monsters (custom packet <c>C1 [size] FB 07 [count] count × {[x] [y]}</c>)
/// and the progress (<c>C1 07 FB 08 [killed packs] [packs] [boss: 0 = not yet, 1 = alive, 2 = defeated]</c>).
/// In the chamber of Kundun, the closed arena (<c>C1 07 FB 09 [x] [y] [radius in half fields]</c>).
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

    /// <summary>
    /// The sub code of the spots with living monsters (0x06 is taken by the monster spots of the map, <see cref="World.MinimapSpotsViewPlugIn"/>).
    /// </summary>
    public const byte SpotsSubCode = 0x07;

    /// <summary>
    /// The sub code of the progress of the instance (killed packs and the state of the boss).
    /// </summary>
    public const byte ProgressSubCode = 0x08;

    /// <summary>
    /// The sub code of the closed arena (chamber of Kundun).
    /// </summary>
    public const byte ArenaSubCode = 0x09;

    /// <summary>
    /// The sub code of the entry dialog of the chamber of Kundun.
    /// </summary>
    public const byte ChamberEntryDialogSubCode = 0x0A;

    /// <summary>
    /// The sub code of the request to enter the chamber of Kundun, sent by the client.
    /// </summary>
    public const byte ChamberEnterRequestSubCode = 0x0B;

    /// <summary>
    /// The sub code of the state of the fight in the chamber of Kundun.
    /// </summary>
    public const byte ChamberStatusSubCode = 0x0C;

    /// <summary>
    /// The maximum number of spots which fit into a C1 packet.
    /// </summary>
    private const int MaximumSpots = (byte.MaxValue - 5) / 2;

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

    /// <inheritdoc />
    public async ValueTask ShowSpotsAsync(IReadOnlyList<Point> spots)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(spots.Count, MaximumSpots);

        int Write()
        {
            var size = 5 + (count * 2);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = (byte)size;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = SpotsSubCode;
            span[4] = (byte)count;
            for (var i = 0; i < count; i++)
            {
                span[5 + (i * 2)] = spots[i].X;
                span[6 + (i * 2)] = spots[i].Y;
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <c>C2 [size] FB 0A [resets u16 LE] [level] [entries left] [entries per week] [seconds until reset u32 LE] [can re-enter]
    /// [has lost map] [tier count] [0] tier count x {[level] [minimum resets u16 LE] [maximum resets u16 LE, 0xFFFF = open]}</c>;
    /// the client answers with <c>C1 04 FB 0B</c> to enter.
    /// </remarks>
    public async ValueTask ShowChamberEntryDialogAsync(KundunChamberInfo info)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(info.Tiers.Count, 20);

        int Write()
        {
            const int headerSize = 18;
            var size = headerSize + (count * 5);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC2;
            BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)size);
            span[3] = Inventory.KundunEssenceViewPlugIn.Code;
            span[4] = ChamberEntryDialogSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[5..], (ushort)Math.Clamp(info.Resets, 0, ushort.MaxValue));
            span[7] = info.TierLevel;
            span[8] = (byte)Math.Clamp(info.EntriesLeft, 0, byte.MaxValue);
            span[9] = (byte)Math.Clamp(info.EntriesPerWeek, 0, byte.MaxValue);
            BinaryPrimitives.WriteUInt32LittleEndian(span[10..], (uint)Math.Max(0, info.TimeUntilReset.TotalSeconds));
            span[14] = info.CanReenter ? (byte)1 : (byte)0;
            span[15] = info.HasLostMap ? (byte)1 : (byte)0;
            span[16] = (byte)count;
            span[17] = 0;
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

    /// <inheritdoc />
    public async ValueTask ShowArenaAsync(Point center, float radius)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 7;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = ArenaSubCode;
            span[4] = center.X;
            span[5] = center.Y;
            span[6] = (byte)Math.Clamp((int)Math.Round(radius * 2), 0, byte.MaxValue);
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowProgressAsync(int clearedPacks, int packCount, byte bossState)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 7;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = ProgressSubCode;
            span[4] = (byte)Math.Clamp(clearedPacks, 0, byte.MaxValue);
            span[5] = (byte)Math.Clamp(packCount, 0, byte.MaxValue);
            span[6] = bossState;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowChamberStatusAsync(KundunChamberStatus status)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 15;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = ChamberStatusSubCode;
            span[4] = status.Level;
            span[5] = status.Phase;
            span[6] = status.PhaseCount;
            span[7] = status.HealthPercent;
            span[8] = status.Illusions;
            span[9] = (byte)((status.IsShielded ? 1 : 0) | (status.IsDefeated ? 2 : 0));
            span[10] = (byte)(status.SecondsLeft & 0xFF);
            span[11] = (byte)(status.SecondsLeft >> 8);
            span[12] = status.LastHealPercent;
            span[13] = status.LastDefensePercent;
            span[14] = status.LastDamagePercent;
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
