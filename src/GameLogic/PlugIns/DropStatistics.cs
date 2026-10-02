// <copyright file="DropStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Collections.Concurrent;

/// <summary>
/// In-memory drop statistics per character, for balance tests (<c>/dropstats</c>).
/// The values are not persisted and are lost on server restart.
/// </summary>
public static class DropStatistics
{
    private static readonly ConcurrentDictionary<string, CharacterDropStatistics> StatisticsByCharacter = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Counts a kill of the character.
    /// </summary>
    /// <param name="characterName">Name of the character.</param>
    public static void AddKill(string characterName)
    {
        Get(characterName).AddKill();
    }

    /// <summary>
    /// Counts an item which dropped by rank for the character.
    /// </summary>
    /// <param name="characterName">Name of the character.</param>
    /// <param name="rank">The rank of the item.</param>
    public static void AddRankItem(string characterName, int rank)
    {
        Get(characterName).AddRankItem(rank);
    }

    /// <summary>
    /// Counts a jewel which dropped for the character.
    /// </summary>
    /// <param name="characterName">Name of the character.</param>
    /// <param name="jewelName">Name of the jewel.</param>
    public static void AddJewel(string characterName, string jewelName)
    {
        Get(characterName).AddJewel(jewelName);
    }

    /// <summary>
    /// Gets a snapshot of the statistics of the character.
    /// </summary>
    /// <param name="characterName">Name of the character.</param>
    /// <returns>The snapshot.</returns>
    public static Snapshot GetSnapshot(string characterName)
    {
        return Get(characterName).GetSnapshot();
    }

    /// <summary>
    /// Clears the statistics of the character.
    /// </summary>
    /// <param name="characterName">Name of the character.</param>
    public static void Reset(string characterName)
    {
        StatisticsByCharacter.TryRemove(characterName, out _);
    }

    private static CharacterDropStatistics Get(string characterName)
    {
        return StatisticsByCharacter.GetOrAdd(characterName, _ => new CharacterDropStatistics());
    }

    /// <summary>
    /// A snapshot of the drop statistics of a character.
    /// </summary>
    public sealed class Snapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Snapshot"/> class.
        /// </summary>
        /// <param name="since">The time since the statistics are collected.</param>
        /// <param name="kills">The number of kills.</param>
        /// <param name="itemsByRank">The number of dropped items by rank.</param>
        /// <param name="jewels">The number of dropped jewels by name.</param>
        public Snapshot(DateTime since, long kills, IReadOnlyDictionary<int, long> itemsByRank, IReadOnlyDictionary<string, long> jewels)
        {
            this.Since = since;
            this.Kills = kills;
            this.ItemsByRank = itemsByRank;
            this.Jewels = jewels;
        }

        /// <summary>
        /// Gets the time (UTC) since the statistics are collected.
        /// </summary>
        public DateTime Since { get; }

        /// <summary>
        /// Gets the number of kills.
        /// </summary>
        public long Kills { get; }

        /// <summary>
        /// Gets the number of dropped items by rank.
        /// </summary>
        public IReadOnlyDictionary<int, long> ItemsByRank { get; }

        /// <summary>
        /// Gets the number of dropped jewels by name.
        /// </summary>
        public IReadOnlyDictionary<string, long> Jewels { get; }
    }

    private sealed class CharacterDropStatistics
    {
        private readonly object _syncRoot = new();
        private readonly Dictionary<int, long> _itemsByRank = new();
        private readonly Dictionary<string, long> _jewels = new();
        private readonly DateTime _since = DateTime.UtcNow;
        private long _kills;

        public void AddKill()
        {
            lock (this._syncRoot)
            {
                this._kills++;
            }
        }

        public void AddRankItem(int rank)
        {
            lock (this._syncRoot)
            {
                this._itemsByRank[rank] = this._itemsByRank.GetValueOrDefault(rank) + 1;
            }
        }

        public void AddJewel(string jewelName)
        {
            lock (this._syncRoot)
            {
                this._jewels[jewelName] = this._jewels.GetValueOrDefault(jewelName) + 1;
            }
        }

        public Snapshot GetSnapshot()
        {
            lock (this._syncRoot)
            {
                return new Snapshot(this._since, this._kills, new Dictionary<int, long>(this._itemsByRank), new Dictionary<string, long>(this._jewels));
            }
        }
    }
}
