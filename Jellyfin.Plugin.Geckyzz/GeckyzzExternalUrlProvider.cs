using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// Provides geckyzz links for supported library items.
/// </summary>
public sealed class GeckyzzExternalUrlProvider : IExternalUrlProvider
{
    private const string TmdbProviderId = "Tmdb";
    private const string MyAnimeListProviderId = "MyAnimeList";
    private const string AlternateMyAnimeListProviderId = "MAL";
    private const string BaseUrl = "https://geckyzz.my.id";
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".m4v", ".avi", ".mov", ".wmv", ".mpg", ".mpeg", ".webm", ".ts"
    };

    private readonly ILogger<GeckyzzExternalUrlProvider> _logger;
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeckyzzExternalUrlProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mappingService">The mapping service.</param>
    public GeckyzzExternalUrlProvider(
        ILogger<GeckyzzExternalUrlProvider> logger,
        GeckyzzMappingService mappingService)
    {
        _logger = logger;
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "geckyzz";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
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
                break;
            case Season season:
                mediaType = "tv";
                idSource = (BaseItem?)season.Series ?? season;
                seasonNumber = season.IndexNumber;
                break;
            default:
                return Array.Empty<string>();
        }

        var hasItemId = TryGetId(idSource, out var itemId);
        if (!hasItemId && !ReferenceEquals(idSource, item))
        {
            hasItemId = TryGetId(item, out itemId);
        }

        if (!hasItemId)
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        var path = mapping?.GeckyzzPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = $"{mediaType}/{Uri.EscapeDataString(itemId)}";
            if (seasonNumber.HasValue && seasonNumber.Value >= 0)
            {
                path += $"/season/{seasonNumber.Value.ToString(CultureInfo.InvariantCulture)}";
            }
        }

        var pageType = HasFansubRelease(item) ? "fsanime" : "anime";
        return new[] { $"{BaseUrl}/{pageType}/{path}" };
    }

    private static bool TryGetId(BaseItem item, out string itemId)
    {
        if (TryGetNumericProviderId(item, TmdbProviderId, out itemId))
        {
            return true;
        }

        return TryGetNumericProviderId(item, MyAnimeListProviderId, out itemId)
            || TryGetNumericProviderId(item, AlternateMyAnimeListProviderId, out itemId);
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

    private bool HasFansubRelease(BaseItem item)
    {
        if (HasFansubMarker(item.Path))
        {
            return true;
        }

        if (!Directory.Exists(item.Path))
        {
            return false;
        }

        try
        {
            return Directory.EnumerateFiles(item.Path, "*", SearchOption.AllDirectories)
                .Any(path => VideoExtensions.Contains(Path.GetExtension(path)) && HasFansubMarker(path));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Unable to inspect video filenames for geckyzz item {ItemId}", item.Id);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unable to inspect video filenames for geckyzz item {ItemId}", item.Id);
            return false;
        }
    }

    private static bool HasFansubMarker(string? path)
    {
        return path?.Contains("CicakRumah", StringComparison.OrdinalIgnoreCase) == true
            || path?.Contains("TokekHutan", StringComparison.OrdinalIgnoreCase) == true;
    }
}
