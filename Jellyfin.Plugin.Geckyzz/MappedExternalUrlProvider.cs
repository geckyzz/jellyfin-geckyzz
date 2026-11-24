using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// Provides a direct external link for a mapped platform.
/// </summary>
public abstract class MappedExternalUrlProvider : IExternalUrlProvider
{
    private readonly GeckyzzMappingService _mappingService;
    private readonly string _target;
    private readonly Func<GeckyzzMapping, bool> _hasTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappedExternalUrlProvider"/> class.
    /// </summary>
    /// <param name="mappingService">The mapping service.</param>
    /// <param name="target">The mapped platform.</param>
    /// <param name="name">The external link's display name.</param>
    /// <param name="hasTarget">Checks whether the mapping contains the target.</param>
    protected MappedExternalUrlProvider(
        GeckyzzMappingService mappingService,
        string target,
        string name,
        Func<GeckyzzMapping, bool> hasTarget)
    {
        _mappingService = mappingService;
        _target = target;
        Name = name;
        _hasTarget = hasTarget;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (!IsAllowedForItem(item))
        {
            return Array.Empty<string>();
        }

        var mapping = _mappingService.GetMapping(item);
        if (mapping is null
            || !_hasTarget(mapping)
            || HasNativeExternalLink(item))
        {
            return Array.Empty<string>();
        }

        var url = BuildUrl(item, mapping);
        return string.IsNullOrEmpty(url) ? Array.Empty<string>() : new[] { url };
    }

    private bool IsAllowedForItem(BaseItem item)
    {
        if (item is Movie or Season)
        {
            return true;
        }

        if (item is not Series)
        {
            return false;
        }

        return _target is not "letterboxd";
    }

    private bool HasNativeExternalLink(BaseItem item)
    {
        var providerId = _target switch
        {
            "imdb" => "Imdb",
            "themoviedb" => "Tmdb",
            "thetvdb" => "Tvdb",
            "trakt" => "Trakt",
            _ => null
        };

        if (providerId is null)
        {
            return false;
        }

        if (item.ProviderIds.ContainsKey(providerId))
        {
            return true;
        }

        return item is Season { Series: not null } season
            && season.Series.ProviderIds.ContainsKey(providerId);
    }

    private string? BuildUrl(BaseItem item, GeckyzzMapping mapping)
    {
        return _target switch
        {
            "anidb" => Append("https://anidb.net/anime/", mapping.AniDbId),
            "anilist" => Append("https://anilist.co/anime/", mapping.AniListId),
            "animenewsnetwork" => Append("https://www.animenewsnetwork.com/encyclopedia/anime.php?id=", mapping.AnimeNewsNetworkId),
            "animeplanet" => Append("https://www.anime-planet.com/anime/", mapping.AnimePlanetSlug),
            "anisearch" => Append("https://www.anisearch.com/anime/", mapping.AniSearchId),
            "annict" => Append("https://annict.com/works/", mapping.AnnictId),
            "hikka" => Append("https://hikka.io/anime/", mapping.HikkaSlug),
            "imdb" => Append("https://www.imdb.com/title/", mapping.ImdbId),
            "kaize" => Append("https://kaize.io/anime/", mapping.KaizeSlug),
            "kitsu" => Append("https://kitsu.app/anime/", mapping.KitsuId),
            "letterboxd" => Append("https://letterboxd.com/film/", mapping.LetterboxdSlug),
            "livechart" => Append("https://www.livechart.me/anime/", mapping.LiveChartId),
            "myanimelist" => Append("https://myanimelist.net/anime/", mapping.MyAnimeListId),
            "nautiljon" => Append("https://www.nautiljon.com/animes/", mapping.NautiljonSlug),
            "otakotaku" => Append("https://otakotaku.com/anime/view/", mapping.OtakotakuId),
            "shikimori" => Append("https://shikimori.io/animes/", mapping.ShikimoriId),
            "shoboi" => Append("https://cal.syoboi.jp/tid/", mapping.ShoboiId),
            "simkl" => Append("https://simkl.com/anime/", mapping.SimklId),
            "themoviedb" => BuildTmdbUrl(item, mapping),
            "thetvdb" => BuildTvdbUrl(item, mapping),
            "trakt" => BuildTraktUrl(item, mapping),
            _ => null
        };
    }

    private static string? BuildTmdbUrl(BaseItem item, GeckyzzMapping mapping)
    {
        if (mapping.TmdbId is not > 0)
        {
            return null;
        }

        var mediaType = item is Movie ? "movie" : "tv";
        var url = $"https://www.themoviedb.org/{mediaType}/{mapping.TmdbId.Value.ToString(CultureInfo.InvariantCulture)}";
        if (item is Season season && season.IndexNumber is >= 0)
        {
            url += $"/season/{season.IndexNumber.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return url;
    }

    private static string? BuildTvdbUrl(BaseItem item, GeckyzzMapping mapping)
    {
        if (item is Movie || mapping.TvdbId is not > 0)
        {
            return null;
        }

        var url = $"https://www.thetvdb.com/series/{mapping.TvdbId.Value.ToString(CultureInfo.InvariantCulture)}";
        if (item is Season)
        {
            var seasonId = mapping.TvdbSeasonId ?? mapping.TraktSeason;
            if (seasonId is > 0)
            {
                url += $"/seasons/{seasonId.Value.ToString(CultureInfo.InvariantCulture)}";
            }
        }

        return url;
    }

    private static string? BuildTraktUrl(BaseItem item, GeckyzzMapping mapping)
    {
        var slug = mapping.TraktSlug;
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var mediaType = item switch
        {
            Movie => "movies",
            Series or Season => "shows",
            _ => string.Empty
        };
        if (mediaType.Length == 0)
        {
            return null;
        }

        var url = $"https://og.trakt.tv/{mediaType}/{Uri.EscapeDataString(slug)}";
        if (item is Season && mapping.TraktSeason is > 0)
        {
            url += $"/seasons/{mapping.TraktSeason.Value.ToString(CultureInfo.InvariantCulture)}";
        }

        return url;
    }

    private static string? Append(string prefix, long? id)
    {
        return id is > 0 ? prefix + id.Value.ToString(CultureInfo.InvariantCulture) : null;
    }

    private static string? Append(string prefix, string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : prefix + Uri.EscapeDataString(id);
    }
}

/// <summary>Provides AniDB links.</summary>
public sealed class AniDbExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AniDbExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "anidb", "AniDB", mapping => mapping.AniDbId is > 0) { }
}

/// <summary>Provides AniList links.</summary>
public sealed class AniListExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AniListExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "anilist", "AniList", mapping => mapping.AniListId is > 0) { }
}

/// <summary>Provides Anime News Network links.</summary>
public sealed class AnimeNewsNetworkExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AnimeNewsNetworkExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "animenewsnetwork", "Anime News Network", mapping => mapping.AnimeNewsNetworkId is > 0) { }
}

/// <summary>Provides Anime-Planet links.</summary>
public sealed class AnimePlanetExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AnimePlanetExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "animeplanet", "Anime-Planet", mapping => !string.IsNullOrWhiteSpace(mapping.AnimePlanetSlug)) { }
}

/// <summary>Provides AniSearch links.</summary>
public sealed class AniSearchExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AniSearchExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "anisearch", "AniSearch", mapping => mapping.AniSearchId is > 0) { }
}

/// <summary>Provides Annict links.</summary>
public sealed class AnnictExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public AnnictExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "annict", "Annict", mapping => mapping.AnnictId is > 0) { }
}

/// <summary>Provides Hikka links.</summary>
public sealed class HikkaExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public HikkaExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "hikka", "Hikka", mapping => !string.IsNullOrWhiteSpace(mapping.HikkaSlug)) { }
}

/// <summary>Provides IMDb links.</summary>
public sealed class ImdbExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public ImdbExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "imdb", "IMDb", mapping => !string.IsNullOrWhiteSpace(mapping.ImdbId)) { }
}

/// <summary>Provides Kaize links.</summary>
public sealed class KaizeExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public KaizeExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "kaize", "Kaize", mapping => !string.IsNullOrWhiteSpace(mapping.KaizeSlug)) { }
}

/// <summary>Provides Kitsu links.</summary>
public sealed class KitsuExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public KitsuExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "kitsu", "Kitsu", mapping => mapping.KitsuId is > 0) { }
}

/// <summary>Provides Letterboxd links.</summary>
public sealed class LetterboxdExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public LetterboxdExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "letterboxd", "Letterboxd", mapping => !string.IsNullOrWhiteSpace(mapping.LetterboxdSlug)) { }
}

/// <summary>Provides LiveChart links.</summary>
public sealed class LiveChartExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public LiveChartExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "livechart", "LiveChart", mapping => mapping.LiveChartId is > 0) { }
}

/// <summary>Provides MyAnimeList links.</summary>
public sealed class MyAnimeListExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public MyAnimeListExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "myanimelist", "MyAnimeList", mapping => mapping.MyAnimeListId is > 0) { }
}

/// <summary>Provides Nautiljon links.</summary>
public sealed class NautiljonExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public NautiljonExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "nautiljon", "Nautiljon", mapping => !string.IsNullOrWhiteSpace(mapping.NautiljonSlug)) { }
}

/// <summary>Provides Otak Otaku links.</summary>
public sealed class OtakotakuExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public OtakotakuExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "otakotaku", "Otak Otaku", mapping => mapping.OtakotakuId is > 0) { }
}

/// <summary>Provides Shikimori links.</summary>
public sealed class ShikimoriExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public ShikimoriExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "shikimori", "Shikimori", mapping => mapping.ShikimoriId is > 0) { }
}

/// <summary>Provides Shoboi links.</summary>
public sealed class ShoboiExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public ShoboiExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "shoboi", "Shoboi", mapping => mapping.ShoboiId is > 0) { }
}

/// <summary>Provides the mapped Simkl link.</summary>
public sealed class SimklExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public SimklExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "simkl", "Simkl", mapping => mapping.SimklId is > 0 && mapping.AniDbId is > 0) { }
}

/// <summary>Provides TMDB links.</summary>
public sealed class TmdbExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public TmdbExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "themoviedb", "TMDB", mapping => mapping.TmdbId is > 0) { }
}

/// <summary>Provides TVDB links.</summary>
public sealed class TvdbExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public TvdbExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "thetvdb", "TVDB", mapping => mapping.TvdbId is > 0) { }
}

/// <summary>Provides Trakt links.</summary>
public sealed class TraktExternalUrlProvider : MappedExternalUrlProvider
{
    /// <summary>Initializes the provider.</summary>
    public TraktExternalUrlProvider(GeckyzzMappingService service)
        : base(service, "trakt", "Trakt", mapping => !string.IsNullOrWhiteSpace(mapping.TraktSlug)) { }
}
