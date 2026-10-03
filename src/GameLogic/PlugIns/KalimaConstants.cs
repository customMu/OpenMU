// <copyright file="KalimaConstants.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Constants for the Kalima map.
/// </summary>
public static class KalimaConstants
{
    /// <summary>
    /// The symbol of kundun item group.
    /// </summary>
    internal const byte SymbolOfKundunGroup = 14;

    /// <summary>
    /// The symbol of kundun item number.
    /// </summary>
    internal const byte SymbolOfKundunNumber = 29;

    /// <summary>
    /// The lost map item group.
    /// </summary>
    internal const byte LostMapGroup = 14;

    /// <summary>
    /// The lost map item number.
    /// </summary>
    internal const byte LostMapNumber = 28;

    /// <summary>
    /// The value of <see cref="Item.PetExperience"/> which marks a lost map as bound to the character.
    /// A lost map is no pet, so this otherwise unused field of the item carries the binding.
    /// </summary>
    private const int BoundLostMapMarker = 1;

    /// <summary>
    /// Determines whether the specified item is a lost map.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>
    ///   <c>true</c> if the specified item is a lost map; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLostMap(this Item item)
    {
        return item.Definition.IsLostMap();
    }

    /// <summary>
    /// Determines whether the specified item is a lost map.
    /// </summary>
    /// <param name="itemDefinition">The item definition.</param>
    /// <returns>
    ///   <c>true</c> if the specified item is a lost map; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsLostMap(this ItemDefinition? itemDefinition)
    {
        return itemDefinition is { Group: LostMapGroup, Number: LostMapNumber };
    }

    /// <summary>
    /// Determines whether the specified item is bound to the character, either by its definition,
    /// or because it's a lost map which was bound when it was bought (e.g. in the essence shop).
    /// A bound item can't leave the inventory (no trade, personal store or vault) and can't be sold.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if the item is bound to the character.</returns>
    public static bool IsBoundToCharacter(this Item item)
    {
        return item.Definition?.IsBoundToCharacter is true
               || (item.IsLostMap() && item.PetExperience == BoundLostMapMarker);
    }

    /// <summary>
    /// Binds the lost map to the character.
    /// </summary>
    /// <param name="item">The lost map.</param>
    /// <returns><c>true</c>, if the item is a lost map and has been bound.</returns>
    public static bool TryBindLostMap(this Item item)
    {
        if (!item.IsLostMap())
        {
            return false;
        }

        item.PetExperience = BoundLostMapMarker;
        return true;
    }

    /// <summary>
    /// Determines whether the specified item is a symbol of kundun.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>
    ///   <c>true</c> if the specified item is a symbol of kundun; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSymbolOfKundun(this Item item)
    {
        return item.Definition is { Group: SymbolOfKundunGroup, Number: SymbolOfKundunNumber };
    }
}