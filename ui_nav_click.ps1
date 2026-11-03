param([string]$TargetName = "KSP 模组")
$out = "C:\Users\adminstartor\DoubaoWork\chats\2026-11-02\new-chat-1\PKL\ui_nav_click_out.txt"
Start-Transcript -Path $out -Force | Out-Null
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class M {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, int e);
  public static void Click(int x, int y){ SetCursorPos(x,y); System.Threading.Thread.Sleep(150);
    mouse_event(0x0002,0,0,0,0); System.Threading.Thread.Sleep(70); mouse_event(0x0004,0,0,0,0); }
}
"@

$proc = Get-Process PKL | Where-Object { $_.MainWindowTitle -like "PKL*" } | Select-Object -First 1
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Fg {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
}
"@
[Fg]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[Fg]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
$all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
$node = $null
foreach ($el in $all) { if ($el.Current.Name -eq $TargetName) { $node = $el; break } }
if ($null -eq $node) { Write-Host "target not found: $TargetName"; Stop-Transcript | Out-Null; exit 1 }

$rect = $node.Current.BoundingRectangle
$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
for ($k=0; $k -lt 8; $k++) {
  $rect = $node.Current.BoundingRectangle
  if ($rect.Width -gt 5 -and $rect.Height -gt 5) { break }
  $parent = $walker.GetParent($node)
  if ($null -eq $parent) { break }
  $node = $parent
}
$cx = [int]($rect.X + $rect.Width/2)
$cy = [int]($rect.Y + $rect.Height/2)
Write-Host ("clicking [" + $TargetName + "] rect X=" + [int]$rect.X + " Y=" + [int]$rect.Y + " W=" + [int]$rect.Width + " H=" + [int]$rect.Height + " -> " + $cx + "," + $cy)
[M]::Click($cx, $cy)
Start-Sleep 2
Stop-Transcript | Out-Null
