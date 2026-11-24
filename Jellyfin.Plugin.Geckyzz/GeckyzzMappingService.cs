using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// Retrieves and caches external identifier mappings from the geckyzz API.
/// </summary>
public sealed partial class GeckyzzMappingService
{
    private static readonly string[] TmdbKeys = ["Tmdb", "TheMovieDb"];
    private static readonly string[] MalKeys = ["MyAnimeList", "MAL"];
    private static readonly string[] AniListKeys = ["AniList", "Al"];
    private static readonly string[] AniDbKeys = ["AniDB", "anidb.net", "Aid"];

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
        var candidatePaths = GetCandidateMappingPaths(item);
        if (candidatePaths.Count == 0)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        // Check if any candidate has a valid cached mapping
        foreach (var path in candidatePaths)
        {
            if (Cache.TryGetValue(path, out var cached) && cached.ExpiresAt > now && cached.Mapping is not null)
            {
                return cached.Mapping;
            }
        }

        // Try candidates in order
        foreach (var path in candidatePaths)
        {
            if (Cache.TryGetValue(path, out var cached) && cached.ExpiresAt > now && cached.Mapping is null)
            {
                continue;
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
                _logger.LogDebug("No geckyzz mapping exists for path {Path} (item {ItemId})", path, item.Id);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Unable to retrieve geckyzz mappings for path {Path} (item {ItemId})", path, item.Id);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Timed out retrieving geckyzz mappings for path {Path} (item {ItemId})", path, item.Id);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Unable to parse geckyzz mappings for path {Path} (item {ItemId})", path, item.Id);
            }

            Cache[path] = new CachedMapping(mapping, now + CacheDuration);

            if (mapping is not null)
            {
                return mapping;
            }
        }

        return null;
    }

    private static List<string> GetCandidateMappingPaths(BaseItem item)
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
                return [];
        }

        var paths = new List<string>(4);

        if (TryGetIdentifier(idSource, item, TmdbKeys, TmdbPathRegex(), out var tmdbId))
        {
            var tmdbPath = $"tmdb/{mediaType}/{tmdbId}";
            if (seasonNumber is >= 0)
            {
                tmdbPath += $"/seasons/{seasonNumber.Value.ToString(CultureInfo.InvariantCulture)}";
            }

            paths.Add(tmdbPath);
        }

        if (TryGetIdentifier(idSource, item, MalKeys, MalPathRegex(), out var malId))
        {
            paths.Add($"mal/{malId}");
        }

        if (TryGetIdentifier(idSource, item, AniListKeys, AniListPathRegex(), out var aniListId))
        {
            paths.Add($"anilist/{aniListId}");
        }

        if (TryGetIdentifier(idSource, item, AniDbKeys, AniDbPathRegex(), out var aniDbId))
        {
            paths.Add($"anidb/{aniDbId}");
        }

        return paths;
    }

    private static bool TryGetIdentifier(
        BaseItem idSource,
        BaseItem item,
        string[] providerKeys,
        Regex pathRegex,
        out string identifier)
    {
        if (TryGetNumericProviderId(idSource, providerKeys, out identifier))
        {
            return true;
        }

        if (!ReferenceEquals(idSource, item) && TryGetNumericProviderId(item, providerKeys, out identifier))
        {
            return true;
        }

        if (TryExtractIdFromPath(idSource.Path, pathRegex, out identifier))
        {
            return true;
        }

        if (!ReferenceEquals(idSource, item) && TryExtractIdFromPath(item.Path, pathRegex, out identifier))
        {
            return true;
        }

        identifier = string.Empty;
        return false;
    }

    private static bool TryGetNumericProviderId(BaseItem item, string[] providerKeys, out string itemId)
    {
        foreach (var (key, value) in item.ProviderIds)
        {
            foreach (var providerKey in providerKeys)
            {
                if (string.Equals(key, providerKey, StringComparison.OrdinalIgnoreCase)
                    && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var numericId)
                    && numericId > 0)
                {
                    itemId = numericId.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
            }
        }

        itemId = string.Empty;
        return false;
    }

    private static bool TryExtractIdFromPath(string? path, Regex regex, out string itemId)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var match = regex.Match(path);
            if (match.Success
                && long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var numericId)
                && numericId > 0)
            {
                itemId = numericId.ToString(CultureInfo.InvariantCulture);
                return true;
            }
        }

        itemId = string.Empty;
        return false;
    }

    [GeneratedRegex(@"\[(?:tmdb|tmdbid)-(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TmdbPathRegex();

    [GeneratedRegex(@"\[(?:mal|myanimelist)-(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MalPathRegex();

    [GeneratedRegex(@"\[(?:anilist|anilistid|al)-(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AniListPathRegex();

    [GeneratedRegex(@"\[(?:anidb|anidbid|aid)-(\d+)\]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AniDbPathRegex();

    private sealed record CachedMapping(GeckyzzMapping? Mapping, DateTimeOffset ExpiresAt);
}
