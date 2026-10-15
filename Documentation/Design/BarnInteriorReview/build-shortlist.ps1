$ErrorActionPreference='Stop'
$audit=Get-Content (Join-Path $PSScriptRoot 'asset-inventory.json') -Raw | ConvertFrom-Json
$rows=@()
$groups=@(
 @{component='Tong kayu';status='Reuse';names=@('TFP_Wood_Barrel_01A')},
 @{component='Ember metal';status='Reuse';names=@('TFP_Bucket_Metal_01A')},
 @{component='Peti kayu';status='Reuse';names=@('TFP_Wooden_Crate_01A','TFP_Wooden_Crate_03A')},
 @{component='Karung pakan';status='Reuse';names=@('TFP_Cereal_Sack_01A','TFP_Cereal_Sack_02A')},
 @{component='Bal jerami';status='Reuse limited / mobile optimization';names=@('TFP_Hay_Bale_01A','TFP_Hay_Bale_04A')},
 @{component='Jerami isi / patch';status='Adapt scale and density';names=@('TFP_Hay_Clump_03A','TFP_Hay_Patch_01A')},
 @{component='Bak air';status='Reuse substitute / adapt feet';names=@('TFP_Trough_03A','TFP_Trough_04A')},
 @{component='Palung pakan 3 sekat';status='New matching kit; existing candidates differ';names=@('TFP_Trough_02A','TFP_Trough_05A')},
 @{component='Papan alat';status='Reuse subset / optimize';names=@('TFP_Tool_Rack_01A','TFP_Tool_Rack_10A')},
 @{component='Alat tangan';status='Reuse';names=@('TFP_Spading_Fork_01A','TFP_Shovel_01A')},
 @{component='Rak penyimpanan';status='Assemble / adapt, not standalone warehouse rack';names=@('TFP_Shelf_01A','TFP_Shelf_02A')},
 @{component='Lemari perlengkapan';status='Adapt';names=@('TFP_Kitchen_Cabinet_02A','TFP_Bathroom_Cabinet_01A','TFP_Tool_Storage_01A')},
 @{component='Wastafel';status='Adapt industrial base';names=@('TFP_Kitchen_Cabinet_Sink_01A','TFP_Bathroom_Sink_01A')},
 @{component='Botol / wadah susu';status='Reuse';names=@('TFP_Bottle_01A','TFP_Milk_Bottle_01A','TFP_Milk_Chug_01A')},
 @{component='Lampu';status='Substitute or reuse prior custom Chicken Store lantern';names=@('TFP_Lamp_02A','TFP_Lamp_03A')},
 @{component='Tangki proses';status='New; water towers differ';names=@('TFP_Water_Tank_01A','TFP_Water_Tank_03A')},
 @{component='Shell interior';status='New/adapt modular room; loft prefab is not full interior';names=@('TFP_Barn_05A_Interior','TFP_Barn_05A_Module_1')}
)
foreach($g in $groups){foreach($name in $g.names){
 $asset=@($audit.candidates | Where-Object {$_.name -eq $name})
 if($asset.Count -ne 1){throw "Missing or ambiguous asset: $name"}
 $rows += [pscustomobject]@{Component=$g.component;Decision=$g.status;Prefab=$name;Triangles=$asset[0].trianglesAllMeshFilters;ImportedVertices=$asset[0].verticesAllMeshFilters;Path=$asset[0].path;Preview=$asset[0].preview}
}}
$rows | Export-Csv -LiteralPath (Join-Path $PSScriptRoot 'asset-shortlist.csv') -NoTypeInformation -Encoding UTF8
"Shortlist saved: $($rows.Count) prefab candidates; native inventory contains $($audit.candidates.Count) entries."
