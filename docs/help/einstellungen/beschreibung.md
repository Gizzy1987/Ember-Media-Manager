← [Back to overview](index.md)

# Settings — Description

## Purpose

Via *Edit → Settings...* the entire application is configured: movie and TV show sources, file and folder naming (NFO/artwork schema), scraper selection and order, module activation, filter and display options. Each module brings its own settings panel and embeds it into the dialog.

## How it works

- **Settings dialog:** tree view on the left, settings panel on the right; changes are applied with *Apply*.
- **Sources:** manage movie/TV show sources with *Add Source* (see [Media Library](../medienbibliothek/index.md)).
- **File naming:** configurable filename schemas per content type (e.g. `<movie>.nfo`, `<movie>-fanart.jpg`, `/extrathumbs/`) — Jellyfin-compatible defaults are preset.
- **Scrapers:** activation, order and field/image selection per scraper group.
- **Modules:** activation and module-specific panels; some changes require a restart.
- **Profiles:** via *Select Profile...* / command-line parameter, separate configurations with their own database are managed — the profile is asked on first start.
- **Advanced settings:** additional key-value options outside the visible dialogs (Advanced Settings), including regex filters, excluded directories, format conversions.

## Examples

- Change the filename convention: Settings → Movies → File Naming → choose schema.
- Only posters above a minimum size: Settings → Images → set size filter.
- Second configuration (e.g. child/parent): create a new profile and choose it at startup.

## Limitations

- The UI is English; translation resources are limited to the shipped language pack.
- An invalid `Settings.xml` leads to default settings instead of a crash; the file is then rewritten.
- Module settings belong to the module — disabled modules show no panels.
