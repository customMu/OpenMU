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
    /// <summary>
    /// The distance from Kundun at which the Illusions of a phase appear: at the edge of the arena of Kalima, so that
    /// the players have to run to each of them.
    /// </summary>
    private const int IllusionSpawnDistance = 8;

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
    private byte _lastHealPercent;
    private byte _lastDefensePercent;
    private byte _lastDamagePercent;
    private KundunChamberStatus? _sentStatus;

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

        this.CloseArena();
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
        await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberKundunAppeared)).ConfigureAwait(false);
        _ = Task.Run(this.RunStatusAsync);
    }

    /// <summary>
    /// The chamber shows its notices in the chat and keeps the state in the banner at the top of the screen,
    /// instead of golden messages in the middle of the screen.
    /// </summary>
    /// <inheritdoc />
    protected override async ValueTask ShowNoticeAsync(string messageKey, params object?[] args)
    {
        await this.ForEachPlayerAsync(player => player.ShowLocalizedBlueMessageAsync(messageKey, args).AsTask()).ConfigureAwait(false);
        await this.SendStatusAsync(true).ConfigureAwait(false);
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
        await ShowProgressAsync(player, 0, 0, 0).ConfigureAwait(false);
        if (player.ViewPlugIns.GetPlugIn<Views.IKalimaInstanceViewPlugIn>() is { } view)
        {
            if (this.ArenaCenter is { } center)
            {
                await view.ShowArenaAsync(center, this._configuration.ArenaRadius).ConfigureAwait(false);
            }

            await view.ShowChamberStatusAsync(this.GetStatus()).ConfigureAwait(false);
        }

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

    /// <summary>
    /// The Illusions of the phases drop money by the level of the chamber; the other monsters drop as usual.
    /// </summary>
    /// <inheritdoc />
    public override MiniGameMonsterDrops? GetMonsterDrops(AttackableNpcBase monster, Player killer)
    {
        if (monster is not Monster { SummonedBy: null } || monster.Definition != this.IllusionDefinition)
        {
            return base.GetMonsterDrops(monster, killer);
        }

        var amounts = this._configuration.IllusionMoneyByLevel;
        var money = amounts.Count > 0 && Rand.NextRandomBool(Math.Clamp(this._configuration.IllusionMoneyChance, 0, 1))
            ? (uint)Math.Max(0, amounts[Math.Clamp(this.Tier.Level - 1, 0, amounts.Count - 1)])
            : 0u;
        return new MiniGameMonsterDrops([], money);
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
        else
        {
            _ = Task.Run(async () => await this.SendStatusAsync(true).ConfigureAwait(false));
        }
    }

    /// <summary>
    /// Gets the current state of the fight.
    /// </summary>
    /// <returns>The state.</returns>
    public KundunChamberStatus GetStatus()
    {
        lock (this)
        {
            var kundun = this._kundun;
            var maximumHealth = kundun?.Attributes[Stats.MaximumHealth] ?? 0;
            var healthPercent = this._isDefeated ? 0
                : kundun is null || maximumHealth <= 0 ? 100
                : (int)Math.Ceiling(Math.Max(0, kundun.Health) * 100.0 / maximumHealth);
            return new KundunChamberStatus(
                (byte)Math.Clamp(this.Tier.Level, 0, byte.MaxValue),
                (byte)Math.Clamp(this._nextPhaseIndex, 0, byte.MaxValue),
                (byte)Math.Clamp(this._phases.Count, 0, byte.MaxValue),
                (byte)Math.Clamp(healthPercent, 0, 100),
                (byte)Math.Clamp(this._aliveIllusions.Count, 0, byte.MaxValue),
                this._isShielded,
                this._isDefeated,
                (ushort)Math.Clamp(this.TimeLeft.TotalSeconds, 0, ushort.MaxValue),
                this._lastHealPercent,
                this._lastDefensePercent,
                this._lastDamagePercent);
        }
    }

    /// <summary>
    /// Sends the state to the players: always when <paramref name="force"/> is set, otherwise only when it changed
    /// (the client counts the time down by itself).
    /// </summary>
    private async ValueTask SendStatusAsync(bool force)
    {
        var status = this.GetStatus();
        lock (this)
        {
            if (!force && this._sentStatus is { } sent && sent with { SecondsLeft = status.SecondsLeft } == status)
            {
                return;
            }

            this._sentStatus = status;
        }

        await this.ForEachPlayerAsync(player => player.InvokeViewPlugInAsync<Views.IKalimaInstanceViewPlugIn>(p => p.ShowChamberStatusAsync(status)).AsTask()).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the health of Kundun in the banner every 2 seconds while the fight lasts.
    /// </summary>
    private async Task RunStatusAsync()
    {
        try
        {
            while (!this.GameEndedToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), this.GameEndedToken).ConfigureAwait(false);
                await this.SendStatusAsync(false).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // the game ended
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error when sending the state of the chamber.", this);
        }
    }

    /// <summary>
    /// Drops the rewards of Kundun: boxes of Kundun of the ranks of the level, a weapon of these ranks by chance and a second one
    /// by a separate chance (both with luck, their skill and a weighted level), and an excellent ring by chance.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="position">The position of Kundun.</param>
    /// <returns>The task.</returns>
    private async ValueTask DropRewardsAsync(Player killer, Point position)
    {
        var configuration = this._configuration;
        var items = this.GameContext.Configuration.Items;
        if (configuration.RewardBoxOfKundun)
        {
            var minimum = Math.Max(0, configuration.MinimumBoxes);
            var count = Rand.NextInt(minimum, Math.Max(minimum, configuration.MaximumBoxes) + 1);
            for (var i = 0; i < count; i++)
            {
                await this.DropItemAsync(killer, KalimaDrops.CreateBoxOfKundun(this.Tier, items), position).ConfigureAwait(false);
            }
        }

        foreach (var chance in new[] { configuration.WeaponChance, configuration.SecondWeaponChance })
        {
            if (Rand.NextRandomBool(Math.Clamp(chance, 0, 1)))
            {
                var weapon = KalimaDrops.CreateWeapon(this.Tier, this.GameContext, configuration.WeaponMinimumLevel, configuration.WeaponLevelWeights, true);
                await this.DropItemAsync(killer, weapon, position).ConfigureAwait(false);
            }
        }

        var optionCounts = configuration.GetJewelryOptionCountWeights(this.Tier.Level);
        if (Rand.NextRandomBool(Math.Clamp(configuration.ExcellentRingChance, 0, 1)))
        {
            await this.DropItemAsync(killer, KalimaDrops.CreateExcellentRing(items, optionCounts), position).ConfigureAwait(false);
        }

        if (Rand.NextRandomBool(Math.Clamp(configuration.ExcellentPendantChance, 0, 1)))
        {
            await this.DropItemAsync(killer, KalimaDrops.CreateExcellentPendant(items, optionCounts), position).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the center of the closed arena, if the arena is closed.
    /// </summary>
    private Point? ArenaCenter => this._configuration.ArenaRadius > 0 && this.BossSpawn is { } spawn
        ? new Point((byte)((spawn.X1 + spawn.X2) / 2), (byte)((spawn.Y1 + spawn.Y2) / 2))
        : null;

    /// <summary>
    /// Closes everything outside of the arena on the map of this chamber: only the ring of columns around
    /// Kundun stays walkable. The map of the chamber is its own, the other Kalima maps stay open.
    /// </summary>
    private void CloseArena()
    {
        if (this.ArenaCenter is not { } center)
        {
            return;
        }

        var terrain = this.Map.Terrain;
        var radius = this._configuration.ArenaRadius;
        for (var x = 0; x <= byte.MaxValue; x++)
        {
            for (var y = 0; y <= byte.MaxValue; y++)
            {
                if (Math.Sqrt(((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y))) >= radius && terrain.WalkMap[x, y])
                {
                    terrain.WalkMap[x, y] = false;
                    terrain.UpdateAiGridValue((byte)x, (byte)y);
                }
            }
        }
    }

    /// <summary>
    /// Gets the walkable spot for an Illusion: at the spawn distance from Kundun in the direction of the angle,
    /// or nearer to Kundun if that's not walkable.
    /// </summary>
    /// <param name="center">The position of Kundun.</param>
    /// <param name="angle">The angle in degrees.</param>
    /// <returns>The spot.</returns>
    private Point GetIllusionSpot(Point center, double angle)
    {
        var walkMap = this.Map.Terrain.WalkMap;
        var radians = angle * Math.PI / 180;
        for (var distance = IllusionSpawnDistance; distance > 1; distance--)
        {
            var x = (int)Math.Round(center.X + (distance * Math.Cos(radians)));
            var y = (int)Math.Round(center.Y + (distance * Math.Sin(radians)));
            if (x is >= 0 and <= byte.MaxValue && y is >= 0 and <= byte.MaxValue && walkMap[x, y])
            {
                return new Point((byte)x, (byte)y);
            }
        }

        return center;
    }

    private async Task StartPhaseAsync(int phaseIndex)
    {
        try
        {
            var phase = this._phases[phaseIndex];
            await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberPhaseFormat), (int)Math.Round(phase.HealthThreshold * 100), phase.IllusionCount).ConfigureAwait(false);
            if (this.IllusionDefinition is not { } illusion || this._kundun is not { } kundun)
            {
                lock (this)
                {
                    this._isShielded = false;
                }

                return;
            }

            // The puppets come out of the edge of the arena, evenly around Kundun, starting at a random side.
            var spawned = 0;
            var center = kundun.Position;
            var startAngle = Rand.NextInt(0, 360);
            for (var i = 0; i < phase.IllusionCount; i++)
            {
                var spot = this.GetIllusionSpot(center, startAngle + (360.0 * i / phase.IllusionCount));
                var monster = await this.SpawnMonsterAsync(i + 1, illusion, spot.X, spot.Y, spot.X, spot.Y).ConfigureAwait(false);
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
            else
            {
                await this.SendStatusAsync(true).ConfigureAwait(false);
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

            // Only what this phase gave Kundun: nothing, only the heal, or the heal and the strength.
            var healPercent = (int)Math.Round(healAmount * 100.0 / Math.Max(1, (int)(this._kundun?.Attributes[Stats.MaximumHealth] ?? 1)));
            var defensePercent = Math.Round(defense * 100, 1);
            var damagePercent = Math.Round(damage * 100, 1);
            lock (this)
            {
                this._lastHealPercent = (byte)Math.Clamp(healPercent, 0, 100);
                this._lastDefensePercent = (byte)Math.Clamp((int)Math.Round(defense * 100), 0, byte.MaxValue);
                this._lastDamagePercent = (byte)Math.Clamp((int)Math.Round(damage * 100), 0, byte.MaxValue);
            }

            if (defensePercent > 0 || damagePercent > 0)
            {
                await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberPhaseEndedStrongerFormat), healPercent, defensePercent, damagePercent).ConfigureAwait(false);
            }
            else if (healPercent > 0)
            {
                await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberPhaseEndedHealFormat), healPercent).ConfigureAwait(false);
            }
            else
            {
                await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberPhaseEndedFormat)).ConfigureAwait(false);
            }
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
            await this.ShowNoticeAsync(nameof(PlayerMessage.KundunChamberVictoryFormat), this.Tier.Level, seconds / 60, seconds % 60).ConfigureAwait(false);

            if (this.GameContext.FeaturePlugIns.GetPlugIn<KundunEssencePlugIn>() is { } essence)
            {
                foreach (var player in players)
                {
                    await essence.TryAddAsync(player, this._configuration.RewardEssence * this.Tier.Level).ConfigureAwait(false);
                }
            }

            var killer = players.FirstOrDefault(p => p.Name == killerName) ?? players.FirstOrDefault();
            if (killer is not null)
            {
                await this.DropRewardsAsync(killer, position).ConfigureAwait(false);
            }

            // The party leader: the party master when in the chamber, else the killer.
            var leader = players.Select(p => p.Party?.PartyMaster).OfType<Player>().FirstOrDefault(players.Contains) ?? killer;
            await this.SaveRankingAsync(players
                .Where(p => p.SelectedCharacter is not null)
                .Select(p => (EncodeRankingMember(p == leader, p), p.SelectedCharacter!, seconds))).ConfigureAwait(false);

            this.FinishAfter(this._configuration.CloseAfterVictory);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error after Kundun died.", this);
        }
    }

    /// <summary>
    /// The "rank" of a member in the ranking of the chamber (all members share the kill): the party leader, the class
    /// and the resets at the time of the kill, which the website shows. Rank % 10: 1 = party leader, 2 = member;
    /// (Rank / 10) % 100 = class number; Rank / 1000 = resets.
    /// </summary>
    private static int EncodeRankingMember(bool isLeader, Player player)
    {
        var classNumber = Math.Clamp((int)(player.SelectedCharacter?.CharacterClass?.Number ?? 0), 0, 99);
        var resets = Math.Clamp((int)(player.Attributes?[Stats.Resets] ?? 0), 0, 999_999);
        return (resets * 1000) + (classNumber * 10) + (isLeader ? 1 : 2);
    }
}
