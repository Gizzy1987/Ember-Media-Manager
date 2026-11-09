# Ember Media Manager for Jellyfin

> **Dies ist ein privates Projekt.** Es ist **nicht fertig** und **nicht einsatzbereit.**
> Es dient meinen eigenen Zwecken und Anforderungen; es ist nicht für den Produktivbetrieb
> gedacht, nicht begleitet, nicht getestet und nicht für den Allgemeinbetrieb freigegeben.
> Es besteht keine Verbindung zum Ember-Team, es wird von diesem weder unterstützt noch mit
> ihm abgestimmt, und es ersetzt das Originalprojekt nicht.
>
> **Hauptcredits gebühren Dan Cooper und dem Ember-Team.** Ember Media Manager wurde von
> **Dan Cooper** und dem **Ember-Team** entwickelt und steht unter der GNU General Public
> License v3. Dieses Repository enthält den **Quellcode** dieses Projekts mit einer auf
> Jellyfin zugeschnittenen Anpassung — ohne die Arbeit des Ember-Teams gäbe es diese
> Anwendung nicht.
>
> **Fork-Kette:** [DanCooper/Ember-MM-Newscraper](https://github.com/DanCooper/Ember-MM-Newscraper)
> (offizielles Upstream) → [martin-stromberg/Ember-Media-Manager](https://github.com/martin-stromberg/Ember-Media-Manager)
> → dieses Repository.
>
> **Jellyfin-Variante (Version 2.0):** Die Kodi-Schnittstelle wurde durch eine
> Jellyfin-Schnittstelle ersetzt, Datei- und Ordnerlayout folgen den Jellyfin-Konventionen
> und die Oberflächensprache ist Standard-German.

[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/Gizzy1987/Ember-Media-Manager)](EmberMediaManager/License.txt)
[![Release](https://img.shields.io/github/actions/workflow/status/Gizzy1987/Ember-Media-Manager/release.yml?label=Release)](https://github.com/Gizzy1987/Ember-Media-Manager/actions/workflows/release.yml)

Ember Media Manager for Jellyfin ist ein Windows-Medienmanager für Filme, Serien und Filmsets.
Er scannt Medienordner in eine lokale Bibliothek, holt Metadaten, Cover, Trailer und Themes aus
Online-Quellen und erzeugt Jellyfin-kompatible NFO- und Bilddateien. Add-ons liefern die
Jellyfin-Schnittstelle, die Trakt.tv-Synchronisation sowie Werkzeuge wie Massenumbenennung
und Export.

## Funktionen

- Medienbibliothek für Filme, Serien/Staffeln/Folgen und Filmsets (lokale SQLite-Datenbank)
- Austauschbare Scraper — Metadaten (IMDb, TMDb, TheTVDB, OMDb, OFDb, Moviepilot, Trakt.tv),
  Bilder (TMDb, Fanart.tv, TheTVDB), Trailer (YouTube, TMDb, Apple, hd-trailers.net, Videobuster)
  und Themes (YouTube, TelevisionTunes)
- Jellyfin-kompatibles Dateilayout: NFO-Dateien und Poster/Fanart/Banner direkt neben den Medien
  - Film- und Serienordner: `Titel (Jahr)`, Staffelordner: `Season 01`
  - Episodendateien: `Serie - SxxExx - Titel`, Episodenbilder: `<videoname>-thumb.jpg`
  - Bilder: `poster.jpg` / `fanart.jpg` / `banner.jpg` (Serie und Staffel),
    NFO-Dateien: `movie.nfo` / `<videoname>.nfo` / `tvshow.nfo`
- Jellyfin-Schnittstellen-Modul: verwaltet Jellyfin-Hosts (Name, Basis-URL, API-Schlüssel),
  prüft die Verbindung und löst einen Bibliotheks-Refresh aus
- Trakt.tv-Modul: Abspielstatus-/Playcount-Synchronisation, Listen und Bewertungen
- Werkzeuge: Bulk Renamer, Movie List Exporter, Media File Manager, Tag Manager,
  Medienlisten-/Filter-Editor, Zuordnungs-Editoren, Video-Quellen-Zuordnung
- Mehrere Profile, erweiterte Einstellungen, Verwaltung von Offline-Medien (Stubs)
- Erweiterbares Modulsystem: Add-on-Assemblies werden aus dem Verzeichnis `Modules` geladen

## Dokumentation

Die Funktionsdokumentation liegt unter [`docs/help/`](docs/help/index.md). Die Änderungshistorie
wird in [`changes.log`](changes.log) geführt, die Release-Notizen stehen in
[`docs/RELEASE_NOTES.md`](docs/RELEASE_NOTES.md).

Die beiden Review-Berichte dokumentieren die überprüften und behobenen Fehler sowie die
gemessenen Performance-Gewinne:
- [`docs/review/2026-11-08-code-review.md`](docs/review/2026-11-08-code-review.md) —
  Code-Review Runde 1: 83 Befunde, davon 2 mit Datenverlust
- [`docs/review/2026-11-09-fork-vergleich-nagten.md`](docs/review/2026-11-09-fork-vergleich-nagten.md) —
  Vergleich mit dem Fork `nagten/Ember-MM-Newscraper` und Übernahme der belegten Fixes

## Links

- Offizielles Upstream-Projekt: https://github.com/DanCooper/Ember-MM-Newscraper

## Bauen

Voraussetzungen: Windows, Visual Studio (MSBuild), .NET Framework 4.8 Runtime.
Ein .NET Framework 4.8 Developer Pack wird **nicht** benötigt — die Referenz-Assemblies werden
über NuGet (`Microsoft.NETFramework.ReferenceAssemblies.net48`) wiederhergestellt und über
`Directory.Build.props` eingebunden.

1. NuGet-Pakete wiederherstellen (packages.config-basiert):

   ```
   .nuget\NuGet.exe restore "Ember Media Manager for Jellyfin.sln"
   ```

2. Solution bauen:

   ```
   msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64
   ```

   Die ausführbare Datei ist `Ember Media Manager for Jellyfin.exe`. Die Ausgabe landet in
   `EmberMM - Release - x64`.

Alle Projekte zielen auf .NET Framework 4.8. Die `TVDB`-Bibliotheksquellen sind unter
`TheTVDBApi/` eingebunden (GPL-3.0, ursprünglich von `DanCooper/TheTVDBApi`).

## Projektstruktur

- `EmberMediaManager/` — WinForms-Hauptanwendung
- `EmberAPI/` — Kernbibliothek (Datenbank, Scanner, Modulsystem, NFO, Bilder, Media-Info, Einstellungen)
- `Addons/` — Add-on-Module (Allgemeinwerkzeuge, Jellyfin-/Trakt-Schnittstellen, alle Scraper)
- `Trakttv/`, `TheTVDBApi/` — API-Client-Bibliotheken
- `BuildSetup/` — NSIS-Installer und Build-Skripte
- `docs/help/` — Funktionsdokumentation
- `docs/review/` — Code-Review-Berichte

## Tests

Das Repository enthält ein Alt-MSTest-Projekt (`EmberAPI_Test/`). Es ist derzeit **nicht Teil
der Solution** und baut nicht — es verweist auf ein `UnitTests`-Projekt, das nicht im
Repository liegt. Es gibt momentan also keine ausführbaren automatisierten Tests.

## CI und Git-Hooks

Das Repository bringt zwei Ebenen von Qualitätssicherungen mit:

- **Lokale Git-Hooks** (`.githooks/`): ein `pre-commit`-Hook, der die `.resx`-Lokalisierung auf
  Konsistenz prüft, und ein `pre-push`-Hook, der das CI-Sicherheitsgate spiegelt
  (`nuget restore` + NU190x-Schwachstellenprüfung, blockierend). Einmal pro Clone aktivieren:

  ```
  .githooks\install-hooks.cmd    :: Windows
  ./.githooks/install-hooks.sh   # Linux/macOS (Git Bash)
  ```

- **GitHub Actions** (`.github/workflows/`): ein `Release`-Workflow baut die Solution auf
  `master` und veröffentlicht das Paket, ein wöchentlicher `Security Scan` prüft die
  NuGet-Abhängigkeiten, und `verify-pr-source` schützt `master` vor direkten Pull Requests.
  Die Workflows für den `staging`-Branch (`Pre-Release`, Promotion-PR, Back-Merge) sind im
  Upstream vorgesehen, in diesem Fork aber **ohne Wirkung** — es existiert nur `master`.

Der `pre-commit`-Hook benötigt Python 3 im `PATH`; der `pre-push`-Scan läuft unter Windows
direkt aus (unter Linux/macOS wird Mono benötigt, sonst wird er mit einer Warnung
übersprungen — das serverseitige CI-Gate gilt trotzdem). Vollständige Dokumentation:
[CI/CD & Git Hooks](docs/help/ci-cd/index.md).

## Lizenz

GPL-3.0 — siehe `EmberMediaManager/License.txt`.

Copyright (C) Dan Cooper und das Ember-Team für Ember Media Manager.
Copyright (C) die Mitwirkenden dieses Forks für die Jellyfin-Anpassung.
