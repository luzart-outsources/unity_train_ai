# Phase C — Quick test launchers for PowerShell.
#
# Usage:
#   .\test-chat.ps1                 # interactive REPL
#   .\test-chat.ps1 smoke           # 27-case smoke test
#   .\test-chat.ps1 eval [v1..v7]   # hardset 53 cases (default v6)
#   .\test-chat.ps1 compare         # compare all versions
#
# Auto-finds the venv Python so you can run from anywhere.

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$Py   = Join-Path $Root "AI_Training\phase_a_sentis\.venv\Scripts\python.exe"
$Sc   = Join-Path $Root "AI_Training\phase_c_chat\scripts"

if (-not (Test-Path $Py)) {
    Write-Error "Python venv not found at $Py"
    exit 1
}

$env:PYTHONIOENCODING = "utf-8"

$mode = if ($args.Count -gt 0) { $args[0] } else { "repl" }

switch ($mode) {
    "repl"  { & $Py -X utf8 (Join-Path $Sc "chat_repl.py") }
    "smoke" { & $Py -X utf8 (Join-Path $Sc "smoke_test_onnx.py") }
    "eval"  {
        $tag = if ($args.Count -gt 1) { $args[1] } else { "v6" }
        & $Py -X utf8 (Join-Path $Sc "eval_student_versioned.py") --tag $tag
    }
    "compare" {
        foreach ($v in @("v1","v2","v3","v4","v5","v6","v7")) {
            $out = & $Py -X utf8 (Join-Path $Sc "eval_student_versioned.py") --tag $v 2>$null `
                | Select-String "OVERALL"
            Write-Host $out
        }
    }
    default { Write-Host "Usage: .\test-chat.ps1 [repl|smoke|eval [tag]|compare]" }
}
