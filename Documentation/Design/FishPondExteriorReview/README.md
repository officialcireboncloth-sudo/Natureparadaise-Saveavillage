# Kolam ikan A — sketsa Lv1–3 untuk ACC

**Update 2026-10-13: model Blender Lv1–3 sudah di-apply ke Unity.** Prefab lama terhubung ke mesh baru, air memakai shader/material dari Toon Pack, kapasitas 50/100/250, dan interaksi dock serta data ikan lolos pemeriksaan Play Mode. File sumber, 12 FBX, LOD, preview dan audit ada di [ArtSource/FishPond_A](../../../ArtSource/FishPond_A/README.md). Isi usulan dan JSON di bawah dipertahankan sebagai arsip sketsa yang menjadi acuan produksi.

**Perbaikan pemasangan:** preview hanya visual dan belum dapat membuka UI/menulis data kolam. Tapak memakai ukuran level masing-masing dan mengikuti rotasi; seluruh tapak harus masuk field yang bersih. Tanah dicangkul tanpa tanaman dapat dipakai. Upgrade memperluas reservasi jika area tambahan kosong. Pengujian alur preview, konfirmasi, dan upgrade Lv1–3 lolos 28 pemeriksaan; audit ada di `ArtSource/FishPond_A/Previews/UnityPlacementAudit.txt`.

**Status saat sketsa dibuat: usulan untuk ACC.** Angka ukuran berikut merupakan keputusan desain, bukan ukuran yang dapat diukur pasti dari foto. Tiga referensi pengguna tersimpan di `References`. Gambar Lv1 dan Lv2 memperlihatkan bentuk yang sama; pembesaran Lv2 dan penambahan penabur mengikuti keterangan pengguna. Tampak belakang, dasar basin, dan posisi kontrol yang tidak terlihat pada referensi adalah rancangan usulan.

Desain memakai batu dengan sudut lembut, dock dari papan kayu hangat, mesin hijau kebiruan, dan besi gelap. Detail dibuat pada geometri besar seperti barn, coop, dan rumah yang sudah ada: tanpa sculpt realistis, micro-detail, texture 4K baru, atau material terpisah per batu. Semua gambar dan ukuran dihitung dari `pond-proposal-A.json`; setelah ACC, JSON ini menjadi acuan penempatan 1:1 untuk model Blender.

## Gambar review

- `pond-levels-overview.png`: ketiga level dalam skala gambar yang sama.
- `pond-Lv1/2/3-front-right.png` dan `pond-Lv1/2/3-rear-left.png`: volume depan-kanan dan belakang-kiri tiap level.
- `pond-Lv1/2/3-elevations.png`: tampak depan, kanan, belakang, kiri beserta batas visual.
- `pond-Lv1/2/3-plan.png`: tapak, grid 1 m, lokasi mesin, dock, tangga, area akses, dan marker interaksi.
- `pond-section-and-foundation.png`: potongan dasar, air, bibir batu, dock, dan terrain.
- `pond-machines-and-dock.png`: pembuat pakan, penabur otomatis, dock, dan dimensi komponen.
- `pond-atlas-and-materials.png`: sampel PSD atlas asli dan rencana material air Toon Pack.
- `pond-proposal-A.json`: seluruh komponen dengan ID unik, posisi XYZ, dimensi/vertex, grup, material, batas visual, marker, kapasitas, dan budget.
- `review-validation.json`: pemeriksaan koordinat, clearance, bahan, dan artefak sketsa; bukan validasi mesh Blender atau Play Mode.

## Ukuran utama

Satu unit = satu meter. **X kanan, Y atas, depan -Z, belakang +Z.** Origin di tengah reservasi tanah `(0,0,0)`; pusat basin bergeser X +1,50 m agar mesin di kiri tetap seimbang dalam footprint. Origin ini sama pada semua level. Saat apply, facade kontrol dan tangga diarahkan ke kamera world fixed; model tidak menjadi billboard yang berputar setiap frame.

| Level | Kapasitas | Basin luar W × D | Air W × D sebelum dock | Luas air geometris | Dock W × D | Mesin |
|---|---:|---:|---:|---:|---:|---|
| 1 | 50 ekor | 8 × 6 m | 6,8 × 4,8 m | 32,517 m² | 3,6 × 2,6 m | Pembuat pakan; barrel pakan manual |
| 2 | 100 ekor | 10 × 7,5 m | 8,8 × 6,3 m | 55,190 m² | 3,6 × 2,6 m | Pembuat pakan + penabur otomatis |
| 3 | 250 ekor | 14 × 10,5 m | 12,8 × 9,3 m | 118,550 m² | 4,2 × 3 m | Pembuat pakan + penabur; kotak suplai kecil |

Luas air dihitung dari polygon 16 titik dengan sudut membulat; bukan W×D penuh. Luas tersebut termasuk air di bawah dock. Area berenang saat implementasi harus mengecualikan dock/tiang dan diberi margin dari dinding. Kapasitas adalah aturan gameplay; ukuran ini tidak mengklaim kepadatan biologis kolam nyata.

| Level | Batas visual total W × D × H | Reservasi tanah + akses usulan |
|---|---:|---:|
| 1 | 11,742 × 7,445 × 2,790 m | 14 × 11 m |
| 2 | 13,744 × 8,945 × 2,990 m | 16 × 13 m |
| 3 | 17,742 × 11,945 × 2,990 m | 20 × 16 m |

Batas visual mencakup basin, batu yang sedikit menonjol, daun jarang, dock, tangga, mesin, karung, tray, inlet, dan outlet. Mesin dan tinggi air **tidak ikut membesar** dengan scale kolam. Tiga model memakai scale `(1,1,1)`; pond dan jumlah batu dibangun menurut ukuran level masing-masing, komponen mesin dipakai ulang.

## Basin, air, dan terrain

Kolam saat ini bisa dipindah melalui sistem free placement. Usulan ini memakai **basin dangkal sedikit terangkat**, agar tidak perlu mengukir lubang Terrain setiap kali dibangun, dipindah, di-upgrade, atau dibongkar.

| Komponen | Ukuran / tinggi |
|---|---|
| Landing pad tanah | Y 0,00 m, rata; variasi tanah maksimum ±0,08 m perlu dicek sebelum placement |
| Dasar basin | Y +0,10 m; bidang opaque, normal ke atas; seluruh area di bawah air tertutup |
| Batu dasar | 5/7/10 boulder tipis di luar area dock; sekitar 0,70 × 0,45 × 0,14 m, pusat Y +0,19 m, puncak +0,26 m |
| Air | Y +0,65 m, datar; kedalaman visual 0,55 m |
| Liner | Dinding kontinu dari Y +0,10 sampai +0,82 m; menutup terrain dan gap antar batu |
| Coping / alas batu | Cincin mengikuti 16 titik outer dan inner, Y +0,78 m |
| Bibir boulder | Tebal sekitar 0,82 m; puncak nominal sekitar +0,90 m, variasi kecil; base sedikit masuk tanah untuk menutup seam |
| Radius sudut outer | Lv1 0,95 m / Lv2 1,10 m / Lv3 1,30 m |
| Inset polygon air | 0,60 m dari batas outer, radius sudut inner outer minus 0,60 m |
| Boulder | Sampling perimeter 32/40/56 posisi sebelum gap tangga; 8 sisi + satu ring bevel, variasi tinggi sekitar 0,76±0,10 m |
| Dock atas | Y +0,95 m pada permukaan papan; nominal deck +0,90 m; tebal papan 0,10 m |
| Inlet | Lv1/Lv2 satu di belakang; Lv3 dua, kiri dan kanan belakang; puncak tower sekitar 1,65 m |
| Overflow | Satu di kanan depan; mouth/trough rendah, mengalir ke saluran drain kecil, bukan simulasi banjir Terrain |

Dasar berada di atas landing pad, sehingga air transparan memperlihatkan liner/dasar kolam, bukan rumput Terrain. Tidak cukup membuat plane biru di atas terrain. Terrain yang miring atau bergelombang di luar toleransi harus diratakan pada pad atau ditolak; air dan dock tetap horizontal. Ini perlu pemeriksaan penempatan saat implementasi, bukan klaim bahwa sistem placement sekarang sudah melakukannya.

## Dock dan jalur player

Player project saat ini di-fit setinggi sekitar **3,6 m**. Mesin maker puncak 2,78 m dan penabur sekitar 2,99 m membuat kontrol/outlet masih berada di area tangan/badan player; bukan skala untuk manusia 1,7 m yang diterapkan tanpa penyesuaian.

Tangga berada di depan-kiri menghadap -Z, lebar **2 m**, tiga anak tangga dengan rise dan tread **0,30 m**. Ini berada di bawah step offset 0,35 m fixture player yang dipakai pada review sebelumnya; berjalan nyata tetap perlu diuji dengan controller prefab saat apply. Rim boulder diberi gap tangga; liner yang berada di bawah deck tetap menutup basin. Tiang 0,22 m pada sudut dan rail 0,10 m hanya di belakang, bukan melintang di pintu masuk.

Lorong tengah dock minimal **1,40 m**. Barrel di kanan belakang; penabur di kiri belakang. Spout penabur mengarah **+Z ke air di belakang dock**, melewati atas rail. Jangan mengarahkannya ke tengah dock, karena pakan akan jatuh ke papan dan spout mengganggu jalur jalan.

| Marker / station | Lv1 XYZ | Lv2 XYZ | Lv3 XYZ |
|---|---|---|---|
| Maker dasar | (-4,20; 0; -1,45) | (-5,20; 0; -2,20) | (-7,20; 0; -3,70) |
| Maker titik berdiri M | (-4,20; 0; -3,50) | (-5,20; 0; -4,25) | (-7,20; 0; -5,75) |
| Dock marker UI kolam P | (-0,55; 0,90; -2,00) | (-1,55; 0,90; -2,75) | (-3,25; 0,90; -4,25) |
| Approach tangga di tanah | (-0,55; 0; -4,60) | (-1,55; 0; -5,35) | (-3,25; 0; -6,85) |
| Barrel dasar | (0,60; 0,90; -1,20) | (-0,40; 0,90; -1,95) | (-1,85; 0,90; -3,05) |
| Penabur dasar | Tidak ada | (-2,80; 0,90; -2,00) | (-4,65; 0,90; -3,10) |
| Emitter penabur | Tidak ada | (-2,80; 1,90; -1,10) | (-4,65; 1,90; -2,20) |

P/M/F pada plan adalah label fungsi, bukan tombol keyboard baru. UI existing tetap menjadi acuan binding. Interaksi perlu memakai marker/station collider, bukan jarak dari root kolam yang berada jauh dari dock pada Lv3. Setelah upgrade memperbesar basin, player yang berdiri pada bekas dock harus dipindahkan ke approach aman sebelum visual berganti, agar tidak tiba-tiba berada di dalam basin.

## Mesin pembuat pakan

Satu mesin yang sama dipakai pada ketiga level, berada di tanah sebelah kiri depan, tidak menutup jalur tangga.

| Komponen | Spesifikasi |
|---|---|
| Casing | 1,45 × 1,45 × 1,10 m (X×Y×Z), pusat Y 1,22 m; sudut bevel satu segmen saat final |
| Empat kaki | 0,15 × 0,65 × 0,15 m; pusat Y 0,38 m |
| Hopper | Mulut 1,60 × 1,34 m, bawah 0,68 × 0,62 m, Y 1,94–2,78 m; empat bidang miring |
| Motor | Radius 0,43 m, panjang 0,55 m, 12 sisi; lima ring pendingin dangkal; bukan ratusan fin |
| Outlet depan | Radius 0,24 m, panjang 0,42 m, 8 sisi, pusat Y 0,89 m, menghadap -Z |
| Tombol | Tiga tombol merah/kuning/hijau, radius 0,085 m, 6 sisi; Y 1,62 m |
| Tray | 1,00 × 0,76 m, dinding sekitar 0,38 m; di depan outlet |
| Karung | Radius 0,37 m, tinggi 0,80 m, 8 sisi; satu karung kiri depan |

Peran: **memproduksi Fish Feed melalui FeedMaker/recipe data**. Mesin ini tidak otomatis sama dengan penabur. Pakan pada hopper/tray sketsa merupakan ilustrasi prop; jumlah pakan gameplay harus memakai data, bukan dihitung dari geometri butiran.

## Penabur otomatis dan storage

Lv2/Lv3 mempunyai satu penabur: tabung 10 sisi dengan radius 0,42/0,47 m, casing tinggi 1,05 m, empat kaki kayu 0,10 m, lid membulat sederhana, satu nozzle 8 sisi. Tinggi puncak sekitar 2,99 m dari ground. Kontrol berada di sisi -Z pada Y 1,90 m; outlet mengarah +Z ke area air, bukan ke jalan player. Barrel kayu 10 sisi dengan dua ring besi tetap dipakai sebagai storage/visual stasiun pakan. Kotak suplai kecil Lv3 berada di belakang mesin, 1,40 × 1,25 × 1,35 m, atap 1,65 × 1,58 m; tidak menjadi bangunan interior tambahan.

Save stok hopper, status otomatis, jadwal penaburan, resep input/output maker, dan konsumsi harian perlu dihubungkan saat implementasi. Jangan memberikan pakan gratis karena efek penabur menyala. Waktu proses, biaya, item resep, jumlah pakan, serta upgrade ekonomi belum ditentukan oleh permintaan model ini; nantinya harus mengikuti data/CSV yang disetujui.

## Atlas dan material air

Atlas yang benar-benar dibaca adalah `TFP_Atlas_1A.psd` **2048×2048**, GUID `522fcb2b5717c7640b460ee9ff73a78a`. Sampel warna batu, batu terang/gelap, kayu, mesin, besi, butiran, tombol, moss/daun berada di JSON dengan piksel top-left dan UV bottom-left. Margin UV **4 px** di sel palet 32×64 px, sama seperti struktur sebelumnya. Tidak repeat seluruh atlas ke sebuah batu atau papan.

Atlas punya palet yang cukup untuk desain ini. Atlas tidak menyediakan texture realistis khusus boulder kolam atau casing mesin sesuai foto; pemisahan papan, batu bevel, ring motor, hopper, nozzle dan tombol akan dibentuk di mesh. **Tidak memerlukan texture runtime baru** untuk ketiga level.

Air tidak di-UV ke palet atlas. Material pack yang diperiksa:

- `TFP_Water_1A.mat`, GUID `593ae163bc656234f924d34c6719b9c1`.
- Shader **`Toon/CustomWaterSimple`**, GUID `8fc8a63c4600c534fa2d53bdba6b79c8`.
- Normal map dan noise texture sudah dirujuk material pack; dipakai ulang, tidak disalin menjadi texture per kolam.
- Shader memakai Scene Depth untuk kedalaman/opacity/foam. `PC_RPAsset` sudah mengaktifkan Depth dan Opaque Texture; `Mobile_RPAsset` saat diperiksa belum mengaktifkan keduanya. Saat apply, depth harus tersedia pada kamera/pipeline target atau foam/depth tidak bisa dianggap berfungsi.

Rencana material air pond adalah **variant/instance khusus dari TFP_Water_1A**, agar tuning kolam tidak mengubah semua air pack: `_Waves=0` untuk permukaan datar, normal/water motion kecil, `_WaveIntensity=0,16`, `_Opacity=0,35`, `_OpacityDepth=1,4`, `_ShallowColorDepth=0,85`, `_FoamOpacity=0,18`, `_FoamDistance=0,06`, `_ReflectionsOpacity=0,08`. Angka tersebut merupakan starting point usulan, perlu review pagi/sore/malam di Unity setelah ACC. Warna, refraksi, ikan terlihat di bawah air, dan foam tidak diklaim sudah diuji oleh gambar sketsa.

## Low poly, mesh dan LOD setelah ACC

| Level | Triangles primitive sketsa | Target LOD0 | Batas LOD0 | Target LOD1 | Target LOD2 |
|---|---:|---:|---:|---:|---:|
| 1 | 2.421 | 3.500 | 5.000 | 1.800 | 650 |
| 2 | 3.051 | 4.500 | 6.500 | 2.300 | 850 |
| 3 | 3.880 | 6.000 | 8.500 | 3.200 | 1.200 |

Hitungan primitive sketsa berasal dari semua face JSON, termasuk aliran simbolis. Target produksi memperhitungkan bevel, cap, sambungan dan detail yang masih perlu dibuat; **belum ada hitungan mesh Blender aktual**.

1. Sumber editable dipisah per grup: basin, rim, dock, maker, auto feeder, storage, inlet/outlet, decor. Nama produksi memakai ID JSON sehingga empat kaki atau batu dengan nama kelompok sama tidak ambigu.
2. Opaque static final digabung menjadi **satu mesh per LOD**. Mesin casing tidak membutuhkan renderer masing-masing; marker child, komponen interaksi, dan collider tetap terpisah tanpa mesh.
3. **Permukaan air satu mesh tersendiri** agar shader bisa diganti di Unity: polygon 16 titik, normal ke atas, triangulasi fan atau grid ringan bila shader nanti memerlukan displacement. UV air tersendiri, bukan UV swatch atlas.
4. Maksimum **dua renderer/material static aktif** pada satu LOD: opaque atlas + air. VFX dan representative fish merupakan renderer runtime tambahan yang dibatasi, bukan termasuk dua renderer static tersebut.
5. LOD1 mengurangi boulder menjadi bentuk lebih sederhana, ring motor, daun, sambungan dan board seam. LOD2 mempertahankan basin, dock, maker/feeder silhouette, inlet dan water boundary dengan sedikit bidang. LOD dibuat di Blender dan LODGroup diarahkan ke mesh itu di Unity; jangan menganggap Unity otomatis membuat decimation mesh.
6. Aliran inlet/outlet dan pakan dibuat dengan VFX Unity; pakai particle/material pack yang ada. Poligon ribbon biru pada sketch hanyalah simbol posisi, tidak dibake ke opaque mesh atau dianggap simulasi air.
7. Collider sederhana berjumlah sedikit: ring/basin blocker + dock/tangga + station body. Tidak satu MeshCollider per boulder. Player tidak berjalan di air; akses lewat dock.
8. Save `.blend`/script/export/preview di `ArtSource/FishPond_A` setelah ACC, dipisah Blender, Exports, References, Scripts, Previews, dan Revisions. Model runtime serta prefab disimpan di folder Assets tersendiri seperti struktur lain. Tidak ada texture artwork/sketch yang diimport sebagai texture game.

## Data existing yang perlu diselaraskan nanti

Audit kode saat membuat sketsa menemukan kapasitas `FishPondService` lama **99/300/999/999**, definition **4/8/12/16**, empat prefab level, serta `maximumVisibleFish` default 8 dengan Inspector range maksimum 10. Permintaan baru menjadi **tiga level 50/100/250**. Sketsa ini mencatat angka baru; kode, balancing dan save belum diubah.

Saat apply, kapasitas harus punya sumber data yang konsisten untuk UI, transaksi, save dan definition. Save pond ID dan ikan harus dipertahankan. Usulan migrasi Lv4 lama → Lv3; bila ikan tersimpan melebihi kapasitas baru, larang penambahan tetapi izinkan pengambilan sampai normal, **jangan menghapus ikan diam-diam**.

Ikan visual dibatasi usulan 8/12/18 model per kolam, menggunakan `ItemSO.worldPrefab` dan metadata ikan asli, bukan spawn 250 renderer. Runtime lama menaruh ikan pada radius 0,32 m dan Y 0,05 m; ini harus diganti area polygon air, exclusion dock, dan band Y **0,38–0,50 m**. Normalize renderer bounds ikan dengan panjang visual sekitar 0,45–0,90 m dan ketebalan vertikal maksimum 0,18 m: bagian bawah tetap di atas batu +0,26 m, bagian atas di bawah air +0,65 m. Jangan hanya menyalin scale placeholder capsule. Batas Inspector perlu diperluas dan pooling dipertimbangkan. Jumlah ikan visual bukan angka kapasitas storage.

Definition placement sekarang memiliki satu footprint. **Dengan sistem saat ini, reservasi paling aman 20×16 m sejak Lv1**, supaya upgrade Lv3 tidak menabrak struktur di samping. Reservasi per-level 14×11/16×13/20×16 m pada sketsa adalah usulan bila implementasi menambahkan pengecekan dan reservasi ekspansi saat upgrade. Jangan menyimpan footprint 5×5 lama setelah model baru di-apply. Konversi meter ke cells harus memakai ukuran cell/snap aktual saat implementasi.

## Pemeriksaan sebelum apply setelah ACC

Ukur triangles/material/renderer/LOD aktual, cocokkan bounds dan marker dengan JSON, normal air/skylight-equivalent face ke luar, scale satu, dan arah camera-facing setelah export. Uji jalan dari approach ke dock, UI kolam, maker, barrel dan penabur. Uji air dengan depth target, terrain tidak muncul di dasar, ikan berada di band renang, partikel jatuh ke air, perubahan level/save, relokasi/demolish, serta upgrade tanpa menimpa player/struktur lain. Baru terapkan prefab sesuai level; tidak ada tindakan Blender/Unity yang dilakukan oleh tahap sketsa ini.
