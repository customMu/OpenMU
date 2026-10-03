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
    VaultItemData Data);
