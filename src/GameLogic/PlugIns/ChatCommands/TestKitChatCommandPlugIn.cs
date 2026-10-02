// <copyright file="TestKitChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which puts a test kit of a rank (armor set, weapon and wings of the class) into the inventory of a character.
/// </summary>
[Guid("5C7A2E94-3B8D-4A16-8F2E-9D4B6C1A7E35")]
[PlugIn]
[Display(Name = nameof(PlugInResources.TestKitChatCommandPlugIn_Name), Description = nameof(PlugInResources.TestKitChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class TestKitChatCommandPlugIn : ChatCommandPlugInBase<TestKitChatCommandPlugIn.Arguments>, IDisabledByDefault
{
    private const string Command = "/testkit";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;
    private const int MaximumRank = 8;
    private const int MaximumEnchant = 15;
    private const byte HelmGroup = 7;
    private const byte GlovesGroup = 10;
    private const byte LastArmorGroup = 11;
    private const byte BowGroup = 4;
    private const byte LastWeaponGroup = 5;
    private const short ArrowsNumber = 15;

    /// <summary>
    /// The upper drop level bounds (exclusive) of the weapon ranks 1 to 7, the rest is rank 8.
    /// </summary>
    private static readonly int[] WeaponRankDropLevelCuts = [16, 30, 47, 66, 83, 100, 128];

    /// <summary>
    /// Weapons which don't follow the drop level cuts.
    /// </summary>
    private static readonly Dictionary<(byte Group, short Number), int> WeaponRankOverrides = new()
    {
        [(0, 32)] = 5, // Sacred Glove
        [(0, 33)] = 6, // Storm Hard Glove
        [(6, 18)] = 6, // Grand Soul Shield
    };

    /// <summary>
    /// The armor set numbers (groups 7 to 11) by class and rank (index 0 = rank 1).
    /// </summary>
    private static readonly Dictionary<TestClass, short[]> ArmorSetsByClass = new()
    {
        [TestClass.DarkWizard] = [2, 4, 7, 3, 35, 18, 22, 30],
        [TestClass.DarkKnight] = [5, 6, 8, 9, 34, 16, 21, 29],
        [TestClass.Elf] = [10, 11, 12, 13, 36, 19, 24, 31],
        [TestClass.MagicGladiator] = [2, 4, 7, 3, 1, 15, 23, 32],
        [TestClass.DarkLord] = [5, 6, 6, 25, 26, 27, 28, 33],
        [TestClass.Summoner] = [39, 39, 39, 40, 41, 42, 44, 43],
        [TestClass.RageFighter] = [5, 6, 8, 9, 59, 60, 61, 73],
    };

    /// <summary>
    /// The wing candidates by class and wing tier (index 0 = first wings). The first wearable candidate of a tier is taken.
    /// </summary>
    private static readonly Dictionary<TestClass, (byte Group, short Number)[][]> WingsByClass = new()
    {
        [TestClass.DarkWizard] = [[(12, 1)], [(12, 4)], [(12, 37)]],
        [TestClass.DarkKnight] = [[(12, 2)], [(12, 5)], [(12, 36)]],
        [TestClass.Elf] = [[(12, 0)], [(12, 3)], [(12, 38)]],
        [TestClass.MagicGladiator] = [[(12, 2), (12, 1)], [(12, 6)], [(12, 39)]],
        [TestClass.DarkLord] = [[(13, 30)], [(13, 30)], [(12, 40)]],
        [TestClass.Summoner] = [[(12, 41)], [(12, 42)], [(12, 43)]],
        [TestClass.RageFighter] = [[(12, 49)], [(12, 49)], [(12, 50)]],
    };

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        if (arguments.Rank is < 1 or > MaximumRank || arguments.Enchant is < 0 or > MaximumEnchant)
        {
            await player.ShowBlueMessageAsync("Usage: /testkit <rank 1-8> <enchant 0-15> [character]").ConfigureAwait(false);
            return;
        }

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
            || targetPlayer.Inventory is not { } inventory)
        {
            return;
        }

        if (TestClasses.Get(characterClass.Number) is not { } testClass)
        {
            await player.ShowBlueMessageAsync("No test kit for this class.").ConfigureAwait(false);
            return;
        }

        var items = targetPlayer.GameContext.Configuration.Items;
        var notWearable = new List<string>();
        var kit = new List<ItemDefinition>();

        if (GetWings(items, testClass, characterClass, arguments.Rank) is { } wings)
        {
            kit.Add(wings);
        }

        var weapon = GetWeapon(items, testClass, characterClass, arguments.Rank);
        if (weapon is not null)
        {
            kit.Add(weapon);
        }

        var setNumber = ArmorSetsByClass[testClass][arguments.Rank - 1];
        for (var group = HelmGroup; group <= LastArmorGroup; group++)
        {
            if ((group == HelmGroup && testClass == TestClass.MagicGladiator)
                || (group == GlovesGroup && testClass == TestClass.RageFighter))
            {
                continue;
            }

            var piece = items.FirstOrDefault(d => d.Group == group && d.Number == setNumber);
            if (piece is null)
            {
                continue;
            }

            if (!piece.QualifiedCharacters.Contains(characterClass))
            {
                notWearable.Add(piece.Name.ValueInNeutralLanguage);
                continue;
            }

            kit.Add(piece);
        }

        var placed = new List<string>();
        var notPlaced = 0;
        foreach (var definition in kit)
        {
            if (await TryAddAsync(targetPlayer, inventory, definition, (byte)arguments.Enchant).ConfigureAwait(false))
            {
                placed.Add(definition.Name.ValueInNeutralLanguage);
            }
            else
            {
                notPlaced++;
            }
        }

        if (weapon?.Group == BowGroup
            && items.FirstOrDefault(d => d.Group == BowGroup && d.Number == ArrowsNumber) is { } arrows)
        {
            if (await TryAddAsync(targetPlayer, inventory, arrows, 0).ConfigureAwait(false))
            {
                placed.Add(arrows.Name.ValueInNeutralLanguage);
            }
            else
            {
                notPlaced++;
            }
        }

        await player.ShowBlueMessageAsync($"Test kit rank {arguments.Rank} +{arguments.Enchant} for {character.Name}: {placed.Count} items, {notPlaced} not placed (no space).").ConfigureAwait(false);
        await player.ShowBlueMessageAsync(string.Join(", ", placed)).ConfigureAwait(false);
        if (weapon is null)
        {
            await player.ShowBlueMessageAsync("No weapon found for this rank.").ConfigureAwait(false);
        }

        if (notWearable.Count > 0)
        {
            await player.ShowBlueMessageAsync($"Not wearable, skipped: {string.Join(", ", notWearable)}").ConfigureAwait(false);
        }
    }

    private static async ValueTask<bool> TryAddAsync(Player targetPlayer, IStorage inventory, ItemDefinition definition, byte enchant)
    {
        var item = targetPlayer.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        if (definition.IsAmmunition)
        {
            item.Durability = definition.Durability;
        }
        else
        {
            item.Level = Math.Min(enchant, definition.MaximumItemLevel);
            item.HasSkill = definition.Group <= LastWeaponGroup && definition.Skill is not null;
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
        }

        if (await inventory.AddItemAsync(item).ConfigureAwait(false))
        {
            await targetPlayer.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
            return true;
        }

        await targetPlayer.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
        return false;
    }

    private static int GetWeaponRank(ItemDefinition definition)
    {
        if (WeaponRankOverrides.TryGetValue((definition.Group, definition.Number), out var rank))
        {
            return rank;
        }

        return 1 + WeaponRankDropLevelCuts.Count(cut => definition.DropLevel >= cut);
    }

    private static float GetPowerUp(ItemDefinition definition, AttributeDefinition attribute)
    {
        return definition.BasePowerUpAttributes.FirstOrDefault(p => p.TargetAttribute == attribute)?.BaseValue ?? 0;
    }

    private static float GetAveragePhysicalDamage(ItemDefinition definition)
    {
        return (GetPowerUp(definition, Stats.MinimumPhysBaseDmgByWeapon) + GetPowerUp(definition, Stats.MaximumPhysBaseDmgByWeapon)) / 2;
    }

    private static bool IsMainHandWeapon(ItemDefinition definition)
    {
        return definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.LeftHandSlot) ?? false;
    }

    /// <summary>
    /// Determines whether the weapon is of the main type of the class.
    /// </summary>
    /// <param name="definition">The weapon definition.</param>
    /// <param name="testClass">The class.</param>
    /// <param name="strict">If <c>true</c>, the preferred subtype is required (two-handed for DK, scepter for DL, stick for SUM).</param>
    private static bool IsClassWeapon(ItemDefinition definition, TestClass testClass, bool strict)
    {
        return testClass switch
        {
            TestClass.DarkWizard => definition.Group == 5 && IsMainHandWeapon(definition),
            TestClass.Summoner => definition.Group == 5 && IsMainHandWeapon(definition)
                                  && (!strict || definition.QualifiedCharacters.All(c => c.Number != 0)),
            TestClass.DarkKnight => definition.Group <= 3 && IsMainHandWeapon(definition)
                                    && (!strict || GetPowerUp(definition, Stats.IsTwoHandedWeaponEquipped) > 0),
            TestClass.Elf => definition.Group == BowGroup && GetPowerUp(definition, Stats.IsBowEquipped) > 0,
            TestClass.MagicGladiator => definition.Group == 0 && IsMainHandWeapon(definition),
            TestClass.DarkLord => definition.Group == 2 && IsMainHandWeapon(definition)
                                  && (!strict || GetPowerUp(definition, Stats.ScepterRise) > 0),
            TestClass.RageFighter => definition.Group == 0 && IsMainHandWeapon(definition),
            _ => false,
        };
    }

    private static ItemDefinition? GetWeapon(IEnumerable<ItemDefinition> items, TestClass testClass, CharacterClass characterClass, int rank)
    {
        var weaponsByRank = items
            .Where(d => d.Group <= LastWeaponGroup && d.DropsFromMonsters && !d.IsAmmunition && d.QualifiedCharacters.Contains(characterClass))
            .Where(d => IsClassWeapon(d, testClass, false))
            .GroupBy(GetWeaponRank)
            .ToDictionary(g => g.Key, g => g.ToList());

        // The nearest lower rank first, then the nearest higher rank (e.g. for the gloves of the rage fighter).
        var rankOrder = Enumerable.Range(1, rank).Reverse().Concat(Enumerable.Range(rank + 1, MaximumRank - rank));
        foreach (var strict in new[] { true, false })
        {
            foreach (var currentRank in rankOrder)
            {
                if (!weaponsByRank.TryGetValue(currentRank, out var weapons))
                {
                    continue;
                }

                var candidates = weapons.Where(d => !strict || IsClassWeapon(d, testClass, true)).ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }

                return testClass == TestClass.DarkKnight
                    ? candidates.OrderByDescending(GetAveragePhysicalDamage).ThenByDescending(d => d.DropLevel).First()
                    : candidates.OrderByDescending(d => d.DropLevel).ThenByDescending(GetAveragePhysicalDamage).First();
            }
        }

        return null;
    }

    private static ItemDefinition? GetWings(IEnumerable<ItemDefinition> items, TestClass testClass, CharacterClass characterClass, int rank)
    {
        // rank 1-2: none, 3-4: first wings, 5-6: second wings, 7-8: third wings
        var tier = (rank - 1) / 2;
        var tiers = WingsByClass[testClass];
        for (var currentTier = Math.Min(tier, tiers.Length); currentTier >= 1; currentTier--)
        {
            foreach (var (group, number) in tiers[currentTier - 1])
            {
                if (items.FirstOrDefault(d => d.Group == group && d.Number == number) is { } wings
                    && wings.QualifiedCharacters.Contains(characterClass))
                {
                    return wings;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Arguments for the test kit chat command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the rank (1 to 8).
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Gets or sets the enchant level of the items (0 to 15).
        /// </summary>
        public int Enchant { get; set; }

        /// <summary>
        /// Gets or sets the character name.
        /// </summary>
        public string? CharacterName { get; set; }
    }
}
