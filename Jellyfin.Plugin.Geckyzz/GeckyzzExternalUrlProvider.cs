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
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (string.IsNullOrWhiteSpace(mapping?.GeckyzzPath))
        {
            return Array.Empty<string>();
        }

        var path = mapping.GeckyzzPath;
        if (item is Series)
        {
            path = StripTrailingSeason(path);
        }

        var pageType = HasFansubRelease(item) ? "fsanime" : "anime";
        return new[] { $"{BaseUrl}/{pageType}/{path}" };
    }

    private static string StripTrailingSeason(string path)
    {
        const string seasonSegment = "/season/";
        var idx = path.LastIndexOf(seasonSegment, StringComparison.Ordinal);
        return idx < 0 ? path : path[..idx];
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
