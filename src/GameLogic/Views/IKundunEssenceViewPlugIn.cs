// <copyright file="IKundunEssenceViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Interface of a view which shows the Kundun Essence currency and the prices of the essence shop.
/// </summary>
public interface IKundunEssenceViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the current balance of essence.
    /// </summary>
    /// <param name="balance">The balance.</param>
    ValueTask ShowBalanceAsync(int balance);

    /// <summary>
    /// Shows the prices of the opened essence shop. An empty list means that the opened shop sells for zen.
    /// </summary>
    /// <param name="prices">The prices per store slot.</param>
    ValueTask ShowShopPricesAsync(IReadOnlyList<(byte Slot, int Price)> prices);
}
