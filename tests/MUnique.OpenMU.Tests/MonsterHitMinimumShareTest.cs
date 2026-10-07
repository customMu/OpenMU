// <copyright file="MonsterHitMinimumShareTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MonsterAttribute = MUnique.OpenMU.Persistence.BasicModel.MonsterAttribute;
using MonsterDefinition = MUnique.OpenMU.Persistence.BasicModel.MonsterDefinition;

/// <summary>
/// Tests for <see cref="Stats.MonsterHitMinimumShare"/>: a monster hit always takes a share of its damage before the
/// defense of the player.
/// </summary>
[TestFixture]
public class MonsterHitMinimumShareTest
{
    private const int MonsterDamage = 100;

    /// <summary>
    /// A defense higher than the monster damage leaves the minimum share of the hit (20 of 100).
    /// </summary>
    [Test]
    public async ValueTask HighDefenseLeavesTheMinimumShareAsync()
    {
        var damages = await this.HitDamagesAsync(defense: 1000, minimumShare: 0.2f).ConfigureAwait(false);
        Assert.That(damages, Is.Not.Empty);
        Assert.That(damages, Is.All.EqualTo(20));
    }

    /// <summary>
    /// A low defense is subtracted as usual (100 - 50 = 50 is more than the minimum share 20).
    /// </summary>
    [Test]
    public async ValueTask LowDefenseIsSubtractedAsUsualAsync()
    {
        var damages = await this.HitDamagesAsync(defense: 50, minimumShare: 0.2f).ConfigureAwait(false);
        Assert.That(damages, Is.Not.Empty);
        Assert.That(damages, Is.All.EqualTo(50));
    }

    /// <summary>
    /// Without the attribute the old minimum (attacker level / 10, at least 1) stays.
    /// </summary>
    [Test]
    public async ValueTask WithoutTheAttributeTheOldMinimumStaysAsync()
    {
        var damages = await this.HitDamagesAsync(defense: 1000, minimumShare: 0f).ConfigureAwait(false);
        Assert.That(damages, Is.Not.Empty);
        Assert.That(damages, Is.All.EqualTo(1));
    }

    private async ValueTask<List<int>> HitDamagesAsync(int defense, float minimumShare)
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var player = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        player.Attributes!.AddElement(new ConstantElement(defense), Stats.DefensePvm);
        player.Attributes.AddElement(new ConstantElement(1), Stats.DefenseDecrement); // 1 for every class in the DB
        if (minimumShare > 0)
        {
            player.Attributes.AddElement(new ConstantElement(minimumShare), Stats.MonsterHitMinimumShare);
        }

        var monster = await CreateMonsterAsync(gameContext).ConfigureAwait(false);
        var damages = new List<int>();
        for (var i = 0; i < 200; i++)
        {
            var hit = await monster.CalculateDamageAsync(player, null, false).ConfigureAwait(false);
            if (hit.HealthDamage > 0)
            {
                damages.Add((int)hit.HealthDamage);
            }
        }

        return damages;
    }

    private static async ValueTask<Monster> CreateMonsterAsync(IGameContext gameContext)
    {
        // a new id: the monster attributes are cached per definition (MonsterAttributeHolder)
        var definition = new MonsterDefinition { Id = Guid.NewGuid(), ObjectKind = NpcObjectKind.Monster };
        definition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.MaximumHealth, Value = 1000 });
        definition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.MinimumPhysBaseDmg, Value = MonsterDamage });
        definition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.MaximumPhysBaseDmg, Value = MonsterDamage });
        definition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.AttackRatePvm, Value = 100000 });
        var map = await gameContext.GetMapAsync(0).ConfigureAwait(false);
        var spawnArea = new MonsterSpawnArea
        {
            MonsterDefinition = definition,
            GameMap = map!.Definition,
            X1 = 100,
            Y1 = 100,
            X2 = 100,
            Y2 = 100,
            Quantity = 1,
        };

        var monster = new Monster(spawnArea, definition, map, NullDropGenerator.Instance, new Mock<INpcIntelligence>().Object, gameContext.PlugInManager, gameContext.PathFinderPool);
        monster.Initialize();
        return monster;
    }
}
