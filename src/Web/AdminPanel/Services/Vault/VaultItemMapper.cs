// <copyright file="VaultItemMapper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Converts items to <see cref="VaultItem"/> and creates them again from <see cref="VaultItemData"/>.
/// </summary>
internal static class VaultItemMapper
{
    /// <summary>
    /// The item level, whose glow the item picture shows, by item level; like in the item editor.
    /// </summary>
    private static readonly string[] BaseClasses = ["DW", "DK", "ELF", "MG", "DL", "SUM", "RF"];

    private static readonly int[] PictureLevels = [0, 0, 0, 3, 3, 5, 5, 7, 7, 9, 9, 11, 11, 13, 13, 15, 15];

    /// <summary>
    /// Describes the item for a website.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The description, including the data to create the item again.</returns>
    public static VaultItem Describe(Item item)
    {
        var definition = item.Definition!;
        var ancientSet = item.ItemSetGroups.FirstOrDefault(s => s.AncientSetDiscriminator != 0)?.ItemSetGroup;
        var isExcellent = item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent);
        var name = definition.GetNameForLevel(item.Level).ToString();
        if (ancientSet is not null)
        {
            name = ancientSet.Name.ToString() + " " + name;
        }

        if (item.Level > 0)
        {
            name += " +" + item.Level;
        }

        return new VaultItem(
            item.GetId(),
            item.ItemSlot,
            name,
            definition.Group,
            definition.Number,
            definition.Width,
            definition.Height,
            isExcellent,
            ancientSet is not null,
            GetPictureFileName(item, isExcellent, ancientSet is not null),
            VaultOptionText.Describe(item),
            ToData(item),
            item.IsStackable() ? (int)item.Durability : 1,
            GetMaximumStack(definition),
            item.Level,
            item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Luck),
            item.HasSkill,
            item.ItemOptions.Count(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent),
            GetKind(definition),
            GetClasses(definition),
            VaultItemDetails.Describe(item));
    }

    /// <summary>
    /// Gets the kind of items of a definition for a website filter.
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <returns>The kind.</returns>
    public static string GetKind(ItemDefinition definition)
    {
        var slots = definition.ItemSlot?.ItemSlots ?? [];
        return definition.Group switch
        {
            <= 5 when slots.Count > 0 => "Weapon",
            6 => "Shield",
            7 => "Helm",
            8 => "Armor",
            9 => "Pants",
            10 => "Gloves",
            11 => "Boots",
            _ when slots.Contains(InventoryConstants.WingsSlot) => "Wings",
            _ when slots.Contains(InventoryConstants.PetSlot) => "Pet",
            _ when slots.Contains(InventoryConstants.PendantSlot) => "Pendant",
            _ when slots.Contains(InventoryConstants.Ring1Slot) || slots.Contains(InventoryConstants.Ring2Slot) => "Ring",
            _ => "Other",
        };
    }

    /// <summary>
    /// Gets the base classes which can wear items of a definition, e.g. "DW" for Dark Wizard, Soul Master and Grand Master.
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <returns>The base classes.</returns>
    public static IReadOnlyList<string> GetClasses(ItemDefinition definition)
    {
        return definition.QualifiedCharacters
            .Select(c => (c.Number / 4) switch
            {
                0 => "DW",
                1 => "DK",
                2 => "ELF",
                3 => "MG",
                4 => "DL",
                5 => "SUM",
                6 => "RF",
                _ => null,
            })
            .OfType<string>()
            .Distinct()
            .OrderBy(c => Array.IndexOf(BaseClasses, c))
            .ToList();
    }

    /// <summary>
    /// Gets the largest stack of items of the definition; 1 when they aren't stackable.
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <returns>The largest stack.</returns>
    public static int GetMaximumStack(ItemDefinition definition)
        => definition.ItemSlot is null && definition.Durability > 1 ? definition.Durability : 1;

    /// <summary>
    /// Determines whether the item is a plain piece of a stackable item, which can be stacked with
    /// other pieces of the same definition: no level, options or sockets.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item is a plain stack.</returns>
    public static bool IsPlainStack(Item item)
        => item.Definition is { } definition
           && GetMaximumStack(definition) > 1
           && item.Level == 0
           && item.ItemOptions.Count == 0
           && item.ItemSetGroups.Count == 0
           && item.SocketCount == 0;

    /// <summary>
    /// Creates a new item from the data.
    /// </summary>
    /// <param name="data">The data.</param>
    /// <param name="configuration">The game configuration of the context.</param>
    /// <param name="context">The context in which the item is created.</param>
    /// <returns>The new item, or <see langword="null"/>, if the definition or an option doesn't exist.</returns>
    public static Item? TryCreate(VaultItemData data, GameConfiguration configuration, IContext context)
    {
        var definition = configuration.Items.FirstOrDefault(d => d.GetId() == data.DefinitionId);
        if (definition is null)
        {
            return null;
        }

        var options = GetAllOptions(configuration);
        var setItems = configuration.ItemSetGroups.SelectMany(g => g.Items).ToDictionary(i => i.GetId());
        if (data.Options.Any(o => !options.ContainsKey(o.OptionId)) || data.SetItemIds.Any(id => !setItems.ContainsKey(id)))
        {
            return null;
        }

        var maximumStack = GetMaximumStack(definition);
        if (maximumStack > 1 && (data.Durability < 1 || data.Durability > maximumStack || data.Durability % 1 != 0))
        {
            // A stack of a stackable item must hold between one piece and the largest stack.
            return null;
        }

        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.Level = data.Level;
        item.Durability = data.Durability;
        item.HasSkill = data.HasSkill;
        item.SocketCount = data.SocketCount;
        item.PetExperience = data.PetExperience;
        foreach (var optionData in data.Options)
        {
            var link = context.CreateNew<ItemOptionLink>();
            link.ItemOption = options[optionData.OptionId];
            link.Level = optionData.Level;
            link.Index = optionData.Index;
            item.ItemOptions.Add(link);
        }

        foreach (var setItemId in data.SetItemIds)
        {
            item.ItemSetGroups.Add(setItems[setItemId]);
        }

        return item;
    }

    /// <summary>
    /// Gets the file name of the item picture, in the same way as the item editor does.
    /// </summary>
    private static string GetPictureFileName(Item item, bool isExcellent, bool isAncient)
    {
        var definition = item.Definition!;
        var level = definition.IsTrainablePet() || definition.IsWing()
            ? 0
            : definition.ItemSlot is not null
                ? (item.Level < PictureLevels.Length ? PictureLevels[item.Level] : 0)
                : item.Level;
        var suffix = isAncient ? "_a" : definition.Group < 12 && isExcellent ? "_e" : string.Empty;
        return $"item_{definition.Group}_{definition.Number}_{level}{suffix}.png";
    }

    private static VaultItemData ToData(Item item)
    {
        return new VaultItemData(
            item.Definition!.GetId(),
            item.Level,
            item.Durability,
            item.HasSkill,
            item.SocketCount,
            item.PetExperience,
            item.ItemOptions
                .Where(o => o.ItemOption is not null)
                .Select(o => new VaultItemOptionData(o.ItemOption!.GetId(), o.Level, o.Index))
                .ToList(),
            item.ItemSetGroups.Select(s => s.GetId()).ToList());
    }

    private static Dictionary<Guid, IncreasableItemOption> GetAllOptions(GameConfiguration configuration)
    {
        var definitions = configuration.ItemOptions
            .Concat(configuration.Items.SelectMany(i => i.PossibleItemOptions))
            .Concat(configuration.ItemSetGroups.Select(g => g.Options).OfType<ItemOptionDefinition>());
        var result = new Dictionary<Guid, IncreasableItemOption>();
        foreach (var option in definitions.SelectMany(d => d.PossibleOptions))
        {
            result.TryAdd(option.GetId(), option);
        }

        return result;
    }
}
