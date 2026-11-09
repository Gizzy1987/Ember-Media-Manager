← [Back to overview](index.md)

# Movies — User Walkthrough

## Prerequisites

At least one movie source is configured and the library has been scanned (see [Media Library](../medienbibliothek/ablauf-anwender.md)).

## Step-by-step guide

### 1. Scrape a movie

1. Select the movie in the *Movies* list (or several via *Mark*).
2. Context menu → *(Re)Scrape Movie* or *Edit → Custom Scraper...* to select fields/images.
3. In "Ask" mode confirm the desired search results in the selection dialog.
4. Check the result: metadata, poster/fanart and trailer are shown in the detail area.

### 2. Edit a movie

1. Select a movie → context menu *Edit Movie* (or double-click on Edit).
2. Edit the fields across the tabs (details, cast, images, streams, file information).
3. Optionally swap images, assign a trailer, choose a movie set in the *Set* field.
4. Save with *OK* — the NFO file and database are updated.

### 3. Organize movies

- *Mark*: for batch actions (scraping, renaming, export).
- *Lock*: protect against automatic changes.
- *Delete Movie*: remove the entry from the database (optionally the files) — mind the *Delete Confirm* dialog.
- *Open Containing Folder*: show the file in Explorer.

### 4. Colours in the list

- **Green**: the entry was newly added by a scan or has just been processed by a scraper. It is waiting for your review.
- **Red**: the entry is marked (*Mark*), e.g. as preparation for a batch action. Marking an entry also clears the green "new" flag.
- **Black**: the entry has been reviewed and confirmed.

*Unmark* / *Unmark All* removes the mark and also clears the green "new" flag, so the list returns to its normal state.

## Result

The movie carries complete metadata and artwork in the Jellyfin-compatible file layout and is ready for media-center export and library views.
