// <copyright file="KalimaInstanceContextTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests for <see cref="KalimaInstanceContext"/> with the real data of Kalima 1.
/// </summary>
[TestFixture]
public class KalimaInstanceContextTest
{
    private static readonly MethodInfo OnMonsterDiedMethod = typeof(KalimaInstanceContext)
        .GetMethod("OnMonsterDied", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// The packs come one after another, with the strength of the tier, and the boss comes after the last pack.
    /// </summary>
    [Test]
    public async ValueTask PacksAndBossAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        await new Persistence.Initialization.VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var gameConfiguration = (await contextProvider.CreateNewConfigurationContext().GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var definition = gameConfiguration.MiniGameDefinitions.Single(d => d.Type == MiniGameType.KalimaInstance && d.GameLevel == 1);

        var mapInitializer = new MapInitializer(gameConfiguration, new NullLogger<MapInitializer>(), NullDropGenerator.Instance, null);
        var plugInManager = new PlugInManager(new List<PlugInConfiguration>(), new NullLoggerFactory(), null, null);
        var gameContext = new GameContext(gameConfiguration, contextProvider, mapInitializer, new NullLoggerFactory(), plugInManager, NullDropGenerator.Instance, new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameContext.PlugInManager;
        mapInitializer.PathFinderPool = gameContext.PathFinderPool;

        var configuration = new KalimaInstanceConfiguration();
        var tier = configuration.GetTier(1)!;
        var monsters = new List<Monster>();
        var instance = new KalimaInstanceContext(new MiniGameMapKey(definition.Entrance!.Map!.Number, 1, "Test"), definition, gameContext, mapInitializer, configuration);
        try
        {
            instance.Map.ObjectAdded += args =>
            {
                if (args.Object is Monster monster)
                {
                    lock (monsters)
                    {
                        monsters.Add(monster);
                    }
                }

                return ValueTask.CompletedTask;
            };

            await instance.InitializeMapStateAsync().ConfigureAwait(false);

            for (var pack = 1; pack <= configuration.PackCount; pack++)
            {
                var packMonsters = await WaitForMonstersAsync(monsters, pack * configuration.MonstersPerPack).ConfigureAwait(false);
                Assert.That(packMonsters, Has.Count.EqualTo(pack * configuration.MonstersPerPack), $"pack {pack}");
                var currentPack = packMonsters.Skip((pack - 1) * configuration.MonstersPerPack).ToList();
                Assert.That(currentPack.All(m => !configuration.BossMonsterNumbers.Contains(m.Definition.Number)), Is.True);
                Assert.That(currentPack.All(m => m.Health == (int)m.Attributes[Stats.MaximumHealth]), Is.True);

                foreach (var monster in currentPack)
                {
                    OnMonsterDiedMethod.Invoke(instance, [monster, new DeathInformation(0, "Test", default, 0)]);
                }
            }

            var all = await WaitForMonstersAsync(monsters, (configuration.PackCount * configuration.MonstersPerPack) + 1).ConfigureAwait(false);
            var boss = all.Last();
            var regular = all.Take(all.Count - 1).ToList();

            Assert.That(configuration.BossMonsterNumbers, Does.Contain(boss.Definition.Number));
            var strength = instance.Strength!;
            var reference = KalimaStrengthCalculator.GetReferenceStrength(gameConfiguration, tier.ReferenceMapNumber)!;
            Assert.That(strength.Health, Is.EqualTo(reference.Health * tier.HealthFactor).Within(1), "the first tier only depends on its reference map");
            Assert.That(strength.Level, Is.EqualTo(reference.Level * tier.LevelFactor).Within(0.01));
            var averageHealth = regular.Average(m => m.Attributes[Stats.MaximumHealth]);
            Assert.That(averageHealth, Is.EqualTo(strength.Health).Within(strength.Health * 0.5), "the packs have about the health of the tier");
            Assert.That(boss.Attributes[Stats.MaximumHealth], Is.EqualTo(strength.Health * configuration.BossHealthFactor).Within(strength.Health * 0.01));
            Assert.That(regular.Average(m => m.Attributes[Stats.Level]), Is.EqualTo(strength.Level).Within(strength.Level * 0.3));

            var allTiers = KalimaStrengthCalculator.Calculate(gameConfiguration, configuration);
            Assert.That(allTiers.Keys, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7 }));
            for (var level = 2; level <= 7; level++)
            {
                Assert.That(allTiers[level].Level, Is.GreaterThanOrEqualTo(allTiers[level - 1].Level * 1.05f).Within(0.01f));
                Assert.That(allTiers[level].Health, Is.GreaterThanOrEqualTo(allTiers[level - 1].Health * 1.05f).Within(1f));
                TestContext.Out.WriteLine($"Kalima {level - 1}: {allTiers[level - 1]}");
            }

            TestContext.Out.WriteLine($"Kalima 7: {allTiers[7]}");
        }
        finally
        {
            await instance.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async ValueTask<List<Monster>> WaitForMonstersAsync(List<Monster> monsters, int count)
    {
        for (var i = 0; i < 100; i++)
        {
            lock (monsters)
            {
                if (monsters.Count >= count)
                {
                    return monsters.ToList();
                }
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        lock (monsters)
        {
            return monsters.ToList();
        }
    }
}
