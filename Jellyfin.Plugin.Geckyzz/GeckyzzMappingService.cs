using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// Retrieves and caches external identifier mappings from the geckyzz API.
/// </summary>
public sealed class GeckyzzMappingService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly ConcurrentDictionary<string, CachedMapping> Cache = new(StringComparer.Ordinal);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private readonly ILogger<GeckyzzMappingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeckyzzMappingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GeckyzzMappingService(ILogger<GeckyzzMappingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the mapped identifiers for a supported item.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <returns>The mapping, if one is available.</returns>
    public GeckyzzMapping? GetMapping(BaseItem item)
    {
        if (!TryGetMappingPath(item, out var path))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (Cache.TryGetValue(path, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Mapping;
        }

        GeckyzzMapping? mapping = null;
        try
        {
            var json = HttpClient.GetStringAsync($"https://geckyzz.my.id/api/mappings/{path}")
                .GetAwaiter()
                .GetResult();
            mapping = JsonSerializer.Deserialize<GeckyzzMapping>(json);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("No geckyzz mapping exists for item {ItemId}", item.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Unable to retrieve geckyzz mappings for item {ItemId}", item.Id);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timed out retrieving geckyzz mappings for item {ItemId}", item.Id);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Unable to parse geckyzz mappings for item {ItemId}", item.Id);
        }

        Cache[path] = new CachedMapping(mapping, now + CacheDuration);
        return mapping;
    }

    private static bool TryGetMappingPath(BaseItem item, out string path)
    {
        string mediaType;
        BaseItem idSource;
        int? seasonNumber = null;

        switch (item)
        {
            case Movie:
                mediaType = "movie";
                idSource = item;
                break;
            case Series:
                mediaType = "tv";
                idSource = item;
                seasonNumber = 1;
                break;
            case Season season:
                mediaType = "tv";
                idSource = (BaseItem?)season.Series ?? season;
                seasonNumber = season.IndexNumber;
                break;
            default:
                path = string.Empty;
                return false;
        }

        var isTmdbId = TryGetNumericProviderId(idSource, "Tmdb", out var itemId);
        if (!isTmdbId && !ReferenceEquals(idSource, item))
        {
            isTmdbId = TryGetNumericProviderId(item, "Tmdb", out itemId);
        }

        if (!isTmdbId
            && !TryGetNumericProviderId(idSource, "MyAnimeList", out itemId)
            && !TryGetNumericProviderId(idSource, "MAL", out itemId)
            && (ReferenceEquals(idSource, item)
                || !TryGetNumericProviderId(item, "MyAnimeList", out itemId)
                    && !TryGetNumericProviderId(item, "MAL", out itemId)))
        {
            path = string.Empty;
            return false;
        }

        var source = isTmdbId ? $"tmdb/{mediaType}/{itemId}" : $"mal/{itemId}";
        if (isTmdbId && seasonNumber is >= 0)
        {
            source += $"/seasons/{seasonNumber.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        path = source;
        return true;
    }

    private static bool TryGetNumericProviderId(BaseItem item, string provider, out string itemId)
    {
        if (item.ProviderIds.TryGetValue(provider, out var value)
            && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var numericId)
            && numericId > 0)
        {
            itemId = numericId.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        itemId = string.Empty;
        return false;
    }

    private sealed record CachedMapping(GeckyzzMapping? Mapping, DateTimeOffset ExpiresAt);
}
