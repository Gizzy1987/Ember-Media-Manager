$ErrorActionPreference = 'Stop'
$schema = "H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL.txt"
$opt = "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\views_optimized.sql"

$lines = Get-Content -LiteralPath $schema
$idx = ($lines | Select-String -Pattern '^CREATE VIEW episodelist' | Select-Object -First 1).LineNumber
if (-not $idx) { throw "CREATE VIEW episodelist nicht gefunden" }

$head = $lines[0..($idx - 2)]
$optLines = Get-Content -LiteralPath $opt
# Kommentarzeilen der optimierten Datei entfernen
$optLines = $optLines | Where-Object { $_ -notmatch '^\s*--' }
# fuehrende Leerzeile nach dem Kommentarblock entfernen
while ($optLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($optLines[0])) { $optLines = $optLines[1..($optLines.Count - 1)] }

$new = @()
$new += $head
$new += $optLines

# Zeilenenden erhalten: die Quelldatei nutzt LF oder CRLF?
$raw = [System.IO.File]::ReadAllText($schema)
$usesCrlf = $raw.Contains("`r`n")
$text = ($new -join "`n")
if ($usesCrlf) { $text = $text -replace "`n", "`r`n" }
[System.IO.File]::WriteAllText($schema, $text, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ("MyVideosDBSQL.txt aktualisiert: {0} Zeilen -> {1} Zeilen (CRLF: {2})" -f $lines.Count, $new.Count, $usesCrlf)
