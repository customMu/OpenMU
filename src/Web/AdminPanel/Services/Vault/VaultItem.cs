// <copyright file="VaultItem.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// An item of a vault, described for a website.
/// </summary>
/// <param name="Id">The id of the item in the database.</param>
/// <param name="Slot">The vault slot.</param>
/// <param name="Name">The name including the set name and the level, e.g. "Hyon Dragon Sword +13".</param>
/// <param name="Group">The item group.</param>
/// <param name="Number">The item number within the group.</param>
/// <param name="Width">The width in vault slots.</param>
/// <param name="Height">The height in vault slots.</param>
/// <param name="IsExcellent">Whether the item has excellent options.</param>
/// <param name="IsAncient">Whether the item belongs to an ancient set.</param>
/// <param name="Image">The file name of the item picture of the item editor, e.g. "item_0_14_11_a.png";
/// the pictures are in <c>src/Web/ItemEditor/wwwroot/img/items</c>.</param>
/// <param name="Options">The descriptions of the options, e.g. "Luck" or "Excellent damage rate +10%".</param>
/// <param name="Data">The data to create the item again.</param>
/// <param name="Count">The number of pieces: the size of the stack for a stackable item (e.g. jewels), otherwise 1.</param>
/// <param name="MaximumStack">The largest stack of this item; 1 when it isn't stackable.</param>
/// <param name="Level">The item level (+0 to +15).</param>
/// <param name="HasLuck">Whether the item has luck.</param>
/// <param name="HasSkill">Whether the item has its skill.</param>
/// <param name="ExcellentCount">The number of excellent options.</param>
/// <param name="Kind">The kind: Weapon, Shield, Helm, Armor, Pants, Gloves, Boots, Wings, Pet, Pendant, Ring or Other.</param>
/// <param name="Details">The white lines of the tooltip of the game: damage or defense, attack speed, durability and requirements.</param>
/// <param name="Classes">The base classes which can wear the item: DW, DK, ELF, MG, DL, SUM, RF; empty when it isn't wearable by class.</param>
public sealed record VaultItem(
    Guid Id,
    byte Slot,
    string Name,
    byte Group,
    short Number,
    byte Width,
    byte Height,
    bool IsExcellent,
    bool IsAncient,
    string Image,
    IReadOnlyList<string> Options,
    VaultItemData Data,
    int Count,
    int MaximumStack,
    byte Level,
    bool HasLuck,
    bool HasSkill,
    int ExcellentCount,
    string Kind,
    IReadOnlyList<string> Classes,
    IReadOnlyList<string> Details);
