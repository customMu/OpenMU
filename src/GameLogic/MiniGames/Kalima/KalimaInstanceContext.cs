// <copyright file="KalimaInstanceContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// The context of a Kalima instance for a party or a single player. The monsters come in packs
/// along the way from the entrance; the next pack appears when the previous one is killed, and
/// the Illusion of Kundun after the last pack. The monsters have the fixed strength of the tier.
/// Players can leave and enter again (e.g. after a death) as long as the instance is running.
/// The spots which still have living monsters are shown on the map of the players.
/// </summary>
public sealed class KalimaInstanceContext : KalimaRunContextBase
{
    private const int PackSpawnSpread = 2;

    private readonly KalimaInstanceConfiguration _configuration;

    /// <summary>
    /// The living monsters of the current pack with their spots.
    /// </summary>
    private readonly Dictionary<AttackableNpcBase, Point> _alivePackMonsters = new();

    private readonly IReadOnlyList<IReadOnlyList<MonsterSpawnArea>> _packs;

    private int _currentPack = -1;
    private bool _bossSpawned;
    private Point? _bossSpot;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaInstanceContext"/> class.
    /// </summary>
    /// <param name="key">The key of this context.</param>
    /// <param name="definition">The definition of the mini game.</param>
    /// <param name="gameContext">The game context, to which this game belongs.</param>
    /// <param name="mapInitializer">The map initializer, which is used when the event starts.</param>
    /// <param name="configuration">The configuration of the Kalima instance.</param>
    public KalimaInstanceContext(MiniGameMapKey key, MiniGameDefinition definition, IGameContext gameContext, IMapInitializer mapInitializer, KalimaInstanceConfiguration configuration)
        : base(key, definition, gameContext, mapInitializer, configuration, configuration.Duration, configuration.CloseWhenEmptyAfter)
    {
        this._configuration = configuration;
        this._packs = KalimaPackPlanner.PlanPacks(this.RegularSpawns.ToList(), GetEntrancePoint(definition), this.Map.Terrain.WalkMap, configuration.PackCount);
    }

    /// <summary>
    /// Gets the experience multiplier of the instance.
    /// </summary>
    public float ExperienceMultiplier => this._configuration.ExperienceMultiplier;

    /// <inheritdoc />
    public override double ItemDropMultiplier => this.Tier.DropMultiplier;

    /// <inheritdoc />
    protected override string ClosesInMinutesMessageKey => nameof(PlayerMessage.KalimaInstanceClosesInMinutesFormat);

    /// <inheritdoc />
    protected override string TimeOverMessageKey => nameof(PlayerMessage.KalimaInstanceTimeOver);

    /// <inheritdoc />
    protected override ValueTask SpawnInitialMonstersAsync() => this.SpawnNextPackAsync();

    /// <inheritdoc />
    protected override void ApplyStrength(Monster monster)
    {
        if (this.IsIllusionOfKundun(monster.Definition))
        {
            this.Scaling.ApplySpecial(monster, this.BossHealth);
        }
        else
        {
            this.Scaling.ApplyRegular(monster);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnPlayerEnteredAsync(Player player)
    {
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KalimaInstanceStatusFormat),
            this.Tier.Level,
            Math.Min(this._currentPack + 1, this._packs.Count),
            this._packs.Count,
            (int)Math.Ceiling(this.TimeLeft.TotalMinutes)).ConfigureAwait(false);
        await ShowSpotsAsync(player, this.GetSpots()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask OnObjectRemovedFromMapAsync((GameMap Map, ILocateable Object) args)
    {
        await base.OnObjectRemovedFromMapAsync(args).ConfigureAwait(false);
        if (args.Object is Player player)
        {
            // The other Kalima maps (e.g. the chamber of Kundun) use the same minimap.
            await ShowSpotsAsync(player, []).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void OnMonsterDied(object? sender, DeathInformation e)
    {
        base.OnMonsterDied(sender, e);
        if (sender is not Monster { SummonedBy: null } monster)
        {
            return;
        }

        var isBoss = this.IsIllusionOfKundun(monster.Definition);
        if (isBoss)
        {
            this.MarkCompleted();
        }

        bool isPackCleared;
        lock (this.SyncRoot)
        {
            isPackCleared = this._alivePackMonsters.Remove(monster) && this._alivePackMonsters.Count == 0;
            if (isBoss)
            {
                this._bossSpot = null;
            }
        }

        var position = monster.Position;
        var killerName = e.KillerName;
        _ = Task.Run(async () =>
        {
            try
            {
                await this.GiveEssenceAsync(monster, isBoss).ConfigureAwait(false);
                await this.DropItemsAsync(position, killerName, isBoss).ConfigureAwait(false);
                if (isBoss)
                {
                    await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstanceBossDefeated)).ConfigureAwait(false);
                }

                if (isPackCleared)
                {
                    await this.SpawnNextPackAsync().ConfigureAwait(false);
                }
                else
                {
                    await this.ShowSpotsToAllAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "{context}: Unexpected error after a monster died.", this);
            }
        });
    }

    private static Point GetEntrancePoint(MiniGameDefinition definition)
    {
        var entrance = definition.Entrance;
        return entrance is null
            ? default
            : new Point((byte)((entrance.X1 + entrance.X2) / 2), (byte)((entrance.Y1 + entrance.Y2) / 2));
    }

    /// <summary>
    /// Spawns the next pack, or the boss after the last pack.
    /// </summary>
    private async ValueTask SpawnNextPackAsync()
    {
        while (!this.IsDisposing && !this.IsDisposed)
        {
            int packIndex;
            lock (this.SyncRoot)
            {
                if (this._alivePackMonsters.Count > 0)
                {
                    return;
                }

                packIndex = ++this._currentPack;
            }

            if (packIndex >= this._packs.Count)
            {
                await this.SpawnBossAsync().ConfigureAwait(false);
                return;
            }

            if (await this.SpawnPackAsync(this._packs[packIndex]).ConfigureAwait(false) > 0)
            {
                await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstancePackFormat), packIndex + 1, this._packs.Count).ConfigureAwait(false);
                await this.ShowSpotsToAllAsync().ConfigureAwait(false);
                return;
            }

            // no monster could be spawned, so we continue with the next pack
        }
    }

    private async ValueTask<int> SpawnPackAsync(IReadOnlyList<MonsterSpawnArea> spawnPoints)
    {
        var spawned = 0;
        for (var i = 0; i < this._configuration.MonstersPerPack && spawnPoints.Count > 0; i++)
        {
            var point = spawnPoints[i % spawnPoints.Count];
            var area = new MonsterSpawnArea
            {
                GameMap = this.Map.Definition,
                MonsterDefinition = point.MonsterDefinition,
                SpawnTrigger = SpawnTrigger.OnceAtEventStart,
                Quantity = 1,
                X1 = (byte)Math.Max(point.X1 - PackSpawnSpread, byte.MinValue),
                X2 = (byte)Math.Min(point.X1 + PackSpawnSpread, byte.MaxValue),
                Y1 = (byte)Math.Max(point.Y1 - PackSpawnSpread, byte.MinValue),
                Y2 = (byte)Math.Min(point.Y1 + PackSpawnSpread, byte.MaxValue),
            };

            if (await this.MapInitializer.InitializeSpawnAsync(i, this.Map, area, this).ConfigureAwait(false) is AttackableNpcBase { IsAlive: true } monster)
            {
                lock (this.SyncRoot)
                {
                    this._alivePackMonsters[monster] = new Point(point.X1, point.Y1);
                }

                spawned++;
            }
        }

        return spawned;
    }

    private async ValueTask SpawnBossAsync()
    {
        lock (this.SyncRoot)
        {
            if (this._bossSpawned || this.BossSpawn?.MonsterDefinition is null)
            {
                return;
            }

            this._bossSpawned = true;
        }

        var spawn = this.BossSpawn;
        if (await this.SpawnMonsterAsync(0, spawn.MonsterDefinition, spawn.X1, spawn.Y1, spawn.X2, spawn.Y2, spawn.Direction).ConfigureAwait(false) is not null)
        {
            lock (this.SyncRoot)
            {
                this._bossSpot = new Point((byte)((spawn.X1 + spawn.X2) / 2), (byte)((spawn.Y1 + spawn.Y2) / 2));
            }
        }

        await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstanceBossAppeared)).ConfigureAwait(false);
        await this.ShowSpotsToAllAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the spots which still have living monsters: the ones of the current pack, or the one of the boss.
    /// </summary>
    /// <returns>The spots.</returns>
    private IReadOnlyList<Point> GetSpots()
    {
        lock (this.SyncRoot)
        {
            if (this._alivePackMonsters.Count > 0)
            {
                return this._alivePackMonsters.Values.Distinct().ToList();
            }

            if (this._bossSpot is { } bossSpot)
            {
                return [bossSpot];
            }

            return [];
        }
    }

    private async ValueTask ShowSpotsToAllAsync()
    {
        var spots = this.GetSpots();
        foreach (var player in this.GetPlayersOnMap())
        {
            await ShowSpotsAsync(player, spots).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gives the Kundun Essence for a killed monster. It's personal: each living player
    /// near the monster rolls its own chance; the boss gives a fixed amount to everyone in the instance.
    /// </summary>
    /// <param name="monster">The killed monster.</param>
    /// <param name="isBoss">If set to <c>true</c>, the monster is the boss.</param>
    private async ValueTask GiveEssenceAsync(Monster monster, bool isBoss)
    {
        if (this.GameContext.FeaturePlugIns.GetPlugIn<KundunEssencePlugIn>() is not { } essence)
        {
            return;
        }

        var players = this.GetPlayersOnMap();

        var configuration = this._configuration;
        foreach (var player in players)
        {
            var amount = isBoss
                ? configuration.BossEssence * this.Tier.Level
                : player.IsAlive && player.GetDistanceTo(monster) <= configuration.EssenceRange
                    ? RollAmount(configuration.EssenceChancePerKill, configuration.EssencePerKill * this.Tier.Level)
                    : 0;
            if (amount > 0)
            {
                await essence.TryAddAsync(player, amount).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Drops the Symbols of Kundun and the Lost Map of the level of this instance. A regular monster
    /// drops a symbol by chance; the boss drops one symbol per player and by chance a lost map.
    /// The items belong to the killer, or are distributed by the drop mode of its party.
    /// </summary>
    /// <param name="position">The position of the killed monster.</param>
    /// <param name="killerName">The name of the killer.</param>
    /// <param name="isBoss">If set to <c>true</c>, the monster is the boss.</param>
    private async ValueTask DropItemsAsync(Point position, string killerName, bool isBoss)
    {
        var players = this.GetPlayersOnMap();
        var killer = players.FirstOrDefault(p => p.Name == killerName) ?? players.FirstOrDefault();
        var playerCount = players.Count;
        if (killer is null)
        {
            return;
        }

        var configuration = this._configuration;
        var symbols = isBoss
            ? Math.Max(1, playerCount) * configuration.BossSymbolsPerPlayer
            : RollAmount(configuration.SymbolDropChance, 1);
        var lostMaps = isBoss ? RollAmount(configuration.BossLostMapChance, 1) : 0;

        var items = this.GameContext.Configuration.Items;
        var level = (byte)this.Tier.Level;
        await this.DropAsync(killer, items.FirstOrDefault(d => d is { Group: KalimaConstants.SymbolOfKundunGroup, Number: KalimaConstants.SymbolOfKundunNumber }), level, symbols, position).ConfigureAwait(false);
        await this.DropAsync(killer, items.FirstOrDefault(d => d.IsLostMap()), level, lostMaps, position).ConfigureAwait(false);
    }
}
