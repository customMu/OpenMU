// <copyright file="TestClasses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// Maps character class numbers to the base classes of the GM test commands.
/// </summary>
public static class TestClasses
{
    /// <summary>
    /// Gets the base class of a character class number.
    /// </summary>
    /// <param name="characterClassNumber">The number of the character class.</param>
    /// <returns>The base class, or <see langword="null"/> if it's unknown.</returns>
    public static TestClass? Get(byte characterClassNumber)
    {
        return characterClassNumber switch
        {
            0 or 2 or 3 => TestClass.DarkWizard,
            4 or 6 or 7 => TestClass.DarkKnight,
            8 or 10 or 11 => TestClass.Elf,
            12 or 13 => TestClass.MagicGladiator,
            16 or 17 => TestClass.DarkLord,
            20 or 22 or 23 => TestClass.Summoner,
            24 or 25 => TestClass.RageFighter,
            _ => null,
        };
    }
}
