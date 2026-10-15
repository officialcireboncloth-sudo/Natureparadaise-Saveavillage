# Barn interior — sketch proposal A / modular kit B

Status: **ACC modeling 14 Oktober 2026; selesai dibuat di ArtSource/BarnInterior_A**. Dokumen ini menyimpan spesifikasi sketch yang menjadi acuan. Dua lembar visual: `A-BarnInterior-layouts.png` untuk empat tahap, `B-BarnInterior-modular-kit.png` untuk kit yang perlu dibuat. Hewan tidak digambar/dimodelkan pada tahap ini. Integrasi Unity belum dilakukan.

## Arah visual dan lingkup

Interior barn merah dengan rangka kayu krem, lantai tanah cokelat keemasan, jerami secukupnya, peralatan teal dan palung/pipa abu metal. Kamera tinggi dari sisi pintu depan mengikuti referensi. Atap dan dinding depan tidak ditampilkan pada sketch agar area aktivitas terbaca; saat implementasi dipilih cutaway/hide sesuai kamera. Dinding belakang utuh, jendela di samping, lentera hangat memakai model yang sudah pernah dibuat.

Satu keluarga ruangan dan peralatan berkembang dari manual, motor, distribusi otomatis, sampai tangki proses. Reuse bentuk prop yang terbukti sesuai dari audit; model baru fokus pada struktur interior dan peralatan utama. Detail serat jerami, baut dan sambungan tidak dimodelkan satu per satu.

## Dimensi/layout usulan

| Tahap visual | Ukuran bersih usulan W × D | Palung pakan | Peralatan belakang |
|---|---|---|---|
| 01 Manual | 12 × 10 m | 2 × 3 kompartemen | Pencacah kecil berengkol, 1 bak air, peti output dan prop dasar |
| 02 Motor | 16 × 12 m | 3 × 3 kompartemen | Pencacah bermotor, 2 bak air, rak, papan alat, lemari perlengkapan |
| 03 Auto | 20 × 14 m | 4 × 3 kompartemen | Pencacah besar, mixer/hopper, pipa melintang + 4 dispenser, sink dan storage |
| 04 Process | 24 × 16 m | 5 × 3 kompartemen | Sistem tahap 03 + 5 dispenser, 2 instance tangki proses dan counter botol |

Tinggi dinding 3,40 m; tebal panel sekitar 0,12 m; modul panel umum lebar 2,00 m. Jendela 1,00 × 1,20 m, ambang Z=1,10 m. Depan terbuka pada gambar, tetapi posisi pintu/exit tetap ditandai di tengah. Warna dan bentuk lentera mengikuti model Chicken Store yang sudah tersedia; tidak perlu membuat seri lampu baru.

Ukuran tersebut **usulan area interior yang dipadatkan**, bukan pengukuran reference dan bukan ukuran placeholder yang telah diterapkan. Placeholder project saat ini adalah 22 × 24, 28 × 30, 36 × 38 dan 46 × 48 m. Persetujuan sketch ini masih harus mencakup pilihan footprint; belum ada collider, navmesh, kamera atau marker yang diubah. Ruang interior merupakan scene tersendiri sehingga ukuran bersihnya tidak diambil langsung dari tapak exterior.

Sumbu untuk persiapan modeling: Blender X kanan, Y ke belakang, Z atas. Origin lantai bersih di tengah sisi masuk: X=0, Y=0, Z=0; dinding belakang Y=D. Pusat pintu/exit X=0, Y=0. Tebal dinding dibangun ke luar area bersih. Palung memanjang ke arah belakang (Y). Target jalur utama 1,80–2,00 m, jalur samping minimum 1,20 m; zona dekat hewan nantinya diperiksa lagi memakai ukuran collider hewan/player asli.

Usulan pusat palung (X, Y), bukan posisi kode final:

- 01: X=-2,30 dan +2,30; Y=4,50 m.
- 02: X=-4, 0, +4; Y=5,50 m.
- 03: X=-6, -2, +2, +6; Y=6,00 m.
- 04: X=-8, -4, 0, +4, +8; Y=6,70 m.

Sisakan foreground sebagai akses masuk dan ruang aktivitas. Peralatan serta stok dikelompokkan di pita belakang sekitar 2–3 m kedalaman. Pipa melintang pada tahap otomatis dipasang di atas ujung belakang palung, tidak melintang rendah di jalur masuk. Penyangga berada di kedua ujung, di luar jalur utama.

## Kit B — yang perlu dibuat

| ID | Modul | Ukuran usulan |
|---|---|---|
| N01 | Kit shell interior: lantai, panel merah, balok krem, jendela/pintu, bracket | Panel 2,00 W × 3,40 H × 0,12 tebal m; variasi ujung/jendela |
| N02 | Palung stainless 3 ruang dengan kaki dan fill terpisah | 3,60 L × 0,90 W × 0,65 H m; 2 sekat; panjang tiap ruang sekitar 1,20 m sebelum tebal rim |
| N03a | Pencacah kecil + engkol | 1,20 W × 1,00 D × 1,40 H m |
| N03b | Pencacah besar + motor | 2,80 W × 1,50 D × 2,10 H m; varian motor menengah memakai komponen keluarga yang sama |
| N04 | Mixer/hopper kedua dengan outlet/panel | 1,40 W × 1,20 D × 2,00 H m |
| N05a | Pipa lurus/siku/T/coupler dan tiang | Diameter pipa sekitar 0,12 m; sumbu pipa utama Z=2,70 m; section 6–8 sisi |
| N05b | Kepala dispenser berulang | 0,40 W × 0,35 D × 0,65 H m; dasar outlet Z=1,40 m |
| N06 | Tangki proses (model sama dipakai 2 kali) | Diameter sekitar 1,00 m; tinggi total 2,10 m termasuk kaki/lid |
| Detail opsional | Kotak P3K, label, panel kecil, adapter, patch jerami sederhana | Ditentukan setelah blockout; gabung ke mesh terkait bila statis |

Tangki tahap 04 diberi nama **process tank**: fungsi susu/cairan/produk belum dikunci oleh gambar. Ukuran adalah envelope komponen utama; engkol, hose, peti output dan jarak servis dapat menambah ruang yang perlu dipesan. Peti, botol, ember dan sebagian alat memakai asset existing.

## R / A / N pada sketch

- **R — Reuse:** tong, ember, peti, karung, botol/wadah susu, alat tangan dan sebagian jerami dari Toon Farm Pack. Lentera berasal dari model custom Chicken Store yang sudah ada.
- **A — Adapt:** kabinet perlengkapan, sink, rak penyimpanan dan bak air; ubah penyangga, warna, komposisi atau proporsi bila perlu. Tidak menganggap papan `Shelf` sebagai rak lengkap.
- **N — New:** shell, palung bersekat, keluarga mesin, kit pipa/dispenser dan tangki.

Asset R tetap diperiksa budgetnya. Bal jerami `TFP_Hay_Bale_01A` asli 888 triangle; bila jumlahnya banyak, usulkan versi sederhana atau LOD. Rumput/jerami 3D tidak dipakai menutupi seluruh lantai. Detail arsiran pada sketch merupakan ilustrasi, bukan janji jumlah mesh atau texture runtime baru.

## Batas mobile yang diusulkan

Target awal untuk **satu layout aktif**, di luar hewan: tahap 01 ≤6k, 02 ≤8k, 03 ≤12k, 04 ≤15k triangle. Ini target desain yang masih harus diaudit, bukan hasil ukur model yang sudah dibuat. Scene hanya menampilkan tahap aktif. Shell dan kit berulang dipakai bersama; cukup satu model tangki untuk dua instance, satu kepala dispenser untuk semua output, dan satu keluarga palung.

Target modul: shell 1–2k; palung lengkap 150–250 triangle; pencacah kecil 500–800, besar 900–1.400; mixer 500–900; dispenser 100–180; tangki 300–500. Prop yang direuse masuk hitungan total, termasuk semua pengulangannya. Pakai material atlas bersama sejauh cocok; air/lampu bisa memerlukan pengaturan material terpisah. Hindari point light per lentera dan fill jerami yang berupa ribuan batang.

## Yang tetap menjadi keputusan integrasi berikutnya

Kapasitas slot pakan di kode saat ini 4/8/14/20. Sketch mengikuti jumlah palung pada reference, yaitu 2/3/4/5, masing-masing tiga kompartemen visual. **Jumlah kompartemen belum dipetakan satu-banding-satu ke kapasitas gameplay.** Kapasitas, save, animal slots dan mesin tidak diubah hanya karena sketch ini. Saat implementasi, marker dan visual harus diikat ulang tanpa membuat palung Cube lama bertumpuk.

Sketch tidak mengubah scene Unity maupun model Blender. Setelah disetujui: blockout dimensi dan jalur dengan ukuran karakter asli → pilih prop R/A final → model kit N dan versi ringan prop mahal → audit geometry/material → integrasi visual/controller → uji kamera/pathing di perangkat target.

## Artefak

- `A-BarnInterior-layouts.png`: empat layout cutaway untuk review.
- `B-BarnInterior-modular-kit.png`: dimensi dan pembagian kit baru.
- `SKETCH_SPEC.md`: acuan ukuran/lingkup utama. Bila anotasi gambar generatif berbeda, dokumen ini yang dipakai untuk pelaksanaan.
- `sketch-prompts.txt`: prompt lengkap built-in image_gen.
- `sketch-review.html`: halaman review kedua lembar dan ringkasan dimensi.
- `README.md`, `asset-shortlist.csv`, `asset-gallery.html`: audit prefab existing yang mendasari pilihan R/A/N.
