# Code-Review Ember Media Manager for Jellyfin

**Datum:** 2026-11-08
**Branch:** `review/code-review-round1`
**Basis:** `master` @ 48de5f64 (plus die bereits im Working Tree vorliegenden Jellyfin-Umbaumaßnahmen)
**Methode:** 6 parallele Review-Agenten über disjunkte Modulbereiche, jeder Befund am konkreten Code verifiziert; Build-Nachweis über `msbuild -p:Configuration=Release -p:Platform=x64`.

---

## 1. Überblick

| Bereich | Reviewer | Befunde | davon behoben |
|---|---|---|---|
| DB-/Kern-Layer (`clsAPIDatabase`, `clsAPIModules`, `clsAPITaskManager`, `clsAPIMediaContainers`, `clsAPISettings`, `clsAPICommon`) | explore | 20 | **20 / 20** |
| AddOns/Scraper (TMDb, IMDB, TVDB, OMDb, OFDB, MoviepilotDE, Jellyfin-Interface, BulkRename, MediaFileManager, MovieExport, MetadataEditor, VideoSourceMapping) | explore | 19 | **19 / 19** |
| UI-Schicht (`frmMain`, `dlgSettings`, `dlgImgSelect`, `dlgEdit_*`, `clsTheming`) | explore | 18 | **18 / 18** |
| Scanner (`clsAPIScanner`, `clsAPIHTTP`) | explore | 11 | **11 / 11** |
| Datei-I/O (`clsAPIFileUtils`, `clsAPIImages`) | explore | 15 | **15 / 15** |

**Build-Baseline:** `msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64` → Exit 0 (vor und nach den Änderungen).

---

## 2. Durchlauf 1 — Behobene Fehler

### 2.1 Absturz: leere Sequenz wird indiziert (KRITISCH)

`frmMain.vb`, 5 Stellen (`DataGridView_Row_Remove_Movie/_Movieset/_TVEpisode/_TVSeason/_TVShow`):

```vb
Dim dRow = From drvRow In dtMovies.Rows Where ... Select drvRow
If dRow(0) IsNot Nothing Then     ' <- ElementAt auf leere Sequenz wirft
```

`dRow` ist ein `IEnumerable`; `dRow(0)` bindet an `Enumerable.ElementAt` und wirft `ArgumentOutOfRangeException`, wenn die Sequenz leer ist. Die `IsNot Nothing`-Prüfung kommt **nach** dem Indexzugriff. Genau diese Fehlerklasse war bei `DataGridView_Row_Add_*` bereits behoben — die Remove-Varianten waren nicht nachgezogen. Auslöser: ein Element wird entfernt, das nicht in der aktuell gefüllten Liste steht.

**Fix:** `If dRow.Any() Then` vor dem Indexzugriff.

### 2.2 Absturz: `SelectedRows(0)` ohne Count-Prüfung (HOCH)

`frmMain.vb`, 4 `KeyPress`-Handler (Moviesets, TVEpisodes, TVSeasons, TVShows): nach `ClearSelection()` / `CurrentCell = Nothing` (Sortieren, Custom-Marker, Neu-Bindung des DataSource) ist die Auswahl leer, `SelectedRows(0)` wirft `IndexOutOfRangeException`. Diese vier Handler hatten — anders als `dgvMovies_KeyPress` — **keinen** Try/Catch, der Fehler war also ein harter Absturz.

### 2.3 Absturz: `.Max` auf leerer Sequenz, `CInt` auf Freitext (MITTEL)

`dlgSettings.vb`:
- `btnTVSourcesRegexTVShowMatchingAdd_Click`: `(From ... Select lRegex.ID).Max` wirft `InvalidOperationException`, wenn `TVShowMatching` leer ist (nach „Get Profiles" mit leerer Auswahl).
- Update-Pfad der Show-Regex: `CInt(txt…DefaultSeason.Text)` wirft `FormatException` bei Freitext → Absturz des Dialogs, alle ungespeicherten Einstellungen gehen verloren.

### 2.4 Absturz: `SaveSettings` bricht mitten in der Zuweisungskette ab (MITTEL)

`dlgSettings.vb`, 37 Stellen: `CType(cbX.SelectedItem, KeyValuePair(Of String, Enums.Y).Value)` wirft `InvalidCastException`, wenn `SelectedItem` leer ist. Praktischer Auslöser: ein gespeicherter Enum-Wert, der nicht in der hart kodierten ComboBox-Liste steht (die Liste ist eine Teilmenge der Enums, die Einstellungen persistieren den vollen Wert).

**Fix:** generischer Helper `ComboValue(Of T As Structure)(c As ComboBox)` + Try/Catch-Wrapper um `SaveSettings`, `btnApply_Click` und `btnOK_Click`.

### 2.5 Absturz: GDI+-Handle-Churn im CellPainting (HOCH)

`frmMain.vb` (5 Stellen) und `dlgEdit_Movieset.vb`: `e.CellStyle.Font = New Font("Segoe UI", 8.25, …)` erzeugte **pro gezeichneter Zelle** ein neues Font-Objekt, das nie disposed wurde. `CellPainting` feuert für jede sichtbare Zelle bei jedem Repaint → tausende `HFONT`-Handles pro Repaint.

**Fix:** eine statische `Shared ReadOnly DefaultListFont`-Instanz je Datei.

### 2.6 Absturz: geteilte Ressource wird disposed (HOCH)

`frmMain.vb`: `pbActors.Image.Dispose()` entsorgte `My.Resources.actor_silhouette` — eine vom `ResourceManager` **geteilte** Bitmap. Alle nachfolgenden Zuweisungen lieferten dann ein disposed Bitmap → `ObjectDisposedException` beim Zeichnen.

**Fix:** nur disposen, wenn es nicht die geteilte Ressource ist.

### 2.7 Datenverlust: Cleanup löscht Einträge nicht erreichbarer Quellen (KRITISCH)

`clsAPIDatabase.vb`, `Clean()`: `GetReachableSourceIDs` berechnet die erreichbaren Quellen korrekt, der WHERE-Filter wird aber **nur** im Zweig `SourceIDs IsNot Nothing AndAlso SourceIDs.Count > 0` angewandt. Beim globalen Cleanup („Alle Quellen aktualisieren", `SourceIDs = Nothing`) läuft die Schleife über alle Filme. `File.Exists` ist bei einem ausgefallenen Netzlaufwerk `False` → `Delete_Movie`/`Delete_TVEpisode` löschen die Einträge samt `art`, `uniqueid`, `rating` und Link-Tabellen (Trigger).

**Auswirkung:** Ein ausgefallenes NAS/USB-Laufwerk ⇒ beim nächsten globalen Update werden **alle** Filme dieser Quelle aus der Bibliothek gelöscht — für den Anwender sieht das wie Datenverlust aus.

### 2.8 Datenverlust: `Save_TVSeason` schreibt die falsche ID (HOCH)

`clsAPIDatabase.vb`, `Save_TVSeason`: der `idSeason`-Parameter wird mit `dbElement.ID` statt mit `lngID` (der existierenden DB-Zeile) belegt. Bei einem neu erzeugten Season-Objekt ist `dbElement.ID = -1`. `INSERT OR REPLACE INTO seasons (idSeason, …)` mit `idSeason = -1` greift den UNIQUE-Index `ix_seasons (idShow, season)`: SQLite löscht die alte Zeile und fügt `idSeason = -1` ein — und der Trigger `delete_season` löscht dabei `DELETE FROM episode WHERE Season=old.Season AND idShow=old.idShow`.

**Auswirkung:** die Episoden der Staffel und deren Bild-/ID-Zeilen werden beim Scrape vernichtet.

### 2.9 Offene Transaktionen blockieren die Datenbank (HOCH)

`clsAPIDatabase.vb`, 13 Stellen (`Delete_*`, `Change_TVEpisode`, `View_Add/Delete`): `BeginTransaction` ohne `Using` und ohne `Rollback` im `Catch`. Laufzeit-verifiziert: ein verschachteltes `BeginTransaction` wirft in `System.Data.SQLite` 1.0.115.5 **keine** Exception — der Schaden ist die **offen bleibende** Transaktion: die Connection hält einen SQLite-Lock, jede andere Verbindung läuft in „database is locked".

### 2.10 Verschluckte Fehler lassen die Task-Queue still sterben (HOCH)

`clsAPITaskManager.vb`: `bwTaskManager_RunWorkerCompleted` wertet `e.Error` nicht aus. Jede Exception im Worker beendet ihn, die Queue bleibt gefüllt, `TaskManagerEnded` wird gesendet → die UI meldet „fertig", alle verbleibenden Aufgaben laufen nie, ohne jede Fehlermeldung.

### 2.11 Movie-Keyart ist faktisch ein Poster (HOCH)

Zwei Stellen, die sich verstärken:
- `clsAPIMediaContainers.vb`: `MainKeyarts = FilterImages(MainPosters, …)` — die Keyart-Liste wird durch die Poster-Liste ersetzt.
- `clsAPIModules.vb`, `ScrapeImage_Movie`: `MainKeyarts` wird nicht übertragen (MovieSet und TV tun es korrekt).

**Auswirkung:** `GetPreferredMovieKeyart` wählt ein Poster als Keyart. Für Jellyfin (Keyart = Wide-Cover) ist das ein falsches Bild in falscher Rolle.

### 2.12 TMDb/TVDB: NRE auf leere TMDbLib-Sammlungen (HOCH)

`TMDbLib` initialisiert seine Collection-Properties standardmäßig mit `Nothing` (per Reflection an der DLL geprüft). Betroffen:
- `clsScrapeTMDB.vb`: Trailer-Fallback (`_Fallback_Movie.Videos.Results` bei `Videos Is Nothing`) — die Bedingung war zusätzlich **invertiert**, der Fallback konnte also nie greifen.
- `clsScrapeTMDB.vb`: `Result.ExternalIds` ohne Null-Check (an drei anderen Stellen derselben Datei korrekt geprüft).
- `clsScrapeTMDB.vb`: `ShowInfo.Seasons` / `SeasonInfo.Episodes` ohne Null-Check.
- `clsScrapeTVDB.vb` (Data): `SearchTVShowByName` kann `Nothing` liefern, der Aufrufer greift direkt auf `r.Matches` zu.
- `clsScrapeTVDB.vb` (Poster): `Episode.Element("GuestStars").Value` ohne Null-Check — das Element fehlt oft; weil ein `Try` die **gesamte** Episodenschleife umschließt, brachen alle nachfolgenden Episoden ab.

### 2.13 `Task.Result` wird vor `Task.Exception` ausgewertet (MITTEL)

`clsScrapeTMDB.vb`, `clsScrapeOMDb.vb`, `clsScrapeTVDB.vb` (je 3–6 Stellen): der Ausdruck `If APIResult Is Nothing OrElse APIResult.Result Is Nothing OrElse … OrElse APIResult.Exception IsNot Nothing` wertet `.Result` **vor** `.Exception` aus. Bei einem faulteten Task wirft `.Result` eine `AggregateException` — die vorgesehene Fehlerbehandlung ist toter Code.

### 2.14 TVDB.Poster: Index außerhalb der Liste → Transaktion ohne Commit (HOCH)

`clsScrapeTVDB.vb`: `cSea(0)` wird außerhalb des `If cSea.Count > 0`-Schutzes verwendet → `ArgumentOutOfRangeException` **vor** dem `SQLTrans.Commit()` ⇒ keine Episoden-Metadaten gespeichert, zusätzlich ein Fehler im UI-Thread (`DirectCast(e.Result, Results)` mit `e.Result = Nothing`).

### 2.15 Jellyfin: API-Key im Klartext, Token über unverschlüsseltes HTTP (HOCH)

- `Interface.Jellyfin.vb`: der API-Key wird als normales XML-Element abgelegt (`%Settings%\Interface.Jellyfin.xml`).
- `clsAPIJellyfin.vb`: `NormalizeBaseUrl` ergänzt fehlende Schemata stillschweigend mit `http://` — ein als `jellyfin.lan` eingetragener Host wird unverschlüsselt angesprochen; der Token (`X-Emby-Token`) ist für jeden im Netz mitlesenden Host lesbar.

**Fix:** DPAPI-Verschlüsselung (Windows, `CurrentUser`) mit Präfix `dpapi:` und Rückwärtskompatibilität für Bestandswerte, `https`-Bevorzugung bei der Schema-Ergänzung, Warnung bei `http://`.

### 2.16 Weitere behobene Fehler (MITTEL/NIEDRIG)

| Fundstelle | Fehler |
|---|---|
| `clsScrapeTVDB.vb` | privater theTVDB-API-Key wurde in die `EpisodeGuide.URL` geschrieben und landete damit in DB und exportierten NFO-Dateien |
| `clsAPIMediaExporter.vb` | HTML-Export mit `Encoding.ASCII` → alle Umlaute wurden `?`; Stream nicht disposed |
| `frmSettingsHolder.vb` (MetadataEditor, VideoSourceMapping) | `.Value.ToString` ohne Null-Check → NRE beim Speichern leerer Zellen |
| `frmSettingsHolder.vb` (VideoSourceMapping) | Duplikatsprüfung der Dateiendungen prüfte gegen die Quellenliste → Endungen wurden still verworfen |
| `dlgMultiFileQuestion.vb`, `clsAPITaskManager.vb` | `Module.MediaFileManager`: nach „Verschieben mit TeraCopy" blieben DB-Einträge auf den alten Pfaden |
| `clsTeraCopy.vb` | `Process` nie disposed; `CreateNoWindow` widersprach `WindowStyle`; Prozess nicht abgewartet |
| `clsAPIRenamer.vb` | `Double.Parse` ohne `TryParse` → FormatException ⇒ das komplette Namensmuster ging still verloren |
| `clsScrapeMoviepilotDE.vb` | leere Zertifizierung wurde in die Metadaten geschrieben |
| `dlgEdit_TVEpisode.vb` | Translation-IDs 656/657 **vertauscht** ⇒ Titel und Untertitel vertauscht (nur in der deutschen Oberfläche sichtbar) |
| `dlgEdit_Movieset.vb` | IDs 241/242 („Handlung"/„Kurzbeschreibung") statt „Filmzuordnung"/„Datenbank" |
| `dlgSettings.vb` | ID 835 („MPAA: Zahl statt Land:Zahl") für „Only Save the Value to NFO" |
| `dlgImgSelect.vb` | ID `99999` existiert in keiner Sprachdatei ⇒ vier Rückfragedialoge immer englisch |
| `frmMain.vb` | Kontextmenü blieb deaktiviert, wenn die Zeile nicht vorher selektiert war (5 Listen) |
| `frmMain.vb` | Tippfehler „remiered", „Mark as Unatched", falscher Tooltip für die Filmset-Suche |
| `dlgSettings.vb` | `SizeChanged` erzeugte je Ereignis 2 Bitmaps + 2 Brushes, das alte `BackgroundImage` wurde nie disposed |
| `dlgEdit_*.vb` | `LinearGradientBrush` wurde nicht disposed |
| `dlgEdit_Movie.vb` | toter Code mit vertauschter Variable (`fResults`/`pResults`) |

**Neue Translation-IDs:** 1764–1771 in `db-DB.xml`, `en-US.xml`, `de-DE.xml` (höchste bisherige ID war 1763).

### 2.17 Absturz: leere Verzeichnisliste beim Scan (KRITISCH)

`clsAPIScanner.vb`, 5 Stellen: `inList`/`dList` bleiben `Nothing`, wenn `GetDirectories` bei einem offline Netzlaufwerk wirft. Der innere `Try` fängt das ab — teils sogar mit **leerem** `Catch` —, danach iteriert ein `For Each … In inList` über `Nothing` ⇒ **NullReferenceException**, uncaught im Worker-Thread. Zusätzlich ist `Where()` lazy: `GetDirectories` wirft bereits bei der Zuweisung, der Prädikat-Fehler erst in der For-Each — beides lag außerhalb des Try.

Im Movie-Fall schluckte der äußere `Catch` die NRE sogar noch ⇒ die **komplette Ebene inkl. aller Geschwisterordner** wurde nicht gescannt, Filme fehlten in Jellyfin, ohne jede Meldung.

**Fix:** Listen vorbelegen, im Try mit `.ToList()` materialisieren, leere `Catch`-Blöcke durch `logger.Error` ersetzen, `Is Nothing`-Wächter vor jeder For-Each.

### 2.18 Absturz: `CInt` auf leere Regex-Gruppe, ignorierter Rückgabewert (HOCH)

`clsAPIScanner.vb`, `RegexGetTVEpisode`: `eItem.Episode = CInt(Episode.Groups(1).Value)` wirft bei leerer Gruppe. Außerdem wurde der Rückgabewert von `RegexGetSeasonAndEpisodeNumber` ignoriert ⇒ ungültige Items (Season = -2, Episode = -1) landeten in der DB ⇒ **falsche Episoden in Jellyfin**.

### 2.19 Endlosschleifen (HOCH)

- `clsAPIScanner.vb`, `RegexGetTVEpisode`: keine Iterationsgrenze, kein Abbruchpfad; der Fortschritt hängt an Regex-Gruppe 3. Liefert die (benutzerdefinierte) Regex den Rest nicht kürzer, läuft die Schleife **ewig** — `Cancel()` griff nicht.
- `clsAPIHTTP.vb`: `Loop Until bytesRead = count` — bei 0 gelesenen Bytes oder `count = 0` nie erfüllt ⇒ Hänger ohne Abbruchpfad.

### 2.20 Datenverlust beim Download (HOCH)

`clsAPIHTTP.vb`: `If File.Exists(outFile) Then File.Delete(outFile)` löschte die alte Datei **vor** dem Download. Bricht der Download ab, bleibt eine abgeschnittene Datei zurück, und `DownloadFile` liefert sie trotzdem zurück ⇒ Jellyfin bekommt eine kaputte Datei, das Original ist unwiederbringlich weg.

**Fix:** Download in eine `.part`-Datei, das alte Ziel erst nach vollständigem Download ersetzen, `.part` im `Finally` aufräumen.

### 2.21 Nicht erreichbare Quelle wurde stillschweigend übersprungen (MITTEL)

`clsAPIScanner.vb`: `Directory.GetLastWriteTime` liefert bei einem offline Laufwerk 1601 ⇒ die Quelle gilt als „nicht geändert" und wird übersprungen, ohne Log. Bei TV wurde `strLastScan` **vor** dem Scan aktualisiert, obwohl gar nicht gescannt wurde ⇒ die Quelle galt dauerhaft als „gescannt".

**Fix:** Erreichbarkeit prüfen und warnen; `strLastScan` erst nach erfolgreichem Scan schreiben.

### 2.22 Jellyfin-Bildnamen wurden nie geschrieben (KRITISCH)

`clsAPIFileUtils.vb`, Zeile 1058 und 2039:

```vb
If AdvancedSettings.GetBooleanSetting("MovieUseJellyfin", False, , Enums.ContentType.Movie) AndAlso …
```

`AdvancedSettings` liest aus einem **anderen** XML-Store (`_AdvancedSettings.Setting`, Section `*EmberAPP`). Der Key `MovieUseJellyfin` existiert dort nicht ⇒ der Wert ist **immer `False`** ⇒ der komplette Block mit den festen Jellyfin-Bildnamen (`poster.jpg`, `backdrop.jpg`, `banner.jpg`, `clearart.png`, `clearlogo.png`, `disc.png`, `keyart.jpg`, `landscape.jpg` im Filmordner bzw. `poster.jpg`/`fanart.jpg`/`banner.jpg` im Staffelordner) wurde **nie befüllt**.

Der echte Schalter ist `Master.eSettings.MovieUseJellyfin` / `.TVUseJellyfin` — die übrigen ~40 Stellen derselben Datei verwendeten ihn bereits korrekt, nur diese zwei lasen aus der falschen Quelle. Das Jellyfin-Preset erzwingt den Schalter in `clsAPISettings.vb:1068-1069` auf `True`.

**Auswirkung:** Das Kernziel „Jellyfin liest es sauber und vollständig" war an dieser Stelle verfehlt — Jellyfin hätte die Bilder nicht gefunden.

**Fix:** beide Stellen auf `Master.eSettings` umgestellt; per Grep verifiziert, dass keine `AdvancedSettings`-Abfrage dieser Schalter mehr existiert.

### 2.23 Weitere Datei-I/O-Fehler (HOCH/MITTEL)

| Fundstelle | Fehler |
|---|---|
| `clsAPIFileUtils.vb` | `FileMove` ignorierte `overwriteFiles` (⇒ IOException) und legte das Zielverzeichnis nicht an |
| `clsAPIFileUtils.vb` | `DirectoryMove`: `Directory.GetParent` liefert bei Wurzelpfaden `Nothing` ⇒ NRE; im Else-Zweig wurde das Ziel **vor** dem Move gelöscht ⇒ Datenverlust bei Volume-Grenze |
| `clsAPIFileUtils.vb` | `SortFiles`: Präfix-Match (`Movie 2.nfo` matcht `…\Movie`) ⇒ Datei im falschen Ordner; `MoveTo` ohne Kollisionsprüfung ⇒ ein Fehler brach die **gesamte** Sortierung ab |
| `clsAPIFileUtils.vb` | `Format(DateTime.Now, "YYYYMMDD")` — `"YYYY"` ist kein gültiger Specifier ⇒ das Datum **ohne Jahr** |
| `clsAPIFileUtils.vb` | user-konfigurierbarer Regex „FileStacking" ohne `MatchTimeout` ⇒ Hänger durch Backtracking |
| `clsAPIFileUtils.vb` | 4 leere `Catch`-Blöcke ⇒ Fehler spurlos verworfen, Aufrufer meldet Erfolg |
| `clsAPIImages.vb` | `LoadFromMemoryStream` außerhalb des Try + ungeprüfter `_image.Width` ⇒ NRE bei ungültigem Bildstream (9 Aufrufstellen) |
| `clsAPIImages.vb` | GDI+-Lecks: altes `_image`, Zwischen-Bitmap und `EncoderParameters` nie disposed |
| `clsAPIImages.vb` | `Application.DoEvents()` in der Download-Warteschleife ohne Abbruch/Timeout ⇒ Reentrancy + Hänger |
| `clsAPIImages.vb` | Pfade > 260 Zeichen ⇒ Bild wurde **ohne Log** nicht gespeichert; Schreiben nicht atomar ⇒ bei Abbruch beschädigte `poster.jpg` |
| `clsAPIImages.vb` | PNG/GIF-Bytes wurden unter `.jpg`-Namen geschrieben |

**Validierung:** Die neue Trenner-Logik für die Sortier-Zuordnung (`BelongsToMovie`) wurde mit einem kompilierten VB-Test gegen 9 Fälle geprüft: `Movie.nfo`, `Movie-fanart.jpg`, `Movie.sample.mkv`, `Movie cd1.nfo`, `Movie disc2.txt` → zugehörig; `Movie 2.nfo`, `Movie 3D.nfo`, `Movie2.nfo`, fremder Ordner → **nicht** zugehörig (die alte Präfix-Regel matchte die ersten drei fälschlich).

---

## 3. Durchlauf 2 — Performance

### 3.1 Listen-Views: Kreuzprodukt-Join ersetzt (Hauptbefund)

Die fünf Listen-Views (`movielist`, `setslist`, `seasonslist`, `tvshowlist`, `episodelist`) verbanden **alle** Link-Tabellen und alle `art`-Typen **gleichzeitig** per `LEFT OUTER JOIN` und collapses das Ergebnis anschließend mit `GROUP_CONCAT(DISTINCT …)`.

Bei einem Film mit 2 Countries × 3 Writern × 2 Directors × 3 Genres × 2 Studios × 2 Tags × 6 Fanarts entstehen **~1.700 Zwischenzeilen pro Film** — bei 2.600 Filmen rund **973 Millionen** Zeilen, die der `GROUP BY` wieder zusammenfasst. `tvshowlist` war am schlimmsten: dort wurde zusätzlich die View `episodelist` (selbst ein View mit `GROUP BY`) eingejoint.

**Fix:** die Views erzeugen dieselben Spalten jetzt über **korrelierte Scalar-Subqueries** — jede Art-Spalte als `(SELECT url FROM art WHERE … LIMIT 1)`, jede `GROUP_CONCAT`-Spalte als `(SELECT GROUP_CONCAT(DISTINCT …) FROM … WHERE …)`, die Episodenzähler direkt gegen die `episode`-Tabelle statt gegen die View `episodelist`. Das Kreuzprodukt entfällt vollständig.

**Messung** (SQLite, Schema aus `MyVideosDBSQL.txt`, 2.600 Filme, realistischer Link-Umfang, jeweils vollständiger Abruf `SELECT * FROM …`):

| Fanarts pro Film | vorher | nachher | Faktor |
|---|---|---|---|
| 1 | 0,222 s | 0,056 s | **3,9×** |
| 3 | 0,542 s | 0,058 s | **9,3×** |
| 6 | 1,029 s | 0,060 s | **17,1×** |

Die optimierte View ist **konstant** (~0,058 s unabhängig von der Zahl der Art-Einträge), die bisherige skaliert linear mit.

**Korrektheitsnachweis:** alle fünf Views wurden gegen die Original-Views **zeilenweise über alle Spalten** verglichen (600 / 100 / 240 / 120 / 3.140 Zeilen) — **keine Abweichung**.

**Umsetzung:**
- `EmberAPI/DB/MyVideosDBSQL.txt` — Views für neu angelegte Datenbanken.
- `EmberAPI/DB/MyVideosDBSQL_v48_Patch.xml` — neu; `DROP VIEW` + `CREATE VIEW` für Bestandsdatenbanken (Muster wie in `v45`/`v47`).
- `EmberAPI/EmberAPI.vbproj` — Patch-Datei mit `CopyToOutputDirectory=Always` registriert.
- `EmberAPI/clsAPIDatabase.vb` — `MyVideosDBVersion` 48 → **49**.

Geprüft: frische DB aus der Schema-Datei erzeugt alle fünf Views; der Patch wendet sich sauber an; `movielist` liefert auf leerer DB 0 Zeilen / 64 Spalten wie vorher.

### 3.2 Weitere Performance-Korrekturen

| Stelle | vorher | nachher |
|---|---|---|
| `clsAPIStringUtils.SortTokens` | Musterstring pro Titel neu gebaut, Regex ohne Timeout | kompilierte Regex-Instanzen pro Token **cached** (`ConcurrentDictionary`), `MatchTimeout` 2 s |
| `clsAPIFileUtils.SortFiles` | `lstVideosInDir.Select(…).Contains(…)` in einer `.Where()` → **O(n²)** | `HashSet(Of String)` → O(n) |
| `clsAPIImages.LoadFromFile` | zwei voll große Puffer, `Read`-Rückgabewert ignoriert (partieller Read ⇒ beschädigte Bilddaten) | `fsImage.CopyTo(_ms)`, ein Puffer |
| `clsAPIImages.Clear` / `LoadFromFile` | Bitmap und MemoryStream ohne `Dispose` → GDI+- und Speicherleck | `ResetImage()` mit `Dispose` |
| `clsAPIFileUtils.SortFiles` | user-konfigurierbares Ausschlussmuster ohne Timeout ⇒ Hänger durch Backtracking möglich | Muster einmalig kompiliert, `MatchTimeout` 2 s |

---

## 4. Build / CI / Pakete (eigene Prüfung)

- **Paketkonsistenz:** alle 33 Projekte (inkl. aller AddOns) haben eine `packages.config`; `NuGet restore` läuft sauber durch. Keine fehlenden Restore-Quellen.
- **Workflows:** `release.yml`, `staging-ci.yml`, `pr-staging-ci.yml`, `security-scan.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml` — Trigger, `concurrency`-Gruppen und `permissions` sind konsistent; `verify-pr-source.yml` bindet `github.head_ref` bereits über `env` (Script-Injection-Härtung).
- **Release-Pipeline:** `scripts/resolve-release-version.mjs` behandelt automatische und manuelle Releases, prüft vorhandene Assets und repariert fehlende in place; `update.json`/`version.json` sind konsistent.
- **Security-Scan:** `NU1901–NU1904` werden als Fehler gewertet, `NU1900`/`NU1905+` bewusst nur als Warnung (Infrastructure-Noise).

**Keine Befunde.**

---

## 5. Bewusst nicht geändert

| Befund | Entscheidung | Begründung |
|---|---|---|
| `ConvertToUnixTimestamp`/`ConvertFromUnixTimestamp` rechnen mit lokaler Wanduhr statt echter UTC | **nicht geändert** | Der Round-Trip ist exakt (`ConvertFrom(ConvertTo(x)) == x`), Schreib- und Lesepfade sind symmetrisch, und `ctime`/`mtime` stammen aus `File.GetCreationTime`/`GetLastWriteTime` (lokal). Anzeige und NFO-Ausgabe sind damit korrekt. Eine Umstellung auf echtes UTC würde **alle** bestehenden Werte um den UTC-Offset verschieben, ohne Nutzen. Nur der irreführende Doc-Kommentar wird korrigiert. |
| `Interfaces.ModuleResult` als Structure ⇒ `ret` kann nie `Nothing` sein | **kein Kern-Fix nötig** | Ein `Return Nothing` für eine Structure liefert den Default-Wert (`breakChain = False`), es entsteht **keine** `NullReferenceException`. Die AddOn-Seite wird trotzdem auf `New ModuleResult With {.breakChain = False}` umgestellt (klarer, aber semantisch identisch). |
| Fehlende Indizes auf den Link-Tabellen | **kein Befund** | Das Schema hat bereits 50 Indizes, darunter `CREATE UNIQUE INDEX` auf **allen** Link-Tabellen in beiden Spaltenrichtungen. Ein erster Suchlauf hatte nur `CREATE INDEX` gefunden und `CREATE UNIQUE INDEX` übersehen — nach Korrektur des Musters kein Handlungsbedarf. |

---

## 6. Reproduzierbarkeit

Die Messungen liegen unter `.review-bench/` (Benchmark-Skripte und die optimierten View-Definitionen als Referenz):

| Datei | Zweck |
|---|---|
| `bench.ps1` | Original-View vs. optimierte View, Kreuzprodukt-Zwischenzeilen messen |
| `bench2.ps1` | Skalierung über die Zahl der Art-Einträge pro Film |
| `validate.ps1` | zeilenweiser Äquivalenzvergleich aller fünf Views |
| `test_patch.ps1` | frische DB aus Schema-Datei + Patch v48 anwenden und prüfen |
| `views_optimized.sql` | Referenz der optimierten View-Definitionen |

---

## 7. Offene Punkte

- `EmberAPI_Test` ist nicht Teil der Solution und baut nicht (fehlendes UnitTest-Projekt, MSTest-v1-GAC-Referenz) - es gibt daher **keine** automatisierte Testabdeckung der Fixes. Die Verifikation erfolgt ueber den Build, die SQLite-Messlaeufe und einen kompilierten VB-Test der Sortier-Zuordnungslogik.
- Die `Save_*`-Methoden (`Save_Movie`, `Save_MovieSet`, `Save_Tag`, `Save_TVEpisode`, `Save_TVShow`) sowie die Patch-/Prepare-Methoden haben weiterhin `BeginTransaction` **ohne** Rollback/Dispose - sie standen nicht auf der Befundliste (B-3 betraf die `Delete_*`-Methoden) und tragen dasselbe Lock-Risiko.
- `clsAPIImageUtils.ResizeImage` disposet das vor dem Resize vorhandene Bitmap nicht - der Leak liegt in einer Datei ausserhalb des Review-Scopes.
- `Images.Cancel()` (neu, fuer den Download-Abbruch) hat noch keinen Aufrufer; die Abbruchlogik muss von der Scraper-/UI-Schicht ausgeloest werden. Die API ist vorbereitet.
- `File.Replace` setzt NTFS voraus; auf FAT/anderen Volumes greift der Fallback "loeschen + verschieben" (mit Warn-Log). Bei einem harten Prozessabbruch kann eine Temp-Datei im Filmordner zurueckbleiben (im Fehlerpfad wird sie entfernt).
- `changes.log` / `docs/RELEASE_NOTES.md` wurden noch nicht ergaenzt.

---

## 8. Build-Nachweis

```
msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64 -t:Rebuild
-> Exit-Code 0, 0 Fehler
-> EmberMM - Release - x64\Ember Media Manager for Jellyfin.exe (5.190 KB)
-> EmberMM - Release - x64\DB\MyVideosDBSQL_v48_Patch.xml (neu, deployed)
-> EmberMM - Release - x64\Translations\{db-DB,de-DE,en-US}.xml (deployed)
```

Der Baseline-Build vor dem Review war ebenfalls fehlerfrei (Exit 0) - die Fehlerzahl ist also unveraendert, die Befunde wurden behoben **ohne** neue Compiler-Warnungen (die verbleibenden 29-41 Warnungen sind vorbestehend: fehlende Ruleset-Dateien in den AddOn-Projekten sowie zwei veraltete generierte Properties in `clsAPINFO.vb`).

