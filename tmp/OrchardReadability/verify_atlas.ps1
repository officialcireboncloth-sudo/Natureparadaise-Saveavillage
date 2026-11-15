$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function Read-BitmapBytes([string]$path) {
    $bitmap = [System.Drawing.Bitmap]::new($path)
    try {
        if ($bitmap.Width -ne 4096 -or $bitmap.Height -ne 4096) { throw $path }
        $bounds = [System.Drawing.Rectangle]::new(0, 0, $bitmap.Width, $bitmap.Height)
        $data = $bitmap.LockBits($bounds, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
                                  [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            if ($data.Stride -ne 16384) { throw "Unexpected pixel stride in $path" }
            $buffer = [byte[]]::new($data.Stride * $bitmap.Height)
            [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buffer, 0, $buffer.Length)
            return ,$buffer
        }
        finally { $bitmap.UnlockBits($data) }
    }
    finally { $bitmap.Dispose() }
}

function Get-TileHashes([byte[]]$pixels) {
    $hash = [System.Security.Cryptography.IncrementalHash]::CreateHash([System.Security.Cryptography.HashAlgorithmName]::SHA256)
    try {
        for ($slot = 0; $slot -lt 64; $slot++) {
            $x = ($slot % 8) * 512 * 4
            $y = [int][Math]::Floor($slot / 8) * 512
            for ($row = $y; $row -lt $y + 512; $row++) {
                $hash.AppendData($pixels, $row * 16384 + $x, 512 * 4)
            }
            [Convert]::ToHexString($hash.GetHashAndReset())
        }
    }
    finally { $hash.Dispose() }
}

$project = (Get-Location).Path
$folder = 'Assets\Nature  Paradaise\Textures\Shared\FruitTrees'
foreach ($map in 'BaseColor', 'Normal', 'SurfaceMask') {
    $relative = Join-Path $folder "NP_FruitTrees_Unified_01_$map.png"
    $previous = Get-TileHashes (Read-BitmapBytes (Join-Path $project $relative))
    $current = Get-TileHashes (Read-BitmapBytes (Join-Path (Join-Path $PSScriptRoot "Atlas") (Split-Path $relative -Leaf)))
    $changed = @(0..63 | Where-Object { $previous[$_] -ne $current[$_] })
    if (($changed | Where-Object { $_ -notin (8,9,16,17,20,21,32,33,36,37) }) -or ($map -eq 'BaseColor' -and $changed.Count -ne 10)) {
        throw "$map differs across these atlas tiles: $changed"
    }
    Write-Output "$map | changed tiles: $changed | all other tiles match"
}

