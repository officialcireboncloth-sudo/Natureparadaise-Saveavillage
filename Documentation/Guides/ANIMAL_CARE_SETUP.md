# Animal Care — setup dan tes

## Setup

- PropertySite Barn/Coop yang selesai dibangun otomatis mendapatkan AnimalHome. Jenis kandang dan kapasitas mengikuti BuildingDefinitionSO / level bangunan. Mamalia masuk Barn, unggas masuk Coop. Bangunan custom perlu mengisi Animal Housing pada definition.
- Untuk kandang manual tanpa PropertySite, pasang AnimalHome, isi Home Id unik dan stabil, Kind, Capacity, serta Door di atas lantai depan pintu. Slot Door juga dapat dipakai untuk memperbaiki pintu otomatis pada bangunan dengan orientasi/model custom.
- AnimalRoutine otomatis dipasang pada AnimalGrowthSystem. Dummy/model pertumbuhan yang sudah diatur tetap dipakai. Hewan lama mencari kandang kosong; pembelian baru ditolak jika tidak ada kapasitas kandang yang sesuai.
- Kandang memakai boarding virtual: model/collider hewan disembunyikan ketika masuk, tetapi growth, produksi, care, dan save tetap aktif. Tidak membuat scene interior Barn/Coop baru. Kelola hewan di dalam melalui menu kandang.
- Grass/Fodder, Animal Treat, dan Animal Medicine tersedia pada tab Hewan toko. Menyabit WorldGatherable Grass menghasilkan Fodder. Mapping produk pada Resources/AnimalCareCatalog: Chicken → Egg, Duck → Duck Egg, Goat → Goat Milk, Sheep → Wool, Cow → Milk. Slot item custom yang sudah terisi tetap diprioritaskan.

## Kontrol dan loop

1. Dekati pintu/trough kandang atau hadapkan player ke hewan, tekan **I** untuk menu Animal Care. Menu menghentikan waktu dan gerakan player; **Esc** menutup.
2. Isi 1 atau 10 Fodder ke trough. Hewan yang sudah lahir dan berada di kandang mengonsumsi satu porsi per hari, tidak berulang karena menu dibuka. Sisa pakan bisa diambil kembali.
3. Pilih hewan dari daftar: ganti nama, Feed, Pet, Treat, Medicine, ambil produk, atau pindahkan penugasan ke kandang lain yang sesuai dan memiliki ruang.
4. Keluarkan hewan pada cuaca baik, pukul 06:00–17:59. Telur dan hewan sakit tidak dapat dikeluarkan. Hewan berjalan mencari Grass yang tersedia dalam radius pencarian lalu mengonsumsinya; satu reward grazing per hari.
5. Panggil pulang lewat menu, atau tunggu pukul 18:00/cuaca buruk. Hewan mencoba kembali melalui jalur yang valid. Jika tidur melewati waktu pulang, pergantian hari menyelesaikan boarding secara simulasi.
6. Produk mempertahankan grade Normal/Bronze/Silver/Gold/Premium di inventory. Timer produksi lama tetap digunakan; sistem ini tidak mengubahnya menjadi tepat satu produk per hari.

Kontrol langsung tetap tersedia: F feed, P pet, G ambil produk, Y treat, O medicine. Menu I tidak mengubah tombol debug.

## Save dan bangunan

Save menyimpan ID/nama/status care/growth/grade produk, Home Id, status boarding/pulang, serta stok trough. Load memulihkan bangunan sebelum penugasan hewan. Relokasi PropertySite memindahkan penugasan dan stok pakan; demolish ditolak selama masih ada penghuni atau sisa pakan.

## Health: penyakit, pemulihan, kekebalan

Atur pada **AnimalGrowthSystem → Condition → Health Rules** di prefab atau hewan scene. Nilai awal:

- `Hungry Days Before Risk = 3`: tidak diberi pakan 3 hari berturut-turut mulai berisiko sakit; `Hunger Sickness Chance = 0.35` (35% setiap daily reset yang memenuhi syarat). Makan memutus streak.
- `Rainy Days Before Risk = 2`: terpapar Rain di luar selama 2 hari berturut-turut mulai berisiko; `Rain Sickness Chance = 0.15` (15%). Satu hari tanpa paparan memutus streak. Paparan singkat pun mencatat hari itu, bukan durasi jam; pulang ke kandang tidak menghapus paparan yang sudah terjadi.
- Risiko cuaca langsung modular: Drizzle 5%, Heavy Rain 30%, Wind Rainstorm/Thunderstorm 50%, Heatwave/Cyclone/Blizzard 80%. Rain biasa memakai streak di atas. Atur melalui `Drizzle/Heavy Rain/Storm/Extreme Weather Sickness Chance`; peluang 0 menonaktifkan tier. Risiko bersama kelaparan/hujan/malam digabung sebagai `1 - hasil perkalian peluang aman`, bukan dijumlahkan.
- Hewan yang masih berada di luar mulai `Night Risk Starts At Hour = 20` mencatat risiko malam 12% pada daily reset. Hewan yang berhasil boarding tidak terkena risiko ini. Auto-recall tetap dimulai pukul 18:00, sehingga malam terutama menghukum jalur pulang yang terhalang atau penempatan pintu yang salah.
- UI menamai tahap sebagai **Healthy → Unwell → Sick → Recovering → Healthy**. State internalnya Mild untuk Unwell dan Severe untuk Sick agar save lama tetap kompatibel. Setelah `Untreated Days To Severe = 3` daily reset berikutnya tanpa obat, Unwell menjadi Sick. Tidak ada kematian atau penularan.
- Obat satu item memulai **Recovering**: `Recovery Days = 2` untuk Mild, `Severe Recovery Days = 3` untuk Severe. Obat tambahan ditolak saat pemulihan dan tidak menghabiskan item.
- Hari pemberian obat belum dihitung. Setiap hari berikutnya harus diberi pakan, berada di kandang saat evaluasi harian, serta tidak terpapar hujan/badai agar sisa pemulihan berkurang. Jika syarat gagal, progress berhenti, bukan diulang dari nol.
- Setelah sembuh, `Immunity Days = 3` melindungi selama tiga evaluasi harian berikutnya dari semua penyebab sakit di atas. Streak kelaparan/paparan tetap bisa berjalan saat kebal; care tetap diperlukan. Tidak ada kekebalan permanen.
- Mild, Severe, dan Recovering menghentikan growth serta produksi baru; hewan tidak bisa turnout. Produk yang sudah siap tetap dapat diambil. Happiness berkurang lagi per hari: Mild -3, Severe -6, Recovering -1. Hari pemulihan terakhir tidak diberi growth retroaktif; produksi kembali diizinkan setelah sehat.

Visual kondisi bersifat modular dan tidak memerlukan asset sekarang. Isi **Animal Growth Profile → Condition Visual Slots** untuk satu spesies, atau **AnimalGrowthSystem → Condition Visual Overrides** untuk satu hewan. Tambahkan slot Mild, Severe, dan Recovering; masing-masing menerima Model Prefab, Scale, Offset, Rotation, serta Animator Controller. Model kosong + Animator Controller hanya mengganti animasi model growth. Semua slot kosong mempertahankan model/animasi growth normal dan tidak membuat dummy kondisi. Collider pada prefab kondisi dibuang agar area interaksi tetap memakai collider root hewan. Visual Healthy memakai Stage Visual Slots yang sudah ada.

Menu **I** menampilkan tahap, sisa care days saat Recovering, dan sisa hari kebal. Save mencakup seluruh progress health, hari obat/evaluasi terakhir, streak, dan paparan hari berjalan. Save lama Healthy/Sick dimigrasikan menjadi Healthy/Mild. Health Rules merupakan konfigurasi prefab/scene, bukan balancing yang disalin ke save.

Tes manual deterministik: set peluang hunger/rain menjadi 1 di prefab/hewan tes, kosongkan trough dan jangan feed/graze selama tiga hari; cek Mild, tunggu tiga hari untuk Severe, beri obat dua kali (hanya satu item terpakai), save/load saat Recovering, rawat sesuai syarat selama tiga hari penuh, lalu pastikan Healthy + immunity tiga hari. Uji hujan lewat dua hari paparan berurutan, dan bandingkan hewan selalu di kandang. Kembalikan peluang balancing setelah tes.

`Tests/AnimalHealthTests.cs` menguji aturan murni, streak, risiko, eskalasi, pemulihan, anti-spam reset/obat, copy state, dan batas kekebalan; bukan pengujian Play Mode atau round-trip JSON di Unity.

## Checklist Play Mode

- Bangun Barn dan Coop; coba membeli mamalia ke Coop saja, kemudian beli sesuai kandang sampai kapasitas penuh.
- Isi pakan, pet dua kali, rename, save/load: nama, stok, penghuni, fed/pet, dan grade tidak berubah atau mendapat reward ganda.
- Sabit Grass dan ambil Fodder; turnout pada hari cerah dengan Grass terjangkau. Pastikan rumput habis saat dimakan, hewan kenyang, dan tidak menghasilkan pickup dari grazing.
- Uji recall/pukul 18:00, hujan, hambatan jalan, serta save/load ketika hewan sedang berjalan atau di kandang.
- Uji treat/medicine, Baby versus Adult, tas penuh saat mengambil produk, relokasi, dan larangan demolish kandang berisi.

Navigasi memakai NavMesh jika tersedia, atau pencarian grid dengan pengecekan ground/collider. Maksimal satu pencarian jalur per frame. Jalur terhalang ditampilkan sebagai status dan dicoba lagi; layout/pintu/collider map harus diuji di Unity. Pengujian aturan C# tidak menggantikan Play Mode, uji navigasi, atau round-trip save game nyata.
