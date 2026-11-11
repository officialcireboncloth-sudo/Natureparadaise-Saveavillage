G01 R05: profil daun Raspberry/Blackberry diukur dari gambar pengguna dan diberi gerigi kecil terarah; kontur dipertahankan pada near mesh. Warna/normal daun Blueberry dipulihkan persis dari sumber sebelum R04. Ranting, bunga, buah dan geometri Blueberry tetap; satu atlas/material dengan 153 permukaan lain serta ikon tetap. Bukti: ArtSource/Crops/G01/Review/Leaf_R05/Completion.json. Catatan berikut merupakan riwayat.

G01 R04: tajuk berry lebih rapat melalui batang melengkung dan ranting pendek yang tersambung; daun Rubus berkelompok tiga, tekstur daun dibedakan per jenis, buah tersebar dalam kedalaman. Tetap satu atlas/material dan 154 alokasi; hanya tiga permukaan daun direvisi, 151 permukaan lain serta ikon, item dan balance tetap. Bukti: ArtSource/Crops/G01/Review/Bush_R04/Completion.json. Catatan berikut merupakan riwayat.

G01 R03: bentuk/proporsi daun, UV urat daun, sudut dan arah daun ketiga berry diperbaiki. Blueberry pada tanaman dikecilkan menjadi 84% dan tangkainya disambung ulang. Atlas/material yang sama dipakai; piksel atlas, ikon dan binding 31 crop tetap. Bukti terbaru: ArtSource/Crops/G01/Review/Leaf_R03/Completion.json. Catatan berikut merupakan riwayat sebelumnya.

Status G01 R02 (2026-11-11): Blueberry, Raspberry dan Blackberry lengkap. Seluruh 31 tanaman sudah terhubung ke profil, hasil normal/mutasi dan toko Benih, dengan 156 binding fase dan 93 sprite. Semua 380 mesh dan 190 prefab memakai satu atlas/material NP_Plants_Unified_01. Raspberry memakai daun bergerigi dalam ruang, 53 butiran per buah dan rongga panen 3D. Sebanyak 30 crop serta 146 alokasi/piksel atlas sebelumnya dipertahankan. Bukti: ArtSource/Crops/Integration/G01_Raspberry_Completion.json. Kompilasi/audit lulus; Unity Editor, Play Mode dan profiling HP belum dijalankan. Catatan di bawah adalah riwayat.

Status G01: Blueberry dan Blackberry sudah dibuat dari referensi dan dihubungkan ke profil, bibit paper bag, hasil normal/mutasi dan toko. Total 30/31 tanaman bereferensi, 151 binding fase, 90 sprite, 368 mesh dan 184 prefab; semua memakai satu atlas/material NP_Plants_Unified_01. Seluruh 28 crop sebelumnya dan 134 alokasi UV/piksel atlas dipertahankan. Raspberry masih menunggu referensinya karena lampiran Blueberry terkirim dua kali. Lihat ArtSource/Crops/Integration/G01_Completion.json dan ArtSource/Crops/G01/README.md. Kompilasi serta audit file/aturan crop terpisah dari Editor, Play Mode dan profiling HP.

Status G13: Lavender sudah terhubung ke profil, bibit, hasil normal/mutasi, toko dan lima fase field beserta model panen. Seluruh 28 tanaman memakai satu atlas/material bersama NP_Plants_Unified_01. Lihat ArtSource/Crops/Integration/G13_Completion.json dan ArtSource/Crops/G13/README.md. Validasi file/compiler dipisahkan dari Editor/Play Mode/profiling HP.

# Tanaman G02-G13 dan toko bibit

28 tanaman terhubung ke profil gameplay, item bibit, hasil panen/mutasi, dan prefab model terbaru. Katalog Crops Catalog pada toko Map memuat 28 bibit di tab Benih. Card toko, panel detail dan inventory memakai icon serta inventoryIllustration pada item yang sama.

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
| G10 | Bunga Matahari, Krisan, Dahlia, Marigold |
| G11 | Tulip |
| G12 | Anggrek / Orchid |
| G13 | Lavender |

Setiap profil memakai Seed (Bulb untuk Tulip), Sprout, Young, fase pembentukan sesuai jenis, dan Ready. Harvest dipakai sebagai objek hasil panen/drop. Kubis mempertahankan enam slot lama; dua slot terakhir berbagi Ready. Ukuran tiap tanaman dibatasi 85% lebar petak, dengan offset yang menapak. Bibit menunjuk kembali ke profil crop yang sama sehingga SeedTool menanam jenis yang dipilih. G07 memakai R02, G08 memakai perbaikan sambungan gandum, dan G09 memakai revisi R02 yang telah ditinjau.

Benih mengikuti tinggi overlay tanah cangkul dengan jarak 2 mm melalui FieldCropView yang sudah ada. Stage Seed mempertahankan LOD termurah agar benih kecil tetap terlihat. Tab lain, price override, scene, model dan atlas yang sudah disetujui dipertahankan.

## Ikon

Assets/Nature  Paradaise/Textures/UI/Icons/Seeds berisi 28 paket 256x256 RGBA; folder Crops berisi 28 hasil normal dan 28 ikon mutasi. Mipmap UI dimatikan, transparansi aktif, batas impor semua platform 256. G10, Tulip, Orchid dan Lavender memakai fungsi kemasan paper bag, label hijau dan font yang sama. Ilustrasi tambahan berasal dari ImageGen; pemisahan atlas, margin, komposisi paket dan label teks dikerjakan di GIMP. Ukuran serta alpha diperiksa pada PNG sebenarnya.

Master editable ada di ArtSource/UIIcons/Seeds/Masters; sumber ilustrasi dan daerah potong ada di Generated. Preview gabungan 28 paket adalah ArtSource/UIIcons/Seeds/SeedPackets_Preview.png. G06_G09_IconAudit.json, G10_IconAudit.json, G11_IconAudit.json, G12_IconAudit.json dan G13_IconAudit.json merekam pembukaan ulang master yang ditambahkan masing-masing batch. Preview khusus Tulip adalah G11_SeedPackets_Preview.png.

## Identitas dan balance

Seluruh baris balance dan asset 12 tanaman lama dipertahankan. Sembilan crop baru memakai nilai awal integrasi: semua musim, panen 5 hari, benih 5 G. Hasil baru mengikuti nilai awal 12 G; khusus gandum, item material Wheat yang sudah ada dipakai kembali dengan item.wheat, GUID, stack dan harga jual 4 G tetap. Ini menjaga referensi resep dan inventory lama. Hanya referensi artwork item Wheat yang ditambahkan. Bunga normal serta mutasinya ditandai nonmakanan. Balancing per spesies tetap dapat diedit melalui Data/Balance/Items.csv dan Crops.csv serta importer yang sudah ada.

## Validasi

Hasil terkini G13 ada pada `ArtSource/Crops/Integration/G13_Completion.json`: 28 crop dan bibit toko, 141 binding fase, 84 sprite, 344 mesh, 172 prefab, satu material runtime. Baseline G13 mencatat 27 crop sebelumnya; data dan artwork tersebut dipertahankan. Pemeriksaan file, compiler dan aturan crop terpisah dari import Editor, Play Mode dan profiling HP. Catatan validasi Tulip serta batch lama di bawah disimpan sebagai riwayat.

Manifest.json mencatat semua profil, seed, hasil normal/mutasi, GUID prefab tiap fase, skala dan offset. IntegrationAudit.json memeriksa 26 rantai profil/item/toko, ID unik, 131 fase, footprint, 78 sprite, CSV dan preservasi aset lama. G11_Baseline.json merekam keadaan 25 crop sebelum penambahan Tulip. Seluruh profil, ikon lama, model/atlas, kedalaman tanam Onion/Radish dan kode hint dipertahankan. Compilation.json mencatat kompilasi runtime/Editor melalui compiler Unity dan referensi cache; hasil ini terpisah dari import Editor/Play Mode. G11_Completion.json merangkum hasil terkini.

Jalankan Nature Paradise > Crops > Validate Applied Crops and Seed Store setelah membuka proyek dari Unity Hub. Fixture memakai Resources sebenarnya, memeriksa sprite import, dan mencoba transaksi 26 bibit dalam inventory sementara. UnityGameplayAudit.json hanya menunjukkan hasil fixture Editor jika dijalankan. Percobaan batch lama pada UnityValidationAttempt.json adalah riwayat G06-G09; validasi Editor dan Play Mode untuk penambahan Tulip belum dijalankan oleh agent. Stop/Play memuat ulang katalog dan cache item di sesi game.

Tulip mengikuti referensi pertumbuhan yang diberikan: umbi, tunas pada umbi, tiga daun muda, kuncup pink, bunga cawan dengan enam tepal dan dua daun, serta bunga panen bertangkai dengan satu daun. Tambahan ini memakai nilai awal bunga yang sudah ada (5 G per bibit, 5 hari, hasil 12 G, nonmakanan). Semua 25 baris balance crop sebelumnya tetap. Review mesh dan sumber editable berada di ArtSource/Crops/G11; laporan apply ada pada G11_Completion.json.

Revisi visual Tulip R02 memperbaiki siluet kelopak oval membulat, proporsi bunga/batang, lengkungan serta perbedaan posisi daun kiri/kanan, dan tekstur petal/daun. GUID prefab, skala crop, harga, pertumbuhan, ikon, toko dan hint dipertahankan. Hanya offset visual Young/Bud/Ready disesuaikan untuk memusatkan mesh baru. Mesh Bulb dan Sprout tidak dibangun ulang. R02_AssetChanges.json mencatat hash sebelum/sesudah untuk perubahan yang diminta; R02_FormAudit.json memeriksa volume, bentuk, putaran daun serta enam wilayah atlas dan mask yang tidak berubah.

G10 mempertahankan semua baris balance 21 crop sebelumnya. Empat tambahan mengikuti nilai awal integrasi: semua musim, pertumbuhan 5 hari, benih 5 G, hasil panen 12 G, nonmakanan. Perluasan memakai prepare_g10_crop_integration.py, inspect_g10_icon_atlas.py, extend_g10_seed_packets_gimp.py, apply_g10_crop_integration.py, edit_crop_csv.mjs G10_, record_g10_applied_catalog.py, audit_crop_gameplay.py dan finalize_g10_crop_integration.py. Edit CSV menggunakan artifact-tool dengan pemeriksaan nilai sel yang persis. Model G10 tidak dibangun ulang saat apply.

## Pembuatan ulang perluasan

1. Python: prepare_crop_extension.py.
2. Blender: ArtSource/UIIcons/Seeds/inspect_icon_atlas.py.
3. GIMP python-fu-eval: extend_seed_packets_gimp.py pada folder ikon. Font harus aktif; jangan gunakan --no-fonts. Memerlukan Generated/G06_G09_Illustrations.png yang disimpan.
4. Python: apply_crop_extension.py dan compile_crop_integration.py.
5. Blender: audit_crop_extension.py.

Script perluasan menambah sembilan bibit pada katalog yang ada. Script apply_crop_gameplay.py lama dilindungi agar tidak mengembalikan katalog yang sudah diperluas menjadi 12 jenis.
