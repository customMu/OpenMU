// <copyright file="IIllusionOfNoriaViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Interface of a view which shows the dialog of the warden of the Illusion of Noria: the quest of the whistle, the entry
/// and the daily quest. Without such a view, the warden only tells the state in the chat.
/// </summary>
public interface IIllusionOfNoriaViewPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the dialog of the warden.
    /// </summary>
    /// <param name="info">The state of the player for the dialog.</param>
    /// <returns>The task.</returns>
    ValueTask ShowWardenDialogAsync(IllusionWardenInfo info);
}

/// <summary>
/// The state of a player for the dialog of the warden of the Illusion of Noria.
/// </summary>
/// <param name="InIllusion">A value indicating whether the warden stands in the illusion (else in Noria).</param>
/// <param name="QuestState">The state of the quest of the whistle: 0 not taken, 1 taken, 2 done.</param>
/// <param name="Resets">The resets of the character.</param>
/// <param name="RequiredResets">The resets which are needed.</param>
/// <param name="HasWhistle">A value indicating whether the character carries the whistle.</param>
/// <param name="DailyState">The state of the daily quest: 0 can be taken, 1 active, 2 completed (reward waits), 3 done today.</param>
/// <param name="DailyMonsters">The monster numbers of the daily quest (empty when not taken).</param>
/// <param name="DailyKills">The kills of each monster of the daily quest.</param>
/// <param name="DailyKillsNeeded">The kills which are needed for each monster.</param>
/// <param name="TimeUntilNextDay">The time until the next day of the daily quest.</param>
/// <param name="Weapons">The equipped weapons with a harmony option which the warden can reset.</param>
/// <param name="Shards">The Illusion Shards of the character.</param>
/// <param name="Exchange">The shop of the warden (empty before the quest is done): item group, number and price in shards.</param>
public sealed record IllusionWardenInfo(
    bool InIllusion,
    byte QuestState,
    int Resets,
    int RequiredResets,
    bool HasWhistle,
    byte DailyState,
    IReadOnlyList<short> DailyMonsters,
    IReadOnlyList<int> DailyKills,
    int DailyKillsNeeded,
    TimeSpan TimeUntilNextDay,
    IReadOnlyList<IllusionResetWeapon> Weapons,
    int Shards,
    IReadOnlyList<(byte Group, short Number, int Price)> Exchange);

/// <summary>
/// An equipped weapon with a harmony option, which the warden can reset for a price.
/// </summary>
/// <param name="Slot">The inventory slot (0 left hand, 1 right hand).</param>
/// <param name="Group">The item group.</param>
/// <param name="Number">The item number.</param>
/// <param name="OptionNumber">The number of the harmony option.</param>
/// <param name="OptionLevel">The level of the harmony option.</param>
/// <param name="Rank">The rank of the weapon (7, 8).</param>
/// <param name="JewelsOfIllusion">The Jewels of Illusion of the price.</param>
/// <param name="LesserStones">The Lesser Mirage Stones of the price.</param>
/// <param name="GreaterStones">The Greater Mirage Stones of the price.</param>
/// <param name="CanPay">A value indicating whether the player carries the price.</param>
public sealed record IllusionResetWeapon(byte Slot, byte Group, short Number, byte OptionNumber, byte OptionLevel, byte Rank, byte JewelsOfIllusion, byte LesserStones, byte GreaterStones, bool CanPay);
