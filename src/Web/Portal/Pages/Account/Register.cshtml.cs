// <copyright file="Register.cshtml.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The registration of a new account.
/// </summary>
[EnableRateLimiting(RateLimitPolicies.Credentials)]
public class RegisterModel : PageModel
{
    private const int MinimumPasswordLength = 3;

    private readonly AccountService _accountService;
    private readonly PortalOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterModel"/> class.
    /// </summary>
    /// <param name="accountService">The account service.</param>
    /// <param name="options">The portal options.</param>
    public RegisterModel(AccountService accountService, IOptions<PortalOptions> options)
    {
        this._accountService = accountService;
        this._options = options.Value;
    }

    /// <summary>
    /// Gets or sets the input.
    /// </summary>
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether the registration is enabled.
    /// </summary>
    public bool RegistrationEnabled => this._options.RegistrationEnabled;

    /// <summary>
    /// Gets the maximum password length.
    /// </summary>
    public int MaximumPasswordLength => this._options.MaximumPasswordLength;

    /// <summary>
    /// Gets a value indicating whether the account was registered successfully.
    /// </summary>
    public bool Registered { get; private set; }

    /// <summary>
    /// Handles the POST request.
    /// </summary>
    /// <returns>The page.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.RegistrationEnabled)
        {
            return this.Page();
        }

        var passwordLength = this.Input.Password.Length;
        if (passwordLength < MinimumPasswordLength || passwordLength > this.MaximumPasswordLength)
        {
            this.ModelState.AddModelError($"{nameof(this.Input)}.{nameof(RegisterInput.Password)}", $"The password must have {MinimumPasswordLength} to {this.MaximumPasswordLength} characters.");
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        if (!await this._accountService.RegisterAsync(this.Input.LoginName, this.Input.Password, this.Input.EMail, this.Input.SecurityCode, this.HttpContext.RequestAborted).ConfigureAwait(false))
        {
            this.ModelState.AddModelError($"{nameof(this.Input)}.{nameof(RegisterInput.LoginName)}", "This login name is already taken.");
            return this.Page();
        }

        this.Registered = true;
        return this.Page();
    }

    /// <summary>
    /// The input of the registration form.
    /// </summary>
    public class RegisterInput
    {
        /// <summary>
        /// Gets or sets the login name.
        /// </summary>
        [Required]
        [RegularExpression("^[A-Za-z0-9]{3,10}$", ErrorMessage = "The login name must have 3 to 10 letters or digits.")]
        public string LoginName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password. The length is validated against the configured maximum.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [RegularExpression("^[\\x21-\\x7E]+$", ErrorMessage = "The password may only contain latin letters, digits and symbols.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password confirmation.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords don't match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the e-mail address.
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string EMail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the security code.
        /// </summary>
        [Required]
        [RegularExpression("^[0-9]{3,10}$", ErrorMessage = "The security code must have 3 to 10 digits.")]
        public string SecurityCode { get; set; } = string.Empty;
    }
}
