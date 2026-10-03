// <copyright file="KalimaInstanceContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// The context of a Kalima instance for a party or a single player. The monsters come in packs
/// along the way from the entrance; the next pack appears when the previous one is killed, and
/// the Illusion of Kundun after the last pack. The monsters have the fixed strength of the tier.
/// Players can leave and enter again (e.g. after a death) as long as the instance is running.
/// </summary>
public sealed class KalimaInstanceContext : MiniGameContext
{
    private const int PackSpawnSpread = 2;

    private const byte DropSpread = 3;

    private static readonly int[] NoticeMinutes = [30, 10, 5, 1];

    private readonly KalimaInstanceConfiguration _configuration;
    private readonly IGameContext _gameContext;
    private readonly IMapInitializer _mapInitializer;
    private readonly SimpleElement _healthMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _bossHealthMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _damageMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _defenseMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _rateMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _levelIncrease = new(0f, AggregateType.AddRaw);
    private readonly HashSet<string> _registeredCharacters = new(StringComparer.Ordinal);
    private readonly HashSet<Player> _playersOnMap = new();
    private readonly HashSet<AttackableNpcBase> _alivePackMonsters = new();
    private readonly object _syncRoot = new();
    private readonly DateTime _endsAt;
    private readonly IReadOnlyList<IReadOnlyList<MonsterSpawnArea>> _packs;
    private readonly MonsterSpawnArea? _bossSpawn;

    private int _currentPack = -1;
    private bool _bossSpawned;
    private int _emptyCheckVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaInstanceContext"/> class.
    /// </summary>
    /// <param name="key">The key of this context.</param>
    /// <param name="definition">The definition of the mini game.</param>
    /// <param name="gameContext">The game context, to which this game belongs.</param>
    /// <param name="mapInitializer">The map initializer, which is used when the event starts.</param>
    /// <param name="configuration">The configuration of the Kalima instance.</param>
    public KalimaInstanceContext(MiniGameMapKey key, MiniGameDefinition definition, IGameContext gameContext, IMapInitializer mapInitializer, KalimaInstanceConfiguration configuration)
        : base(key, definition, gameContext, mapInitializer)
    {
        this._configuration = configuration;
        this._gameContext = gameContext;
        this._mapInitializer = mapInitializer;
        this.Tier = configuration.GetTier(definition.GameLevel) ?? new KalimaInstanceTier { Level = definition.GameLevel };
        this.Strength = KalimaStrengthCalculator.Calculate(gameContext.Configuration, configuration).GetValueOrDefault(this.Tier.Level);
        this._endsAt = DateTime.UtcNow + configuration.Duration;

        var monsterSpawns = this.Map.Definition.MonsterSpawns
            .Where(area => area is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .ToList();
        var regularSpawns = monsterSpawns.Where(area => !this.IsBoss(area.MonsterDefinition!)).ToList();
        this._bossSpawn = monsterSpawns.FirstOrDefault(area => this.IsBoss(area.MonsterDefinition!));
        this._packs = KalimaPackPlanner.PlanPacks(regularSpawns, GetEntrancePoint(definition), this.Map.Terrain.WalkMap, configuration.PackCount);
        this.CalculateStrength(regularSpawns);

        _ = Task.Run(() => this.RunTimerAsync(this.GameEndedToken), this.GameEndedToken);
        this.StartEmptyCheck();
    }

    /// <summary>
    /// Gets the tier of this instance.
    /// </summary>
    public KalimaInstanceTier Tier { get; }

    /// <summary>
    /// Gets the strength of the regular monsters, if the reference map of the tier has monsters.
    /// </summary>
    public KalimaStrength? Strength { get; }

    /// <summary>
    /// Gets the remaining time until the instance closes.
    /// </summary>
    public TimeSpan TimeLeft => (this._endsAt - DateTime.UtcNow).AtLeast(TimeSpan.Zero);

    /// <summary>
    /// Gets the experience multiplier of the instance.
    /// </summary>
    public float ExperienceMultiplier => this._configuration.ExperienceMultiplier;

    /// <inheritdoc />
    public override bool IsHuntingGround => true;

    /// <inheritdoc />
    public override bool IgnoresResetPenalty => true;

    /// <inheritdoc />
    public override double ItemDropMultiplier => this.Tier.DropMultiplier;

    /// <summary>
    /// Gets a value indicating whether the instance accepts players.
    /// </summary>
    public bool IsAcceptingPlayers => this.IsEnteringAllowed(this.State) && !this.IsDisposing && !this.IsDisposed;

    /// <inheritdoc />
    protected override bool EndsWhenAllPlayersLeft => false;

    /// <summary>
    /// Determines whether the character of the player already used an entry for this instance,
    /// so that it can enter again without using another one.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player is registered for this instance.</returns>
    public bool IsRegistered(Player player)
    {
        lock (this._syncRoot)
        {
            return player.SelectedCharacter is { } character && this._registeredCharacters.Contains(character.Name);
        }
    }

    /// <summary>
    /// Registers the character of the player for this instance.
    /// </summary>
    /// <param name="player">The player.</param>
    public void Register(Player player)
    {
        lock (this._syncRoot)
        {
            if (player.SelectedCharacter is { } character)
            {
                this._registeredCharacters.Add(character.Name);
            }
        }
    }

    /// <summary>
    /// Spawns the npcs of the map (e.g. the potion seller) and the first pack, instead of all monsters of the map.
    /// </summary>
    /// <returns>The task.</returns>
    public override async ValueTask InitializeMapStateAsync()
    {
        var npcSpawns = this.Map.Definition.MonsterSpawns
            .Where(area => area is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition: { } npc } && npc.ObjectKind != NpcObjectKind.Monster);
        foreach (var area in npcSpawns)
        {
            for (var i = 0; i < area.Quantity; i++)
            {
                await this._mapInitializer.InitializeSpawnAsync(i, this.Map, area).ConfigureAwait(false);
            }
        }

        await this.SpawnNextPackAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override bool IsEnteringAllowed(MiniGameState state) => state is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing;

    /// <inheritdoc />
    protected override async ValueTask OnObjectAddedToMapAsync((GameMap Map, ILocateable Object) args)
    {
        await base.OnObjectAddedToMapAsync(args).ConfigureAwait(false);

        switch (args.Object)
        {
            case Monster { SummonedBy: null } monster:
                this.ApplyStrength(monster);
                break;
            case Player player:
                lock (this._syncRoot)
                {
                    this._playersOnMap.Add(player);
                    this._emptyCheckVersion++;
                }

                await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.KalimaInstanceStatusFormat),
                    this.Tier.Level,
                    Math.Min(this._currentPack + 1, this._packs.Count),
                    this._packs.Count,
                    (int)Math.Ceiling(this.TimeLeft.TotalMinutes)).ConfigureAwait(false);
                break;
            default:
                // nothing to do
                break;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnObjectRemovedFromMapAsync((GameMap Map, ILocateable Object) args)
    {
        await base.OnObjectRemovedFromMapAsync(args).ConfigureAwait(false);

        if (args.Object is Player player)
        {
            bool isEmpty;
            lock (this._syncRoot)
            {
                this._playersOnMap.Remove(player);
                isEmpty = this._playersOnMap.Count == 0;
            }

            if (isEmpty)
            {
                this.StartEmptyCheck();
            }
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

        var isBoss = this.IsBoss(monster.Definition);
        bool isPackCleared;
        lock (this._syncRoot)
        {
            isPackCleared = this._alivePackMonsters.Remove(monster) && this._alivePackMonsters.Count == 0;
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
                else if (isPackCleared)
                {
                    await this.SpawnNextPackAsync().ConfigureAwait(false);
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

    private static float GetAverage(IReadOnlyCollection<MonsterSpawnArea> spawns, AttributeDefinition attribute)
    {
        return spawns.Count == 0 ? 0 : spawns.Average(area => KalimaStrengthCalculator.GetValue(area.MonsterDefinition!, attribute));
    }

    private static float GetRatio(float target, float average) => target > 0 && average > 0 ? target / average : 1.0f;

    private static int RollAmount(float chance, int amount) => chance > 0 && Rand.NextRandomBool((double)Math.Min(chance, 1f)) ? amount : 0;

    private bool IsBoss(MonsterDefinition monster) => this._configuration.BossMonsterNumbers.Contains(monster.Number);

    /// <summary>
    /// Calculates the multipliers which give the regular monsters the average strength of the tier.
    /// The differences between the monster types stay.
    /// </summary>
    /// <param name="regularSpawns">The spawns of the regular monsters.</param>
    private void CalculateStrength(IReadOnlyCollection<MonsterSpawnArea> regularSpawns)
    {
        if (this.Strength is not { } strength)
        {
            this.Logger.LogWarning("{context}: The reference map of the tier has no monsters, the monsters keep their strength.", this);
            return;
        }

        var averageHealth = GetAverage(regularSpawns, Stats.MaximumHealth);
        var averageLevel = GetAverage(regularSpawns, Stats.Level);

        this._healthMultiplier.Value = GetRatio(strength.Health, averageHealth);
        this._damageMultiplier.Value = GetRatio(strength.Damage, GetAverage(regularSpawns, Stats.MaximumPhysBaseDmg));
        this._defenseMultiplier.Value = GetRatio(strength.Defense, GetAverage(regularSpawns, Stats.DefenseBase));
        this._rateMultiplier.Value = GetRatio(strength.Level, averageLevel);
        this._levelIncrease.Value = strength.Level > 0 && averageLevel > 0 ? strength.Level - averageLevel : 0;

        var bossHealth = this._bossSpawn?.MonsterDefinition is { } boss ? KalimaStrengthCalculator.GetValue(boss, Stats.MaximumHealth) : 0;
        this._bossHealthMultiplier.Value = bossHealth > 0 ? this._configuration.BossHealthFactor * strength.Health / bossHealth : 1.0f;
    }

    private void ApplyStrength(Monster monster)
    {
        var attributes = monster.Attributes;
        attributes.AddElement(this.IsBoss(monster.Definition) ? this._bossHealthMultiplier : this._healthMultiplier, Stats.MaximumHealth);
        attributes.AddElement(this._damageMultiplier, Stats.MinimumPhysBaseDmg);
        attributes.AddElement(this._damageMultiplier, Stats.MaximumPhysBaseDmg);
        attributes.AddElement(this._defenseMultiplier, Stats.DefenseBase);
        attributes.AddElement(this._rateMultiplier, Stats.AttackRatePvm);
        attributes.AddElement(this._rateMultiplier, Stats.DefenseRatePvm);
        attributes.AddElement(this._levelIncrease, Stats.Level);
        monster.Health = (int)attributes[Stats.MaximumHealth];
    }

    /// <summary>
    /// Spawns the next pack, or the boss after the last pack.
    /// </summary>
    private async ValueTask SpawnNextPackAsync()
    {
        while (!this.IsDisposing && !this.IsDisposed)
        {
            int packIndex;
            lock (this._syncRoot)
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

            if (await this._mapInitializer.InitializeSpawnAsync(i, this.Map, area, this).ConfigureAwait(false) is AttackableNpcBase { IsAlive: true } monster)
            {
                lock (this._syncRoot)
                {
                    this._alivePackMonsters.Add(monster);
                }

                spawned++;
            }
        }

        return spawned;
    }

    private async ValueTask SpawnBossAsync()
    {
        lock (this._syncRoot)
        {
            if (this._bossSpawned || this._bossSpawn is null)
            {
                return;
            }

            this._bossSpawned = true;
        }

        var area = new MonsterSpawnArea
        {
            GameMap = this.Map.Definition,
            MonsterDefinition = this._bossSpawn.MonsterDefinition,
            SpawnTrigger = SpawnTrigger.OnceAtEventStart,
            Quantity = 1,
            Direction = this._bossSpawn.Direction,
            X1 = this._bossSpawn.X1,
            X2 = this._bossSpawn.X2,
            Y1 = this._bossSpawn.Y1,
            Y2 = this._bossSpawn.Y2,
        };

        await this._mapInitializer.InitializeSpawnAsync(0, this.Map, area, this).ConfigureAwait(false);
        await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstanceBossAppeared)).ConfigureAwait(false);
    }

    /// <summary>
    /// Gives the Kundun Essence for a killed monster. It's personal: each living player
    /// near the monster rolls its own chance; the boss gives a fixed amount to everyone in the instance.
    /// </summary>
    /// <param name="monster">The killed monster.</param>
    /// <param name="isBoss">If set to <c>true</c>, the monster is the boss.</param>
    private async ValueTask GiveEssenceAsync(Monster monster, bool isBoss)
    {
        if (this._gameContext.FeaturePlugIns.GetPlugIn<KundunEssencePlugIn>() is not { } essence)
        {
            return;
        }

        List<Player> players;
        lock (this._syncRoot)
        {
            players = this._playersOnMap.ToList();
        }

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
        Player? killer;
        int playerCount;
        lock (this._syncRoot)
        {
            killer = this._playersOnMap.FirstOrDefault(p => p.Name == killerName) ?? this._playersOnMap.FirstOrDefault();
            playerCount = this._playersOnMap.Count;
        }

        if (killer is null)
        {
            return;
        }

        var configuration = this._configuration;
        var symbols = isBoss
            ? Math.Max(1, playerCount) * configuration.BossSymbolsPerPlayer
            : RollAmount(configuration.SymbolDropChance, 1);
        var lostMaps = isBoss ? RollAmount(configuration.BossLostMapChance, 1) : 0;

        var items = this._gameContext.Configuration.Items;
        await this.DropAsync(killer, items.FirstOrDefault(d => d is { Group: KalimaConstants.SymbolOfKundunGroup, Number: KalimaConstants.SymbolOfKundunNumber }), symbols, position).ConfigureAwait(false);
        await this.DropAsync(killer, items.FirstOrDefault(d => d.IsLostMap()), lostMaps, position).ConfigureAwait(false);
    }

    private async ValueTask DropAsync(Player killer, ItemDefinition? definition, int count, Point position)
    {
        if (definition is null)
        {
            if (count > 0)
            {
                this.Logger.LogWarning("{context}: The item definition to drop is missing.", this);
            }

            return;
        }

        for (var i = 0; i < count; i++)
        {
            var item = new TemporaryItem
            {
                Definition = definition,
                Level = (byte)this.Tier.Level,
                Durability = 1,
            };
            var dropPosition = i == 0 ? position : this.Map.Terrain.GetRandomCoordinate(position, DropSpread);
            await this.Map.AddAsync(killer.CreateDropForKiller(item, dropPosition, this.Map)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Closes the instance after some time without players, so that it doesn't use resources for nothing.
    /// </summary>
    private void StartEmptyCheck()
    {
        int version;
        lock (this._syncRoot)
        {
            version = ++this._emptyCheckVersion;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(this._configuration.CloseWhenEmptyAfter, this.GameEndedToken).ConfigureAwait(false);
                lock (this._syncRoot)
                {
                    if (version != this._emptyCheckVersion || this._playersOnMap.Count > 0)
                    {
                        return;
                    }
                }

                this.Logger.LogDebug("{context}: Closing the empty instance.", this);
                this.FinishEvent();
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                // the game ended
            }
        });
    }

    private async Task RunTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            foreach (var minutes in NoticeMinutes)
            {
                var delay = this.TimeLeft - TimeSpan.FromMinutes(minutes);
                if (delay < TimeSpan.Zero)
                {
                    continue;
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstanceClosesInMinutesFormat), minutes).ConfigureAwait(false);
            }

            await Task.Delay(this.TimeLeft, cancellationToken).ConfigureAwait(false);
            await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KalimaInstanceTimeOver)).ConfigureAwait(false);
            this.FinishEvent();
        }
        catch (OperationCanceledException)
        {
            // the game ended
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error in the timer of the instance.", this);
        }
    }
}
