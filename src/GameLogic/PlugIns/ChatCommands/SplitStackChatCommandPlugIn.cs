// <copyright file="SplitStackChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which splits a part of a stackable item (e.g. jewels) into a new stack.
/// The game client opens a small popup and sends this command when the player confirms it.
/// </summary>
[Guid("41EAD937-A452-46DE-879A-7BFD04066A15")]
[PlugIn]
[Display(Name = nameof(PlugInResources.SplitStackChatCommandPlugIn_Name), Description = nameof(PlugInResources.SplitStackChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class SplitStackChatCommandPlugIn : ChatCommandPlugInBase<SplitStackChatCommandPlugIn.Arguments>
{
    private const string Command = "/split";

    private const CharacterStatus MinimumStatus = CharacterStatus.Normal;

    private readonly SplitItemStackAction _action = new();

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        await this._action.SplitAsync(player, arguments.Slot, arguments.Amount, arguments.Group, arguments.Number).ConfigureAwait(false);
    }

    /// <summary>
    /// Arguments for this command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the inventory slot of the stack.
        /// </summary>
        public byte Slot { get; set; }

        /// <summary>
        /// Gets or sets the amount of pieces which should be split off.
        /// </summary>
        public byte Amount { get; set; }

        /// <summary>
        /// Gets or sets the item group the client expects in the slot.
        /// </summary>
        public byte Group { get; set; }

        /// <summary>
        /// Gets or sets the item number the client expects in the slot.
        /// </summary>
        public short Number { get; set; }
    }
}
