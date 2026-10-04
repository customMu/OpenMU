// <copyright file="KalimaPackPlanner.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Plans the monster packs of a Kalima instance: the spawn points of the regular Kalima map are
/// ordered by their walking distance from the entrance and split into consecutive packs, so the
/// packs follow the way from the entrance to the Illusion of Kundun. Midway monsters (like the Aegis,
/// whose spots are next to the boss) are taken out of the way and put into the packs of a part of it.
/// </summary>
public static class KalimaPackPlanner
{
    private const int Unreachable = int.MaxValue;

    private static readonly (int X, int Y)[] Directions = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    /// <summary>
    /// Plans the packs.
    /// </summary>
    /// <param name="spawnPoints">The spawn points of the regular monsters.</param>
    /// <param name="entrance">The entrance point.</param>
    /// <param name="walkMap">The walk map of the terrain.</param>
    /// <param name="packCount">The number of packs.</param>
    /// <returns>The spawn points of each pack, in the order of the way.</returns>
    public static IReadOnlyList<IReadOnlyList<MonsterSpawnArea>> PlanPacks(IReadOnlyList<MonsterSpawnArea> spawnPoints, Point entrance, bool[,] walkMap, int packCount)
        => PlanPacks(spawnPoints, entrance, walkMap, packCount, _ => false, 0, 0);

    /// <summary>
    /// Plans the packs; the midway monsters appear in the packs between <paramref name="midwayFrom"/> and
    /// <paramref name="midwayTo"/> (shares of the way) at the spots of the way, instead of at their own spots.
    /// </summary>
    /// <param name="spawnPoints">The spawn points of the regular monsters.</param>
    /// <param name="entrance">The entrance point.</param>
    /// <param name="walkMap">The walk map of the terrain.</param>
    /// <param name="packCount">The number of packs.</param>
    /// <param name="isMidway">Determines whether the spawn point is one of a midway monster.</param>
    /// <param name="midwayFrom">The start of the packs with the midway monsters, as a share of the way.</param>
    /// <param name="midwayTo">The end of the packs with the midway monsters, as a share of the way.</param>
    /// <returns>The spawn points of each pack, in the order of the way.</returns>
    public static IReadOnlyList<IReadOnlyList<MonsterSpawnArea>> PlanPacks(IReadOnlyList<MonsterSpawnArea> spawnPoints, Point entrance, bool[,] walkMap, int packCount, Func<MonsterSpawnArea, bool> isMidway, double midwayFrom, double midwayTo)
    {
        var midway = spawnPoints.Where(isMidway).ToList();
        var way = spawnPoints.Where(area => !isMidway(area)).ToList();
        if (midway.Count == 0 || way.Count == 0)
        {
            return PlanWay(spawnPoints, entrance, walkMap, packCount);
        }

        // One midway pack per pack of the part of the way, at most one per spot of the way.
        var first = Math.Clamp((int)Math.Round(midwayFrom * packCount), 1, Math.Max(1, packCount - 1));
        var last = Math.Clamp((int)Math.Round(midwayTo * packCount) - 1, first, Math.Max(first, packCount - 1));
        var midwayPacks = Math.Min(last - first + 1, Math.Max(0, packCount - 1));
        var wayPacks = PlanWay(way, entrance, walkMap, packCount - midwayPacks);
        midwayPacks = Math.Min(midwayPacks, wayPacks.Count);

        // The midway monsters ambush the players at the spot of the pack they just killed,
        // after each way pack starting at the share 'midwayFrom' of the way.
        var monsters = midway.Select(area => area.MonsterDefinition).Distinct().ToList();
        var firstAmbush = Math.Clamp(first - 1, 0, wayPacks.Count - midwayPacks);
        var packs = new List<IReadOnlyList<MonsterSpawnArea>>(wayPacks.Count + midwayPacks);
        for (var pack = 0; pack < wayPacks.Count; pack++)
        {
            packs.Add(wayPacks[pack]);
            var ambush = pack - firstAmbush;
            if (ambush >= 0 && ambush < midwayPacks)
            {
                var monster = monsters[ambush % monsters.Count];
                packs.Add(wayPacks[pack].Select(area => new MonsterSpawnArea
                {
                    GameMap = area.GameMap,
                    MonsterDefinition = monster,
                    SpawnTrigger = area.SpawnTrigger,
                    Quantity = area.Quantity,
                    Direction = area.Direction,
                    X1 = area.X1,
                    X2 = area.X2,
                    Y1 = area.Y1,
                    Y2 = area.Y2,
                }).ToList());
            }
        }

        return packs;
    }

    private static IReadOnlyList<IReadOnlyList<MonsterSpawnArea>> PlanWay(IReadOnlyList<MonsterSpawnArea> spawnPoints, Point entrance, bool[,] walkMap, int packCount)
    {
        if (spawnPoints.Count == 0 || packCount <= 0)
        {
            return [];
        }

        var distances = CalculateWalkingDistances(entrance, walkMap);
        var ordered = spawnPoints
            .OrderBy(area => distances[area.X1, area.Y1])
            .ThenBy(area => DistanceSquared(entrance, area))
            .ToList();

        var count = Math.Min(packCount, ordered.Count);
        var packs = new List<IReadOnlyList<MonsterSpawnArea>>(count);
        for (var pack = 0; pack < count; pack++)
        {
            var start = pack * ordered.Count / count;
            var end = (pack + 1) * ordered.Count / count;
            packs.Add(ordered.GetRange(start, end - start));
        }

        return packs;
    }

    /// <summary>
    /// Calculates the walking distance of each tile from the start point, with a breadth-first search.
    /// </summary>
    /// <param name="start">The start point.</param>
    /// <param name="walkMap">The walk map.</param>
    /// <returns>The distances; <see cref="int.MaxValue"/> for unreachable tiles.</returns>
    public static int[,] CalculateWalkingDistances(Point start, bool[,] walkMap)
    {
        var width = walkMap.GetLength(0);
        var height = walkMap.GetLength(1);
        var distances = new int[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                distances[x, y] = Unreachable;
            }
        }

        var queue = new Queue<(int X, int Y)>();
        distances[start.X, start.Y] = 0;
        queue.Enqueue((start.X, start.Y));
        while (queue.TryDequeue(out var current))
        {
            var nextDistance = distances[current.X, current.Y] + 1;
            foreach (var (dx, dy) in Directions)
            {
                var x = current.X + dx;
                var y = current.Y + dy;
                if (x < 0 || y < 0 || x >= width || y >= height || !walkMap[x, y] || distances[x, y] != Unreachable)
                {
                    continue;
                }

                distances[x, y] = nextDistance;
                queue.Enqueue((x, y));
            }
        }

        return distances;
    }

    private static int DistanceSquared(Point point, MonsterSpawnArea area)
    {
        var dx = area.X1 - point.X;
        var dy = area.Y1 - point.Y;
        return (dx * dx) + (dy * dy);
    }
}
