$ErrorActionPreference = 'Stop'
Set-Location "H:\Arbeitszimmer\Ember-Media-Manager"

function Get-Pkgs([string]$rev) {
    $h = @{}
    if ($rev -eq "OURS") {
        Get-ChildItem -Recurse -Filter packages.config -File |
            Where-Object { $_.FullName -notlike "*\packages\*" } | ForEach-Object {
                $rel = $_.FullName.Replace("H:\Arbeitszimmer\Ember-Media-Manager\", "").Replace("packages.config", "").Replace("\", "/")
                ([xml](Get-Content $_.FullName -Raw)).packages.package | ForEach-Object {
                    $h["$rel$($_.id)"] = "$($_.version)"
                }
            }
    } else {
        foreach ($f in (git ls-tree -r --name-only FETCH_HEAD | Select-String -Pattern "packages\.config$")) {
            $c = git show "FETCH_HEAD:$($f.ToString())" 2>$null
            if ($c) {
                $rel = $f.ToString().Replace("packages.config", "")
                ([xml]$c).packages.package | ForEach-Object { $h["$rel$($_.id)"] = "$($_.version)" }
            }
        }
    }
    return $h
}

$ours = Get-Pkgs "OURS"
$theirs = Get-Pkgs "THEIRS"
$keys = ($ours.Keys + $theirs.Keys) | Sort-Object -Unique
$diff = @()
foreach ($k in $keys) {
    $a = $ours[$k]; $b = $theirs[$k]
    # nur Pakete, die in BEIDEN Baeumen vorkommen und sich in der Version unterscheiden
    if ($a -and $b -and $a -ne $b) {
        $diff += [PSCustomObject]@{ Paket = $k; Uns = $a; Ihre = $b }
    }
}
"GEMEINSAME Pakete mit abweichender Version: $($diff.Count)"
$diff | Sort-Object Uns, Ihre | Format-Table -AutoSize | Out-String -Width 220

"`n--- Nur bei UNS (nicht bei ihnen) ---"
($ours.Keys | Where-Object { -not $theirs.ContainsKey($_) } | Sort-Object) -join "`n"
"`n--- Nur bei IHNEN (nicht bei uns), ohne Kodi ---"
($theirs.Keys | Where-Object { -not $ours.ContainsKey($_) -and $_ -notlike "*Kodi*" } | Sort-Object) -join "`n"
