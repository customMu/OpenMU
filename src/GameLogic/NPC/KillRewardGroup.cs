// <copyright file="KillRewardGroup.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// A group which dealt damage to a monster: either a single player or a party.
/// </summary>
internal sealed class KillRewardGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KillRewardGroup"/> class.
    /// </summary>
    /// <param name="party">The party, or <c>null</c> for a single player.</param>
    public KillRewardGroup(Party? party)
    {
        this.Party = party;
    }

    /// <summary>
    /// Gets the party, or <c>null</c> for a single player.
    /// </summary>
    public Party? Party { get; }

    /// <summary>
    /// Gets or sets the total damage of the group.
    /// </summary>
    public long Damage { get; set; }

    /// <summary>
    /// Gets or sets the living member with the highest damage, which represents the group.
    /// Experience goes to the party members in the view range of this player.
    /// </summary>
    public Player? Representative { get; set; }

    /// <summary>
    /// Gets or sets the damage of the <see cref="Representative"/>.
    /// </summary>
    public long RepresentativeDamage { get; set; }

    /// <summary>
    /// Builds the groups from the damage per player. Players which left the map or are offline are ignored;
    /// their damage still counts for their party, if another member can represent it.
    /// Groups without a living representative on the map are left out.
    /// </summary>
    /// <param name="damageByPlayer">The damage per player.</param>
    /// <param name="map">The map of the killed monster.</param>
    /// <returns>The groups with a representative.</returns>
    public static List<KillRewardGroup> Create(IEnumerable<KeyValuePair<Player, long>> damageByPlayer, GameMap map)
    {
        var groups = new Dictionary<object, KillRewardGroup>();
        foreach (var (player, damage) in damageByPlayer)
        {
            if (damage <= 0 || player.Attributes is null || player.CurrentMap != map)
            {
                continue;
            }

            var party = player.Party;
            object key = (object?)party ?? player;
            if (!groups.TryGetValue(key, out var group))
            {
                group = new KillRewardGroup(party);
                groups.Add(key, group);
            }

            group.Damage += damage;
            if (player.IsAlive && (group.Representative is null || damage > group.RepresentativeDamage))
            {
                group.Representative = player;
                group.RepresentativeDamage = damage;
            }
        }

        return groups.Values.Where(g => g.Representative is not null).ToList();
    }
}
