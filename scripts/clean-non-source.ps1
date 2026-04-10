$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$pathsToRemove = @(
    'artifacts',
    'target',
    'lagencysisyphus',
    'apps/desktop/node_modules',
    'apps/desktop/dist',
    'apps/desktop/app-icon.png',
    'apps/desktop/src-tauri/target',
    'apps/desktop/src-tauri/gen'
)

$patternsToRemove = @(
    '**/bin',
    '**/obj',
    '**/target'
)

$removed = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in $pathsToRemove) {
    $fullPath = Join-Path $projectRoot $relativePath
    if (Test-Path $fullPath) {
        Remove-Item -Recurse -Force $fullPath
        $removed.Add($relativePath)
    }
}

foreach ($pattern in $patternsToRemove) {
    $matches = Get-ChildItem -Path $projectRoot -Directory -Recurse -Force |
        Where-Object {
            $relative = [IO.Path]::GetRelativePath($projectRoot, $_.FullName).Replace('\', '/')
            [System.Management.Automation.WildcardPattern]::new($pattern, 'IgnoreCase').IsMatch($relative)
        }

    foreach ($match in $matches) {
        $relative = [IO.Path]::GetRelativePath($projectRoot, $match.FullName).Replace('\', '/')
        if (Test-Path $match.FullName) {
            Remove-Item -Recurse -Force $match.FullName
            $removed.Add($relative)
        }
    }
}

$removed | Sort-Object -Unique
