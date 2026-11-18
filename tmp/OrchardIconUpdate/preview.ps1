$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$canvas=[Drawing.Bitmap]::new(1490,534,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics=[Drawing.Graphics]::FromImage($canvas)
$keys='apple','avocado','banana','cherry','coconut','durian','guava','mango','orange','pear','rambutan'
$labels='Apel','Alpukat','Pisang','Cherry','Kelapa','Durian','Jambu','Mangga','Jeruk','Pir','Rambutan'
$base=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#24362f'))
$cell=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#30453b'))
$ink=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#fff2d5'))
$type=[Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Bold)
$title=[Drawing.Font]::new('Segoe UI',22,[Drawing.FontStyle]::Bold)
$center=[Drawing.StringFormat]::new(); $center.Alignment=[Drawing.StringAlignment]::Center
$graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
try {
    $graphics.FillRectangle($base,0,0,$canvas.Width,$canvas.Height)
    $graphics.DrawString('Ikon kebun buah',$title,$ink,20,18)
    for ($col=0; $col -lt $keys.Length; $col++) {
        $x=105+124*$col
        $graphics.DrawString($labels[$col],$type,$ink,[Drawing.RectangleF]::new($x,69,115,30),$center)
        $key=$keys[$col]
        $icons="icon_tree_fruit_$key.png","icon_tree_fruit_${key}_mutation.png","icon_tree_seed_$key.png"
        for ($row=0; $row -lt 3; $row++) {
            $y=104+135*$row
            $graphics.FillRectangle($cell,$x,$y,115,122)
            $sprite=[Drawing.Bitmap]::new((Join-Path (Join-Path $PSScriptRoot 'Icons') $icons[$row]))
            try {
                if ($sprite.Width -ne 256 -or $sprite.Height -ne 256 -or $sprite.GetPixel(0,0).A -ne 0) { throw $icons[$row] }
                $graphics.DrawImage($sprite,$x+4,$y+7,107,107)
            } finally { $sprite.Dispose() }
        }
    }
    foreach($entry in @(@('Buah',104),@('Mutasi',239),@('Bibit',374))) {
        $graphics.DrawString($entry[0],$type,$ink,20,$entry[1]+50)
    }
    $canvas.Save((Join-Path $PSScriptRoot 'NP_Tree_Shop_Icons.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose(); $canvas.Dispose(); $base.Dispose(); $cell.Dispose(); $ink.Dispose()
    $type.Dispose(); $title.Dispose(); $center.Dispose()
}
