$ErrorActionPreference = 'Stop'
Set-Location "H:\Arbeitszimmer\Ember-Media-Manager"

$env:Path += ";C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin"

Write-Host "=== 1) Test-Branch anlegen ==="
git checkout -b test/tmdblib-2.3.0 2>&1 | Select-Object -Last 2

$projects = @(
    @{ Dir = "Addons\scraper.TMDB.Data";       Proj = "scraper.Data.TMDB.vbproj" },
    @{ Dir = "Addons\scraper.TMDB.Poster";     Proj = "scraper.Image.TMDB.vbproj" },
    @{ Dir = "Addons\scraper.TMDB.Trailer";    Proj = "scraper.Trailer.TMDB.vbproj" }
)

Write-Host "`n=== 2) packages.config + vbproj auf TMDbLib 2.3.0 umstellen ==="
foreach ($p in $projects) {
    $pc = Join-Path $p.Dir "packages.config"
    $raw = [System.IO.File]::ReadAllText($pc)
    $raw = $raw -replace '(<package id="TMDbLib" version=")1\.9\.1(")', '$12.3.0$2'
    [System.IO.File]::WriteAllText($pc, $raw, (New-Object System.Text.UTF8Encoding($false)))

    $proj = Join-Path $p.Dir $p.Proj
    $raw = [System.IO.File]::ReadAllText($proj)
    $raw = $raw -replace 'TMDbLib, Version=1\.0\.0\.0', 'TMDbLib, Version=2.0.0.0'
    $raw = $raw -replace 'packages\\TMDbLib\.1\.9\.1\\lib\\net45\\TMDbLib\.dll', 'packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll'
    [System.IO.File]::WriteAllText($proj, $raw, (New-Object System.Text.UTF8Encoding($false)))
    "  $($p.Dir) angepasst"
}

Write-Host "`n=== 3) Kontrolle ==="
Select-String -Path ($projects | ForEach-Object { Join-Path $_.Dir "packages.config" }) -Pattern "TMDbLib" | ForEach-Object { "  $($_.Filename): $($_.Line.Trim())" }
Select-String -Path ($projects | ForEach-Object { Join-Path $_.Dir $_.Proj }) -Pattern "TMDbLib" | ForEach-Object { "  $($_.Filename):$($_.LineNumber): $($_.Line.Trim())" }

Write-Host "`n=== 4) NuGet-Restore (nur die TMDb-Projekte) ==="
& ".\.nuget\NuGet.exe" restore "Ember Media Manager for Jellyfin.sln" -Verbosity quiet 2>&1 | Select-Object -Last 5
if (-not (Test-Path "packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll")) {
    Write-Host "  FEHLER: TMDbLib 2.3.0 wurde nicht wiederhergestellt."
    Write-Host "  Verfuegbare TMDbLib-Pakete im packages-Ordner:"
    Get-ChildItem packages -Filter "TMDbLib*" -Directory | ForEach-Object { "    $($_.Name)" }
    exit 1
}
Write-Host "  TMDbLib 2.3.0 wiederhergestellt."

Write-Host "`n=== 5) Build der drei TMDb-Projekte ==="
$ok = $true
foreach ($p in $projects) {
    $proj = Join-Path $p.Dir $p.Proj
    $out = & msbuild $proj -p:Configuration=Release -p:Platform=x64 -v:minimal -m 2>&1
    $code = $LASTEXITCODE
    $errs = @($out | Select-String -Pattern ": error")
    Write-Host "  $($p.Proj) -> Exit $code, $($errs.Count) Fehler"
    if ($errs.Count -gt 0) {
        $ok = $false
        $errs | Select-Object -First 12 | ForEach-Object { "      $($_.Line.Trim())" }
    }
}

Write-Host "`n=== 6) Ergebnis ==="
if ($ok) { Write-Host "  ERFOLG: Unser TMDb-Scraper kompiliert unveraendert gegen TMDbLib 2.3.0." -ForegroundColor Green }
else { Write-Host "  FEHLGESCHLAGEN: API-Brueche im TMDb-Scraper." -ForegroundColor Yellow }
