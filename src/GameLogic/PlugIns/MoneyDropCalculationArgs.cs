// <copyright file="MoneyDropCalculationArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// The arguments of the <see cref="IMoneyDropCalculationPlugIn"/>. Plugin points can't return
/// values, so the calculated amount is passed in and out through this object.
/// </summary>
public sealed class MoneyDropCalculationArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyDropCalculationArgs"/> class.
    /// </summary>
    /// <param name="killedObject">The killed object which drops the money.</param>
    /// <param name="monster">The definition of the killed monster.</param>
    /// <param name="map">The map on which the monster has been killed.</param>
    /// <param name="killer">The player which killed the monster.</param>
    /// <param name="participants">The players which take part in the money drop (killer and nearby party members).</param>
    /// <param name="amount">The amount which was calculated by the drop generator.</param>
    public MoneyDropCalculationArgs(IAttackable killedObject, MonsterDefinition monster, GameMap map, Player killer, IReadOnlyList<Player> participants, uint amount)
    {
        this.KilledObject = killedObject;
        this.Monster = monster;
        this.Map = map;
        this.Killer = killer;
        this.Participants = participants;
        this.Amount = amount;
    }

    /// <summary>
    /// Gets the killed object which drops the money.
    /// </summary>
    public IAttackable KilledObject { get; }

    /// <summary>
    /// Gets the definition of the killed monster.
    /// </summary>
    public MonsterDefinition Monster { get; }

    /// <summary>
    /// Gets the map on which the monster has been killed.
    /// </summary>
    public GameMap Map { get; }

    /// <summary>
    /// Gets the player which killed the monster.
    /// </summary>
    public Player Killer { get; }

    /// <summary>
    /// Gets the players which take part in the money drop.
    /// </summary>
    public IReadOnlyList<Player> Participants { get; }

    /// <summary>
    /// Gets or sets the total amount of money which is dropped, before the money rate of the receiving players is applied.
    /// A value of <c>0</c> suppresses the money drop.
    /// </summary>
    public uint Amount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the money is split equally between the <see cref="Participants"/>,
    /// instead of proportionally to their gained experience.
    /// </summary>
    public bool SplitEqually { get; set; }
}
