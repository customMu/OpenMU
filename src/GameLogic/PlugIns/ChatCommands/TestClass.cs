// <copyright file="TestClass.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

/// <summary>
/// The base character classes, used by the GM test commands (<c>/testkit</c>, <c>/testbuild</c>).
/// </summary>
public enum TestClass
{
    /// <summary>
    /// Dark Wizard, Soul Master, Grand Master.
    /// </summary>
    DarkWizard,

    /// <summary>
    /// Dark Knight, Blade Knight, Blade Master.
    /// </summary>
    DarkKnight,

    /// <summary>
    /// Fairy Elf, Muse Elf, High Elf.
    /// </summary>
    Elf,

    /// <summary>
    /// Magic Gladiator, Duel Master.
    /// </summary>
    MagicGladiator,

    /// <summary>
    /// Dark Lord, Lord Emperor.
    /// </summary>
    DarkLord,

    /// <summary>
    /// Summoner, Bloody Summoner, Dimension Master.
    /// </summary>
    Summoner,

    /// <summary>
    /// Rage Fighter, Fist Master.
    /// </summary>
    RageFighter,
}
