// <copyright file="MiniGameMonsterDrops.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The drops of a monster which was killed in a mini game that replaces the regular drops (e.g. the Kalima instance).
/// </summary>
/// <param name="Items">The dropped items.</param>
/// <param name="Money">The dropped money; 0 for none.</param>
public sealed record MiniGameMonsterDrops(IReadOnlyList<Item> Items, uint Money)
{
    /// <summary>
    /// Gets drops without items and money.
    /// </summary>
    public static MiniGameMonsterDrops None { get; } = new([], 0);
}
