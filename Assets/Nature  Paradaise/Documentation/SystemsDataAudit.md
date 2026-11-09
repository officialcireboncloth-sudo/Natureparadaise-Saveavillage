# Audit tanaman, pohon buah, dan ikan — 8 November 2026

Audit ini membandingkan source runtime, ScriptableObject, dan CSV yang ada dengan aturan yang ditulis di chat. Isi lengkap ketiga workbook Excel belum berhasil dimaterialisasi; angka harga, daftar spesies, musim, dan interval per jenis di workbook belum dapat dinyatakan cocok. Nama file dengan angka 13/43 tidak dijadikan sumber untuk mengarang spesies yang belum tersedia.

| Sistem | Yang sudah ada sebelum audit | Kekurangan yang ditemukan | Implementasi audit ini |
| --- | --- | --- | --- |
| Tanaman biasa | 1 crop: Cabbage; bibit, musim, pertumbuhan, siram, layu/mati, regrow, ledger kualitas pupuk/tanah/air, inventory dan save | Mutasi sebelumnya dijamin pada ★5; belum ada bonus panen 20%; harga memakai multiplier umum | Mutasi 1% per panen; bonus jumlah 2× dengan peluang 20% khusus ★5; harga eksplisit bintang 1–5 dan mutasi 3×; hasil undian disimpan agar gagal panen karena tas penuh tidak mengundi ulang |
| Pohon buah | 4 profil: Apple, Mango, Orange, Coconut; bibit, pertumbuhan, musim, siklus buah, tetap hidup setelah panen, save | Matang 56/84 hari tergantung profil, hasil tetap 1 buah, belum ada perawatan/bintang/mutasi | Matang 60 hari, ledger hari pupuk aktif dan minimum tanah, hasil 5/7/10/15/20, 1% mutasi khusus ★5 mengganti tepat satu buah, reset perawatan tiap panen, pupuk Super/Premium tetap berlaku sesuai sisa durasi |
| Ikan | 7 profil: Lake Bass, Mackerel, River Carp, Tilapia, Tuna, Koi, Marlin; filter perairan/musim/jam/cuaca/joran, encounter, bite/hook/minigame, ukuran, estimasi berat, kolam dan harga ukuran | Profil lengkap workbook belum tersedia; harga, ukuran/berat dan persebaran belum diverifikasi terhadap workbook | Fondasi fishing dipertahankan; jalur jual shop lama diperbaiki agar membaca kualitas dan ukuran stack yang benar |

## Aturan yang dipakai

- Tanaman: peluang mutasi 1% per aksi panen yang berhasil, pada kualitas apa pun. Satu undian menentukan jenis hasil untuk seluruh hasil tanaman tersebut; bonus jumlah adalah undian terpisah. Bonus 20% hanya untuk tanaman ★5, tidak berlaku pada pohon buah.
- Pohon: tidak dipupuk → ★1; lebih dari 0 tetapi kurang dari 50% → ★2; 50–kurang dari 80% → ★3; 80–kurang dari 100% → ★4; seluruh hari terpupuk dan tanah tidak pernah kurang dari 80 → ★5. Seluruh hari terpupuk tetapi tanah pernah di bawah 80 tetap ★4.
- Pohon ★5: 99% menghasilkan 20 buah normal, 1% menghasilkan 19 normal + 1 mutasi. Tidak ada tambahan buah dari mutasi.
- Efek pupuk aktif dihitung setiap hari tanpa perlu memakai item baru. Efek tetap berkurang selama jeda musim atau menunggu pengambilan buah. Riwayat kualitas hanya mencatat hari pertumbuhan dan pembentukan buah; berhenti sementara di luar musim setelah pohon dewasa atau saat sudah siap dipanen.
- Pertumbuhan pohon buah tetap berjalan sepanjang musim dan mencapai dewasa setelah 60 hari pertumbuhan kalender. Pohon liar memakai aturan lamanya.
- **Dewasa tidak sama dengan panen pertama.** Setelah dewasa, musim dan interval pembentukan buah masih berlaku. Empat profil saat ini mempertahankan 2 hari berbunga + 3 hari pembentukan buah; panen pertama paling cepat setelah 65 hari dalam musim yang cocok. Interval ini sementara mengikuti data lama, belum terverifikasi terhadap Excel.
- Riwayat pertumbuhan save lama yang belum tercatat tidak diberi kredit perawatan sempurna. Data perawatan dan undian tersimpan bersama progress pohon/crop. Pengambilan hasil normal dan mutasi masuk tas sebagai satu transaksi; tas penuh tidak menghabiskan siklus panen.

## Harga dan alur penggunaan

`ItemSO.sellPricesByStars` menerima tepat lima harga dari workbook. Nilai kosong mempertahankan harga legacy (`sellPrice × (1 + 0,2 × bintang)`), sehingga harga lama **belum dianggap harga final Excel**. `mutationSourceItem` menghubungkan varian dengan hasil normal; harga mutasi = 3 × harga normal pada bintang yang sama. Market/shipping memakai fungsi harga item; shop lama kini membaca kualitas/ukuran stack yang benar, termasuk tampilan harga unit.

CSV Items mendukung kolom opsional `sell_prices_by_stars` (lima angka dipisah `|`) dan `mutation_source_item_id`. CSV Crops mendukung `mutation_produce_item_id`, `mutation_chance`, dan `five_star_double_yield_chance`. CSV lama tetap dapat diimpor. Empat varian mutasi buah memakai folder item mutasi yang sudah ada dan ID item tersendiri.

Untuk memupuk pohon: pilih pupuk kualitas pada hotbar, arahkan tile pohon yang ditanam di field, lalu gunakan aksi tool seperti memupuk crop. Indikator menunjukkan pohon yang bisa dipupuk; efek aktif tidak bisa ditimpa, dan pohon siap panen tidak menerima pupuk. Prompt pohon menunjukkan bintang saat ini, hari terpupuk/tercatat, sisa efek, dan jumlah buah saat siap panen.

## Yang masih perlu dilengkapi

1. Impor daftar tanaman, pohon dan ikan lengkap dari isi workbook; sekarang hanya 1/4/7 profil yang dapat diverifikasi di repo.
2. Isi harga bibit dan harga hasil setiap bintang, musim buah, jeda panen tiap spesies, serta ukuran/berat/lokasi/jam ikan sesuai tabel asli. Nilai lama tidak diganti dengan tebakan.
3. Slot visual pohon telah menjadi lima tahap: biji, tunas, pohon muda, buah terbentuk, siap panen. **Model khusus lima tahap tiap spesies belum tersedia**: empat profil masih memakai mesh dasar dengan perubahan skala; tahap buah dan siap panen belum punya artwork berbeda. Ikon/model khusus empat buah mutasi juga belum dibuat.
4. Verifikasi interaksi hotbar, indikator, inventory, penyimpanan JSON, dan tampilan pohon langsung di Unity. Kompilasi di luar editor tidak menggantikan pengujian Play Mode.

## Verifikasi

- `Tests/FertilizerQualityTests.cs`: 65 pemeriksaan ledger tanaman/durasi pupuk.
- `Tests/HarvestSystemsTests.cs`: 66 pemeriksaan batas persentase, 60 hari, musim, reset/copy, migrasi save, peluang, hasil dan harga, menggunakan source runtime dengan stand-in engine minimal.
- `Tests/audit_cultivation_assets.py`: 58 pemeriksaan CSV, referensi asset mutasi, identitas, profil buah dan lima slot tahap.
- Menu Unity `Nature Paradise > Data CSV > Validate Harvest and Fruit Trees`: fixture scene sementara untuk transaksi tas penuh, reentry, jumlah 19+1, JSON, binding save, dan harga shop. Tidak memuat/menulis save pemain. Validator ini perlu dijalankan dalam editor.
- Validator Fertilizer Maker disesuaikan dengan aturan peluang; fixture memaksa undian berhasil agar tes tidak acak.
