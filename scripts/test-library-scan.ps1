Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$exe = "H:\Arbeitszimmer\Ember-Media-Manager\EmberMM - Release - x64\Ember Media Manager for Jellyfin.exe"
Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Start-Process -FilePath $exe
Start-Sleep -Seconds 15

$proc = Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "FEHLER: App startet nicht"; exit 1 }
Write-Output "App gestartet (PID $($proc.Id))"

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond) | Select-Object -First 1

# Button "Bibliothek aktualisieren" suchen und ausloesen
$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Bibliothek aktualisieren")
$btn = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
if (-not $btn) { Write-Output "FEHLER: Button 'Bibliothek aktualisieren' nicht gefunden"; exit 2 }

Write-Output "Starte Scan ..."
$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()

# warten und dabei beobachten
for ($i = 1; $i -le 24; $i++) {
    Start-Sleep -Seconds 5
    $p = Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { Write-Output "ABSTURZ nach $($i * 5) Sekunden!"; exit 3 }
}
Write-Output "App laeuft nach 120 Sekunden Scan weiter - kein Absturz"
