# Audit reuse interior Coop — 15 Oktober 2026

Status: inventaris sebelum modeling. Tidak mengubah model, prefab, scene, atau gameplay. Preview audit dibuat melalui impor FBX di Blender background; bukan eksekusi model Coop. Daftar ini memperbarui perkiraan reuse pada sketch.

## 1. Reuse Barn

Sumber: `ArtSource/BarnInterior_A/Blender/BarnInterior_A.blend`, modul FBX di `ArtSource/BarnInterior_A/Exports/`, statistik di `ArtSource/BarnInterior_A/Previews/MeshManifest.json`.

| Bagian Coop | Asset yang ada | Keputusan |
| --- | --- | --- |
| Dinding merah, jendela, tiang, pengaku | N01_Wall_2m, N01_WindowWall_2m, N01_Post, N01_Brace | Reuse modul, susun sesuai ukuran ruangan Coop |
| Jalur distribusi otomatis Lv3–4 | N05_Pipe_2m, N05_Elbow, N05_Tee, N05_Coupler, N05_Support | Reuse; sesuaikan panjang, diameter dan rute |
| Palung ayam 3 ruang | N02_Trough_3Slots, 132 triangle; N02_FeedFill, 24 triangle | Adaptasi menjadi palung rendah 1.5 × .5 × .3 m, ujung membulat dan fill keemasan terpisah; bukan salin ukuran palung sapi |
| Mesin pakan kecil/besar | N03_Manual 286 triangle; N03_Motor/N03_LargeMotor 466 triangle | Reuse bagian badan, motor, tombol, outlet. Hopper mangkuk biji dan proporsi Coop perlu adaptasi; mesin Barn utuh belum sesuai |
| Meja mesin | A_BottleCounter 36 triangle, beam kayu existing | Adaptasi meja kayu tinggi .9 m dan lebar sesuai mesin |
| Tenggeran | Beam/rangka kayu existing | Rakit modul baru sederhana dari komponen kayu |
| Penyimpanan opsional | A_StorageRack, peti/karung/barel reuse dari source Barn | Pakai seperlunya sesuai referensi; tidak menambah dekor berlebihan |

Gunakan modul sumber berskala dasar. Pembesaran prefab Barn 1.75× sebelumnya tidak otomatis disalin ke Coop. Dimensi sketch Coop adalah ukuran akhir usulan.

## 2. Reuse Toon Farm Pack selain atlas

Root: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/`. Mesh di `Models/`, prefab di `Prefabs/Props/`.

| Asset | Pemakaian | Status |
| --- | --- | --- |
| TFP_Cereal_Sack_01A / 02A | Karung biji dekat mesin | Ada; varian 01A sudah digunakan pada Barn |
| TFP_Wooden_Crate_01A | Peti output pakan/storage | Ada, reuse Barn |
| TFP_Bucket_Metal_01A | Ember opsional | Ada, reuse Barn |
| TFP_Wood_Barrel_01A | Barel opsional | Ada, reuse Barn |
| TFP_Raw_Egg_01A | Telur utuh di penetas dan tray mesin mayones | Bentuk telur utuh sudah dicek dari preview FBX; sesuaikan ukuran/orientasi |
| TFP_Jar_01A / 01B | Toples hasil mayones | Mesh 112 triangle; reuse bentuk, adaptasi UV/warna isi krem dan tutup teal, hilangkan tampilan label yang tidak sesuai |
| TFP_Honey_Jar_01A | Alternatif bentuk toples | Ada, 112 triangle; tidak perlu memakai semua varian |
| TFP_Kitchen_Bowl_02A | Basis bowl mixer/hopper | 164 triangle; adaptasi ukuran, profil dan UV warna metal/teal |
| TFP_Kitchen_Bowl_01A | Alternatif bowl dangkal | 116 triangle; tersedia, pilih sesuai siluet |

`TFP_Egg_01A` dan `TFP_Egg_02A` adalah telur goreng setelah diperiksa visual, sehingga tidak dipakai untuk penetas. Jangan memilih berdasarkan nama Egg saja. Preview kandidat dan statistik mesh sumber disimpan di `ReusePreviews/`. Hitungan source FBX bukan hitungan vertex impor Unity.

## 3. Yang tetap perlu dibuat/dirakit khusus Coop

| Kit | Pekerjaan baru | Bagian yang bisa reuse |
| --- | --- | --- |
| Penetas 1 telur, Lv1–3 | Housing teal, cup telur, hood, panel kecil | Telur utuh Toonpack, meja kayu, palet atlas |
| Penetas 3 telur, Lv4 | Housing lebar dan satu hood dengan **3 cup** | Telur berbagi mesh, bahasa bentuk penetas kecil |
| Mesin pakan kecil & besar | Hopper mangkuk biji, sambungan dan penyesuaian proporsi | Badan/motor/tombol/outlet Barn, bowl kandidat Toonpack, peti |
| Mesin mayones, Lv3–4 | Rangka mixer, paddle, tray input, penataan output | Bowl/toples/telur Toonpack, motor/tombol Barn |
| Feeder otomatis, Lv3–4 | Hopper distribusi dan kepala dispenser berbentuk lonceng | Pipa, siku, T, coupler, tiang Barn |
| Tempat minum ayam | Tangki krem terbalik, tray teal dan dudukan | Material existing dan rangka kayu; palung air sapi bukan pengganti bentuk ini |
| Palung rendah 3 ruang | Adaptasi bentuk/ukuran dari palung Barn | Topologi bersekat + fill existing |
| Tenggeran & meja | Assembly kayu sesuai ukuran mesin | Beam/box existing |
| Lantai dan isi pakan | Susun lantai serta fill sederhana keemasan | Palet tanah/jerami dan fill Barn; tidak membuat butiran pakan satu per satu |

Pencarian nama prefab tidak menemukan prefab khusus incubator, hatch machine, mayonnaise, feeder otomatis atau drinker yang cocok. Ini bukan klaim audit visual setiap prefab dalam pack; mesin di atas tetap direncanakan rakitan khusus berdasarkan referensi. Tidak menambah tangki proses susu atau mesin lain yang tidak diminta. Hewan tidak termasuk pekerjaan ini.

## 4. Atlas dan kaca

**Atlas opaque siap dipakai.** `Models/Materials/TFP_Atlas_1A.mat` memakai `Textures/TFP_Atlas_1A.psd`, GUID `522fcb2b5717c7640b460ee9ff73a78a`. Hash PSD saat audit sama dengan catatan atlas Barn. Material prefab egg/jar yang dicek juga resolve ke material atlas ini. Warna merah, krem, kayu, teal, metal abu dan keemasan mencakup kebutuhan Coop. Blender preview atlas existing tersedia di `ArtSource/Barn_A/References/TFP_Atlas_1A_BlenderPreview.png`. Mesh baru/adaptasi perlu UV ke bagian atlas yang sesuai; tidak perlu atlas runtime baru untuk palet ini.

**Material kaca juga sudah ada:** `TFP_Glass_Windows_1A.mat` dan `TFP_Glass_Windows_1B.mat`. YAML menunjukkan surface transparent, alpha sekitar .392/.188, ZWrite off, beserta texture kaca existing. Jadi hood tidak harus mulai dari material baru. Kecocokan UV/window pattern, transparansi hood di Unity dan overdraw HP tetap perlu diuji pada bentuk penetas nanti; belum diklaim selesai. Jika diperlukan, buat variant lokal khusus Coop tanpa mengubah material bersama Toonpack. Batasi satu lapisan hood dan tanpa lampu realtime per mesin.

## 5. Pembagian level

| Level | Reuse bersama + kit khusus |
| --- | --- |
| 1 — 6 ayam | Shell, karung/peti, meja, tenggeran, drinker, penetas 1 telur, mesin pakan kecil, 2 palung × 3 ruang |
| 2 — 9 ayam | Kit sama; mesin pakan besar; 3 palung |
| 3 — 12 ayam | Mesin pakan besar, penetas 1 telur, mayones, jalur otomatis 4 kepala, 4 palung |
| 4 — 15 ayam | Kit Lv3 dipakai lagi; penetas 3 telur, jalur otomatis 5 kepala, 5 palung |

Penetas satu telur Lv1–3 tetap usulan sketch; kapasitas tiga telur Lv4 adalah permintaan eksplisit. Target triangle layout tanpa ayam masih perkiraan 5k/6.5k/9k/10k; belum ada hasil ukur Coop. Reuse menghemat pembuatan, tetapi tetap memerlukan audit mesh, renderers dan material setelah assembly.
