param(
    [string]$DbPath = "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\bench.db",
    [int]$MovieCount = 2600
)

$ErrorActionPreference = 'Stop'
$pkg = "H:\Arbeitszimmer\Ember-Media-Manager\packages\Stub.System.Data.SQLite.Core.NetFramework.1.0.115.5"
Add-Type -Path "$pkg\lib\net46\System.Data.SQLite.dll"
[System.IO.Directory]::CreateDirectory((Split-Path $DbPath)) | Out-Null
if (Test-Path $DbPath) { Remove-Item $DbPath -Force }

$conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DbPath;Version=3;")
$conn.Open()

function Exec([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 300
    [void]$cmd.ExecuteNonQuery()
    $cmd.Dispose()
}
function Scalar([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 300
    $r = $cmd.ExecuteScalar()
    $cmd.Dispose()
    return $r
}

# --- Schema anwenden (Tabellen zuerst, ohne Views/Indizes-Fehler) ---
$schema = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL.txt" -Raw
# Statements trennen
$stmts = [regex]::Split($schema, ';') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' -and $_ -notmatch '^--' }
foreach ($s in $stmts) {
    # Index-Statements ohne zugehoeriges CREATE TABLE sind ok (IF NOT EXISTS not vorhanden -> abfangen)
    try { Exec $s } catch { Write-Host "SKIP: $($s.Substring(0,[Math]::Min(60,$s.Length)))..." }
}

Write-Host "Schema angewendet."

# --- Synthetische Daten ---
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Exec @"
BEGIN;
-- Filme
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO movie (idMovie, idSource, MoviePath, Title, SortTitle, Type, "New", Mark, "Lock")
SELECT x, 1, 'C:\Movies\Film ' || x || '\Film ' || x || '.mkv', 'Film ' || x, 'Film ' || x, 0, 0, 0, 0 FROM seq;
COMMIT;
"@

# Actors: 4000 Schauspieler
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 4000)
INSERT INTO actors (idActor, strActor) SELECT x, 'Schauspieler ' || x FROM seq;
COMMIT;
"@

# Link-Tabellen: pro Film N verschiedene Links. Die Offsets sind so gewaehlt, dass
# (idMovie, idLink) garantiert eindeutig bleibt (UNIQUE-Constraint) und kein
# Index-Wrapper noetig ist: je Link-Tabelle wird pro Film ein eigener Block ueber
# unterschiedliche id-Werte erzeugt.
function InsertLinks([string]$Table, [string]$IdCol, [string]$Offset, [int]$Count, [string]$ExtraCols = '', [string]$ExtraVals = '') {
    $sql = @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO $Table ($IdCol, idMovie $ExtraCols)
SELECT (x * $Offset) % 4000 + 1, x $ExtraVals FROM seq;
COMMIT;
"@
    Exec $sql
}

# Genres (18), Studios (120), Countries (60), Tags (40)
Exec "BEGIN; INSERT INTO genre (idGenre, strGenre) SELECT x, 'Genre ' || x FROM (WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 18) SELECT x FROM seq); COMMIT;"
Exec "BEGIN; INSERT INTO studio (idStudio, strStudio) SELECT x, 'Studio ' || x FROM (WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 120) SELECT x FROM seq); COMMIT;"
Exec "BEGIN; INSERT INTO country (idCountry, strCountry) SELECT x, 'Land ' || x FROM (WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 60) SELECT x FROM seq); COMMIT;"
Exec "BEGIN; INSERT INTO tag (idTag, strTag) SELECT x, 'Tag ' || x FROM (WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < 40) SELECT x FROM seq); COMMIT;"

# Link-Tabellen mit realistischen Anzahlen:
#   4 Actors, 3 Writer, 2 Director, 3 Genre, 2 Studio, 2 Country, 2 Tag pro Film
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO actorlinkmovie (idActor, idMovie, strRole, iOrder)
SELECT (x % 400) * 10 + 1, x, 'Actor', 0 FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO actorlinkmovie (idActor, idMovie, strRole, iOrder)
SELECT (x % 400) * 10 + 2, x, 'Actor', 0 FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO actorlinkmovie (idActor, idMovie, strRole, iOrder)
SELECT (x % 400) * 10 + 3, x, 'Actor', 0 FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO actorlinkmovie (idActor, idMovie, strRole, iOrder)
SELECT (x % 400) * 10 + 4, x, 'Actor', 0 FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO writerlinkmovie (idWriter, idMovie) SELECT (x % 400) * 10 + 5, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO writerlinkmovie (idWriter, idMovie) SELECT (x % 400) * 10 + 6, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO writerlinkmovie (idWriter, idMovie) SELECT (x % 400) * 10 + 7, x FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO directorlinkmovie (idDirector, idMovie) SELECT (x % 400) * 10 + 8, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO directorlinkmovie (idDirector, idMovie) SELECT (x % 400) * 10 + 9, x FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO genrelinkmovie (idGenre, idMovie) SELECT (x % 18) + 1, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO genrelinkmovie (idGenre, idMovie) SELECT ((x + 1) % 18) + 1, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO genrelinkmovie (idGenre, idMovie) SELECT ((x + 2) % 18) + 1, x FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO studiolinkmovie (idStudio, idMovie) SELECT (x % 120) + 1, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO studiolinkmovie (idStudio, idMovie) SELECT ((x + 1) % 120) + 1, x FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO countrylinkmovie (idCountry, idMovie) SELECT (x % 60) + 1, x FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO countrylinkmovie (idCountry, idMovie) SELECT ((x + 1) % 60) + 1, x FROM seq;
COMMIT;
"@
Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO taglinks (idTag, idMedia, media_type) SELECT (x % 40) + 1, x, 'movie' FROM seq;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO taglinks (idTag, idMedia, media_type) SELECT ((x + 1) % 40) + 1, x, 'movie' FROM seq;
COMMIT;
"@

# Art: 1 Poster, 1 Fanart, 1 Banner, 1 Clearart, 1 Clearlogo, 1 Discart, 1 Keyart, 1 Landscape pro Film
foreach ($t in @('poster','fanart','banner','clearart','clearlogo','discart','keyart','landscape')) {
    Exec @"
BEGIN;
WITH RECURSIVE seq(x) AS (SELECT 1 UNION ALL SELECT x+1 FROM seq WHERE x < $MovieCount)
INSERT INTO art (media_id, media_type, type, url) SELECT x, 'movie', '$t', 'http://localhost/' || '$t' || x || '.jpg' FROM seq;
COMMIT;
"@
}

$sw.Stop()
Write-Host ("Daten erzeugt: {0:N1} s" -f $sw.Elapsed.TotalSeconds)

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$n = Scalar "SELECT COUNT(*) FROM movie;"
$sw.Stop()
Write-Host ("Filme: $n  (Count {0:N2} s)" -f $sw.Elapsed.TotalSeconds)

# ANALYZE fuer realistische Query-Plan-Statistik
Exec "ANALYZE;"

# --- Messung 1: aktuelle movielist-View ---
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) FROM (SELECT * FROM movielist);"
$cmd.CommandTimeout = 600
$r1 = $cmd.ExecuteScalar()
$sw.Stop()
$t1 = $sw.Elapsed.TotalSeconds
$cmd.Dispose()
Write-Host ("IST movielist (Kreuzprodukt-Join): {0:N3} s" -f $t1)

# Zwischenzeilen messen
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) FROM movie, countrylinkmovie, writerlinkmovie, directorlinkmovie, genrelinkmovie, studiolinkmovie, taglinks WHERE countrylinkmovie.idMovie=writerlinkmovie.idMovie AND writerlinkmovie.idMovie=directorlinkmovie.idMovie AND directorlinkmovie.idMovie=genrelinkmovie.idMovie AND genrelinkmovie.idMovie=studiolinkmovie.idMovie AND studiolinkmovie.idMovie=taglinks.idMedia;"
$cmd.CommandTimeout = 600
$cross = $cmd.ExecuteScalar()
$cmd.Dispose()
Write-Host ("Kreuzprodukt-Zwischenzeilen: $cross")

# --- Messung 2: optimierte View mit Scalar-Subqueries ---
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

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT COUNT(*) FROM (SELECT * FROM movielist_opt);"
$cmd.CommandTimeout = 600
$r2 = $cmd.ExecuteScalar()
$sw.Stop()
$t2 = $sw.Elapsed.TotalSeconds
$cmd.Dispose()
Write-Host ("SOLL movielist_opt (Scalar-Subqueries): {0:N3} s" -f $t2)

# --- Messung 3: kompletter Listenabruf (wie in der App: SELECT * FROM movielist) ---
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM movielist;"
$cmd.CommandTimeout = 600
$da = New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
[void]$da.Fill($dt)
$sw.Stop()
$t3 = $sw.Elapsed.TotalSeconds
$rows = $dt.Rows.Count
$cmd.Dispose(); $da.Dispose()
Write-Host ("IST SELECT * FROM movielist -> {0} Zeilen in {1:N3} s" -f $rows, $t3)

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM movielist_opt;"
$cmd.CommandTimeout = 600
$da = New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
$dt2 = New-Object System.Data.DataTable
[void]$da.Fill($dt2)
$sw.Stop()
$t4 = $sw.Elapsed.TotalSeconds
$rows2 = $dt2.Rows.Count
$cmd.Dispose(); $da.Dispose()
Write-Host ("SOLL SELECT * FROM movielist_opt -> {0} Zeilen in {1:N3} s" -f $rows2, $t4)

Write-Host ""
Write-Host "=============================================="
Write-Host ("Faktor IST/SOLL (Count):  {0:N1}x" -f ($t1 / $t2))
Write-Host ("Faktor IST/SOLL (Fetch):  {0:N1}x" -f ($t3 / $t4))
Write-Host ("Ersparnis absolut:        {0:N3} s" -f ($t3 - $t4))
Write-Host "=============================================="

$conn.Close()
