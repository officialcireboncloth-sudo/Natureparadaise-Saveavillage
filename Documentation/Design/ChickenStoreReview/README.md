# Chicken Store — sketch proposal A

Status: ACC oleh pengguna 14 Oktober 2026 melalui permintaan eksekusi di Blender yang sudah terbuka. Model dan FBX tersedia di `ArtSource/ChickenStore_A`. Reference berasal dari gambar pengguna 14 Oktober 2026, disalin sebagai `reference.png`.

## Lingkup

Bangunan utama LIVE CHICKEN STORE dan kandang kecil merah di kanan dipersiapkan sebagai dua asset terpisah karena keduanya terlihat pada reference. Kandang diberi label bangunan pendamping untuk direview bersama. Pagar memakai kit modular `FarmBuildingsFence_A` yang sudah ada; tidak membuat ulang pagar. Exterior mencakup struktur, atap, lis, fondasi, cerobong, kanopi, counter yang menyatu bangunan, jendela/pintu, lampu fasad, papan nama dan relief logo.

Tidak termasuk ayam hidup/anak ayam, tanaman, bunga/pot, kebun, jalan, terrain, telur/keranjang, karung, tong, peti, ember/bak atau interior lengkap. Counter tetap kosong. Tampak toko memakai pintu tertutup supaya konstruksi daun/jendela pintu jelas; daun dibuat terpisah nanti, tetapi interior dan animasinya belum termasuk. Bukaan kecil kandang tetap berupa recess gelap kosong, tanpa ayam/telur.

PNG adalah sketch konsep dari built-in image_gen. Arsiran, serat dan guratan watercolor bukan tekstur game baru. Angka di dokumen ini merupakan sumber ukuran proposal; jangan mengukur ulang PNG generatif. Sisi belakang, posisi bukaan tersembunyi dan semua ukuran adalah usulan, bukan pengukuran reference.

## A — Toko utama

| Komponen | Ukuran usulan |
|---|---|
| Tapak badan W × D | 8,00 × 6,00 m |
| Eave / ridge atap utama | 3,10 / 4,70 m |
| Puncak gable tengah bertingkat | 5,60 m |
| Puncak dua cerobong | Maksimum 5,30 m |
| Fondasi / ambang pintu | 0,25 / 0,15 m |
| Overhang atap utama | 0,40 m tiap tepi |
| Envelope atap utama | 8,80 × 6,80 m sebelum tebal lis |
| Kanopi kiri W × proyeksi | 2,90 × 1,20 m dari dinding depan |
| Envelope visual W × D | Sekitar 8,80 × 7,60 m sebelum tambahan tebal lis/lampu |
| Bukaan pintu W × H | 1,40 × 2,45 m, dasar 0,15 m |
| Jendela kanan W × H | 1,60 × 1,65 m |
| Bukaan counter kiri W × H | 2,80 × 1,70 m |
| Counter permanen | Lebar 2,80 m, kedalaman 0,60 m, tinggi 1,00 m |
| Papan nama W × H | 4,40 × 0,75 m, crest lengkung terpisah di atas |
| Gable bawah / gable cap atas | Lebar 4,80 / 3,20 m |

Atap utama adalah pelana dengan ridge **kiri–kanan**, bukan gambrel barn. Gable tengah menonjol ke depan, dengan dua garis atap/lis bertingkat mengikuti reference. Cap atas diusulkan kedalaman 2,40 m, puncak 5,60 m dan ujung eave sekitar 4,45 m. Gable bawah diusulkan puncak 5,05 m dan ujung lis sekitar 3,40 m. Sambungan atap harus diselesaikan sebagai volume bertemu yang bersih saat blockout, bukan dua bidang yang bertumpuk/copanar.

Blender: X kanan, Y belakang, Z atas; depan -Y. Origin tengah tapak di tanah, 1 unit = 1 m. Dinding depan Y=-3 m, belakang +3 m. Pusat pintu X=+0,35 m, counter/kanopi X=-2,25 m, jendela kanan X=+2,65 m. Dua cerobong X≈±1,45 m, Y≈+0,30 m, penampang sekitar 0,40 × 0,45 m. Kanopi dipasang pada Z≈2,90 m dan turun ke tepi depan Z≈2,60 m, valance scallop sederhana. Lis, bracket dan ketebalan akhir dapat menambah envelope visual; ukuran final harus diaudit pada mesh.

Belakang diusulkan satu pintu servis 0,90 × 2,05 m dan satu jendela 1 × 1 m. Kiri satu shutter; kanan satu jendela kecil. Papan nama memakai tulisan persis **LIVE CHICKEN STORE**, relief siluet ayam sederhana, bukan hewan hidup. Daun ornamental pada sign dapat disederhanakan menjadi relief geometry; tidak memerlukan texture logo baru.

## B — Kandang pendamping

| Komponen | Ukuran usulan |
|---|---|
| Badan W × D | 3,00 × 2,80 m |
| Tinggi ridge / eave | 3,10 / 1,75 m |
| Fondasi | 0,15 m |
| Overhang | 0,25 m tiap tepi |
| Envelope atap W × D | 3,50 × 3,30 m sebelum ketebalan lis |
| Bukaan pop-door | 0,65 × 0,95 m, dasar 0,12 m |
| Ramp W × proyeksi | 0,65 × 0,70 m |
| Dua kotak sarang terpasang | Masing-masing 0,68 × 0,60 m, proyeksi depan 0,35 m |
| Vent lengkung loft | Sekitar 0,40 × 0,45 m |
| Ornamen ayam di ridge | Opsional, tambahan tinggi maksimum 0,35 m |

Pelana sederhana ridge depan–belakang, dinding papan merah, lis krem dan atap abu kehijauan. Kotak sarang berada dalam lebar badan, dengan lid kecil miring. Belakang diusulkan hatch perawatan tertutup 0,75 × 1,00 m. Tiap sisi satu ventilasi atas kecil 0,35 × 0,30 m. Ornamen ridge merupakan siluet/weather vane, dipisahkan dan opsional. Tinggi tanpa ornamen 3,10 m; dengan ornamen maksimum sekitar 3,45 m. Envelope termasuk ramp menjadi sekitar 3,50 × 3,75 m, sebelum detail tebal. Tidak mengganti `Coop_A` yang sudah ada.

## Audit atlas: bisa, dengan pendekatan palet + geometry

Atlas asli yang diperiksa: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd`, 2048 × 2048, GUID `522fcb2b5717c7640b460ee9ff73a78a`. SHA-256 sekarang sama dengan audit sebelumnya: `7fe0569f122bda89faca68565ea94bdb8be1431298424424f2776f23e987e231`. Material `TFP_Atlas_1A.mat` mengarahkan `_MainTex` dan `_TextureSample` ke GUID tersebut. Sampling RGB/UV dilakukan dari composite atlas asli yang sama untuk preview Blender; JSON hasil di `atlas-audit.json`.

| Bagian | Bisa memakai atlas? | Cara eksekusi |
|---|---|---|
| Kayu, plester, cat merah, batu, metal | Ya | UV tiap face ke patch palet; timber/board/bracing berupa mesh |
| Atap sirap | Ya untuk warna | Bentuk/batas genteng geometry sederhana; atlas tidak menyediakan pola genteng reference ini |
| Kanopi merah–ivory | Ya | Strip dan valance mesh, UV bergantian ke dua patch; tidak perlu texture garis baru |
| Papan nama dan logo ayam | Ya untuk warna | Teks triangulated mesh dan relief siluet; tidak ada tulisan/sign persis ini dalam atlas |
| Jendela hangat | Ya, opaque stylized | Patch amber di bidang inset plus mullion geometry; transparansi kaca tidak diperlukan |
| Lampu | Ya untuk warna | Besi + patch kuning; emission opsional setelah memeriksa material lights pack yang sudah ada |
| Serat kayu / anyaman kain / goresan realistis | Tidak sebagai pola siap pakai reference | Disederhanakan pada gaya stylized; tidak menjanjikan detail gambar yang sama persis |

Palet RGB yang disampling: kayu (163,115,56), plester (225,220,184), atap (73,90,95), kanopi merah (197,66,40), ivory cerah (255,246,236), batu (176,160,148), besi (49,49,49), kaca amber (223,188,87), lampu (255,217,86). Koordinat patch dan margin UV tersimpan di JSON; swatch sheet memakai RGB asli tersebut.

Kesimpulan: **tidak perlu atlas/material albedo baru** untuk model exterior stylized ini. Satu material opaque bersama adalah target utama. Glow khusus, jika dipakai, menggunakan material pack yang sudah tersedia dan diverifikasi lagi saat modeling. Warna akhir perlu diperiksa di shader Toon Unity, karena preview Blender tidak identik dengan shader project.

## Bahan untuk eksekusi sesudah ACC

Reference asli, dua turnaround, ukuran proposal dan audit atlas disimpan dalam folder ini. Collection sumber Blender dipisahkan menjadi Store_Main, Coop_Companion, Export dan Preview. Subbagian atap, timber, plester/papan, kanopi, counter, pintu, papan nama, jendela dan hardware tetap dapat diedit. Target awal toko sekitar 10–18k triangles dan kandang 3–6k, disesuaikan setelah audit tanpa menambah mikrodetail tak terlihat.

Modeling telah diotorisasi dan dikerjakan di Blender. Audit mesh, UV, ukuran dan impor ulang FBX dicatat di `ArtSource/ChickenStore_A/Previews/Verification.json`. FBX memakai file root identity dan mempertahankan transform child/engsel; konversi ke Unity mengikuti audit farm sebelumnya. Pagar tetap reuse prefab modular. Integrasi Chicken Store ke Map merupakan tahap berikutnya.
