// <copyright file="Logout.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages.Account;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// Signs the user out. Only POST is accepted, so that a link on another site can't log the user out.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// Handles the GET request.
    /// </summary>
    /// <returns>A redirect to the start page.</returns>
    public IActionResult OnGet() => this.RedirectToPage("/Index");

    /// <summary>
    /// Handles the POST request.
    /// </summary>
    /// <returns>A redirect to the start page.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return this.RedirectToPage("/Index");
    }
}
