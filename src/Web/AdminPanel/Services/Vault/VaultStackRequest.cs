// <copyright file="VaultStackRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// A request to put pieces of a stackable item (e.g. jewels) into a vault.
/// </summary>
/// <param name="Group">The item group.</param>
/// <param name="Number">The item number within the group.</param>
/// <param name="Count">The number of pieces.</param>
public sealed record VaultStackRequest(byte Group, short Number, int Count);
