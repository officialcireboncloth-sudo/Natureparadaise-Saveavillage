Status terkini: seluruh 31 tanaman sudah terhubung ke profil, item hasil normal/mutasi, toko bibit dan 93 sprite. G01 lengkap: Blueberry, Raspberry, Blackberry. Total 156 binding fase, 380 mesh dan 190 prefab memakai satu atlas/material NP_Plants_Unified_01. Data serta artwork 30 tanaman sebelumnya dipertahankan. Lihat G01_Raspberry_Completion.json dan ../G01/README.md. Kompilasi dan audit file/aturan crop lulus; Editor, Play Mode dan profiling HP belum dijalankan. Bagian berikut merupakan riwayat integrasi sebelumnya.

# Tanaman G02-G09 dan toko bibit

21 tanaman terhubung ke profil gameplay, item bibit, hasil panen/mutasi, dan prefab model terbaru. Katalog Crops Catalog pada toko Map memuat 21 bibit di tab Benih. Card toko, panel detail dan inventory memakai icon serta inventoryIllustration pada item yang sama.

| Grup | Tanaman |
| --- | --- |
| G02 | Semangka, Melon, Zucchini |
| G03 | Timun, Ercis, Kiwi |
| G04 | Buncis, Cabai, Terong |
| G05 | Kubis, Brokoli, Kembang Kol |
| G06 | Lobak, Bawang Bombay |
| G07 | Bayam, Kangkung |
| G08 | Padi, Gandum |
| G09 | Mawar, Kembang Sepatu, Melati |

Setiap profil memakai Seed, Sprout, Young, fase pembentukan sesuai jenis, dan Ready. Harvest dipakai sebagai objek hasil panen/drop. Kubis mempertahankan enam slot lama; dua slot terakhir berbagi Ready. Ukuran tiap tanaman dibatasi 85% lebar petak, dengan offset yang menapak. Bibit menunjuk kembali ke profil crop yang sama sehingga SeedTool menanam jenis yang dipilih. G07 memakai R02, G08 memakai perbaikan sambungan gandum, dan G09 memakai revisi R02 yang telah ditinjau.

Benih mengikuti tinggi overlay tanah cangkul dengan jarak 2 mm melalui FieldCropView yang sudah ada. Stage Seed mempertahankan LOD termurah agar benih kecil tetap terlihat. Tab lain, price override, scene, model dan atlas yang sudah disetujui dipertahankan.

## Ikon

Assets/Nature  Paradaise/Textures/UI/Icons/Seeds berisi 21 paket 256x256 RGBA; folder Crops berisi 21 hasil normal dan 21 ikon mutasi. Mipmap UI dimatikan, transparansi aktif, batas impor semua platform 256. Dua belas ikon awal dipertahankan. Sembilan ilustrasi baru berasal dari ImageGen; pemisahan atlas pada gutter transparan, margin, komposisi paket dan label teks dikerjakan di GIMP. Ukuran serta alpha diperiksa pada PNG sebenarnya.

Master editable ada di ArtSource/UIIcons/Seeds/Masters; sumber ilustrasi dan daerah potong ada di Generated. Preview gabungan 21 paket adalah ArtSource/UIIcons/Seeds/SeedPackets_Preview.png. G06_G09_IconAudit.json mencatat pembukaan ulang 18 master baru.

## Identitas dan balance

Seluruh baris balance dan asset 12 tanaman lama dipertahankan. Sembilan crop baru memakai nilai awal integrasi: semua musim, panen 5 hari, benih 5 G. Hasil baru mengikuti nilai awal 12 G; khusus gandum, item material Wheat yang sudah ada dipakai kembali dengan item.wheat, GUID, stack dan harga jual 4 G tetap. Ini menjaga referensi resep dan inventory lama. Hanya referensi artwork item Wheat yang ditambahkan. Bunga normal serta mutasinya ditandai nonmakanan. Balancing per spesies tetap dapat diedit melalui Data/Balance/Items.csv dan Crops.csv serta importer yang sudah ada.

## Validasi

Manifest.json mencatat semua profil, seed, hasil normal/mutasi, GUID prefab tiap fase, skala dan offset. IntegrationAudit.json memeriksa 21 rantai profil/item/toko, ID unik, model tiap fase, footprint, 63 sprite, CSV dan preservasi aset lama. G06_G09_Baseline.json adalah baseline perluasan. Compilation.json mencatat kompilasi runtime/Editor melalui compiler Unity dan referensi cache; hasil ini terpisah dari import Editor/Play Mode.

Jalankan Nature Paradise > Crops > Validate Applied Crops and Seed Store setelah membuka proyek dari Unity Hub. Fixture memakai Resources sebenarnya, memeriksa sprite import, dan mencoba transaksi 21 bibit dalam inventory sementara. UnityGameplayAudit.json hanya menunjukkan hasil fixture Editor jika dijalankan; UnityValidationAttempt.json mencatat percobaan eksekusi sesi ini. Play Mode dicatat terpisah dan tidak disimpulkan dari audit file.

## Pembuatan ulang perluasan

1. Python: prepare_crop_extension.py.
2. Blender: ArtSource/UIIcons/Seeds/inspect_icon_atlas.py.
3. GIMP python-fu-eval: extend_seed_packets_gimp.py pada folder ikon. Font harus aktif; jangan gunakan --no-fonts. Memerlukan Generated/G06_G09_Illustrations.png yang disimpan.
4. Python: apply_crop_extension.py dan compile_crop_integration.py.
5. Blender: audit_crop_extension.py.

Script perluasan menambah sembilan bibit pada katalog yang ada. Script apply_crop_gameplay.py lama dilindungi agar tidak mengembalikan katalog yang sudah diperluas menjadi 12 jenis.
