# Weather Gameplay System

`WeatherSystem` menentukan cuaca hari ini dan besok secara deterministik. Forecast,
cuaca sebelumnya, dan hari gotong royong disimpan oleh `SaveManager`.

Saat cuaca berubah, `WeatherImpactFlow` mengirim snapshot dengan urutan channel:

`Lighting → Sky → Audio → NPC → Farming → Hunting → Fishing → Gameplay Ready`

Sistem yang belum memiliki gameplay penuh dapat subscribe ke channel masing-masing.
`NpcOutdoorActivitiesAllowed`, `CurrentHuntingMultiplier`, dan
`CurrentFishingMultiplier` sudah tersedia sebagai API tanpa membuat ketergantungan
langsung ke implementasi NPC, hunting, atau fishing nanti.

## Efek aktif

| Cuaca | Crop | Hewan di luar | Player / dunia |
| --- | --- | --- | --- |
| Cerah Mendung | Normal | Aman | Normal |
| Panas Terik | Growth 0.8x | Risiko sakit 80% | Normal |
| Gerimis | Tersiram, growth normal | Risiko sakit 25%; Heart/XP -10 | Normal |
| Hujan Sedang | Tersiram, growth normal | Risiko sakit 25%; Heart/XP -15 | Normal |
| Hujan Lebat | Tersiram, growth normal | Risiko sakit 25%; Heart/XP -20 | Normal |
| Hujan Angin Badai | Tersiram, growth normal, peluang hilang dasar 1% | Risiko sakit 60%; Heart/XP -50 | Biaya stamina outdoor 3x; pingsan di rumah jam 12 jika stamina dipaksa |
| Angin Topan | Tersiram, growth normal, peluang hilang dasar 3% | Risiko sakit 90%; Heart/XP -100 | Pintu rumah tidak dapat dipakai keluar; besok hari gotong royong |
| Badai Petir | Tersiram, growth normal, peluang hilang dasar 1% | Risiko sakit 60% | TV mati, stamina outdoor 2.5x, risiko petir dan bangun di klinik jam 12 |
| Hujan Salju | Tidak mengubah gameplay crop | Aman | Normal |
| Badai Salju | Growth normal, peluang hilang dasar 3% | Risiko sakit 90%; Heart/XP -30 | Biaya stamina outdoor 4x; longsoran memakai hazard zone |

Penalti hewan diproses satu kali pada `OnBeforeDayChange`, sehingga tidak berlipat
karena frame rate. Tanaman hujan mendapat `CropWaterSource.Rain` pada hari aktif.

Peluang crop hilang dihitung dengan:

`Risiko Cuaca × Wind Vulnerability Crop × Kerentanan Growth Stage`

Seed memakai pengali stage 0.35 dan meningkat bertahap hingga 1.25 saat mature.
Nilai `wind_vulnerability` dapat diedit melalui `Crops.csv`; contoh crop rendah bisa
memakai 0.6, crop biasa 1.0, dan crop tinggi seperti Corn 1.5. Tanah tetap terolah
ketika crop hilang. Field di Greenhouse harus mengaktifkan `Protected From Weather`
agar hujan tidak menyiramnya dan Storm/Extreme Weather tidak mencabut crop.

## Authoring level

- Tambahkan `WeatherDebrisSpawner` pada object area desa. Isi `Debris Prefabs` dan
  `Spawn Points`. Puing muncul hanya pada satu hari setelah topan lalu dibersihkan.
- Tambahkan `WeatherHazardZone` pada trigger area gunung dan pilih
  `Blizzard Avalanche`. Player yang masuk saat badai salju dibawa ke klinik.
- `Thunder Lightning` tersedia untuk lokasi petir khusus. Badai petir juga memiliki
  pemeriksaan petir outdoor global dari `WeatherSystem`.
- Tambahkan `WeatherAudioController` pada object audio environment, lalu isi loop
  gerimis, hujan, hujan lebat, salju, dan angin. Volume mengikuti intensitas snapshot.
- Semua cuaca hujan menggunakan preset bersama `Resources/Profiles/Soft Rain Visuals.asset`.
  Tetes pendek dan percikan kecil berjalan pada dua ParticleSystem dengan material
  `SoftRain_Drop` / `SoftRain_Splash`. Preset dapat diganti lewat `Rain Visual Profile`
  pada WeatherSystem. Slot particle lama tetap digunakan untuk salju / fallback.
- Tekan `F12` saat `Debug Clues` aktif untuk mengganti cuaca hari ini secara berurutan.

Prefab puing dan posisi hazard tidak dibuat otomatis supaya layout map tetap dapat
diatur manual tanpa object dummy yang muncul kembali.

## Hujan lembut dan kenyamanan visual

Default baru adalah **Ringan**, juga untuk pengaturan tersimpan sebelum opsi ini ada.
Player dapat memilih **Mati / Ringan / Normal** melalui Menu > Pengaturan > Grafis >
Efek Hujan, lalu Terapkan. Opsi disimpan bersama pengaturan UI, bukan save cuaca.
Flag scene lama `enableRainVisuals = false` hanya berlaku untuk renderer fallback lama;
ia tidak mematikan sistem Soft Rain yang baru. Scene tidak perlu disimpan ulang.

Gerimis memakai tetesan lebih transparan dan sedikit. Hujan sedang/lebat meningkatkan
jumlah tetesan secara bertahap; badai memakai arah miring yang stabil tanpa noise
berputar, kamera goyang atau flash lighting. Lebar tetesan default 0.045m, panjang
sekitar 0.225m, tinggi jatuh maksimum 3m, dengan warna air abu kebiruan yang lembut.
Tetes dekat kamera dibuang/fade dan opacity dikurangi di atas karakter. Percikan
berupa cincin pendek 0.18–0.30 detik, dengan posisi hasil probe permukaan. Di air,
bentuk cincin yang sama terbaca sebagai riak jika permukaan memiliki collider.

Sampling memakai maksimal 32 probe permukaan per detik, empat per frame, dan hasil
probe dibagi dalam kelompok tetesan kecil. Tidak ada collision/noise per partikel,
texture baru, material per tetes, atau game object per tetes. Area distribusi mengikuti
viewport kamera; jumlah tidak meningkat mengikuti luas seluruh map. Batas mobile
160 tetes + 64 percikan; desktop 256 + 96. Ringan memakai 55% kepadatan Normal,
dan mobile memakai pengali 80% lagi. Batas adalah kapasitas, bukan jumlah wajib tampil.

Intensitas emisi memudar selama sekitar 1.2 detik ketika cuaca berubah. Masuk interior,
teleport kamera dan opsi Mati membersihkan partikel serta impact tertunda agar tidak
terbawa masuk ruangan. Material tidak membuat shadow dan queue 3020 menjaga hujan
terlihat di atas overlay tanah. Mesh tanah, tanaman, perhitungan penyiraman oleh hujan,
forecast/save cuaca, audio ambience dan risiko cuaca tetap memakai sistem yang ada.
Efek ini bukan simulasi akumulasi genangan atau collision tetes pada setiap daun.

## Pencahayaan per musim

`DayNightCycle` mengambil preset dari `Resources/Profiles/Seasons`, melalui
`SeasonVisualController.CurrentProfile`. Preset berlaku otomatis saat musim berubah
atau `RefreshFromCalendar` dipanggil setelah load. Pencahayaan matahari, ambient fill,
bayangan dan exposure langit diinterpolasi secara halus; pergantian musim tidak
membuat material atlas baru atau menambah lampu / volume / texture per musim.

| Musim | Suasana cahaya | Terbit–terbenam | Pengali matahari | Pengali kekuatan bayangan |
| --- | --- | --- | --- | --- |
| Semi | Lembut, segar, fill netral sedikit sejuk | 05:45–18:15 | 1.00 | 0.90 |
| Panas | Lebih terang, hangat ringan, siang lebih panjang | 05:15–19:15 | 1.08 | 1.00 |
| Gugur | Keemasan, fill hangat, matahari lebih rendah | 06:00–17:45 | 0.92 | 0.88 |
| Dingin | Sejuk, fill biru lembut, bayangan lebih ringan | 06:30–17:15 | 0.87 | 0.80 |

`lightingTint` hanya memberi warna matahari. `ambientTint` mengatur cahaya isi
terpisah agar area teduh tidak ikut terlalu kuning. `sunElevationMultiplier`
merendahkan sudut matahari pada gugur/dingin; `shadowStrengthMultiplier` mengurangi
kontras bayangan tanpa menambah kualitas shadow map atau biaya render baru.

Malam memakai `nightTint`, `nightSunMultiplier`, `nightAmbientMultiplier` dan
`nightSkyExposureMultiplier`, sehingga warna hangat siang tidak menumpuk di cahaya
bulan. Batas ambient malam juga tetap berlaku pada palet waktu stylized, termasuk
saat badai. Modifier cuaca tetap dikombinasikan dengan preset musim; gerimis/hujan
dan badai tidak memakai pencahayaan cerah musim panas secara paksa.

Interior tetap menggunakan fill interior yang sudah ada; preset outdoor tidak
mengubah pencahayaan rumah. Profil musim dan Garden Lighting dapat diedit tanpa
menyimpan ulang Map. Untuk inspeksi Editor/Development Build, gunakan menu musim
`PageUp`, pilih musim dan cuaca, lalu `PageDown` untuk kembali mengikuti kalender.

## Stabilitas lighting pagi/sore

`DayNightCycle` menginterpolasi sudut matahari dan bulan secara eksplisit.
Jangan menggunakan `Quaternion.Slerp` langsung antara rotasi yang berbeda tepat
180 derajat: jalur terpendeknya ambigu dan pembulatan bisa membalik arah cahaya
antar-frame, menyebabkan dunia berkedip terang/gelap saat sunrise atau sunset.
Jam, tint, ambient, dan modifier cuaca tetap bergerak mengikuti waktu game.
