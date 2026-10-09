// <copyright file="IllusionOfNoriaViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.MiniGames;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the dialog of the warden of the Illusion of Noria on the custom client:
/// C2 [size] FB 12 [in illusion] [quest state] [has whistle] [resets u16] [required resets u16] [daily state] [count]
/// [kills needed u16] [seconds until the next day u32], then count x ([monster number u16] [kills u16]), then [weapons],
/// weapons x ([slot] [group] [number u16] [option] [option level] [rank] [jewels] [lesser stones] [greater stones] [can pay]),
/// then [shards u32] [exchange count], exchange x ([group] [number u16] [price u16]); little endian.
/// The client answers with C1 05 FB 13 [action] (see IllusionOfNoriaPlugIn.Action*). FB 14 is the same state without
/// the dialog (an answer to the action 0x30 and after a kill of the daily quest).
/// </summary>
[PlugIn]
[Display(Name = "Illusion of Noria view", Description = "Shows the dialog of the warden of the Illusion of Noria on the custom client.")]
[Guid("D8A35F21-4C7B-4E96-B0D2-7F1E9A6C3B58")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class IllusionOfNoriaViewPlugIn : IIllusionOfNoriaViewPlugIn
{
    /// <summary>
    /// The sub code of the dialog of the warden.
    /// </summary>
    public const byte WardenDialogSubCode = 0x12;

    /// <summary>
    /// The sub code of an action of the dialog, sent by the client.
    /// </summary>
    public const byte WardenActionSubCode = 0x13;

    /// <summary>
    /// The sub code of the state of the warden without the dialog (the same data as <see cref="WardenDialogSubCode"/>).
    /// </summary>
    public const byte WardenStateSubCode = 0x14;

    /// <summary>
    /// The sub code of the remaining seconds of the Golden Curse: C1 05 FB 15 [seconds].
    /// </summary>
    public const byte CurseTimeSubCode = 0x15;

    /// <summary>
    /// The sub code of the remaining seconds of the Veil Ward: C1 06 FB 16 [seconds, 2 bytes little endian].
    /// </summary>
    public const byte WardTimeSubCode = 0x16;

    /// <summary>
    /// The sub code of the remaining seconds of the Blessing of the Veil: C1 06 FB 17 [seconds, 2 bytes little endian].
    /// </summary>
    public const byte BlessingTimeSubCode = 0x17;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="IllusionOfNoriaViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public IllusionOfNoriaViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public ValueTask ShowWardenDialogAsync(IllusionWardenInfo info) => this.SendAsync(info, WardenDialogSubCode);

    /// <inheritdoc />
    public ValueTask UpdateWardenStateAsync(IllusionWardenInfo info) => this.SendAsync(info, WardenStateSubCode);

    /// <inheritdoc />
    public async ValueTask ShowWardTimeAsync(TimeSpan remaining)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var seconds = (ushort)Math.Clamp((int)Math.Ceiling(remaining.TotalSeconds), 0, ushort.MaxValue);

        int Write()
        {
            var span = connection.Output.GetSpan(6)[..6];
            span[0] = 0xC1;
            span[1] = 6;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = WardTimeSubCode;
            span[4] = (byte)seconds;
            span[5] = (byte)(seconds >> 8);
            return 6;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowBlessingTimeAsync(TimeSpan remaining)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var seconds = (ushort)Math.Clamp((int)Math.Ceiling(remaining.TotalSeconds), 0, ushort.MaxValue);

        int Write()
        {
            var span = connection.Output.GetSpan(6)[..6];
            span[0] = 0xC1;
            span[1] = 6;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = BlessingTimeSubCode;
            span[4] = (byte)seconds;
            span[5] = (byte)(seconds >> 8);
            return 6;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowCurseTimeAsync(TimeSpan remaining)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var seconds = (byte)Math.Clamp((int)Math.Ceiling(remaining.TotalSeconds), 0, byte.MaxValue);

        int Write()
        {
            var span = connection.Output.GetSpan(5)[..5];
            span[0] = 0xC1;
            span[1] = 5;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = CurseTimeSubCode;
            span[4] = seconds;
            return 5;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private async ValueTask SendAsync(IllusionWardenInfo info, byte subCode)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(3, Math.Min(info.DailyMonsters.Count, info.DailyKills.Count));
        var weapons = Math.Min(2, info.Weapons.Count);
        var exchange = Math.Min(32, info.Exchange.Count);

        int Write()
        {
            const int headerSize = 20;
            const int weaponSize = 11;
            const int exchangeSize = 5;
            var size = headerSize + (count * 4) + 1 + (weapons * weaponSize) + 5 + (exchange * exchangeSize);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC2;
            BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)size);
            span[3] = Inventory.KundunEssenceViewPlugIn.Code;
            span[4] = subCode;
            span[5] = info.InIllusion ? (byte)1 : (byte)0;
            span[6] = info.QuestState;
            span[7] = info.HasWhistle ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt16LittleEndian(span[8..], (ushort)Math.Clamp(info.Resets, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(span[10..], (ushort)Math.Clamp(info.RequiredResets, 0, ushort.MaxValue));
            span[12] = info.DailyState;
            span[13] = (byte)count;
            BinaryPrimitives.WriteUInt16LittleEndian(span[14..], (ushort)Math.Clamp(info.DailyKillsNeeded, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt32LittleEndian(span[16..], (uint)Math.Max(0, info.TimeUntilNextDay.TotalSeconds));
            for (var i = 0; i < count; i++)
            {
                var entry = span.Slice(headerSize + (i * 4), 4);
                BinaryPrimitives.WriteUInt16LittleEndian(entry, (ushort)info.DailyMonsters[i]);
                BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], (ushort)Math.Clamp(info.DailyKills[i], 0, ushort.MaxValue));
            }

            var offset = headerSize + (count * 4);
            span[offset] = (byte)weapons;
            for (var i = 0; i < weapons; i++)
            {
                var weapon = info.Weapons[i];
                var entry = span.Slice(offset + 1 + (i * weaponSize), weaponSize);
                entry[0] = weapon.Slot;
                entry[1] = weapon.Group;
                BinaryPrimitives.WriteUInt16LittleEndian(entry[2..], (ushort)weapon.Number);
                entry[4] = weapon.OptionNumber;
                entry[5] = weapon.OptionLevel;
                entry[6] = weapon.Rank;
                entry[7] = weapon.JewelsOfIllusion;
                entry[8] = weapon.LesserStones;
                entry[9] = weapon.GreaterStones;
                entry[10] = weapon.CanPay ? (byte)1 : (byte)0;
            }

            offset += 1 + (weapons * weaponSize);
            BinaryPrimitives.WriteUInt32LittleEndian(span[offset..], (uint)Math.Max(0, info.Shards));
            span[offset + 4] = (byte)exchange;
            for (var i = 0; i < exchange; i++)
            {
                var (group, number, price) = info.Exchange[i];
                var entry = span.Slice(offset + 5 + (i * exchangeSize), exchangeSize);
                entry[0] = group;
                BinaryPrimitives.WriteUInt16LittleEndian(entry[1..], (ushort)number);
                BinaryPrimitives.WriteUInt16LittleEndian(entry[3..], (ushort)Math.Clamp(price, 0, ushort.MaxValue));
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
