← [Back to overview](index.md)

# Movies — Description

## Purpose

The movie list (*Movies*) shows all scanned movies of the media library. For each movie Ember Media Manager manages — alongside the media files — the complete metadata (title, original title, year, genre, plot, cast, ratings, external IDs), all image types (poster, fanart, banner, ClearArt, ClearLogo, DiscArt, landscape, extrathumbs, extrafanart), trailers, themes and subtitles — compatible with the Jellyfin file and NFO schema.

## How it works

- **Editing:** *Edit Movie* opens the edit dialog with all metadata fields, image selection, cast, streams and file information.
- **Scraping:** Via the context menu *(Re)Scrape Movie* or *Custom Scraper...*, metadata/images/trailers are fetched from the enabled scrapers (see [Scrapers](../scraper/index.md)).
- **Images:** Images can be set from scraper results, from local files or via URL (*Set As Fanart*, `dlgImageManual`).
- **Sets:** Movies can be assigned to movie sets (*Set* field, *Sets Manager*).
- **Status:** *Mark*, *Lock*, *New* flag control batch actions and write protection.
- **DVD Profiler import:** Existing DVD Profiler collections can be imported via the import dialog and matched to movie entries.

## Examples

- Movie with wrong title: *Edit Movie* → correct title/year → saving writes NFO and database.
- Replace poster: in the detail area poster context menu → *Change* → choose a local file or scraper result.
- Re-scrape several movies: *Mark* the movies → *(Re)Scrape Selected Movies* → *Marked Movies - Auto*.

## Limitations

- Locked (*Lock*) movies are not modified by scrape and update runs.
- The IMDb ID is the leading unique identifier for movies (project design decision); if missing, it is resolved via the scrapers.
- File changes made outside Ember only appear after the next library scan.
