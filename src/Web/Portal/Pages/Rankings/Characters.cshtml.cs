// <copyright file="Characters.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages.Rankings;

using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The character ranking.
/// </summary>
public class CharactersModel : PageModel
{
    private readonly RankingService _rankingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharactersModel"/> class.
    /// </summary>
    /// <param name="rankingService">The ranking service.</param>
    public CharactersModel(RankingService rankingService)
    {
        this._rankingService = rankingService;
    }

    /// <summary>
    /// Gets the ranking entries.
    /// </summary>
    public IReadOnlyList<CharacterRankingEntry> Entries { get; private set; } = [];

    /// <summary>
    /// Handles the GET request.
    /// </summary>
    public async Task OnGetAsync()
    {
        this.Entries = await this._rankingService.GetCharacterRankingAsync(this.HttpContext.RequestAborted).ConfigureAwait(false);
    }
}
