// <copyright file="ItemDropOwnerExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Extensions to drop items for a killer, considering the drop mode of its party.
/// </summary>
public static class ItemDropOwnerExtensions
{
    /// <summary>
    /// Creates a dropped item for a kill of the specified player. The owners depend on the drop mode of its party.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="item">The item.</param>
    /// <param name="position">The position of the drop.</param>
    /// <param name="map">The map.</param>
    /// <returns>The dropped item, which still needs to be added to the map.</returns>
    public static DroppedItem CreateDropForKiller(this Player killer, Item item, Point position, GameMap map)
    {
        if (killer.Party is not { } party)
        {
            return new DroppedItem(item, position, map, null, killer.GetAsEnumerable());
        }

        // Quest items are free for the party in every mode: only members with the quest can pick them up anyway,
        // an assignment to a member without the quest would lose the item.
        if (item.Definition?.IsQuestItem == true)
        {
            return new DroppedItem(item, position, map, null, party.PartyList, false, DroppedItem.TimeUntilDropIsFree);
        }

        var owners = party.GetItemOwners(killer, position);
        var priority = party.DropMode == PartyDropMode.Free ? DroppedItem.TimeUntilDropIsFree : DroppedItem.TimeUntilAssignedDropIsFree;
        return new DroppedItem(item, position, map, null, owners, false, priority);
    }
}
