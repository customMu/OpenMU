// <copyright file="TestBuildChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which redistributes the stat points of a character like a build of the balance model.
/// </summary>
[Guid("8B3E5D17-2A6C-4F91-9E4D-6C1F8A2B7D53")]
[PlugIn]
[Display(Name = nameof(PlugInResources.TestBuildChatCommandPlugIn_Name), Description = nameof(PlugInResources.TestBuildChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class TestBuildChatCommandPlugIn : ChatCommandPlugInBase<TestBuildChatCommandPlugIn.Arguments>, IDisabledByDefault
{
    private const string Command = "/testbuild";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <summary>
    /// The stats in the order of the shares of a build.
    /// </summary>
    private static readonly AttributeDefinition[] BuildStats = [Stats.BaseStrength, Stats.BaseAgility, Stats.BaseVitality, Stats.BaseEnergy, Stats.BaseLeadership];

    /// <summary>
    /// The builds by base class: shares of the free points for strength, agility, vitality, energy and command.
    /// </summary>
    private static readonly Dictionary<TestClass, Dictionary<string, double[]>> Builds = new()
    {
        [TestClass.DarkKnight] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tank"] = [.40, .15, .40, .05, 0],
            ["dd"] = [.50, .30, .15, .05, 0],
        },
        [TestClass.DarkWizard] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dd"] = [.05, .15, .20, .60, 0],
            ["supp"] = [.05, .15, .40, .40, 0],
        },
        [TestClass.Elf] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["agi"] = [.15, .55, .20, .10, 0],
            ["supp"] = [.10, .20, .20, .50, 0],
        },
        [TestClass.MagicGladiator] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["str"] = [.45, .25, .20, .10, 0],
            ["ene"] = [.10, .25, .20, .45, 0],
        },
        [TestClass.DarkLord] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["str"] = [.40, .20, .20, .05, .15],
            ["cmd"] = [.20, .15, .20, .05, .40],
        },
        [TestClass.Summoner] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dd"] = [.05, .15, .25, .55, 0],
        },
        [TestClass.RageFighter] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vit"] = [.35, .20, .40, .05, 0],
        },
    };

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        var targetPlayer = player;
        if (arguments.CharacterName is { } characterName)
        {
            targetPlayer = player.GameContext.GetPlayerByCharacterName(characterName);
            if (targetPlayer?.SelectedCharacter is null ||
                !targetPlayer.SelectedCharacter.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase))
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.CharacterNotFound), characterName).ConfigureAwait(false);
                return;
            }
        }

        if (targetPlayer.SelectedCharacter is not { CharacterClass: { } characterClass } character
            || targetPlayer.Attributes is not { } attributes)
        {
            return;
        }

        var testClass = TestClasses.Get(characterClass.Number);
        if (testClass is null || !Builds.TryGetValue(testClass.Value, out var classBuilds))
        {
            await player.ShowBlueMessageAsync("No builds for this class.").ConfigureAwait(false);
            return;
        }

        if (arguments.Build is null || !classBuilds.TryGetValue(arguments.Build, out var shares))
        {
            await player.ShowBlueMessageAsync($"Builds of {testClass}: {string.Join(", ", classBuilds.Keys)}").ConfigureAwait(false);
            return;
        }

        var statDefinitions = characterClass.StatAttributes
            .Where(s => s.IncreasableByPlayer && s.Attribute is not null)
            .ToDictionary(s => s.Attribute!, s => (int)s.BaseValue);

        // The points a character really has with its reset count and level (not the points it has now, e.g. after /setresets),
        // so that a test character matches the balance model.
        var freePoints = StatPointsCalculator.GetPointsOfResetsAndLevel(targetPlayer);

        var newValues = new long[BuildStats.Length];
        long distributed = 0;
        for (var i = 0; i < BuildStats.Length; i++)
        {
            if (!statDefinitions.ContainsKey(BuildStats[i]))
            {
                continue;
            }

            newValues[i] = (long)Math.Floor(shares[i] * freePoints);
            distributed += newValues[i];
        }

        newValues[0] += freePoints - distributed;

        for (var i = 0; i < BuildStats.Length; i++)
        {
            if (statDefinitions.TryGetValue(BuildStats[i], out var baseValue))
            {
                attributes[BuildStats[i]] = checked((int)(baseValue + newValues[i]));
            }
        }

        character.LevelUpPoints = 0;

        await targetPlayer.InvokeViewPlugInAsync<IUpdateCharacterBaseStatsPlugIn>(p => p.UpdateCharacterBaseStatsAsync()).ConfigureAwait(false);
        await targetPlayer.InvokeViewPlugInAsync<IUpdateCharacterStatsPlugIn>(p => p.UpdateCharacterStatsAsync()).ConfigureAwait(false);
        await targetPlayer.InvokeViewPlugInAsync<IUpdateLevelPlugIn>(p => p.UpdateLevelAsync()).ConfigureAwait(false);

        await player.ShowBlueMessageAsync(
                $"{character.Name} {testClass} {arguments.Build}: {freePoints} points -> STR {(int)attributes[Stats.BaseStrength]}, AGI {(int)attributes[Stats.BaseAgility]}, VIT {(int)attributes[Stats.BaseVitality]}, ENE {(int)attributes[Stats.BaseEnergy]}, CMD {(int)attributes[Stats.BaseLeadership]}. Relog if the client shows old values.")
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for the test build chat command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the build name, e.g. "tank" or "dd".
        /// </summary>
        public string? Build { get; set; }

        /// <summary>
        /// Gets or sets the character name.
        /// </summary>
        public string? CharacterName { get; set; }
    }
}
