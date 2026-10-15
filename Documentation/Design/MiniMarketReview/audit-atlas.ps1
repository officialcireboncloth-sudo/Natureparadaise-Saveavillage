$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$previous=Get-Content (Join-Path $root 'Documentation/Design/ChickenStoreReview/atlas-audit.json') -Raw | ConvertFrom-Json
$hash=(Get-FileHash -LiteralPath (Join-Path $root $previous.sourcePSD) -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $previous.sourceSha256) {throw 'Atlas changed; inspect again before reusing audit.'}
$bitmap=[System.Drawing.Bitmap]::new((Join-Path $root 'ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png'))
$swatches=[ordered]@{}
foreach($name in @('plaster','timber','wood_shadow','foundation','metal','lantern','window','awning_ivory')) {
 $s=$previous.swatches.$name
 $c=$bitmap.GetPixel($s.pixel[0],$s.pixel[1])
 if ($c.R -ne $s.rgb[0] -or $c.G -ne $s.rgb[1] -or $c.B -ne $s.rgb[2]) {throw "Swatch mismatch: $name"}
 $swatches[$name]=$s
}
# Choose actual plain palette cells, not foliage tiles, near the reference olive colors.
foreach($target in @(@{name='olive_coping';rgb=@(82,105,73)},@{name='olive_awning';rgb=@(108,127,85)},@{name='leaf_emblem';rgb=@(126,164,48)})) {
 $best=$null;$score=[double]::MaxValue
 for($y=1056;$y -lt 2048;$y+=64) {
  for($x=16;$x -lt 1024;$x+=32) {
   $c=$bitmap.GetPixel($x,$y)
   if ($c.G -le $c.R -or $c.G -le $c.B) {continue}
   $s=[math]::Pow($c.R-$target.rgb[0],2)+[math]::Pow($c.G-$target.rgb[1],2)+[math]::Pow($c.B-$target.rgb[2],2)
   if($s -ge $score){continue}
   $score=$s
   $best=[ordered]@{pixel=@($x,$y);rgb=@($c.R,$c.G,$c.B);safeUvBottomLeft=@((($x-12)/2048.0),((2048-$y-28)/2048.0),(($x+12)/2048.0),((2048-$y+28)/2048.0))}
  }
 }
 if($null -eq $best){throw 'No suitable green swatch'}
 $swatches[$target.name]=$best
}
$bitmap.Dispose()
$audit=[ordered]@{status='EXISTING_ATLAS_SUPPORTS_STYLIZED_EXTERIOR';date='2026-10-14';sourcePSD=$previous.sourcePSD;sourceGUID=$previous.sourceGUID;sourceSha256=$hash;dimensions=$previous.dimensions;material=$previous.material;swatches=$swatches;limitations=@('Palette sampling supports stylized colors; it does not reproduce reference wood grain or roof panel artwork.','SUPERMARKET text and leaf emblem require mesh geometry; the atlas does not contain that exact storefront sign.','Opaque amber glass proposed; transparent furnished interior is outside the exterior scope.')}
$audit | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'atlas-audit.json') -Encoding UTF8
$swatches.GetEnumerator() | ForEach-Object {"$($_.Key): $($_.Value.rgb -join ', ')"}
