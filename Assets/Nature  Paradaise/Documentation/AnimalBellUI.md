# UI Bell Kandang

E pada station bell membuka panel kandang yang bersangkutan. F memanggil hewan masuk, G mengeluarkan hewan, dan Esc/E atau tombol Tutup menutup panel. Tombol normal abu-abu, hijau saat hover, dan redup saat tidak tersedia. Ikon bell, cuaca, info, serta panel artwork berupa slot Sprite opsional, diisi lewat **Nature Paradise/UI/Animal Bell/Select Image Slots** (`Resources/UI/AnimalBellTheme.asset`). Screenshot dipakai sebagai referensi layout.

Jumlah hewan diperbarui langsung dari resident kandang: di luar, di dalam, dan sedang kembali menuju kandang merupakan kelompok terpisah. Hewan yang belum lahir tidak ditampilkan dalam hitungan. Perintah hanya menargetkan kandang station ini; cooldown, batas jam 06:00–18:00, pemeriksaan kesehatan, save state, suara dan movement tetap memakai PlayerAnimalBell/AnimalRoutine yang ada.

UI menampilkan peringatan cuaca dari WeatherSystem. Hujan/salju memberi saran perlindungan tetapi tidak menambahkan larangan keluar; aturan lama memang tetap mengizinkan risiko cuaca. Tombol keluar dinonaktifkan jika resident yang sudah lahir tidak memenuhi aturan release, dengan alasan sakit/jam pada panel.

Panel mengunci input/gerak player dan prompt interaksi lain. Clock dan time scale tidak dipause, supaya hewan tetap bisa berjalan masuk kandang. UI menutup serta melepaskan lock saat ditutup, station dinonaktifkan, kandang tidak tersedia, scene berganti, atau player dipindahkan keluar radius bell. Pelepasan hewan tetap memakai perilaku ReleaseAt lama, tanpa mengubahnya menjadi animasi berjalan keluar baru.
