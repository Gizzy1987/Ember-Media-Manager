Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$proc = Get-Process -Name "Ember Media Manager*" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $proc) { Write-Output "App laeuft nicht"; exit 1 }

$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)

foreach ($w in $windows) {
    Write-Output "### Fenster: '$($w.Current.Name)'  (Klasse $($w.Current.ClassName))"
}

function Dump-Menu($el, $depth) {
    if ($depth -gt 3) { return }
    $items = $el.FindAll([System.Windows.Automation.TreeScope]::Children,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($i in $items) {
        $name = $i.Current.Name
        if ($name) {
            Write-Output ("{0}- {1}  [{2}]" -f ("  " * ($depth + 1)), $name, $i.Current.ControlType.ProgrammaticName)
        }
        Dump-Menu $i ($depth + 1)
    }
}

foreach ($w in $windows) {
    Dump-Menu $w 0
}
