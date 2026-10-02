// <copyright file="GuildMemberRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Data;

using MUnique.OpenMU.Interfaces;

/// <summary>
/// A row of the <c>guild."GuildMember"</c> table.
/// </summary>
public class GuildMemberRow
{
    /// <summary>
    /// Gets or sets the identifier, which is the same as the id of the character.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the guild.
    /// </summary>
    public Guid GuildId { get; set; }

    /// <summary>
    /// Gets or sets the position of the member in the guild.
    /// </summary>
    public GuildPosition Status { get; set; }
}
