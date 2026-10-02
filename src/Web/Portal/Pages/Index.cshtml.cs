// <copyright file="Index.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The start page.
/// </summary>
public class IndexModel : PageModel
{
    private const int TopCount = 10;

    private readonly RankingService _rankingService;
    private readonly ServerStatusService _statusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="rankingService">The ranking service.</param>
    /// <param name="statusService">The server status service.</param>
    /// <param name="options">The portal options.</param>
    public IndexModel(RankingService rankingService, ServerStatusService statusService, IOptions<PortalOptions> options)
    {
        this._rankingService = rankingService;
        this._statusService = statusService;
        this.ServerName = options.Value.ServerName;
    }

    /// <summary>
    /// Gets the name of the server.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Gets the server status.
    /// </summary>
    public ServerStatus? Status { get; private set; }

    /// <summary>
    /// Gets the top characters.
    /// </summary>
    public IReadOnlyList<CharacterRankingEntry> TopCharacters { get; private set; } = [];

    /// <summary>
    /// Gets the top guilds.
    /// </summary>
    public IReadOnlyList<GuildRankingEntry> TopGuilds { get; private set; } = [];

    /// <summary>
    /// Handles the GET request.
    /// </summary>
    public async Task OnGetAsync()
    {
        this.Status = await this._statusService.GetStatusAsync(this.HttpContext.RequestAborted).ConfigureAwait(false);
        this.TopCharacters = (await this._rankingService.GetCharacterRankingAsync(this.HttpContext.RequestAborted).ConfigureAwait(false)).Take(TopCount).ToList();
        this.TopGuilds = (await this._rankingService.GetGuildRankingAsync(this.HttpContext.RequestAborted).ConfigureAwait(false)).Take(5).ToList();
    }
}
