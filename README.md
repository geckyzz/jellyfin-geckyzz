# Jellyfin geckyzz

A Jellyfin plugin that enriches your anime library with direct links to `https://geckyzz.my.id` and a wide range of anime database platforms for movies, series, and seasons. Powered by [nattadasu's AnimeAPI](https://animeapi.my.id) with supplementary entries stored directly on geckyzz, it bridges your media with online tracking and metadata services.

## Features

- **Multi-Platform Anime Database Links**: Automatically queries mappings powered by [nattadasu's AnimeAPI](https://animeapi.my.id) (and geckyzz-stored entries) to populate direct external links for platforms including AniList, MyAnimeList, AniDB, Kitsu, Anime-Planet, LiveChart, Annict, AniSearch, Shikimori, Simkl, and more.
- **Fast Shortcut Links**: Provides direct external links based on TMDB and IMDb ID availability for WeTrakr, Letterboxd (for movies, with slug/TMDB/IMDb resolution), publicmetadb, Bingeboxd, Refract, Seenr, cinematique, Serializd, and PunchPlay (including season subpaths where supported).
- **Direct Destination Links**: Links navigate directly to each respective platform matching AnimeAPI URL formats without external redirects.
- **Flexible Identifier Lookup**: Resolves mappings using TMDB IDs, with fallback support for MyAnimeList (including `jellyfin-plugin-myanimelist` provider IDs and `[mal-<id>]` folder naming), AniList, and AniDB (from `jellyfin-meta-plugins` or folder tags).
- **Fansub Release Detection**: Automatically routes geckyzz links to `/fsanime/` instead of `/anime/` when an item or its video filenames contain `CicakRumah` or `TokekHutan` release markers.

## Install

Add this plugin repository in Jellyfin:

`https://raw.githubusercontent.com/geckyzz/jellyfin-geckyzz/main/manifest.json`

The repository manifest lists installable versions after each plugin release. Builds run automatically on pushes and pull requests to `main`.
