$ErrorActionPreference = 'Continue'
# Live-Abfrage der IMDb-GraphQL-API: exakte Struktur des certificate-Feldes
$H = @{
    'User-Agent'      = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36'
    'Origin'          = 'https://www.imdb.com'
    'Referer'         = 'https://www.imdb.com/'
    'Content-Type'    = 'application/json'
    'Accept'          = 'application/json'
    'Accept-Language' = 'en-US,en;q=0.9'
}
$q = @'
{"query":"query { title(id: \"tt0111161\") { id titleText { text } certificate { rating country { id text } attributes { text } } } }"}
'@
try {
    $r = Invoke-RestMethod -Uri "https://api.graphql.imdb.com/" -Method Post -Headers $H -Body $q -TimeoutSec 40
    "--- certificate (Singular) ---"
    $r.data.title.certificate | ConvertTo-Json -Depth 6
} catch {
    "Singular-Feld: $($_.Exception.Message)"
}

$q2 = @'
{"query":"query { title(id: \"tt0111161\") { id certificates(first: 10) { edges { node { rating country { id text } attributes { text } } } } } }"}
'@
try {
    $r2 = Invoke-RestMethod -Uri "https://api.graphql.imdb.com/" -Method Post -Headers $H -Body $q2 -TimeoutSec 40
    "--- certificates (Plural, Connection) ---"
    $r2.data.title.certificates | ConvertTo-Json -Depth 6
} catch {
    "Plural-Feld: $($_.Exception.Message)"
}

"--- Serie tt0903747 (Breaking Bad) ---"
$q3 = @'
{"query":"query { title(id: \"tt0903747\") { id certificate { rating country { id text } attributes { text } } } }"}
'@
try {
    $r3 = Invoke-RestMethod -Uri "https://api.graphql.imdb.com/" -Method Post -Headers $H -Body $q3 -TimeoutSec 40
    $r3.data.title.certificate | ConvertTo-Json -Depth 6
} catch {
    "Serie: $($_.Exception.Message)"
}
