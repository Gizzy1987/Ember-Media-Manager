# Ember Media Manager for Jellyfin

> **Dies ist ein Fork.** Dieses Repository ist ein Fork von
> [martin-stromberg/Ember-Media-Manager](https://github.com/martin-stromberg/Ember-Media-Manager),
> das seinerseits auf [DanCooper/Ember-MM-Newscraper](https://github.com/DanCooper/Ember-MM-Newscraper)
> — dem offiziellen Zuhause von Ember Media Manager — basiert.
>
> **Das gesamte Verdienst an der Anwendung selbst gebührt Dan Cooper und dem Ember-Team.**
> Ember Media Manager wurde von **Dan Cooper** und dem **Ember-Team** entwickelt und steht unter
> der GNU General Public License v3. Dieses Repository enthält den **Quellcode** dieses Projekts
> mit einer auf Jellyfin zugeschnittenen Anpassung; ohne die Arbeit des Ember-Teams gäbe es
> diese Anwendung nicht.
>
> Dieser Fork dient **eigenen Zwecken und Anforderungen** (z. B. CI/CD-Infrastruktur,
> Code-Review und Fehlerbehebung). Er steht in keiner Verbindung zum Ember-Team, wird von ihm
> weder unterstützt noch mit ihm abgestimmt, und er beansprucht nicht, das Originalprojekt
> zu ersetzen. Wer Ember Media Manager sucht, sollte das [Originalprojekt](https://github.com/DanCooper/Ember-MM-Newscraper)
> verwenden.
>
> Dieser Fork ist die **Jellyfin-Variante** von Ember Media Manager (Version 1.12): Die
> Kodi-Schnittstelle wurde durch eine Jellyfin-Schnittstelle ersetzt, Datei- und Ordnerlayout
> folgen den Jellyfin-Konventionen und die Oberflächensprache ist Standard-German.

[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/github/license/Gizzy1987/Ember-Media-Manager)](EmberMediaManager/License.txt)
[![Release](https://img.shields.io/github/actions/workflow/status/Gizzy1987/Ember-Media-Manager/release.yml?label=Release)](https://github.com/Gizzy1987/Ember-Media-Manager/actions/workflows/release.yml)
[![Pre-Release](https://img.shields.io/github/actions/workflow/status/Gizzy1987/Ember-Media-Manager/staging-ci.yml?branch=staging&label=Pre-Release)](https://github.com/Gizzy1987/Ember-Media-Manager/actions/workflows/staging-ci.yml)

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
[`docs/RELEASE_NOTES.md`](docs/RELEASE_NOTES.md). Das Ergebnis des Code-Reviews vom
08.11.2026 mit allen 83 behobenen Befunden und den Performance-Messungen steht in
[`docs/review/2026-11-08-code-review.md`](docs/review/2026-11-08-code-review.md).

## Links

- Offizielles Upstream-Projekt: https://github.com/DanCooper/Ember-MM-Newscraper
- Zwischenstand dieses Forks: https://github.com/martin-stromberg/Ember-Media-Manager

## Bauen

Voraussetzungen: Windows, Visual Studio (MSBuild), .NET Framework 4.8 Runtime.
Ein .NET Framework 4.8 Developer Pack wird **nicht** benötigt — die Referenz-Assemblies werden
über NuGet (`Microsoft.NETFramework.ReferenceAssemblies.net48`) wiederhergestellt und über
`Directory.Build.props` eingebunden.

1. NuGet-Pakete wiederherstellen (packages.config-basiert):

   ```
   .nuget\NuGet.exe restore "Ember Media Manager for Jellyfin.sln"
   ```

2. Solution bauen (unterstützte Plattformen: x86 und x64; `Any CPU` kompiliert ebenfalls,
   liefert aber nicht die nativen Plattform-Abhängigkeiten aus):

   ```
   msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64
   ```

   Die ausführbare Datei ist `Ember Media Manager for Jellyfin.exe`. Die Ausgabe landet in
   `EmberMM - Release - x64` (bzw. `... - x86` / `... - AnyCPU`).

Alle Projekte zielen auf .NET Framework 4.8. Die `TVDB`-Bibliotheksquellen sind unter
`TheTVDBApi/` eingebunden (GPL-3.0, ursprünglich von `DanCooper/TheTVDBApi`).

## Projektstruktur

- `EmberMediaManager/` — WinForms-Hauptanwendung
- `EmberAPI/` — Kernbibliothek (Datenbank, Scanner, Modulsystem, NFO, Bilder, Media-Info, Einstellungen)
- `Addons/` — Add-on-Module (Allgemeinwerkzeuge, Jellyfin-/Trakt-Schnittstellen, alle Scraper)
- `Trakttv/`, `TheTVDBApi/` — API-Client-Bibliotheken
- `BuildSetup/` — NSIS-Installer und Build-Skripte
- `docs/help/` — Funktionsdokumentation

## Tests

Das Repository enthält ein Alt-MSTest-Projekt (`EmberAPI_Test/`). Es ist derzeit **nicht Teil
der Solution** und baut nicht — es verweist auf ein `UnitTests`-Projekt, das nicht im
Repository liegt. Es gibt momentan also keine ausführbaren automatisierten Tests.

## Kontinuierliche Integration / Git-Hooks

Das Repository bringt zwei Ebenen von Qualitätssicherungen mit:

- **Lokale Git-Hooks** (`.githooks/`): ein `pre-commit`-Hook, der die `.resx`-Lokalisierung auf
  Konsistenz prüft, und ein `pre-push`-Hook, der das CI-Sicherheitsgate spiegelt
  (`nuget restore` + NU190x-Schwachstellenprüfung, blockierend). Einmal pro Clone aktivieren:

  ```
  .githooks\install-hooks.cmd    :: Windows
  ./.githooks/install-hooks.sh   # Linux/macOS (Git Bash)
  ```

- **GitHub Actions** (`.github/workflows/`): Pull Requests gegen `staging` laufen als
  `PR CI for Staging` (NuGet-Schwachstellengate + Builds `Debug|x86` und `Release|x64`).
  Pushes auf `staging` erzeugen zusätzlich `vX.Y.Z-rc.N`-Pre-Releases (`Pre-Release`); ein
  erfolgreicher Lauf öffnet einen Entwurf-Promotion-PR `staging` → `master` (PRs gegen `master`
  werden nur aus `staging` angenommen). Pushes auf `master` lösen einen Back-Merge-PR
  `master` → `staging` und den `Release`-Workflow aus (semantic-release; manuelle
  `v*.*.*`-Tags werden ebenfalls unterstützt). Ein wöchentlicher `Security Scan` läuft
  montags um 04:00 UTC.

Der `pre-commit`-Hook benötigt Python 3 im `PATH`; der `pre-push`-Scan läuft unter Windows
direkt aus (unter Linux/macOS wird Mono benötigt, sonst wird er mit einer Warnung
übersprungen — das CI-Gate auf der Serverseite gilt trotzdem). Vollständige Dokumentation:
[CI/CD & Git Hooks](docs/help/ci-cd/index.md).

## Lizenz

GPL-3.0 — siehe `EmberMediaManager/License.txt`.

Copyright (C) Dan Cooper und das Ember-Team für Ember Media Manager.
Copyright (C) die Mitwirkenden dieses Forks für die Jellyfin-Anpassung.
