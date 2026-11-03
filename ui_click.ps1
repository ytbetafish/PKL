$out = "C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\ui_click_out.txt"
Start-Transcript -Path $out -Force | Out-Null
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process PKL | Where-Object { $_.MainWindowTitle -like "PKL*" } | Select-Object -First 1
Write-Host ("PID=" + $proc.Id)
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
Write-Host ("descendants=" + $all.Count)
$i = 0
foreach ($el in $all) {
  $ct = $el.Current.ControlType.ProgrammaticName
  Write-Host ("[$i] name='" + $el.Current.Name + "' type=" + $ct + " aid=" + $el.Current.AutomationId)
  $i++
}
Stop-Transcript | Out-Null
