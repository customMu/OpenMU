// <copyright file="PortalOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal;

/// <summary>
/// Options of the portal, bound to the <c>Portal</c> configuration section.
/// </summary>
public class PortalOptions
{
    /// <summary>
    /// The name of the configuration section.
    /// </summary>
    public const string SectionName = "Portal";

    /// <summary>
    /// Gets or sets the name of the server which is shown in the page title and header.
    /// </summary>
    public string ServerName { get; set; } = "OpenMU";

    /// <summary>
    /// Gets or sets a value indicating whether new accounts can be registered.
    /// </summary>
    public bool RegistrationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum password length.
    /// Older game clients only support 10 characters, newer ones support up to 20.
    /// </summary>
    public int MaximumPasswordLength { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of entries which are shown in a ranking.
    /// </summary>
    public int RankingSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether characters of bot accounts are shown in the rankings.
    /// </summary>
    public bool ShowBotsInRankings { get; set; }

    /// <summary>
    /// Gets or sets the duration for which ranking and status results are cached.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the base address of the admin panel, e.g. <c>http://localhost/</c>.
    /// It's used to query the server status. When it's empty, the status is not shown.
    /// </summary>
    public string? AdminPanelUrl { get; set; }

    /// <summary>
    /// Gets or sets the API key which is used to access the admin panel API.
    /// It can be created on the "API Keys" page of the admin panel.
    /// </summary>
    public string? AdminPanelApiKey { get; set; }

    /// <summary>
    /// Gets or sets the download links of the game client.
    /// </summary>
    public List<DownloadLink> Downloads { get; set; } = [];
}
