Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

function Get-AppWindow {
    $proc = Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) { return $null }
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond) | Select-Object -First 1
}

$win = Get-AppWindow
if (-not $win) { Write-Output "FEHLER: App laeuft nicht"; exit 1 }

# --- Menüleiste auslesen -------------------------------------------------
$menuCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::MenuBar)
$menu = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $menuCond)
Write-Output "=== Menueleiste ==="
foreach ($m in $menu.FindAll([System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)) {
    Write-Output ("  - {0}" -f $m.Current.Name)
}

# --- Einstellungen öffnen ------------------------------------------------
# "Einstellungen ..." liegt unter "Bearbeiten" - erst das Untermenü aufklappen
$editItem = $null
foreach ($m in $menu.FindAll([System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)) {
    if ($m.Current.Name -eq "Bearbeiten") { $editItem = $m }
}
if ($editItem) {
    try {
        $expand = $editItem.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
        $expand.Expand()
        Start-Sleep -Seconds 2
    } catch { Write-Output "  (Untermenue nicht aufklappbar: $($_.Exception.Message))" }
}

$itemCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty, "Einstellungen ...")
$item = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $itemCond)
if (-not $item) {
    $itemCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, "Einstellungen …")
    $item = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $itemCond)
}
if (-not $item) { Write-Output "FEHLER: Menuepunkt 'Einstellungen' nicht gefunden"; exit 2 }

Write-Output "=== Klicke '$($item.Current.Name)' ==="
$invoke = $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
$invoke.Invoke()
Start-Sleep -Seconds 6

$proc = Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) {
    Write-Output "RESULTAT: App ist abgestuerzt (Prozess weg)"
    exit 3
}
Write-Output "RESULTAT: App lebt noch (PID $($proc.Id))"

# --- Einstellungsfenster auslesen ---------------------------------------
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
Write-Output "=== Fenster der App ==="
foreach ($w in $wins) { Write-Output ("  '{0}'  [{1}]" -f $w.Current.Name, $w.Current.ClassName) }

foreach ($w in $wins) {
    if ($w.Current.Name -match 'Einstellung') {
        Write-Output "=== Auszug aus dem Einstellungsfenster ==="
        $n = 0
        foreach ($i in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants,
                [System.Windows.Automation.Condition]::TrueCondition)) {
            if ($i.Current.Name -and $n -lt 40) {
                Write-Output ("  - {0}" -f $i.Current.Name); $n++
            }
        }
    }
}
