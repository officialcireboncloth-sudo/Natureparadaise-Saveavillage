# Kalender empat bulan dan cuaca

Satu tahun berisi 120 hari. Setelah Winter, kalender kembali ke Spring tahun berikutnya.

| Bulan | Nama HUD | Hari | Peluang cuaca per hari |
|---|---|---:|---|
| 1 — Spring | Musim Hujan | 31 | Hujan 50%, Cerah 30%, Gerimis 20% |
| 2 — Summer | Musim Panas | 28 | Gerimis 10%, Hujan 15%, Panas 75% |
| 3 — Autumn | Musim Gugur | 31 | Hujan 10%, Gerimis 5%, Cerah 34%, Mendung 50%, Badai Hujan dan Angin 1% |
| 4 — Winter | Musim Salju | 30 | Hujan Salju 60%, Cerah 38%, Badai Salju 2% |

Peluang merupakan undian per hari; bukan jatah pasti dalam satu bulan. Edit `Data/Balance/Schedules/WeatherProbabilities.csv`; setiap bulan wajib berjumlah 100%. Impor mengisi `Resources/Profiles/Season Weather Probabilities.asset`. Mendung (`Cloudy`) berbeda dari Cerah Berawan (`PartlyCloudy`): tidak menampilkan matahari atau hujan, dan cahaya lebih redup.

## Pilihan manual dan jadwal

Mengganti **Current Weather** pada Inspector WeatherSystem memilih mode **Manual**. Pilihan ini bertahan saat Play, pergantian hari, dan save/load. Pilih **Forecast Mode: Automatic** atau **AUTO WEATHER** pada menu debug untuk kembali ke peluang musim dan menghitung ulang forecast.

Dalam mode Automatic, override tanggal di Game Schedules lebih diutamakan daripada peluang musim. `WeatherSchedule.csv` memakai `year,month,season,day,weather,note`: bulan 1–4 dan tanggal lokal, bukan hanya nomor hari berjalan. Nama Indonesia seperti Cerah/Gerimis maupun enum Inggris tetap diterima. Auto mengikuti probabilitas. CSV cuaca yang disimpan otomatis divalidasi/diimpor saat Unity Refresh, terpisah dari cafe; format lama `absolute_day,weather` tetap dapat diimpor. Lihat `Data/Balance/Schedules/PANDUAN_CUACA.md`. Build memakai asset hasil import; sertakan asset beserta meta saat commit.

## Save dan tanggal

Clock/save menyimpan jumlah hari berjalan; nilainya tidak di-reset di batas bulan. Kalender, HUD, TV, panel hewan, menu jeda, pertumbuhan musim, dan label slot memakai konversi yang sama. Durasi konstruksi dan pertumbuhan berbasis hari tetap menggunakan hari berjalan.

Save lama mempertahankan hari berjalan. Forecast Automatic mengikuti jadwal/peluang yang terbaru dengan seed save yang sama, termasuk hari yang sedang dimuat. Aturan yang tidak berubah menghasilkan forecast yang sama. Forecast Manual tetap dipertahankan sampai Automatic dipilih. Setelah pembaruan, konversi tanggal menggunakan panjang bulan baru. Save hewan lama mempertahankan umur dan menyesuaikan indeks bulan tanpa menambah umur secara mendadak.

Asset jadwal dan CSV menyediakan 1200 tanggal untuk tahun 1–10. Menu **Nature Paradise > Data CSV > Schedules and Cafe > Upgrade Four Month Calendar** memigrasikan jadwal lama berdasarkan tahun/bulan/tanggal, mempertahankan catatan dan override. Import edit CSV yang belum diterapkan sebelum menjalankan upgrade; export akan menulis ulang kedua CSV kalender/cuaca. Kalender dan peluang musim tetap berulang setelah tahun 10.

Saat handoff, sertakan script beserta meta, Game Schedules, House Calendar, Season Weather Probabilities dan CSV yang berubah. Tidak perlu menyertakan Library atau hasil build.
