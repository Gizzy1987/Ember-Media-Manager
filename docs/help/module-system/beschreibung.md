← [Back to overview](index.md)

# Module System — Description

## Purpose

Ember Media Manager is built around a loadable module system: all scrapers, tools and interfaces are standalone assemblies in the `Modules` directory, loaded at startup and addressed through clearly defined interfaces. This allows adding new sources and tools without changing the main application.

## How it works

- **Loading:** The `ModulesManager` (singleton) loads all assemblies from `Modules/` and sorts them by implemented interface into separate lists (generic modules, data/image/theme/trailer scrapers per content type).
- **Contract:** Every module implements `GenericModule` — with `Init`, `RunGeneric`, `InjectSetup`, properties (`ModuleName`, `ModuleVersion`, `Enabled`, `IsBusy`) and events (`GenericEvent`, `ModuleEnabledChanged`, `ModuleSettingsChanged`, `SetupNeedsRestart`).
- **Events:** Modules subscribe to `ModuleEventType` events via `ModuleType`; `frmMain`/`ModulesManager` call `RunGeneric` when the event occurs (e.g. `AfterEdit_Movie`, `AfterUpdateDB_Movie`, `ScraperSingle_*`).
- **UI integration:** Modules provide their settings panel via `InjectSetup()` into the settings dialog and register toolstrip entries in the main UI.
- **Activation:** Each module can be enabled/disabled; the state lives in the settings.

## Examples (module kinds)

- Generic module: Bulk Renamer, Media File Manager, Tag Manager
- Interface module: Jellyfin interface, Trakt.tv interface
- Scraper module: `scraper.Data.IMDB`, `scraper.Image.FanartTV`, `scraper.Trailer.YouTube`, `scraper.Theme.TelevisionTunes`

## Limitations

- Modules are loaded via reflection from the `Modules` folder — assemblies not present on disk or not matching the contract do not appear.
- Two project directories (`scraper.EmberCore.XML`, `scraper.TVDB.Poster`) exist in the repository but are not part of the solution and are not built.
