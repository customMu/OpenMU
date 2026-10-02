// <copyright file="CharacterRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// An entry of the character ranking.
/// </summary>
/// <param name="Rank">The rank, starting at 1.</param>
/// <param name="Name">The character name.</param>
/// <param name="ClassName">The name of the character class.</param>
/// <param name="Level">The level.</param>
/// <param name="MasterLevel">The master level.</param>
/// <param name="Resets">The number of resets.</param>
/// <param name="GuildName">The name of the guild, if the character is a guild member.</param>
public record CharacterRankingEntry(int Rank, string Name, string ClassName, int Level, int MasterLevel, int Resets, string? GuildName);
