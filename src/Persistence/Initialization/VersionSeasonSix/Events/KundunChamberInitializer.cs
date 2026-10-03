// <copyright file="KundunChamberInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The initializer for the chamber of Kundun: the monsters Kundun 1-7 (copies of the Illusion of Kundun of
/// the Kalima map of their level), the mini game definitions and the keeper (David) in Lorencia.
/// </summary>
internal class KundunChamberInitializer : InitializerBase
{
    /// <summary>
    /// The number of the spawn of the keeper in Lorencia.
    /// </summary>
    internal const short KeeperSpawnNumber = 62;

    /// <summary>
    /// The position of the keeper in Lorencia, between Lugard and Delgado.
    /// </summary>
    internal const byte KeeperX = 130;

    /// <summary>
    /// The position of the keeper in Lorencia, between Lugard and Delgado.
    /// </summary>
    internal const byte KeeperY = 127;

    private const byte LorenciaNumber = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="KundunChamberInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public KundunChamberInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.CreateKundunMonsters();
        this.CreateMiniGameDefinitions();
        this.CreateKeeperSpawn();
    }

    private void CreateKundunMonsters()
    {
        var configuration = new KundunChamberConfiguration();
        var kalimaConfiguration = new KalimaInstanceConfiguration();
        for (var level = 1; level <= KalimaInstanceInitializer.KalimaMapNumbers.Length; level++)
        {
            var number = configuration.GetKundunMonsterNumber(level);
            if (number is null || this.GameConfiguration.Monsters.Any(m => m.Number == number))
            {
                continue;
            }

            var mapNumber = KalimaInstanceInitializer.KalimaMapNumbers[level - 1];
            var illusion = this.GameConfiguration.Maps
                .Where(m => m.Number == mapNumber)
                .SelectMany(m => m.MonsterSpawns)
                .Select(s => s.MonsterDefinition)
                .FirstOrDefault(m => m is not null && kalimaConfiguration.BossMonsterNumbers.Contains(m.Number));
            if (illusion is null)
            {
                continue;
            }

            var kundun = this.Context.CreateNew<MonsterDefinition>();
            kundun.Number = number.Value;
            kundun.Designation = $"Kundun {level}";
            kundun.MoveRange = illusion.MoveRange;
            kundun.AttackRange = illusion.AttackRange;
            kundun.ViewRange = illusion.ViewRange;
            kundun.MoveDelay = illusion.MoveDelay;
            kundun.AttackDelay = illusion.AttackDelay;
            kundun.RespawnDelay = illusion.RespawnDelay;
            kundun.Attribute = illusion.Attribute;
            kundun.NumberOfMaximumItemDrops = Math.Max(illusion.NumberOfMaximumItemDrops, 3);
            kundun.ObjectKind = NpcObjectKind.Monster;
            kundun.IntelligenceTypeName = illusion.IntelligenceTypeName;
            kundun.AttackSkill = illusion.AttackSkill;
            var attributes = illusion.Attributes
                .Where(a => a.AttributeDefinition is not null)
                .ToDictionary(a => a.AttributeDefinition!, a => a.Value);
            kundun.AddAttributes(attributes, this.Context, this.GameConfiguration);
            kundun.SetGuid(kundun.Number);
            this.GameConfiguration.Monsters.Add(kundun);
        }
    }

    private void CreateMiniGameDefinitions()
    {
        for (byte level = 1; level <= KalimaInstanceInitializer.KalimaMapNumbers.Length; level++)
        {
            if (this.GameConfiguration.MiniGameDefinitions.Any(d => d.Type == MiniGameType.KundunChamber && d.GameLevel == level))
            {
                continue;
            }

            var mapNumber = KalimaInstanceInitializer.KalimaMapNumbers[level - 1];
            var entrance = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber)?.ExitGates.FirstOrDefault();
            if (entrance is null)
            {
                continue;
            }

            var definition = this.Context.CreateNew<MiniGameDefinition>();
            this.GameConfiguration.MiniGameDefinitions.Add(definition);
            definition.SetGuid((short)MiniGameType.KundunChamber, level);
            definition.Name = $"Chamber of Kundun {level}";
            definition.Description = $"The weekly fight against Kundun {level} for a party or a single player, on the map Kalima {level}. The playing time is configured in the plugin \"Chamber of Kundun\", the game duration has to be longer. The ranking contains the kill times in seconds.";
            definition.Type = MiniGameType.KundunChamber;
            definition.GameLevel = level;
            definition.EnterDuration = TimeSpan.Zero;
            definition.GameDuration = TimeSpan.FromMinutes(59);
            definition.ExitDuration = TimeSpan.FromMinutes(1);
            definition.MaximumPlayerCount = 5;
            definition.MinimumCharacterLevel = 0;
            definition.MaximumCharacterLevel = 400;
            definition.MinimumSpecialCharacterLevel = 0;
            definition.MaximumSpecialCharacterLevel = 400;
            definition.MapCreationPolicy = MiniGameMapCreationPolicy.OnePerParty;
            definition.AllowParty = true;
            definition.ArePlayerKillersAllowedToEnter = true;
            definition.SaveRankingStatistics = true;
            definition.Entrance = entrance;
        }
    }

    private void CreateKeeperSpawn()
    {
        var lorencia = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == LorenciaNumber);
        var npcNumber = new KundunChamberConfiguration().KeeperNpcNumber;
        var npc = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == npcNumber);
        if (lorencia is null || npc is null || lorencia.MonsterSpawns.Any(s => s.MonsterDefinition == npc))
        {
            return;
        }

        var area = this.Context.CreateNew<MonsterSpawnArea>();
        area.SetGuid(lorencia.Number, KeeperSpawnNumber);
        area.GameMap = lorencia;
        area.MonsterDefinition = npc;
        area.Quantity = 1;
        area.Direction = Direction.South;
        area.SpawnTrigger = SpawnTrigger.Automatic;
        area.X1 = KeeperX;
        area.X2 = KeeperX;
        area.Y1 = KeeperY;
        area.Y2 = KeeperY;
        lorencia.MonsterSpawns.Add(area);
    }
}
