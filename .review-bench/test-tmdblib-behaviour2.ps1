$ErrorActionPreference = 'Continue'
$repo = "H:\Arbeitszimmer\Ember-Media-Manager"

function Test-Dll([string]$dll, [string]$label) {
    if (-not (Test-Path $dll)) { Write-Output ("  FEHLT: " + $dll); return }
    Write-Output ("=== " + $label + " ===")
    try {
        $asm = [System.Reflection.Assembly]::LoadFrom($dll)
    } catch {
        Write-Output ("  Laden fehlgeschlagen: " + $_.Exception.Message); return
    }
    Write-Output ("  Datei: " + (Split-Path $dll -Leaf))

    foreach ($tn in @("TMDbLib.Objects.Movies.Movie", "TMDbLib.Objects.TvShows.TvShow", "TMDbLib.Objects.TvShows.TvSeason")) {
        $t = $asm.GetType($tn)
        if (-not $t) { Write-Output ("  Typ nicht gefunden: " + $tn); continue }
        $ctor = $t.GetConstructor(@())
        Write-Output ("  [" + $tn + "] parameterloser Konstruktor: " + $(if ($ctor) { "ja" } else { "nein" }))
        $inst = $null
        if ($ctor) {
            try { $inst = $ctor.Invoke(@()) } catch { Write-Output ("    Instantiierung: " + $_.Exception.InnerException.Message) }
        }
        foreach ($pn in @("Videos", "Images", "Genres", "Credits", "ExternalIds", "Seasons", "Episodes", "ProductionCompanies", "SpokenLanguages")) {
            $p = $t.GetProperty($pn)
            if (-not $p) { continue }
            if (-not $inst) { Write-Output ("      {0,-22} <keine Instanz>" -f $pn); continue }
            try {
                $v = $p.GetValue($inst)
                if ($null -eq $v) {
                    Write-Output ("      {0,-22} NULL   <-- NRE-Risiko bleibt" -f $pn)
                } else {
                    Write-Output ("      {0,-22} initialisiert ({1})" -f $pn, $v.GetType().Name)
                }
            } catch {
                Write-Output ("      {0,-22} <Lesefehler>" -f $pn)
            }
        }
    }
}

Test-Dll (Join-Path $repo "packages\TMDbLib.1.9.1\lib\net45\TMDbLib.dll") "TMDbLib 1.9.1 (unser Stand)"
Write-Output ""
Test-Dll (Join-Path $repo "packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll") "TMDbLib 2.3.0 (Fremd-Fork)"
