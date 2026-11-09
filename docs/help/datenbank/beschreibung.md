← [Back to overview](index.md)

# Database — Description

## Purpose

The local SQLite database holds the entire library state: movies, movie sets, TV shows, seasons, episodes and all related data (persons, genres, studios, countries, ratings, artwork, tags, external IDs, file and stream information, source directories). The tables follow the legacy `MyVideos` naming convention inherited from earlier Ember versions.

## How it works

- **File:** `MyVideos{Profile}.emm` in the profile directory; all access goes through the `Database` class (`System.Data.SQLite`).
- **Schema:** Initial schema in `MyVideosDBSQL.txt`; evolution via numbered patch files (`MyVideosDBSQL_v2` … `v47`) applied when the database is opened.
- **Transactions:** Batch operations (scan, multi-scrape) run transactionally (`CommandsTransaction`); single actions run directly (`CommandsNoTransaction`).
- **Flags:** Entries carry status such as New, Marked, Lock as well as source and file references.
- **Cleanup:** Cleanup functions remove orphaned entries (missing files, empty seasons, unused genres/studios/countries).

## Limitations

- The database is not intended for direct external write access; changes belong in the application.
- A schema upgrade is one-directional — backing up the `.emm` file before version changes is advisable.
- A corrupted database can only be recovered from a backup or rebuilt via a fresh scan.
