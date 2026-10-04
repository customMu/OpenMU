// <copyright file="KundunChamberJewelryLevel.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

/// <summary>
/// The weights of the number of excellent options of the ring and the pendant of Kundun of a chamber level.
/// </summary>
public class KundunChamberJewelryLevel
{
    /// <summary>
    /// Gets or sets the level of the chamber (1-7).
    /// </summary>
    [Display(Name = "Level")]
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the weight of one excellent option.
    /// </summary>
    [Display(Name = "1 option")]
    public int OneOption { get; set; } = 100;

    /// <summary>
    /// Gets or sets the weight of two excellent options.
    /// </summary>
    [Display(Name = "2 options")]
    public int TwoOptions { get; set; }

    /// <summary>
    /// Gets or sets the weight of three excellent options.
    /// </summary>
    [Display(Name = "3 options")]
    public int ThreeOptions { get; set; }

    /// <summary>
    /// Gets the weights of 1, 2 and 3 options.
    /// </summary>
    /// <returns>The weights.</returns>
    public IList<int> GetWeights() => [this.OneOption, this.TwoOptions, this.ThreeOptions];

    /// <inheritdoc />
    public override string ToString() => $"Chamber {this.Level}: {this.OneOption} / {this.TwoOptions} / {this.ThreeOptions}";
}
