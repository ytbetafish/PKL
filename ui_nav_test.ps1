$out = "C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\ui_nav_out.txt"
Start-Transcript -Path $out -Force | Out-Null
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$proc = Get-Process PKL | Where-Object { $_.MainWindowTitle -like "PKL*" } | Select-Object -First 1
if (-not $proc) { Write-Host "no PKL window"; exit 1 }
Write-Host ("PID=" + $proc.Id + " hWnd=" + $proc.MainWindowHandle)

$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
Write-Host ("root=" + $root.Current.Name)

# Walk descendants, print all elements whose Name contains our nav labels
$cond = New-Object System.Windows.Automation.Condition
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
Write-Host ("descendants=" + $all.Count)
$navNames = @("主页","KSP 模组","KSP模组","设置","KSP 启动","KSP启动")
foreach ($el in $all) {
  $n = $el.Current.Name
  if ($navNames -contains $n) {
    Write-Host ("FOUND name='" + $n + "' type=" + $el.Current.ControlType.ProgrammaticName + " autoId=" + $el.Current.AutomationId)
  }
}
Stop-Transcript | Out-Null
