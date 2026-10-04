// <copyright file="SpotOccupancyTracker.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Tracks which monster spots of the hunting maps are occupied by players, for the website: a player occupies a spot
/// when it stays within <see cref="SpotRadius"/> fields of the spot for at least <see cref="MinimumStay"/>.
/// The positions are sampled every few seconds, starting with the first query. Invisible players and players in
/// mini games are not counted; no names are given out.
/// </summary>
public sealed class SpotOccupancyTracker
{
    /// <summary>
    /// The distance in fields from the center of a spot, within which a player is on the spot.
    /// </summary>
    public const int SpotRadius = 6;

    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(5);

    private static readonly ConditionalWeakTable<IGameContext, SpotOccupancyTracker> Trackers = new();

    private readonly IGameContext _gameContext;
    private readonly ConcurrentDictionary<Player, (short Map, int Spot, DateTime Since)> _positions = new();
    private readonly ConcurrentDictionary<GameMapDefinition, IReadOnlyList<MinimapSpot>> _spots = new();
    private readonly Timer _timer;
    private int _sampling;

    private SpotOccupancyTracker(IGameContext gameContext)
    {
        this._gameContext = gameContext;
        this._timer = new Timer(_ => _ = this.SampleAsync(), null, TimeSpan.Zero, SampleInterval);
    }

    /// <summary>
    /// Gets the time a player has to stay on a spot to occupy it.
    /// </summary>
    public static TimeSpan MinimumStay { get; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets the tracker of the game context; it starts sampling on the first call.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The tracker.</returns>
    public static SpotOccupancyTracker For(IGameContext gameContext) => Trackers.GetValue(gameContext, context => new SpotOccupancyTracker(context));

    /// <summary>
    /// Gets the spots of the map with the players which occupy them.
    /// </summary>
    /// <param name="map">The map definition.</param>
    /// <returns>The spots.</returns>
    public IReadOnlyList<SpotState> GetSpots(GameMapDefinition map)
    {
        var spots = this.GetMapSpots(map);
        var now = DateTime.UtcNow;
        var occupants = this._positions.Values
            .Where(p => p.Map == map.Number && p.Spot >= 0 && now - p.Since >= MinimumStay)
            .GroupBy(p => p.Spot)
            .ToDictionary(g => g.Key, g => (Players: g.Count(), Since: g.Min(p => p.Since)));
        return spots
            .Select((spot, index) => occupants.TryGetValue(index, out var occupant)
                ? new SpotState(spot, occupant.Players, (int)(now - occupant.Since).TotalSeconds)
                : new SpotState(spot, 0, 0))
            .ToList();
    }

    private IReadOnlyList<MinimapSpot> GetMapSpots(GameMapDefinition map) => this._spots.GetOrAdd(map, MinimapSpotsPlugIn.CalculateSpots);

    private async Task SampleAsync()
    {
        if (Interlocked.Exchange(ref this._sampling, 1) == 1)
        {
            return;
        }

        try
        {
            var players = await this._gameContext.GetPlayersAsync().ConfigureAwait(false);
            var now = DateTime.UtcNow;
            foreach (var player in players)
            {
                if (player.CurrentMap?.Definition is not { } map
                    || player.CurrentMiniGame is not null
                    || player.IsInvisible
                    || !player.IsAlive)
                {
                    this._positions.TryRemove(player, out _);
                    continue;
                }

                var spot = this.FindSpot(map, player.Position);
                var previous = this._positions.TryGetValue(player, out var p) ? p : default;
                var since = previous.Map == map.Number && previous.Spot == spot && previous.Since != default ? previous.Since : now;
                this._positions[player] = (map.Number, spot, since);
            }

            foreach (var gone in this._positions.Keys.Except(players).ToList())
            {
                this._positions.TryRemove(gone, out _);
            }
        }
        catch (Exception ex)
        {
            this._gameContext.LoggerFactory.CreateLogger<SpotOccupancyTracker>().LogDebug(ex, "Sampling the positions of the players failed.");
        }
        finally
        {
            Interlocked.Exchange(ref this._sampling, 0);
        }
    }

    private int FindSpot(GameMapDefinition map, Pathfinding.Point position)
    {
        var spots = this.GetMapSpots(map);
        var best = -1;
        var bestDistance = int.MaxValue;
        for (var i = 0; i < spots.Count; i++)
        {
            var distance = Math.Max(Math.Abs(spots[i].X - position.X), Math.Abs(spots[i].Y - position.Y));
            if (distance <= SpotRadius && distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }
}

/// <summary>
/// A monster spot with the players which occupy it.
/// </summary>
/// <param name="Spot">The spot.</param>
/// <param name="Players">The number of players which stay on the spot for at least the minimum time.</param>
/// <param name="OccupiedSeconds">The seconds since the first of these players arrived.</param>
public sealed record SpotState(MinimapSpot Spot, int Players, int OccupiedSeconds);
