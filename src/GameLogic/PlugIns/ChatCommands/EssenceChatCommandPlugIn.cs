// <copyright file="EssenceChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.KundunEssence;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands.Arguments;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command which shows the Kundun Essence of the character.
/// </summary>
[Guid("2D7A4F93-6C1B-4E58-8A3F-9B0E5C7D1A24")]
[PlugIn]
[Display(Name = nameof(PlugInResources.EssenceChatCommandPlugIn_Name), Description = nameof(PlugInResources.EssenceChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(EmptyChatCommandArgs), CharacterStatus.Normal)]
public class EssenceChatCommandPlugIn : ChatCommandPlugInBase<EmptyChatCommandArgs>
{
    private const string Command = "/essence";

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, EmptyChatCommandArgs arguments)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<KundunEssencePlugIn>() is not { } essence)
        {
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunEssenceBalanceFormat), essence.GetBalance(player)).ConfigureAwait(false);
    }
}
