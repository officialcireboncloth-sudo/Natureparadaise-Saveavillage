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
- Slot particle pada `WeatherSystem` sekarang aktif. Prefab yang diisi akan mengikuti
  `Effect Follow Target` dan otomatis berganti saat cuaca berubah.
- Tekan `F12` saat `Debug Clues` aktif untuk mengganti cuaca hari ini secara berurutan.

Prefab puing dan posisi hazard tidak dibuat otomatis supaya layout map tetap dapat
diatur manual tanpa object dummy yang muncul kembali.
