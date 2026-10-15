$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$auditPath = Join-Path $PSScriptRoot 'atlas-audit.json'
$audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$preview = Join-Path $workspace 'ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png'
$bitmap = [System.Drawing.Bitmap]::new($preview)
$best = $null
$bestScore = 1000000
for ($y = 1056; $y -lt 2048; $y += 64) {
    for ($x = 16; $x -lt 1024; $x += 32) {
        $color = $bitmap.GetPixel($x, $y)
        if ($color.R -le 220 -or $color.G -le 220 -or $color.B -le 210) { continue }
        $score = [math]::Abs($color.R - 240) + [math]::Abs($color.G - 235) + [math]::Abs($color.B - 220)
        if ($score -ge $bestScore) { continue }
        $bestScore = $score
        $safeUv = @()
        $safeUv += ($x - 12) / 2048.0
        $safeUv += (2048 - $y - 28) / 2048.0
        $safeUv += ($x + 12) / 2048.0
        $safeUv += (2048 - $y + 28) / 2048.0
        $best = @{pixel = @($x, $y); rgb = @($color.R, $color.G, $color.B); safeUvBottomLeft = $safeUv}
    }
}
$bitmap.Dispose()
if ($null -eq $best) { throw 'No safe ivory palette swatch found.' }
$audit.swatches.awning_ivory = $best
$audit | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $auditPath
$best | ConvertTo-Json
