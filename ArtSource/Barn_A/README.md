# Barn A — exterior low poly

Dibuat di Blender dari sketsa exterior A yang sudah disetujui. Master adalah
Lv3: badan 8 × 10 m, batas visual 8,9 × 10,9 × 6,8 m. Lima level memakai mesh,
material, UV dan detail yang sama; hanya uniform scale yang berubah.

Jumlah triangles aktual: LOD0 **3.938**, LOD1 **1.788**, LOD2 **368**.
Ukuran, origin, UV, arah permukaan dan import ulang ketiga FBX lolos audit;
hasil lengkap tersimpan di `Previews/BlenderSourceAudit.json`.

| Level | Scale XYZ | Badan lebar × kedalaman |
|---|---:|---:|
| 1 | 0,750 | 6 × 7,5 m |
| 2 | 0,875 | 7 × 8,75 m |
| 3 | 1,000 | 8 × 10 m |
| 4 | 1,125 | 9 × 11,25 m |
| 5 | 1,250 | 10 × 12,5 m |

## Folder

- `Blender/Barn_A_Combined.blend`: file utama gabungan; satu objek mesh bangunan
  yang terlihat. LOD1 dan LOD2 disimpan tersembunyi di collection yang sama.
- `Blender/Barn_A_Master.blend`: arsip file mentah part terpisah yang dapat diedit; atlas preview
  sudah dipack ke dalam file sehingga tidak hilang ketika file dipindah.
- `Exports/`: FBX LOD0, LOD1 dan LOD2, satu mesh per file, maksimum dua material.
- `References/`: ukuran/sketsa yang di-ACC serta composite atlas asli untuk
  Blender. PNG atlas di sini **hanya untuk Blender**, bukan texture game baru.
- `Scripts/build_barn.py`: sumber rebuild yang membaca ukuran dari JSON.
- `Scripts/verify_barn.py`: pemeriksaan file tersimpan dan import ulang FBX.
- `Previews/`: render sisi bangunan, perbandingan level, manifest triangles dan
  hasil pemeriksaan. Angka aktual ada di `MeshManifest.json`.

## Membuka dan mengedit

File gabungan memakai scene `Barn A - Combined Lv3`. Pilih `Barn_A_Master_LOD0`
untuk memindahkan atau mengedit satu bangunan sekaligus. Dua material atlas
tetap berada dalam satu mesh; tidak menjadi objek bangunan terpisah.

File part mempunyai dua scene. `Barn A - Editable Master Lv3` berisi komponen master,
sedangkan `Barn A - Levels 1 to 5 - shared mesh` menunjukkan kelima ukurannya.
Collection `01 EDITABLE` mengelompokkan dinding, atap, fondasi, timber dan hardware.

Komponen rangka yang bertemu tetap tersimpan sebagai operand di
`Joint operands`. Hasil `*_CleanJoint` memakai Boolean union dan dissolve
coplanar; ini menghilangkan bidang yang bertumpuk. Untuk mengedit joint, buka
operand yang disembunyikan, ubah mesh sumber, lalu sembunyikan lagi agar hanya
hasil union yang ditampilkan. Bagian lain dapat diedit langsung.

Collection `02 EXPORT MESHES` berisi mesh hasil yang digabung dan ditriangulasi.
Collection `03 UNITY MARKERS` berisi pivot tanah dan posisi masuk.
Collection `04 PREVIEW STUDIO` adalah lantai, lampu dan kamera preview; semuanya
dikecualikan dari FBX.

Blender memakai X kanan, Y belakang, Z atas; depan -Y. Satu unit = satu meter.
Origin master di tengah tapak pada tanah, transform export identity.
FBX memakai axis forward -Z/up Y; import kembali ke Blender sudah diperiksa
untuk ukuran dan origin. Material Blender adalah preview matte, bukan shader
CustomToon Unity.

## Material Unity

Saat diimport ke Unity, remap material berdasarkan nama:

- `TFP_Atlas_1A` → material pack `TFP_Atlas_1A.mat` yang sudah ada.
- `TFP_Atlas_Lights_1A` → material pack `TFP_Atlas_Lights_1A.mat` untuk core lampu.

Keduanya memakai atlas pack GUID `522fcb2b5717c7640b460ee9ff73a78a`. Jangan
membuat salinan texture PNG preview atau material per level di Assets.
UV semua face diarahkan ke sel palet yang telah diaudit. Tidak ada normal,
roughness, AO atau albedo baru untuk runtime.

FBX sudah diimport ke `Assets/Nature  Paradaise/mesh/Buildings/Barn_A` sebagai
`Barn_A_High`, `Barn_A_Medium`, `Barn_A_Low`. Nama import netral mencegah Unity
mengira file LOD tunggal sebagai grup otomatis yang tidak lengkap.

Prefab `Prefabs/Barn/Exterior/BarnExterior_Lv1` sampai `BarnExterior_Lv5`
sudah memakai mesh baru. Prefab lama Lv1–4 mempertahankan GUID dan root-nya;
Building Definition tetap menunjuk prefab yang sama, sehingga build, upgrade,
dan load save memakai exterior baru. Harga, material biaya dan kapasitas lama
tetap. Lv5 tersedia sebagai prefab exterior, tetapi belum menjadi opsi upgrade
karena data gameplay barn baru mempunyai empat level.

Setiap prefab memakai satu MeshRenderer per LOD (dua slot material), satu
BoxCollider badan, marker entrance +Z, dan LODGroup. Mesh sumber tetap depan -Z;
wrapper Unity mengoreksi yaw model 180° agar pintu mengikuti panah placement +Z.
Entrance lokal `(0,0,5.9) × scale` berada di depan pintu, tanpa mengganti yaw site/save.
LOD0/1/2 dibuat di Blender;
Unity memilih otomatis, bukan menghasilkan topology baru. Batas tinggi layar
28% / 10% / 1,2%, dengan crossfade. Di bawah 1,2% bangunan dicull. Kelima prefab
berbagi tiga mesh dan material yang sama. Footprint data/CSV 12 × 14 grid
mereservasi envelope ukuran terbesar untuk ruang upgrade.

Menu reapply: `Nature Paradise > Barn > Apply Approved Barn A Exterior`.
`UnityImportAudit.txt` memuat hasil pemeriksaan native import, tiga render LOD,
culling jarak jauh dan pemulihan PropertySite level 1–4. Pengujian pemulihan
memanggil jalur runtime asli pada preview scene terisolasi, termasuk pintu
bangunan diputar 0/90/180/270°. Ketiga LOD di kelima level diperiksa arah depannya
terhadap +Z placement. Tidak mengubah save player atau layout Map.

## Rebuild

Jalankan script lewat Blender Python pada proses terpisah. Script memulai scene
baru pada proses itu, membangun ulang file hasil di folder Barn_A, dan tidak
mengubah scene terbuka pada proses Blender lain. `-- --skip-renders` dapat
dipakai setelah script untuk melewati pembuatan ulang PNG ketika perubahan
hanya mengenai topology. Pemeriksaan harus tetap dijalankan setelah rebuild.
Setelah rebuild jalankan `Scripts/make_combined.py` untuk file gabungan, lalu
menu reapply Unity untuk menyegarkan mesh/prefab. `export_unity_lods.py` dapat
mengekspor ulang tiga mesh dari file gabungan tanpa membangun ulang komponen.
