// <copyright file="AccountRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Data;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// A row of the <c>data."Account"</c> table.
/// All non-nullable columns without a database default must be mapped, so that new accounts can be inserted.
/// </summary>
public class AccountRow
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the login name.
    /// </summary>
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt hash of the password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the security code which is required to delete characters.
    /// </summary>
    public string SecurityCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the e-mail address.
    /// </summary>
    public string EMail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO code of the preferred language.
    /// </summary>
    public string LanguageIsoCode { get; set; } = "en";

    /// <summary>
    /// Gets or sets the registration date.
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// Gets or sets the state.
    /// </summary>
    public AccountState State { get; set; }

    /// <summary>
    /// Gets or sets the time zone.
    /// </summary>
    public short TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the vault password.
    /// </summary>
    public string VaultPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the vault is extended.
    /// </summary>
    public bool IsVaultExtended { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this account is a template for new accounts.
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this account belongs to a bot.
    /// </summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the network traffic of this account is observed.
    /// </summary>
    public bool IsNetworkObservationActive { get; set; }
}
