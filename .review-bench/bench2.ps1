param(
    [int]$MovieCount = 2600,
    [int]$FanartsPerMovie = 6
)
# Erweitert den Benchmark: wie skaliert der Kreuzprodukt-Join mit der Zahl der
# Art-Eintraege pro Film (mehrere Fanarts/Extrafanarts sind in echten Bibliotheken
# die Regel)?
$ErrorActionPreference = 'Stop'
$pkg = "H:\Arbeitszimmer\Ember-Media-Manager\packages\Stub.System.Data.SQLite.Core.NetFramework.1.0.115.5"
Add-Type -Path "$pkg\lib\net46\System.Data.SQLite.dll"
$DbPath = "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\bench2.db"
[System.IO.Directory]::CreateDirectory((Split-Path $DbPath)) | Out-Null
if (Test-Path $DbPath) { Remove-Item $DbPath -Force }
$conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DbPath;Version=3;")
$conn.Open()
function Exec([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    [void]$cmd.ExecuteNonQuery(); $cmd.Dispose()
}
function TimeQuery([string]$sql) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    $da = New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    [void]$da.Fill($dt)
    $sw.Stop()
    $n = $dt.Rows.Count
    $cmd.Dispose(); $da.Dispose()
    return @{ Sec = $sw.Elapsed.TotalSeconds; Rows = $n }
}

$schema = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL.txt" -Raw
$stmts = [regex]::Split($schema, ';') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' -and $_ -notmatch '^--' }
foreach ($s in $stmts) { try { Exec $s } catch { } }

# Basisdaten wie im Haupt-Benchmark
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO movie (idMovie, idSource, MoviePath, Title, SortTitle, Type, "New", Mark, "Lock")
SELECT x, 1, 'C:\Movies\F' || x, 'Film ' || x, 'Film ' || x, 0, 0, 0, 0 FROM seq;
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
COMMIT;
"@
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
# Art: mehrere Fanarts pro Film (realistisch: Fanart + Extrafanarts)
foreach ($k in 0..($FanartsPerMovie - 1)) {
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
Exec "ANALYZE;"

$optSql = @"
CREATE VIEW movielist_opt AS
SELECT
  movie.*,
  CASE WHEN movie.SortTitle IS NOT '' THEN movie.SortTitle ELSE movie.Title END AS SortedTitle,
  source.strName AS Source,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='banner' LIMIT 1) AS BannerPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='clearart' LIMIT 1) AS ClearArtPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='clearlogo' LIMIT 1) AS ClearLogoPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='discart' LIMIT 1) AS DiscArtPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='fanart' LIMIT 1) AS FanartPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='keyart' LIMIT 1) AS KeyartPath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='landscape' LIMIT 1) AS LandscapePath,
  (SELECT url FROM art WHERE media_id=movie.idMovie AND media_type='movie' AND type='poster' LIMIT 1) AS PosterPath,
  (SELECT GROUP_CONCAT(DISTINCT countries.strCountry) FROM countrylinkmovie clm JOIN country countries ON countries.idCountry=clm.idCountry WHERE clm.idMovie=movie.idMovie) AS Country,
  (SELECT GROUP_CONCAT(DISTINCT credits.strActor) FROM writerlinkmovie wlm JOIN actors credits ON credits.idActor=wlm.idWriter WHERE wlm.idMovie=movie.idMovie) AS Credits,
  (SELECT GROUP_CONCAT(DISTINCT directors.strActor) FROM directorlinkmovie dlm JOIN actors directors ON directors.idActor=dlm.idDirector WHERE dlm.idMovie=movie.idMovie) AS Director,
  (SELECT GROUP_CONCAT(DISTINCT genres.strGenre) FROM genrelinkmovie glm JOIN genre genres ON genres.idGenre=glm.idGenre WHERE glm.idMovie=movie.idMovie) AS Genre,
  (SELECT GROUP_CONCAT(DISTINCT studios.strStudio) FROM studiolinkmovie slm JOIN studio studios ON studios.idStudio=slm.idStudio WHERE slm.idMovie=movie.idMovie) AS Studio,
  (SELECT GROUP_CONCAT(DISTINCT tags.strTag) FROM taglinks taglink JOIN tag tags ON tags.idTag=taglink.idTag WHERE taglink.idMedia=movie.idMovie AND taglink.media_type='movie') AS 'Tag'
FROM movie
LEFT OUTER JOIN moviesource AS source ON (source.idSource = movie.idSource);
"@
Exec $optSql

Write-Host ""
Write-Host "=== $MovieCount Filme, $FanartsPerMovie Fanarts pro Film ==="
$r1 = TimeQuery "SELECT * FROM movielist;"
$r2 = TimeQuery "SELECT * FROM movielist_opt;"
Write-Host ("IST movielist     : {0,8:N3} s  ({1} Zeilen)" -f $r1.Sec, $r1.Rows)
Write-Host ("SOLL movielist_opt : {0,8:N3} s  ({1} Zeilen)" -f $r2.Sec, $r2.Rows)
Write-Host ("Faktor            : {0,8:N1}x" -f ($r1.Sec / $r2.Sec))
$conn.Close()
