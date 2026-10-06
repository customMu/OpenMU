// <copyright file="MonsterDropMultiplier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// A drop chance multiplier for a specific monster, e.g. a boss which takes much longer to kill.
/// </summary>
public class MonsterDropMultiplier
{
    /// <summary>
    /// Gets or sets the monster number.
    /// </summary>
    [Display(Name = "Monster number", Description = "The monster number as shown in the monster definition.")]
    public short MonsterNumber { get; set; }

    /// <summary>
    /// Gets or sets the multiplier.
    /// </summary>
    [Display(Name = "Multiplier")]
    public float Multiplier { get; set; } = 1f;

    /// <summary>
    /// Gets the multiplier of a monster; monsters which are not listed use 1.
    /// </summary>
    /// <param name="multipliers">The multipliers.</param>
    /// <param name="monsterNumber">The monster number.</param>
    /// <returns>The multiplier.</returns>
    public static float Get(IEnumerable<MonsterDropMultiplier>? multipliers, short monsterNumber)
    {
        foreach (var entry in multipliers ?? [])
        {
            if (entry.MonsterNumber == monsterNumber)
            {
                return entry.Multiplier;
            }
        }

        return 1f;
    }
}
