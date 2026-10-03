// <copyright file="KalimaPackPlannerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests for <see cref="KalimaPackPlanner"/>.
/// </summary>
[TestFixture]
public class KalimaPackPlannerTest
{
    /// <summary>
    /// The packs follow the walking distance, not the air-line distance:
    /// in a U-shaped corridor, the end of the U is the last pack, although it's close to the start.
    /// </summary>
    [Test]
    public void PacksFollowTheWay()
    {
        var walkMap = new bool[256, 256];
        for (var i = 10; i <= 50; i++)
        {
            walkMap[i, 10] = true; // top, going right
            walkMap[50, i] = true; // right side, going down
            walkMap[i, 50] = true; // bottom, going left
        }

        var start = new Point(10, 10);
        var first = CreateSpawn(30, 10);
        var second = CreateSpawn(50, 30);
        var third = CreateSpawn(30, 50);
        var last = CreateSpawn(10, 50); // close to the start by air, but at the end of the way

        var packs = KalimaPackPlanner.PlanPacks([last, third, first, second], start, walkMap, 4);

        Assert.That(packs.Select(p => p.Single()), Is.EqualTo(new[] { first, second, third, last }));
    }

    /// <summary>
    /// The spawn points are split into the requested number of consecutive packs.
    /// </summary>
    [Test]
    public void SplitsIntoPacks()
    {
        var walkMap = new bool[256, 256];
        var spawns = new List<MonsterSpawnArea>();
        for (byte x = 0; x < 60; x++)
        {
            walkMap[x, 0] = true;
            spawns.Add(CreateSpawn(x, 0));
        }

        var packs = KalimaPackPlanner.PlanPacks(spawns, new Point(0, 0), walkMap, 10);

        Assert.That(packs, Has.Count.EqualTo(10));
        Assert.That(packs.Select(p => p.Count), Is.All.EqualTo(6));
        Assert.That(packs.SelectMany(p => p), Is.EqualTo(spawns));
    }

    /// <summary>
    /// The real Kalima maps: the packs lead from the entrance towards the Illusion of Kundun,
    /// and the data initialization creates the mini games and removes the regular symbol drops.
    /// </summary>
    [Test]
    public async ValueTask RealKalimaMapsAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        await new Persistence.Initialization.VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var configuration = (await contextProvider.CreateNewConfigurationContext().GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var kalimaConfiguration = new KalimaInstanceConfiguration();

        var definitions = configuration.MiniGameDefinitions.Where(d => d.Type == MiniGameType.KalimaInstance).OrderBy(d => d.GameLevel).ToList();
        Assert.That(definitions.Select(d => (int)d.GameLevel), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(configuration.Maps.SelectMany(m => m.DropItemGroups).SelectMany(g => g.PossibleItems).Any(i => i.Group == 14 && i.Number == 29), Is.False);

        foreach (var definition in definitions)
        {
            var map = definition.Entrance!.Map!;
            var regularSpawns = map.MonsterSpawns
                .Where(s => s is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster }
                            && !kalimaConfiguration.BossMonsterNumbers.Contains(s.MonsterDefinition!.Number))
                .ToList();
            var boss = map.MonsterSpawns.Single(s => kalimaConfiguration.BossMonsterNumbers.Contains(s.MonsterDefinition!.Number));
            var entrance = new Point((byte)((definition.Entrance.X1 + definition.Entrance.X2) / 2), (byte)((definition.Entrance.Y1 + definition.Entrance.Y2) / 2));
            var terrain = new GameMapTerrain(map);

            var packs = KalimaPackPlanner.PlanPacks(regularSpawns, entrance, terrain.WalkMap, kalimaConfiguration.PackCount);
            var distances = KalimaPackPlanner.CalculateWalkingDistances(entrance, terrain.WalkMap);

            Assert.That(packs, Has.Count.EqualTo(10), map.Name.ToString());
            Assert.That(packs.SelectMany(p => p).All(s => distances[s.X1, s.Y1] != int.MaxValue), Is.True, $"{map.Name}: unreachable spawn");
            Assert.That(distances[boss.X1, boss.Y1], Is.Not.EqualTo(int.MaxValue), $"{map.Name}: unreachable boss");

            var firstPackDistance = packs[0].Max(s => distances[s.X1, s.Y1]);
            var lastPackDistance = packs[^1].Min(s => distances[s.X1, s.Y1]);
            Assert.That(firstPackDistance, Is.LessThan(lastPackDistance), map.Name.ToString());
        }
    }

    private static MonsterSpawnArea CreateSpawn(byte x, byte y) => new() { X1 = x, X2 = x, Y1 = y, Y2 = y };
}
