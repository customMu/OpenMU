// <copyright file="VaultResultCode.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// The outcome of an operation of the <see cref="AccountVaultService"/>.
/// </summary>
public enum VaultResultCode
{
    /// <summary>
    /// The operation was done.
    /// </summary>
    Success,

    /// <summary>
    /// The account or the item doesn't exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// The account is just logging in or another operation of the website runs for it; try again.
    /// </summary>
    Busy,

    /// <summary>
    /// The player has the vault window open, so the vault can't be changed in the background.
    /// </summary>
    VaultOpen,

    /// <summary>
    /// There is not enough zen in the vault.
    /// </summary>
    NotEnoughMoney,

    /// <summary>
    /// The zen would exceed <see cref="DataModel.Configuration.GameConfiguration.MaximumVaultMoney"/>.
    /// </summary>
    MoneyLimitExceeded,

    /// <summary>
    /// There is no free space for the item in the vault.
    /// </summary>
    NoSpace,

    /// <summary>
    /// The item data refers to an item definition or option which doesn't exist (anymore).
    /// </summary>
    InvalidItem,

    /// <summary>
    /// The stack has fewer pieces than requested.
    /// </summary>
    NotEnoughItems,

    /// <summary>
    /// No game server runs in this process.
    /// </summary>
    Unavailable,
}
