using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Geckyzz;

/// <summary>
/// Mappings returned by the geckyzz API.
/// </summary>
public sealed class GeckyzzMapping
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the MyAnimeList identifier.
    /// </summary>
    [JsonPropertyName("myanimelist")]
    public long? MyAnimeListId { get; set; }

    /// <summary>
    /// Gets or sets the canonical geckyzz path.
    /// </summary>
    [JsonPropertyName("geckyzz")]
    public string? GeckyzzPath { get; set; }

    /// <summary>
    /// Gets or sets the AniDB identifier.
    /// </summary>
    [JsonPropertyName("anidb")]
    public long? AniDbId { get; set; }

    /// <summary>
    /// Gets or sets the AniList identifier.
    /// </summary>
    [JsonPropertyName("anilist")]
    public long? AniListId { get; set; }

    /// <summary>
    /// Gets or sets the Anime News Network identifier.
    /// </summary>
    [JsonPropertyName("animenewsnetwork")]
    public long? AnimeNewsNetworkId { get; set; }

    /// <summary>
    /// Gets or sets the Anime-Planet slug.
    /// </summary>
    [JsonPropertyName("animeplanet")]
    public string? AnimePlanetSlug { get; set; }

    /// <summary>
    /// Gets or sets the AniSearch identifier.
    /// </summary>
    [JsonPropertyName("anisearch")]
    public long? AniSearchId { get; set; }

    /// <summary>
    /// Gets or sets the Annict identifier.
    /// </summary>
    [JsonPropertyName("annict")]
    public long? AnnictId { get; set; }

    /// <summary>
    /// Gets or sets the Simkl identifier.
    /// </summary>
    [JsonPropertyName("simkl")]
    public long? SimklId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB identifier.
    /// </summary>
    [JsonPropertyName("themoviedb")]
    public long? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB season identifier.
    /// </summary>
    [JsonPropertyName("themoviedb_season_id")]
    public long? TmdbSeasonId { get; set; }

    /// <summary>
    /// Gets or sets the TMDB media type.
    /// </summary>
    [JsonPropertyName("themoviedb_type")]
    public string? TmdbType { get; set; }

    /// <summary>
    /// Gets or sets the TVDB identifier.
    /// </summary>
    [JsonPropertyName("thetvdb")]
    public long? TvdbId { get; set; }

    /// <summary>
    /// Gets or sets the TVDB season identifier.
    /// </summary>
    [JsonPropertyName("thetvdb_season_id")]
    public long? TvdbSeasonId { get; set; }

    /// <summary>
    /// Gets or sets the Hikka slug.
    /// </summary>
    [JsonPropertyName("hikka")]
    public string? HikkaSlug { get; set; }

    /// <summary>
    /// Gets or sets the IMDb identifier.
    /// </summary>
    [JsonPropertyName("imdb")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the Kaize slug.
    /// </summary>
    [JsonPropertyName("kaize")]
    public string? KaizeSlug { get; set; }

    /// <summary>
    /// Gets or sets the Kaize identifier.
    /// </summary>
    [JsonPropertyName("kaize_id")]
    public long? KaizeId { get; set; }

    /// <summary>
    /// Gets or sets the Kitsu identifier.
    /// </summary>
    [JsonPropertyName("kitsu")]
    public long? KitsuId { get; set; }

    /// <summary>
    /// Gets or sets the Letterboxd list identifier.
    /// </summary>
    [JsonPropertyName("letterboxd_lid")]
    public string? LetterboxdListId { get; set; }

    /// <summary>
    /// Gets or sets the Letterboxd slug.
    /// </summary>
    [JsonPropertyName("letterboxd_slug")]
    public string? LetterboxdSlug { get; set; }

    /// <summary>
    /// Gets or sets the Letterboxd user identifier.
    /// </summary>
    [JsonPropertyName("letterboxd_uid")]
    public long? LetterboxdUserId { get; set; }

    /// <summary>
    /// Gets or sets the LiveChart identifier.
    /// </summary>
    [JsonPropertyName("livechart")]
    public long? LiveChartId { get; set; }

    /// <summary>
    /// Gets or sets the Nautiljon slug.
    /// </summary>
    [JsonPropertyName("nautiljon")]
    public string? NautiljonSlug { get; set; }

    /// <summary>
    /// Gets or sets the Nautiljon identifier.
    /// </summary>
    [JsonPropertyName("nautiljon_id")]
    public long? NautiljonId { get; set; }

    /// <summary>
    /// Gets or sets the Notify identifier.
    /// </summary>
    [JsonPropertyName("notify")]
    public string? NotifyId { get; set; }

    /// <summary>
    /// Gets or sets the Otakotaku identifier.
    /// </summary>
    [JsonPropertyName("otakotaku")]
    public long? OtakotakuId { get; set; }

    /// <summary>
    /// Gets or sets the Shikimori identifier.
    /// </summary>
    [JsonPropertyName("shikimori")]
    public long? ShikimoriId { get; set; }

    /// <summary>
    /// Gets or sets the Shoboi identifier.
    /// </summary>
    [JsonPropertyName("shoboi")]
    public long? ShoboiId { get; set; }

    /// <summary>
    /// Gets or sets the Silveryasha identifier.
    /// </summary>
    [JsonPropertyName("silveryasha")]
    public long? SilveryashaId { get; set; }

    /// <summary>
    /// Gets or sets the Trakt identifier.
    /// </summary>
    [JsonPropertyName("trakt")]
    public long? TraktId { get; set; }

    /// <summary>
    /// Gets or sets the Trakt season number.
    /// </summary>
    [JsonPropertyName("trakt_season")]
    public int? TraktSeason { get; set; }

    /// <summary>
    /// Gets or sets the Trakt season identifier.
    /// </summary>
    [JsonPropertyName("trakt_season_id")]
    public long? TraktSeasonId { get; set; }

    /// <summary>
    /// Gets or sets whether the Trakt mapping may be invalid.
    /// </summary>
    [JsonPropertyName("trakt_may_invalid")]
    public bool? TraktMayBeInvalid { get; set; }

    /// <summary>
    /// Gets or sets the Trakt slug.
    /// </summary>
    [JsonPropertyName("trakt_slug")]
    public string? TraktSlug { get; set; }

    /// <summary>
    /// Gets or sets the Trakt media type.
    /// </summary>
    [JsonPropertyName("trakt_type")]
    public string? TraktType { get; set; }
}
