$ErrorActionPreference = 'Continue'
$repo = "H:\Arbeitszimmer\Ember-Media-Manager"

function Test-Dll([string]$dll) {
    if (-not (Test-Path $dll)) { Write-Host "  FEHLT: $dll"; return }
    $asm = [System.Reflection.Assembly]::LoadFrom($dll)
    Write-Host "  --- $([System.IO.Path]::GetFileName((Split-Path $dll -Parent))) ---"
    $ver = $asm.GetName().Version
    Write-Host "    Assembly-Version: $ver"

    foreach ($tn in @("TMDbLib.Objects.Movies.Movie", "TMDbLib.Objects.TvShows.TvShow", "TMDbLib.Objects.TvShows.TvSeason")) {
        $t = $asm.GetType($tn)
        if (-not $t) { continue }
        Write-Host "    [$tn]"
        foreach ($pn in @("Videos", "Images", "Genres", "Credits", "ExternalIds", "Seasons", "Episodes", "ProductionCompanies", "SpokenLanguages")) {
            $p = $t.GetProperty($pn)
            if (-not $p) { continue }
            # Instanz erzeugen und Property-Wert pruefen
            try {
                $inst = [Activator]::CreateInstance($t)
                $v = $p.GetValue($inst)
                $state = if ($null -eq $v) { "NULL  <-- NRE-Risiko" } else { "initialisiert ($($v.GetType().Name))" }
                Write-Host "      {0,-22} {1}" -f $pn, $state
            } catch {
                Write-Host "      {0,-22} <Instantiierung nicht moeglich>" -f $pn
            }
        }
    }
}

Write-Host "=== TMDbLib 1.9.1 (unser aktueller Stand) ==="
Test-Dll (Join-Path $repo "packages\TMDbLib.1.9.1\lib\net45\TMDbLib.dll")

Write-Host ""
Write-Host "=== TMDbLib 2.3.0 (aus dem Fremd-Fork) ==="
Test-Dll (Join-Path $repo "packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll")
