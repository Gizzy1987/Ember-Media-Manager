# Fork-Vergleich: nagten/Ember-MM-Newscraper

**Datum:** 09.11.2026
**Verglichener Fork:** [nagten/Ember-MM-Newscraper](https://github.com/nagten/Ember-MM-Newscraper) (15 Sterne, 15.07.2026 gepusht, `da2790c4`)
**Unser Stand:** `master` @ `4d452a32`

Die Fork-Kette lautet: `DanCooper/Ember-MM-Newscraper` (offizielles Upstream, 329 Sterne)
→ `martin-stromberg/Ember-Media-Manager` → `Gizzy1987/Ember-Media-Manager` (unser Fork).

---

## 1. Methodik

Der Fremd-Fork wurde per `git fetch` in die lokale Historie geholt (`FETCH_HEAD`) und **ohne** Checkout mit `git show FETCH_HEAD:<pfad>` gegen unseren `master` verglichen. Der Arbeitsbaum blieb unverändert.

Ergebnis: **31 Commits**, die der Fremd-Fork hat und wir nicht, sowie **66 Pakete** mit abweichender Version (nur solche, die in *beiden* Bäumen vorkommen).

Der wichtigste methodische Punkt: Der Fremd-Fork liest IMDB per **HTML-Scraping** (`__NEXT_DATA__`), wir per **GraphQL-API** (`clsScrapeIMDBApi.vb`). Die 12 `clsIMDBJson.vb`-basierten Commits sind deshalb strukturell nicht portierbar — der Vergleich musste am *Verhalten* erfolgen, nicht am Patch.

Alle Aussagen wurden am Code und, wo möglich, **live gegen die IMDb-GraphQL-API** geprüft.

---

## 2. Übernommene Fixes

### 2.1 IMDB: Kategorie „distributors" → „distribution" (HOCH)

`Addons/scraper.IMDB.Data/Scraper/clsScrapeIMDB.vb:359`

```vb
' vorher
If strCategory = "production" OrElse (_SpecialSettings.StudiowithDistributors AndAlso strCategory = "distributors") Then
' nachher
If strCategory = "production" OrElse (_SpecialSettings.StudiowithDistributors AndAlso strCategory = "distribution") Then
```

Die GraphQL-API liefert für Verleiher die Kategorie-ID `distribution`, nicht `distributors`. Live verifiziert:

```json
{ "category": { "id": "distribution", "text": "Distributors" } }
```

**Auswirkung vorher:** Die Option *„Include Distributors"* (`IMDB_Data.vb` → `_SpecialSettings_Movie.StudiowithDistributors`) war **stillschweigend wirkungslos** — der Nutzer aktiviert sie im UI, es passiert nichts.

### 2.2 IMDB: Laufzeit als Minuten statt „H:MM" (HOCH)

`clsScrapeIMDB.vb:279` (Film), `:508` (Folge), `:733` (Serie)

```vb
' vorher
nMovie.Runtime = String.Format("{0}:{1:00}", lSeconds \ 3600, (lSeconds Mod 3600) \ 60)
' nachher
nMovie.Runtime = CStr(Math.Round(lSeconds / 60, 0))
```

Beleg für die Korrektheit:

- Die Projektkonvention ist Minuten: `clsAPISettings.vb:382` `MovieScraperDurationRuntimeFormat = "<m>"` (und `:622` für TV).
- Alle anderen Scraper liefern Minuten: TMDb `clsScrapeTMDB.vb:504` (`Result.Runtime`), TVDB `clsScrapeTVDB.vb:349` (`Series.Runtime`).
- Der NFO-Writer reformatiert **nicht**: `clsAPINFO.vb:334-337` schreibt `scrapedmovie.Runtime` unverändert in `<runtime>`. Das `FormatDuration` aus `clsAPIMediaInfo.vb` gilt nur für die **MediaInfo**-Dauer, nicht für die Scraper-Laufzeit.

**Auswirkung vorher:** Ein per IMDB gescrapter Film landete als `<runtime>2:22</runtime>` in der NFO statt `<runtime>142</runtime>`. Jellyfin/Kodi erwarten Minuten ⇒ falscher Laufzeitwert in der Bibliothek. Live verifiziert: `tt0111161` → `runtime.seconds = 8520` ⇒ 142 Minuten.

### 2.3 IMDB: Tagline für Serien (MITTEL)

`clsScrapeIMDB.vb`, `GetTVShowInfo`

Die GraphQL-Query `SHOW_FIELDS` (`clsScrapeIMDBApi.vb:78`) fragt `taglines(first: 1)` **bereits ab** — der Wert wurde nur nie verwendet. Der Movie-Pfad (`clsScrapeIMDB.vb:322-330`) macht es vor, die Serie nicht.

Fix: analoge Verarbeitung in `GetTVShowInfo`. Live verifiziert (`tt0903747`): `taglines[0].text` wird geliefert.

### 2.4 IMDB: Zertifizierung und MPAA aus der GraphQL-API (MITTEL)

`clsScrapeIMDBApi.vb` (Query) + `clsScrapeIMDB.vb` (`GetMovieInfo`/`GetTVShowInfo`)

Unsere Annahme im Code war:

```vb
'the GraphQL API does not provide certifications, they are scraped by the other scrapers
```

**Diese Annahme war falsch.** Die API liefert ein `certificate`-Feld — live verifiziert:

```json
{ "rating": "TV-MA",
  "country": { "id": "US", "text": "United States" },
  "attributes": [ { "text": "original rating" } ] }
```

Umsetzung: `certificate` in `TITLE_FIELDS` und `SHOW_FIELDS` ergänzt, neue Helfer `GetCertificateRating` und `ParseCertificate`. Format nach der Konvention der anderen Scraper: `Land:FSK` (z.B. `United States:R`); `MPAA` erhält das Kürzel (`R`, `TV-MA`).

Live verifiziert mit den **echten** Query-Konstanten aus der Quelle:
- `tt0111161`: `certificate.rating = R`, `certificate.country = United States` ⇒ `United States:R`
- `tt0903747`: `certificate.rating = TV-MA` ⇒ `United States:TV-MA`

### 2.5 IMDB: „Get Studio" lief ins Leere (HOCH)

`clsScrapeIMDB.vb:875` (`GetMovieStudios`), aufgerufen aus `IMDB_Data.vb:423`

Die Funktion lud die HTML-Seite `http://www.imdb.com/title/<id>/reference` per `HtmlWeb`. **Live verifiziert: HTTP 202 mit leerem Body** ⇒ die Funktion lieferte immer eine leere Liste. Das Modul *„Get Movie Studio"* war damit wirkungslos.

Fix: Umstellung auf die GraphQL-Quelle `companyCredits` (dieselbe, die `GetMovieInfo` nutzt), inklusive der Distributoren-Kategorie aus 2.1. Der verwaiste HTML-Parser `ParseStudios` wurde entfernt.

---

## 3. Übernommene Paket-Updates

| Paket | vorher | nachher | Begründung |
|---|---|---|---|
| `TMDbLib` | 1.9.1 | **2.3.0** | Bringt die TMDb-API-Kompatibilität der 2024er-Änderungen. **Compile-Probe: alle drei TMDb-Projekte kompilieren ohne jede Code-Änderung (0 Fehler).** Das NULL-Verhalten der Sammlungen (`Videos`, `ExternalIds`, …) ist in 1.9.1 und 2.3.0 **identisch** — die im Code-Review eingebauten Null-Guards bleiben also nötig und sind kein Regressionsrisiko. |
| `Newtonsoft.Json` | 13.0.3 | **13.0.4** | Letzte 13.x-Version; reiner Patch-Sprung. |
| `HtmlAgilityPack` | 1.11.42 | **1.12.4** | HTML-Parser-Fixes für IMDB, Davestrailerpage und VideobusterDE. |
| `System.Data.SQLite` | 1.0.115.5 | 1.0.119.0 | **Bewusst nicht übernommen** — unsere Version ist wegen des Native-Deployments (`SQLite.Interop.dll`) bewusst so gewählt; ein Paketwechsel müsste das Deployment erneut validiert werden. |

---

## 4. Übernommene Binaries

| Datei | vorher | nachher |
|---|---|---|
| `EmberAPI/x64/ffmpeg.exe` | 3.1 (2016, 39,1 MB) | **8.1 (2026, 101,1 MB)** |
| `EmberAPI/x64/MediaInfo.dll` | 17.12 (2013, 5,9 MB) | **26.01 (8,4 MB)** |
| `EmberAPI/x86/MediaInfo.dll` | 17.12 (4,6 MB) | **26.01 (7,5 MB)** |

Warum das wichtig ist:

- Der alte ffmpeg hatte **keinen AV1-Decoder** (die Konfiguration enthielt weder `--enable-libaom` noch `--enable-libdav1d`). Für AV1-Dateien konnte Ember **keine Thumbnails** erzeugen. Der neue Build bringt `libaom-av1`, `av1`, `av1_cuvid` und `av1_qsv` mit (verifiziert über `ffmpeg -decoders`).
- MediaInfo 17.12 stammt von 2013 und erkennt moderne Streams (HEVC Main10, AV1, Atmos, DTS:X) nur ungenau. 26.01 ist der Stand von 2026.
- Die von Ember genutzten ffmpeg-Aufrufe (`-ss/-i/-frames:v/-y` für Thumbnails, `-vf cropdetect -f null NUL`, `Duration:`-Parsing) sind versionsstabil. Das einzige in ffmpeg 6+ entfernte Flag (`-vsync`) steht in unserem Code nur in einem **auskommentierten** Block.

**Offene Lücke (ohne Belang):** `EmberAPI/x86/ffmpeg.exe` bleibt auf dem 2016er-Build. Der Fremd-Fork hat ebenfalls kein aktualisiertes x86-Binär, und offizielle win32-Builds von ffmpeg 8.1 sind nicht verfügbar (gyan.dev liefert nur win64, BtbN hat keine win32-Assets im aktuellen Release). Da das Projekt **ohnehin auf x64 ausgerichtet** wird, ist dieser Rest unkritisch — das x86-Build-Ziel sollte ohnehin entfallen.

---

## 5. NLog: interne Fehler jetzt sichtbar

`EmberAPI/NLog.config`

```xml
<nlog ... internalLogLevel="Error" internalLogFile="${basedir}/Log/nlog_errors.log">
```

NLog-interne Fehler (z. B. ein fehlerhaftes Target) landeten bisher ausschließlich im Debug-Output und waren für den Anwender unsichtbar. Die Optionen gibt es seit **NLog 4.3**, sind also unabhängig von der NLog-Version einsetzbar.

**Bewusst nicht übernommen:** der Versionssprung NLog 4.7.14 → 6.1.2 des Fremd-Forks. Zwei Major-Versionen mit Verhaltensänderungen, und wir sind mit 4.7.14 bereits deutlich neuer als der Fremd-Fork (4.2.3).

---

## 6. Bewusst NICHT übernommen

| Element | Begründung |
|---|---|
| `32ebc9a9`, `d3def01d` („Refresh Metadata"-Feature) | Existiert auch im Fork-HEAD nicht mehr (durch `1e926603` wieder entfernt). Zusätzlich ist unser `frmMain.vb` Jellyfin-umgebaut. |
| `18d4dcb4` (Selenium-Webdriver) | Wir nutzen die GraphQL-API statt HTML-Scraping — kein Browser nötig. |
| `39ca84b6` (Browser-Erkennung per Registry) | Ohne Selenium sinnlos. |
| `c76bc30e` (`.Wait()`/`.Result`-Antipattern) | Unser IMDB-Modul hat kein solches Muster (`git grep -E '\.Wait\(\)'` → leer). |
| `189dff8b`, `96a44ea4`, `1c113ed8`, `36521967`, `dfe955bd`, `2418e362`, `823b3d06`, `065f9879`, `3201a579`, `e86a9e92`, `1e926603` | Alle `__NEXT_DATA__`/HTML-basiert. Die Dateien `clsIMDBJson.vb` und `clsIMDBSearchResults.vb` existieren bei uns nicht. |
| `189dff8b` (CreditCategory-IDs) | **Unser Parsing ist nicht kaputt.** Wir vergleichen `category.id` aus der GraphQL-API mit `actor`/`actress`/`self`/`director`/`writer`/`creator`. Live verifiziert: `credits` liefert `{actor, actress}`, `principalCredits` liefert `{actor, actress, director, writer}`. Unsere Vergleiche sind korrekt. |
| `695e90ac` (Top250 nur bei Rank ≤ 250) | Wir parsen Top250 gar nicht. Die GraphQL-API kennt kein `topRanking`-Feld (live getestet: HTTP 400). Fehlendes Feature, kein Fix. |
| `cd4d48cf`, `6f4d3deb` (net48-Teil) | Wir sind bereits `net48`. |
| `09132cba` / `6f4d3deb` KodiAPI-Teile | Kodi wurde bei uns entfernt. |
| `6f4d3deb` (Löschen von `dlgTVChangeEp`) | **Kein Duplikat bei uns.** `Addons/scraper.TVDB.Poster/TVScraper/dlgTVChangeEp.vb` wird von `clsScrapeTVDB.vb:256` genutzt, `EmberMediaManager/dlgTVChangeEp.vb` von `frmMain.vb:2623`. Zwei verschiedene Klassen in zwei Assemblies, beide aktiv — ein Löschen hätte den Build gebrochen. |
| NLog 6.1.2, TMDbLib „API-Bruch"-Einschätzung | TMDbLib 2.3.0 kompiliert bei uns **ohne** Code-Änderung (Compile-Probe). Die Einschätzung „großer API-Bruch" trifft für unseren Code nicht zu. |

---

## 7. Weitere Beobachtungen ohne Handlungsbedarf

- **`GetMovieStudios` war der einzige HTML-überlebende Pfad** im IMDB-Modul — nach der Umstellung bleibt das Modul vollständig auf der GraphQL-API.
- **Top250 fehlt komplett** (weder im Code noch in der GraphQL-API). Falls gewünscht, wäre TMDb die Quelle — eigenes Ticket.
- **`nTVShow.Directors` wird nie gesetzt**, die Property existiert aber (`clsAPIMediaContainers.vb`). Die GraphQL-API liefert Directors für Serien nicht in `principalCredits` — Datenquelle müsste geklärt werden.
- **`GetSearchMovieInfo` / `SearchMovie`** nutzen die Suggestion-API (`v3.sg.media-imdb.com`), nicht das `/find/`-HTML des Forks — nicht betroffen.

---

## 8. Reproduzierbarkeit

Die Prüfskripte liegen unter `.review-bench/`:

| Datei | Zweck |
|---|---|
| `pkg-diff.ps1` | Paketversionen beider Bäume gegenüberstellen |
| `test-tmdblib2.ps1` | TMDbLib-2.3.0-Compile-Probe |
| `test-tmdblib-behaviour2.ps1` | NULL-Verhalten der TMDbLib-Sammlungen vergleichen |
| `test-imdb-query.ps1` | Die echten Query-Konstanten live gegen die IMDb-API testen |
| `imdb-cert.ps1` | GraphQL-Struktur des `certificate`-Feldes ermitteln |
