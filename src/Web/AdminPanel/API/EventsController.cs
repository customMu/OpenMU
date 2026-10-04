// <copyright file="EventsController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// The state of the events of the game, for the website: which events run now, where, and when they start next.
/// </summary>
[Route("api/events")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Viewer)]
public class EventsController : Controller
{
    private readonly IDictionary<int, IGameServer> _gameServers;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventsController"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers.</param>
    public EventsController(IDictionary<int, IGameServer> gameServers) => this._gameServers = gameServers;

    /// <summary>
    /// Gets the events: invasions are active while at least one of their monsters lives; the mini games are active
    /// while their entry is open; the times are seconds from now.
    /// </summary>
    /// <returns>The events.</returns>
    [HttpGet]
    public async Task<IActionResult> GetEventsAsync()
    {
        if (this._gameServers.Values.OfType<GameServer>().FirstOrDefault() is not { } server)
        {
            return this.Ok(Array.Empty<EventState>());
        }

        var context = server.Context;
        var plugIns = context.PlugInManager;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, context.ServerTimeZone);
        var result = new List<EventState>();

        foreach (var invasion in plugIns.GetActivePlugInsOf<IPeriodicTaskPlugIn>().Where(p => IsInvasion(p.GetType())))
        {
            var monsters = (IReadOnlyList<GameLogic.NPC.Monster>)invasion.GetType().GetMethod("GetAliveMonsters")!.Invoke(invasion, [context])!;
            var maps = monsters
                .Select(m => m.CurrentMap?.Definition.Name.ValueInNeutralLanguage)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();
            var next = (DateTime?)invasion.GetType().GetMethod("GetNextStartLocal")!.Invoke(invasion, [context]);
            result.Add(new EventState(
                "invasion",
                GetName(invasion.GetType()),
                monsters.Count > 0 ? string.Join(", ", maps) : null,
                monsters.Count > 0,
                monsters.Count,
                ToSeconds(next, nowLocal),
                null));
        }

        await AddMiniGameAsync(result, context, MiniGameType.BloodCastle, "Blood Castle", "Devias").ConfigureAwait(false);
        await AddMiniGameAsync(result, context, MiniGameType.DevilSquare, "Devil Square", "Noria").ConfigureAwait(false);
        await AddMiniGameAsync(result, context, MiniGameType.Kanturu, "Kanturu Refinery Tower", "Kanturu").ConfigureAwait(false);

        if (plugIns.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<HappyHourPlugIn>().FirstOrDefault() is { } happyHour)
        {
            var multiplier = happyHour.Configuration?.ExperienceMultiplier ?? 1;
            result.Add(new EventState(
                "happy-hour",
                "Happy Hour",
                "All maps",
                happyHour.GetTaskState(context) == PeriodicTaskState.Started,
                null,
                ToSeconds(happyHour.GetNextStartLocal(context), nowLocal),
                $"EXP x{multiplier:0.##}"));
        }

        return this.Ok(result);
    }

    private static async ValueTask AddMiniGameAsync(List<EventState> result, IGameContext context, MiniGameType type, string name, string location)
    {
        var definition = context.Configuration.MiniGameDefinitions.Where(d => d.Type == type).OrderBy(d => d.GameLevel).FirstOrDefault();
        var strategy = context.PlugInManager.GetStrategy<MiniGameType, IPeriodicMiniGameStartPlugIn>(type);
        if (definition is null || strategy is null)
        {
            return;
        }

        var untilStart = await strategy.GetDurationUntilNextStartAsync(context, definition).ConfigureAwait(false);
        var isOpen = untilStart == TimeSpan.Zero;
        result.Add(new EventState(
            "minigame",
            name,
            location,
            isOpen,
            null,
            untilStart is { } duration && !isOpen ? (int)Math.Ceiling(duration.TotalSeconds) : null,
            isOpen ? "Entry open" : null));
    }

    private static bool IsInvasion(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseInvasionPlugIn<>))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetName(Type type)
    {
        var display = type.GetCustomAttribute<DisplayAttribute>();
        return display?.GetName() ?? type.Name;
    }

    private static int? ToSeconds(DateTime? nextLocal, DateTime nowLocal) =>
        nextLocal is { } next ? (int)Math.Max(0, Math.Ceiling((next - nowLocal).TotalSeconds)) : null;

    /// <summary>
    /// The state of an event.
    /// </summary>
    /// <param name="Kind">The kind: invasion, minigame or happy-hour.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Location">Where the event is (an invasion: the maps with living monsters; a mini game: the entry).</param>
    /// <param name="Active">Whether the event is active now.</param>
    /// <param name="Monsters">The living monsters of an invasion.</param>
    /// <param name="StartsInSeconds">The seconds until the next start, if known and not active.</param>
    /// <param name="Info">Additional information, e.g. the multiplier of the happy hour.</param>
    public sealed record EventState(string Kind, string Name, string? Location, bool Active, int? Monsters, int? StartsInSeconds, string? Info);
}
