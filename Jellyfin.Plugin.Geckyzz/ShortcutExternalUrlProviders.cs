using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Geckyzz;

internal static class ProviderIdHelper
{
    public static bool TryGetTmdbId(BaseItem item, GeckyzzMapping? mapping, out string tmdbId)
    {
        if (mapping?.TmdbId is > 0)
        {
            tmdbId = mapping.TmdbId.Value.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        if (TryGetNumericId(item, "Tmdb", out tmdbId) || TryGetNumericId(item, "TheMovieDb", out tmdbId))
        {
            return true;
        }

        if (item is Season { Series: not null } season
            && (TryGetNumericId(season.Series, "Tmdb", out tmdbId) || TryGetNumericId(season.Series, "TheMovieDb", out tmdbId)))
        {
            return true;
        }

        tmdbId = string.Empty;
        return false;
    }

    public static bool TryGetImdbId(BaseItem item, GeckyzzMapping? mapping, out string imdbId)
    {
        if (!string.IsNullOrWhiteSpace(mapping?.ImdbId))
        {
            imdbId = NormalizeImdbId(mapping.ImdbId);
            return true;
        }

        if (item.ProviderIds.TryGetValue("Imdb", out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            imdbId = NormalizeImdbId(raw);
            return true;
        }

        if (item is Season { Series: not null } season
            && season.Series.ProviderIds.TryGetValue("Imdb", out raw)
            && !string.IsNullOrWhiteSpace(raw))
        {
            imdbId = NormalizeImdbId(raw);
            return true;
        }

        imdbId = string.Empty;
        return false;
    }

    public static string NormalizeImdbId(string raw)
    {
        var trimmed = raw.Trim();
        return trimmed.StartsWith("tt", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : "tt" + trimmed;
    }

    public static bool TryGetNumericId(BaseItem item, string key, out string id)
    {
        foreach (var (k, v) in item.ProviderIds)
        {
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase)
                && long.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var num)
                && num > 0)
            {
                id = num.ToString(CultureInfo.InvariantCulture);
                return true;
            }
        }

        id = string.Empty;
        return false;
    }
}

/// <summary>Provides WeTrakr links.</summary>
public sealed class WeTrakrExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="WeTrakrExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public WeTrakrExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "WeTrakr";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            var section = item is Movie ? "movies" : "shows";
            return new[] { $"https://wetrakr.com/tmdb/{section}/{tmdbId}" };
        }

        if (ProviderIdHelper.TryGetImdbId(item, mapping, out var imdbId))
        {
            return new[] { $"https://wetrakr.com/imdb/{imdbId}" };
        }

        return Array.Empty<string>();
    }
}

/// <summary>Provides publicmetadb links.</summary>
public sealed class PublicMetaDbExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="PublicMetaDbExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public PublicMetaDbExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "publicmetadb";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            var mediaType = item is Movie ? "movie" : "tv";
            return new[] { $"https://publicmetadb.com/{mediaType}/{tmdbId}" };
        }

        return Array.Empty<string>();
    }
}

/// <summary>Provides Bingeboxd links.</summary>
public sealed class BingeboxdExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="BingeboxdExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public BingeboxdExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "Bingeboxd";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            var section = item is Movie ? "movies" : "shows";
            return new[] { $"https://bingeboxd.com/{section}/{tmdbId}" };
        }

        return Array.Empty<string>();
    }
}

/// <summary>Provides Refract links.</summary>
public sealed class RefractExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="RefractExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public RefractExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "Refract";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            var mediaType = item is Movie ? "movie" : "tv";
            return new[] { $"https://getrefract.app/item/{mediaType}/{tmdbId}" };
        }

        return Array.Empty<string>();
    }
}

/// <summary>Provides Seenr links.</summary>
public sealed class SeenrExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="SeenrExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public SeenrExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "Seenr";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (!ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            return Array.Empty<string>();
        }

        if (item is Movie)
        {
            return new[] { $"https://seenr.app/en/app/movies/{tmdbId}" };
        }

        var url = $"https://seenr.app/en/app/shows/{tmdbId}";
        if (item is Season season && season.IndexNumber is >= 0)
        {
            url += $"/seasons/{season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return new[] { url };
    }
}

/// <summary>Provides cinematique links.</summary>
public sealed class CinematiqueExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="CinematiqueExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public CinematiqueExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "cinematique";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (!ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            return Array.Empty<string>();
        }

        if (item is Movie)
        {
            return new[] { $"https://cinematique.me/#/movie/{tmdbId}" };
        }

        var url = $"https://cinematique.me/#/tv/{tmdbId}";
        if (item is Season season && season.IndexNumber is >= 0)
        {
            url += $"/season/{season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return new[] { url };
    }
}

/// <summary>Provides Serializd links.</summary>
public sealed class SerializdExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="SerializdExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public SerializdExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "Serializd";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (!ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            return Array.Empty<string>();
        }

        if (item is Season season && season.IndexNumber is >= 0)
        {
            var seasonId = mapping?.TmdbSeasonId;
            if (seasonId is null && ProviderIdHelper.TryGetNumericId(season, "Tmdb", out var seasonTmdbIdStr)
                && long.TryParse(seasonTmdbIdStr, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedSeasonId))
            {
                seasonId = parsedSeasonId;
            }

            if (seasonId is > 0)
            {
                return new[]
                {
                    $"https://www.serializd.com/show/{tmdbId}/season/{seasonId.Value.ToString(CultureInfo.InvariantCulture)}/{season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)}"
                };
            }
        }

        return new[] { $"https://www.serializd.com/show/{tmdbId}" };
    }
}

/// <summary>Provides PunchPlay links.</summary>
public sealed class PunchPlayExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;

    /// <summary>Initializes a new instance of the <see cref="PunchPlayExternalUrlProvider"/> class.</summary>
    /// <param name="mappingService">The mapping service.</param>
    public PunchPlayExternalUrlProvider(GeckyzzMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    /// <inheritdoc />
    public string Name => "PunchPlay";

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is not (Movie or Series or Season))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (!ProviderIdHelper.TryGetTmdbId(item, mapping, out var tmdbId))
        {
            return Array.Empty<string>();
        }

        if (item is Movie)
        {
            return new[] { $"https://punchplay.tv/title/movie/{tmdbId}" };
        }

        var url = $"https://punchplay.tv/title/show/{tmdbId}";
        if (item is Season season && season.IndexNumber is >= 0)
        {
            url += $"/season/{season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return new[] { url };
    }
}
