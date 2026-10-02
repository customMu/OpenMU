// <copyright file="RankingService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Web.Portal.Data;

/// <summary>
/// Provides the rankings and character overviews.
/// </summary>
public class RankingService
{
    private readonly PortalDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly PortalOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankingService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The portal options.</param>
    public RankingService(PortalDbContext context, IMemoryCache cache, IOptions<PortalOptions> options)
    {
        this._context = context;
        this._cache = cache;
        this._options = options.Value;
    }

    /// <summary>
    /// Gets the character ranking, ordered by resets, level, master level and experience.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ranking entries.</returns>
    public Task<IReadOnlyList<CharacterRankingEntry>> GetCharacterRankingAsync(CancellationToken cancellationToken = default)
    {
        return this.GetCachedAsync(nameof(this.GetCharacterRankingAsync), async () =>
        {
            var showBots = this._options.ShowBotsInRankings;
            var rankedAccounts = this._context.Accounts
                .Where(a => !a.IsTemplate && a.State == AccountState.Normal && (showBots || !a.IsBot));

            var rows = await this.SelectCharacters(this._context.Characters.Where(c => rankedAccounts.Any(a => a.Id == c.AccountId)))
                .OrderByDescending(c => c.Resets)
                .ThenByDescending(c => c.Level)
                .ThenByDescending(c => c.MasterLevel)
                .ThenByDescending(c => c.Experience)
                .ThenBy(c => c.Name)
                .Take(this._options.RankingSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return rows.Select((r, i) => r.ToEntry(i + 1)).ToList();
        });
    }

    /// <summary>
    /// Gets the guild ranking, ordered by score.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ranking entries.</returns>
    public Task<IReadOnlyList<GuildRankingEntry>> GetGuildRankingAsync(CancellationToken cancellationToken = default)
    {
        return this.GetCachedAsync(nameof(this.GetGuildRankingAsync), async () =>
        {
            var rows = await this._context.Guilds
                .Select(g => new
                {
                    g.Name,
                    g.Score,
                    MemberCount = this._context.GuildMembers.Count(m => m.GuildId == g.Id),
                    MasterName = this._context.GuildMembers
                        .Where(m => m.GuildId == g.Id && m.Status == GuildPosition.GuildMaster)
                        .Join(this._context.Characters, m => m.Id, c => c.Id, (m, c) => c.Name)
                        .FirstOrDefault(),
                })
                .OrderByDescending(g => g.Score)
                .ThenByDescending(g => g.MemberCount)
                .ThenBy(g => g.Name)
                .Take(this._options.RankingSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return rows.Select((g, i) => new GuildRankingEntry(i + 1, g.Name, g.Score, g.MemberCount, g.MasterName)).ToList();
        });
    }

    /// <summary>
    /// Gets the characters of an account. The result is not cached.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The characters, with a rank of 0.</returns>
    public async Task<IReadOnlyList<CharacterRankingEntry>> GetCharactersOfAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var rows = await this.SelectCharacters(this._context.Characters.Where(c => c.AccountId == accountId))
            .OrderByDescending(c => c.Resets)
            .ThenByDescending(c => c.Level)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(r => r.ToEntry(0)).ToList();
    }

    private IQueryable<CharacterProjection> SelectCharacters(IQueryable<CharacterRow> characters)
    {
        var attributes = this._context.StatAttributes;
        return characters.Select(c => new CharacterProjection
        {
            Name = c.Name,
            ClassName = this._context.CharacterClasses.Where(cc => cc.Id == c.CharacterClassId).Select(cc => cc.Name).FirstOrDefault(),
            Level = attributes.Where(a => a.CharacterId == c.Id && a.DefinitionId == StatAttributeRow.LevelId).Select(a => a.Value).FirstOrDefault(),
            MasterLevel = attributes.Where(a => a.CharacterId == c.Id && a.DefinitionId == StatAttributeRow.MasterLevelId).Select(a => a.Value).FirstOrDefault(),
            Resets = attributes.Where(a => a.CharacterId == c.Id && a.DefinitionId == StatAttributeRow.ResetsId).Select(a => a.Value).FirstOrDefault(),
            Experience = c.Experience,
            GuildName = this._context.GuildMembers
                .Where(m => m.Id == c.Id)
                .Join(this._context.Guilds, m => m.GuildId, g => g.Id, (m, g) => g.Name)
                .FirstOrDefault(),
        });
    }

    private async Task<IReadOnlyList<T>> GetCachedAsync<T>(string key, Func<Task<List<T>>> factory)
    {
        var result = await this._cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = this._options.CacheDuration;
            return factory();
        }).ConfigureAwait(false);

        return result ?? [];
    }

    /// <summary>
    /// Intermediate projection of a character, which can be translated to SQL.
    /// </summary>
    private sealed class CharacterProjection
    {
        public string Name { get; init; } = string.Empty;

        public string? ClassName { get; init; }

        public float Level { get; init; }

        public float MasterLevel { get; init; }

        public float Resets { get; init; }

        public long Experience { get; init; }

        public string? GuildName { get; init; }

        public CharacterRankingEntry ToEntry(int rank) =>
            new(rank, this.Name, this.ClassName ?? "?", (int)this.Level, (int)this.MasterLevel, (int)this.Resets, this.GuildName);
    }
}
