// <copyright file="KalimaRunContextBase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// The base of the instanced runs on the Kalima maps for a party or a single player (the Kalima instance
/// and the chamber of Kundun): a time limit with notices, closing when nobody is inside anymore, and
/// free re-entry for the characters which paid their entry. The monsters have the strength of the tier.
/// </summary>
public abstract class KalimaRunContextBase : MiniGameContext
{
    private const byte DropSpread = 3;

    private static readonly int[] NoticeMinutes = [30, 10, 5, 1];

    private readonly HashSet<string> _registeredCharacters = new(StringComparer.Ordinal);
    private readonly HashSet<Player> _playersOnMap = new();
    private readonly TimeSpan _closeWhenEmptyAfter;
    private readonly DateTime _endsAt;

    private int _emptyCheckVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaRunContextBase"/> class.
    /// </summary>
    /// <param name="key">The key of this context.</param>
    /// <param name="definition">The definition of the mini game.</param>
    /// <param name="gameContext">The game context, to which this game belongs.</param>
    /// <param name="mapInitializer">The map initializer, which is used when the event starts.</param>
    /// <param name="kalimaConfiguration">The configuration of the Kalima instance, which defines the tiers.</param>
    /// <param name="duration">The duration of the run.</param>
    /// <param name="closeWhenEmptyAfter">The time after which the run is closed, when nobody is inside.</param>
    protected KalimaRunContextBase(
        MiniGameMapKey key,
        MiniGameDefinition definition,
        IGameContext gameContext,
        IMapInitializer mapInitializer,
        KalimaInstanceConfiguration kalimaConfiguration,
        TimeSpan duration,
        TimeSpan closeWhenEmptyAfter)
        : base(key, definition, gameContext, mapInitializer)
    {
        this.KalimaConfiguration = kalimaConfiguration;
        this.GameContext = gameContext;
        this.MapInitializer = mapInitializer;
        this._closeWhenEmptyAfter = closeWhenEmptyAfter;
        this._endsAt = DateTime.UtcNow + duration;
        this.Tier = kalimaConfiguration.GetTier(definition.GameLevel) ?? new KalimaInstanceTier { Level = definition.GameLevel };

        var monsterSpawns = this.Map.Definition.MonsterSpawns
            .Where(area => area is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .ToList();
        this.RegularSpawns = monsterSpawns.Where(area => !this.IsIllusionOfKundun(area.MonsterDefinition!)).ToList();
        this.BossSpawn = monsterSpawns.FirstOrDefault(area => this.IsIllusionOfKundun(area.MonsterDefinition!));

        var strength = KalimaStrengthCalculator.Calculate(gameContext.Configuration, kalimaConfiguration).GetValueOrDefault(this.Tier.Level);
        this.Scaling = new KalimaTierScaling(strength, this.RegularSpawns);
        if (strength is null)
        {
            this.Logger.LogWarning("{context}: The reference map of the tier has no monsters, the monsters keep their strength.", this);
        }
    }

    /// <summary>
    /// Gets the tier of this run.
    /// </summary>
    public KalimaInstanceTier Tier { get; }

    /// <summary>
    /// Gets the strength of the regular monsters of the tier, if the reference map of the tier has monsters.
    /// </summary>
    public KalimaStrength? Strength => this.Scaling.Strength;

    /// <summary>
    /// Gets the maximum health of the Illusion of Kundun of the Kalima instance of this tier.
    /// </summary>
    public float BossHealth => (this.Strength?.Health ?? 0) * this.KalimaConfiguration.BossHealthFactor;

    /// <summary>
    /// Gets the remaining time until the run closes.
    /// </summary>
    public TimeSpan TimeLeft => (this._endsAt - DateTime.UtcNow).AtLeast(TimeSpan.Zero);

    /// <summary>
    /// Gets a value indicating whether the run accepts players.
    /// </summary>
    public bool IsAcceptingPlayers => this.IsEnteringAllowed(this.State) && !this.IsDisposing && !this.IsDisposed;

    /// <inheritdoc />
    public override bool IsHuntingGround => true;

    /// <inheritdoc />
    public override bool IgnoresResetPenalty => true;

    /// <summary>
    /// Gets the configuration of the Kalima instance.
    /// </summary>
    protected KalimaInstanceConfiguration KalimaConfiguration { get; }

    /// <summary>
    /// Gets the game context.
    /// </summary>
    protected IGameContext GameContext { get; }

    /// <summary>
    /// Gets the map initializer.
    /// </summary>
    protected IMapInitializer MapInitializer { get; }

    /// <summary>
    /// Gets the scaling of the monsters to the tier.
    /// </summary>
    protected KalimaTierScaling Scaling { get; }

    /// <summary>
    /// Gets the spawns of the regular monsters of the map.
    /// </summary>
    protected IReadOnlyList<MonsterSpawnArea> RegularSpawns { get; }

    /// <summary>
    /// Gets the spawn of the Illusion of Kundun of the map.
    /// </summary>
    protected MonsterSpawnArea? BossSpawn { get; }

    /// <summary>
    /// Gets the synchronization root of the state of the run.
    /// </summary>
    protected object SyncRoot { get; } = new();

    /// <summary>
    /// Gets the key of the message which notifies the players about the remaining minutes.
    /// </summary>
    protected abstract string ClosesInMinutesMessageKey { get; }

    /// <summary>
    /// Gets the key of the message which notifies the players that the time is over.
    /// </summary>
    protected abstract string TimeOverMessageKey { get; }

    /// <inheritdoc />
    protected override bool EndsWhenAllPlayersLeft => false;

    /// <summary>
    /// Determines whether the character of the player already paid the entry for this run,
    /// so that it can enter again without paying again.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player is registered for this run.</returns>
    public bool IsRegistered(Player player)
    {
        lock (this.SyncRoot)
        {
            return player.SelectedCharacter is { } character && this._registeredCharacters.Contains(character.Name);
        }
    }

    /// <summary>
    /// Registers the character of the player for this run.
    /// </summary>
    /// <param name="player">The player.</param>
    public void Register(Player player)
    {
        lock (this.SyncRoot)
        {
            if (player.SelectedCharacter is { } character)
            {
                this._registeredCharacters.Add(character.Name);
            }
        }
    }

    /// <summary>
    /// Spawns the npcs of the map (e.g. the potion seller) and the initial monsters of the run, instead of all monsters of the map.
    /// The timer and the check for an empty run start here, too.
    /// </summary>
    /// <returns>The task.</returns>
    public override async ValueTask InitializeMapStateAsync()
    {
        _ = Task.Run(() => this.RunTimerAsync(this.GameEndedToken), this.GameEndedToken);
        this.StartEmptyCheck();

        var npcSpawns = this.Map.Definition.MonsterSpawns
            .Where(area => area is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition: { } npc } && npc.ObjectKind != NpcObjectKind.Monster);
        foreach (var area in npcSpawns)
        {
            for (var i = 0; i < area.Quantity; i++)
            {
                await this.MapInitializer.InitializeSpawnAsync(i, this.Map, area).ConfigureAwait(false);
            }
        }

        await this.SpawnInitialMonstersAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the amount with the specified chance, otherwise 0.
    /// </summary>
    /// <param name="chance">The chance (1.0 = 100 %).</param>
    /// <param name="amount">The amount.</param>
    /// <returns>The amount or 0.</returns>
    protected static int RollAmount(float chance, int amount) => chance > 0 && Rand.NextRandomBool((double)Math.Min(chance, 1f)) ? amount : 0;

    /// <summary>
    /// Determines whether the monster is an Illusion of Kundun.
    /// </summary>
    /// <param name="monster">The monster definition.</param>
    /// <returns><c>true</c>, if the monster is an Illusion of Kundun.</returns>
    protected bool IsIllusionOfKundun(MonsterDefinition monster) => this.KalimaConfiguration.BossMonsterNumbers.Contains(monster.Number);

    /// <summary>
    /// Gets the players which are currently in the run.
    /// </summary>
    /// <returns>The players.</returns>
    protected List<Player> GetPlayersOnMap()
    {
        lock (this.SyncRoot)
        {
            return this._playersOnMap.ToList();
        }
    }

    /// <summary>
    /// Spawns the initial monsters of the run.
    /// </summary>
    /// <returns>The task.</returns>
    protected abstract ValueTask SpawnInitialMonstersAsync();

    /// <summary>
    /// Applies the strength of the tier to a monster which was added to the map.
    /// </summary>
    /// <param name="monster">The monster.</param>
    protected abstract void ApplyStrength(Monster monster);

    /// <summary>
    /// Called when a player entered the run, e.g. to show the status.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    protected abstract ValueTask OnPlayerEnteredAsync(Player player);

    /// <summary>
    /// Spawns a monster at the specified area.
    /// </summary>
    /// <param name="index">The index of the monster, for the spawn.</param>
    /// <param name="definition">The monster definition.</param>
    /// <param name="x1">The minimum x coordinate.</param>
    /// <param name="y1">The minimum y coordinate.</param>
    /// <param name="x2">The maximum x coordinate.</param>
    /// <param name="y2">The maximum y coordinate.</param>
    /// <param name="direction">The direction.</param>
    /// <returns>The spawned monster, if it's alive.</returns>
    protected async ValueTask<Monster?> SpawnMonsterAsync(int index, MonsterDefinition definition, byte x1, byte y1, byte x2, byte y2, Direction direction = Direction.Undefined)
    {
        var area = new MonsterSpawnArea
        {
            GameMap = this.Map.Definition,
            MonsterDefinition = definition,
            SpawnTrigger = SpawnTrigger.OnceAtEventStart,
            Quantity = 1,
            Direction = direction,
            X1 = x1,
            X2 = x2,
            Y1 = y1,
            Y2 = y2,
        };

        return await this.MapInitializer.InitializeSpawnAsync(index, this.Map, area, this).ConfigureAwait(false) as Monster is { IsAlive: true } monster
            ? monster
            : null;
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
                lock (this.SyncRoot)
                {
                    this._playersOnMap.Add(player);
                    this._emptyCheckVersion++;
                }

                await this.OnPlayerEnteredAsync(player).ConfigureAwait(false);
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
            lock (this.SyncRoot)
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

    /// <summary>
    /// Drops new items for the killer. The items belong to the killer, or are distributed by the drop mode of its party.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="definition">The item definition.</param>
    /// <param name="level">The item level.</param>
    /// <param name="count">The number of items.</param>
    /// <param name="position">The position of the drop.</param>
    /// <returns>The task.</returns>
    protected async ValueTask DropAsync(Player killer, ItemDefinition? definition, byte level, int count, Point position)
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
                Level = level,
                Durability = 1,
            };
            var dropPosition = i == 0 ? position : this.Map.Terrain.GetRandomCoordinate(position, DropSpread);
            await this.Map.AddAsync(killer.CreateDropForKiller(item, dropPosition, this.Map)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the time after which a completed run (its boss is dead) closes when nobody is inside anymore.
    /// </summary>
    protected virtual TimeSpan CloseWhenCompletedAndEmptyAfter => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets a value indicating whether the run is completed, e.g. its boss is dead.
    /// </summary>
    protected bool IsCompleted { get; private set; }

    /// <summary>
    /// Marks the run as completed: it closes <see cref="CloseWhenCompletedAndEmptyAfter"/> after the last player left.
    /// </summary>
    protected void MarkCompleted()
    {
        bool isEmpty;
        lock (this.SyncRoot)
        {
            this.IsCompleted = true;
            isEmpty = this._playersOnMap.Count == 0;
        }

        if (isEmpty)
        {
            this.StartEmptyCheck();
        }
    }

    /// <summary>
    /// Finishes the run after the specified delay, e.g. to give the players time to pick up the drop.
    /// </summary>
    /// <param name="delay">The delay.</param>
    protected void FinishAfter(TimeSpan delay)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, this.GameEndedToken).ConfigureAwait(false);
                this.FinishEvent();
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                // the game ended already
            }
        });
    }

    /// <summary>
    /// Closes the run after some time without players, so that it doesn't use resources for nothing.
    /// </summary>
    private void StartEmptyCheck()
    {
        int version;
        lock (this.SyncRoot)
        {
            version = ++this._emptyCheckVersion;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var delay = this.IsCompleted ? this.CloseWhenCompletedAndEmptyAfter : this._closeWhenEmptyAfter;
                await Task.Delay(delay, this.GameEndedToken).ConfigureAwait(false);
                lock (this.SyncRoot)
                {
                    if (version != this._emptyCheckVersion || this._playersOnMap.Count > 0)
                    {
                        return;
                    }
                }

                this.Logger.LogDebug("{context}: Closing the empty run.", this);
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
                await this.ShowGoldenMessageAsync(this.ClosesInMinutesMessageKey, minutes).ConfigureAwait(false);
            }

            await Task.Delay(this.TimeLeft, cancellationToken).ConfigureAwait(false);
            await this.ShowGoldenMessageAsync(this.TimeOverMessageKey).ConfigureAwait(false);
            this.FinishEvent();
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // the game ended
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error in the timer of the run.", this);
        }
    }
}
