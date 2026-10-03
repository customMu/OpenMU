// <copyright file="KalimaInstanceInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The initializer for the Kalima instance (Kalima 1-7 as daily instance) and the essence shop:
/// the mini game definitions, the gatekeeper (Lugard) and the essence shop (Delgado) in Lorencia.
/// </summary>
internal class KalimaInstanceInitializer : InitializerBase
{
    /// <summary>
    /// The number of the spawn of the gatekeeper in Lorencia.
    /// </summary>
    internal const short GatekeeperSpawnNumber = 60;

    /// <summary>
    /// The number of the spawn of the essence shop in Lorencia.
    /// </summary>
    internal const short EssenceShopSpawnNumber = 61;

    private const byte LorenciaNumber = 0;

    private const int StoreRowSize = 8;

    private const int StoreSize = 120;

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
        this.CreateEssenceShop();
        RemoveSymbolDrops(this.GameConfiguration);
    }

    /// <summary>
    /// Removes the regular drops of the Symbols of Kundun: they drop in the Kalima instance only
    /// (plugin "Kalima instance"), the Symbol +N in Kalima N.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void RemoveSymbolDrops(GameConfiguration gameConfiguration)
    {
        static bool IsSymbolDrop(DropItemGroup group) => group.PossibleItems.Any(item => item.Group == 14 && item.Number == 29);

        foreach (var map in gameConfiguration.Maps)
        {
            foreach (var group in map.DropItemGroups.Where(IsSymbolDrop).ToList())
            {
                map.DropItemGroups.Remove(group);
            }
        }

        foreach (var monster in gameConfiguration.Monsters)
        {
            foreach (var group in monster.DropItemGroups.Where(IsSymbolDrop).ToList())
            {
                monster.DropItemGroups.Remove(group);
            }
        }
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
            definition.Description = $"The daily Kalima {level} instance for a party or a single player. The playing time is configured in the plugin \"Kalima instance\", the game duration has to be longer.";
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
        this.CreateNpcSpawn(lorencia, EssenceShopSpawnNumber, new KundunEssenceConfiguration().ShopNpcNumber, 136, 127, Direction.SouthWest);
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

    /// <summary>
    /// Creates the essence shop, or adds the items of the default prices which are missing in it (e.g. the lost maps).
    /// </summary>
    internal void CreateEssenceShop()
    {
        var shopNpc = this.GameConfiguration.Monsters.FirstOrDefault(m => m.Number == new KundunEssenceConfiguration().ShopNpcNumber);
        if (shopNpc is null)
        {
            return;
        }

        if (shopNpc.MerchantStore is not { } store)
        {
            store = this.Context.CreateNew<ItemStorage>();
            shopNpc.NpcWindow = NpcWindow.Merchant;
            shopNpc.MerchantStore = store;
        }

        var occupiedSlots = new HashSet<int>();
        foreach (var storeItem in store.Items)
        {
            var width = Math.Max((int)(storeItem.Definition?.Width ?? 1), 1);
            var height = Math.Max((int)(storeItem.Definition?.Height ?? 1), 1);
            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    occupiedSlots.Add(storeItem.ItemSlot + (row * StoreRowSize) + column);
                }
            }
        }

        foreach (var price in new KundunEssenceConfiguration().Prices)
        {
            var level = (byte)Math.Max(0, price.Level);
            var definition = this.GameConfiguration.Items.FirstOrDefault(i => i.Group == price.Group && i.Number == price.Number);
            if (definition is null
                || store.Items.Any(i => i.Definition == definition && (price.Level < 0 || i.Level == level)))
            {
                continue;
            }

            var slot = Enumerable.Range(0, StoreSize).FirstOrDefault(s => !occupiedSlots.Contains(s), -1);
            if (slot < 0)
            {
                return;
            }

            var item = this.Context.CreateNew<Item>();
            item.Definition = definition;
            item.Level = level;
            item.Durability = 1;
            item.ItemSlot = (byte)slot;
            store.Items.Add(item);
            occupiedSlots.Add(slot);
        }
    }
}
