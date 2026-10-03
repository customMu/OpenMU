// <copyright file="VaultItemOptionData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// An option of a <see cref="VaultItemData"/>; the values of an <see cref="DataModel.Entities.ItemOptionLink"/>.
/// </summary>
/// <param name="OptionId">The id of the <see cref="DataModel.Configuration.Items.IncreasableItemOption"/>.</param>
/// <param name="Level">The level of the option.</param>
/// <param name="Index">The index of the option, e.g. the socket slot.</param>
public sealed record VaultItemOptionData(Guid OptionId, int Level, int Index);
