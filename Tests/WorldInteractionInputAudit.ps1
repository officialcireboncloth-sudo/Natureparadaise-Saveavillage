$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scriptRoot = Join-Path $projectRoot 'Assets/Nature  Paradaise/Script'
$producers = @()
$failures = @()
Get-ChildItem -LiteralPath $scriptRoot -Recurse -Filter '*.cs' |
    Where-Object { $_.FullName -notmatch '[\\/]Editor[\\/]' } | ForEach-Object {
        $source = [System.IO.File]::ReadAllText($_.FullName)
        if ($source -match 'PlayerInteractionTarget\.(Press|PressPickup)\(') {
            $failures += $_.Name + ': legacy input bypasses visible prompt ownership'
        }
        if ($source -notmatch 'WorldInteractionPrompt\.(Request|RequestClean)\(') { return }
        $category = if ($source -match 'PlayerInteractionTarget\.PressPrompt\(') { 'prompt-owned' }
            elseif ($source -match 'IsUsePressed\([^,\)]*,\s*this\)') { 'tool-owner context' }
            elseif ($_.Name -eq 'WorldInteractablePromptSource.cs') { 'display-only' }
            else { $failures += $_.Name + ': prompt producer has no owner-aware input path'; 'FAIL' }
        $producers += [PSCustomObject]@{ File = $_.Name; Input = $category }
    }
$producers | Sort-Object File | Format-Table -AutoSize
if ($failures.Count -gt 0) { throw ($failures -join [Environment]::NewLine) }
Write-Output ('PASS: ' + $producers.Count + ' runtime prompt producers audited; no legacy Press/PressPickup calls remain.')
