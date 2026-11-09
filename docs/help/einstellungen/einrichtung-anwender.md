← [Back to overview](index.md)

# Settings — Setup

## Purpose

Basic configuration after installation: choose a profile, add sources, set file naming and scrapers, enable modules.

## Steps

### 1. Choose a profile

- On first start the profile selection appears (*Select Profile...*) — take the default profile or create your own.
- Per profile, settings and database are stored separately.

### 2. Set up sources

1. Open *Edit → Settings...*.
2. Under *Movies → Sources* / *TV Shows → Sources* add the directories with *Add Source* (see [Media Library — Setup](../medienbibliothek/ablauf-anwender.md)).

### 3. Define file naming and content

- Check the NFO/artwork schema per content type (default: Jellyfin schema).
- Enable the desired image types and fields (poster, fanart, extrathumbs, trailer, theme …).

### 4. Enable scrapers and modules

1. In the scraper areas per content type enable the data, image, trailer and theme scrapers and set the *Scrape Order* (see [Scrapers — Setup](../scraper/einrichtung-anwender.md)).
2. Enable generic modules (tools, Jellyfin, Trakt) and configure their panels.
3. *Apply* — on "Setup Needs Restart" restart the application.

### 5. Fill the library for the first time

- Run *Tools → Update Library* and afterwards scrape the new entries.

## Notes

- Choose the certification country (Certification, e.g. FSK/MPAA country) appropriately in the settings — it affects age ratings.
- For multi-user usage a profile per person is recommended.
