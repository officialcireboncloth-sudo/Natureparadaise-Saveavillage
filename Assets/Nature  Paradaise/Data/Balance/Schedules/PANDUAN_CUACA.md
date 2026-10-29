# Mengatur cuaca

Satu tahun hanya **4 bulan**: Spring 31 hari, Summer 28 hari, Autumn 31 hari, Winter 30 hari. Jadwal menyediakan tahun 1–10. Setelahnya peluang musim tetap berulang.

## Cuaca normal: WeatherProbabilities.csv

Satu baris untuk satu bulan. Angka cuaca adalah persen, dan total setiap baris wajib **100**. `month`, `season`, dan `days` menunjukkan bulan yang sedang diedit; jangan mengubah panjang bulannya.

| Bulan | Musim | Hari | Peluang yang disepakati |
|---|---|---:|---|
| 1 | Spring | 31 | Hujan 50%, Cerah 30%, Gerimis 20% |
| 2 | Summer | 28 | Gerimis 10%, Hujan 15%, Panas 75% |
| 3 | Autumn | 31 | Hujan 10%, Gerimis 5%, Cerah 34%, Mendung 50%, Badai Hujan dan Angin 1% |
| 4 | Winter | 30 | Hujan Salju 60%, Cerah 38%, Badai Salju 2% |

Ini peluang per hari, bukan jatah pasti jumlah hari dalam sebulan.

## Cuaca tanggal tertentu: WeatherSchedule.csv

| Kolom | Isi |
|---|---|
| year | Tahun permainan, 1–10 |
| month | Bulan 1 Spring, 2 Summer, 3 Autumn, 4 Winter |
| season | Nama musim, harus cocok dengan month |
| day | Tanggal di dalam bulan tersebut |
| weather | `Auto` untuk peluang musim, atau nama cuaca untuk tanggal khusus |
| note | Catatan bebas; tidak memengaruhi cuaca |

Contoh: untuk membuat **tahun 1, Spring tanggal 5** cerah, cari baris `1,1,Spring,5,Auto,` lalu ganti hanya `Auto` menjadi `Cerah`. Untuk mengembalikan cuaca normal, ubah lagi menjadi `Auto`.

Nama yang diterima: Cerah, Cerah Berawan, Panas, Gerimis, Hujan, Hujan Lebat, Mendung, Badai Hujan dan Angin, Topan, Badai Petir, Hujan Salju, Badai Salju. Nama Inggris seperti `Sunny`, `Drizzle`, `HeavyRain`, `Cloudy`, `Snow` juga tetap diterima. `Auto` atau sel kosong memakai peluang musim.

Jangan menghapus baris tanggal; tersedia 1200 baris untuk 10 tahun. Summer tidak memiliki tanggal 29–31. Tanggal duplikat, cuaca salah ketik, atau persentase bukan 100% akan membatalkan impor; aturan sebelumnya tetap dipakai.

`CalendarDates.csv` adalah daftar kalender yang sama, dengan nama bulan dan hari yang terbaca. Edit `note` untuk catatan kalender. Gunakan WeatherSchedule untuk mengubah cuaca, bukan CalendarDates.

## Menerapkan ke Unity

1. Simpan CSV dari editor teks atau Excel sebagai **CSV UTF-8 dengan pemisah koma**.
2. Simpan dengan **Ctrl+S** di WPS/Excel, lalu kembali ke Unity. CSV boleh tetap terbuka di WPS. Saat Refresh, kedua CSV cuaca otomatis divalidasi dan diimpor, termasuk jika sedang Play. Jika file masih sibuk disimpan, impor mencoba lagi sebentar. Jika Auto Refresh dimatikan, tekan **Ctrl+R**. Sebelum masuk Play, Unity juga membaca ulang CSV yang sudah tersimpan agar jadwal terbaru diterapkan sebelum cuaca hari pertama dibuat.
3. Pilih WeatherSystem di Hierarchy. `Forecast Mode` harus **Automatic**. Inspector menampilkan sumber cuaca yang aktif. Tombol **Gunakan CSV / Probabilitas Musim (Automatic)** mengakhiri pilihan cuaca manual.

Impor khusus cuaca juga tersedia melalui **Nature Paradise → Data CSV → Weather → Import Weather CSV**. Perubahan menu cafe tidak menghalangi impor cuaca. `Export Weather CSV` menulis ulang CSV dari asset; jangan gunakan Export untuk menerapkan CSV yang baru diedit.

Dalam Automatic, tanggal khusus mengalahkan peluang musim. Cuaca dari save otomatis dihitung lagi memakai aturan terbaru dan seed save tersebut, sehingga Gerimis lama tidak menutupi tanggal yang sekarang diatur Cerah. Tanpa perubahan aturan, hasil tetap deterministik. Save dengan cuaca **Manual** sengaja mempertahankan pilihan pengujian sampai Automatic dipilih. Preview musim pada menu debug juga harus dikembalikan ke kalender jika ingin mengikuti jadwal tanggal.

Untuk client, sertakan CSV, script, `Game Schedules.asset`, dan `Season Weather Probabilities.asset` beserta meta yang berubah. Build memakai asset hasil impor, bukan membaca CSV dari disk. Tidak perlu New Game untuk menerapkan aturan baru pada save Automatic.

Jika profil peluang menampilkan Missing Script, CSV yang benar tetap tidak dapat diterapkan. Referensi script pada profil sudah diperbaiki pada revisi ini; pastikan file profil dan meta script ikut dikirim bersama, bukan hanya CSV.
