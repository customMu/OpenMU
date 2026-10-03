// <copyright file="AccountVaultService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Reads and changes the vault of an account for a website, e.g. to move zen and items between
/// the game and a web auction, no matter if the account is in the game or not.
/// </summary>
/// <remarks>
/// While the account is in the game, the server holds it in memory and saves it periodically,
/// so a change made directly in the database would be overwritten. That's why the change is
/// done here, on the account in memory, while holding the persistence lock of the player, which
/// also serializes it against the actions of the player. The vault must not be open in the game,
/// because the open vault window keeps its own copy of the item positions.
/// When the account is not in the game, it's registered at the login server for the time of the
/// change, so that nobody can log in with it before the change is saved.
/// </remarks>
public sealed class AccountVaultService
{
    /// <summary>
    /// The server id under which an account is registered at the login server while it's changed here.
    /// </summary>
    public const byte WebsiteServerId = byte.MaxValue;

    private readonly IDictionary<int, IGameServer> _gameServers;
    private readonly ILoginServer _loginServer;
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly ILogger<AccountVaultService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountVaultService"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers of this process.</param>
    /// <param name="loginServer">The login server.</param>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="logger">The logger.</param>
    public AccountVaultService(
        IDictionary<int, IGameServer> gameServers,
        ILoginServer loginServer,
        IPersistenceContextProvider persistenceContextProvider,
        ILogger<AccountVaultService> logger)
    {
        this._gameServers = gameServers;
        this._loginServer = loginServer;
        this._persistenceContextProvider = persistenceContextProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the vault of the account.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <returns>The vault.</returns>
    public ValueTask<VaultResult<VaultView>> GetVaultAsync(string accountName)
    {
        return this.RunAsync(accountName, false, access =>
        {
            var vault = access.Account.Vault;
            var items = vault?.Items.Where(i => i.Definition is not null).OrderBy(i => i.ItemSlot).Select(VaultItemMapper.Describe).ToList() ?? [];
            var view = new VaultView(vault?.Money ?? 0, access.Configuration.MaximumVaultMoney, access.Player is not null, items);
            return ValueTask.FromResult(VaultResult<VaultView>.Success(view));
        });
    }

    /// <summary>
    /// Puts zen into the vault or takes it out.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="amount">The zen to put into the vault; negative to take zen out.</param>
    /// <returns>The zen in the vault after the change.</returns>
    public ValueTask<VaultResult<int>> ChangeMoneyAsync(string accountName, int amount)
    {
        return this.RunAsync(accountName, true, async access =>
        {
            var vault = access.GetOrCreateVault();
            if (amount < 0 && vault.Money < -(long)amount)
            {
                return VaultResult<int>.Fail(VaultResultCode.NotEnoughMoney);
            }

            if (amount > 0 && (long)vault.Money + amount > access.Configuration.MaximumVaultMoney)
            {
                return VaultResult<int>.Fail(VaultResultCode.MoneyLimitExceeded);
            }

            vault.Money += amount;
            var message = amount > 0
                ? $"Website: {amount:N0} zen were put into your vault."
                : $"Website: {-amount:N0} zen were taken from your vault.";
            await access.NotifyAsync(message).ConfigureAwait(false);
            this._logger.LogInformation("Website changed the vault zen of account {Account} by {Amount} to {Money}.", accountName, amount, vault.Money);
            return VaultResult<int>.Success(vault.Money);
        });
    }

    /// <summary>
    /// Takes an item out of the vault and out of the game.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="itemId">The id of the item.</param>
    /// <returns>The taken item with the data to create it again.</returns>
    public ValueTask<VaultResult<VaultItem>> TakeItemAsync(string accountName, Guid itemId)
    {
        return this.RunAsync(accountName, true, async access =>
        {
            var item = access.Account.Vault?.Items.FirstOrDefault(i => i.GetId() == itemId);
            if (item?.Definition is null)
            {
                return VaultResult<VaultItem>.Fail(VaultResultCode.NotFound);
            }

            var description = VaultItemMapper.Describe(item);
            await access.CreateStorage().RemoveItemAsync(item).ConfigureAwait(false);
            await access.Context.DeleteAsync(item).ConfigureAwait(false);
            await access.NotifyAsync($"Website: {description.Name} was taken from your vault.").ConfigureAwait(false);
            this._logger.LogInformation("Website took item {Item} ({ItemId}) from the vault of account {Account}: {Data}", description.Name, itemId, accountName, description.Data);
            return VaultResult<VaultItem>.Success(description);
        });
    }

    /// <summary>
    /// Creates an item in a free place of the vault.
    /// </summary>
    /// <param name="accountName">The login name of the account.</param>
    /// <param name="data">The data of the item.</param>
    /// <returns>The created item.</returns>
    public ValueTask<VaultResult<VaultItem>> PutItemAsync(string accountName, VaultItemData data)
    {
        return this.RunAsync(accountName, true, async access =>
        {
            access.GetOrCreateVault();
            if (VaultItemMapper.TryCreate(data, access.Configuration, access.Context) is not { } item)
            {
                return VaultResult<VaultItem>.Fail(VaultResultCode.InvalidItem);
            }

            if (!await access.CreateStorage().AddItemAsync(item).ConfigureAwait(false))
            {
                await access.Context.DeleteAsync(item).ConfigureAwait(false);
                return VaultResult<VaultItem>.Fail(VaultResultCode.NoSpace);
            }

            var description = VaultItemMapper.Describe(item);
            await access.NotifyAsync($"Website: {description.Name} was put into your vault.").ConfigureAwait(false);
            this._logger.LogInformation("Website put item {Item} into the vault of account {Account}: {Data}", description.Name, accountName, data);
            return VaultResult<VaultItem>.Success(description);
        });
    }

    private static async ValueTask<Player?> FindPlayerAsync(IEnumerable<GameServer.GameServer> gameServers, string accountName)
    {
        foreach (var gameServer in gameServers)
        {
            var players = await gameServer.Context.GetPlayersAsync().ConfigureAwait(false);
            if (players.FirstOrDefault(p => p.Account?.LoginName == accountName) is { } player)
            {
                return player;
            }
        }

        return null;
    }

    private static ValueTask<VaultResult<T>> RunOnPlayerAsync<T>(Player player, bool modifies, Func<VaultAccess, ValueTask<VaultResult<T>>> operation)
    {
        return player.RunPersistenceExclusiveAsync(async () =>
        {
            if (player.Account is not { } account)
            {
                return VaultResult<T>.Fail(VaultResultCode.Busy);
            }

            if (modifies && player.Vault is not null)
            {
                return VaultResult<T>.Fail(VaultResultCode.VaultOpen);
            }

            var access = new VaultAccess(account, player.PersistenceContext, player.GameContext.Configuration, player);
            var result = await operation(access).ConfigureAwait(false);
            if (modifies && result.IsSuccess && !await player.SaveProgressAsync().ConfigureAwait(false))
            {
                // The change stays in memory and is saved with the next progress save.
                player.Logger.LogWarning("Saving the vault change of the website failed for account {Account}.", account.LoginName);
            }

            return result;
        });
    }

    private async ValueTask<VaultResult<T>> RunAsync<T>(string accountName, bool modifies, Func<VaultAccess, ValueTask<VaultResult<T>>> operation)
    {
        var gameServers = this._gameServers.Values.OfType<GameServer.GameServer>().ToList();
        if (gameServers.Count == 0)
        {
            return VaultResult<T>.Fail(VaultResultCode.Unavailable);
        }

        if (await FindPlayerAsync(gameServers, accountName).ConfigureAwait(false) is { } player)
        {
            return await RunOnPlayerAsync(player, modifies, operation).ConfigureAwait(false);
        }

        var configuration = gameServers[0].Context.Configuration;
        if (!modifies)
        {
            return await this.RunOnDatabaseAsync(accountName, configuration, false, operation).ConfigureAwait(false);
        }

        if (!await this._loginServer.TryLoginAsync(accountName, WebsiteServerId).ConfigureAwait(false))
        {
            // Logged in a moment ago, or just logging in, or another change is running.
            return VaultResult<T>.Fail(VaultResultCode.Busy);
        }

        try
        {
            return await this.RunOnDatabaseAsync(accountName, configuration, true, operation).ConfigureAwait(false);
        }
        finally
        {
            await this._loginServer.LogOffAsync(accountName, WebsiteServerId).ConfigureAwait(false);
        }
    }

    private async ValueTask<VaultResult<T>> RunOnDatabaseAsync<T>(string accountName, GameConfiguration configuration, bool modifies, Func<VaultAccess, ValueTask<VaultResult<T>>> operation)
    {
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(configuration);
        if (await context.GetAccountByLoginNameAsync(accountName).ConfigureAwait(false) is not { } account)
        {
            return VaultResult<T>.Fail(VaultResultCode.NotFound);
        }

        var access = new VaultAccess(account, context, configuration, null);
        var result = await operation(access).ConfigureAwait(false);
        if (modifies && result.IsSuccess && !await context.SaveChangesAsync().ConfigureAwait(false))
        {
            this._logger.LogError("Saving the vault change of the website failed for account {Account}.", accountName);
            return VaultResult<T>.Fail(VaultResultCode.Busy);
        }

        return result;
    }

    /// <summary>
    /// The account whose vault is read or changed, and how it's accessed.
    /// </summary>
    /// <param name="Account">The account.</param>
    /// <param name="Context">The context which tracks the account.</param>
    /// <param name="Configuration">The game configuration of the context.</param>
    /// <param name="Player">The player, when the account is in the game.</param>
    private sealed record VaultAccess(Account Account, IContext Context, GameConfiguration Configuration, Player? Player)
    {
        public ItemStorage GetOrCreateVault() => this.Account.Vault ??= this.Context.CreateNew<ItemStorage>();

        public Storage CreateStorage()
        {
            var size = this.Account.IsVaultExtended ? InventoryConstants.WarehouseSize * 2 : InventoryConstants.WarehouseSize;
            return new Storage(size, this.GetOrCreateVault());
        }

        public ValueTask NotifyAsync(string message) => this.Player?.ShowBlueMessageAsync(message) ?? ValueTask.CompletedTask;
    }
}
