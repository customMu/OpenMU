// <copyright file="KundunChamberTest.cs" company="MUnique">
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
/// Tests for the chamber of Kundun.
/// </summary>
[TestFixture]
public class KundunChamberTest
{
    private static readonly MethodInfo OnMonsterDiedMethod = typeof(KundunChamberContext)
        .GetMethod("OnMonsterDied", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// The week starts on monday at the reset hour, the time before still belongs to the previous week.
    /// </summary>
    [Test]
    public void WeekStartsOnMondayAtResetHour()
    {
        var configuration = new KundunChamberConfiguration { WeeklyResetHour = 6, TimeZoneId = "UTC" };
        var saturday = configuration.GetWeekNumber(new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc));
        var mondayBeforeReset = configuration.GetWeekNumber(new DateTime(2026, 10, 5, 5, 59, 0, DateTimeKind.Utc));
        var mondayAtReset = configuration.GetWeekNumber(new DateTime(2026, 10, 5, 6, 0, 0, DateTimeKind.Utc));
        var sundayEvening = configuration.GetWeekNumber(new DateTime(2026, 10, 11, 23, 0, 0, DateTimeKind.Utc));
        var nextMonday = configuration.GetWeekNumber(new DateTime(2026, 10, 12, 6, 0, 0, DateTimeKind.Utc));

        Assert.That(mondayBeforeReset, Is.EqualTo(saturday));
        Assert.That(mondayAtReset, Is.EqualTo(saturday + 1));
        Assert.That(sundayEvening, Is.EqualTo(mondayAtReset));
        Assert.That(nextMonday, Is.EqualTo(mondayAtReset + 1));
    }

    /// <summary>
    /// The remaining time until the weekly reset.
    /// </summary>
    [Test]
    public void TimeUntilNextReset()
    {
        var configuration = new KundunChamberConfiguration { WeeklyResetHour = 6, TimeZoneId = "UTC" };
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 3, 6, 0, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromDays(2)));
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 5, 5, 0, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromHours(1)));
        Assert.That(configuration.GetTimeUntilNextReset(new DateTime(2026, 10, 5, 6, 0, 0, DateTimeKind.Utc)), Is.EqualTo(TimeSpan.FromDays(7)));
    }

    /// <summary>
    /// The effects of the phases by the seconds after the free seconds, with their caps.
    /// </summary>
    [Test]
    public void PhaseEffects()
    {
        var phases = new KundunChamberConfiguration().GetOrderedPhases();
        Assert.That(phases.Select(p => p.HealthThreshold), Is.EqualTo(new[] { 0.75f, 0.5f, 0.25f }));

        // a party which needed 90 seconds, i.e. 70 seconds after the free ones
        Assert.That(phases[0].CalculateEffects(70).Heal, Is.EqualTo(0.07f).Within(0.0001f));
        Assert.That(phases[1].CalculateEffects(70), Is.EqualTo((0.084f, 0.14f, 0f)).Using<(float, float, float)>((a, b) => Math.Abs(a.Item1 - b.Item1) < 0.0001f && Math.Abs(a.Item2 - b.Item2) < 0.0001f && Math.Abs(a.Item3 - b.Item3) < 0.0001f));
        Assert.That(phases[2].CalculateEffects(70).Damage, Is.EqualTo(0.21f).Within(0.0001f));

        // the caps
        Assert.That(phases[0].CalculateEffects(1000).Heal, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(phases[1].CalculateEffects(1000).Defense, Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(phases[2].CalculateEffects(1000), Is.EqualTo((0.25f, 0f, 0.5f)));

        // within the free seconds
        Assert.That(phases[2].CalculateEffects(-5), Is.EqualTo((0f, 0f, 0f)));
    }

    /// <summary>
    /// The initial data contains Kundun 1-7, the chambers and the keeper on a walkable tile in Lorencia.
    /// </summary>
    [Test]
    public async ValueTask InitialDataAsync()
    {
        var (gameConfiguration, _) = await CreateDataAsync().ConfigureAwait(false);
        var configuration = new KundunChamberConfiguration();
        for (var level = 1; level <= 7; level++)
        {
            var kundun = gameConfiguration.Monsters.Single(m => m.Number == configuration.GetKundunMonsterNumber(level));
            Assert.That(kundun.Attributes.Any(a => a.AttributeDefinition == Stats.MaximumHealth && a.Value > 0), Is.True, $"Kundun {level}");
            var chamber = gameConfiguration.MiniGameDefinitions.Single(d => d.Type == MiniGameType.KundunChamber && d.GameLevel == level);
            Assert.That(chamber.SaveRankingStatistics, Is.True);
            Assert.That(chamber.Entrance, Is.Not.Null);
        }

        var lorencia = gameConfiguration.Maps.Single(m => m.Number == 0);
        var keeper = lorencia.MonsterSpawns.Single(s => s.MonsterDefinition?.Number == configuration.KeeperNpcNumber);
        var terrain = new GameMapTerrain(lorencia);
        Assert.That(terrain.WalkMap[keeper.X1, keeper.Y1], Is.True, "the keeper stands on a walkable tile");
        Assert.That(terrain.SafezoneMap[keeper.X1, keeper.Y1], Is.True, "the keeper stands in the safezone");
    }

    /// <summary>
    /// Kundun stops at the threshold of a phase and is invulnerable until the Illusions are killed.
    /// Then he heals by the time which was needed, and a phase doesn't repeat after the healing.
    /// </summary>
    [Test]
    public async ValueTask PhasesAsync()
    {
        var (gameConfiguration, contextProvider) = await CreateDataAsync().ConfigureAwait(false);
        var definition = gameConfiguration.MiniGameDefinitions.Single(d => d.Type == MiniGameType.KundunChamber && d.GameLevel == 1);
        var mapInitializer = new MapInitializer(gameConfiguration, new NullLogger<MapInitializer>(), NullDropGenerator.Instance, null);
        var plugInManager = new PlugInManager(new List<PlugInConfiguration>(), new NullLoggerFactory(), null, null);
        var gameContext = new GameContext(gameConfiguration, contextProvider, mapInitializer, new NullLoggerFactory(), plugInManager, NullDropGenerator.Instance, new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameContext.PlugInManager;
        mapInitializer.PathFinderPool = gameContext.PathFinderPool;

        // 100 seconds after the free ones, so that the first phase heals 10 %.
        var configuration = new KundunChamberConfiguration { FreeSecondsPerPhase = -100 };
        var kalimaConfiguration = new KalimaInstanceConfiguration();
        var monsters = new List<Monster>();
        var chamber = new KundunChamberContext(new MiniGameMapKey(definition.Entrance!.Map!.Number, 1, "Test", MiniGameType.KundunChamber), definition, gameContext, mapInitializer, configuration, kalimaConfiguration);
        try
        {
            chamber.Map.ObjectAdded += args =>
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

            await chamber.InitializeMapStateAsync().ConfigureAwait(false);
            var kundun = chamber.Kundun!;
            Assert.That(kundun, Is.Not.Null);
            Assert.That(kundun.Definition.Number, Is.EqualTo(configuration.GetKundunMonsterNumber(1)));
            var maximumHealth = (int)kundun.Attributes[Stats.MaximumHealth];
            Assert.That(maximumHealth, Is.EqualTo(chamber.BossHealth * configuration.KundunHealthFactor).Within(chamber.BossHealth * 0.01));
            Assert.That(kundun.Health, Is.EqualTo(maximumHealth));

            // A hit to half of the health stops at 75 %.
            Hit(chamber, kundun, (uint)(maximumHealth / 2));
            Assert.That(kundun.Health, Is.EqualTo(maximumHealth * 0.75).Within(2));
            var threshold75 = kundun.Health;
            Assert.That(chamber.IsShielded, Is.True);
            Hit(chamber, kundun, 1000);
            Assert.That(kundun.Health, Is.EqualTo(threshold75), "Kundun is invulnerable during the phase");

            var illusions = await WaitForMonstersAsync(monsters, 2).ConfigureAwait(false);
            var illusion = illusions.Last();
            Assert.That(kalimaConfiguration.BossMonsterNumbers, Does.Contain(illusion.Definition.Number));
            Assert.That(illusion.Attributes[Stats.MaximumHealth], Is.EqualTo(chamber.BossHealth * 0.5f).Within(chamber.BossHealth * 0.01));

            OnMonsterDiedMethod.Invoke(chamber, [illusion, new DeathInformation(0, "Test", default, 0)]);
            await WaitUntilAsync(() => !chamber.IsShielded).ConfigureAwait(false);
            Assert.That(chamber.IsShielded, Is.False);
            Assert.That(kundun.Health, Is.EqualTo(threshold75 + (maximumHealth * 0.1)).Within(maximumHealth * 0.003), "Kundun healed about 10 % (plus the real time of the phase)");

            // The healing brought him above 75 % again, but the phase doesn't repeat: the next stop is 50 %.
            Hit(chamber, kundun, (uint)(kundun.Health - (maximumHealth * 0.7)));
            Assert.That(chamber.IsShielded, Is.False);
            Hit(chamber, kundun, (uint)maximumHealth);
            Assert.That(kundun.Health, Is.EqualTo(maximumHealth * 0.5).Within(2));
            Assert.That(chamber.IsShielded, Is.True);

            var secondPhase = await WaitForMonstersAsync(monsters, 4).ConfigureAwait(false);
            Assert.That(secondPhase, Has.Count.EqualTo(4), "the second phase has 2 Illusions");
        }
        finally
        {
            await chamber.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static void Hit(KundunChamberContext chamber, Monster kundun, uint damage)
    {
        lock (chamber)
        {
            var limited = chamber.LimitDamage(kundun, damage);
            kundun.Health -= (int)limited;
        }
    }

    private static async ValueTask<(GameConfiguration GameConfiguration, InMemoryPersistenceContextProvider ContextProvider)> CreateDataAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        await new Persistence.Initialization.VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var gameConfiguration = (await contextProvider.CreateNewConfigurationContext().GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        return (gameConfiguration, contextProvider);
    }

    private static async ValueTask WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private static async ValueTask<List<Monster>> WaitForMonstersAsync(List<Monster> monsters, int count)
    {
        await WaitUntilAsync(() =>
        {
            lock (monsters)
            {
                return monsters.Count >= count;
            }
        }).ConfigureAwait(false);

        lock (monsters)
        {
            return monsters.ToList();
        }
    }
}
