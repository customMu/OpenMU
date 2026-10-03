// <copyright file="KalimaInstanceContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// The context of a Kalima instance. It's a regular Kalima map for a party or a single player,
/// which runs for the configured time. The monsters are scaled by the tier and by the number of players.
/// Players can leave and enter again (e.g. after a death) as long as the instance is running.
/// </summary>
public sealed class KalimaInstanceContext : MiniGameContext
{
    private static readonly TimeSpan MinimumPhaseDuration = TimeSpan.FromSeconds(30);

    private static readonly int[] NoticeMinutes = [30, 10, 5, 1];

    private readonly KalimaInstanceConfiguration _configuration;
    private readonly SimpleElement _healthMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _defenseMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _damageMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly HashSet<string> _registeredCharacters = new(StringComparer.Ordinal);
    private readonly HashSet<Player> _playersOnMap = new();
    private readonly HashSet<AttackableNpcBase> _scaledMonsters = new();
    private readonly object _syncRoot = new();
    private readonly DateTime _endsAt;

    private double _dropMultiplier = 1.0;

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
        this.Tier = configuration.GetTier(definition.GameLevel) ?? new KalimaInstanceTier { Level = definition.GameLevel };

        // The base class waits for the entering players and a countdown, before the game duration starts.
        // The instance is playable during that time, so the whole time counts.
        this._endsAt = DateTime.UtcNow
                       + definition.EnterDuration.AtLeast(MinimumPhaseDuration)
                       + MinimumPhaseDuration
                       + definition.GameDuration.AtLeast(MinimumPhaseDuration);

        this.UpdateScaling(0);
        _ = Task.Run(() => this.RunTimeNoticesAsync(this.GameEndedToken), this.GameEndedToken);
    }

    /// <summary>
    /// Gets the tier of this instance.
    /// </summary>
    public KalimaInstanceTier Tier { get; }

    /// <summary>
    /// Gets the remaining time until the instance closes.
    /// </summary>
    public TimeSpan TimeLeft => (this._endsAt - DateTime.UtcNow).AtLeast(TimeSpan.Zero);

    /// <inheritdoc />
    public override bool IsHuntingGround => true;

    /// <inheritdoc />
    public override bool IgnoresResetPenalty => true;

    /// <inheritdoc />
    public override double ItemDropMultiplier => Volatile.Read(ref this._dropMultiplier);

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

    /// <inheritdoc />
    protected override bool IsEnteringAllowed(MiniGameState state) => state is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing;

    /// <inheritdoc />
    protected override async ValueTask OnObjectAddedToMapAsync((GameMap Map, ILocateable Object) args)
    {
        await base.OnObjectAddedToMapAsync(args).ConfigureAwait(false);

        switch (args.Object)
        {
            case Monster { SummonedBy: null } monster:
                this.ApplyScaling(monster);
                break;
            case Player player:
                int playerCount;
                lock (this._syncRoot)
                {
                    this._playersOnMap.Add(player);
                    playerCount = this._playersOnMap.Count;
                }

                this.UpdateScaling(playerCount);
                await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.KalimaInstanceStatusFormat),
                    this.Tier.Level,
                    (int)Math.Ceiling(this.TimeLeft.TotalMinutes),
                    playerCount).ConfigureAwait(false);
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

        switch (args.Object)
        {
            case Player player:
                int playerCount;
                lock (this._syncRoot)
                {
                    this._playersOnMap.Remove(player);
                    playerCount = this._playersOnMap.Count;
                }

                this.UpdateScaling(playerCount);
                break;
            case AttackableNpcBase monster:
                lock (this._syncRoot)
                {
                    this._scaledMonsters.Remove(monster);
                }

                break;
            default:
                // nothing to do
                break;
        }
    }

    private void ApplyScaling(AttackableNpcBase monster)
    {
        lock (this._syncRoot)
        {
            if (!this._scaledMonsters.Add(monster))
            {
                return;
            }
        }

        monster.Attributes.AddElement(this._healthMultiplier, Stats.MaximumHealth);
        monster.Attributes.AddElement(this._defenseMultiplier, Stats.DefenseBase);
        monster.Attributes.AddElement(this._damageMultiplier, Stats.AttackDamageIncrease);
        monster.Health = (int)monster.Attributes[Stats.MaximumHealth];
    }

    /// <summary>
    /// Updates the multipliers for the specified player count.
    /// The current health of the living monsters is adapted, so their health bar stays at the same percentage.
    /// </summary>
    /// <param name="playerCount">The player count.</param>
    private void UpdateScaling(int playerCount)
    {
        var configuration = this._configuration;
        var tier = this.Tier;
        var newHealth = tier.HealthMultiplier * configuration.GetPlayerFactor(configuration.HealthPerPlayer, playerCount);
        var oldHealth = this._healthMultiplier.Value;

        this._defenseMultiplier.Value = tier.DefenseMultiplier * configuration.GetPlayerFactor(configuration.DefensePerPlayer, playerCount);
        this._damageMultiplier.Value = tier.DamageMultiplier * configuration.GetPlayerFactor(configuration.DamagePerPlayer, playerCount);
        Volatile.Write(ref this._dropMultiplier, tier.DropMultiplier * configuration.GetPlayerFactor(configuration.DropPerPlayer, playerCount));

        if (Math.Abs(newHealth - oldHealth) < 0.0001f)
        {
            return;
        }

        this._healthMultiplier.Value = newHealth;
        if (oldHealth <= 0)
        {
            return;
        }

        List<AttackableNpcBase> monsters;
        lock (this._syncRoot)
        {
            monsters = this._scaledMonsters.ToList();
        }

        var ratio = newHealth / oldHealth;
        foreach (var monster in monsters.Where(m => m.IsAlive))
        {
            monster.Health = Math.Max(1, (int)(monster.Health * ratio));
        }
    }

    private async Task RunTimeNoticesAsync(CancellationToken cancellationToken)
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
                await this.ForEachPlayerAsync(player => player.ShowLocalizedGoldenMessageAsync(
                    nameof(PlayerMessage.KalimaInstanceClosesInMinutesFormat),
                    minutes).AsTask()).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // the game ended
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "{context}: Unexpected error during the time notices.", this);
        }
    }
}
