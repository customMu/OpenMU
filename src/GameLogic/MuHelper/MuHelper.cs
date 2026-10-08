// <copyright file="MuHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MuHelper;

using System.Diagnostics;
using System.Threading;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views.MuHelper;

/// <summary>
/// Implements the logic of the 'MU Helper'.
/// </summary>
public class MuHelper : AsyncDisposable
{
    /// <summary>
    /// The <see cref="IElement"/> which is added to the players <see cref="IAttributeSystem"/>
    /// when the MU Helper is active.
    /// </summary>
    private static readonly IElement ActiveElement = new ConstantElement(1);

    /// <summary>
    /// Associated player.
    /// </summary>
    private readonly Player _player;

    /// <summary>
    /// The current configuration.
    /// </summary>
    private readonly MuHelperConfiguration _configuration;

    private CancellationTokenSource? _stopCts;
    private Task? _runTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="MuHelper"/> class.
    /// </summary>
    /// <param name="player">current player.</param>
    public MuHelper(Player player)
    {
        this._player = player;
        this._configuration = this._player.GameContext.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()?.Configuration ?? new MuHelperConfiguration();
    }

    /// <summary>
    /// Start Mu Helper.
    /// </summary>
    public async ValueTask<bool> TryStartAsync()
    {
        if (this._runTask is not null)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperAlreadyRunning)).ConfigureAwait(false);
            return false;
        }

        if (this._player.Level < this._configuration.MinLevel)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperMinimumLevel), this._configuration.MinLevel).ConfigureAwait(false);
            return false;
        }

        if (this._player.Level > this._configuration.MaxLevel)
        {
            await this._player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MuHelperMaximumLevel), this._configuration.MaxLevel).ConfigureAwait(false);
            return false;
        }

        // The helper has no fee by time: it keeps a share of the picked up zen (MuHelperZenFeePlugIn).
        await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.StartAsync()).ConfigureAwait(false);

        this._player.Attributes?.AddElement(ActiveElement, Stats.IsMuHelperActive);
        this._stopCts?.Dispose();
        this._stopCts = new CancellationTokenSource();
        var cts = this._stopCts.Token;
        this._runTask = this.RunLoopAsync(cts);
        return true;
    }

    /// <summary>
    /// Stops the MU Helper.
    /// </summary>
    public async ValueTask StopAsync()
    {
        if (this._runTask is not { } runTask
            || this._stopCts is not { } stopCts)
        {
            return;
        }

        try
        {
            this._player.Attributes?.RemoveElement(ActiveElement, Stats.IsMuHelperActive);

            await stopCts.CancelAsync().ConfigureAwait(false);

            // Skip awaiting the loop task if we are currently executing inside it
            // (i.e. CollectAsync triggered StopAsync), to avoid a self-deadlock.
            if (runTask.Id != Task.CurrentId)
            {
                await runTask.ConfigureAwait(false);
            }

            this._runTask = null;
            stopCts.Dispose();
            this._stopCts = null;

            await this._player.InvokeViewPlugInAsync<IMuHelperStatusUpdatePlugIn>(p => p.StopAsync()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._player.Logger.LogWarning(ex, "Exception during stopping the mu helper for {CharacterName}: {Error}", this._player.Name, ex.Message);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await this.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a value indicating whether the helper runs.
    /// </summary>
    public bool IsRunning => this._runTask is not null;

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // runs until StopAsync cancels it
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // expected when StopAsync cancels the token.
        }
    }
}
