$dir = 'C:\Users\admin\AppData\Local\Unity\Editor'
if (-not (Test-Path $dir)) { Write-Output "Editor log dir missing"; exit 0 }
$logs = Get-ChildItem $dir -Filter 'Editor*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 2
foreach ($l in $logs) {
    Write-Output ('=== ' + $l.Name + ' (' + $l.LastWriteTime + ') ===')
    Get-Content $l.FullName -Tail 300 | Where-Object { $_ -match 'Exception|Error|crash|TDR|D3D|NullRef|Missing|Stack|Assert' } | Select-Object -Last 40
}
