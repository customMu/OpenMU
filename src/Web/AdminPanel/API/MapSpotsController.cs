// <copyright file="MapSpotsController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// The monster spots of the hunting maps with the players on them, for the maps of the website.
/// A spot is occupied when a player stays on it for at least 15 seconds; no names are given out.
/// </summary>
[Route("api/map-spots")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Viewer)]
public class MapSpotsController : Controller
{
    private readonly IDictionary<int, IGameServer> _gameServers;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapSpotsController"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers.</param>
    public MapSpotsController(IDictionary<int, IGameServer> gameServers) => this._gameServers = gameServers;

    /// <summary>
    /// Gets the game servers; every server has its own players and so its own occupied spots.
    /// </summary>
    /// <returns>The game servers.</returns>
    [HttpGet]
    public IActionResult GetServers()
    {
        var servers = this._gameServers.Values.OfType<GameServer>()
            .OrderBy(s => s.Id)
            .Select(s => new ServerDto(s.Id, s.Description, s.ServerState == ServerState.Started, s.CurrentConnections))
            .ToList();
        return this.Ok(servers);
    }

    /// <summary>
    /// Gets the spots of the map on the game server.
    /// </summary>
    /// <param name="server">The id of the game server.</param>
    /// <param name="map">The map number.</param>
    /// <returns>The spots, or "not found".</returns>
    [HttpGet("{server:int}/{map:int}")]
    public IActionResult GetSpots(int server, short map)
    {
        if (this._gameServers.Values.OfType<GameServer>().FirstOrDefault(s => s.Id == server) is not { } gameServer
            || gameServer.Context.Configuration.Maps.FirstOrDefault(m => m.Number == map) is not { } definition)
        {
            return this.NotFound();
        }

        var spots = SpotOccupancyTracker.For(gameServer.Context).GetSpots(definition)
            .Select(s => new SpotDto(s.Spot.X, s.Spot.Y, s.Spot.Name, s.Spot.Level, s.Spot.Count, s.Players, s.OccupiedSeconds))
            .ToList();
        return this.Ok(new MapDto(definition.Number, definition.Name.ValueInNeutralLanguage, spots));
    }

    /// <summary>
    /// A game server.
    /// </summary>
    /// <param name="Id">The id.</param>
    /// <param name="Name">The name (description).</param>
    /// <param name="Online">A value indicating whether the server is started.</param>
    /// <param name="Players">The number of connected players.</param>
    public sealed record ServerDto(int Id, string Name, bool Online, int Players);

    /// <summary>
    /// The spots of a map.
    /// </summary>
    /// <param name="Number">The map number.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Spots">The spots.</param>
    public sealed record MapDto(short Number, string Name, IReadOnlyList<SpotDto> Spots);

    /// <summary>
    /// A spot.
    /// </summary>
    /// <param name="X">The x coordinate of the center.</param>
    /// <param name="Y">The y coordinate of the center.</param>
    /// <param name="Monster">The name of the monster.</param>
    /// <param name="Level">The level of the monster.</param>
    /// <param name="Monsters">The number of monsters.</param>
    /// <param name="Players">The players which occupy the spot.</param>
    /// <param name="OccupiedSeconds">The seconds since the spot is occupied.</param>
    public sealed record SpotDto(byte X, byte Y, string Monster, int Level, int Monsters, int Players, int OccupiedSeconds);
}
