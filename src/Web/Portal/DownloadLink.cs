// <copyright file="DownloadLink.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal;

/// <summary>
/// A download link of the game client.
/// </summary>
/// <param name="Title">The title.</param>
/// <param name="Url">The URL.</param>
/// <param name="Size">The human-readable size, e.g. "1.2 GB".</param>
public record DownloadLink(string Title, string Url, string? Size);
