// <copyright file="Index.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages.Account;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The overview of the logged in account.
/// </summary>
public class AccountIndexModel : PageModel
{
    private readonly RankingService _rankingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountIndexModel"/> class.
    /// </summary>
    /// <param name="rankingService">The ranking service.</param>
    public AccountIndexModel(RankingService rankingService)
    {
        this._rankingService = rankingService;
    }

    /// <summary>
    /// Gets the characters of the account.
    /// </summary>
    public IReadOnlyList<CharacterRankingEntry> Characters { get; private set; } = [];

    /// <summary>
    /// Handles the GET request.
    /// </summary>
    /// <returns>The page.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        if (!Guid.TryParse(this.User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
        {
            return this.Challenge();
        }

        this.Characters = await this._rankingService.GetCharactersOfAccountAsync(accountId, this.HttpContext.RequestAborted).ConfigureAwait(false);
        return this.Page();
    }
}
