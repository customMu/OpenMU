// <copyright file="ServerStatusService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Portal.Services;

using System.Net.Http;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Queries the server status from the public API of the admin panel (<c>GET api/status</c>).
/// </summary>
public class ServerStatusService
{
    private const string CacheKey = nameof(ServerStatusService);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly PortalOptions _options;
    private readonly ILogger<ServerStatusService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerStatusService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The portal options.</param>
    /// <param name="logger">The logger.</param>
    public ServerStatusService(HttpClient httpClient, IMemoryCache cache, IOptions<PortalOptions> options, ILogger<ServerStatusService> logger)
    {
        this._httpClient = httpClient;
        this._cache = cache;
        this._options = options.Value;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the server status.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status, or <c>null</c> if no admin panel is configured.</returns>
    public async Task<ServerStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this._options.AdminPanelUrl))
        {
            return null;
        }

        return await this._cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = this._options.CacheDuration;
            return await this.QueryStatusAsync(cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the response of the status endpoint.
    /// The admin panel returns the JSON object serialized as JSON string, so both forms are accepted.
    /// </summary>
    /// <param name="json">The response body.</param>
    /// <returns>The parsed status.</returns>
    internal static ServerStatus Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.String)
        {
            return Parse(document.RootElement.GetString()!);
        }

        var players = document.RootElement.TryGetProperty("players", out var playersElement) && playersElement.TryGetInt32(out var count)
            ? count
            : 0;
        return new ServerStatus(true, players);
    }

    private async Task<ServerStatus> QueryStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(this._options.AdminPanelUrl!), "api/status"));
            if (!string.IsNullOrEmpty(this._options.AdminPanelApiKey))
            {
                request.Headers.Add("X-Api-Key", this._options.AdminPanelApiKey);
            }

            using var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return Parse(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            this._logger.LogWarning(ex, "Could not query the server status.");
            return new ServerStatus(false, 0);
        }
    }
}
