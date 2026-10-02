// <copyright file="CharacterRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Data;

/// <summary>
/// A row of the <c>data."Character"</c> table. The portal only reads characters.
/// </summary>
public class CharacterRow
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the account.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the character class.
    /// </summary>
    public Guid CharacterClassId { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the experience.
    /// </summary>
    public long Experience { get; set; }

    /// <summary>
    /// Gets or sets the master experience.
    /// </summary>
    public long MasterExperience { get; set; }

    /// <summary>
    /// Gets or sets the player kill count.
    /// </summary>
    public int PlayerKillCount { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreateDate { get; set; }
}
