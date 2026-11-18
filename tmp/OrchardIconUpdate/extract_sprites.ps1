$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @"
using System;
public static class OrchardSpriteMatte {
    public static int[] Normalize(byte[] rgba, int width, int height) {
        int left = width, top = height, right = -1, bottom = -1;
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int index = (y * width + x) * 4;
                int original = rgba[index + 3];
                int alpha = Math.Max(0, Math.Min(255, (original - 222) * 255 / 24));
                int low = Math.Min(rgba[index], Math.Min(rgba[index + 1], rgba[index + 2]));
                if (low > 241 && original < 240) alpha = 0;
                rgba[index + 3] = (byte)alpha;
                if (alpha > 60) {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
        }
        if (left >= right || top >= bottom || left < 3 || top < 3 || right >= width - 3 || bottom >= height - 3)
            throw new InvalidOperationException("The icon does not fit inside its grid cell.");
        return new int[] {left - 3, top - 3, right - left + 7, bottom - top + 7};
    }
}
"@
$keys='apple','avocado','banana','cherry','coconut','durian','guava','mango','orange','pear','rambutan'
$folder=Join-Path $PSScriptRoot 'Illustrations'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$source=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'Illustration.png'))
$format=[Drawing.Imaging.PixelFormat]::Format32bppArgb
try {
    $width=[int]($source.Width/4); $height=[int]($source.Height/3)
    for ($i=0; $i -lt $keys.Length; $i++) {
        $key=$keys[$i]
        $region=[Drawing.Rectangle]::new(($i%4)*$width,[int][Math]::Floor($i/4)*$height,$width,$height)
        $cell=$source.Clone($region,$format)
        try {
            $bounds=[Drawing.Rectangle]::new(0,0,$width,$height)
            $data=$cell.LockBits($bounds,[Drawing.Imaging.ImageLockMode]::ReadWrite,$format)
            try {
                $bytes=[byte[]]::new($height*$data.Stride)
                [Runtime.InteropServices.Marshal]::Copy($data.Scan0,$bytes,0,$bytes.Length)
                $crop=[OrchardSpriteMatte]::Normalize($bytes,$width,$height)
                [Runtime.InteropServices.Marshal]::Copy($bytes,0,$data.Scan0,$bytes.Length)
            } finally { $cell.UnlockBits($data) }
            $icon=$cell.Clone([Drawing.Rectangle]::new($crop[0],$crop[1],$crop[2],$crop[3]),$format)
            try { $icon.Save((Join-Path $folder "$key.png"),[Drawing.Imaging.ImageFormat]::Png) }
            finally { $icon.Dispose() }
            "$key $($crop[2])x$($crop[3])"
        } finally { $cell.Dispose() }
    }
} finally { $source.Dispose() }
