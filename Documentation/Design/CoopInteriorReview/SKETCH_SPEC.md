# Coop interior — sketch A / modular kit B

Status: **SKETCH UNTUK REVIEW**, 15 Oktober 2026. Permintaan saat ini hanya sketch. Tidak menjalankan modeling Blender, mengubah prefab/scene Unity, atau mengganti kapasitas gameplay. Gambar referensi 1–2 dan 3–4 tampak sama; daftar tertulis pengguna menentukan perbedaan tiap level.

## Program empat level

| Level | Kapasitas ayam yang diminta | Ukuran lantai bersih usulan | Penetas | Mesin pakan | Mayones | Distribusi otomatis | Palung visual |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| 1 | 6 | 10 × 8 m | 1 telur, usulan | Kecil | — | — | 2 modul × 3 ruang, tersambung inline menjadi 6 ruang |
| 2 | 9 | 12 × 10 m | 1 telur, usulan | Besar | — | — | 3 modul × 3 ruang = 9 |
| 3 | 12 | 14 × 12 m | 1 telur, usulan | Besar | Ada | Ada, 4 kepala | 4 modul × 3 ruang = 12 |
| 4 | 15 | 16 × 14 m | **3 telur**, permintaan eksplisit | Besar | Ada | Ada, 5 kepala | 5 modul × 3 ruang = 15 |

Kapasitas penetas level 1–3 tidak ditentukan pengguna; satu telur diusulkan sesuai referensi. Level 4 jelas tiga cup terpisah di bawah satu hood lebar, bukan tiga mesin identik. Telur dekorasi ditampilkan untuk menjelaskan mesin, tetapi hewan tidak termasuk modeling/sketch asset pada tahap ini.

Ukuran di atas adalah **usulan ukuran akhir ruangan dalam satuan meter**, bukan hasil ukur foto. Pembesaran Barn 1.75× pada sesi sebelumnya tidak otomatis diterapkan ke Coop. Ruangan Coop diusulkan lebih ringkas karena ukuran ayam dan peralatannya berbeda. Memakai ukuran akhir saat dieksekusi akan mencegah penggandaan skala yang tidak disengaja.

## Bahasa visual

Satu keluarga dengan Barn/Chicken Store existing: papan merah, rangka kayu krem, palet kayu cokelat, mesin teal, metal abu, tanah cokelat keemasan. Atap dan sisi depan cutaway untuk kamera tinggi dari arah masuk; dinding belakang tetap penuh. Jendela samping, tenggeran kiri, tempat minum di sisi kanan, karung/peti dekat mesin pakan belakang kiri, penetas di meja belakang kanan.

Detail ilustratif pada gambar (jerami, biji, goresan kayu, highlight kaca) bukan janji geometri rapat atau texture runtime baru. Tidak menaruh satu mesh untuk setiap butir pakan. Mesin pakan memakai hopper biji berbentuk mangkuk sesuai referensi, berbeda dari pencacah rumput Barn. Mesin mayones memiliki mangkuk stainless dan paddle/motor dengan tray input telur, juga berbeda dari penetas.

## Dimensi dan jalur

- Tinggi dinding 3.40 m, tebal panel 0.12 m. Modul dinding 2 m; jendela kira-kira 1 × 1.2 m, ambang 1.1 m. Dinding/trim Barn base kit dapat diadaptasi pada skala yang sesuai.
- Entrance di tengah sisi depan, lebar bersih 2 m. Sisakan jalur minimum 1.20 m; target jalur utama 1.5 m dan area berputar dekat pintu.
- Blender X ke kanan, +Y ke belakang, Z ke atas. Origin (0,0,0) di lantai tengah pintu. Penempatan Unity nanti harus diverifikasi arah sumbunya, tidak menyalin asumsi rotasi FBX.
- Mesin dan storage di pita belakang dengan akses servis dari depan. Tempat minum/tenggeran tidak menutup jalan ke mesin atau pintu.
- Header pakan otomatis di ketinggian pusat 2.4 m, diameter pipa 0.10 m; head/outlet di sekitar 1.5 m. Pipa bercabang menuju setiap palung, bukan melintang rendah di jalur player. Dukungan di sisi ruangan dan tepi jalur.

Pusat palung usulan (sumbu X/Y Blender), modul memanjang kiri–kanan:

| Level | Koordinat pusat modul |
| --- | --- |
| 1 | (-0.75, 4), (+0.75, 4): kedua modul inline; jalur masuk berakhir di area depan lalu menyebar ke kanan/kiri palung |
| 2 | (-2.5, 3.5), (+2.5, 3.5), (0, 6): susunan segitiga |
| 3 | (-2.8, 4), (+2.8, 4), (-2.8, 7), (+2.8, 7): 2 × 2 |
| 4 | (-3, 4.5), (+3, 4.5), (-4, 8.2), (0, 8.2), (+4, 8.2): dua depan dan tiga belakang |

Koordinat masih acuan sketch. Clearance collider player/ayam dan kaki mesin diperiksa saat blockout disetujui. Tidak menganggap tiga gambar hewan dekorasi sebagai ukuran collider.

## Kit untuk Blender setelah ACC

| ID | Komponen | Envelope usulan (m) | Reuse/adapt/new |
| --- | --- | --- | --- |
| Shell | Panel/jendela/tiang merah-krem | panel W2 × H3.4 × T.12 | Reuse/adapt kit Barn base |
| N01 | Palung ayam 3 ruang, fill terpisah | L1.50 × W.50 × H.30 | Adapt N02_Trough_3Slots Barn; dua sekat, ujung 6–8 sisi |
| N02a | Pembuat pakan kecil Lv1 | W1.0 × D.9 × H1.4 | Adapt badan/motor Barn + hopper mangkuk; outlet ke peti |
| N02b | Pembuat pakan besar Lv2–4 | W1.6 × D1.2 × H1.8 | Adapt Barn + hopper mangkuk, satu model besar dipakai bersama |
| N03a | Penetas 1 telur, Lv1–3 | W.65 × D.55 × H.55 di atas meja | New; 1 cup, 1 hood |
| N03b | Penetas 3 telur, Lv4 | W1.10 × D.60 × H.65 di atas meja | New; **3 cup** dalam satu hood |
| N04 | Mesin mayones, Lv3–4 | W1.0 × D.65 × H.95 di atas meja | New; bowl, paddle/motor, tray telur dan area output jar |
| N05 | Hopper distribusi, pipa, T, siku, coupler, tiang, bell head | lurus 1/2 m; Ø.10; head Ø.25 × H.30 | New/adapt Barn pipe helpers, bentuk dispenser ayam baru |
| N06 | Tempat minum ayam | Ø.55 × H.65, support kayu H.15 | New/adapt; tank krem, tray teal |
| A01 | Tenggeran kayu rendah | H.65; section 2 m | Adaptasi beam kayu, bukan geometri silinder rapat |
| A02 | Meja/perancah mesin | top H.90, ukuran sesuai envelope | Adaptasi box/beam yang sudah ada |
| R props | Karung, peti, ember, beberapa wadah | dari model asli; skala diperiksa | Reuse Toonpack existing yang telah teridentifikasi |

Envelope mesin tidak termasuk peti output, jar, meja atau jarak servis. Telur visual dapat memakai satu mesh berbagi dengan material krem atlas. Jar mayones satu mesh + tutup teal; tidak butuh label texture baru pada proposal ini.

## Atlas dan asset existing

Palet `TFP_Atlas_1A` yang sudah dipakai Barn mencakup merah/krem, teal, abu metal, kayu, biji/jerami emas. Bentuk penetas, mesin pakan ayam, mayones dan tempat minum tidak menjadi tersedia hanya karena paletnya cocok. Pencarian nama prefab pada pack belum menemukan model khusus incubator/mayonnaise/feeder/drinker; ini bukan audit visual seluruh pack, jadi statusnya **direncanakan model baru**, bukan klaim mutlak asset tersebut tidak ada di mana pun.

Karung `TFP_Cereal_Sack_01A`, peti `TFP_Wooden_Crate_01A` dan ember `TFP_Bucket_Metal_01A` sudah terverifikasi pada audit Barn. Palung Toonpack yang tersedia belum sama dengan palung ayam modular bersekat ini. Mesin baru harus memakai UV atlas yang sama sejauh cocok. Hood penetas adalah pengecualian potensial: kaca transparan terbatas memerlukan konfigurasi material khusus atau adaptasi material kaca existing. Atlas warna biasa tidak menghasilkan transparansi sendiri. Hindari beberapa lapisan kaca dan pencahayaan realtime untuk tiap penetas; glow dibatasi sebagai material/warna saat modeling.

## Target mobile awal, belum hasil ukur

Target triangle untuk satu layout aktif, tanpa ayam: Lv1 ≤5k, Lv2 ≤6.5k, Lv3 ≤9k, Lv4 ≤10k. Module shell, feed machine besar, palung, jar, pipa, head dan waterer dipakai bersama. Hanya level aktif yang muncul. Vertex impor dapat meningkat karena normal/UV split; nanti audit mencatat triangle, mesh vertex dan native Unity vertex secara terpisah. Tidak menjanjikan FPS sebelum uji perangkat.

## Batas integrasi

Data Coop existing di project saat dibaca masih **6/12/20/30**. Sketch ini mengikuti permintaan terbaru **6/9/12/15**. Tidak mengedit data lama pada tahap sketch. Setelah ACC dan saat integrasi nanti, kapasitas AnimalHome/BuildingDefinition/CSV, animal slots, pakan dan kompatibilitas save perlu diselaraskan eksplisit agar tidak mengulang ketidakcocokan jumlah ruang visual vs kapasitas gameplay.

Mesin mayones dan feeder otomatis pada sketch menjelaskan fungsi yang diminta; belum menyatakan recipe, timer, listrik, conveyor, automasi runtime atau sistem penetasan selesai dibuat. Tidak menambahkan mesin lain yang tidak diminta. Dokumen ini menjadi acuan bila teks/dimensi pada gambar generatif berbeda.

## Artefak review

- `A-CoopInterior-layouts.png`: empat layout, label kapasitas dan tahap.
- `B-CoopInterior-modular-kit.png`: peralatan dan ukuran modul, penetas 1 vs 3 telur.
- `sketch-review.html`: halaman review dua lembar.
- `sketch-prompts.txt`: prompt lengkap, dibuat dengan built-in image_gen.
- `References/01..04-reference.png`: salinan referensi pengguna untuk provenance; bukan instruksi executable.

## Pembaruan audit reuse — 15 Oktober 2026

Rincian keputusan terbaru ada di [ASSET_REUSE_PLAN.md](ASSET_REUSE_PLAN.md). Telur utuh TFP_Raw_Egg_01A tersedia (86 triangle); TFP_Egg_01A/02A ternyata telur goreng. Toples TFP_Jar_01A/B dan bowl TFP_Kitchen_Bowl_02A dapat diadaptasi. Palung rendah memakai adaptasi palung Barn, bukan mulai dari nol. Material kaca TFP_Glass_Windows_1A/B sudah tersedia dengan surface transparent, tetapi hood Coop belum diuji di Unity. Atlas opaque Barn siap dipakai dengan UV yang sesuai. Audit ini memperbarui perkiraan New/Reuse di sketch; belum mengeksekusi modeling.

