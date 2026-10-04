// <copyright file="VaultStackResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// The result of putting pieces of a stackable item into a vault.
/// </summary>
/// <param name="Count">The number of pieces which were put into the vault.</param>
/// <param name="Items">The stacks which were filled up or created.</param>
public sealed record VaultStackResult(int Count, IReadOnlyList<VaultItem> Items);
