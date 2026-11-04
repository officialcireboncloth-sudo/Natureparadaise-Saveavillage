# Persiapan penjaga dan gerobak pasar

Tanggal: 4 November 2026. Tahap ini merapikan karakter dan mendata display; gerobak serta penempatan penjaga di scene belum dibuat.

## Karakter penjaga

- Folder runtime: `mesh/Character/MarketKeeper/`.
- Model: `MarketKeeper.fbx`; mesh `MarketKeeper_Body`; rig `MarketKeeper_Rig`; skeleton `MarketKeeper_Skeleton`. Nama tulang Mixamo dipertahankan agar retargeting tidak berubah.
- Tekstur: `Textures/MarketKeeper_BaseColor.jpg`, benar-benar diubah dari 4096×4096 menjadi 1024×1024 melalui GIMP NOHALO, JPEG kualitas 95.
- Ukuran tekstur: 2.586.092 → 356.010 byte (turun sekitar 86%). Jumlah piksel menjadi 1/16 dari sumber.
- Material Unity: `Materials/MarketKeeper.mat`, URP Lit, satu tekstur warna; tidak ada normal/roughness map yang diunggah.
- Android/iOS: batas 1024, ASTC 6×6, mipmap aktif, salinan tekstur CPU dimatikan.
- Geometri sumber tetap 2.815 vertex dan 5.465 triangle, 65 tulang, satu material. Avatar Humanoid diperiksa di Unity.
- Sumber edit: `Source~/Characters/MarketKeeper/MarketKeeper.blend`. Belum ada Action animasi dalam file unggahan; idle/jalan dan penempatan akan disambungkan saat implementasi penjaga.
- Cadangan unggahan utuh 4K berada di `ArtSource/MarketKeeper/Originals/` (lokal, diabaikan Git). Sumber kerja Blender memakai tekstur 1K agar tidak menggandakan unggahan 4K.

## Pilihan display Toon Farm Pack

Daftar diambil dari prefab yang benar-benar tersedia, bukan daftar tanaman gameplay. Satu jenis dapat memiliki model satuan, tumpukan, peti, kotak, dan potongan. Jumlah varian mencakup seluruh bentuk tersebut.

### Buah

| Jenis | Varian prefab | Contoh model |
| --- | ---: | --- |
| Apel | 30 | `TFP_Apple_01A.prefab` |
| Pisang | 20 | `TFP_Banana_01A.prefab` |
| Beri | 2 | `TFP_Berries_Crate_Small_01A.prefab` |
| Melon jingga | 3 | `TFP_Cantaloupe_01A.prefab` |
| Kelapa | 7 | `TFP_Coconut_01A.prefab` |
| Grapefruit | 6 | `TFP_Grapefruit_01A.prefab` |
| Jeruk | 7 | `TFP_Orange_01A.prefab` |
| Pepaya | 2 | `TFP_Papaya_01A.prefab` |
| Pir | 14 | `TFP_Pear_01A.prefab` |
| Nanas | 1 | `TFP_Pineapple_01A.prefab` |
| Delima | 14 | `TFP_Pomegranate_01A.prefab` |
| Semangka | 8 | `TFP_Watermelon_01A.prefab` |

### Sayuran dan bahan kebun

| Jenis | Varian prefab | Contoh model |
| --- | ---: | --- |
| Asparagus | 3 | `TFP_Asparagus_01A.prefab` |
| Paprika | 23 | `TFP_Bell_Pepper_01A.prefab` |
| Kubis | 1 | `TFP_Cabbage_01A.prefab` |
| Wortel | 9 | `TFP_Carrot_01A.prefab` |
| Kembang kol | 2 | `TFP_Cauliflower_01A.prefab` |
| Tomat ceri | 22 | `TFP_Cherry_Tomato_01A.prefab` |
| Mentimun | 8 | `TFP_Cucumber_01A.prefab` |
| Jagung | 5 | `TFP_Ear_Corn_01A.prefab` |
| Terong | 10 | `TFP_Eggplant_01A.prefab` |
| Bawang putih | 8 | `TFP_Garlic_01A.prefab` |
| Cabai | 2 | `TFP_Hot_Pepper_01A.prefab` |
| Leek | 7 | `TFP_Leek_01A.prefab` |
| Bawang bombai | 17 | `TFP_Onion_01A.prefab` |
| Daun bawang | 6 | `TFP_Onion_Spring_01A.prefab` |
| Pepper (varian pack) | 20 | `TFP_Pepper_01A.prefab` |
| Kentang | 7 | `TFP_Potato_01A.prefab` |
| Labu | 5 | `TFP_Pumpkin_01A.prefab` |
| Lobak | 1 | `TFP_Radish_01A.prefab` |
| Salad (nama aset pack) | 1 | `TFP_Salad_01A.prefab` |
| Bayam | 1 | `TFP_Spinach_Pile_01A.prefab` |
| Ubi | 7 | `TFP_Sweet_Potato_01A.prefab` |
| Tomat | 26 | `TFP_Tomato_01A.prefab` |
| Zukini | 20 | `TFP_Zucchini_01A.prefab` |

## Pemetaan ke referensi gerobak

| Bagian referensi | Aset tersedia |
| --- | --- |
| Lemari kaca atas | Pisang, jeruk, pir |
| Lemari kaca bawah | Tomat, wortel, terong |
| Keranjang sayur | Kubis, kembang kol, bayam/Salad sebagai alternatif sayur daun |
| Keranjang depan | Tomat dan pir |
| Peti bawah | Kentang |

- Buah naga, mangga, jambu, dan pakcoy tidak ditemukan dengan nama tersebut dalam Toon Farm Pack. Buah naga perlu dibuat sendiri bila harus sama persis; jangan mengganti diam-diam lalu mengklaim 1:1.
- Beri hanya ditemukan dalam dua prefab peti kecil, belum ada prefab buah satuan terpisah.
- Bentuk gerobak kayu, lemari kaca, roda, pegangan, rak bawah, dan keranjang mengikuti gambar referensi pada tahap model berikutnya.
- Gunakan atlas modular cozy yang sudah ada untuk kayu/besi/anyaman. Mesh display mengambil aset Toonpack; kebutuhan berbagi atlas harus diperiksa dari UV dan materialnya saat dirakit.
- Kaca memerlukan material transparan terpisah yang sederhana. Batasi lapisan transparan untuk mobile; atlas sendiri tidak menghilangkan biaya transparansi.

## Verifikasi

- FBX diekspor dan diimpor ulang di Blender; jumlah geometri, tulang, vertex group, lapisan UV, dan sambungan tekstur diperiksa.
- Model, material, avatar, ukuran tekstur, skin binding, dan pengaturan mobile diperiksa lewat editor Unity 6000.0.81f1 di salinan uji.
- Hasil native Unity: `MarketKeeperImportAudit.txt`. Scene dan save game utama tidak diubah.
