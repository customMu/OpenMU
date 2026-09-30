// <copyright file="AdditionalItemDropArgs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The arguments for <see cref="IAdditionalItemDropPlugIn"/>.
/// </summary>
public sealed class AdditionalItemDropArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalItemDropArgs"/> class.
    /// </summary>
    /// <param name="killedObject">The killed object.</param>
    /// <param name="monster">The definition of the killed monster.</param>
    /// <param name="map">The map on which the monster was killed.</param>
    /// <param name="killer">The player who owns the drop.</param>
    /// <param name="items">The items which are going to be dropped. Additional items are added to this list.</param>
    public AdditionalItemDropArgs(IAttackable killedObject, MonsterDefinition monster, GameMap map, Player killer, List<Item> items)
    {
        this.KilledObject = killedObject;
        this.Monster = monster;
        this.Map = map;
        this.Killer = killer;
        this.Items = items;
    }

    /// <summary>
    /// Gets the killed object.
    /// </summary>
    public IAttackable KilledObject { get; }

    /// <summary>
    /// Gets the definition of the killed monster.
    /// </summary>
    public MonsterDefinition Monster { get; }

    /// <summary>
    /// Gets the map on which the monster was killed.
    /// </summary>
    public GameMap Map { get; }

    /// <summary>
    /// Gets the player who owns the drop.
    /// </summary>
    public Player Killer { get; }

    /// <summary>
    /// Gets the items which are going to be dropped. Additional items are added to this list.
    /// </summary>
    public List<Item> Items { get; }
}
