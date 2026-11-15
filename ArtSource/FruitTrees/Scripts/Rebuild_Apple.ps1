param(
    [string]$Blender = 'C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe',
    [string]$Gimp
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$scratch = Join-Path $project 'tmp\FruitTreeApple'
if (-not (Test-Path -LiteralPath $Blender -PathType Leaf)) {
    throw "Executable not found: $Blender"
}
New-Item -ItemType Directory -Force -Path $scratch | Out-Null

$blenderProfile = $env:BLENDER_USER_RESOURCES
Push-Location -LiteralPath $project
try {
    $env:BLENDER_USER_RESOURCES = Join-Path $scratch 'BlenderProfile'
    & (Join-Path $PSScriptRoot 'Rebuild_Atlas.ps1') -Gimp $Gimp

    $build = Join-Path $PSScriptRoot 'build_apple_blender.py'
    $publish = Join-Path $PSScriptRoot 'publish_apple_unity.py'
    & $Blender -b --factory-startup -t 8 --python-exit-code 1 --python $build --python $publish
    if ($LASTEXITCODE -ne 0) { throw 'Blender could not build or publish the Apple assets.' }

    foreach ($render in 'NP_Apple_Mobile_R13.png', 'NP_Apple_Close_R13.png') {
        $preview = Join-Path $project "ArtSource\FruitTrees\Previews\$render"
        if (-not (Test-Path -LiteralPath $preview -PathType Leaf)) {
            throw "Blender did not create the revised preview: $preview"
        }
        Write-Output "Apple build complete. Revised preview: $preview"
    }
}
finally {
    $env:BLENDER_USER_RESOURCES = $blenderProfile
    Pop-Location
}
