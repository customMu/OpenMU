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
/// The client answers with C1 05 FB 13 [action] (see IllusionOfNoriaPlugIn.Action*); a purchase is C1 08 FB 13 20 [group] [number u16].
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

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="IllusionOfNoriaViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public IllusionOfNoriaViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowWardenDialogAsync(IllusionWardenInfo info)
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
            span[4] = WardenDialogSubCode;
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
