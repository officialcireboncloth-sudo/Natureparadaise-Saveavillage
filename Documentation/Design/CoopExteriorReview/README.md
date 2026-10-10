# Coop exterior A — paket sketsa untuk review

**Status: sketsa disetujui, model Blender dibuat di `ArtSource/Coop_A`, dan exterior diterapkan ke Unity.** Prefab Lv1–5 tersedia; gameplay Lv1–4 memakai visual baru. Audit implementasi ada di `ArtSource/Coop_A/Previews/UnityImportAudit.txt`. Dokumen ini mempertahankan acuan desain awal. Referensi adalah gambar kandang ayam krem dengan trim abu kebiruan yang diberikan pengguna. Ukuran di bawah merupakan keputusan desain; gambar perspektif tidak memberikan ukuran absolut yang dapat diukur secara pasti.

Mengikuti format barn, rencana exterior Lv1–5 memakai **bentuk, warna, material, jumlah detail, UV dan topology yang sama; hanya uniform scale XYZ yang berubah**. Lv3 adalah ukuran master. Tidak ada sayap, lantai, dekorasi, atau warna tambahan ketika naik level. Interior dan balancing bukan bagian sketsa exterior ini.

## Paket gambar dan acuan produksi

- `coop-volume-front-right.png`: volume depan dan kanan; seluruh komponen berasal dari koordinat proposal yang sama.
- `coop-volume-rear-left.png`: volume belakang dan kiri, termasuk rancangan sisi yang tidak terlihat dalam referensi.
- `coop-four-views.png`: empat elevasi; versi besar per sisi juga tersedia.
- `coop-top.png`: tampak atas, roof overhang, ramp, dan tonjolan kotak sarang.
- `coop-levels-and-measurements.png`: ukuran Lv1–5 serta ringkasan perhitungan.
- `coop-level-comparison.png`: lima silhouette pada satu skala gambar untuk menilai pembesaran antar level.
- `coop-atlas-and-materials.png`: crop atlas asli, warna terpilih, piksel dan UV aman.
- `coop-proposal-A.json`: acuan ukuran dan penempatan, 335 komponen/marka sketsa; jumlah ini **bukan** jumlah objek/renderer yang akan diekspor. Semua detail besar memiliki posisi XYZ, dimensi atau endpoint, material dan facade.
- `review-validation.json`: hasil audit batas seluruh primitive termasuk ketebalan beam, ukuran tiap level, formula atap/ramp, clearance pintu, UV dan hash atlas.
- `reference-coop.png`: salinan referensi agar tetap tersedia ketika clipboard temp hilang.

Setelah ACC, koordinat JSON menjadi acuan produksi. Jangan menebak ukuran ulang dari PNG atau mengubah proporsi ketika memasang prefab. Sketsa memperlihatkan struktur dan penempatan; bevel, kedalaman lip shingle dan batu bersudut lembut akan dibuat dalam batas dimensi yang telah disetujui. Ini bukan render shader Unity final.

## Dimensi dan scale

Satu unit Unity = satu meter. Unity: X kanan, Y atas, Z belakang; **depan -Z**. Blender: X kanan, Y belakang, Z atas; depan -Y. Pivot tetap pusat badan di tanah `(0,0,0)`, bukan pusat AABB yang bergeser karena ramp dan kotak sarang.

| Level | Scale XYZ | Badan lebar × dalam | Batas visual lebar × dalam × tinggi | Pintu lebar × tinggi |
|---|---:|---:|---:|---:|
| 1 | 0,750 | 4,50 × 5,25 m | 5,5125 × 6,525 × 4,305 m | 1,125 × 2,2125 m |
| 2 | 0,875 | 5,25 × 6,125 m | 6,43125 × 7,6125 × 5,0225 m | 1,3125 × 2,58125 m |
| 3 | 1,000 | 6,00 × 7,00 m | 7,35 × 8,70 × 5,74 m | 1,50 × 2,95 m |
| 4 | 1,125 | 6,75 × 7,875 m | 8,26875 × 9,7875 × 6,4575 m | 1,6875 × 3,31875 m |
| 5 | 1,250 | 7,50 × 8,75 m | 9,1875 × 10,875 × 7,175 m | 1,875 × 3,6875 m |

Batas visual termasuk atap, trim, tangga, lampu, ramp ayam dan kotak sarang. **Bukan** hanya lebar atap 6,80 m × kedalaman atap 7,80 m. Pintu paling kecil masih tinggi 2,2125 m; pendekatan ini mengasumsikan player sekitar 1,75–1,80 m dan perlu dicek terhadap capsule aktual saat implementasi.

## Bagian master Lv3

Ukuran W × H × T menggunakan orientasi facade; posisi selalu Unity XYZ. Untuk semua bagian rinci, JSON menyimpan axis/world size sehingga tidak ada ambigu saat produksi.

| Bagian | Dimensi / penempatan yang ditentukan |
|---|---|
| Badan | X -3..+3; Z -3,5..+3,5; dinding Y 0,45..3,80 m |
| Fondasi | Tinggi 0,45 m; dua course 0,225 m; sekitar 33 batu/course melingkari perimeter |
| Papan krem | Pitch 0,25 m; groove 0,008 m; sekitar 104 panel di empat dinding sebelum gable/opening |
| Atap pelana | Ridge Y 5,60 m; eave struktur Y 3,80 m; eave luar Y 3,56 m |
| Roof overhang | 0,40 m di empat sisi; bidang atap X ±3,40 dan Z ±3,90 m |
| Atap low poly | 8 baris/slope × 12 kolom sepanjang depth; lip 0,02 m; tebal shell maksimum 0,06 m ke bawah |
| Ridge cap | 0,20 × 0,14 × 7,80 m; pusat `(0,5.67,0)`; puncak Y 5,74 m |
| Tiang sudut | 0,22 × 3,35 × 0,22 m; pusat X ±2,89 / Z ±3,39; empat tiang |
| Fascia gable | Lebar 0,16 / tebal 0,16 m; berada di ujung overhang Z ±3,82, inset di bawah bidang atap |
| Horizontal rail | Lebar 6,00 / tinggi 0,16 / tebal 0,12 m; Y 3,72; depan dan belakang |
| Brace eave | Empat per sisi, Z -2,85 / -0,95 / +0,95 / +2,85; balok 0,12 × 0,12 m |
| Pintu manusia | Satu daun, 1,50 × 2,95 m; pusat X -1,25; Y bawah 0,30, atas 3,25; muka Z -3,56 |
| Frame pintu | Jamb 0,18 m; lebar total 1,86 m; lintel pusat Y 3,34; depan frame Z -3,72 |
| Brace pintu | Satu diagonal bawah kiri ke atas kanan, lebar 0,14 m; cross-rail Y 1,45 |
| Engsel pintu | Dua plat 0,32 × 0,10 × 0,05 m; Y 0,68 / 2,85; handle di X -0,65, Y 1,65 |
| Tangga depan | Bawah: 1,86 × 0,15 × 0,70 m, pusat Z -3,95; atas tinggi 0,30 m dan depth 0,34 |
| Lampu | Frame 0,26 × 0,30 × 0,20 m; pusat X -1,25, Y 3,55, Z -3,78; core terpisah material emissive |
| Window depan | Clear 1,45 × 1,20 m; pusat X +1,45, Y 2,50; sisi kiri half shutter, kanan kawat |
| Dua window kanan | Clear 1,30 × 1,20 m; pusat Z -1,40 dan +1,55; Y 2,50; half shutter + kawat |
| Window kiri | Usulan satu window tengah Z 0; clear 1,45 × 1,20 m; Y 2,50 |
| Frame window | Jamb/rail 0,14 m; outline luar 0,28 m lebih besar daripada clear size |
| Kawat | Flat diamond lattice tipis 0,018 m; maksimal 8 strip/window; bidang kawat di depan inset gelap |
| Pintu ayam | 0,78 × 0,90 m; pusat `(1.45,0.90,-3.56)`; bawah Y 0,45; dua engsel |
| Frame pintu ayam | Lebar total 1,02 m, tinggi 1,14 m; pusat rail Y 0,39 / 1,41 |
| Ramp ayam | Lebar 1,00 m; atas Y 0,45 / Z -3,73; bawah Y 0 / Z -4,80; 5 cleat melintang |
| Kotak sarang kanan | Body X 3,06..3,90; Y 0,60..1,45; Z -2,30..-0,50; panjang 1,80 m |
| Tutup sarang | Panjang 1,90 m; X 3,05..3,95; atas Y 1,63 ke Y 1,35; dua engsel dan handle kecil |
| Penyangga sarang | Dua brace pada Z -2,05 / -0,75; lebar 0,10 m; endpoint di JSON |
| Vent belakang | Clear 1,10 × 0,65 m; pusat `(0,4.35,3.54)`; 4 louver; frame luar 1,38 × 0,93 m |
| Entrance manusia | `(-1.25,0,-4.65)` sebelum scale; di depan tangga dan di luar body collider |

**Sisi tidak terlihat dalam referensi:** kiri mengikuti trim/papan/fondasi yang sama dengan satu window tengah; belakang memakai satu vent di gable, tanpa pintu tambahan. Tidak ada jendela loft depan karena ciri referensi coop berbeda dari barn. Tanah, semak, daun di bawah fondasi dan fence tidak digabung dalam mesh bangunan.

Pintu/shutter/hatch/nest lid ditampilkan **tertutup dan statis**, sesuai permintaan satu bangunan gabungan pada barn. Pintu manusia berfungsi sebagai titik interaksi menuju interior terpisah; tidak perlu animated hinge exterior. Membuka hatch untuk ayam atau mengambil telur lewat lid belum merupakan fitur yang dibuat dalam proposal ini.

## Perhitungan yang digunakan

- Rise atap = 5,60 - 3,80 = **1,80 m**; half span = 6/2 = **3,00 m**.
- Slope = atan(1,80/3,00) = **30,9638°**. Eave luar = 5,60 - 0,60 × 3,40 = **3,56 m**.
- Panjang slope termasuk overhang = √(3,40² + 2,04²) = **3,9650 m**.
- Luas dua bidang atap = 2 × 7,80 × 3,9650 = **61,8547 m²**. Ini luas geometri master, bukan jumlah item kayu untuk gameplay.
- Ramp: run 1,07 m; rise 0,45 m; panjang **1,1608 m**; slope **22,8097°**.
- Tutup sarang: run 0,90 m; rise 0,28 m; slope **17,2815°**.
- Envelope master: X -3,40..+3,95; Y 0..5,74; Z -4,80..+3,90 → **7,35 × 8,70 × 5,74 m**.
- Semua posisi/panjang/clearance = nilai master × scale level. Luas bidang = luas master × scale²; triangles tidak bertambah dengan scale.
- Tapak cadangan Lv5 = **10 × 13 m**. Batas site terhadap pivot: X -4,25..+5,75; Z -8,125..+4,875. Ini mencakup ramp dan ruang pendekatan di depan; site tidak simetris terhadap pivot.
- Footprint grid harus dihitung `ceil(width / CellSize)` dan `ceil(depth / CellSize)`. Offset pusat site dari pivot bangunan adalah `(0.75,0,-1.625)`; bila anchor placement berada di pusat reservasi, posisi root = anchor minus offset tersebut (setelah rotasi yaw). Offset ini adalah dunia untuk cadangan maksimum; jangan menggeser mesh pivot.

## Audit atlas, UV dan material

Atlas yang diperiksa langsung: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd`, composite **2048 × 2048**. Hash SHA256 dicatat di JSON. Atlas asli dan material bersama tidak dimodifikasi. Palet kiri bawah cukup untuk semua warna utama.

Warna sampel yang dipilih: papan `#e1dcb8`, trim utama `#8f938f`, sisi trim abu kebiruan `#4d6d74`, atap `#524741`, batu `#b0a094`, variasi batu `#b7af9f`, hardware/rongga `#313131`, kawat `#8f8467`, core lampu `#ffd956`. Trim atlas yang terdekat lebih netral daripada warna biru pucat dalam referensi; jangan mengklaim warna referensi identik. Bentuk dipertahankan, warna menyesuaikan palet pack agar cocok dengan world.

Setiap swatch memiliki pixel rectangle 32 × 64 px, pixel sampel dan UV safe rectangle dengan **inset 4 px**, dengan piksel dari kiri atas / UV dari kiri bawah. Tidak menggunakan tiling seluruh atlas untuk mengejar motif papan. PNG audit hanyalah dokumentasi, tidak dimasukkan ke runtime sebagai texture baru.

**Dua material maksimum, atlas yang sama:** `TFP_Atlas_1A.mat` untuk seluruh opaque body dan `TFP_Atlas_Lights_1A.mat` hanya core lampu. Tidak membuat albedo/normal/AO map baru, atlas duplikat per level, atau alpha map kawat.

Pola papan, bevel kayu, sambungan batu, lip/garis shingle, handle dan kawat dibentuk dengan geometri/face dan UV palet. Referensi menampilkan kawat hex rapat; proposal menyederhanakannya menjadi diamond lattice yang masih terbaca dari kamera game. Tidak menambah silinder untuk setiap kabel, paku mikro, scratches realistis, subdivision sculpt atau setiap genteng solid. Dinding inset gelap memberi kesan ruang di balik kawat.

## Budget low poly dan LOD

| Bagian | Allowance triangles LOD0 |
|---|---:|
| Panel dinding 104 × 4 + gable | 418 |
| Atap: 2 × 8 × 12 × 2, plus lip setara | 768 |
| Fondasi: sekitar 66 blok sederhana × 12 | 792 |
| Tiang, trim, frame, brace | 520 |
| Pintu, shutter, hatch | 240 |
| Kawat window dan vent | 96 |
| Kotak sarang, ramp, tangga | 160 |
| Lampu dan hardware | 80 |
| **Total estimasi** | **3.074** |

Ini **budget perencanaan**, bukan hasil ukur mesh final. Batas LOD0 **4.200 triangles** memberi ruang untuk bevel batu/trim satu segmen dan koreksi siluet; final harus diukur setelah triangulasi dan penghapusan muka tersembunyi. Target LOD1 sekitar **1.600**, LOD2 sekitar **420**. Jumlah material tetap maksimum dua. Lv1–5 menggunakan mesh LOD yang sama sehingga tidak membutuhkan lima salinan geometri/texture.

LOD dibuat di Blender sebagai simplifikasi yang mempertahankan bentuk: LOD1 mengurangi baris shingle, batu/bevel dan kawat; LOD2 memakai roof planes, shell, ramp, sarang dan facade marks besar. **Unity LODGroup memilih mesh yang sudah dibuat, tidak otomatis membuat geometri LOD.** Pengaturan transisi/culling akan diuji terhadap kamera game setelah ACC; tidak mengklaim jarak tertentu sudah teruji pada coop baru.

## Produksi setelah ACC dan integrasi

1. Bangun master Lv3 dari JSON, unit meter, pivot ground-center; apply rotation/scale sebelum export. Tebal wall shell bila diperlukan 0,16 m ke dalam badan, bukan menambah footprint.
2. Papan memakai face/subdivision dengan groove dangkal. Fondasi dua course dengan bentuk batu besar dan satu segmen bevel; jitter maksimum 0,02 m ke dalam envelope. Roof shell maksimum 0,06 m ke bawah; lip tidak boleh melampaui ridge cap atau batas XYZ.
3. Fascia/brace/window/hatch/nestbox dibuat pada koordinat proposal. Bevel trim 0,02 m satu segmen; hardware dibesarkan secukupnya dalam dimensi yang sudah ditentukan. Lantern dibuat 6–8 sisi dalam bounding box yang sama.
4. UV setiap face masuk patch palet yang diaudit; emission hanya core lampu. Shared PSD/material/import settings tetap dipertahankan.
5. Simpan sumber editable bagian-bagian dan file gabungan. File gabungan menampilkan **satu mesh bangunan LOD0**, LOD1/2 disimpan tersembunyi; export masing-masing LOD sebagai satu mesh dengan maksimum dua material slots.
6. Buat satu set High/Medium/Low FBX dan wrapper prefab Lv1–5 dengan uniform scales. Pertahankan GUID/root fileID prefab yang sudah ada ketika mengganti visual, supaya referensi data tidak putus. Verifikasi konversi sumbu FBX serta arah depan setelah import.
7. Collider badan sederhana: pusat `(0,1.9,0)`, size `(6,3.8,7)` × scale. Tambahkan satu box collider sarang bila diperlukan; jangan MeshCollider setiap genteng/batu. Ramp/tangga dekoratif tidak menghalangi approach marker. Entrance manusia di `(-1.25,0,-4.65)` × scale, approach trigger terpisah di luar collider.
8. Periksa shape/scale/rotasi/UV, triangles aktual, shader pagi/sore/malam, source Blender, native Unity import, tiap LOD, culling, marker, placement offset dan player interaction. Jangan mengklaim Play Mode lulus dari preview render saja.

Organisasi setelah disetujui:

```text
Documentation/Design/CoopExteriorReview/  # sketsa, spesifikasi dan audit sekarang
ArtSource/Coop_A/
  Blender/Coop_A_Master.blend            # archive komponen editable
  Blender/Coop_A_Combined.blend          # satu mesh visible per bangunan
  Scripts/                              # builder/export/validation
  References/                           # referensi dan spec yang di-ACC
  Previews/                             # render dan audit implementasi
Assets/Nature  Paradaise/mesh/Buildings/Coop_A/
  Coop_A_High.fbx
  Coop_A_Medium.fbx
  Coop_A_Low.fbx
Assets/Nature  Paradaise/Prefabs/Coop/Exterior/
  CoopExterior_Lv1.prefab ... CoopExterior_Lv5.prefab
```

**Kondisi data saat ini:** `Coop Building.asset` memiliki Lv1–4, kapasitas 6/12/20/30, footprint 3 × 3 sel. Asset ini belum diubah. Sketsa exterior Lv5 siap dihitung, tetapi harga, durasi, kapasitas, unlock dan interior Lv5 tidak ditentukan sepihak. Footprint 3 × 3 saat ini tidak otomatis cocok dengan bentuk baru; pembaruan placement harus mengikuti ukuran CellSize dan site maksimum di atas pada tahap apply.

## Verifikasi sketsa

`build_review.py` membangun spesifikasi dan gambar orthographic; `draw_volume.py` menghasilkan volume dengan z-buffer dari koordinat yang sama; `validate_review.py` mengecek semua batas geometri (termasuk beam thickness), unique names, formula atap/ramp, dimensi/clearance setiap scale, cadangan site, budget arithmetic, sample atlas/UV/SHA256 serta batas data gameplay yang ada. Audit PASS disimpan dalam `review-validation.json`.

Folder ini menyimpan sketsa dan audit proposal. Implementasi Blender/FBX dan audit Unity ada di `ArtSource/Coop_A/`. Tahap apply mengganti isi wrapper prefab coop, menautkannya ke definition dan memperbarui footprint definition/CSV; atlas pack dan scene Map tidak diubah.
