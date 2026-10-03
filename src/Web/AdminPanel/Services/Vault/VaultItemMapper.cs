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
            DescribeOptions(item),
            ToData(item));
    }

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

    private static List<string> DescribeOptions(Item item)
    {
        var result = new List<string>();
        if (item.HasSkill)
        {
            result.Add("Skill");
        }

        if (item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Luck))
        {
            result.Add("Luck");
        }

        foreach (var option in item.ItemOptions
                     .Where(o => o.ItemOption is not null && o.ItemOption.OptionType != ItemOptionTypes.Luck)
                     .OrderBy(o => o.ItemOption!.OptionType == ItemOptionTypes.Option ? 0 : 1)
                     .ThenBy(o => o.Index))
        {
            var itemOption = option.ItemOption!;
            var level = itemOption.LevelType == LevelType.ItemLevel ? item.Level : option.Level;
            var powerUp = itemOption.LevelDependentOptions.FirstOrDefault(o => o.Level == level)?.PowerUpDefinition
                          ?? itemOption.PowerUpDefinition;
            if (powerUp is not null)
            {
                result.Add("+" + powerUp);
            }
        }

        if (item.SocketCount > 0)
        {
            result.Add($"Sockets: {item.SocketCount}");
        }

        return result;
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
