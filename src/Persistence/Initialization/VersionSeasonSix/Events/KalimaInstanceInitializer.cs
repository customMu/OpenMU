// <copyright file="KalimaInstanceInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.KundunSymbols;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The initializer for the Kalima instance (Kalima 1-7 as daily instance) and the symbol shop:
/// the mini game definitions, the gatekeeper (Lugard) and the symbol shop (Delgado) in Lorencia.
/// </summary>
internal class KalimaInstanceInitializer : InitializerBase
{
    /// <summary>
    /// The number of the spawn of the gatekeeper in Lorencia.
    /// </summary>
    internal const short GatekeeperSpawnNumber = 60;

    /// <summary>
    /// The number of the spawn of the symbol shop in Lorencia.
    /// </summary>
    internal const short SymbolShopSpawnNumber = 61;

    private const byte LorenciaNumber = 0;

    /// <summary>
    /// The numbers of the maps Kalima 1 to 7.
    /// </summary>
    internal static readonly short[] KalimaMapNumbers = [24, 25, 26, 27, 28, 29, 36];

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaInstanceInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public KalimaInstanceInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.CreateMiniGameDefinitions();
        this.CreateLorenciaSpawns();
        this.CreateSymbolShop();
    }

    private void CreateMiniGameDefinitions()
    {
        for (byte level = 1; level <= KalimaMapNumbers.Length; level++)
        {
            if (this.GameConfiguration.MiniGameDefinitions.Any(d => d.Type == MiniGameType.KalimaInstance && d.GameLevel == level))
            {
                continue;
            }

            var mapNumber = KalimaMapNumbers[level - 1];
            var entrance = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber)?.ExitGates.FirstOrDefault();
            if (entrance is null)
            {
                continue;
            }

            var definition = this.Context.CreateNew<MiniGameDefinition>();
            this.GameConfiguration.MiniGameDefinitions.Add(definition);
            definition.SetGuid((short)MiniGameType.KalimaInstance, level);
            definition.Name = $"Kalima Instance {level}";
            definition.Description = $"The daily Kalima {level} instance for a party or a single player. The game duration plus one minute is the playing time.";
            definition.Type = MiniGameType.KalimaInstance;
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
            definition.SaveRankingStatistics = false;
            definition.Entrance = entrance;
        }
    }

    private void CreateLorenciaSpawns()
    {
        var lorencia = this.GameConfiguration.Maps.FirstOrDefault(m => m.Number == LorenciaNumber);
        if (lorencia is null)
        {
            return;
        }

        this.CreateNpcSpawn(lorencia, GatekeeperSpawnNumber, new KalimaInstanceConfiguration().GatekeeperNpcNumber, 125, 127, Direction.SouthEast);
        this.CreateNpcSpawn(lorencia, SymbolShopSpawnNumber, new KundunSymbolsConfiguration().ShopNpcNumber, 136, 127, Direction.SouthWest);
    }

    private void CreateNpcSpawn(GameMapDefinition map, short spawnNumber, short npcNumber, byte x, byte y, Direction direction)
    {
        var npc = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == npcNumber);
        if (npc is null || map.MonsterSpawns.Any(s => s.MonsterDefinition == npc))
        {
            return;
        }

        var area = this.Context.CreateNew<MonsterSpawnArea>();
        area.SetGuid(map.Number, spawnNumber);
        area.GameMap = map;
        area.MonsterDefinition = npc;
        area.Quantity = 1;
        area.Direction = direction;
        area.SpawnTrigger = SpawnTrigger.Automatic;
        area.X1 = x;
        area.X2 = x;
        area.Y1 = y;
        area.Y2 = y;
        map.MonsterSpawns.Add(area);
    }

    private void CreateSymbolShop()
    {
        var shopNpc = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == new KundunSymbolsConfiguration().ShopNpcNumber);
        if (shopNpc is null || shopNpc.MerchantStore is not null)
        {
            return;
        }

        var store = this.Context.CreateNew<ItemStorage>();
        byte slot = 0;
        foreach (var price in new KundunSymbolsConfiguration().Prices)
        {
            var definition = this.GameConfiguration.Items.FirstOrDefault(i => i.Group == price.Group && i.Number == price.Number);
            if (definition is null)
            {
                continue;
            }

            var item = this.Context.CreateNew<Item>();
            item.Definition = definition;
            item.Level = (byte)Math.Max(0, price.Level);
            item.Durability = 1;
            item.ItemSlot = slot++;
            store.Items.Add(item);
        }

        shopNpc.NpcWindow = NpcWindow.Merchant;
        shopNpc.MerchantStore = store;
    }
}
