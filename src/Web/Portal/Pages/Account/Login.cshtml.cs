// <copyright file="Login.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The login with the game account.
/// </summary>
[EnableRateLimiting(RateLimitPolicies.Credentials)]
public class LoginModel : PageModel
{
    private readonly AccountService _accountService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginModel"/> class.
    /// </summary>
    /// <param name="accountService">The account service.</param>
    public LoginModel(AccountService accountService)
    {
        this._accountService = accountService;
    }

    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the URL to which the user is redirected after the login.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Handles the POST request.
    /// </summary>
    /// <returns>The page, or a redirect after a successful login.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var account = await this._accountService.AuthenticateAsync(this.Input.LoginName, this.Input.Password, this.HttpContext.RequestAborted).ConfigureAwait(false);
        if (account is null)
        {
            this.ModelState.AddModelError(string.Empty, "Invalid login name or password.");
            return this.Page();
        }

        if (account.State is AccountState.Banned or AccountState.TemporarilyBanned)
        {
            this.ModelState.AddModelError(string.Empty, "This account is banned.");
            return this.Page();
        }

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.LoginName),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
        await this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity)).ConfigureAwait(false);

        return this.LocalRedirect(this.Url.IsLocalUrl(this.ReturnUrl) ? this.ReturnUrl : "/Account");
    }

    /// <summary>
    /// The input of the login form.
    /// </summary>
    public class LoginInput
    {
        /// <summary>
        /// Gets or sets the login name.
        /// </summary>
        [Required]
        [StringLength(10)]
        public string LoginName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Required]
        [StringLength(20)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
