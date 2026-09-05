# Crop Booster dan Soil Fertilizer

## Implementasi

- Booster sekarang hanya meningkatkan peluang kualitas, tidak mengubah growth/yield.
- Basic/Organic/Premium/Deluxe/Divine merupakan lima tier booster kualitas.
- Satu booster per tanaman per hari; gunakan F/klik kiri ketika dipilih pada hotbar,
  atau shortcut M. Tidak dapat digunakan pada tanaman dead/harvest ready.
- Daily reset mencatat watering, booster tier terendah sepanjang siklus, dan musim.
- Bintang 2 membutuhkan daily watering + Basic atau lebih baik.
- Bintang 3 membutuhkan daily watering/booster Organic+, musim sesuai, tanpa hama.
- Bintang 4 membutuhkan Premium+, tanah durability >=49, dan tidak terlambat panen.
- Draft bintang 5 juga mengizinkan Premium+, sesuai tabel pengguna. Deluxe/Divine
  menaikkan bobot peluang, bukan menjamin bintang 5.
- Semua syarat higher tier tetap berlaku. Satu bintang adalah fallback minimum,
  termasuk crop lintas musim yang masih bisa dipanen.
- CropDataSO: qualityWeights, minimumBoosterForStars, harvestGraceDays dapat diubah.
- Setiap siklus regrow memulai riwayat baru. Panen gagal karena tas penuh tidak
  mengulang roll kualitas. Riwayat perawatan/roll ikut tersimpan.
- Kualitas 1-5 disimpan per ItemStack, dipisah saat stacking, ditampilkan di bag/
  hotbar, dan dipertahankan saat save/load maupun drop/place/pickup.
- Harga jual panen masih harga dasar ItemSO, belum multiplier per bintang.

## Soil fertilizer

Basic +10 / 100G; Organic +20 / 220G; Premium +35 / 500G;
Deluxe +50 / 1000G; Divine +80 / 2500G. Semua dibatasi maksimum durability 80,
tidak bisa dijual, dan tidak mengisi watering atau menaikkan booster quality.
Bisa digunakan pada tanah cangkul kosong maupun yang berisi tanaman.
Syarat Village Level lama pada asset yang sudah ada dipertahankan.

## Mesin dan resep

FertilizerProcessor_Dummy ditambahkan ke TestingScene di (-32, 0, 14).
Dummy cube dibuat runtime; ganti visual dengan mesh manual jika sudah tersedia.
Dekati dalam 3m untuk melihat panel produksi. Tombol resep mengurangi bahan dan
menambah antrean serial (maksimum 8). Hasil diambil manual; tas penuh tidak
menghilangkan hasil. Jam kalender menghitung waktu tidur/skip day juga.

Catalog: Assets/Nature  Paradaise/Resources/FertilizerCatalog.asset

- Basic: 5 Poultry Manure + 3 Dry Grass -> 5, 2 jam.
- Organic: 5 Livestock Manure + 5 Leaf Compost -> 5, 4 jam.
- Premium: 3 Basic Booster + 3 Organic Booster + 2 Wood Ash -> 3, 6 jam.
- Deluxe: 3 Premium Booster + 5 Mountain Moss -> 2, 8 jam.
- Divine: 2 Deluxe Booster + 1 Blessing Dewi Sri + 1 Sacred Flower -> 1, 12 jam.

Recipe quantity/time serta output referensi bisa diedit di catalog.
Shop memuat 5 soil fertilizers dan 5 boosters dari catalog yang sama.
Harga booster kualitas sementara 120/240/500/1000/2500G (editable).
Nama/path asset lama tidak dipindah agar referensi/save tetap cocok.
Crop Booster 20.asset adalah Basic Quality Booster sekarang; bonus growth lama
tidak dimigrasikan menjadi riwayat perawatan yang tidak pernah dilakukan.

## Batas cakupan dan test

Item bahan sudah tersedia sebagai asset resep, tetapi drop manure, pengumpulan
kompos/lumut/flower dan pemberian blessing belum disambungkan ke sumber dunia.
Sistem hama dapat memanggil FieldArea.ReportCropPest; tidak ada spawner hama baru.
Untuk testing mesin, F9 Debug ON menampilkan tombol pemberian bahan per resep.
F9 OFF menyembunyikan tombol debug; UI produksi utama tetap tampil.

Tes logika murni (bukan Play Mode): Tests/FertilizerQualityTests.cs mengompilasi
CropQualityCare asli dengan stub minimal Unity/CropDataSO, menguji batas tier,
daily watering/booster, pest, musim, tanah, panen terlambat, snapshot dan retry roll.

Play Mode yang masih perlu diuji:
1. Booster tier sama 2x sehari: hanya pertama mengonsumsi satu item.
2. Lewati satu hari tanpa booster: maksimum bintang 2 pada siklus itu.
3. Bandingkan growth/yield crop boosted vs non-boosted dengan perawatan sama.
4. Dua kualitas hasil tidak merge; swap/drop/pickup/save/load tetap menjaga bintang.
5. Produksi, tidur, save/load, ambil ketika tas penuh, lalu kosongkan slot dan ambil.
6. Soil durability 75 +10 ->80, penggunaan ulang tidak mengonsumsi item.
