// <copyright file="ItemConsumeAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.ComponentModel;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Action to consume an item.
/// </summary>
public class ItemConsumeAction
{
    private readonly IItemConsumeHandlerPlugIn _magicEffectHandler = new ApplyMagicEffectConsumeHandlerPlugIn();

    /// <summary>
    /// Handles the consume request.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="inventorySlot">The inventory slot.</param>
    /// <param name="inventoryTargetSlot">The inventory target slot.</param>
    /// <param name="fruitUsage">The fruit usage.</param>
    public async ValueTask HandleConsumeRequestAsync(Player player, byte inventorySlot, byte inventoryTargetSlot, FruitUsage fruitUsage)
    {
        var item = player.Inventory?.GetItem(inventorySlot);
        if (item?.Definition is null)
        {
            player.Logger.LogInformation("[MultiShotDebug][Dispatch] item or item.Definition is NULL at slot " + inventorySlot);
            await player.InvokeViewPlugInAsync<IRequestedItemConsumptionFailedPlugIn>(p => p.RequestedItemConsumptionFailedAsync()).ConfigureAwait(false);
            return;
        }

        player.Logger.LogInformation($"[MultiShotDebug][Dispatch] item={item.Definition.Name}, Number={item.Definition.Number}, Group={item.Definition.Group}, Skill={(item.Definition.Skill == null ? "NULL" : item.Definition.Skill.Name)}, IsWearable={item.IsWearable()}, ConsumeEffect={(item.Definition.ConsumeEffect == null ? "NULL" : "SET")}");

        var consumeHandler = player.GameContext.PlugInManager.GetStrategy<ItemIdentifier, IItemConsumeHandlerPlugIn>(new ItemIdentifier(item.Definition.Number, item.Definition.Group))
                      ?? player.GameContext.PlugInManager.GetStrategy<ItemIdentifier, IItemConsumeHandlerPlugIn>(new ItemIdentifier(null, item.Definition.Group));

        player.Logger.LogInformation($"[MultiShotDebug][Dispatch] after exact/group match: consumeHandler={(consumeHandler == null ? "NULL" : consumeHandler.GetType().Name)}");

        if (consumeHandler is null && item.Definition.Skill is { } && !item.IsWearable())
        {
            consumeHandler = player.GameContext.PlugInManager.GetStrategy<ItemIdentifier, IItemConsumeHandlerPlugIn>(ItemConstants.AllScrolls);
            player.Logger.LogInformation($"[MultiShotDebug][Dispatch] after AllScrolls fallback: consumeHandler={(consumeHandler == null ? "NULL" : consumeHandler.GetType().Name)}");
        }

        if (consumeHandler is null && item.Definition.ConsumeEffect is { })
        {
            consumeHandler = this._magicEffectHandler;
            player.Logger.LogInformation("[MultiShotDebug][Dispatch] fell back to magicEffectHandler");
        }

        if (consumeHandler is null)
        {
            player.Logger.LogInformation("[MultiShotDebug][Dispatch] NO HANDLER FOUND AT ALL - sending UsingThisItemNotImplemented");
            await player.InvokeViewPlugInAsync<IRequestedItemConsumptionFailedPlugIn>(p => p.RequestedItemConsumptionFailedAsync()).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.UsingThisItemNotImplemented)).ConfigureAwait(false);
            return;
        }

        var targetItem = player.Inventory!.GetItem(inventoryTargetSlot);

        if (player.GameContext.PlugInManager.GetPlugInPoint<IItemConsumingPlugIn>() is { } plugInPoint)
        {
            var eventArgs = new CancelEventArgs();
            plugInPoint.ItemConsuming(player, item, targetItem, eventArgs);
            if (eventArgs.Cancel)
            {
                player.Logger.LogInformation("[MultiShotDebug][Dispatch] cancelled by IItemConsumingPlugIn");
                return;
            }
        }

        player.Logger.LogInformation($"[MultiShotDebug][Dispatch] calling {consumeHandler.GetType().Name}.ConsumeItemAsync");
        if (!await consumeHandler.ConsumeItemAsync(player, item, targetItem, fruitUsage).ConfigureAwait(false))
        {
            player.Logger.LogInformation("[MultiShotDebug][Dispatch] ConsumeItemAsync returned false");
            await player.InvokeViewPlugInAsync<IRequestedItemConsumptionFailedPlugIn>(p => p.RequestedItemConsumptionFailedAsync()).ConfigureAwait(false);
            return;
        }

        if (item.Durability == 0)
        {
            await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
        }
        else
        {
            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(item, true)).ConfigureAwait(false);
        }

        player.GameContext.PlugInManager.GetPlugInPoint<IItemConsumedPlugIn>()?.ItemConsumed(player, item, targetItem);
    }
}