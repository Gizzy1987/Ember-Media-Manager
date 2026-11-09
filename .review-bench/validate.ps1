param([int]$MovieCount = 600, [int]$ShowCount = 120, [int]$EpisodesPerShow = 26)
# Validiert die optimierten Listen-Views gegen die Original-Views:
#  - Zeilenweiser Vergleich (Spaltenwerte) Original vs. optimiert
#  - Performance-Vergleich
$ErrorActionPreference = 'Stop'
$pkg = "H:\Arbeitszimmer\Ember-Media-Manager\packages\Stub.System.Data.SQLite.Core.NetFramework.1.0.115.5"
Add-Type -Path "$pkg\lib\net46\System.Data.SQLite.dll"
$DbPath = "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\validate.db"
[System.IO.Directory]::CreateDirectory((Split-Path $DbPath)) | Out-Null
if (Test-Path $DbPath) { Remove-Item $DbPath -Force }
$conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DbPath;Version=3;")
$conn.Open()
function Exec([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    [void]$cmd.ExecuteNonQuery(); $cmd.Dispose()
}
function Query([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    $da = New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    [void]$da.Fill($dt)
    $cmd.Dispose(); $da.Dispose()
    return ,$dt
}

# 1) Schema OHNE Views anwenden, dann Original-Views anlegen
$schema = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL.txt" -Raw
# Views aus dem Schema entfernen, wir legen sie selbst an
$viewStart = $schema.IndexOf('CREATE VIEW episodelist')
$schemaOnly = $schema.Substring(0, $viewStart)
$stmts = [regex]::Split($schemaOnly, ';') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' -and $_ -notmatch '^--' }
foreach ($s in $stmts) { try { Exec $s } catch { } }

$origViews = $schema.Substring($viewStart)
foreach ($s in ([regex]::Split($origViews, ';') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })) {
    Exec $s
}

# 2) Testdaten
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO movie (idMovie, idSource, MoviePath, Title, SortTitle, Type, "New", Mark, "Lock")
SELECT x, 1, 'C:\M\F' || x, 'Film ' || x, 'Film ' || x, 0, 0, 0, 0 FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 4000)
INSERT INTO actors (idActor, strActor) SELECT x, 'Schauspieler ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 18)
INSERT INTO genre (idGenre, strGenre) SELECT x, 'Genre ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 120)
INSERT INTO studio (idStudio, strStudio) SELECT x, 'Studio ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 60)
INSERT INTO country (idCountry, strCountry) SELECT x, 'Land ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 40)
INSERT INTO tag (idTag, strTag) SELECT x, 'Tag ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 20)
INSERT INTO moviesource (idSource, strName, strPath) SELECT x, 'Quelle ' || x, 'C:\Q' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 20)
INSERT INTO tvshowsource (idSource, strName, strPath) SELECT x, 'TVQuelle ' || x, 'C:\TQ' || x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 100)
INSERT INTO sets (idSet, Title, Plot) SELECT x, 'Set ' || x, 'Plot ' || x FROM seq;
COMMIT;
"@
# Links (kollisionsfrei)
foreach ($k in 1..4) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO actorlinkmovie (idActor, idMovie, strRole, iOrder) SELECT (x % 400) * 10 + $k, x, 'Actor', 0 FROM seq;
COMMIT;
"@
}
foreach ($k in 1..3) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO writerlinkmovie (idWriter, idMovie) SELECT (x % 400) * 10 + 4 + $k, x FROM seq;
COMMIT;
"@
}
foreach ($k in 1..2) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO directorlinkmovie (idDirector, idMovie) SELECT (x % 400) * 10 + 7 + $k, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO studiolinkmovie (idStudio, idMovie) SELECT ((x + $($k-1)) % 120) + 1, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO countrylinkmovie (idCountry, idMovie) SELECT ((x + $($k-1)) % 60) + 1, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO taglinks (idTag, idMedia, media_type) SELECT ((x + $($k-1)) % 40) + 1, x, 'movie' FROM seq;
COMMIT;
"@
}
foreach ($k in 0..2) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO genrelinkmovie (idGenre, idMovie) SELECT ((x + $k) % 18) + 1, x FROM seq;
COMMIT;
"@
}
# Sets <-> Movies
foreach ($k in 0..3) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO setlinkmovie (idSet, idMovie, iOrder) SELECT ((x + $k) % 100) + 1, x, $k FROM seq;
COMMIT;
"@
}
# Art: 2 Fanarts + 1 je Typ, pro Movie
foreach ($k in 0..1) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'movie', 'fanart', 'http://l/f' || x || '_$k.jpg' FROM seq;
COMMIT;
"@
}
foreach ($t in @('poster','banner','clearart','clearlogo','discart','keyart','landscape')) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'movie', '$t', 'http://l/$t' || x || '.jpg' FROM seq;
COMMIT;
"@
}
# TV-Shows, Seasons, Episodes
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO tvshow (idShow, idSource, TVShowPath, Title, SortTitle, "New", Mark, "Lock")
SELECT x, 1, 'C:\TV\S' || x, 'Serie ' || x, 'Serie ' || x, 0, 0, 0 FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO seasons (idSeason, idShow, Season, Title) SELECT x, x, 1, 'Staffel 1' FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO seasons (idSeason, idShow, Season, Title) SELECT 100000 + x, x, 2, 'Staffel 2' FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * $EpisodesPerShow))
INSERT INTO episode (idEpisode, idShow, idFile, idSource, Episode, Title, Season, "New", Mark, "Lock", Playcount)
SELECT x, ((x - 1) / $EpisodesPerShow) + 1, x + 1, 1, ((x - 1) % $EpisodesPerShow) + 1, 'Episode ' || x,
       CASE WHEN ((x - 1) % $EpisodesPerShow) < 10 THEN 1 ELSE 2 END, 0,
       CASE WHEN (x % 3) = 0 THEN 1 ELSE 0 END,
       CASE WHEN (x % 7) = 0 THEN 1 ELSE 0 END,
       CASE WHEN (x % 4) = 0 THEN 1 ELSE NULL END FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * $EpisodesPerShow))
INSERT INTO files (idFile, strFilename) SELECT x + 1, 'C:\TV\E' || x || '.mkv' FROM seq;
COMMIT;
"@
# Episoden ohne Datei (Missing) + Art/Link fuer Episoden
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 20)
INSERT INTO episode (idEpisode, idShow, idFile, idSource, Episode, Title, Season, "New", Mark, "Lock")
SELECT 900000 + x, 1, -1, 1, x, 'Fehlende ' || x, 1, 0, 0, 0 FROM seq;
COMMIT;
"@
foreach ($t in @('fanart','thumb')) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * $EpisodesPerShow))
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'episode', '$t', 'http://l/ep$t' || x || '.jpg' FROM seq;
COMMIT;
"@
}
foreach ($k in 1..2) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * $EpisodesPerShow))
INSERT INTO writerlinkepisode (idWriter, idEpisode) SELECT (x % 400) * 10 + $k, x FROM seq;
COMMIT;
"@
}
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * $EpisodesPerShow))
INSERT INTO directorlinkepisode (idDirector, idEpisode) SELECT (x % 400) * 10 + 3, x FROM seq;
COMMIT;
"@
# Show-Links + Season-Art
foreach ($k in 1..2) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO creatorlinktvshow (idActor, idShow) SELECT (x % 400) * 10 + $k, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO directorlinktvshow (idDirector, idShow) SELECT (x % 400) * 10 + 4 + $k, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO genrelinktvshow (idGenre, idShow) SELECT ((x + $($k-1)) % 18) + 1, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO studiolinktvshow (idStudio, idShow) SELECT ((x + $($k-1)) % 120) + 1, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO countrylinktvshow (idCountry, idShow) SELECT ((x + $($k-1)) % 60) + 1, x FROM seq;
COMMIT;
"@
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO taglinks (idTag, idMedia, media_type) SELECT ((x + $($k-1)) % 40) + 1, x, 'tvshow' FROM seq;
COMMIT;
"@
}
foreach ($t in @('banner','fanart','landscape','poster')) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < ($ShowCount * 2))
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'season', '$t', 'http://l/s$t' || x || '.jpg' FROM seq;
COMMIT;
"@
}
foreach ($t in @('banner','characterart','clearart','clearlogo','fanart','keyart','landscape','poster')) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $ShowCount)
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'tvshow', '$t', 'http://l/sh$t' || x || '.jpg' FROM seq;
COMMIT;
"@
}
Exec "ANALYZE;"

# 3) Original-Views: Ergebnisse einsammeln
Write-Host "Testdaten erzeugt. Vergleiche Original-Views gegen optimierte Views ..."
$views = @(
    @{ Name='movielist'; Key='idMovie' },
    @{ Name='setslist'; Key='idSet' },
    @{ Name='seasonslist'; Key='idSeason' },
    @{ Name='tvshowlist'; Key='idShow' },
    @{ Name='episodelist'; Key='idEpisode' }
)
$origData = @{}
foreach ($v in $views) {
    $dt = Query "SELECT * FROM $($v.Name);"
    $origData[$v.Name] = $dt
    Write-Host ("  Original {0,-12} : {1} Zeilen, {2} Spalten" -f $v.Name, $dt.Rows.Count, $dt.Columns.Count)
}

# 4) Optimierte Views anwenden
$opt = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\views_optimized.sql" -Raw
$opt = ($opt -split "`r?`n" | Where-Object { $_ -notmatch '^\s*--' }) -join "`n"
foreach ($s in ([regex]::Split($opt, ';') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })) {
    Exec $s
}

$fail = 0
foreach ($v in $views) {
    $dtO = $origData[$v.Name]
    $dtN = Query "SELECT * FROM $($v.Name);"
    $msg = ""
    if ($dtO.Rows.Count -ne $dtN.Rows.Count) {
        $msg += "ZEILENZAHL WEICHT AB ($($dtO.Rows.Count) vs $($dtN.Rows.Count)) "
        $fail++
    }
    # Spalten vergleichen
    $oCols = @($dtO.Columns | ForEach-Object { $_.ColumnName })
    $nCols = @($dtN.Columns | ForEach-Object { $_.ColumnName })
    $missing = $oCols | Where-Object { $nCols -notcontains $_ }
    $extra = $nCols | Where-Object { $oCols -notcontains $_ }
    if ($missing.Count -gt 0) { $msg += "FEHLENDE SPALTEN: $($missing -join ',') " }
    if ($extra.Count -gt 0) { $msg += "ZUSAETZLICHE SPALTEN: $($extra -join ',') " }
    # Werte vergleichen (zeilenweise, per Key)
    $key = $v.Key
    $oMap = @{}
    foreach ($r in $dtO.Rows) {
        $kk = [System.Convert]::ToString($r.Item($key))
        $oMap[$kk] = $r
    }
    $diffCols = @{}
    $rowDiff = 0
    foreach ($r in $dtN.Rows) {
        $k = [System.Convert]::ToString($r.Item($key))
        if (-not $oMap.ContainsKey($k)) { $rowDiff++; continue }
        $or = $oMap[$k]
        foreach ($c in $oCols) {
            if ($c -eq $key) { continue }
            $oval = $or.Item($c)
            $nval = $r.Item($c)
            $ov = if ($oval -is [DBNull]) { $null } else { [string]$oval }
            $nv = if ($nval -is [DBNull]) { $null } else { [string]$nval }
            if ($ov -ne $nv) {
                # GROUP_CONCAT-Reihenfolge kann abweichen -> als Menge vergleichen
                $same = $false
                if ($ov -and $nv) {
                    $os = ($ov -split ',') | Sort-Object
                    $ns = ($nv -split ',') | Sort-Object
                    if (($os -join '|') -eq ($ns -join '|')) { $same = $true }
                }
                if (-not $same) {
                    if (-not $diffCols.ContainsKey($c)) { $diffCols[$c] = 0 }
                    $diffCols[$c]++
                }
            }
        }
    }
    if ($rowDiff -gt 0) { $msg += "$rowDiff Zeilen ohne Gegenstueck " }
    if ($diffCols.Count -gt 0) {
        $msg += "WERT-ABWEICHUNGEN: " + (($diffCols.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', ') + " "
        $fail++
    }
    if ($msg -eq "") { Write-Host ("  OK  {0,-12} : {1} Zeilen identisch" -f $v.Name, $dtN.Rows.Count) }
    else { Write-Host ("  DIFF {0,-12} : {1}" -f $v.Name, $msg) -ForegroundColor Yellow }
}

# 5) Performance
Write-Host ""
Write-Host "Performance:"
foreach ($v in $views) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $null = Query "SELECT * FROM $($v.Name);"
    $sw.Stop()
    Write-Host ("  {0,-12} : {1,8:N3} s" -f $v.Name, $sw.Elapsed.TotalSeconds)
}
Write-Host ""
if ($fail -eq 0) { Write-Host "ERGEBNIS: Alle Views semantisch identisch." -ForegroundColor Green }
else { Write-Host "ERGEBNIS: $fail Abweichung(en) - bitte pruefen." -ForegroundColor Red }
$conn.Close()
