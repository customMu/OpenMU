// <copyright file="AccountService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Portal.Data;
using Npgsql;

/// <summary>
/// Registers and authenticates game accounts.
/// </summary>
public class AccountService
{
    /// <summary>
    /// A hash which is verified when an account doesn't exist, so that the response time
    /// doesn't reveal whether a login name is taken.
    /// </summary>
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    private readonly PortalDbContext _context;
    private readonly ILogger<AccountService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public AccountService(PortalDbContext context, ILogger<AccountService> logger)
    {
        this._context = context;
        this._logger = logger;
    }

    /// <summary>
    /// Registers a new account.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The password.</param>
    /// <param name="email">The e-mail address.</param>
    /// <param name="securityCode">The security code, which is required to delete characters in the game.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c>, if the account was created; <c>false</c>, if the login name is already taken.</returns>
    public async Task<bool> RegisterAsync(string loginName, string password, string email, string securityCode, CancellationToken cancellationToken = default)
    {
        // The unique index is case-sensitive, but names which differ only in case would confuse players.
        var lowerName = loginName.ToLowerInvariant();
        if (await this._context.Accounts.AnyAsync(a => a.LoginName.ToLower() == lowerName, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        this._context.Accounts.Add(new AccountRow
        {
            Id = GuidV7.NewGuid(),
            LoginName = loginName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            EMail = email,
            SecurityCode = securityCode,
            RegistrationDate = DateTime.UtcNow,
            State = AccountState.Normal,
        });

        try
        {
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Someone else registered the same name in the meantime.
            this._context.ChangeTracker.Clear();
            return false;
        }

        this._logger.LogInformation("Registered account {LoginName}.", loginName);
        return true;
    }

    /// <summary>
    /// Authenticates an account.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account, if the credentials are valid; otherwise, <c>null</c>.</returns>
    public async Task<AccountRow?> AuthenticateAsync(string loginName, string password, CancellationToken cancellationToken = default)
    {
        var account = await this._context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.LoginName == loginName && !a.IsTemplate && !a.IsBot, cancellationToken)
            .ConfigureAwait(false);

        var isValid = BCrypt.Net.BCrypt.Verify(password, account?.PasswordHash ?? DummyHash);
        return isValid ? account : null;
    }
}
