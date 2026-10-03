// <copyright file="VaultItemData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// Everything which is needed to create an item again after it was taken out of the game,
/// e.g. while it's offered on the auction of the website.
/// </summary>
/// <param name="DefinitionId">The id of the <see cref="DataModel.Configuration.Items.ItemDefinition"/>.</param>
/// <param name="Level">The item level.</param>
/// <param name="Durability">The durability.</param>
/// <param name="HasSkill">Whether the item has its skill.</param>
/// <param name="SocketCount">The number of sockets.</param>
/// <param name="PetExperience">The experience of a pet.</param>
/// <param name="Options">The options.</param>
/// <param name="SetItemIds">The ids of the <see cref="DataModel.Configuration.Items.ItemOfItemSet"/>, e.g. of an ancient set.</param>
public sealed record VaultItemData(
    Guid DefinitionId,
    byte Level,
    double Durability,
    bool HasSkill,
    int SocketCount,
    int PetExperience,
    IReadOnlyList<VaultItemOptionData> Options,
    IReadOnlyList<Guid> SetItemIds);
