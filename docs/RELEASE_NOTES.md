# Release Notes

## Version 1.12 (2026-11-07)

### Important Notes Before Update

- The application is now released as **Ember Media Manager for Jellyfin**; the product name in the program, the splash screen and the about dialog has been changed accordingly.
- The user interface language is now standard German (`de-DE`); the translation file `EmberAPI/Translations/de_DE.xml` is complete.
- The Kodi interface module (`generic.Interface.Kodi`) and the C# client library `KodiAPI` (`XBMCRPC`) have been removed. Individual items are no longer synchronized with a media server — the Jellyfin interface triggers a library refresh instead.
- The naming presets in the settings have been reduced to "Jellyfin" and "Expert"; the former "Kodi", "Kodi Addons", "NMT" and "Boxee" presets are gone. Existing profiles keep their stored settings.
- The Kodi trailer URL format (`plugin://plugin.video.youtube`) is no longer supported — Jellyfin uses normal YouTube URLs.

### What's New

- New Jellyfin interface module `generic.Interface.Jellyfin`: Jellyfin hosts with name, base URL and API key, connection test via `GET /System/Info`, list of the server libraries via `GET /Library/VirtualFolders`.
- Library refresh via `POST {base-url}/Library/Refresh` (header `X-Emby-Token`); a refresh is triggered automatically after sync and remove events and is debounced, so a batch import results in a single scan.
- Menu entry "Jellyfin" in the tools menu and in the tray menu with the items "Update Library" and "Check Connection".
- The file and folder layout follows the Jellyfin conventions: movie and show folders `Title (Year)`, season folders `Season 01`, episode files `Series - SxxExx - Title`, images `poster.jpg` / `fanart.jpg` / `banner.jpg` (show and season), episode artwork `<videoname>-thumb.jpg`, NFO files `movie.nfo` / `<videoname>.nfo` / `tvshow.nfo`.
- New splash screen for the Jellyfin edition.
- Documentation adapted: `docs/help/kodi/` replaced by `docs/help/jellyfin/`, `README.md` and the feature documentation updated.

### Wichtige Hinweise vor dem Update

- Die Anwendung wird nun als **Ember Media Manager for Jellyfin** veröffentlicht; der Produktname in Programm, Splashscreen und „Über"-Dialog wurde entsprechend geändert.
- Die Sprache der Oberfläche ist jetzt Standard Deutsch (`de-DE`); die Übersetzungsdatei `EmberAPI/Translations/de_DE.xml` ist vollständig.
- Das Kodi-Interface-Modul (`generic.Interface.Kodi`) und die C#-Client-Bibliothek `KodiAPI` (`XBMCRPC`) wurden entfernt. Einzelne Einträge werden nicht mehr mit einem Medienserver synchronisiert — stattdessen löst die Jellyfin-Schnittstelle eine Bibliotheksaktualisierung aus.
- Die Namens-Presets in den Einstellungen wurden auf „Jellyfin" und „Expert" reduziert; die bisherigen Presets „Kodi", „Kodi Addons", „NMT" und „Boxee" sind entfallen. Bestehende Profile behalten ihre gespeicherten Einstellungen.
- Das Kodi-Trailer-URL-Format (`plugin://plugin.video.youtube`) wird nicht mehr unterstützt — Jellyfin verwendet normale YouTube-URLs.

### Neuerungen

- Neues Jellyfin-Interface-Modul `generic.Interface.Jellyfin`: Jellyfin-Hosts mit Name, Basis-URL und API-Schlüssel, Verbindungstest über `GET /System/Info`, Anzeige der Server-Bibliotheken über `GET /Library/VirtualFolders`.
- Bibliotheksaktualisierung über `POST {Basis-URL}/Library/Refresh` (Header `X-Emby-Token`); die Aktualisierung wird nach Synchronisations- und Entfernen-Ereignissen automatisch ausgelöst und entprellt, sodass ein Stapelimport nur einen Scan auslöst.
- Menüpunkt „Jellyfin" im Werkzeuge-Menü und im Tray-Menü mit den Unterpunkten „Bibliothek aktualisieren" und „Verbindung testen".
- Das Datei- und Ordner-Layout folgt den Jellyfin-Konventionen: Film- und Serienordner `Titel (Jahr)`, Staffelordner `Season 01`, Episoden-Dateien `Serie - SxxExx - Titel`, Bilder `poster.jpg` / `fanart.jpg` / `banner.jpg` (Serie und Staffel), Episoden-Bildmaterial `<videoname>-thumb.jpg`, NFO-Dateien `movie.nfo` / `<videoname>.nfo` / `tvshow.nfo`.
- Neuer Splashscreen für die Jellyfin-Ausgabe.
- Dokumentation angepasst: `docs/help/kodi/` durch `docs/help/jellyfin/` ersetzt, `README.md` und die Featuredokumentation aktualisiert.

## Earlier release notes

### Important Notes Before Update

- The solution now targets .NET Framework 4.8 — the .NET Framework 4.8 runtime is required to run the application.
- The TVDB library is vendored inside the repository (`TheTVDBApi/`); a separate checkout next to the repository is no longer needed.
- NuGet restore is required before building (`.nuget\NuGet.exe restore "Ember Media Manager.sln"`); no .NET 4.8 Developer Pack is needed.

### What's New

- `Newtonsoft.Json` updated uniformly to 13.0.3 across all projects — fixes a high-severity denial-of-service vulnerability (GHSA-5crp-9r3c-p9vr).
- Complete feature documentation added under `docs/help/` (media library, movies, TV shows, movie sets, scrapers, Kodi, Trakt.tv, tools, settings, module system, database, build).
- `changes.log` backfilled with retroactive entries for all major development steps (2013–2020).
- `README.md` updated: feature overview, project structure, test status, license and documentation references.
- Solution builds self-contained again after a fresh clone (previously failed on the missing external `TheTVDBApi` project).
- All projects retargeted to .NET Framework 4.8 (previously mixed 3.5 / 4.5 / 4.8 targets).
- NuGet tooling updated to 6.14.0; package restore works again.
- Compile errors fixed (missing tag database methods, renamed XML cache call, invalid `Prefer32Bit` flags, conditional post-build copy).
- Fixed a startup crash on first run: missing `Settings.xml` no longer throws `FileNotFoundException`; default settings are used instead.
- Build instructions added to `README.md` and `docs/help/build/`.

### Wichtige Hinweise vor dem Update

- Die Solution zielt nun auf .NET Framework 4.8 — zum Ausführen der Anwendung ist die .NET Framework 4.8 Runtime erforderlich.
- Die TVDB-Bibliothek ist im Repository vendored (`TheTVDBApi/`); ein separater Checkout neben dem Repository ist nicht mehr nötig.
- Vor dem Bauen ist ein NuGet-Restore erforderlich (`.nuget\NuGet.exe restore "Ember Media Manager.sln"`); ein .NET 4.8 Developer Pack wird nicht benötigt.

### Neuerungen

- `Newtonsoft.Json` in allen Projekten einheitlich auf 13.0.3 aktualisiert — behebt eine hochkritische Denial-of-Service-Schwachstelle (GHSA-5crp-9r3c-p9vr).
- Vollständige Featuredokumentation unter `docs/help/` ergänzt (Medienbibliothek, Filme, Serien, Filmsammlungen, Scraper, Kodi, Trakt.tv, Werkzeuge, Einstellungen, Modulsystem, Datenbank, Build).
- `changes.log` um retroaktive Einträge für alle wesentlichen Entwicklungsschritte (2013–2020) ergänzt.
- `README.md` aktualisiert: Feature-Übersicht, Projektstruktur, Teststatus, Lizenz und Dokumentationsverweise.
- Die Solution ist nach einem frischen Clone wieder eigenständig baubar (zuvor scheiterte der Build am fehlenden externen `TheTVDBApi`-Projekt).
- Alle Projekte wurden auf .NET Framework 4.8 angehoben (zuvor gemischte Targets 3.5 / 4.5 / 4.8).
- NuGet-Werkzeuge auf 6.14.0 aktualisiert; der Paket-Restore funktioniert wieder.
- Kompilierfehler behoben (fehlende Tag-Datenbankmethoden, umbenannter XML-Cache-Aufruf, ungültige `Prefer32Bit`-Einstellungen, abgesichertes Post-Build-Kopieren).
- Startabsturz beim ersten Lauf behoben: Eine fehlende `Settings.xml` löst keine `FileNotFoundException` mehr aus; stattdessen werden Standardeinstellungen verwendet.
- Build-Anleitung in `README.md` und `docs/help/build/` ergänzt.
