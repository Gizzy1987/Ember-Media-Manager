$ErrorActionPreference = 'Continue'
$repo = "H:\Arbeitszimmer\Ember-Media-Manager"
$env:Path += ";C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin"

$dirs = @("Addons\scraper.TMDB.Data", "Addons\scraper.TMDB.Poster", "Addons\scraper.TMDB.Trailer")

Write-Host "=== TMDbLib-Referenzen umstellen ==="
foreach ($d in $dirs) {
    $pc = Join-Path $repo "$d\packages.config"
    $r = [System.IO.File]::ReadAllText($pc)
    $r = $r -replace '(<package id="TMDbLib" version=")1\.9\.1(")', '${1}2.3.0${2}'
    [System.IO.File]::WriteAllText($pc, $r, (New-Object System.Text.UTF8Encoding($false)))

    $pj = (Get-ChildItem (Join-Path $repo "$d\*.vbproj")).FullName
    $r = [System.IO.File]::ReadAllText($pj)
    $r = $r -replace 'TMDbLib, Version=1\.0\.0\.0', 'TMDbLib, Version=2.0.0.0'
    $r = $r -replace 'packages\\TMDbLib\.1\.9\.1\\lib\\net45\\TMDbLib\.dll', 'packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll'
    [System.IO.File]::WriteAllText($pj, $r, (New-Object System.Text.UTF8Encoding($false)))
    "  $d ok"
}

Write-Host "`n=== Kontrolle ==="
Get-ChildItem (Join-Path $repo "Addons\scraper.TMDB.*\packages.config") | ForEach-Object {
    $m = Select-String -Path $_.FullName -Pattern "TMDbLib"
    if ($m) { "  $($_.Name): $($m.Line.Trim())" } else { "  $($_.Name): KEIN TMDbLib-EINTRAG (FEHLER!)" }
}
Get-ChildItem (Join-Path $repo "Addons\scraper.TMDB.*\*.vbproj") | ForEach-Object {
    (Select-String -Path $_.FullName -Pattern "TMDbLib").Line.Trim()
}

Write-Host "`n=== NuGet-Restore ==="
Push-Location $repo
& ".\.nuget\NuGet.exe" restore "Ember Media Manager for Jellyfin.sln" -Verbosity quiet 2>&1 | Select-Object -Last 4
Pop-Location

if (-not (Test-Path (Join-Path $repo "packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll"))) {
    Write-Host "FEHLER: TMDbLib 2.3.0 nicht wiederhergestellt. Verfuegbar:"
    Get-ChildItem (Join-Path $repo "packages") -Filter "TMDbLib*" -Directory | ForEach-Object { "  $($_.Name)" }
    exit 1
}
Write-Host "  TMDbLib 2.3.0 liegt vor."

Write-Host "`n=== Build der drei TMDb-Projekte ==="
$ok = $true
foreach ($d in $dirs) {
    $pj = (Get-ChildItem (Join-Path $repo "$d\*.vbproj")).FullName
    $out = & msbuild $pj -p:Configuration=Release -p:Platform=x64 -v:minimal -m 2>&1
    $errs = @($out | Select-String -Pattern ": error")
    Write-Host "  $(Split-Path $pj -Leaf) -> Exit $LASTEXITCODE, $($errs.Count) Fehler"
    if ($errs.Count -gt 0) {
        $ok = $false
        $errs | Select-Object -First 15 | ForEach-Object { "      $($_.Line.Trim())" }
    }
}

Write-Host "`n=== Ergebnis ==="
if ($ok) { Write-Host "  ERFOLG: Unser TMDb-Scraper kompiliert unveraendert gegen TMDbLib 2.3.0." -ForegroundColor Green }
else { Write-Host "  FEHLGESCHLAGEN: API-Brueche vorhanden." -ForegroundColor Yellow }
