# check-core.ps1
# PostToolUse hook: detect forbidden Unity API usage in Game.Core (homework constraints A/F).
# exit 0 = clean / exit 2 = violation (stderr is sent back to Claude)
# NOTE: keep this file ASCII-only. PowerShell 5.1 misreads UTF-8 without BOM.

$core_dir = "Assets/Scripts/Game.Core"

# Nothing to check until the Core folder exists
if (-not (Test-Path $core_dir)) { exit 0 }

# Forbidden patterns (constraint A and F of the homework spec)
$patterns = @(
    'using\s+UnityEngine',
    '\bMonoBehaviour\b',
    '\bGameObject\b',
    '\bTransform\b',
    '\bVector2Int\b',
    '\bVector2\b',
    '\bVector3\b',
    '\bTime\.',
    '\bRigidbody\b',
    '\bCollider\b',
    'UnityEngine\.Random',
    '\bRandom\.Range\b',
    '\bRandom\.value\b'
)

$violations = @()

# Scan every .cs file under Core
$files = Get-ChildItem -Path $core_dir -Filter *.cs -Recurse -File -ErrorAction SilentlyContinue
foreach ($f in $files) {
    foreach ($p in $patterns) {
        $hits = Select-String -Path $f.FullName -Pattern $p
        foreach ($h in $hits) {
            $violations += "$($h.Path):$($h.LineNumber): $($h.Line.Trim())"
        }
    }
}

# Constraint F: Core asmdef must not reference UnityEngine
$asmdef = Join-Path $core_dir "Game.Core.asmdef"
if (Test-Path $asmdef) {
    try {
        $json = Get-Content $asmdef -Raw | ConvertFrom-Json
        if (-not $json.noEngineReferences) {
            $violations += "${asmdef}: must set ""noEngineReferences"": true (Core must not reference UnityEngine)"
        }
    } catch {
        $violations += "${asmdef}: not readable as JSON"
    }
}

if ($violations.Count -gt 0) {
    [Console]::Error.WriteLine("[check-core] Game.Core constraint violation (fix immediately, do not weaken tests):")
    foreach ($v in $violations) { [Console]::Error.WriteLine("  $v") }
    exit 2
}

exit 0
