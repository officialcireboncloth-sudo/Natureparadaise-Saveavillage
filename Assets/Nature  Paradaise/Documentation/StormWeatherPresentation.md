# Revisi tampilan cuaca badai

| Cuaca | Tampilan |
|---|---|
| HeavyRain | 160 emisi/detik pada Normal desktop; lebih padat daripada Rain (85), sedikit miring |
| WindRainStorm | 185 emisi/detik, kemiringan lebih besar, garis hembusan tipis, vegetasi bergoyang searah hujan |
| Cyclone | 220 emisi/detik, kemiringan paling tajam, hembusan serta goyangan terkuat |
| Thunderstorm | 195 emisi/detik, hujan angin, suara petir yang sudah ada dan pulsa cahaya lingkungan singkat |

Angka emisi adalah target sebelum pengali Ringan/mobile dan batas partikel. Tidak menambah texture, kamera, physics per tetes, noise berputar, atau kamera goyang. Gerimis dan Rain mempertahankan presetnya. Tiga batch dipakai ulang: hujan, percikan, dan hembusan. Batas mobile 160/64/24, desktop 256/96/48. Sampling tetap maksimum empat raycast per Tick dan 32/detik.

Wind menggerakkan shader Meadow, properti angin material daun yang didukung, Terrain grass melalui TerrainData runtime bersama, dan pivot visual WorldTree. Akar/collider pohon serta state pertumbuhan tidak dimiringkan. Satu driver dimiliki WeatherSystem; tidak menambah Update pada tiap pohon. Material daun menggunakan nilai sementara di memori dan dipulihkan saat selesai, masuk interior, atau driver dinonaktifkan. TerrainData aset tidak diubah.

Thunderstorm memakai satu pulsa cahaya hingga 0,7 yang meluruh dalam sekitar 0,28 detik, tanpa rangkaian flash cepat. Interval petir tetap 5–14 detik sesuai konfigurasi WeatherSystem. Ringan membatasi pulsa ke 0,35; Mati mematikannya. Cahaya matahari/ambient dihitung ulang oleh DayNightCycle; keluar badai/interior membersihkan pulsa. Mekanisme risiko petir dan sound lama tetap terpisah dari efek kilat ini.

Jika slot thunderClips scene kosong, WeatherSystem memakai Resources/Audio/Weather/Thunder: efek gemuruh sintetis singkat dua detik, mono 22,05 kHz, sumber WAV sekitar 88 KB dan impor Vorbis. Klip pilihan di Inspector tetap diutamakan. Tidak ada audio yang dibuat per frame.

Pengaturan di Resources/Profiles/Soft Rain Visuals. Pengujian cuaca dapat dipilih lewat F12 → Waktu & Cuaca. Revisi ini tidak mengubah CSV, peluang musim, kalender, atau isi save. Bukti pengujian lokal: ArtSource/StormWeatherRevision.
