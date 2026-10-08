// <copyright file="AttackableNpcBase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

using System.Diagnostics;
using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// An abstract base class for an <see cref="IAttackable"/> <see cref="NonPlayerCharacter"/>.
/// </summary>
public abstract class AttackableNpcBase : NonPlayerCharacter, IAttackable
{
    private const byte MaximumDropDistance = 2;

    private readonly IEventStateProvider? _eventStateProvider;
    private readonly IDropGenerator _dropGenerator;
    private readonly PlugInManager _plugInManager;
    private readonly List<IDisposable> _registrations = new();

    /// <summary>
    /// The damage which each player (including his summons) has dealt to this instance since it spawned.
    /// Used to distribute the rewards by damage, see <see cref="DamageBasedKillRewardsPlugIn"/>.
    /// </summary>
    private readonly Dictionary<Player, long> _damageByPlayer = new();

    private int _health;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttackableNpcBase" /> class.
    /// </summary>
    /// <param name="spawnInfo">The spawn information.</param>
    /// <param name="stats">The stats.</param>
    /// <param name="map">The map.</param>
    /// <param name="eventStateProvider">The event state provider.</param>
    /// <param name="dropGenerator">The drop generator.</param>
    /// <param name="plugInManager">The plug in manager.</param>
    protected AttackableNpcBase(MonsterSpawnArea spawnInfo, MonsterDefinition stats, GameMap map, IEventStateProvider? eventStateProvider, IDropGenerator dropGenerator, PlugInManager plugInManager)
        : base(spawnInfo, stats, map)
    {
        this._eventStateProvider = eventStateProvider;
        this._dropGenerator = dropGenerator;
        this._plugInManager = plugInManager;
        this.MagicEffectList = new MagicEffectsList(this);
        this.Attributes = new MonsterAttributeHolder(this);
    }

    /// <summary>
    /// Occurs when this instance died.
    /// </summary>
    public event EventHandler<DeathInformation>? Died;

    /// <inheritdoc />
    public IAttributeSystem Attributes { get; }

    /// <inheritdoc />
    public MagicEffectsList MagicEffectList { get; }

    /// <inheritdoc />
    public bool IsAlive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this <see cref="IAttackable" /> is currently teleporting and can't be directly targeted.
    /// It can still receive damage, if the teleport target coordinates are within an target skill area for area attacks.
    /// </summary>
    /// <value>
    ///   <c>true</c> if teleporting; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>Teleporting for monsters or npcs is not implemented yet.</remarks>
    public bool IsTeleporting => false;

    /// <inheritdoc />
    public DeathInformation? LastDeath { get; protected set; }

    /// <inheritdoc/>
    public override Point Position
    {
        get => base.Position;
        set
        {
            if (base.Position != value)
            {
                base.Position = value;
                this._plugInManager?.GetPlugInPoint<IAttackableMovedPlugIn>()?.AttackableMoved(this);
            }
        }
    }

    /// <summary>
    /// Gets or sets the current health.
    /// </summary>
    public int Health
    {
        get => Math.Max(Volatile.Read(ref this._health), 0);
        set => Interlocked.Exchange(ref this._health, value);
    }

    /// <summary>
    /// Gets or sets the limiter of the damage which this npc takes, e.g. for the phases of a boss.
    /// </summary>
    public IDamageLimiter? DamageLimiter { get; set; }

    private bool ShouldRespawn => this.SpawnArea.SpawnTrigger == SpawnTrigger.Automatic
                                  || (this.SpawnArea.SpawnTrigger == SpawnTrigger.AutomaticDuringEvent && (this._eventStateProvider?.IsEventRunning ?? false))
                                  || (this.SpawnArea.SpawnTrigger == SpawnTrigger.AutomaticDuringWave && (this._eventStateProvider?.IsSpawnWaveActive(this.SpawnArea.WaveNumber) ?? false));

    /// <inheritdoc />
    public async ValueTask<HitInfo?> AttackByAsync(IAttacker attacker, SkillEntry? skill, bool isCombo, double damageFactor = 1.0, bool? isFinalStreakHit = null)
    {
        if (this.Definition.ObjectKind == NpcObjectKind.Guard
            || this.IsAttackBlockedBySafezone(attacker)
            || !this.CanBeAttackedBy(attacker))
        {
            return null;
        }

        (attacker as Player ?? (attacker as IPlayerSurrogate)?.Owner)?.RememberCombatTarget(this);

        var hitInfo = await attacker.CalculateDamageAsync(this, skill, isCombo, damageFactor).ConfigureAwait(false);

        if (skill?.Skill is not { } attackSkill || attackSkill.DamageType != DamageType.Fenrir)
        {
            attacker.ApplyAmmunitionConsumption(hitInfo);
        }

        await this.HitAsync(hitInfo, attacker, skill?.Skill, isFinalStreakHit).ConfigureAwait(false);

        if (hitInfo.HealthDamage > 0)
        {
            if (this.Attributes[Stats.IsAsleep] > 0)
            {
                await this.MagicEffectList.ClearAllEffectsProducingSpecificStatAsync(Stats.IsAsleep).ConfigureAwait(false);
            }

            if (attacker is Player player)
            {
                await player.AfterHitTargetAsync().ConfigureAwait(false);

                if (this.IsAlive && Rand.NextRandomBool(player.Attributes![Stats.MaceMasteryStunChance]))
                {
                    await player.ApplyMaceMasteryStunEffectAsync(this).ConfigureAwait(false);
                }
            }

            if (attacker as IPlayerSurrogate is { } playerSurrogate)
            {
                await playerSurrogate.Owner.AfterHitTargetAsync().ConfigureAwait(false);
            }
        }

        return hitInfo;
    }

    /// <inheritdoc />
    public abstract ValueTask ReflectDamageAsync(IAttacker reflector, uint damage);

    /// <inheritdoc />
    public abstract ValueTask ApplyPoisonDamageAsync(IAttacker initialAttacker, uint damage);

    /// <inheritdoc />
    public abstract ValueTask ApplyBleedingDamageAsync(IAttacker initialAttacker, uint damage);

    /// <inheritdoc/>
    public ValueTask KillInstantlyAsync()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        lock (this._damageByPlayer)
        {
            this._damageByPlayer.Clear();
        }

        this.Health = this.SpawnArea.MaximumHealthOverride ?? (int)this.Attributes[Stats.MaximumHealth];
        this.IsAlive = true;
    }

    /// <summary>
    /// Reloads the attributes from the <see cref="NonPlayerCharacter.Definition"/>, so that changes
    /// to the monster definition take effect on this already spawned instance.
    /// </summary>
    public void ReloadAttributes()
    {
        (this.Attributes as MonsterAttributeHolder)?.ApplyChanges();
    }

    /// <summary>
    /// Registers a disposable (e.g. a configuration change registration) to be disposed
    /// together with this instance.
    /// </summary>
    /// <param name="disposable">The disposable.</param>
    public void RegisterDisposable(IDisposable disposable)
    {
        this._registrations.Add(disposable);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool managed)
    {
        if (managed)
        {
            this.Died = null;
            this.IsAlive = false;
            foreach (var registration in this._registrations)
            {
                registration.Dispose();
            }

            this._registrations.Clear();
        }

        base.Dispose(managed);
    }

    /// <summary>
    /// Called when the object is removed from the map.
    /// </summary>
    protected virtual void OnRemoveFromMap()
    {
        // can be overwritten to do additional stuff.
    }

    /// <summary>
    /// Hits this instance with the specified hit information.
    /// </summary>
    /// <param name="hitInfo">The hit information.</param>
    /// <param name="attacker">The attacker.</param>
    /// <param name="skill">The skill.</param>
    /// <param name="isFinalStreakHit">
    ///     Not <c>null</c> when it's a rage fighter multiple hit skill:
    ///     <c>true</c>, if it's the final hit;
    ///     <c>false</c>, for other hits.
    /// </param>
    protected async ValueTask HitAsync(HitInfo hitInfo, IAttacker attacker, Skill? skill, bool? isFinalStreakHit = null)
    {
        if (!this.IsAlive)
        {
            return;
        }

        var killed = this.TryHit(hitInfo.HealthDamage + hitInfo.ShieldDamage, attacker);

        var player = this.GetHitNotificationTarget(attacker);
        if (player is not null)
        {
            if (isFinalStreakHit.HasValue)
            {
                hitInfo.Attributes |= DamageAttributes.RageFighterStreakHit;

                if (isFinalStreakHit.Value || killed)
                {
                    hitInfo.Attributes |= DamageAttributes.RageFighterStreakFinalHit;
                }
            }

            await player.InvokeViewPlugInAsync<IShowHitPlugIn>(p => p.ShowHitAsync(this, hitInfo)).ConfigureAwait(false);
            player.GameContext.PlugInManager.GetPlugInPoint<IAttackableGotHitPlugIn>()?.AttackableGotHit(this, attacker, hitInfo);
        }

        if (killed)
        {
            this.LastDeath = new DeathInformation(attacker.Id, attacker.GetName(), hitInfo, skill?.Number ?? 0);
            await this.OnDeathAsync(attacker).ConfigureAwait(false);
            this.Died?.Invoke(this, this.LastDeath);
            if (!this.ShouldRespawn)
            {
                await this.RemoveFromMapAndDisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Gets the target of a hit notification.
    /// </summary>
    /// <param name="attacker">The attacker.</param>
    /// <returns>The target player of a hit notification.</returns>
    protected virtual Player? GetHitNotificationTarget(IAttacker attacker)
    {
        return attacker as Player ?? (attacker as IPlayerSurrogate)?.Owner;
    }

    /// <summary>
    /// Determines whether the specified attacker may attack this NPC.
    /// </summary>
    /// <param name="attacker">The attacker.</param>
    /// <returns><see langword="true"/> when the attack is allowed; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CanBeAttackedBy(IAttacker attacker) => true;

    /// <summary>
    /// Registers the hit.
    /// </summary>
    /// <param name="attacker">The attacker.</param>
    protected virtual void RegisterHit(IAttacker attacker)
    {
        // can be overwritten
    }

    /// <summary>
    /// Atomically restores health without exceeding the specified maximum.
    /// </summary>
    /// <param name="amount">The maximum amount of health to restore.</param>
    /// <param name="maximumHealth">The maximum health after the restoration.</param>
    /// <returns>The restored health.</returns>
    protected int RestoreHealth(int amount, int maximumHealth)
    {
        if (amount <= 0 || maximumHealth <= 0)
        {
            return 0;
        }

        while (true)
        {
            var currentHealth = Volatile.Read(ref this._health);
            if (currentHealth <= 0 || currentHealth >= maximumHealth)
            {
                return 0;
            }

            var restoredHealth = Math.Min(amount, maximumHealth - currentHealth);
            if (Interlocked.CompareExchange(
                    ref this._health,
                    currentHealth + restoredHealth,
                    currentHealth) == currentHealth)
            {
                if (currentHealth + restoredHealth >= maximumHealth)
                {
                    // Fully healed again: the previous fight is over, earlier damage doesn't count anymore.
                    lock (this._damageByPlayer)
                    {
                        this._damageByPlayer.Clear();
                    }
                }

                return restoredHealth;
            }
        }
    }

    /// <summary>
    /// Called when this instance died.
    /// </summary>
    /// <param name="attacker">The attacker which killed this instance.</param>
    protected virtual async ValueTask OnDeathAsync(IAttacker attacker)
    {
        if (this.ShouldRespawn)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(this.Definition.RespawnDelay).ConfigureAwait(false);
                    await this.RespawnAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.Fail($"Unexpected error during respawning the attackable npc {this}: {ex}", ex.StackTrace);
                }
            });
        }

        await this.ForEachWorldObserverAsync<IObjectGotKilledPlugIn>(p => p.ObjectGotKilledAsync(this, attacker), true).ConfigureAwait(false);

        var player = this.GetHitNotificationTarget(attacker);
        if (player is { })
        {
            IReadOnlyList<ExperienceShare> experienceShares;
            var dropOwner = player;
            if (player.GameContext.FeaturePlugIns.GetPlugIn<DamageBasedKillRewardsPlugIn>() is not null
                && await this.TryDistributeExperienceByDamageAsync().ConfigureAwait(false) is { } damageResult)
            {
                (experienceShares, dropOwner) = damageResult;
            }
            else
            {
                experienceShares = player.Party is { } party
                    ? await party.DistributeExperienceAfterKillAsync(this, player).ConfigureAwait(false)
                    : [new ExperienceShare(player, await player.AddExpAfterKillAsync(this).ConfigureAwait(false))];
            }

            lock (this._damageByPlayer)
            {
                this._damageByPlayer.Clear();
            }

            if (attacker == player)
            {
                await player.AfterKilledMonsterAsync().ConfigureAwait(false);
            }

            if (player.GameContext.PlugInManager.GetPlugInPoint<IAttackableGotKilledPlugIn>() is { } plugInPoint)
            {
                await plugInPoint.AttackableGotKilledAsync(this, attacker).ConfigureAwait(false);
            }

            if (!this.IsSummonedMonster && player.SelectedCharacter is { } selectedCharacter)
            {
                if (selectedCharacter.State > HeroState.Normal)
                {
                    selectedCharacter.StateRemainingSeconds -= (int)this.Attributes[Stats.Level];
                }
            }

            if (!this.IsSummonedMonster && dropOwner.SelectedCharacter is not null
                && !this.IsDropSuppressedByResetPenalty(dropOwner, experienceShares))
            {
                _ = this.DropItemDelayedAsync(dropOwner, experienceShares); // don't wait for completion.
            }
        }
    }

    /// <summary>
    /// Determines whether the drop is suppressed by the <see cref="ResetPenaltyPlugIn"/>, because a receiver
    /// of the drop group has outgrown the tier of this monster.
    /// </summary>
    /// <param name="dropOwner">The player who owns the drop.</param>
    /// <param name="experienceShares">The experience shares of the drop group.</param>
    /// <returns><c>true</c>, if nothing should be dropped.</returns>
    private bool IsDropSuppressedByResetPenalty(Player dropOwner, IReadOnlyList<ExperienceShare> experienceShares)
    {
        if (dropOwner.GameContext.FeaturePlugIns.GetPlugIn<ResetPenaltyPlugIn>() is not { } resetPenalty)
        {
            return false;
        }

        var receivers = experienceShares.Count > 0
            ? experienceShares.Select(share => share.Player)
            : dropOwner.GetAsEnumerable();
        return resetPenalty.ShouldSuppressDrop(this, receivers);
    }

    private async ValueTask RemoveFromMapAndDisposeAsync()
    {
        await this.CurrentMap.RemoveAsync(this).ConfigureAwait(false);
        this.Dispose();
        this.OnRemoveFromMap();
    }

    /// <summary>
    /// Respawns this instance on the map.
    /// </summary>
    private async ValueTask RespawnAsync()
    {
        try
        {
            if (!this.ShouldRespawn)
            {
                await this.RemoveFromMapAndDisposeAsync().ConfigureAwait(false);
                return;
            }

            this.Initialize();
            await this.CurrentMap.RespawnAsync(this).ConfigureAwait(false);
            this.OnSpawn();
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message, ex.StackTrace);
        }
    }

    private bool TryHit(uint damage, IAttacker attacker)
    {
        if (this.DamageLimiter is { } limiter)
        {
            lock (limiter)
            {
                return this.ApplyHit(limiter.LimitDamage(this, damage), attacker);
            }
        }

        return this.ApplyHit(damage, attacker);
    }

    private bool ApplyHit(uint damage, IAttacker attacker)
    {
        if (damage > 0)
        {
            this.RegisterHit(attacker);
            this.RecordDamage(attacker, damage);
        }

        if (damage >= this.Health)
        {
            this.IsAlive = false;
            this.Health = 0;
            return true;
        }

        try
        {
            Interlocked.Add(ref this._health, -(int)damage);
            return false;
        }
        catch
        {
            return false;
        }
    }

    private void RecordDamage(IAttacker attacker, uint damage)
    {
        if (this.GetHitNotificationTarget(attacker) is not { } player)
        {
            return;
        }

        // Overkill damage of the last hit doesn't count.
        var effectiveDamage = Math.Min(damage, (uint)this.Health);
        if (effectiveDamage == 0)
        {
            return;
        }

        lock (this._damageByPlayer)
        {
            this._damageByPlayer[player] = this._damageByPlayer.GetValueOrDefault(player) + effectiveDamage;
        }
    }

    /// <summary>
    /// Distributes the experience between the groups (single players or parties) in proportion to their damage.
    /// </summary>
    /// <returns>
    /// The experience shares of the group with the highest damage and the player which owns the drop,
    /// or <c>null</c> if there is no group with a representative (then the default distribution is used).
    /// </returns>
    private async ValueTask<(IReadOnlyList<ExperienceShare> DropShares, Player DropOwner)?> TryDistributeExperienceByDamageAsync()
    {
        List<KillRewardGroup> groups;
        lock (this._damageByPlayer)
        {
            groups = KillRewardGroup.Create(this._damageByPlayer.ToList(), this.CurrentMap);
        }

        var totalDamage = groups.Sum(g => g.Damage);
        if (totalDamage <= 0)
        {
            return null;
        }

        KillRewardGroup? topGroup = null;
        IReadOnlyList<ExperienceShare> topShares = [];
        foreach (var group in groups)
        {
            var damageShare = (double)group.Damage / totalDamage;
            var representative = group.Representative!;
            IReadOnlyList<ExperienceShare> shares = group.Party is { } party
                ? await party.DistributeExperienceAfterKillAsync(this, representative, damageShare).ConfigureAwait(false)
                : [new ExperienceShare(representative, await representative.AddExpAfterKillAsync(this, damageShare).ConfigureAwait(false))];

            if (topGroup is null || group.Damage > topGroup.Damage)
            {
                topGroup = group;
                topShares = shares;
            }
        }

        return (topShares, topGroup!.Representative!);
    }

    private async ValueTask HandleMoneyDropAsync(uint amount, Player killer, IReadOnlyList<ExperienceShare> experienceShares, bool splitEqually)
    {
        // By default, each player gets the part of the money which matches the experience they gained from the kill,
        // so that money follows the same distribution as the experience it is derived from.
        // A plugin can request an equal split instead.
        var shares = splitEqually
            ? MoneyDistribution.CreateEqualShares(amount, experienceShares.Select(share => share.Player).ToList())
            : MoneyDistribution.CreateShares(amount, experienceShares);

        // We don't drop money in Devil Square, etc.
        var shouldDropMoney = killer.GameContext.Configuration.ShouldDropMoney && killer.CurrentMiniGame is null or { IsHuntingGround: true };
        if (!shouldDropMoney)
        {
            if (killer.Party is { } party)
            {
                await party.DistributeMoneyAfterKillAsync(this, killer, shares).ConfigureAwait(false);
            }
            else
            {
                _ = MoneyDistribution.TryPay(killer, amount);
            }

            return;
        }

        // Zen auto loot: straight into the inventory of the owner of the drop (or its party, by the shares). What doesn't
        // fit (maximum money) goes to the other party members which have room; what nobody can take drops on the ground.
        if (killer.GameContext.FeaturePlugIns.GetPlugIn<ZenAutoLootPlugIn>() is { } autoLoot && autoLoot.IsActiveFor(killer))
        {
            var rest = killer.Party is { } ownerParty
                ? MoneyDistribution.PaySharesWithOverflow(shares, member => ownerParty.IsEligibleForMoney(member, killer))
                : MoneyDistribution.PayAsMuchAsFits(killer, amount);
            if (rest == 0)
            {
                return;
            }

            if (rest < amount)
            {
                shares = MoneyDistribution.ScaleShares(shares, rest);
                amount = rest;
            }
        }

        var owners = killer.Party?.PartyList.AsEnumerable() ?? killer.GetAsEnumerable();
        var droppedMoney = new DroppedMoney(amount, this.Position, this.CurrentMap, shares, owners);
        await this.CurrentMap.AddAsync(droppedMoney).ConfigureAwait(false);
    }

    private async ValueTask DropItemAsync(IReadOnlyList<ExperienceShare> experienceShares, Player killer)
    {
        if (killer.CurrentMiniGame?.GetMonsterDrops(this, killer) is { } ownDrops)
        {
            // The game (e.g. the Kalima instance) has its own drops instead of the regular ones.
            if (ownDrops.Money > 0)
            {
                await this.HandleMoneyDropAsync(ownDrops.Money, killer, experienceShares, true).ConfigureAwait(false);
            }

            await this.DropItemsAtAsync(ownDrops.Items, killer, ownDrops.Money == 0).ConfigureAwait(false);
            return;
        }

        var exp = 0;
        foreach (var share in experienceShares)
        {
            exp += share.Experience;
        }

        var (generatedItems, droppedMoney) = await this._dropGenerator.GenerateItemDropsAsync(this.Definition, exp, killer).ConfigureAwait(false);
        if (droppedMoney > 0)
        {
            var moneyArgs = new MoneyDropCalculationArgs(
                this,
                this.Definition,
                this.CurrentMap,
                killer,
                experienceShares.Select(share => share.Player).ToList(),
                droppedMoney.Value);
            if (this._plugInManager?.GetPlugInPoint<IMoneyDropCalculationPlugIn>() is { } moneyPlugIn)
            {
                await moneyPlugIn.CalculateMoneyDropAsync(moneyArgs).ConfigureAwait(false);
            }

            if (moneyArgs.Amount > 0)
            {
                await this.HandleMoneyDropAsync(moneyArgs.Amount, killer, experienceShares, moneyArgs.SplitEqually).ConfigureAwait(false);
            }
        }

        var items = new List<Item>(generatedItems);
        await this.AddAdditionalItemDropsAsync(killer, items).ConfigureAwait(false);

        // Mini games like the Kalima instance multiply the drop by rolling it again.
        var additionalRolls = GetAdditionalDropRolls(killer.CurrentMiniGame?.ItemDropMultiplier ?? 1.0);
        for (var roll = 0; roll < additionalRolls; roll++)
        {
            var (rolledItems, _) = await this._dropGenerator.GenerateItemDropsAsync(this.Definition, exp, killer).ConfigureAwait(false);
            var rolled = new List<Item>(rolledItems);
            await this.AddAdditionalItemDropsAsync(killer, rolled).ConfigureAwait(false);
            items.AddRange(rolled);
        }

        await this.DropItemsAtAsync(items, killer, !droppedMoney.HasValue).ConfigureAwait(false);
    }

    private async ValueTask DropItemsAtAsync(IReadOnlyList<Item> items, Player killer, bool firstItemOnPosition)
    {
        var firstItem = firstItemOnPosition;
        foreach (var item in items)
        {
            Point dropCoordinates;
            if (firstItem)
            {
                dropCoordinates = this.Position;
                firstItem = false;
            }
            else
            {
                dropCoordinates = this.CurrentMap.Terrain.GetRandomCoordinate(this.Position, MaximumDropDistance);
            }

            var droppedItem = killer.CreateDropForKiller(item, dropCoordinates, this.CurrentMap);
            await this.CurrentMap.AddAsync(droppedItem).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the number of additional drop rolls for a drop multiplier.
    /// The integral part (minus the regular roll) is guaranteed, the fractional part is a chance for one more roll.
    /// </summary>
    /// <param name="multiplier">The drop multiplier.</param>
    /// <returns>The number of additional drop rolls.</returns>
    private static int GetAdditionalDropRolls(double multiplier)
    {
        if (multiplier <= 1.0)
        {
            return 0;
        }

        var rolls = (int)Math.Floor(multiplier) - 1;
        var fraction = multiplier - Math.Floor(multiplier);
        if (fraction > 0 && Rand.NextRandomBool(fraction))
        {
            rolls++;
        }

        return rolls;
    }

    private async ValueTask AddAdditionalItemDropsAsync(Player killer, List<Item> items)
    {
        if (this._plugInManager?.GetPlugInPoint<IAdditionalItemDropPlugIn>() is { } additionalDropPlugIn)
        {
            var dropArgs = new AdditionalItemDropArgs(this, this.Definition, this.CurrentMap, killer, items);
            await additionalDropPlugIn.AddItemDropsAsync(dropArgs).ConfigureAwait(false);
        }
    }

    private async ValueTask DropItemDelayedAsync(Player player, IReadOnlyList<ExperienceShare> experienceShares)
    {
        try
        {
            await Task.Delay(1000).ConfigureAwait(false);
            await this.DropItemAsync(experienceShares, player).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogDebug(ex, "Dropping an item failed after killing '{this}': {ex}", this, ex);
        }
    }
}
