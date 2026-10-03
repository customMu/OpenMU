// <copyright file="VaultMoneyRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// A request to change the zen of a vault.
/// </summary>
/// <param name="Amount">The zen to put into the vault; negative to take zen out.</param>
public sealed record VaultMoneyRequest(int Amount);
