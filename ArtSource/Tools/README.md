# Tools: sumber asli dan texture 256

Asset game berada di `Assets/Nature  Paradaise/mesh/Prop/Tools`.

Model dan folder texture memiliki pasangan nama yang sama:

| Model | Texture milik model tersebut |
| --- | --- |
| `Tool_Axe.fbx` | `Tool_Axe.fbm/` |
| `Tool_Brush.fbx` | `Tool_Brush.fbm/` |
| `Tool_FishingRod.fbx` | `Tool_FishingRod.fbm/` |
| `Tool_Hammer.fbx` | `Tool_Hammer.fbm/` |
| `Tool_Hoe.fbx` | `Tool_Hoe.fbm/` |
| `Tool_Milker.fbx` | `Tool_Milker.fbm/` |
| `Tool_Pickaxe.fbx` | `Tool_Pickaxe.fbm/` |
| `Tool_Shears.fbx` | `Tool_Shears.fbm/` |
| `Tool_Sickle.fbx` | `Tool_Sickle.fbm/` |
| `Tool_WateringCan.fbx` | `Tool_WateringCan.fbm/` |

Semua 33 texture game diperkecil dengan **GIMP 3.0.8 / NoHalo** menjadi **256 × 256**, lalu diekspor kembali ke file di folder texture model masing-masing. Format JPEG/PNG dan nama file texture asal dipertahankan untuk kompatibilitas referensi FBX. Tidak ada penggabungan atlas atau perubahan UV/geometri.

`Originals/<NamaTool>/` menyimpan FBX asli dan `Textures/` resolusi 2K/4K sebelum diubah, beserta meta untuk pemulihan GUID. Folder ini berada di luar Unity `Assets`, sehingga sumber besar tidak ikut di-import sebagai texture game. Texture game total berubah dari **42,69 MiB** menjadi **1,33 MiB**.

`TextureManifest.json` mencatat pasangan nama sebelum/sesudah, asal texture, ukuran dan jumlah byte. `UnityImportAudit.txt` mencatat hasil pemeriksaan impor native Unity. Importer game dibatasi ke 256, normal map memakai tipe Normal Map, sedangkan metallic/roughness/RM memakai data linear.

`Scripts/DownscaleGimp.py` adalah catatan proses batch GIMP untuk kumpulan ini. Script dijalankan pada nama impor asal sebelum penggantian nama; jangan menjalankannya langsung pada asset hasil yang sudah diorganisasi. Untuk membuat ulang hasil, kerjakan pada salinan sumber `Originals` dan pertahankan manifest pemetaan.

Model FBX disalin/diubah namanya saja; hash biner dan GUID seluruh 10 FBX cocok dengan sumber asli. Ukuran FBX yang masih besar berasal dari geometri: Pickaxe sekitar 968 ribu vertex dan WateringCan sekitar 1,08 juta vertex saat di-import Unity. Integrasi player memakai mesh turunan di subfolder `Gameplay/`, dengan versi lebih ringan untuk model padat dan pivot di genggaman. FBX asli tetap utuh. Lihat `Documentation/PLAYER_TOOL_VISUALS.md` untuk pemetaan hotbar dan pengaturan Inspector.
