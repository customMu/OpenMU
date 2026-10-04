// <copyright file="KundunEssencePrice.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.KundunEssence;

/// <summary>
/// The price of an item in the essence shop.
/// </summary>
public class KundunEssencePrice
{
    /// <summary>
    /// Gets or sets the name (only for the admin panel).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item group.
    /// </summary>
    [Display(Name = "Item group")]
    public byte Group { get; set; }

    /// <summary>
    /// Gets or sets the item number.
    /// </summary>
    [Display(Name = "Item number")]
    public short Number { get; set; }

    /// <summary>
    /// Gets or sets the item level, -1 for any level.
    /// </summary>
    [Display(Name = "Item level", Description = "-1 = any level.")]
    public int Level { get; set; } = -1;

    /// <summary>
    /// Gets or sets the amount of a stack (e.g. 50 potions), 0 for any amount.
    /// </summary>
    [Display(Name = "Amount", Description = "The size of the stack in the store (e.g. 50, 100 or 255 potions), 0 = any. The price is for the whole stack.")]
    public int Amount { get; set; }

    /// <summary>
    /// Gets or sets the price in essence.
    /// </summary>
    [Display(Name = "Price (essence)")]
    public int Price { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.Name} ({this.Group}/{this.Number}, level {this.Level}, amount {this.Amount}): {this.Price}";
}
