# Jellyfin geckyzz

A Jellyfin plugin that adds an external link to `https://geckyzz.my.id` for movies, series, and seasons. It uses the item's TMDB ID, falling back to a MyAnimeList ID when TMDB is unavailable. Season links include the season index.

When an item or one of its video filenames contains `CicakRumah` or `TokekHutan`, the link uses the `/fsanime/` route instead of `/anime/`.

The plugin looks up geckyzz API mappings and adds direct links for available mapped platforms, following AnimeAPI's platform URL formats. Links go straight to each platform, without an AnimeAPI redirect. Simkl links are only added when an AniDB mapping is absent.

## Install

Add this plugin repository in Jellyfin:

`https://raw.githubusercontent.com/geckyzz/jellyfin-geckyzz/main/manifest.json`

The repository manifest will list installable versions after the first plugin release. Builds run on pushes and pull requests to `main`.
