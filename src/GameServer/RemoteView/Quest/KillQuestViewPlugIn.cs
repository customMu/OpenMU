// <copyright file="KillQuestViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Quest;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MUnique.OpenMU.GameLogic.PlugIns.KillQuests;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the state of the kill quests to the quest window of the custom client (key T), as custom packet
/// <c>C1 245 FB 0D [quest number u16 LE, from 1; quest count + 1 when all are done] [quest count u16 LE] [kills u32 LE]
/// [kills needed u32 LE] [stat points of the reward u16 LE] [reward waiting: 0/1] [stat points from quests u16 LE]
/// [monster name, 32 bytes UTF-8] [reward text, 192 bytes UTF-8, one item per line]</c>.
/// The client takes a waiting reward with <c>C1 04 FB 0E</c>.
/// </summary>
[PlugIn]
[Display(Name = "Kill quest view", Description = "Shows the current kill quest in the quest window of the custom client.")]
[Guid("6A1D9E47-3B82-4C5F-8E06-D4B7A2C9F135")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class KillQuestViewPlugIn : IKillQuestViewPlugIn
{
    /// <summary>
    /// The sub code of the state of the kill quests.
    /// </summary>
    public const byte StateSubCode = 0x0D;

    /// <summary>
    /// The sub code of the request of the client to take a waiting reward.
    /// </summary>
    public const byte ClaimRequestSubCode = 0x0E;

    private const int Size = 245;
    private const int NameLength = 32;
    private const int RewardLength = 192;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="KillQuestViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public KillQuestViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowKillQuestAsync(KillQuestState state)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            var span = connection.Output.GetSpan(Size)[..Size];
            span.Clear();
            span[0] = 0xC1;
            span[1] = Size;
            span[2] = Inventory.KundunEssenceViewPlugIn.Code;
            span[3] = StateSubCode;
            BinaryPrimitives.WriteUInt16LittleEndian(span[4..], (ushort)Math.Clamp(state.Number, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(span[6..], (ushort)Math.Clamp(state.Count, 0, ushort.MaxValue));
            BinaryPrimitives.WriteUInt32LittleEndian(span[8..], (uint)Math.Max(0, state.Kills));
            BinaryPrimitives.WriteUInt32LittleEndian(span[12..], (uint)Math.Max(0, state.KillsNeeded));
            BinaryPrimitives.WriteUInt16LittleEndian(span[16..], (ushort)Math.Clamp(state.RewardPoints, 0, ushort.MaxValue));
            span[18] = state.RewardWaiting ? (byte)1 : (byte)0;
            BinaryPrimitives.WriteUInt16LittleEndian(span[19..], (ushort)Math.Clamp(state.QuestPoints, 0, ushort.MaxValue));
            WriteText(span.Slice(21, NameLength), state.MonsterName);
            WriteText(span.Slice(21 + NameLength, RewardLength), state.RewardText);
            return Size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    private static void WriteText(Span<byte> target, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        bytes.AsSpan(0, Math.Min(bytes.Length, target.Length - 1)).CopyTo(target);
    }
}
