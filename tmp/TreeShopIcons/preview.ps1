$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::new(1140, 462)
$canvas = [System.Drawing.Graphics]::FromImage($bitmap)
$canvas.Clear([System.Drawing.Color]::FromArgb(51, 62, 48))
$canvas.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$canvas.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$ink = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(248, 238, 213))
$tile = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(65, 76, 57))
$head = [System.Drawing.Font]::new('Segoe UI Semibold', 22)
$label = [System.Drawing.Font]::new('Segoe UI Semibold', 13)
$row = [System.Drawing.Font]::new('Segoe UI', 12)
$center = [System.Drawing.StringFormat]::new()
$center.Alignment = [System.Drawing.StringAlignment]::Center
$species = 'apple','avocado','cherry','guava','mango','orange','coconut'
$names = 'Apel','Alpukat','Cherry','Jambu biji','Mangga','Jeruk','Kelapa'
try {
    $canvas.DrawString('Ikon pohon buah', $head, $ink, 25, 18)
    $canvas.DrawString('Bibit pohon', $row, $ink, 25, 68)
    $canvas.DrawString('Buah panen', $row, $ink, 25, 265)
    for ($i = 0; $i -lt $species.Count; $i++) {
        $left = 25 + $i * 158
        $canvas.FillRectangle($tile, $left, 101, 142, 133)
        $canvas.FillRectangle($tile, $left, 295, 142, 140)
        foreach ($kind in 'seed','fruit') {
            $top = if ($kind -eq 'seed') { 101 } else { 293 }
            $path = Join-Path $PSScriptRoot ('icon_tree_{0}_{1}.png' -f $kind,$species[$i])
            $icon = [System.Drawing.Image]::FromFile($path)
            try { $canvas.DrawImage($icon, $left + 6, $top, 130, 133) }
            finally { $icon.Dispose() }
        }
        $canvas.DrawString($names[$i], $label, $ink,
            [System.Drawing.RectangleF]::new($left, 235, 142, 29), $center)
    }
    $bitmap.Save((Join-Path $PSScriptRoot 'NP_Tree_Shop_Icons.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    foreach ($drawable in $center,$head,$label,$row,$ink,$tile,$canvas,$bitmap) { $drawable.Dispose() }
}
