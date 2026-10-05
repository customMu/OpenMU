// <copyright file="MuHelperZenFeePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MuHelper;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The MU Helper keeps a share of the zen which is picked up while it runs (online or offline leveling), instead of
/// the fee by time of the <see cref="MuHelperFeaturePlugIn"/>: the fee follows the income, so it never costs more
/// than the helper brings. While this plugin is active, the fee by time isn't taken.
/// </summary>
[PlugIn]
[Display(Name = "MU Helper zen fee", Description = "The MU Helper keeps a share of the picked up zen while it runs, instead of the fee every 5 minutes. The client shows the same share in the helper statistics (GameLogic/MuHelper/HelperZenFee.h) - keep them the same.")]
[Guid("7C2E9B41-5A8D-4F36-B0E2-1D9A6C3F8E57")]
public class MuHelperZenFeePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<MuHelperZenFeeConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public MuHelperZenFeeConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MuHelperZenFeeConfiguration();

    /// <summary>
    /// Determines whether the fee by picked up zen replaces the fee by time of the MU Helper.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns><c>true</c>, if the plugin is active.</returns>
    public static bool IsActive(IGameContext? gameContext) => gameContext?.FeaturePlugIns.GetPlugIn<MuHelperZenFeePlugIn>() is not null;

    /// <summary>
    /// Gets the zen which the MU Helper keeps of the picked up zen of the player; 0 when the helper doesn't run.
    /// </summary>
    /// <param name="player">The player who gets the zen.</param>
    /// <param name="amount">The picked up zen of the player.</param>
    /// <returns>The fee.</returns>
    public static long GetFee(Player player, long amount)
    {
        if (amount <= 0 || player.GameContext?.FeaturePlugIns.GetPlugIn<MuHelperZenFeePlugIn>() is not { } plugIn)
        {
            return 0;
        }

        var configuration = plugIn.Configuration ??= new MuHelperZenFeeConfiguration();
        float percent;
        if (player is OfflinePlayer)
        {
            if (player.Account?.IsBot == true)
            {
                return 0;
            }

            percent = configuration.OfflineFeePercent;
        }
        else if (player.MuHelper.IsRunning)
        {
            percent = player.Attributes?[Stats.IsVip] > 0 ? configuration.VipFeePercent : configuration.FeePercent;
        }
        else
        {
            return 0;
        }

        return (long)Math.Floor(amount * Math.Clamp(percent, 0f, 100f) / 100.0);
    }
}

/// <summary>
/// The configuration of the <see cref="MuHelperZenFeePlugIn"/>.
/// </summary>
public class MuHelperZenFeeConfiguration
{
    /// <summary>
    /// Gets or sets the share of the picked up zen in percent, which the helper keeps.
    /// </summary>
    [Display(Name = "Fee (%)", Description = "Share of the picked up zen which the MU Helper keeps while it runs.")]
    public float FeePercent { get; set; } = 20f;

    /// <summary>
    /// Gets or sets the share for VIP accounts in percent.
    /// </summary>
    [Display(Name = "VIP fee (%)", Description = "The share for VIP accounts (account attribute 'Is VIP').")]
    public float VipFeePercent { get; set; } = 20f;

    /// <summary>
    /// Gets or sets the share during offline leveling in percent.
    /// </summary>
    [Display(Name = "Offline fee (%)", Description = "The share during offline leveling.")]
    public float OfflineFeePercent { get; set; } = 20f;
}
