// <copyright file="StatAttributeRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Data;

/// <summary>
/// A row of the <c>data."StatAttribute"</c> table.
/// Values like the level or the reset count of a character are stored here.
/// </summary>
public class StatAttributeRow
{
    /// <summary>
    /// The definition id of the character level (<c>Stats.Level</c>).
    /// </summary>
    public static readonly Guid LevelId = new("560931AD-0901-4342-B7F4-FD2E2FCC0563");

    /// <summary>
    /// The definition id of the master level (<c>Stats.MasterLevel</c>).
    /// </summary>
    public static readonly Guid MasterLevelId = new("70CD8C10-391A-4C51-9AA4-A854600E3A9F");

    /// <summary>
    /// The definition id of the reset count (<c>Stats.Resets</c>).
    /// </summary>
    public static readonly Guid ResetsId = new("89A891A7-F9F9-4AB5-AF36-12056E53A5F7");

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the character this attribute belongs to.
    /// </summary>
    public Guid? CharacterId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the attribute definition.
    /// </summary>
    public Guid? DefinitionId { get; set; }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public float Value { get; set; }
}
