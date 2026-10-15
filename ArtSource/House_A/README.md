# House A — empat master exterior low poly

**Selesai dibuat di Blender 4.5.14 LTS dan diterapkan ke Unity pada 12 Oktober 2026.** Mengikuti proposal rumah yang disetujui pengguna, termasuk gambar koreksi Lv5. Revisi kaca bay kanan Lv5 memakai biru solid dari atlas. File sumber bisa dibuka tanpa mencari texture karena satu image atlas sudah packed.

## File utama dan cara membuka

- `Blender/House_A_Masters.blend`: satu file berisi scene `House A Lv1`, `House A Lv2`, `House A Lv3-4`, `House A Lv5`, serta `House A Levels 1-5`. File dibuka pada Lv5. Ganti scene dari dropdown kanan atas untuk melihat desain lainnya.
- Tiap scene punya collection `01 EDITABLE SOURCE`, `02 COMBINED LODS`, `03 PREVIEW STUDIO`, `04 MARKERS`. Source tersembunyi agar tidak muncul bangunan ganda; LOD0 gabungan yang terlihat. LOD1/2 tersimpan tetapi tersembunyi. Aktifkan satu LOD saja saat membandingkan.
- `Exports/`: 12 FBX, empat desain × tiga LOD. Satu mesh per FBX, tanpa camera/light/floor/rig dan tanpa embedded texture. Node memakai nama High/Medium/Low supaya Unity tidak salah mendeteksi rantai LOD otomatis dari satu file.
- `References/House_A_Approved.json`: snapshot koordinat proposal yang di-ACC. Status di snapshot tetap mempertahankan tulisan tahap proposal; status produksi terkini ada di `Previews/MeshManifest.json` dan README ini.
- `References/ApprovedSpecification.md`: spesifikasi dan empat gambar asli pengguna. `TFP_…_BlenderPreview.png` adalah composite PSD asli untuk Blender, **bukan texture runtime baru**.
- `Previews/`: render depan-kanan dan belakang-kiri tiap desain, perbandingan lima level, manifest mesh, serta audit file sumber/import ulang FBX.
- `Scripts/build_house.py`, `render_house.py`, `verify_house.py`: proses yang bisa diulang lewat Blender background. Build membuat mesh dan FBX; render membaca mesh tersimpan; verify mengaudit file yang benar-benar disimpan/diekspor.

## Geometry dan ukuran

Sumbu spesifikasi X kanan pada sketch, Y atas, depan +Z. FBX masuk Unity dengan konversi handedness normal: **kanan pada sketch menjadi local -X saat facade depan +Z**; collider sayap mengikuti konversi yang sama. Blender `(X,-Z,Y)`, depan -Y. Origin semua mesh di tanah pada pusat dinding depan/pintu; lokasi dan rotasi nol, scale satu. Pintu clear 2 × 4 m. Ukuran structure utama dan teras mengikuti snapshot. Lv3/Lv4 memakai **mesh datablock yang sama dan scale sama** dalam scene perbandingan; tidak ada mesh Lv4 duplikat.

| Model | LOD0 aktual | LOD1 aktual | LOD2 aktual | Badan utama W × D |
|---|---:|---:|---:|---:|
| Lv1 | 3.831 tris | 2.167 tris | 589 tris | 9 × 7,5 m |
| Lv2 | 4.661 tris | 2.669 tris | 661 tris | 13 × 8,5 m |
| Lv3–4 | 5.715 tris | 3.191 tris | 907 tris | 9,6 × 8,5 m + sayap |
| Lv5 | 5.030 tris | 3.006 tris | 972 tris | 15 × 8,5 m + bay kaca |

Ini hitungan setelah triangulasi/export, bukan budget perkiraan. Semua berada di bawah budget proposal. Distant LOD mengurangi genteng menjadi strip/plane, menghapus lip kecil, detail batu, bunga dan hardware; frame jendela dibuat plane. Bounds LOD0 mengikuti proposal dengan toleransi 0,051 m. Pada LOD jauh, ujung flower cluster yang dihilangkan mengurangi bounds sedikit; audit membatasi deviasi 0,13 m. Pintu dan silhouette struktur utama tetap di koordinat yang sama.

Penyesuaian produksi yang diperlukan untuk sambungan bersih, tanpa mengubah envelope/pintu/tapak:

- Frame jendela disusun tanpa box koplanar yang bertumpuk di titik silang. Outer frame tetap pada posisi proposal.
- Bagian belakang roof teras dihubungkan ke titik pertemuan dengan slope main roof; rear barge/gable yang tersembunyi dibuang. Muka gable, ridge height dan front eave tetap mengikuti proposal.
- Flue cerobong dibuat berlubang, sesuai spesifikasi; tidak memakai box tertutup placeholder.
- Bay kanan Lv5 memiliki panel biru opaque sesuai revisi pengguna. Inset jendela warm pada facade di belakang panel masih tersimpan pada source, tetapi tertutup panel; ini bukan ruangan gameplay baru.
- Bevel satu segmen pada post/base/trim yang besar; genteng berupa bidang datar + shallow lip, tanpa sculpt atau micro detail.

## Atlas benar-benar dipakai

Opaque dan lamp/window face memakai **image atlas Toon Farm Pack yang sama**, dengan UV di swatch aman 24 × 56 px di dalam sel 32 × 64 px. Source atlas asli `TFP_Atlas_1A.psd` 2048 × 2048, GUID `522fcb2b5717c7640b460ee9ff73a78a`. File PSD pack tidak diubah. Preview PNG di References berasal dari composite PSD itu, bukan gambar/material baru buatan per rumah.

| Slot | Nama material pada FBX | Mapping Unity |
|---|---|---|
| 0 | `TFP_Atlas_1A` | Material pack `TFP_Atlas_1A.mat` (CustomToon) |
| 1 | `TFP_Atlas_Lights_1A` | Material pack `TFP_Atlas_Lights_1A.mat` (URP emission) |

Panel bay kanan Lv5 menggunakan slot 0, **RGB 137/178/197, UV pusat (0,2109375; 0,078125)** pada swatch biru atlas, alpha 1. Material/texture kaca transparan tidak lagi dipakai oleh mesh rumah. Jendela biasa tetap warm pada slot 1. Shader Blender adalah preview Principled; tampilan final mengikuti shader/lighting Unity. Tidak membake albedo/normal/AO baru; tidak membuat image untuk tiap level.

Satu image atlas dipakai bersama oleh empat master dan packed **sekali** di `.blend`. Total raw `.blend` sekitar 8,2 MiB termasuk source editable/LOD/preview image. Semua 12 FBX total sekitar **0,8 MiB**, tanpa embedded texture. Gambar render di Previews untuk review saja, tidak diimport sebagai texture game.

## Verifikasi

`Previews/BlenderSourceAudit.json`: **PASS, 302 pemeriksaan** terhadap saved source dan import ulang 12 FBX. Yang diperiksa meliputi triangles, bounds, mesh count, material name/count, UV atlas termasuk panel biru opaque, degenerate face, outward slate normal, origin/transform, vertex positions sebelum/sesudah export, checksum FBX, atlas source hash, serta sharing mesh Lv3/4. Studio dan marker tidak ikut FBX. `BlueRevision.json` mencatat bahwa revisi warna tidak mengubah geometry.

`Previews/UnityImportAudit.txt`: **APPLY_PASS** untuk 12 mesh, lima prefab/LODGroup, material atlas, dan tiga scene (`Map`, `Map_GameplayPreview`, `TestingScene`). Prefab tersimpan di `Assets/Nature  Paradaise/Prefabs/PlayerHouse/Exterior`, FBX runtime di `Assets/Nature  Paradaise/mesh/Buildings/House_A`. Lv3–4 menunjuk mesh yang sama. Definisi rumah menghubungkan prefab tiap level, dengan reservasi 22 × 17 m. Interior yang sudah di-ACC tetap scene terpisah.

`Stairs_Interact_E` berada pada tangga setiap level dengan trigger sebesar tangga dan radius tambahan 0,8 m. `PlayerHouseExitSpawn` mengikuti teras masing-masing level. Semua facade depan diarahkan ke arah horizontal kamera fixed scene saat apply; bukan billboard yang berputar setiap frame. Visual/portal exterior lama dinonaktifkan. Controller tetap memuat level dari save, dan mematikan collider/portal model sebelumnya segera saat level berganti.

Menu editor: `Nature Paradise > House > Apply Approved House A Exterior`. Apply dapat diulang tanpa menggeser anchor pintu lagi; backup scene awal ada di `Library/HouseExteriorBackups`. FBX memakai konversi handedness standar Unity, tanpa AssetPostprocessor global atau scale negatif; bay kanan tetap di sisi kanan gambar seperti Blender.

`Previews/UnityPlayAudit.txt`: **PLAY_PASS** pada fixture terisolasi tanpa SaveManager. Controller diuji Lv1 → 2 → 3 → 4 → 5 → 1, satu portal/return marker aktif pada frame pergantian, CharacterController naik tangga semua level, penolakan interaksi belakang, mesh bersama Lv3–4, dan transisi interior sebenarnya serta kembali ke approach tangga pada Lv1/Lv5. Preview `House_A_Lv…_Unity.png` dirender dengan material pack di Unity, bukan mockup.
