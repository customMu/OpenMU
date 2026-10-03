// <copyright file="KundunEssenceViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the Kundun Essence currency and the prices of the essence shop with custom packets of this server:
/// <list type="bullet">
///   <item>Balance: C1 08 FB 01, followed by the balance (uint32, little endian).</item>
///   <item>Shop prices: C2 [length, big endian] FB 02 [count], followed by count entries of the store slot (byte) and the price (uint32, little endian). Count 0 means the opened shop sells for zen.</item>
/// </list>
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.KundunEssenceViewPlugIn_Name), Description = nameof(PlugInResources.KundunEssenceViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("E4A17C52-8D3B-4F96-B1E0-7C2A5D9F4B83")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public class KundunEssenceViewPlugIn : IKundunEssenceViewPlugIn
{
    /// <summary>
    /// The head code of the custom packets of the Kalima features of this server.
    /// </summary>
    public const byte Code = 0xFB;

    /// <summary>
    /// The sub code of the balance packet.
    /// </summary>
    public const byte BalanceSubCode = 0x01;

    /// <summary>
    /// The sub code of the shop prices packet.
    /// </summary>
    public const byte ShopPricesSubCode = 0x02;

    private const int MaximumShopEntries = 120;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="KundunEssenceViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public KundunEssenceViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowBalanceAsync(int balance)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        int Write()
        {
            const int size = 8;
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC1;
            span[1] = size;
            span[2] = Code;
            span[3] = BalanceSubCode;
            BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)Math.Max(0, balance));
            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask ShowShopPricesAsync(IReadOnlyList<(byte Slot, int Price)> prices)
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var count = Math.Min(prices.Count, MaximumShopEntries);

        int Write()
        {
            var size = 6 + (count * 5);
            var span = connection.Output.GetSpan(size)[..size];
            span[0] = 0xC2;
            BinaryPrimitives.WriteUInt16BigEndian(span[1..], (ushort)size);
            span[3] = Code;
            span[4] = ShopPricesSubCode;
            span[5] = (byte)count;
            for (var i = 0; i < count; i++)
            {
                var entry = span.Slice(6 + (i * 5), 5);
                entry[0] = prices[i].Slot;
                BinaryPrimitives.WriteUInt32LittleEndian(entry[1..], (uint)Math.Max(0, prices[i].Price));
            }

            return size;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }
}
