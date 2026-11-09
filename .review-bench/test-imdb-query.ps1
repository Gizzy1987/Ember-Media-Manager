$ErrorActionPreference = 'Continue'
# Testet die ECHTEN Query-Konstanten aus der Quelle gegen die IMDb-GraphQL-API.
$src = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\Addons\scraper.IMDB.Data\Scraper\clsScrapeIMDBApi.vb" -Raw

# TITLE_FIELDS aus der Quelle extrahieren (VB-Stringverkettung auswerten)
function Get-VbConst([string]$name) {
    $m = [regex]::Match($src, "Public Const $name As String = (.*?)\r?\n\r?\n", [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $m.Success) { return $null }
    $expr = $m.Groups[1].Value
    # alle "..."-Literale einsammeln und verketten
    $parts = [regex]::Matches($expr, '"((?:[^"]|"")*)"') | ForEach-Object { $_.Groups[1].Value -replace '""', '"' }
    return ($parts -join "")
}

$H = @{
    'User-Agent'      = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36'
    'Origin'          = 'https://www.imdb.com'
    'Referer'         = 'https://www.imdb.com/'
    'Content-Type'    = 'application/json'
    'Accept'          = 'application/json'
    'Accept-Language' = 'en-US,en;q=0.9'
}

foreach ($case in @(@{ Const = 'TITLE_FIELDS'; Id = 'tt0111161'; Label = 'Film (Der Shawshank-Redemption)' },
                    @{ Const = 'SHOW_FIELDS';  Id = 'tt0903747'; Label = 'Serie (Breaking Bad)' })) {
    $fields = Get-VbConst $case.Const
    if (-not $fields) { Write-Output ("$($case.Const): KONSTANTE NICHT GEFUNDEN"); continue }
    Write-Output ("=== " + $case.Const + " - " + $case.Label + " ===")
    Write-Output ("  certificate im Query: " + $(if ($fields -match "certificate \{") { "JA" } else { "NEIN" }))
    $q = @{ query = "query { title(id: `"$($case.Id)`") { $fields } }" } | ConvertTo-Json -Compress
    try {
        $r = Invoke-RestMethod -Uri "https://api.graphql.imdb.com/" -Method Post -Headers $H -Body $q -TimeoutSec 45
        if ($r.errors) {
            Write-Output ("  GraphQL-Fehler: " + ($r.errors | ForEach-Object { $_.message }) -join " | ")
        } else {
            $cert = $r.data.title.certificate
            Write-Output ("  HTTP/GraphQL: OK")
            Write-Output ("  certificate.rating  = '" + $cert.rating + "'")
            Write-Output ("  certificate.country = '" + $cert.country.text + "' (" + $cert.country.id + ")")
            Write-Output ("  -> Certifications-Eintrag waere: '" + $cert.country.text + ":" + $cert.rating + "'")
            Write-Output ("  runtime.seconds      = '" + $r.data.title.runtime.seconds + "'  -> NFO-Minuten: " + [math]::Round(([double]$r.data.title.runtime.seconds) / 60, 0))
            $tag = $r.data.title.taglines.edges | Select-Object -First 1
            if ($tag) { Write-Output ("  taglines[0].text     = '" + $tag.node.text + "'") }
        }
    } catch {
        Write-Output ("  Abfrage fehlgeschlagen: " + $_.Exception.Message)
    }
    Write-Output ""
}
