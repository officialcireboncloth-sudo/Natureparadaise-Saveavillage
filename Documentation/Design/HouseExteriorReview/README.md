# Rumah exterior A — sketsa Lv1–5 untuk review

**Status: sketsa disetujui pengguna; empat master Blender dan 12 FBX LOD sudah dibuat dan diterapkan ke Unity.** File produksi dan audit ada di `ArtSource/House_A`; bagian berikut tetap merupakan arsip keputusan sketsa sebelum produksi. **Revisi produksi berikutnya: panel bay kanan Lv5 biru solid opaque dari atlas, menggantikan rencana kaca transparan di arsip ini.** Referensi Lv5 sudah memakai gambar koreksi pengguna: teras besar, dua skylight, dan ruang kaca kanan. Ada empat desain: Lv1, Lv2, Lv3–4, Lv5. Lv3 dan Lv4 memakai **model, material, ukuran, dan scale yang sama**; perbedaan upgrade interior tetap mengikuti data game yang ada.

Ukuran berikut adalah keputusan desain untuk disetujui, bukan pengukuran yang bisa diketahui pasti dari gambar. Gambar adalah sketsa volume low poly dengan palet pack, bukan janji detail fotorealistik dari referensi. Gambar, tampak, dan manifest berasal dari koordinat yang sama. Setelah ACC, `house-proposal-A.json` menjadi acuan ukuran dan penempatan produksi; jangan menebak ukuran lagi dari gambar PNG.

## Berkas review

- [Perbandingan empat bentuk](house-levels-overview.png): skala gambar sama agar perkembangan ukuran bisa dibandingkan.
- Setiap desain punya `house-Lv…-front-right.png`, `house-Lv…-rear-left.png`, `house-Lv…-elevations.png`, dan `house-Lv…-plan.png`. Ada empat tampak orthographic, ukuran keseluruhan, tapak, dan titik masuk.
- [Audit atlas dan material](house-atlas-and-materials.png): swatch diambil dari PSD asli, lokasi piksel dan UV aman dicatat dalam manifest.
- `References/`: empat gambar pengguna, tidak menggunakan gambar Lv5 lama yang terduplikasi.
- `house-proposal-A.json`: semua komponen sketsa, ID unik, posisi XYZ, dimensi, material, bounds, anchor, dan budget LOD.
- `build_review.py`: pembangun gambar dan manifest, **tidak mengekspor mesh atau mengubah Unity**. `validate_review.py` memeriksa data, sumber atlas, ukuran, jalur masuk, dan artefak. `validation-report.txt` menyimpan hasilnya.

## Ukuran dan arah

Satu unit = satu meter. **X kanan pada tampak depan, Y atas, depan +Z, belakang -Z.** Ridge atap utama memanjang kiri–kanan sepanjang X, sesuai referensi. Teras/dormer mempunyai ridge Z dan gable menghadap depan. Origin `(0,0,0)` ada di tengah dinding depan pada tanah, tepat di bawah pintu; bukan di tengah tapak keseluruhan. Dinding depan selalu Z=0. Rumah bertambah ke belakang dan ke kanan tanpa menggeser pintu utama.

| Model / level | Badan utama W × D | Batas visual W × D × H | Teras W × D | Bentuk khusus |
|---|---:|---:|---:|---|
| Lv1 | 9,00 × 7,50 m | 10,090 × 11,690 × 9,035 m | 3,80 × 2,20 m | Cottage kecil, satu jendela depan kiri, satu kanan |
| Lv2 | 13,00 × 8,50 m | 14,090 × 13,290 × 9,335 m | 5,80 × 2,80 m | Lebih lebar, dua jendela depan, satu skylight kanan |
| Lv3–4 | 9,60 × 8,50 m | 14,790 × 12,890 × 9,335 m | 4,40 × 2,40 m | Dormer depan dan sayap kanan 4,20 × 6,80 m |
| Lv5 | 15,00 × 8,50 m | 19,035 × 13,490 × 9,335 m | 6,60 × 3,00 m | Dua skylight dan ruang kaca bersegi di kanan |

Batas visual mencakup atap, lis, trim, planter, tangga, dan cerobong. Ukuran badan utama tidak mencakup sayap/ruang kaca. Semua transform produksi memakai scale `(1,1,1)`; perbedaan bentuk dibuat pada mesh, tidak dengan stretch mesh Lv1. Tinggi keseluruhan hampir tetap; progression terutama lewat lebar, teras, serta tambahan volume.

Player saat ini di-fit setinggi **3,6 m** (`PlayerVisualSetup.PlayerVisualHeight`). Karena itu pintu clear direncanakan **2,00 × 4,00 m** pada semua level. Tinggi pintu bukan disamakan dengan pintu manusia 2 m. Interior memakai scene tersendiri; lantai interior yang sudah di-ACC tidak dikecilkan untuk mengikuti tapak exterior. Ukuran interior Lv3/4/5 yang ada berbeda dari footprint exterior dan tetap menjadi acuan interior.

## Komponen bersama dan ukurannya

| Komponen | Keputusan produksi |
|---|---|
| Foundation | Y 0–0,60 m; dua baris blok batu besar, pitch sekitar 0,85 m, kedalaman relief 0,12 m |
| Dinding utama | Y 0,60–4,80 m; cream plaster/stucco sederhana; tanpa sculpt displacement |
| Atap utama | Ridge Lv1 Y 7,20 m; lainnya Y 7,50 m. Eave struktur Y 4,80 m; overhang 0,40 m |
| Kemiringan utama | Lv1 32,62°; Lv2/Lv3–4/Lv5 32,43°. Eave luar lebih rendah dari eave struktur mengikuti slope |
| Slate/shingle | 7 baris per slope, kolom pitch sekitar 0,82 m. Subdivisi plane, variasi UV, lip baris 0,035 m; bukan ratusan genteng solid |
| Lis / tiang | Lis 0,18–0,19 m, tiang sudut 0,22 m, post teras 0,24 m; bevel 0,025 m satu segmen setelah ACC |
| Pintu | Panel W 2 m / Y 0,60–4,60 m. Jamb pusat X ±1,12 m, lebar 0,20 m; lintel pusat Y 4,69 m |
| Kaca pintu | 1,20 × 1,35 m, pusat Y 3,35 m; frame silang dan handle kanan |
| Jendela standar | Pane 1,80 × 1,90 m, pusat Y 2,85 m; frame 0,14 m, inset dangkal; empat pane |
| Planter jendela | Lebar pane +0,35 m; tinggi 0,40 m; daun/bunga berupa cluster sederhana, bukan kelopak mikro |
| Lantai teras | Y +0,60 m. Tiga anak tangga @ rise 0,20 m / depth 0,50 m, total extension 1,50 m ke depan |
| Truss teras | Kingpost 0,16 m dan dua brace 0,13 m. Posisi teras, tinggi eave/ridge berbeda per desain (tabel berikut) |
| Lampu | Di kiri pintu: pusat `(-1.55,3.75,0.38)`; housing 0,28 × 0,45 × 0,25 m. Emission hanya core kecil |
| Cerobong | Stack 0,90 × 1,60 × 0,90 m, cap 1,14 × 0,22 × 1,14 m, flue 0,55 × 0,35 × 0,55 m |
| Cerobong posisi | X = 0,21 × lebar badan utama, Z = -0,52 × depth; tinggi dihitung dari ridge. Flue final berlubang, bukan box tertutup sketsa |
| Vent gable | Satu tiap ujung main roof, sekitar 0,90 × 0,72 m, empat bilah; warna trim/metal |
| Pot teras | 8 sisi, radius 0,32 m, tinggi 0,56 m, foliage kecil. Diposisikan dekat sisi teras untuk menjaga entry lane |
| Hanging planter | Lv2/Lv3–4/Lv5: dua pot radius 0,24 m, tinggi 0,30 m, rantai geometris sederhana tanpa tiap mata rantai |

| Model | Teras eave Y | Teras ridge Y | Approach di tanah (X,Y,Z) |
|---|---:|---:|---:|
| Lv1 | 4,45 m | 5,90 m | (0,0,4.60) |
| Lv2 | 4,55 m | 6,35 m | (0,0,5.20) |
| Lv3–4 | 4,50 m | 6,10 m | (0,0,4.80) |
| Lv5 | 4,60 m | 6,50 m | (0,0,5.40) |

Pintu dan area tangga mempunyai jalur masuk kosong lebar **2,40 m**. Pot, kursi, hanging planter dan post tidak masuk jalur ini. Tinggi batas pintu Y 4,60 m; lantai tinggi 0,60 m; clear height 4 m. Posisi interaksi dalam manifest adalah titik desain pendekatan; implementasi portal harus tetap memakai collider/closest point sesuai `HouseScenePortal`, bukan langsung menganggap titik jauh di depan otomatis bisa berinteraksi dengan pintu.

## Detail tiap bentuk, termasuk sisi yang tidak terlihat

**Lv1:** badan utama X -4,50…+4,50 dan Z -7,50…0. Teras di tengah. Jendela depan X -2,79; kanan Z -3,975. Sisi kiri diusulkan dua jendela Z -2,25/-5,475; belakang dua jendela X ±2,43. Tidak ada skylight/dormer/sayap. Lampu dan flowerbox memberi aksen cozy tanpa menambah banyak geometri.

**Lv2:** badan X ±6,50, Z -8,50…0. Jendela depan X ±4,29; kanan Z -4,505. Satu skylight pada slope depan di X +4,00/Z -1,65. Pane skylight 1,30 m sepanjang X dan 1,65 m sepanjang proyeksi Z, mengikuti kemiringan atap. Kiri dua jendela Z -2,55/-6,205; belakang X ±3,51. Teras lebih lebar dengan hanging planter di kedua sisi.

**Lv3–4:** badan X ±4,80, Z -8,50…0. Sayap kanan center X 6,80, W 4,20, D 6,80; X 4,70…8,90 dan Z -7,30…-0,50; eave Y 4,15 / ridge Y 5,95 m. Overlap shell 0,10 m di sambungan untuk sketsa; face tersembunyi di sambungan dibuang saat model final. Jendela sayap ada di depan, kanan, belakang. Jendela depan badan X -3,20. Dormer W 2,60 × D 2,24 m, depan Z -1,00, belakang -3,24, eave Y 7,02 / ridge Y 8,05 m; kaca 1,50 × 1,10 m di Y 6,30. Tidak ada skylight tambahan. Sisi kiri dan belakang badan tetap dua jendela. Lv4 tidak menambah lantai atau mengubah exterior; upgrade interior tetap bisa berlangsung.

**Lv5:** badan X ±7,50, Z -8,50…0. Jendela depan X ±4,95. Dua skylight slope depan di X -5/+5, Z -1,65. Ruang kaca berbatas poligon XZ `(7.50,-0.70), (9.60,-0.70), (10.95,-1.80), (10.95,-5.40), (9.60,-6.50), (7.50,-6.50)`. Ekstensi maximum **3,45 m** ke kanan dan depth **5,80 m**. Lantai Y 0,65 / eave 4,55 m; apex atap kaca `(8.25,6.45,-3.60)`. Lima facade luar mempunyai mullion tengah dan dua transom Y 2,10/3,40 m; roof facets dengan hip frame. Satu dinding yang menempel rumah bukan kaca berlapis ganda. Bay adalah exterior dekoratif; tidak membuat ruangan gameplay baru secara diam-diam. Kursi teras statis di kiri pintu dengan seat center `(-2.00,1.15,1.25)` tetap di luar entry lane. Kiri dan belakang badan mengikuti prinsip Lv2 dengan jarak lebih lebar.

Semua sisi belakang/kiri di atas adalah **usulan**, karena tidak ada dalam gambar pengguna. Bentuk belakang dibuat konsisten dengan shell: dinding krem, fondasi batu, trim, jendela dan siluet atap yang sama. Tidak menambah pintu belakang, balkon, atau lorong baru. Model tidak mempunyai interior lengkap di dalam shell exterior; portal menuju interior yang sudah ada.

Semak, pohon, rumput, paving taman, mailbox, barrel dan batu liar di sekeliling referensi adalah **dressing map terpisah**, bukan disatukan sebagai mesh halaman besar di setiap level. Ini memungkinkan terrain blending dan posisi taman disesuaikan tanpa membangun ulang rumah. Sketch fokus bangunan; attached flowerbox/pot/lantern dan kursi Lv5 masuk komponen rumah. Pack punya model kursi/barrel, tetapi proposal menaruh kursi sederhana dengan ukuran pasti agar penempatan tidak bergantung pada bounds asset lain yang belum di-fit.

## Atlas, kaca dan kebutuhan material

Sumber opaque yang dibaca: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd`, composite **2048 × 2048**, GUID `522fcb2b5717c7640b460ee9ff73a78a`. Palet kiri bawah berisi sel 32 × 64 px. Warna cream, trim abu kebiruan, roof gelap, kayu, batu, brick, metal, daun/bunga dan core kuning tersedia. SHA-256 sumber dan swatch piksel dicatat di JSON agar audit bisa diulang.

Atlas tidak mempunyai albedo slate/stucco/susunan batu yang sama persis dengan referensi. Low poly dibuat dari shape, bevel kecil, relief atap dan variasi palet. Tidak perlu membuat albedo/normal/AO baru atau menyalin atlas per level. Serat kayu, pori plaster, paku kecil, retakan dan kelopak bunga realistis pada referensi disederhanakan.

| Slot | Material pack yang dipakai | Penggunaan |
|---|---|---|
| 0 | `Models/Materials/TFP_Atlas_1A.mat` | Opaque shell/atap/trim/door/stone/flowerbox; shader pack CustomToon |
| 1 | `Models/Materials/TFP_Atlas_Lights_1A.mat` | Core lantern dan inset jendela warm; **shader URP emissive transparent**, bukan CustomToon |
| 2, hanya Lv5 | `Models/Materials/TFP_Glass_Windows_1B.mat` | Glass bay roof/walls, texture pack `TFP_Glass_Windows_1B.psd` **128 × 128**, GUID `d961c196f859dee448ef832d1c0da9e8` |

Glass 1B saat ini mempunyai `_BaseColor.a=0.1882353`, transparent queue 3000, `_ZWrite=0`, `_Cull=2`; tidak menulis depth/shadow seperti dinding opaque. Gambar review menampilkan glass sebagai warna schematic agar volume mudah dibaca, **bukan simulasi transparansi/refraction Unity**. Di model produksi, glass memakai UV texture kaca sendiri, bukan swatch “glass” di atlas. Swatch tersebut hanya alternatif warna untuk audit; tidak menjadi glass material runtime. Emission material pack berintensitas sekitar 1,498 sebelum lighting/bloom; uji siang/malam saat apply, jangan mengubah shared material yang dipakai aset lain untuk memaksa preview.

UV opaque dihitung dari koordinat piksel kiri atas ke UV kiri bawah. Margin 4 px di setiap sel memberi rect aman 24 × 56 px. Orientasi gradient per face digunakan untuk variasi lembut, bukan repeat seluruh atlas. Maksimal **2 slot untuk Lv1/2/3–4, 3 untuk Lv5**. Tidak menambah runtime texture baru. Tidak membuat Point Light pada semua jendela; satu lantern light terpisah opsional setelah profiling/night review.

## Low poly, penggabungan dan LOD setelah ACC

| Model | Target LOD0 | Batas LOD0 | Target LOD1 | Target LOD2 | Triangles primitive sketsa |
|---|---:|---:|---:|---:|---:|
| Lv1 | 4.800 | 6.000 | 2.200 | 650 | 3.148 |
| Lv2 | 6.000 | 7.500 | 2.800 | 800 | 3.966 |
| Lv3–4 | 7.600 | 9.500 | 3.500 | 1.100 | 4.836 |
| Lv5 | 8.500 | 11.000 | 4.000 | 1.300 | 4.333 |

Target/batas adalah **budget produksi, belum hasil triangulasi mesh Blender**. Jumlah primitive sketsa dihitung dari face kotak/beam/polygon; bevel, perforasi flue, ketebalan roof/window recess, normal dan penghapusan face tersembunyi akan mengubah hitungan. Budget per shell/roof/trim/decor/chimney/bay dijumlahkan dalam manifest. Keempat desain sederhana cukup untuk kamera top-down; tidak memakai sculpt/subdivision tinggi.

Urutan produksi yang direncanakan:

1. Buat empat master di Blender. Konversi Unity XYZ ke Blender `(X,-Z,Y)`; muka Blender -Y harus menjadi **Unity +Z** sesudah FBX import. Unit meter, apply scale/rotation. Lv3/Lv4 menunjuk mesh yang sama, tidak menyalin mesh.
2. Bangun shell, gable, fondasi dan roof planes dari manifest. Tambahkan bevel satu segmen 0,025 m pada edge yang terbaca; buang face di bawah/di dalam shell/sambungan wing. Sambungan dormer/porch dibuat rapi, tidak tumpang tindih transparan.
3. Atap berupa subdivided planes dan shallow row lips; slate stagger/UV variation cukup, tanpa solid tiles penuh. Tambahkan ketebalan fascia/lip di edge silhouette saja. LOD1 mengurangi lip/bevel/flower clusters; LOD2 mempertahankan ridge, porch, dormer/wing/bay siluet dengan facade sederhana.
4. Lampu 6–8 sisi, pots 8 sisi, flower clusters dan trim sederhana. Handle/hinge tidak memakai cylinder puluhan sisi. Tidak membuat individual chain links atau ribuan petals.
5. UV sesuai atlas/material audit. Bay memakai glass plane normals keluar dan tanpa pane coplanar berganda; satu sisi menempel badan dihapus. Model bay tidak berisi detail interior baru; gunakan inset warm facade/baked visual sederhana bila dibutuhkan untuk keterbacaan setelah review shader.
6. **Satu mesh gabungan per bangunan per LOD**, submesh mengikuti material slots; pintu/post/pot tidak menjadi objek animasi terpisah. `.blend` menyimpan collection sumber terorganisasi dan collection Export. Transform final/bounds/pivot tetap sesuai manifest; toleransi edge tambahan bevel maksimum 0,05 m, bukan mengubah ukuran untuk “fit”.
7. Buat LOD0/1/2 di Blender; Unity `LODGroup` memilih ketiganya. Unity tidak dianggap otomatis membuat topology LOD dari FBX. Ambang screen-relative awal 0,55/0,25/0,08 untuk dituning terhadap kamera aktual; jangan cull house yang masih jelas terlihat pada zoom gameplay.
8. Collider sederhana: body/wing/bay box compound; collision facade pintu terpisah dari trigger entry, walkable porch/steps. Pot/flower/roof tile tidak mendapat collider masing-masing. Uji CharacterController bisa melewati tangga 0,20 m; bila stepOffset tidak cukup, pakai collision ramp tak terlihat setinggi 0,60 sepanjang 1,50 m (21,8°), visual tangga tetap sama.
9. Review FBX di Unity: arah depan, empat siluet, bounds, triangles aktual, jumlah renderer/material, UV bleeding, bay transparency dan lighting pagi/malam. Uji portal/return spawn dan upgrade Lv1→5 menjaga titik pintu. Interior/gameplay unlock yang sudah ada tetap mengikuti data.

## Integrasi Unity dan area yang perlu disediakan

Data `Player House Building.asset` sudah punya level 1–5, footprint **6 × 5 cells**, dan `completedPrefab` kosong tiap level; exterior saat ini melalui scene visuals. Tahap sketch ini tidak mengubah biaya/material/durasi upgrade, prefab, scene, atau layout interior. Nantinya link empat master ke lima entries dan pastikan scene debug/runtime memilih prefab yang benar (`PlayerHouseController` menaruh runtime prefab di local zero/identity).

Area maksimum yang disarankan untuk semua level: **22 × 17 m**, lokal X **-9,50…12,50** dan Z **-10…+7**, center `(1.50,-1.50)` relatif pintu. Ini mengandung bounds Lv5 dan ruang approach +5,40 m, dengan ruang cadangan tepi. Karena origin ada di pintu dan sayap kanan tidak simetris, center reservasi bukan origin mesh. Sistem placement harus memperhitungkan offset tersebut, atau wrapper placement berpusat di reservasi dengan mesh/portal offset bersama; tidak boleh langsung menganggap footprint berpusat di pintu.

Jika `FieldArea.CellSize=1 m`, reservasi ini berarti **22 × 17 cells**, bukan 6 × 5. Jumlah cells final `ceil(ukuran world / CellSize aktual)`. Rumah authored bisa tetap ditempatkan di anchor yang sama jika area tersebut kosong; sebelum apply cek overlap path/terrain/struktur lain. Portal trigger dan return marker dipasang di depan facade yang aktif; marker lama tidak boleh tertinggal di tengah shell atau di belakang rumah. Jalan/map dressing menyambung approach, bukan menutupnya.

Rencana organisasi file produksi:

```text
ArtSource/House_A/
  Blender/House_A_Masters.blend
  Scripts/
  References/              # proposal yang di-ACC + gambar pengguna
  Previews/                # render/audit Blender dan Unity
Assets/Nature  Paradaise/Mesh/Buildings/House_A/
  House_A_Lv1_LOD0.fbx ... LOD2.fbx
  House_A_Lv2_LOD0.fbx ... LOD2.fbx
  House_A_Lv3_4_LOD0.fbx ... LOD2.fbx
  House_A_Lv5_LOD0.fbx ... LOD2.fbx
Assets/Nature  Paradaise/Prefabs/PlayerHouse/Exterior/
  HouseExterior_Lv1 ... HouseExterior_Lv5
```

File mentah tidak dimasukkan ke Assets agar Unity tidak mengimpor `.blend` secara otomatis. Material/texture tetap mengacu pada pack, tidak dibuat salinan per bangunan atau level. Review pengguna sudah ACC dan tahap modelling selesai; apply ke Unity merupakan tahap berikutnya.
