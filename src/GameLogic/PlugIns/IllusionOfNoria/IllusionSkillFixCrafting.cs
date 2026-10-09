// <copyright file="IllusionSkillFixCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// The Chaos Machine of the Illusion of Noria (the Chaos Goblin in the town of the illusion, 09.10.2026): the skill fix
/// option of a rank 7-8 weapon of +10 and more is added and removed here. Only on the map of the illusion.
/// </summary>
public abstract class IllusionSkillFixCrafting : BaseItemCraftingHandler
{
    /// <summary>
    /// The crafting number of adding the option (the client sends it, Mix ID of the recipe).
    /// </summary>
    public const byte AddNumber = 90;

    /// <summary>
    /// The crafting number of removing the option.
    /// </summary>
    public const byte RemoveNumber = 91;

    /// <summary>
    /// Gets the zen of the mix.
    /// </summary>
    protected virtual int Price => 0;

    /// <inheritdoc />
    public override CraftingResult? TryGetRequiredItems(Player player, out IList<CraftingRequiredItemLink> items, out byte successRateByItems)
    {
        items = new List<CraftingRequiredItemLink>();
        successRateByItems = 100;
        if (player.GameContext.FeaturePlugIns.GetPlugIn<IllusionOfNoriaPlugIn>() is not { } illusion
            || player.CurrentMap?.Definition.Number != illusion.GetConfiguration().MapNumber)
        {
            return CraftingResult.IncorrectMixItems;
        }

        var storage = player.TemporaryStorage?.Items.ToList() ?? [];
        var weapons = storage.Where(i => i.Definition is { Group: <= 5 } && !i.IsStackable()).ToList();
        if (weapons.Count != 1)
        {
            return CraftingResult.IncorrectMixItems;
        }

        var weapon = weapons[0];
        var rest = storage.Where(i => i != weapon).ToList();
        if (this.Check(player, illusion, weapon, rest, items) is { } error)
        {
            return error;
        }

        items.Insert(0, new CraftingRequiredItemLink([weapon], new ItemCraftingRequiredItem { SuccessResult = MixResult.StaysAsIs, FailResult = MixResult.StaysAsIs }));
        return null;
    }

    /// <summary>
    /// Checks the weapon and the other items of the mix and adds the consumed items to <paramref name="items"/>.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="illusion">The plugin of the illusion.</param>
    /// <param name="weapon">The weapon.</param>
    /// <param name="rest">The other items of the mix.</param>
    /// <param name="items">The consumed items.</param>
    /// <returns>The error, or <c>null</c>.</returns>
    protected abstract CraftingResult? Check(Player player, IllusionOfNoriaPlugIn illusion, Item weapon, IList<Item> rest, IList<CraftingRequiredItemLink> items);

    /// <summary>
    /// Takes exactly <paramref name="amount"/> pieces of the item from <paramref name="rest"/> (stacks or single items).
    /// </summary>
    /// <param name="rest">The other items of the mix; the taken ones are removed.</param>
    /// <param name="items">The consumed items.</param>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <param name="amount">The pieces.</param>
    /// <returns><c>true</c>, if exactly so many pieces are there.</returns>
    protected static bool TakeExactly(IList<Item> rest, IList<CraftingRequiredItemLink> items, byte group, short number, int amount)
    {
        var found = rest.Where(i => i.Definition is { } d && d.Group == group && d.Number == number).ToList();
        var count = found.Sum(i => i.IsStackable() ? (int)i.Durability : 1);
        if (count != amount)
        {
            return false;
        }

        if (found.Count > 0)
        {
            items.Add(new CraftingRequiredItemLink(found, new ItemCraftingRequiredItem { SuccessResult = MixResult.Disappear, FailResult = MixResult.Disappear }));
            found.ForEach(i => rest.Remove(i));
        }

        return true;
    }

    /// <summary>
    /// Gets the skill fix option of the weapon, or <c>null</c>.
    /// </summary>
    /// <param name="weapon">The weapon.</param>
    /// <returns>The option link.</returns>
    protected static ItemOptionLink? GetOption(Item weapon) => weapon.ItemOptions.FirstOrDefault(o => o.ItemOption?.OptionType == ItemOptionTypes.SkillFixOption);

    /// <inheritdoc />
    protected override int GetPrice(byte successRate, IList<CraftingRequiredItemLink> requiredItems) => this.Price;
}

/// <summary>
/// Adds the skill fix option: a rank 7-8 weapon of +10 and more without the option, an Echo (its skill, which the weapon
/// must have for the class of the player), 10 Jewels of Soul, Bless, Chaos, Creation and Life, 10 Illusion Shards
/// (configurable), 5 000 000 zen; 100 %.
/// </summary>
public class IllusionAddSkillFixCrafting : IllusionSkillFixCrafting
{
    private static readonly (byte Group, short Number)[] Jewels = [(14, 14), (14, 13), (12, 15), (14, 22), (14, 16)];

    /// <summary>
    /// The pieces of each jewel.
    /// </summary>
    public const int JewelAmount = 10;

    /// <summary>
    /// The zen of the mix.
    /// </summary>
    public const int Zen = 5_000_000;

    /// <inheritdoc />
    protected override int Price => Zen;

    /// <inheritdoc />
    protected override CraftingResult? Check(Player player, IllusionOfNoriaPlugIn illusion, Item weapon, IList<Item> rest, IList<CraftingRequiredItemLink> items)
    {
        var stones = rest.Where(i => i.Definition is { } d && illusion.GetConfiguration().GetEchoSkill(d.Group, d.Number) is not null).ToList();
        if (stones.Count != 1 || stones[0].Durability != 1)
        {
            return CraftingResult.IncorrectMixItems;
        }

        if (weapon.Level < 10 || GetOption(weapon) is not null
            || !weapon.Definition!.PossibleItemOptions.SelectMany(o => o.PossibleOptions).Any(o => o.OptionType == ItemOptionTypes.SkillFixOption))
        {
            return CraftingResult.InvalidItemLevel;
        }

        var stone = stones[0];
        if (illusion.GetEchoHandler(player, stone)?.GetOptionNumber(weapon) is null)
        {
            return CraftingResult.IncorrectMixItems; // the Echo of a skill which the weapon (for this class) doesn't have
        }

        rest.Remove(stone);
        items.Add(new CraftingRequiredItemLink([stone], new ItemCraftingRequiredItem { SuccessResult = MixResult.Disappear, FailResult = MixResult.Disappear }));
        foreach (var (group, number) in Jewels)
        {
            if (!TakeExactly(rest, items, group, number, JewelAmount))
            {
                return CraftingResult.LackingMixItems;
            }
        }

        var configuration = illusion.GetConfiguration();
        if (!TakeExactly(rest, items, configuration.ShardItemGroup, configuration.ShardItemNumber, configuration.AddOptionShards))
        {
            return CraftingResult.LackingMixItems;
        }

        return rest.Count == 0 ? null : CraftingResult.IncorrectMixItems;
    }

    /// <inheritdoc />
    protected override ValueTask<List<Item>> CreateOrModifyResultItemsAsync(IList<CraftingRequiredItemLink> requiredItems, Player player, byte socketSlot, byte successRate)
    {
        var weapon = requiredItems[0].Items.First();
        var stone = requiredItems[1].Items.First();
        player.GameContext.FeaturePlugIns.GetPlugIn<IllusionOfNoriaPlugIn>()?.GetEchoHandler(player, stone)?.ApplyTo(weapon, player.PersistenceContext);
        return ValueTask.FromResult(new List<Item> { weapon });
    }
}

/// <summary>
/// Removes the skill fix option of a weapon: rank 7 for 1 Jewel of Illusion + 4 Lesser + 1 Greater Mirage Stone + 10
/// Illusion Shards, rank 8 for 2 + 10 + 2 + 20 (configuration 'Reset price' of the plugin Illusion of Noria).
/// </summary>
public class IllusionRemoveSkillFixCrafting : IllusionSkillFixCrafting
{
    /// <inheritdoc />
    protected override CraftingResult? Check(Player player, IllusionOfNoriaPlugIn illusion, Item weapon, IList<Item> rest, IList<CraftingRequiredItemLink> items)
    {
        if (GetOption(weapon) is null)
        {
            return CraftingResult.IncorrectMixItems;
        }

        var (_, price) = illusion.GetConfiguration().GetResetPrice(weapon.Definition!.Group, weapon.Definition.Number);
        foreach (var item in price.GroupBy(p => (p.ItemGroup, p.ItemNumber)))
        {
            if (!TakeExactly(rest, items, item.Key.ItemGroup, item.Key.ItemNumber, item.Sum(p => p.MinimumAmount)))
            {
                return CraftingResult.LackingMixItems;
            }
        }

        return rest.Count == 0 ? null : CraftingResult.IncorrectMixItems;
    }

    /// <inheritdoc />
    protected override async ValueTask<List<Item>> CreateOrModifyResultItemsAsync(IList<CraftingRequiredItemLink> requiredItems, Player player, byte socketSlot, byte successRate)
    {
        var weapon = requiredItems[0].Items.First();
        if (GetOption(weapon) is { } link)
        {
            weapon.ItemOptions.Remove(link);
            await player.PersistenceContext.DeleteAsync(link).ConfigureAwait(false);
        }

        return [weapon];
    }
}

/// <summary>
/// Creates a random Echo (one of all 22) from a Jewel of Illusion and 20 Illusion Shards (configurable), 100 %.
/// Only on the map of the illusion.
/// </summary>
public class IllusionCreateEchoCrafting : BaseItemCraftingHandler
{
    /// <summary>
    /// The crafting number (the client sends it, Mix ID of the recipe).
    /// </summary>
    public const byte Number = 92;

    /// <inheritdoc />
    public override CraftingResult? TryGetRequiredItems(Player player, out IList<CraftingRequiredItemLink> items, out byte successRateByItems)
    {
        items = new List<CraftingRequiredItemLink>();
        successRateByItems = 100;
        if (player.GameContext.FeaturePlugIns.GetPlugIn<IllusionOfNoriaPlugIn>() is not { } illusion
            || player.CurrentMap?.Definition.Number != illusion.GetConfiguration().MapNumber)
        {
            return CraftingResult.IncorrectMixItems;
        }

        var configuration = illusion.GetConfiguration();
        var storage = player.TemporaryStorage?.Items.ToList() ?? [];
        (byte Group, short Number, int Amount)[] prices =
        [
            (14, IllusionOfNoriaPlugIn.IllusionJewelNumber, configuration.EchoJewels),
            (configuration.ShardItemGroup, configuration.ShardItemNumber, configuration.EchoShards),
        ];
        foreach (var (group, number, amount) in prices)
        {
            var found = storage.Where(i => i.Definition is { } d && d.Group == group && d.Number == number).ToList();
            if (found.Sum(i => i.IsStackable() ? (int)i.Durability : 1) != amount)
            {
                return CraftingResult.LackingMixItems;
            }

            if (found.Count > 0)
            {
                items.Add(new CraftingRequiredItemLink(found, new ItemCraftingRequiredItem { SuccessResult = MixResult.Disappear, FailResult = MixResult.Disappear }));
                found.ForEach(i => storage.Remove(i));
            }
        }

        return storage.Count == 0 ? null : CraftingResult.IncorrectMixItems;
    }

    /// <inheritdoc />
    protected override int GetPrice(byte successRate, IList<CraftingRequiredItemLink> requiredItems) => 0;

    /// <inheritdoc />
    protected override async ValueTask<List<Item>> CreateOrModifyResultItemsAsync(IList<CraftingRequiredItemLink> requiredItems, Player player, byte socketSlot, byte successRate)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<IllusionOfNoriaPlugIn>() is not { } illusion
            || illusion.CreateRandomEchoes(player.GameContext, 1).FirstOrDefault() is not { } echo)
        {
            return [];
        }

        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = echo.Definition;
        item.Durability = 1;
        await player.TemporaryStorage!.AddItemAsync(item).ConfigureAwait(false);
        return [item];
    }
}
