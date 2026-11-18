param(
    [string]$Gimp,
    [switch]$Repaint,
    [string[]]$Slots,
    [string]$StageDirectory,
    [ValidateSet('Orchard','OilPalm')][string]$AtlasVariant = 'Orchard'
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$project = (Resolve-Path -LiteralPath (Join-Path $source '..\..')).Path
$scratch = Join-Path $project 'tmp\FruitTreeApple'
$textureAssets = Join-Path $project 'Assets\Nature  Paradaise\Textures\Shared\FruitTrees'

if (-not $Gimp) {
    $programFolders = @(
        (Join-Path $env:USERPROFILE 'AppData\Local\Programs'),
        $env:ProgramFiles
    )
    $Gimp = $programFolders | ForEach-Object { Join-Path $_ 'GIMP 3\bin\gimp-console-3.exe' } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $Gimp -or -not (Test-Path -LiteralPath $Gimp -PathType Leaf)) {
    throw 'Specify a GIMP 3 console executable with -Gimp.'
}
if ($Repaint -and $Slots) {
    throw 'Choose either -Repaint for the whole atlas or -Slots for specific textures.'
}
if (($Repaint -or $Slots) -and -not $StageDirectory) {
    $StageDirectory = Join-Path $scratch 'AtlasRepaint'
}

$publish = -not $StageDirectory
if ($publish) {
    $StageDirectory = Join-Path $scratch ([guid]::NewGuid().ToString('N'))
}
New-Item -ItemType Directory -Force -Path $StageDirectory | Out-Null
$stage = (Resolve-Path -LiteralPath $StageDirectory).Path
$xcfs = Join-Path $source 'Textures'
if ($stage -eq $xcfs -or $stage -eq $textureAssets) {
    throw 'Choose a staging folder outside the source and Unity texture folders.'
}

$gimpProfile = $env:GIMP3_DIRECTORY
$oldStage = $env:NP_TREE_ATLAS_STAGE
$oldRefresh = $env:NP_TREE_ATLAS_REFRESH
$oldSlots = $env:NP_TREE_ATLAS_SLOTS
$oldVariant = $env:NP_TREE_ATLAS_VARIANT
try {
    $env:GIMP3_DIRECTORY = Join-Path $scratch 'GimpProfile'
    $env:NP_TREE_ATLAS_STAGE = $stage
    $env:NP_TREE_ATLAS_REFRESH = if ($Repaint) { '1' } else { '0' }
    $env:NP_TREE_ATLAS_SLOTS = $Slots -join ','
    $env:NP_TREE_ATLAS_VARIANT = $AtlasVariant
    $paint = (Join-Path $PSScriptRoot 'paint_fruit_tree_atlas_gimp.py').Replace('\', '/').Replace("'", "\'")
    $batch = "exec(compile(open('$paint',encoding='utf-8').read(),'$paint','exec'),{'__file__':'$paint'})"
    & $Gimp -n -i --batch-interpreter=python-fu-eval --batch $batch --quit
    if ($LASTEXITCODE -ne 0) { throw 'GIMP could not export the fruit-tree atlas.' }

    $atlas = Get-Content -LiteralPath (Join-Path $source 'AtlasRegistry.json') -Raw | ConvertFrom-Json
    if ($AtlasVariant -eq 'OilPalm') { $atlas.name = 'NP_OilPalm_01' }
    $maps = 'BaseColor', 'Normal', 'SurfaceMask'
    foreach ($map in $maps) {
        foreach ($extension in 'xcf', 'png') {
            $file = Join-Path $stage "$($atlas.name)_$map.$extension"
            if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -lt 1) {
                throw "GIMP did not finish exporting $map.$extension"
            }
        }
    }
    if ($publish) {
        New-Item -ItemType Directory -Force -Path $xcfs, $textureAssets | Out-Null
        foreach ($map in $maps) {
            $name = "$($atlas.name)_$map"
            Copy-Item -LiteralPath (Join-Path $stage "$name.xcf") -Destination $xcfs
            Copy-Item -LiteralPath (Join-Path $stage "$name.png") -Destination $textureAssets
        }
        Write-Output "Updated GIMP sources and Unity textures for $($atlas.name)."
    }
    else {
        Write-Output "Staged $($atlas.name) in $stage"
    }
}
finally {
    $env:GIMP3_DIRECTORY = $gimpProfile
    $env:NP_TREE_ATLAS_STAGE = $oldStage
    $env:NP_TREE_ATLAS_REFRESH = $oldRefresh
    $env:NP_TREE_ATLAS_SLOTS = $oldSlots
    $env:NP_TREE_ATLAS_VARIANT = $oldVariant
}
