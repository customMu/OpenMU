// <copyright file="GuildRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// An entry of the guild ranking.
/// </summary>
/// <param name="Rank">The rank, starting at 1.</param>
/// <param name="Name">The guild name.</param>
/// <param name="Score">The score.</param>
/// <param name="MemberCount">The number of members.</param>
/// <param name="MasterName">The name of the guild master.</param>
public record GuildRankingEntry(int Rank, string Name, int Score, int MemberCount, string? MasterName);
