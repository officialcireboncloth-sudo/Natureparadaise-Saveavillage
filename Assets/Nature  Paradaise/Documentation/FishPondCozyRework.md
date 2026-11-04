# Rework Kolam Ikan Cozy — Level 1

Model mengikuti referensi FishPond_Target.png: tepian batu sungai berlumut, dasar batu dan kerikil terendam, dek kayu di depan kiri dengan pagar di belakang, tiga anak tangga, pancuran pada batu di belakang kanan, mesin pakan teal dengan corong kuningan terbuka, baki hasil, tong dan karung gantung pada tiang.

Sumber Blender: ../Source~/FishPondCozy/FishPond_Cozy_Master.blend. Model dan titik interaksi dipasang ke prefab FishPond_Lv1 yang sudah dipakai katalog bangunan dan save. Root scale 0.4 dan tapak 5.6 x 4.4 meter dipertahankan. Seluruh model masuk ke tapak itu. Kapasitas Level 1 tetap 50 ikan. Level 2/3 memakai aset sebelumnya.

Revisi material menambahkan atlas bersama Cozy 03: batu abu-abu, lumut olive, kayu hangat dan kerikil dasar. Warna atlas berukuran 1024; normal dan surface mask 512; kompresi mobile ASTC 6x6. Sumber dibuat memakai built-in imagegen, lalu dikemas di GIMP. Prompt lengkap berada di ../Source~/SharedAtlas/Extension03/Generation.json. UV papan mengikuti ukuran fisik, arah serat horizontal, dan ujung kayu memakai end grain. Setiap papan memakai crop berbeda. Kerikil bawah air dibuat melalui tekstur pada enam bidang agar skalanya kecil dan geometry lebih ringan.

Tiga LOD solid kini 14,142 / 8,184 / 3,640 triangles, dengan 2 / 1 / 1 material slot. Air merupakan mesh mandiri 24 triangles, dengan UV tersendiri. Di Blender berupa template; di Unity memakai FishPondWater_Cozy, varian hijau kebiruan shader Toonpack Toon/CustomWaterSimple yang mempertahankan normal/noise bawaan. Anchor pancuran dan percikan disediakan untuk efek aliran berikutnya. Atlas 01/02, shader bersama, material air lama dan Level 2/3 tetap dipertahankan.

## Verifikasi di Unity asli

Diuji pada salinan proyek terpisah tanpa SaveManager produksi:

- **68 pemeriksaan import, prefab dan collision lolos.** Termasuk normal/winding/UV, semua segitiga tetap dalam satu slot atlas berpading pada ketiga LOD, ukuran tekstur mobile, dasar kerikil menghadap atas, material bersama, bounds tiap LOD, tapak penempatan, water surface terpisah, 18 posisi berenang di luar dek, identitas prefab/pond dan jalur tangga. CharacterController ukuran player berhasil naik ketiga tangga lalu mencapai titik interaksi dek.
- **20 pemeriksaan Play Mode lolos.** Semua tiga level tetap dapat membuka UI kolam, menggunakan komponen mesin pakan, menerima dan mengambil ikan. Penggantian visual Level 1 dan pemulihan record mempertahankan ikan serta stok pakan.
- **41 file yang dilindungi tetap identik**, termasuk katalog bangunan, prefab Level 2/3, metadata prefab Level 1, atlas/material bersama dan material air.
- **23 file prefab, mesh, material, tekstur dan metadata hasil pengujian disalin identik** ke proyek utama.

Hasil review asli Unity berada di FishPondCozyReview: tampak depan, tiga perempat, belakang, atas, dua LOD jauh, Audit.txt, PlayAudit.txt dan MainApply.txt. Tidak ada penulisan save pemain atau scene Map produksi.

Menu apply versi Pond A mengarahkan ke setup Cozy jika manifest ini tersedia agar model baru tetap dipakai.
