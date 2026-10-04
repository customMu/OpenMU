// <copyright file="AccountVaultController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.AdminPanel.Services.Vault;

/// <summary>
/// The vault of an account, for a website which moves zen and items between the game and
/// its own features, e.g. an auction. The changes work while the player is in the game, too.
/// </summary>
/// <remarks>
/// The API is called with an API key and not from a browser form, so it doesn't use antiforgery tokens.
/// Errors are returned as <c>{ "error": "&lt;VaultResultCode&gt;" }</c>: 404 for <c>NotFound</c>,
/// 409 for <c>Busy</c> and <c>VaultOpen</c> (try again later), 422 for <c>NotEnoughMoney</c>,
/// <c>MoneyLimitExceeded</c>, <c>NotEnoughItems</c> and <c>NoSpace</c>, 400 for <c>InvalidItem</c> and 503 for <c>Unavailable</c>.
/// </remarks>
[ApiController]
[RequireAntiforgeryToken(false)]
[Route("api/accounts/{accountName}/vault")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
public class AccountVaultController : Controller
{
    private readonly AccountVaultService _vaultService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountVaultController"/> class.
    /// </summary>
    /// <param name="vaultService">The vault service.</param>
    public AccountVaultController(AccountVaultService vaultService) => this._vaultService = vaultService;

    /// <inheritdoc />
    /// <remarks>
    /// The authorization policies let everybody in as long as no admin user exists (first start).
    /// The vault can be changed with this API, so it always requires an authenticated caller.
    /// </remarks>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (this.User.Identity?.IsAuthenticated is not true)
        {
            context.Result = this.Unauthorized();
            return;
        }

        base.OnActionExecuting(context);
    }

    /// <summary>
    /// Gets the zen and the items of the vault.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <returns>The vault.</returns>
    [HttpGet]
    public async Task<IActionResult> GetAsync(string accountName)
        => this.ToResponse(await this._vaultService.GetVaultAsync(accountName).ConfigureAwait(false));

    /// <summary>
    /// Puts zen into the vault, or takes it out with a negative amount.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="request">The request.</param>
    /// <returns>The zen in the vault after the change, as <c>{ "money": 123 }</c>.</returns>
    [HttpPost("money")]
    public async Task<IActionResult> ChangeMoneyAsync(string accountName, [FromBody] VaultMoneyRequest request)
    {
        var result = await this._vaultService.ChangeMoneyAsync(accountName, request.Amount).ConfigureAwait(false);
        return result.IsSuccess ? this.Ok(new { money = result.Value }) : this.ToError(result.Code);
    }

    /// <summary>
    /// Takes an item out of the vault and out of the game.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="itemId">The id of the item.</param>
    /// <param name="amount">The number of pieces to take from a stack (e.g. jewels); without it, the whole item is taken.</param>
    /// <returns>The taken item, including the data to put it into a vault again.</returns>
    [HttpPost("items/{itemId:guid}/take")]
    public async Task<IActionResult> TakeItemAsync(string accountName, Guid itemId, [FromQuery] int? amount = null)
        => this.ToResponse(await this._vaultService.TakeItemAsync(accountName, itemId, amount).ConfigureAwait(false));

    /// <summary>
    /// Creates an item in a free place of the vault.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="data">The data of the item, as it was returned when it was taken.</param>
    /// <returns>The created item.</returns>
    [HttpPost("items")]
    public async Task<IActionResult> PutItemAsync(string accountName, [FromBody] VaultItemData data)
        => this.ToResponse(await this._vaultService.PutItemAsync(accountName, data).ConfigureAwait(false));

    /// <summary>
    /// Puts pieces of a stackable item (e.g. jewels) into the vault, onto the existing stacks first,
    /// then into new stacks. Either all pieces are put into the vault, or none (<c>NoSpace</c>).
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="request">The item group and number and the number of pieces.</param>
    /// <returns>The filled up and created stacks.</returns>
    [HttpPost("stacks")]
    public async Task<IActionResult> PutStackAsync(string accountName, [FromBody] VaultStackRequest request)
        => this.ToResponse(await this._vaultService.PutStackAsync(accountName, request).ConfigureAwait(false));

    private IActionResult ToResponse<T>(VaultResult<T> result)
        => result.IsSuccess ? this.Ok(result.Value) : this.ToError(result.Code);

    private IActionResult ToError(VaultResultCode code)
    {
        var status = code switch
        {
            VaultResultCode.NotFound => StatusCodes.Status404NotFound,
            VaultResultCode.Busy or VaultResultCode.VaultOpen => StatusCodes.Status409Conflict,
            VaultResultCode.InvalidItem => StatusCodes.Status400BadRequest,
            VaultResultCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return this.StatusCode(status, new { error = code.ToString() });
    }
}
