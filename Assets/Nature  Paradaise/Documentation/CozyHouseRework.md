# Rework eksterior rumah Lv.1 dan atlas bersama

31 Oktober 2026. Eksterior rumah Lv.1 sudah dipasang pada prefab yang dipakai game.
Patokan visual adalah cottage di sisi kanan gambar perbandingan: genteng zaitun,
plester krem, rangka kayu madu, pintu lengkung, cerobong, pondasi batu, bunga,
rambatan, kursi teras, dan lampu. Ini merupakan interpretasi model game dari
referensi tunggal, bukan jaminan hasil identik piksel dengan render referensi.

Revisi UV dan pola genteng (1 November 2026): genteng zaitun kini berupa kepingan kotak bersusun,
bukan pola sisik membulat. Setiap keping mesh mengambil satu keping tekstur utuh.
UV pondasi, tangga, alas tiang, serta cerobong memakai ukuran fisik yang konsisten;
baris batu tetap mendatar, sedangkan permukaan atas tangga memakai paving.
Bentuk mesh atap dan seluruh koordinat slot atlas tetap dipertahankan.

## Hasil yang dipasang

Semua lokasi berikut relatif terhadap `Assets/Nature  Paradaise/` (dua spasi).

| Hasil | Lokasi |
| --- | --- |
| Prefab rumah yang dipakai sistem level | `Prefabs/PlayerHouse/Exterior/HouseExterior_Lv1.prefab` |
| Tiga mesh ekspor | `mesh/Buildings/HouseCozy/HouseCozy_Lv1_LOD0.fbx` hingga `LOD2.fbx` |
| Material bersama | `Material/Shared/NP_CozySurfaces_01.mat`, `NP_CozyFoliage_01.mat`, `NP_CozyGlow_01.mat` |
| PNG dan manifest koordinat UV | `Textures/Shared/Cozy/` |
| Master Blender dengan modul yang bisa diedit | `Source~/HouseCozy/House_Cozy_Master.blend` |
| Sumber GIMP berlapis | `Source~/SharedAtlas/*.xcf` |
| Referensi dan skrip pembuatan ulang | `Source~/HouseCozy/`, `Source~/SharedAtlas/` |
| Prompt pembuatan gambar awal | `Source~/SharedAtlas/ImageGenerationPrompts.md` |
| Preview Unity dan laporan pemeriksaan | `Documentation/CozyHouseReview/` |

Folder `Source~` sengaja tidak diimpor oleh Unity, tetapi tetap disertakan melalui
Git. FBX/PNG adalah berkas yang dipakai runtime. Master House A dan atlas lama
tetap menjadi sumber aset lain selama rework bertahap.

## Atlas modular

`NP_CozyAtlasSlots.json` menyediakan 42 slot permukaan bernama dan grid foliage
4×4. Gunakan `uvSafe` pada manifest untuk menempatkan UV; jangan memindahkan slot
yang sudah digunakan. Margin sumber 8 pixel membantu mengurangi warna bocor.

Permukaan mencakup kayu madu/gelap/krem/merah, ujung balok, papan lantai,
genteng zaitun/abu/merah, plester, batu pondasi/paving, bata, besi, aluminium,
terakota, kain, goni, tali/anyaman, tanah, jerami, pakan, serta palet kecil.
Foliage menyediakan daun, rambatan, dan bunga dengan alpha cutout.
Slot untuk aset berikutnya sudah tersedia; aset tersebut belum otomatis dirework.

| Peta | Sumber | Import mobile Android/iOS |
| --- | --- | --- |
| Warna permukaan | 2048×2048, sRGB | Maksimum 1024, ASTC 6×6 |
| Normal tangent +Y | 1024×1024, linear | Maksimum 1024, ASTC 6×6 |
| Surface mask | 2048×2048, linear | Maksimum 1024, ASTC 6×6 |
| Warna foliage + alpha | 1024×1024, sRGB | Maksimum 1024, ASTC 6×6 |

Surface mask mengikuti URP: R metallic, G occlusion netral, A smoothness.
Normal dibuat dari height tersendiri di GIMP, bukan langsung dari warna albedo.
Material permukaan memakai kekuatan normal 0,32. Foliage memakai alpha cutoff
0,42 dan dua sisi, tanpa transparent blending. Warna/gambar awal dibuat dengan
imagegen lalu dipotong, ditata, dan dilengkapi normal/mask melalui GIMP.

## Model dan biaya tampilan

| LOD | Segitiga | Material | Mulai dipakai pada tinggi relatif layar |
| --- | ---: | ---: | ---: |
| 0 | 12.905 | 3 | 0,18 |
| 1 | 6.085 | 2 | 0,07 |
| 2 | 2.259 | 2 | 0,012 |

Master menyimpan 822 modul untuk kemudahan edit; ekspor tiap LOD digabung menjadi
satu mesh. Beberapa material tetap membutuhkan beberapa draw call. Genteng dan
ornamen yang memengaruhi siluet dipertahankan pada LOD dekat, kemudian dikurangi
untuk jarak jauh. Mesh tidak menyimpan salinan CPU yang bisa dibaca runtime.
Belum ada pengukuran FPS pada perangkat mobile fisik.

## Integrasi game

GUID prefab rumah Lv.1 dipertahankan sehingga referensi Map dan definisi upgrade
tetap mengarah ke prefab yang sama. Skala root 0,6, collider badan/teras/tangga,
volume interaksi pintu, serta titik keluar tidak dipindahkan. Model rumah level
lebih tinggi dan interior belum diganti pada tahap ini.

Lampu `CozyLanternLight` ditempatkan pada lentera model, berwarna hangat,
intensitas 0,8, jangkauan 4, tanpa shadow map. Saat edit lampu nonaktif;
`OutdoorNightLighting` yang sudah ada mengatur nyalanya mengikuti waktu game.
Preview malam adalah pemeriksaan material/lampu di scene uji, bukan screenshot
Map dengan seluruh sistem cuaca aktif.

## Mengedit dan memasang ulang

1. Edit master Blender atau XCF. Perubahan pada XCF harus diekspor ke PNG;
   perubahan master Blender harus diekspor ke ketiga FBX.
2. Pertahankan nama slot/material, koordinat atlas, orientasi depan +Z di Unity,
   dan skala. Skrip sumber tersedia untuk menghasilkan ulang basis aset;
   menjalankannya akan mengganti hasil edit manual pada keluaran yang sama.
3. Untuk memasang ulang setelah ekspor, hentikan Play Mode lalu pilih
   **Nature Paradise → Art → Apply Cozy House Level 1**.
   Prefab utama sudah diterapkan, jadi menu ini tidak wajib dijalankan pada clone.
4. Biarkan Unity menyelesaikan import, lalu mulai Play Mode baru dan cek rumah Lv.1.

Jangan mengirim hanya prefab/FBX: sertakan PNG, material, dan seluruh `.meta`
terkait agar GUID normal, foliage, dan mesh sama di PC client.

## Validasi

Unity 6000.0.81f1 berhasil mengompilasi dan menjalankan apply pada salinan proyek
uji terisolasi. Sebanyak 38 pemeriksaan lulus, mencakup mesh/material/UV,
normal dan mask, referensi prefab, skala/anchor/collider yang dipertahankan,
interaksi dari tangga, serta penolakan interaksi dari belakang rumah.
Hasil teruji kemudian disalin ke proyek utama dan dicocokkan hash-nya.
Preview depan, samping-belakang, LOD jauh, serta malam tersedia untuk review.
Sesi Play dan save milik pengguna tidak digunakan untuk pengujian ini.

Revisi UV menambahkan preview dekat `HouseCozy_Unity_StoneUV.png` dan
`HouseCozy_Unity_RoofUV.png`. `RoofMasonryAudit.txt` memeriksa bahwa piksel di
luar slot genteng tidak berubah, 42 slot atlas tidak berpindah, vertex mesh atap
tetap sama, serta UV dan arah permukaan batu valid. Sumber revisi pola berada di
`Source~/SharedAtlas/revise_roof_gimp.py`; skrip packing menjalankannya setelah
packing awal agar rebuild berikutnya tetap menghasilkan pola genteng kotak.


Revisi 1 November 2026: ujung baris genteng selang-seling dilengkapi sampai tepi;
lapisan bawah dan slab genteng ditutup sehingga arah permukaan aman saat impor.
Sebanyak 200 titik pada masing-masing dari 3 LOD tidak menemukan celah atap.
Preview tambahan tersedia pada HouseCozy_Unity_RoofTop.png. Atlas tidak berubah.
