// <copyright file="VaultView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// The vault of an account.
/// </summary>
/// <param name="Money">The zen in the vault.</param>
/// <param name="MaximumMoney">The maximum zen the vault can hold.</param>
/// <param name="IsOnline">Whether the account is in the game right now.</param>
/// <param name="Items">The items.</param>
/// <param name="Rows">The number of rows of 8 slots: 15, or 30 for an extended vault.</param>
public sealed record VaultView(int Money, int MaximumMoney, bool IsOnline, IReadOnlyList<VaultItem> Items, int Rows);
