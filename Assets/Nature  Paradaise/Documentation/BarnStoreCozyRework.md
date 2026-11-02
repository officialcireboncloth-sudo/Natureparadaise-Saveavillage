# Barn Store — exterior cozy

1 November 2026. Acuan adalah **Barn Store kanan bawah** pada gambar perbandingan.
Bangunan kandang ternak pada gambar atas bukan target pekerjaan ini.

## Hasil

Atap utama slate dengan gable depan, rangka kayu madu dan papan merah, shutter
loteng silang, pintu ganda berkaca dengan engsel/pull logam, etalase kiri dan
flower box, papan BARN STORE, dua lampu, rooster vane, planter, karung, paket
pakan, garpu, sekop, ember aluminium dan rak display yang terbuka di depan dengan tembok kanan tertutup.
Sisi dan belakang dilengkapi agar dapat dilihat di dunia game. Gambar depan
menjadi acuan bentuk, susunan dan warna; pencahayaan render aset terisolasi
berbeda dari pencahayaan Map yang mengikuti jam/cuaca.

## Berkas

Semua lokasi berikut relatif terhadap `Assets/Nature  Paradaise/` (dua spasi).

| Isi | Lokasi |
| --- | --- |
| Blender master modular | `Source~/BarnStoreCozy/BarnStore_Cozy_Master.blend` |
| Sumber pembentukan ulang, manifest, gambar acuan | `Source~/BarnStoreCozy/` |
| Tiga FBX | `mesh/Buildings/BarnStoreCozy/` |
| Prefab produksi | `Prefabs/FarmReference/Modules/Farm_BarnStore_B.prefab` |
| Preview Unity dan audit | `Documentation/BarnStoreCozyReview/` |
| Penerapan ulang Editor | `Nature Paradise > Art > Apply Cozy Barn Store` |

Master menyimpan bagian editable pada koleksi `01 EDITABLE HOUSE MODULES`,
mesh ekspor pada `02 GAME LODS`, serta studio dan anchor yang tidak diekspor.
Sumber menggunakan helper bersama `Source~/PoultryCozy/build_poultry_cozy.py`
dan `Source~/HouseCozy/build_house_cozy.py`; sertakan keduanya saat handoff.

## Atlas dan performa

Menggunakan tiga material bersama NP_CozySurfaces_01, NP_CozyFoliage_01 dan
NP_CozyGlow_01; tidak menambah tekstur unik per bangunan. Warna, normal,
surface mask dan foliage memakai atlas cozy yang sudah ada. Profil mobile
atlas tetap maksimum 1024 dengan ASTC 6x6. Mesh tidak menyimpan salinan CPU.

| LOD | Segitiga | Material |
| --- | ---: | ---: |
| 0 | 29.242 | 3 |
| 1 | 9.424 | 2 |
| 2 | 4.045 | 2 |

Ambang LOD 0,23 / 0,085 / 0,012. Dua point light hangat tidak memakai shadow
map dan awalnya nonaktif, mengikuti scheduler lampu malam yang sudah ada.
FPS pada perangkat mobile fisik belum diukur.

## Verifikasi

Unity 6000.0.81f1 mengimpor dan merender prefab pada salinan pengujian terisolasi.
Audit memeriksa skala meter, arah facade, jumlah segitiga, UV atlas, material,
LOD, collider lama, jalur pintu, display kanan, shader dan lampu. Lihat laporan
`UnityApplyAudit.txt` beserta preview depan, sudut kanan, belakang, LOD2 dan malam.
Root/GUID serta collider lama dipertahankan; tembok kanan mendapatkan satu collider solid baru. FarmReference_Group, seluruh
prefab exterior barn milik player dan definisi Barn Building diverifikasi
melalui hash sebelum/sesudah. Scene Map dan data save tidak ditulis.


Revisi 1 November 2026: sisi luar kanan ditutup papan tembok merah sampai
bawah atap pada semua LOD. Rak depan tetap terlihat. Cek Unity memastikan sisi
kanan menahan player, sementara jalur pintu dan akses rak depan tetap terbuka.
Audit terbaru berisi 32 pemeriksaan lulus.
