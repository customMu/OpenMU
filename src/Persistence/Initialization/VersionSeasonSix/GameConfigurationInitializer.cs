// <copyright file="GameConfigurationInitializer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using System.Reflection;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Items;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

/// <summary>
/// Initializes the <see cref="GameConfiguration"/>.
/// </summary>
public class GameConfigurationInitializer : GameConfigurationInitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameConfigurationInitializer"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public GameConfigurationInitializer(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<ItemOptionType> OptionTypes
    {
        get
        {
            // we take all in season 6
            return typeof(ItemOptionTypes)
                .GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => p.PropertyType == typeof(ItemOptionType))
                .Select(p => p.GetValue(typeof(ItemOptionType)))
                .OfType<ItemOptionType>()
                .ToList();
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        this.GameConfiguration.ItemOptions.Add(this.CreateOptionDefinition(Stats.BaseDamageBonus, ItemOptionDefinitionNumbers.PhysicalAndWizardryAttack));
        this.GameConfiguration.ItemOptions.Add(this.CreateOptionDefinition(Stats.CurseBaseDmg, ItemOptionDefinitionNumbers.CurseAttack));

        var maximumAllianceSizeDef = Stats.MaximumAllianceSize.GetPersistent(this.GameConfiguration);
        var maximumAllianceSizeValue = this.Context.CreateNew<ConstValueAttribute>(5f, maximumAllianceSizeDef, AggregateType.AddRaw);
        this.GameConfiguration.GlobalBaseAttributeValues.Add(maximumAllianceSizeValue);

        new CharacterClassInitialization(this.Context, this.GameConfiguration).Initialize();
        new SkillsInitializer(this.Context, this.GameConfiguration).Initialize();
        new Orbs(this.Context, this.GameConfiguration).Initialize();
        new Scrolls(this.Context, this.GameConfiguration).Initialize();
        new EventTicketItems(this.Context, this.GameConfiguration).Initialize();
        new Wings(this.Context, this.GameConfiguration).Initialize();
        new Pets(this.Context, this.GameConfiguration).Initialize();
        new ExcellentOptions(this.Context, this.GameConfiguration).Initialize();
        new HarmonyOptions(this.Context, this.GameConfiguration).Initialize();
        new GuardianOptions(this.Context, this.GameConfiguration).Initialize();
        new Armors(this.Context, this.GameConfiguration).Initialize();
        new Weapons(this.Context, this.GameConfiguration).Initialize();
        new Potions(this.Context, this.GameConfiguration).Initialize();
        new Jewels(this.Context, this.GameConfiguration).Initialize();
        new Misc(this.Context, this.GameConfiguration).Initialize();
        new PackedJewels(this.Context, this.GameConfiguration).Initialize();
        new Jewelery(this.Context, this.GameConfiguration).Initialize();
        new AncientSets(this.Context, this.GameConfiguration).Initialize();
        new BoxOfLuck(this.Context, this.GameConfiguration).Initialize();
        new NpcInitialization(this.Context, this.GameConfiguration).Initialize();
        new InvasionMobsInitialization(this.Context, this.GameConfiguration).Initialize();
        new GameMapsInitializer(this.Context, this.GameConfiguration).Initialize();
        this.AssignCharacterClassHomeMaps();
        this.WireRenaDropToEventMaps();
        new SocketSystem(this.Context, this.GameConfiguration).Initialize();
        new ChaosMixes(this.Context, this.GameConfiguration).Initialize();
        new Gates(this.Context, this.GameConfiguration).Initialize();
        new Quest(this.Context, this.GameConfiguration).Initialize();
        new Quests(this.Context, this.GameConfiguration).Initialize();
        new DevilSquareInitializer(this.Context, this.GameConfiguration).Initialize();
        new BloodCastleInitializer(this.Context, this.GameConfiguration).Initialize();
        new ChaosCastleInitializer(this.Context, this.GameConfiguration).Initialize();
        new CastleSiegeInitializer(this.Context, this.GameConfiguration).Initialize();
        new KanturuInitializer(this.Context, this.GameConfiguration).Initialize();
        new KalimaInstanceInitializer(this.Context, this.GameConfiguration).Initialize();

        // At the very end, so that no other initializer copies the increased stack size (255) into rewards or similar.
        this.MakeJewelsStackable();
    }

    /// <summary>
    /// Wires the Rena <see cref="DropItemGroup"/> (created in <see cref="Misc"/>) to the Blood Castle
    /// and Devil Square 5-7 event maps, so it only drops there instead of everywhere.
    /// </summary>
    private void WireRenaDropToEventMaps()
    {
        var renaDropGroup = this.GameConfiguration.DropItemGroups
            .FirstOrDefault(g => g.PossibleItems.Any(i => i.Group == 14 && i.Number == 21));
        if (renaDropGroup is null)
        {
            return;
        }

        var eventMaps = this.GameConfiguration.Maps
            .Where(m => m.Name.Value?.StartsWith("Blood Castle") is true || m.Name.Value is "Devil Square 5" or "Devil Square 6" or "Devil Square 7");
        foreach (var map in eventMaps)
        {
            if (!map.DropItemGroups.Contains(renaDropGroup))
            {
                map.DropItemGroups.Add(renaDropGroup);
            }
        }
    }

    /// <summary>
    /// Makes the jewels and refine stones stackable in the inventory, by increasing the maximum stack size (which is the durability of the definition).
    /// The item counter is sent to the client as a single byte, so 255 is the maximum.
    /// New drops and crafting results are still created as single pieces.
    /// </summary>
    private void MakeJewelsStackable()
    {
        const byte maximumStackSize = 255;
        var stackableItems = new (byte Group, short Number)[]
        {
            (14, 13), // Jewel of Bless
            (14, 14), // Jewel of Soul
            (14, 16), // Jewel of Life
            (14, 22), // Jewel of Creation
            (12, 15), // Jewel of Chaos
            (14, 31), // Jewel of Guardian
            (14, 41), // Gemstone
            (14, 42), // Jewel of Harmony
            (14, 43), // Lower Refine Stone
            (14, 44), // Higher Refine Stone
        };

        foreach (var (group, number) in stackableItems)
        {
            var definition = this.GameConfiguration.Items.FirstOrDefault(i => i.Group == group && i.Number == number);
            if (definition is not null)
            {
                definition.Durability = maximumStackSize;
            }
        }
    }
}
