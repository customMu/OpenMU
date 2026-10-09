// <copyright file="MinimapSpotsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sends the monster spots of a map to the client when a player entered it, so that the minimap shows them.
/// Spawn areas of the same monster close to each other are shown as one spot.
/// </summary>
[PlugIn]
[Display(Name = "Minimap monster spots", Description = "Shows the monster spots of the map on the minimap of the custom client.")]
[Guid("A3C6E0F1-4B27-4D98-8E15-6F2B9C7D1A54")]
public class MinimapSpotsPlugIn : IObjectAddedToMapPlugIn
{
    /// <summary>
    /// The distance in fields within which spawn areas of the same monster are merged into one spot.
    /// </summary>
    private const int MergeDistance = 6;

    private const int MaximumSpots = 400;

    /// <summary>
    /// The bosses of the hunting maps (role "boss" of tools/balance/mob-layout.csv): their spots are marked on the minimap.
    /// </summary>
    private static readonly HashSet<short> BossMonsters = [38, 49, 59, 63, 77, 161, 181, 189, 197, 267, 275, 295, 309, 338, 459];

    /// <summary>
    /// The maps without spots: the monsters of the Illusion of Noria (82) roam over the whole map.
    /// </summary>
    private static readonly HashSet<short> MapsWithoutSpots = [82];

    private readonly ConditionalWeakTable<GameMapDefinition, IReadOnlyList<MinimapSpot>> _spotsByMap = new();

    /// <summary>
    /// Calculates the spots of a map.
    /// </summary>
    /// <param name="map">The map definition.</param>
    /// <returns>The spots.</returns>
    public static IReadOnlyList<MinimapSpot> CalculateSpots(GameMapDefinition map)
    {
        if (MapsWithoutSpots.Contains(map.Number))
        {
            return []; // an empty list clears the spots of the previous map on the client
        }

        var clusters = new List<(MonsterDefinition Monster, double SumX, double SumY, int Weight, int Count)>();
        foreach (var area in map.MonsterSpawns.Where(a => a is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster }))
        {
            var monster = area.MonsterDefinition!;
            var x = (area.X1 + area.X2) / 2.0;
            var y = (area.Y1 + area.Y2) / 2.0;
            var index = clusters.FindIndex(c => c.Monster == monster
                                                && Math.Abs((c.SumX / c.Weight) - x) <= MergeDistance
                                                && Math.Abs((c.SumY / c.Weight) - y) <= MergeDistance);
            if (index >= 0)
            {
                var c = clusters[index];
                clusters[index] = (monster, c.SumX + x, c.SumY + y, c.Weight + 1, c.Count + area.Quantity);
            }
            else
            {
                clusters.Add((monster, x, y, 1, area.Quantity));
            }
        }

        return clusters
            .Take(MaximumSpots)
            .Select(c => new MinimapSpot(
                (byte)Math.Round(c.SumX / c.Weight),
                (byte)Math.Round(c.SumY / c.Weight),
                c.Monster.Designation.ValueInNeutralLanguage,
                (int)(c.Monster.Attributes.FirstOrDefault(a => a.AttributeDefinition?.Id == Stats.Level.Id)?.Value ?? 0),
                c.Count,
                BossMonsters.Contains(c.Monster.Number)))
            .ToList();
    }

    /// <inheritdoc />
    public async ValueTask ObjectAddedToMapAsync(GameMap map, ILocateable addedObject)
    {
        if (addedObject is not Player player || player.ViewPlugIns.GetPlugIn<IMinimapSpotsViewPlugIn>() is not { } view)
        {
            return;
        }

        var spots = this._spotsByMap.GetValue(map.Definition, CalculateSpots);
        await view.ShowSpotsAsync(spots).ConfigureAwait(false);
    }
}
