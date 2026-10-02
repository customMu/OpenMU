// <copyright file="ServerStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

/// <summary>
/// The status of the game server.
/// </summary>
/// <param name="IsOnline">A value indicating whether the server is reachable.</param>
/// <param name="PlayerCount">The number of players which are currently online.</param>
public record ServerStatus(bool IsOnline, int PlayerCount);
