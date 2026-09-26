// <copyright file="IMoneyDropCalculationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which is called when a monster has been killed and its money drop is about to be
/// handed out, so that the amount and the way it is split between the players can be changed.
/// </summary>
/// <remarks>
/// If no plugin is active, the amount which the drop generator calculated is used, and the money is split
/// proportionally to the experience gained by the players.
/// </remarks>
[Guid("A90A6C8E-CD7D-47A3-82F4-DE3BA6AFAD66")]
[PlugInPoint("Money drop calculation", "Plugins which calculate or modify the amount of money which a killed monster drops, and how it is split between players.")]
public interface IMoneyDropCalculationPlugIn
{
    /// <summary>
    /// Is called when the money drop of a killed monster is about to be handed out.
    /// </summary>
    /// <param name="args">The arguments, which hold the amount and can be modified by the plugin.</param>
    ValueTask CalculateMoneyDropAsync(MoneyDropCalculationArgs args);
}
