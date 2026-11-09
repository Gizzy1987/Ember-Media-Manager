$ErrorActionPreference = 'Stop'
$pkg = "H:\Arbeitszimmer\Ember-Media-Manager\packages\Stub.System.Data.SQLite.Core.NetFramework.1.0.115.5"
Add-Type -Path "$pkg\lib\net46\System.Data.SQLite.dll"
$DbPath = "H:\Arbeitszimmer\Ember-Media-Manager\.review-bench\fresh.db"
if (Test-Path $DbPath) { Remove-Item $DbPath -Force }
$conn = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$DbPath;Version=3;")
$conn.Open()
function Exec([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    [void]$cmd.ExecuteNonQuery(); $cmd.Dispose()
}
function Scalar([string]$sql) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 600
    $r = $cmd.ExecuteScalar(); $cmd.Dispose(); return $r
}

# 1) Frische DB aus der aktualisierten Schema-Datei erzeugen (exakt wie die App: ein Script)
$schema = [System.IO.File]::ReadAllText("H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL.txt")
Exec $schema
Write-Host "Frische DB aus MyVideosDBSQL.txt erzeugt."

# 2) Views der frischen DB pruefen
$views = @('movielist','setslist','seasonslist','tvshowlist','episodelist')
foreach ($v in $views) {
    $exists = Scalar "SELECT COUNT(*) FROM sqlite_master WHERE type='view' AND name='$v';"
    Write-Host ("  View {0,-12} vorhanden: {1}" -f $v, $exists)
}

# 3) Patch-Datei v48: DROP + CREATE testen (wie beim Upgrade)
Write-Host ""
Write-Host "Teste Patch MyVideosDBSQL_v48_Patch.xml ..."
[xml]$patch = Get-Content "H:\Arbeitszimmer\Ember-Media-Manager\EmberAPI\DB\MyVideosDBSQL_v48_Patch.xml" -Raw
$cmds = $patch.CommandFile.transaction.command
Write-Host ("  Commands im Patch: {0}" -f $cmds.Count)
foreach ($c in $cmds) {
    Exec $c.execute
    Write-Host ("  OK: {0}" -f $c.description)
}

# 4) Views nach dem Patch pruefen
Write-Host ""
foreach ($v in $views) {
    $exists = Scalar "SELECT COUNT(*) FROM sqlite_master WHERE type='view' AND name='$v';"
    Write-Host ("  View {0,-12} vorhanden nach Patch: {1}" -f $v, $exists)
}

# 5) Sanity: leere Views liefern 0 Zeilen, Spalten existieren
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT * FROM movielist;"
$da = New-Object System.Data.SQLite.SQLiteDataAdapter($cmd)
$dt = New-Object System.Data.DataTable
[void]$da.Fill($dt)
Write-Host ("  movielist auf leerer DB: {0} Zeilen, {1} Spalten" -f $dt.Rows.Count, $dt.Columns.Count)
$cmd.Dispose(); $da.Dispose()

$conn.Close()
Write-Host ""
Write-Host "ERGEBNIS: Schema + Patch fehlerfrei anwendbar." -ForegroundColor Green
