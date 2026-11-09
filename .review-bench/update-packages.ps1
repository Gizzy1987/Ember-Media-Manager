$ErrorActionPreference = 'Continue'
$repo = "H:\Arbeitszimmer\Ember-Media-Manager"
$env:Path += ";C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin"

# Paket-Updates: Newtonsoft.Json 13.0.3 -> 13.0.4, HtmlAgilityPack 1.11.42 -> 1.12.4,
# TMDbLib 1.9.1 -> 2.3.0 (Compile-Probe bestanden).
$updates = @(
    @{ Id = 'Newtonsoft.Json';  From = '13.0.3';  To = '13.0.4';  HintFrom = 'packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll';  HintTo = 'packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll';  VerFrom = '13.0.0.0'; VerTo = '13.0.0.0' },
    @{ Id = 'HtmlAgilityPack';  From = '1.11.42'; To = '1.12.4';  HintFrom = 'packages\HtmlAgilityPack.1.11.42\lib\Net45\HtmlAgilityPack.dll';  HintTo = 'packages\HtmlAgilityPack.1.12.4\lib\Net45\HtmlAgilityPack.dll';  VerFrom = '1.11.42.0'; VerTo = '1.12.4.0' },
    @{ Id = 'TMDbLib';          From = '1.9.1';   To = '2.3.0';   HintFrom = 'packages\TMDbLib.1.9.1\lib\net45\TMDbLib.dll';  HintTo = 'packages\TMDbLib.2.3.0\lib\net45\TMDbLib.dll';  VerFrom = '1.0.0.0';  VerTo = '2.0.0.0' }
)

function Set-Package([string]$root, $u) {
    $changed = 0
    # 1) alle packages.config unterhalb von $root
    Get-ChildItem -Path $root -Recurse -Filter packages.config -File |
        Where-Object { $_.FullName -notlike "*\packages\*" } | ForEach-Object {
            $raw = [System.IO.File]::ReadAllText($_.FullName)
            $pat = '(<package id="' + [regex]::Escape($u.Id) + '" version=")' + [regex]::Escape($u.From) + '(")'
            if ($raw -match $pat) {
                $raw = [regex]::Replace($raw, $pat, '${1}' + $u.To + '${2}')
                [System.IO.File]::WriteAllText($_.FullName, $raw, (New-Object System.Text.UTF8Encoding($false)))
                $changed++
            }
        }
    # 2) alle vbproj unterhalb von $root: HintPath + Assembly-Version
    Get-ChildItem -Path $root -Recurse -Filter *.vbproj -File | ForEach-Object {
        $raw = [System.IO.File]::ReadAllText($_.FullName)
        $orig = $raw
        $raw = $raw.Replace($u.HintFrom, $u.HintTo)
        if ($u.VerFrom -ne $u.VerTo) {
            $raw = $raw.Replace($u.Id + ', Version=' + $u.VerFrom, $u.Id + ', Version=' + $u.VerTo)
        }
        if ($raw -ne $orig) {
            [System.IO.File]::WriteAllText($_.FullName, $raw, (New-Object System.Text.UTF8Encoding($false)))
            $changed++
        }
    }
    return $changed
}

foreach ($u in $updates) {
    $n = Set-Package $repo $u
    Write-Output ("{0} {1} -> {2}: {3} Dateien angepasst" -f $u.Id, $u.From, $u.To, $n)
}

Write-Output ""
Write-Output "=== NuGet-Restore ==="
Push-Location $repo
& ".\.nuget\NuGet.exe" restore "Ember Media Manager for Jellyfin.sln" -Verbosity quiet 2>&1 | Select-Object -Last 4
Pop-Location

Write-Output ""
Write-Output "=== Pakete im packages-Ordner ==="
foreach ($u in $updates) {
    $found = Get-ChildItem (Join-Path $repo "packages") -Filter "$($u.Id).*" -Directory -ErrorAction SilentlyContinue | ForEach-Object { $_.Name }
    Write-Output ("  " + $u.Id + ": " + ($found -join ", "))
}

Write-Output ""
Write-Output "=== Voll-Build ==="
Push-Location $repo
$out = & msbuild "Ember Media Manager for Jellyfin.sln" -p:Configuration=Release -p:Platform=x64 -t:Rebuild -v:minimal -m 2>&1
$code = $LASTEXITCODE
Pop-Location
$errs = @($out | Select-String -Pattern ": error")
Write-Output ("  Exit-Code: $code, Fehler: " + $errs.Count)
$errs | Select-Object -First 20 | ForEach-Object { "    " + $_.Line.Trim() }
