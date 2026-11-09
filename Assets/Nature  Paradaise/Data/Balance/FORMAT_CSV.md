# Standar CSV Nature Paradise

Semua data memakai panel yang sama: **Nature Paradise > Data CSV > Balance CSV Panel**.

**Export → Edit CSV → Periksa → Import.** Tombol setiap kelompok ditempatkan dalam urutan yang sama. `Buka CSV` menunjukkan file/folder terkait. Export mengambil nilai asset dan menimpa CSV: impor dahulu edit yang belum diterapkan.

## Format file

| Bagian | Aturan |
| --- | --- |
| Encoding | CSV UTF-8; exporter menyertakan BOM agar Excel membaca teks dengan benar |
| Pemisah kolom | Koma `,` |
| Akhir record | CRLF; teks berisi beberapa baris tetap boleh di dalam tanda kutip |
| Header | Satu baris paling atas; nama kolom unik, tidak kosong, mengikuti hasil Export |
| Record | Satu item/entri/setting per baris sesuai kelompok data |
| Angka | Tanpa simbol G/Rp/persen dan tanpa pemisah ribuan: `6400` |
| Desimal | Titik, misalnya `0.25`; bukan `0,25` |
| Boolean | `TRUE` / `FALSE` |
| Nilai daftar | Umumnya `|`; contoh bahan resep `item.corn:5|item.soybean:1` |
| Harga kosong | Hanya untuk kolom yang mendukung nilai belum ditetapkan, misalnya harga Animals.csv; berbeda dari angka `0` |
| ID dan referensi | ID lama, GUID, asset_path dan binding teknis dipertahankan |
| Judul/catatan | Panduan ada di Markdown, bukan baris judul/total tambahan di dalam tabel CSV |

Kolom tetap mengikuti jenis datanya; format fisik dan cara kerja seragam. Jangan mengganti header atau menyamakan semua kategori menjadi kolom `name,value`: item, ikan, resep dan jadwal memiliki relasi yang berbeda. Untuk file baru gunakan nama kolom `snake_case` dan ID stabil; header khusus pada dataset lama tetap mengikuti schema exporter.

## Letak data

| Kelompok | File/folder | Yang diedit |
| --- | --- | --- |
| Item | Items.csv | Nama, harga, stack, efek dan relasi item |
| Tanaman | Crops.csv | Pertumbuhan, musim, hasil dan peluang panen |
| Hewan | Animals.csv | Daftar varian, fungsi, harga beli/dewasa/produk; kosong tetap belum ditetapkan |
| Ikan | Fish.csv | Habitat, waktu, ukuran, joran dan parameter menangkap |
| Resep | Recipes.csv | Bahan, hasil dan syarat masakan |
| Angka lanjutan | Settings/*.csv | Hanya `value`; satu property asset per baris |
| Kalender/cuaca/festival/cafe | Schedules/*.csv | Tanggal, jadwal, cuaca, acara dan menu |
| Stamina | Stamina/*.csv | CSV runtime; simpan lalu gunakan Periksa Stamina |

`Animals.csv` dan `Settings/Animals.csv` berbeda: yang pertama adalah daftar/harga hewan, yang kedua angka lanjutan seperti durasi pertumbuhan. Harga varian dan produk sebaiknya diedit melalui daftar utama agar relasinya ikut diterapkan. Peluang umumnya 0–1, sementara tabel probabilitas cuaca memakai persen 0–100; ikuti panduan tiap kolom.

## Membuka dan menyimpan

1. Export kelompok yang akan diedit, lalu buka file melalui Excel/Google Sheets sebagai UTF-8 dengan delimiter koma.
2. Pertahankan ID, header, dan binding yang sudah ada. Ubah nilai yang dibutuhkan; tidak perlu menambahkan judul, baris subtotal, sel gabungan atau formula.
3. Simpan sebagai **CSV UTF-8 (Comma delimited)**. Satu sheet menjadi satu CSV.
4. Jalankan Periksa, perbaiki pesan error, lalu Import kelompok tersebut. Periksa hasil dalam game.

CSV menyimpan data saja. Warna, font, filter dan lebar kolom Excel tidak tersimpan di CSV. Jika kelak membutuhkan workbook tampilan berwarna, gunakan template `.xlsx` sebagai tampilan kerja dan ekspor tiap sheet ke schema CSV ini.

Tombol **Rapikan format semua CSV** menyamakan encoding, pemisah dan akhir record pada file yang sudah ada. Header, urutan record dan nilai sel dipertahankan; salinan sebelum perubahan disimpan di `BalanceBackups/CsvFormat-*`. Tombol ini bukan impor data gameplay dan bukan pengganti Periksa tiap kelompok.

Verifikasi format: 43 pemeriksaan alat baca/tulis lolos, termasuk pembacaan dan penulisan ulang 25 file CSV project. Perbandingan seluruh nilai sel sebelum/sesudah perapihan tetap sama.
