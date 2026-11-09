# Ember Media Manager for Jellyfin

> **This is a private fork.** This repository is a private fork of [DanCooper/Ember-MM-Newscraper](https://github.com/DanCooper/Ember-MM-Newscraper), the official home of Ember Media Manager. All credit for the application itself goes to **DanCooper and the Ember team**.
>
> This fork serves **my own purposes and requirements** only (e.g. CI/CD infrastructure experiments). Nothing done here is affiliated with, endorsed by, or coordinated with the Ember team in any way, and this fork makes no claim to replace or supersede the original project. If you are looking for Ember Media Manager, please use the upstream repository.
>
> This fork is the **Jellyfin variant** of Ember Media Manager (version 1.12): the Kodi interface has been replaced by a Jellyfin interface, the file and folder layout follows Jellyfin conventions and the user interface language is standard German.

[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/martin-stromberg/Ember-Media-Manager)](EmberMediaManager/License.txt)
[![Release](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Ember-Media-Manager/release.yml?label=Release)](https://github.com/martin-stromberg/Ember-Media-Manager/actions/workflows/release.yml)
[![Pre-Release](https://img.shields.io/github/actions/workflow/status/martin-stromberg/Ember-Media-Manager/staging-ci.yml?branch=staging&label=Pre-Release)](https://github.com/martin-stromberg/Ember-Media-Manager/actions/workflows/staging-ci.yml)

Ember Media Manager for Jellyfin is a Windows media manager for movies, TV shows and movie sets. It scans media folders into a local library, scrapes metadata, artwork, trailers and themes from online sources and produces Jellyfin-compatible NFO and artwork files. Add-ons provide the Jellyfin interface and Trakt.tv synchronization as well as bulk renaming, exporting and other tools.

## Features

- Media library for movies, TV shows/seasons/episodes and movie sets (local SQLite database)
- Pluggable scrapers — data (IMDb, TMDb, TheTVDB, OMDb, OFDb, Moviepilot, Trakt.tv), images (TMDb, Fanart.tv, TheTVDB), trailers (YouTube, TMDb, Apple, hd-trailers.net, Videobuster) and themes (YouTube, TelevisionTunes)
- Jellyfin-compatible file layout: NFO files and poster/fanart/banner artwork next to the media
  - Movie and TV show folders: `Title (Year)`, season folders: `Season 01`
  - Episode files: `Series - SxxExx - Title`, episode artwork: `<videoname>-thumb.jpg`
  - Images: `poster.jpg` / `fanart.jpg` / `banner.jpg` (show and season), NFO files: `movie.nfo` / `<videoname>.nfo` / `tvshow.nfo`
- Jellyfin interface module: manages Jellyfin hosts (name, base URL, API key), tests the connection and triggers a library refresh
- Trakt.tv module: watched-state/playcount sync, lists and ratings
- Tools: Bulk Renamer, Movie List Exporter, Media File Manager, Tag Manager, media list/filter editor, mapping editors, video source mapping
- Multi-profile support, advanced settings, offline media (stub) management
- Extensible module system: add-on assemblies loaded from the `Modules` directory

## Documentation

Feature documentation lives under [`docs/help/`](docs/help/index.md). The change history is tracked in [`changes.log`](changes.log); release notes are in [`docs/RELEASE_NOTES.md`](docs/RELEASE_NOTES.md).

## Links
- Upstream project: https://github.com/DanCooper/Ember-MM-Newscraper

## Building

Requirements: Windows, Visual Studio (MSBuild), .NET Framework 4.8 runtime.
A .NET Framework 4.8 Developer Pack is **not** required — the reference
assemblies are restored via NuGet (`Microsoft.NETFramework.ReferenceAssemblies.net48`)
and wired in through `Directory.Build.props`.

1. Restore NuGet packages (packages.config based):

   ```
   .nuget\NuGet.exe restore "Ember Media Manager for Jellyfin.sln"
   ```

2. Build the solution (supported platforms: x86 and x64; `Any CPU` also compiles,
   but does not ship the native per-platform dependencies):

   ```
   msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64
   ```

   The executable is `Ember Media Manager for Jellyfin.exe`. Output lands in `EmberMM - Release - x64` (resp. `... - x86` / `... - AnyCPU`).

All projects target .NET Framework 4.8. The `TVDB` library sources are vendored
under `TheTVDBApi/` (GPL-3.0, originally from `DanCooper/TheTVDBApi`).

## Project structure

- `EmberMediaManager/` — main WinForms application
- `EmberAPI/` — core library (database, scanner, module system, NFO, images, media info, settings)
- `Addons/` — add-on modules (generic tools, Jellyfin/Trakt interfaces, all scrapers)
- `Trakttv/`, `TheTVDBApi/` — API client libraries
- `BuildSetup/` — NSIS installer and build scripts
- `docs/help/` — feature documentation

## Tests

The repository contains a legacy MSTest project (`EmberAPI_Test/`). It is currently **not part of the solution** and does not build — it references a `UnitTests` project that is not in the repository. There are no executable automated tests at the moment.

## Continuous Integration / Git Hooks

The repository ships two layers of quality gates:

- **Local Git hooks** (`.githooks/`): a `pre-commit` hook that checks `.resx` localization consistency, and a `pre-push` hook that mirrors the CI security gate (`nuget restore` + NU190x vulnerability check, blocking). Activate them once per clone:

  ```
  .githooks\install-hooks.cmd    :: Windows
  ./.githooks/install-hooks.sh   # Linux/macOS (Git Bash)
  ```

- **GitHub Actions** (`.github/workflows/`): PRs against `staging` run `PR CI for Staging` (NuGet vulnerability gate + `Debug|x86` and `Release|x64` builds). Pushes to `staging` additionally create `vX.Y.Z-rc.N` pre-releases (`Pre-Release`); a successful run opens a draft promotion PR `staging` → `master` (PRs against `master` are only accepted from `staging`). Pushes to `master` trigger a back-merge PR `master` → `staging` and the `Release` workflow (semantic-release; manual `v*.*.*` tags are also supported). A weekly `Security Scan` runs every Monday 04:00 UTC.

The `pre-commit` hook needs Python 3 on `PATH`; the `pre-push` scan runs out of the box on Windows (on Linux/macOS it requires Mono, otherwise it is skipped with a warning — the server-side CI gate still applies). Full documentation: [CI/CD & Git Hooks](docs/help/ci-cd/index.md).

## License

GPL-3.0 — see `EmberMediaManager/License.txt`.
