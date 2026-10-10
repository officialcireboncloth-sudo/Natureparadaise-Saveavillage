# Coop A — exterior low poly

Dibuat di Blender 4.5 dari sketsa coop A yang disetujui pengguna. Cara konstruksi, atlas, preview matte, clean timber joints, format export, dan organisasi mengikuti Barn A. Badan master Lv3 **6 × 7 m**; batas visual **7,35 × 8,70 × 5,74 m**, termasuk roof overhang, ramp ayam dan kotak sarang.

| Mesh | Triangles aktual |
|---|---:|
| LOD0 / High | 3.316 |
| LOD1 / Medium | 1.278 |
| LOD2 / Low | 286 |

Ketiga mesh memakai dua material slots: `TFP_Atlas_1A` dan `TFP_Atlas_Lights_1A` untuk core lampu. Palet berasal dari PSD pack yang sudah ada. **Tidak ada texture runtime baru.** PNG composite untuk Blender dipack ke file sumber; PSD asli tidak diubah.

## File utama dan organisasi

- `Blender/Coop_A_Combined.blend`: file utama, scene **Coop A - Combined Lv3**; satu objek mesh bangunan LOD0 terlihat, LOD1/2 tersembunyi. Studio preview disembunyikan dari viewport dan tidak masuk export.
- `Blender/Coop_A_Master.blend`: arsip komponen editable. Collection memisahkan dinding, atap, fondasi, timber dan hardware.
- `Exports/Coop_A_High.fbx`, `Coop_A_Medium.fbx`, `Coop_A_Low.fbx`: satu mesh per FBX, neutral node names supaya tidak menjadi grup otomatis Unity yang tidak lengkap.
- `References/Coop_A_Approved.json`: snapshot spesifikasi yang di-ACC, ukuran/koordinat/UV. Referensi dan sketsa yang disetujui juga disimpan di sini.
- `References/TFP_Atlas_1A_BlenderPreview.png`: composite atlas untuk Blender saja; bukan pengganti PSD di Unity.
- `Previews/`: render nyata dari mesh gabungan, empat sisi, depan-kanan/belakang-kiri, LOD1/2, perbandingan lima level, `MeshManifest.json` dan `BlenderSourceAudit.json`.
- `Scripts/`: rebuild, penggabungan, export dan audit. `mesh_helpers.py` adalah salinan helper Barn A yang diadaptasi untuk Coop A sehingga sumber ini tetap mandiri.

## Bentuk dan detail

Papan dinding pitch 0,25 m, groove dangkal dan variasi UV palet. Atap LOD0 memakai 8 baris per slope × 12 kolom sepanjang depth, lip tipis dan underside 0,06 m; tidak setiap genteng menjadi box solid. Fondasi dua course memakai batu besar dengan facet bevel sederhana di dalam footprint badan. Tiang/frame memiliki bevel satu segmen; frame yang berpotongan dan brace pintu memakai Boolean union untuk membersihkan bidang tumpang tindih.

Depan: pintu manusia satu daun dan lampu di kiri, satu window half-shutter/kawat di kanan, hatch ayam dengan ramp di bawahnya. Kanan: dua window dan kotak sarang bertutup miring. Kiri: satu window tengah. Belakang: satu vent gable. Jumlah dan penempatan mengikuti sketsa. Kawat berupa diamond lattice tipis sesuai simplifikasi yang telah ditampilkan di proposal. Pintu, shutter dan lid tetap statis di mesh gabungan.

Source editable mempertahankan operand frame/brace di collection `Joint operands`. Operand tersembunyi menghasilkan objek `*_CleanJoint` dengan live exact Boolean + dissolve. Untuk mengedit, buka operand terkait, edit bentuknya, lalu sembunyikan lagi agar hanya hasil clean joint terlihat. File gabungan sudah mengevaluasi semua modifier, menggabungkan bangunan dan melakukan triangulasi.

LOD1 mengurangi shingle, facet batu dan hardware; LOD2 mempertahankan siluet bangunan, trim utama, ramp dan kotak sarang dengan face sederhana. Geometri LOD dibuat di Blender. Unity LODGroup nanti memilih mesh tersebut; Unity tidak otomatis membuat geometry baru dari file ini.

Diagonal pintu tetap memiliki ketebalan di ketiga LOD agar sisi balok terbaca oleh shader toon. Frame distant memakai pemisahan 1 cm untuk menghindari bidang berimpitan di depth buffer Unity.

## Level, satuan dan pivot

Lima level memakai **mesh, UV, material dan jumlah detail yang sama**, hanya uniform scale. Scene kedua **Coop A - Levels 1 to 5 - shared mesh** memakai lima instances yang berbagi satu Mesh datablock.

| Level | Scale XYZ | Badan lebar × dalam |
|---|---:|---:|
| 1 | 0,750 | 4,50 × 5,25 m |
| 2 | 0,875 | 5,25 × 6,125 m |
| 3 | 1,000 | 6,00 × 7,00 m |
| 4 | 1,125 | 6,75 × 7,875 m |
| 5 | 1,250 | 7,50 × 8,75 m |

Satu unit = satu meter. Blender X kanan / Y belakang / Z atas; depan -Y. Unity X kanan / Y atas / Z belakang; depan -Z. Origin semua export berada pada pusat badan di tanah, transform master identity. Konversi FBX `axis_forward=-Z, axis_up=Y` telah diperiksa dengan import ulang ketiga FBX ke Blender.

Mesh sumber Unity tetap depan -Z; wrapper mengoreksi yaw model 180° agar depan bangunan mengikuti panah placement +Z. Door clear Lv1 **1,125 × 2,2125 m**. Cadangan site Lv5 tetap 10 × 13 m. Setelah koreksi arah, Model_Editable bergeser `(0.75,0,-1.625)` dan diberi uniform scale; entrance menjadi `(0.75,0,-1.625) + (1.25,0,4.65) × scale`. Pusat collider sarang ikut berputar. Origin mesh FBX tetap pusat badan di tanah dan yaw site/save tidak diubah. Approved JSON menyimpan ukuran desain dan offset sumber sebelum normalisasi wrapper ini.

## Verifikasi dan status integrasi

Audit membaca ulang file yang tersimpan, memeriksa dimensi/posisi 127 komponen authored, endpoint surface, arah normals dinding/atap, origin, batas visual ketiga LOD, UV masuk palet yang benar, material, triangles aktual, tidak ada degenerate face, lima level berbagi mesh, uniform scale dan marker. Fingerprint geometri memastikan mesh master dan combined sama. FBX diimport ulang dan posisi vertex diperiksa terhadap mesh final agar hanya bangunan yang masuk, tanpa lantai/lampu/kamera studio.

Render preview memakai mesh **gabungan** yang akan diekspor, bukan hanya komponen raw atau sketsa. Material Blender merupakan preview matte; shader game tetap perlu material CustomToon pack ketika diterapkan ke Unity.

**Status: sudah diterapkan dan divalidasi di Unity 6000.0.81f1.** FBX produksi ada di `Assets/Nature  Paradaise/mesh/Buildings/Coop_A/`; prefab Lv1–5 ada di `Assets/Nature  Paradaise/Prefabs/Coop/Exterior/`. Prefab Lv1–4 mempertahankan GUID/root fileID dan terhubung ke Coop Building. Lv5 adalah visual siap pakai; balancing/interior Lv5 belum ditentukan.

Prefab memakai dua material atlas pack yang sudah ada, satu MeshRenderer per LOD, timed crossfade dengan thresholds 28% / 10% / 1,2% screen height dan culling di bawah threshold terakhir. Collider berupa dua box sederhana: badan dan kotak sarang. Tangga dan ramp dekoratif tidak memblokir approach pintu. Referensi model/collider/entrance memakai komponen authoring animal-housing yang telah dipakai oleh portal Barn/Coop.

Coop Building serta dua baris footprint di `Data/Balance/Settings/Buildings.csv` diperbarui ke 10 × 13. Free placement OutsideField menggunakan ukuran tersebut dalam meter; aturan FieldOnly dengan CellSize lain memerlukan konversi bila nanti mode placement diubah. Harga, material cost, durasi, capacity dan unlock tetap data yang sebelumnya.

`Previews/UnityImportAudit.txt` mencatat native import, dimensions, triangles/submeshes, prefab identity, site envelope, LOD/culling dan actual PropertySite restore Lv1–4. Audit menjalankan akses pintu/collision/kind/capacity serta destination CoopInterior pada yaw 0/90/180/270 dalam isolated editor preview scene. Gambar `Coop_A_Unity_LOD0/1/2.png` memakai material Unity; `Coop_A_Unity_Culled.png` memverifikasi frame kosong di jarak culling. Ini pengujian native editor dan jalur runtime yang dipanggil langsung, bukan sesi Play Mode map lengkap.

Menu Unity: **Nature Paradise → Coop → Apply Approved Coop A Exterior** untuk mengulang impor dan audit; **Open Approved Coop A Lv3** membuka prefab utama untuk inspeksi. Tidak perlu mengganti setiap coop yang dibangun melalui sistem data, karena completedPrefab sudah mengarah ke wrapper baru.

## Rebuild

Gunakan Blender pada proses terpisah agar scene pengguna yang terbuka tidak berubah. Sumber helper sudah disimpan lokal; jalankan urutan:

1. `build_coop.py -- --skip-renders`: build master, shared level instances dan mesh LOD, simpan arsip editable.
2. `make_combined.py`: buat file utama gabungan, LOD0 terlihat.
3. `export_lods.py`: export High/Medium/Low ke Exports.
4. `verify_coop.py`: audit saved files dan FBX roundtrip.
5. `render_previews.py`: render dari combined file tanpa menyimpan perubahan scene.

`prepare_sources.py` hanya untuk merekam ulang input desain/helper; tidak diperlukan setiap rebuild. Seluruh proses menulis hanya ke folder Coop_A. Backup `.blend1` hasil iterasi dapat dihapus setelah audit sumber final berhasil karena editable master dan file gabungan tetap tersedia.
