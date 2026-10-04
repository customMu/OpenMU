// <copyright file="KundunChamberContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// The context of the chamber of Kundun for a party or a single player, on the Kalima map of its level.
/// Kundun appears right away. At the health thresholds of the phases he stops, Illusions of Kundun appear
/// and he's invulnerable until they are killed. After a phase he heals and gets stronger, depending on the
/// time which the players needed for the Illusions. The time of the kill is saved as ranking.
/// </summary>
public sealed class KundunChamberContext : KalimaRunContextBase, IDamageLimiter
{
    private const byte IllusionSpawnDistance = 4;

    /// <summary>
    /// The item group and number of the Box of Kundun; the levels 8 to 12 are the boxes +1 to +5.
    /// </summary>
    private const byte BoxOfKundunGroup = 14;

    private const byte BoxOfKundunNumber = 11;

    private const byte BoxOfKundunFirstLevel = 8;

    private const int BoxOfKundunMaximumGrade = 5;

    private readonly KundunChamberConfiguration _configuration;
    private readonly IReadOnlyList<KundunChamberPhase> _phases;
    private readonly HashSet<Monster> _aliveIllusions = new();
    private readonly SimpleElement _defenseBonus = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _damageBonus = new(1.0f, AggregateType.Multiplicate);

    private Monster? _kundun;
    private DateTime _kundunSpawnedAt;
    private int _nextPhaseIndex;
    private int _activePhaseIndex = -1;
    private DateTime _phaseStartedAt;
    private bool _isShielded;
    private bool _isDefeated;

    /// <summary>
    /// Initializes a new instance of the <see cref="KundunChamberContext"/> class.
    /// </summary>
    /// <param name="key">The key of this context.</param>
    /// <param name="definition">The definition of the mini game.</param>
    /// <param name="gameContext">The game context, to which this game belongs.</param>
    /// <param name="mapInitializer">The map initializer, which is used when the event starts.</param>
    /// <param name="configuration">The configuration of the chamber.</param>
    /// <param name="kalimaConfiguration">The configuration of the Kalima instance, which defines the strength of the tiers.</param>
    public KundunChamberContext(MiniGameMapKey key, MiniGameDefinition definition, IGameContext gameContext, IMapInitializer mapInitializer, KundunChamberConfiguration configuration, KalimaInstanceConfiguration kalimaConfiguration)
        : base(key, definition, gameContext, mapInitializer, kalimaConfiguration, configuration.Duration, configuration.CloseWhenEmptyAfter)
    {
        this._configuration = configuration;
        this._phases = configuration.GetOrderedPhases();
        var kundunNumber = configuration.GetKundunMonsterNumber(this.Tier.Level);
        this.KundunDefinition = gameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == kundunNumber) ?? this.BossSpawn?.MonsterDefinition;
        this.IllusionDefinition = this.BossSpawn?.MonsterDefinition;
    }

    /// <summary>
    /// Gets the monster definition of Kundun.
    /// </summary>
    public MonsterDefinition? KundunDefinition { get; }

    /// <summary>
    /// Gets the monster definition of the Illusions of the phases.
    /// </summary>
    public MonsterDefinition? IllusionDefinition { get; }

    /// <summary>
    /// Gets the spawned Kundun.
    /// </summary>
    public Monster? Kundun => this._kundun;

    /// <summary>
    /// Gets a value indicating whether Kundun is invulnerable, because the Illusions of a phase are alive.
    /// </summary>
    public bool IsShielded
    {
        get
        {
            lock (this)
            {
                return this._isShielded;
            }
        }
    }

    /// <summary>
    /// Gets the current damage bonus factor of Kundun (1.0 = no bonus).
    /// </summary>
    public float DamageBonus => this._damageBonus.Value;

    /// <summary>
    /// Gets the current defense bonus factor of Kundun (1.0 = no bonus).
    /// </summary>
    public float DefenseBonus => this._defenseBonus.Value;

    /// <inheritdoc />
    protected override string ClosesInMinutesMessageKey => nameof(PlayerMessage.KundunChamberClosesInMinutesFormat);

    /// <inheritdoc />
    protected override string TimeOverMessageKey => nameof(PlayerMessage.KundunChamberTimeOver);

    /// <inheritdoc />
    /// <remarks>Called while the hit is locked by this instance (see <see cref="AttackableNpcBase.DamageLimiter"/>).</remarks>
    public uint LimitDamage(AttackableNpcBase npc, uint damage)
    {
        if (npc != this._kundun)
        {
            return damage;
        }

        if (this._isShielded)
        {
            return 0;
        }

        if (this._nextPhaseIndex >= this._phases.Count)
        {
            return damage;
        }

        var phase = this._phases[this._nextPhaseIndex];
        var maximumHealth = npc.Attributes[Stats.MaximumHealth];
        var threshold = (int)Math.Ceiling(maximumHealth * phase.HealthThreshold);
        if (npc.Health - (long)damage > threshold)
        {
            return damage;
        }

        // The phase starts: the damage below the threshold is cut and Kundun is invulnerable until the Illusions are dead.
        var limitedDamage = (uint)Math.Max(0, npc.Health - threshold);
        this._activePhaseIndex = this._nextPhaseIndex;
        this._nextPhaseIndex++;
        this._isShielded = true;
        this._phaseStartedAt = DateTime.UtcNow;
        var phaseIndex = this._activePhaseIndex;
        _ = Task.Run(() => this.StartPhaseAsync(phaseIndex));
        return limitedDamage;
    }

    /// <inheritdoc />
    protected override async ValueTask SpawnInitialMonstersAsync()
    {
        if (this.KundunDefinition is not { } definition || this.BossSpawn is not { } spawn)
        {
            this.Logger.LogWarning("{context}: Kundun or his spawn on the map is missing.", this);
            return;
        }

        var kundun = await this.SpawnMonsterAsync(0, definition, spawn.X1, spawn.Y1, spawn.X2, spawn.Y2, spawn.Direction).ConfigureAwait(false);
        if (kundun is null)
        {
            return;
        }

        lock (this)
        {
            this._kundun = kundun;
            this._kundunSpawnedAt = DateTime.UtcNow;
        }

        kundun.DamageLimiter = this;
        await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KundunChamberKundunAppeared)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ApplyStrength(Monster monster)
    {
        var bossHealth = this.BossHealth;
        if (monster.Definition == this.KundunDefinition && this._kundun is null)
        {
            this.Scaling.ApplySpecial(monster, bossHealth * this._configuration.KundunHealthFactor, this._configuration.KundunDamageFactor);
            monster.Attributes.AddElement(this._damageBonus, Stats.MinimumPhysBaseDmg);
            monster.Attributes.AddElement(this._damageBonus, Stats.MaximumPhysBaseDmg);
            this.Scaling.AddDefense(monster, this._defenseBonus);
            return;
        }

        var phaseIndex = this._activePhaseIndex;
        var phase = phaseIndex >= 0 && phaseIndex < this._phases.Count ? this._phases[phaseIndex] : null;
        this.Scaling.ApplySpecial(monster, bossHealth * (phase?.IllusionHealthFactor ?? 1f), phase?.IllusionDamageFactor ?? 1f);
    }

    /// <inheritdoc />
    protected override async ValueTask OnPlayerEnteredAsync(Player player)
    {
        // The spots of a Kalima instance belong to the instance only.
        await ShowSpotsAsync(player, []).ConfigureAwait(false);

        var kundun = this._kundun;
        var healthPercent = kundun is { } k && k.Attributes[Stats.MaximumHealth] > 0
            ? (int)Math.Ceiling(k.Health * 100.0 / k.Attributes[Stats.MaximumHealth])
            : 100;
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KundunChamberStatusFormat),
            this.Tier.Level,
            healthPercent,
            (int)Math.Ceiling(this.TimeLeft.TotalMinutes)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void OnMonsterDied(object? sender, DeathInformation e)
    {
        base.OnMonsterDied(sender, e);
        if (sender is not Monster { SummonedBy: null } monster)
        {
            return;
        }

        var position = monster.Position;
        if (monster == this._kundun)
        {
            _ = Task.Run(() => this.OnKundunDefeatedAsync(position, e.KillerName));
            return;
        }

        bool phaseCleared;
        int phaseIndex;
        TimeSpan phaseDuration;
        lock (this)
        {
            phaseCleared = this._aliveIllusions.Remove(monster) && this._aliveIllusions.Count == 0 && this._isShielded;
            phaseIndex = this._activePhaseIndex;
            phaseDuration = DateTime.UtcNow - this._phaseStartedAt;
        }

        if (phaseCleared)
        {
            _ = Task.Run(() => this.EndPhaseAsync(phaseIndex, phaseDuration));
        }
    }

    private async Task StartPhaseAsync(int phaseIndex)
    {
        try
        {
            var phase = this._phases[phaseIndex];
            await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KundunChamberPhaseFormat), (int)Math.Round(phase.HealthThreshold * 100), phase.IllusionCount).ConfigureAwait(false);
            if (this.IllusionDefinition is not { } illusion || this._kundun is not { } kundun)
            {
                lock (this)
                {
                    this._isShielded = false;
                }

                return;
            }

            var spawned = 0;
            for (var i = 0; i < phase.IllusionCount; i++)
            {
                var center = kundun.Position;
                var monster = await this.SpawnMonsterAsync(
                    i + 1,
                    illusion,
                    (byte)Math.Max(center.X - IllusionSpawnDistance, 0),
                    (byte)Math.Max(center.Y - IllusionSpawnDistance, 0),
                    (byte)Math.Min(center.X + IllusionSpawnDistance, byte.MaxValue),
                    (byte)Math.Min(center.Y + IllusionSpawnDistance, byte.MaxValue)).ConfigureAwait(false);
                if (monster is not null)
                {
                    lock (this)
                    {
                        this._aliveIllusions.Add(monster);
                    }

                    spawned++;
                }
            }

            if (spawned == 0)
            {
                await this.EndPhaseAsync(phaseIndex, TimeSpan.Zero).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error when starting the phase {phase}.", this, phaseIndex);
        }
    }

    /// <summary>
    /// Ends the phase: Kundun heals and gets stronger, depending on the time which was needed for the Illusions.
    /// </summary>
    /// <param name="phaseIndex">The index of the phase.</param>
    /// <param name="duration">The time which the players needed for the Illusions.</param>
    private async Task EndPhaseAsync(int phaseIndex, TimeSpan duration)
    {
        try
        {
            var phase = this._phases[phaseIndex];
            var (heal, defense, damage) = phase.CalculateEffects(duration.TotalSeconds - this._configuration.FreeSecondsPerPhase);
            var healAmount = 0;
            lock (this)
            {
                if (this._kundun is { IsAlive: true } kundun)
                {
                    var maximumHealth = (int)kundun.Attributes[Stats.MaximumHealth];
                    healAmount = Math.Min((int)(maximumHealth * heal), maximumHealth - kundun.Health);
                    kundun.Health += Math.Max(0, healAmount);
                }

                this._defenseBonus.Value += defense;
                this._damageBonus.Value += damage;
                this._isShielded = false;
            }

            await this.ShowGoldenMessageAsync(
                nameof(PlayerMessage.KundunChamberPhaseEndedFormat),
                (int)Math.Round(duration.TotalSeconds),
                (int)Math.Round(heal * 100),
                (int)Math.Round((this._defenseBonus.Value - 1) * 100),
                (int)Math.Round((this._damageBonus.Value - 1) * 100)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error when ending the phase {phase}.", this, phaseIndex);
        }
    }

    private async Task OnKundunDefeatedAsync(Point position, string killerName)
    {
        try
        {
            TimeSpan fightDuration;
            lock (this)
            {
                if (this._isDefeated)
                {
                    return;
                }

                this._isDefeated = true;
                fightDuration = DateTime.UtcNow - this._kundunSpawnedAt;
            }

            var players = this.GetPlayersOnMap();
            var seconds = (int)Math.Ceiling(fightDuration.TotalSeconds);
            await this.ShowGoldenMessageAsync(nameof(PlayerMessage.KundunChamberVictoryFormat), this.Tier.Level, seconds / 60, seconds % 60).ConfigureAwait(false);

            if (this.GameContext.FeaturePlugIns.GetPlugIn<KundunEssencePlugIn>() is { } essence)
            {
                foreach (var player in players)
                {
                    await essence.TryAddAsync(player, this._configuration.RewardEssence * this.Tier.Level).ConfigureAwait(false);
                }
            }

            var killer = players.FirstOrDefault(p => p.Name == killerName) ?? players.FirstOrDefault();
            if (killer is not null && this._configuration.RewardBoxOfKundun)
            {
                var box = this.GameContext.Configuration.Items.FirstOrDefault(d => d is { Group: BoxOfKundunGroup, Number: BoxOfKundunNumber });
                var boxLevel = (byte)(BoxOfKundunFirstLevel - 1 + Math.Clamp(this.Tier.Level, 1, BoxOfKundunMaximumGrade));
                await this.DropAsync(killer, box, boxLevel, Math.Max(1, players.Count), position).ConfigureAwait(false);
            }

            await this.SaveRankingAsync(players
                .Where(p => p.SelectedCharacter is not null)
                .Select(p => (1, p.SelectedCharacter!, seconds))).ConfigureAwait(false);

            this.FinishAfter(this._configuration.CloseAfterVictory);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error after Kundun died.", this);
        }
    }
}
