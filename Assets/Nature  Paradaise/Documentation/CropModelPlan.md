# Rencana model dan atlas tanaman field

Tanggal audit: 8 November 2026. Sumber: daftar tanaman awal, 20 gambar Part 1, 11 gambar Part 2, serta aset project yang diperiksa pada tanggal ini.

Status terkini: G02, G03, G04 dan G05 sudah terhubung ke 12 profil tanaman gameplay, item bibit/hasil panen dan katalog toko bibit. Ikon kemasan kertas 256px dibuat di GIMP. Lihat `CropGameplayIntegration.md` untuk binding, nilai awal CSV dan status validasi. Bagian di bawah menyimpan rencana serta catatan pengerjaan art sebelumnya.

## 1. Jumlah dan cakupan

| Cakupan | Jumlah | Keterangan |
| --- | ---: | --- |
| Referensi Part 1 | 20 | Tanaman pangan |
| Referensi Part 2 | 11 | Water Spinach dan 10 bunga |
| Total tanaman bergambar | **31** | Masing-masing masuk tepat satu grup utama |
| Grup model | **13** | Berdasarkan bentuk dan komponen produksi |
| Daftar awal | 34 baris | Onion muncul dua kali |
| Nama unik daftar awal | **33** | Onion dihitung satu jenis sampai ada bukti varian berbeda |
| Nama tanpa gambar yang terlihat | **2** | Daisy dan Moonflower |

| Kategori jenis untuk game | Jumlah |
| --- | ---: |
| Buah | 6 |
| Sayuran dan polong | 13 |
| Serealia | 2 |
| Bunga | 10 |
| **Total bergambar** | **31** |

Ada 21 tanaman pangan dan 10 bunga. Kiwi tetap tanaman field sesuai daftar pengguna. Corn/Jagung dan Soybean/Kedelai pada resep pakan berada di luar daftar gambar ini.

Normalisasi nama: Rasberry menjadi Raspberry, Watter melon menjadi Watermelon, Chilli Paper menjadi Chili Pepper, spanish menjadi Spinach, WEAT menjadi Wheat. Red Watermelon dipakai sebagai referensi Watermelon. Onion (1) belum dianggap jenis tambahan. Penulisan Zucchini dipakai konsisten.

## 2. Tiga belas grup model

Grup adalah kelompok pengerjaan Blender. Anggotanya berbagi komponen atau cara penyusunan; bentuk akhir tetap mengikuti masing-masing referensi. Komponen juga dapat digunakan lintas grup.

| ID | Grup | Jumlah | Anggota | Komponen bersama | Perbedaan yang dipertahankan |
| --- | --- | ---: | --- | --- | --- |
| G01 | Semak berry | 3 | Blueberry, Raspberry, Blackberry | Percabangan, tangkai, titik pemasangan buah | Daun oval/bergerigi, bunga lonceng/terbuka, buah bulat/majemuk |
| G02 | Buah dekat tanah | 3 | Watermelon, Melon, Zucchini | Daun berlekuk, tangkai, dasar bunga kuning | Watermelon/Melon menjalar; Zucchini berupa roset. Daun, ukuran bunga dan buah berbeda |
| G03 | Rambatan berpenyangga | 3 | Kiwi, Pea, Cucumber | Tiang/palang, sulur, tangkai | Daun, bunga, susunan rambatan, buah kiwi/polong/timun |
| G04 | Semak hasil menggantung | 3 | Eggplant, Chili Pepper, Green Bean | Batang bercabang, tangkai, pemasangan hasil | Daun, bunga, buah dan warna batang. Green Bean tanpa penyangga pada gambar |
| G05 | Roset dan kepala sayuran | 3 | Cabbage, Broccoli, Cauliflower | Dasar roset, tangkai tebal, sebagian daun | Kubis berlapis; kepala Broccoli bercabang hijau; Cauliflower berkepala krem |
| G06 | Akar dan umbi | 2 | Radish, Onion | Dasar umbi, serabut akar | Radish merah-putih berdaun lebar; Onion berkulit keemasan berdaun tabung |
| G07 | Sayuran daun | 2 | Spinach, Water Spinach | Tangkai hijau, susunan rumpun, panen ikat | Spinach rendah berdaun oval; Water Spinach tinggi, beruas dan berongga |
| G08 | Rumpun serealia | 2 | Rice, Wheat | Daun pita, batang, rumpun, palet hijau/emas | Malai padi menggantung; bulir gandum tegak |
| G09 | Semak bunga | 3 | Rose, Hibiscus, Jasmine | Percabangan, tangkai, pemasangan kuncup | Rose berduri dan berkelopak spiral; Hibiscus berbenang sari panjang; Jasmine putih kecil |
| G10 | Kelopak radial | 4 | Sunflower, Chrysanthemum, Dahlia, Marigold | Modul kelopak, susunan melingkar, tangkai/kuncup dasar | Sunflower tinggi dengan disk cokelat; Chrysanthemum membulat; Dahlia runcing; Marigold berombak |
| G11 | Tulip | 1 | Tulip | Dasar umbi/akar dari G06; tangkai umum | Daun pita lebar, kuncup dan bunga cawan |
| G12 | Orchid | 1 | Orchid | Tangkai, akar dan kuncup dasar | Daun pangkal tebal, akar udara, tangkai lengkung, bibir bunga asimetris |
| G13 | Lavender | 1 | Lavender | Batang ramping, daun sempit, pengulangan kuntum | Rumpun abu-hijau dan bunga ungu bertingkat |
| **Total** | | **31** | | | |

Daisy dan Moonflower dicatat sebagai referensi tertunda dari daftar awal. Keduanya belum menambah jumlah grup atau kebutuhan atlas yang dikunci. Bentuknya akan dicocokkan setelah referensi atau keputusan desain tersedia.

## 3. Daftar visual lengkap

Tahap 1 umumnya Seed; Tulip memakai Bulb. Tahap 4 mengikuti proses pembentukan hasil sehingga tidak semuanya berbunga. Setiap baris memiliki lima tampilan pertumbuhan dan satu tampilan panen.

| No. | Tanaman | Part | Jenis | Grup | Tahap 4 | Detail khas dan hasil panen |
| --- | --- | ---: | --- | --- | --- | --- |
| 1 | Blueberry | 1 | Buah | G01 | Flowering | Daun oval, bunga lonceng putih, buah biru bulat dengan mahkota cekung |
| 2 | Raspberry | 1 | Buah | G01 | Flowering | Batang kemerahan, daun bergerigi, bunga putih, buah majemuk merah |
| 3 | Blackberry | 1 | Buah | G01 | Flowering | Daun bergerigi, bunga putih/pink, buah majemuk ungu gelap memanjang |
| 4 | Watermelon | 1 | Buah | G02 | Flowering | Sulur menjalar, daun berlekuk, bunga kuning, semangka hijau bergaris |
| 5 | Melon | 1 | Buah | G02 | Flowering | Rambatan rendah, daun membulat, bunga kuning, buah berkulit jaring kekuningan |
| 6 | Kiwi | 1 | Buah | G03 | Flowering | Daun lebar, bunga putih, penyangga kayu, buah oval cokelat |
| 7 | Radish | 1 | Sayuran | G06 | Root Forming | Daun berlekuk, umbi merah-putih, akar runcing |
| 8 | Cabbage | 1 | Sayuran | G05 | Head Forming | Roset daun berlapis membentuk kepala kubis |
| 9 | Pea | 1 | Sayuran | G03 | Flowering | Sulur, daun oval, bunga putih, penyangga kayu, polong berisi biji bulat |
| 10 | Cucumber | 1 | Sayuran | G03 | Flowering | Sulur berpenyangga, daun lebar, bunga kuning, buah hijau panjang berbintil |
| 11 | Eggplant | 1 | Sayuran | G04 | Flowering | Batang keunguan, daun lebar, bunga ungu, buah ungu dengan kelopak hijau |
| 12 | Chili Pepper | 1 | Sayuran | G04 | Flowering | Daun runcing, bunga putih kecil, cabai hijau/merah panjang |
| 13 | Broccoli | 1 | Sayuran | G05 | Head Forming | Daun hijau kebiruan, batang bercabang tebal, kepala hijau berkelompok |
| 14 | Cauliflower | 1 | Sayuran | G05 | Head Forming | Daun roset hijau kebiruan mengelilingi kepala krem berkelompok |
| 15 | Onion | 1 | Sayuran | G06 | Bulb Forming | Daun tabung, kulit umbi cokelat keemasan, akar serabut |
| 16 | Spinach | 1 | Sayuran | G07 | Leaf Growth | Rumpun daun oval hijau gelap; panen daun bertangkai |
| 17 | Green Bean | 1 | Sayuran | G04 | Flowering | Semak tegak, bunga putih, polong ramping; tanpa penyangga |
| 18 | Zucchini | 1 | Sayuran | G02 | Flowering | Roset daun berlekuk, bunga kuning besar, buah hijau gelap memanjang |
| 19 | Rice | 1 | Serealia | G08 | Heading | Rumpun daun pita, malai menggantung, bulir hijau berubah emas |
| 20 | Wheat | 1 | Serealia | G08 | Heading | Rumpun daun pita, bulir tegak, warna hijau berubah keemasan |
| 21 | Tulip | 2 | Bunga | G11 | Bud Forming | Tahap 1 umbi, daun pita lebar, kuncup pink, bunga cawan dan tangkai potong |
| 22 | Rose | 2 | Bunga | G09 | Bud Forming | Cabang berduri, daun bergerigi, kuncup merah, kelopak spiral berlapis |
| 23 | Lavender | 2 | Bunga | G13 | Bud Forming | Daun sempit abu-hijau, bunga ungu bertingkat, panen tangkai/ikat |
| 24 | Water Spinach | 2 | Sayuran | G07 | Leaf Growth | Batang beruas berongga, daun runcing, rumpun tinggi dan panen ikat |
| 25 | Sunflower | 2 | Bunga | G10 | Bud Forming | Batang tinggi, daun lebar, kelopak kuning, disk cokelat, biji bergaris |
| 26 | Hibiscus | 2 | Bunga | G09 | Bud Forming | Semak daun bergerigi, bunga merah lebar dan benang sari panjang |
| 27 | Orchid | 2 | Bunga | G12 | Bud Forming | Daun pangkal tebal, akar udara, tangkai lengkung, bunga ungu dengan bibir khas |
| 28 | Jasmine | 2 | Bunga | G09 | Bud Forming | Semak daun oval mengilap, kuncup krem dan bunga putih kecil berlapis |
| 29 | Chrysanthemum | 2 | Bunga | G10 | Bud Forming | Daun berlekuk, kuncup bulat, kepala kuning berkelopak melengkung rapat |
| 30 | Dahlia | 2 | Bunga | G10 | Bud Forming | Daun bergerigi, kuncup bulat, kelopak pink/coral runcing berlapis |
| 31 | Marigold | 2 | Bunga | G10 | Bud Forming | Daun halus bergerigi, kuncup memanjang, bunga oranye berombak |

## 4. Jumlah keluaran dan kit bersama

31 tanaman x 5 tahap = **155 tampilan pertumbuhan**. Ditambah 31 tampilan panen = **186 keluaran visual dasar**. Jumlah ini menghitung tampilan yang perlu tersedia. Jumlah mesh unik ditetapkan setelah komponen dipilih: mesh dapat dibagi antar tahap melalui skala, susunan dan komponen aktif.

| Kit bersama | Isi | Batas penggunaan ulang |
| --- | --- | --- |
| Biji / umbi | Biji bulat, pipih, oval, bulir, umbi Tulip | Bentuk biji khas tetap dibedakan |
| Tunas | Tunas dua daun sebagai dasar umum | Onion, Rice, Wheat, Tulip memerlukan bentuk tunas tersendiri |
| Batang / akar | Batang hijau/ungu, cabang kayu, serabut, akar udara | Water Spinach perlu batang berongga; Rose perlu duri |
| Daun | Oval, bergerigi, berlekuk, pita, tabung, sempit, tebal | Siluet dan proporsi mengikuti tanaman |
| Rambatan / penyangga | Sulur, tangkai lengkung, tiang, palang, pengikat | Ground vine dan trellis disusun berbeda; kayu dibagi oleh G03 |
| Bunga pangan | Dasar putih/kuning, sepal hijau | Blueberry perlu lonceng; Eggplant perlu bunga ungu |
| Bunga hias | Tangkai, sepal, kuncup, modul kelopak | Bibir Orchid, benang sari Hibiscus, spiral Rose, cawan Tulip tetap khas |
| Hasil panen | Buah, polong, umbi, kepala, tangkai, ikatan | Komponen tanaman matang dapat menjadi sumber, setelah skala/pivot/UV dicek |

Model belah, irisan, tumpukan, peti, variasi warna tambahan, mutasi, layu/mati, regrow dan LOD belum masuk angka 186. Potongan buah pada gambar menjadi referensi bagian dalam; kebutuhan model potongan ditetapkan saat penggunaannya dipilih. Satu hasil panen dapat memiliki tambahan varian bila diputuskan kemudian.

## 5. Sumber model existing

Audit mencakup keberadaan file, hubungan prefab/material dan profil Cabbage. Kesesuaian geometri dengan gambar baru belum disahkan lewat review Blender. Sumber di bawah adalah kandidat untuk dipakai ulang atau dimodifikasi.

Folder model: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models/`.

Prefab tanaman: `Prefabs/Vegetation/Agricultural Plants/` dalam pack tersebut. Prefab hasil panen: `Prefabs/Props/Produce/`.

| Tanaman | Contoh sumber yang ditemukan | Kebutuhan penyesuaian |
| --- | --- | --- |
| Cabbage | `TFP_Cabbage_Plant_01A.fbx`, `TFP_Cabbage_Plant_02A.fbx`, `TFP_Cabbage_Plant_03A.fbx`, `TFP_Cabbage_01A.fbx` | Review tiga tahap dasar dan kepala panen |
| Cauliflower | `TFP_Cauliflower_Plant_01A.fbx`, `TFP_Cauliflower_Plant_04A.fbx`, `TFP_Cauliflower_01A.fbx` | Daun, roset, kepadatan dan ukuran kepala |
| Cucumber | `TFP_Cucumber_Plant_01A.fbx`, `TFP_Cucumber_Plant_03A.fbx`, `TFP_Cucumber_01A.fbx` | Susunan penyangga, rambatan, daun dan tahap awal |
| Eggplant | `TFP_Eggplant_Plant_01A.fbx`, `TFP_Eggplant_Plant_04A.fbx`, `TFP_Eggplant_01A.fbx` | Bunga, kelopak, batang, proporsi buah |
| Chili Pepper | `TFP_Pepper_Plant_01A.fbx`, `TFP_Pepper_Plant_04A.fbx`, `TFP_Hot_Pepper_01A.fbx` | Cabai panjang perlu dibedakan dari Bell Pepper; evaluasi bentuk semak |
| Green Bean | `TFP_Beans_Plant_01A.fbx`, `TFP_Beans_Plant_02A.fbx`, `TFP_Beans_Plant_03A.fbx` | Pastikan siluet semak tegak, bentuk daun dan polong |
| Radish | `TFP_Radish_Plant_01A.fbx`, `TFP_Radish_Plant_01A_Leaves.fbx`, `TFP_Radish_01A.fbx` | Lengkapi tahap muda dan pembentukan akar |
| Onion | `TFP_Onion_01A.fbx`, `TFP_Onion_02A.fbx`, `TFP_Onion_Spring_01A.fbx` | Umbi tersedia; daun Spring Onion hanya kandidat komponen, tetap ikuti bawang bombai |
| Spinach | `TFP_Spinach_Plant_01A.fbx`, `TFP_Spinach_Plant_02A.fbx`, `TFP_Spinach_Pile_01A.fbx` | Lengkapi tahap lain dan review bentuk panen |
| Watermelon | `TFP_Watermelon_01A.fbx`, `TFP_Watermelon_Leaves_01A.fbx`, `TFP_Watermelon_Vine_01A.fbx`, `TFP_Watermelon_Module_01A.fbx` | Rakit ulang tahap memakai buah, daun dan sulur |
| Zucchini | `TFP_Zucchini_01A.fbx`, `TFP_Zucchini_Module_01A.fbx` | Cocokkan susunan roset dan bunga besar |
| Wheat | `TFP_Wheat_Patch_01.fbx`, `TFP_Wheat_Patch_02.fbx`, `TFP_Wheat_Strand_01.fbx` | Review rumpun/bulir, tekstur dan tahap hijau/emas |
| Sunflower | `TFP_Sunflower_01A_Closed.fbx`, `TFP_Sunflower_01A_Open.fbx` dan variasi 02A | Lengkapi tahap awal, biji dan hasil potong |

Sumber bernama relevan ditemukan untuk **13 dari 31 tanaman**, termasuk kandidat alias Pepper/Hot Pepper dan Beans. Ini tidak berarti 13 tanaman sudah selesai dengan lima tahap baru. Delapan belas tanaman lain belum ditemukan lewat nama model spesifik pada audit ini; komponen umum dari aset lain tetap dapat dinilai saat eksekusi.

`TFP_Daisy_Grubber_*` adalah alat berkebun, tidak dihitung sebagai bunga Daisy. `Grass_Rosette_*` adalah rumput, tidak dihitung sebagai Rose.

### Cabbage yang sudah aktif

| Slot profil | Prefab lokal | Sumber prefab pack |
| --- | --- | --- |
| Seed, Sprout | `Prefabs/Crops/Cabbage Small.prefab` | `TFP_Cabbage_Plant_01A.prefab` |
| Small Plant, Growing | `Prefabs/Crops/Cabbage Growing.prefab` | `TFP_Cabbage_Plant_02A.prefab` |
| Mature, Harvest Ready | `Prefabs/Crops/Cabbage Mature.prefab` | `TFP_Cabbage_Plant_03A.prefab` |
| Kandidat model panen lokal | `Prefabs/Crops/Cabbage Harvest.prefab` | `TFP_Cabbage_01A.prefab` |

`Resources/Profiles/Farming/Cabbage Crop.asset` memiliki enam slot pertumbuhan yang memakai tiga prefab dasar. Referensi baru memiliki lima tahap. Pemetaan visual ditentukan saat G05 dieksekusi. Seed sekarang memakai tanaman berskala kecil sehingga perlu direview terhadap referensi biji.

## 6. Atlas dan material existing

Folder bersama: `Assets/Nature  Paradaise/Textures/Shared/Cozy/`.

| Sumber diperiksa | Isi relevan | Pemanfaatan |
| --- | --- | --- |
| `NP_CozyAtlasSlots.json` / `NP_CozySurfaces_01_*` | WoodHoney, WoodDark, Rope, EndGrain, Earth, Hay, Grain; LeafGreen, FlowerWhite, FlowerGold, FoodOrange, FoodRed, FoodPurple | Kayu dan pengikat penyangga; acuan palet tanaman. Bidang warna kecil belum mewakili detail semua daun/buah |
| `NP_CozyFoliage_01_BaseColor.png` / `NP_CozyFoliage_01.mat` | Grid 4 x 4 daun ivy, rantai ivy, bunga dekoratif putih/kuning dan rumpunnya; gambar telah ditinjau | Kandidat detail jauh/bunga generik bila cocok. Ivy tidak otomatis cocok dengan tanaman pangan. Material alpha clip dua sisi |
| Cozy 02 / 03 | Kayu bersih, PondWood, PondLeaf dan palet prop | Alternatif kayu/palet; bukan halaman utama tanaman |
| Cozy 04 | Leaf dan Soil kecil di antara material mesin | Acuan warna tambahan |
| Cozy 05 | EnamelRed dan GaugeFace | Tidak ada kebutuhan khusus tanaman yang ditemukan |
| `TFP_Atlas_Vegetation_1A.mat` | Material tanaman pack; dipakai sumber Cabbage/Cauliflower | Pertahankan di sumber. Konversi salinan ke Cozy perlu pemeriksaan UV/shader |
| `TFP_Atlas_1A.psd`, `TFP_Atlas_Cutouts_1A.psd`, `TFP_Atlas_Billboards_1A.psd` | Sumber tekstur pack tersedia | Periksa UV komponen terpilih sebelum memilih mempertahankan atau memindahkan atlas |

Cozy 01 memiliki BaseColor 2048 x 2048, Normal 1024 x 1024, dan foliage terpisah 1024 x 1024. Manifest tidak mencadangkan ruang kosong untuk seluruh tanaman. Koordinat slot existing dipertahankan; halaman lama tidak direpack untuk memasukkan tanaman.

`Material/Shared/CozyAtlas.shader` mendukung BaseMap, Normal opsional, SurfaceMask opsional dan alpha clip. SurfaceMask memakai **R untuk metallic, A untuk smoothness**; kanal lain tidak diasumsikan sebagai AO. Material foliage existing menonaktifkan Normal/SurfaceMask, jadi semua halaman tanaman tidak wajib memiliki tiga map.

## 7. Kebutuhan atlas tanaman

Usulan awal: **tiga keluarga tekstur**, masing-masing ditargetkan mulai dari satu halaman jika muat. Jumlah file atlas fisik dan ukurannya dipastikan setelah uji UV grup pertama. Grup model tidak berarti satu atlas tersendiri.

| Kode / nama kerja | Isi yang disiapkan | Pemakai |
| --- | --- | --- |
| A01 / `NP_Plants_Core_01` | Daun hijau muda/tua/kebiruan/abu-hijau; urat oval, bergerigi, berlekuk, pita, sempit dan tebal; batang hijau/ungu/berkayu; sulur, akar, sepal, kuncup hijau, biji umum, transisi hijau/emas | Komponen umum G01-G13 |
| A02 / `NP_Plants_Produce_01` | Warna berry, garis semangka, jaring melon, kulit kiwi, kulit Onion, gradasi Radish, Eggplant ungu, cabai hijau/merah, polong/timun/zucchini, kepala Broccoli/Cauliflower, bulir padi/gandum | Hasil G01-G08 |
| A03 / `NP_Plants_Flowers_01` | Kelopak putih/krem, kuning, pink/coral, merah, ungu, oranye; gradasi/urat, bibir Orchid, disk Sunflower, benang sari/polen, biji Sunflower bergaris, bunga pangan putih/kuning/ungu | G09-G13 dan bunga tanaman pangan |
| W01 / atlas kayu existing | WoodHoney/WoodDark, EndGrain, Rope | Penyangga G03; memakai Cozy 01 |

| Slot / pola khusus | Kebutuhan |
| --- | --- |
| `LeafOval`, `LeafSerrated`, `LeafLobed`, `LeafStrap`, `LeafNarrow`, `LeafThick` | Warna/urat daun. Siluet utama lewat geometri |
| `WatermelonStripe`, `MelonNet`, `KiwiSkin` | Pola kulit buah; rambut kiwi tidak dimodel satu per satu |
| `OnionSkin`, `RadishGradient` | Serat kulit dan peralihan merah-putih |
| `BerryBlue`, `BerryRed`, `BerryDark` | Palet berry; mahkota blueberry dan bentuk buah majemuk memakai geometri sederhana |
| `ProduceGreen`, `ProduceRed`, `EggplantPurple` | Palet hasil panjang/polong/terong; bentuk hasil tetap berbeda |
| `HeadGreen`, `HeadCream` | Warna kepala; klaster utama sederhana, detail halus tekstur/normal bila terlihat |
| `GrainGreen`, `GrainGold`, `SeedStripe` | Tahap bulir dan biji khas |
| `PetalWhite`, `PetalGold`, `PetalPink`, `PetalRed`, `PetalPurple`, `PetalOrange` | Palet/gradasi kelopak bersama |
| `OrchidLip`, `FlowerCenter`, `SunflowerDisk` | Detail khas bunga, diberi ruang sesuai keterbacaan dari kamera game |

Aturan eksekusi atlas:

- BaseColor wajib. Normal halus dibuat bila perlu relief. SurfaceMask dibuat jika variasi kilap perlu dibedakan dalam satu material; tanaman memakai metallic nol.
- Warna mengikuti gaya Cozy project. Detail gambar disederhanakan sesuai jarak kamera; bentuk khas dan tahap tumbuh tetap terbaca.
- Ukuran awal uji BaseColor 1024 x 1024 per halaman tanaman. Keputusan ekspor mengikuti uji kamera field dan kepadatan UV.
- Catat padding dan `uvSafe` seperti manifest Cozy. Layout UV Normal/SurfaceMask mengikuti BaseColor meski resolusinya dapat berbeda.
- Keluarga tekstur tidak otomatis menentukan jumlah material per tanaman. Packing mengutamakan satu material tubuh tanaman bila daun/hasil dapat dimuat bersama. Palet umum kecil boleh disalin dari master ke halaman yang digunakan untuk mengurangi pergantian material. Kayu dapat memakai material existing terpisah. Nilai hasilnya pada petak berisi banyak tanaman.
- Daun memakai geometri sederhana untuk siluet dekat. Kartu alpha dipakai bila diperlukan, misalnya detail jauh/LOD, dengan material cutout. Jumlah daun, bulir dan kelopak ditetapkan pada percobaan pertama.
- Slot bagian dalam buah belum wajib; tambahkan jika model potongan dipilih sebagai kebutuhan.
- Simpan source GIMP berlapis, map PNG, manifest slot dan material bersama. Jangan membuat ulang kayu atau palet yang sudah sesuai hanya karena berganti grup.

## 8. Pengerjaan per grup

**Usulan pertama: G05, Cabbage/Broccoli/Cauliflower.** Cabbage mempunyai prefab lokal dan profil aktif; Cauliflower mempunyai sumber pack. Daun/rosetnya bisa menjadi dasar bersama. Broccoli tetap memerlukan kepala bercabang khas. Percobaan ini menetapkan skala, gaya daun, UV, tekstur kepala dan pemetaan tahap sebelum diperluas.

| Urutan | Keluaran |
| --- | --- |
| 1. Pilih grup | Anggota dan referensi untuk satu batch |
| 2. Review sumber di Blender | Putusan per komponen: pakai ulang, ubah, atau buat baru; periksa geometri, material, UV, pivot dan LOD bila tersedia |
| 3. Tetapkan layout UV | Slot atlas yang diperlukan, ukuran uji, komponen bersama dan pembagian material |
| 4. Kerjakan atlas | Source GIMP, BaseColor, map tambahan yang perlu, manifest slot. Mulai dari kebutuhan grup terpilih |
| 5. Kerjakan model | Source Blender dengan kit komponen, lima tampilan tahap dan hasil panen |
| 6. Review di Unity | Ukuran, keterbacaan tahap, warna, penyangga, hasil panen dan biaya render dari kamera field |
| 7. Integrasi visual | Hubungkan aset yang telah direview; gunakan kit/slot yang sesuai untuk grup berikutnya |

Sesudah G05, sumber pack yang cukup banyak tersedia untuk G02, G04, G06 dan G07. G01 serta sebagian besar bunga membutuhkan lebih banyak bentuk khas baru. Urutan akhir mengikuti grup pilihan pengguna.

## 9. Tata letak file

Dokumen rencana tunggal: `Assets/Nature  Paradaise/Documentation/CropModelPlan.md`.

Lokasi kerja source dan aset visual:

| Isi | Lokasi |
| --- | --- |
| Source Blender/GIMP/manifest pengerjaan | `ArtSource/Crops/` dengan kit bersama dan subfolder ID grup |
| Mesh baru | `Assets/Nature  Paradaise/mesh/Crops/` |
| Prefab visual | `Assets/Nature  Paradaise/Prefabs/Crops/` |
| Atlas | `Assets/Nature  Paradaise/Textures/Shared/Plants/` |
| Material | `Assets/Nature  Paradaise/Material/Shared/Plants/` |

Kelak daftar art dapat diekspor sebagai CSV dengan kolom tetap, misalnya `plant_id`, `display_name`, `reference_part`, `model_group`, `source_asset`, `reuse_decision`, `atlas_family`, `stage`, `art_status`. Import CSV art memerlukan jalur kerja yang sesuai; importer balance sekarang tidak otomatis menerima kolom art tersebut.

Harga, musim, hari tumbuh, jumlah hasil, regrow dan angka balance tidak diturunkan dari ilustrasi.

## 10. Hasil pengerjaan G05 dan pustaka atlas

Pengguna memilih menyiapkan tekstur seluruh kategori lebih dahulu, lalu mengerjakan G05 di Blender. Keputusan atlas: terpisah dari atlas bangunan/mesin, menggunakan shader Cozy yang sama. Kayu penyangga tetap memakai sumber existing.

- Tiga halaman Core / Produce / Flowers, masing-masing 1024 x 1024 dengan 64 slot bernama dan inset UV 10px. Ada BaseColor, Normal dan SurfaceMask untuk setiap halaman, serta source GIMP berlapis.
- Komponen daun, batang dan biji umum diulang dari definisi yang sama pada halaman Produce/Flowers sehingga satu tubuh tanaman dapat memakai satu material utama.
- `NP_Plants_Catalog.json` menghubungkan 31 tanaman bergambar ke halaman dan slot yang diperlukan. Daisy/Moonflower tetap ditandai sebagai referensi tertunda.
- G05: Cabbage, Cauliflower dan Broccoli masing-masing memiliki Seed, Sprout, Young, HeadForming, Ready, serta hasil Harvest. Total 18 tampilan memakai 14 model dasar karena Seed/Sprout dibagi antar spesies. Setiap model mempunyai LOD1.
- Revisi R02 memakai kit daun membulat untuk tanaman muda, kepala kubis terbungkus rapat, kepala cauliflower/broccoli dengan permukaan menyatu, serta batang broccoli bercabang. Salinan aset pack tetap disimpan sebagai referensi di koleksi tersembunyi.
- Aset runtime: FBX, JSON mesh, native Mesh dan prefab visual dengan dua LOD. Profil/balance/scene existing tidak diganti pada batch art ini.

Panduan dan lokasi file: `ArtSource/Crops/README.md`.
Source Blender: `ArtSource/Crops/G05/Blender/G05_Brassica_Master.blend`.
Preview: `ArtSource/Crops/Previews/AtlasReview.png`, `G05_GrowthStages.png`, `G05_Closeup.png`.
Laporan pemeriksaan: `ArtSource/Crops/Review/PlantArtAudit.json` dan `BlenderReopenAudit.json`.

Revisi R02 menanggapi masukan bentuk dan warna: daun dibuat lebih lebar/melengkung, permukaan kepala disatukan agar tidak saling memotong, palet hijau diperkuat dan warna cauliflower menjadi krem. Slot atlas dan koordinat UV dipertahankan.

## 11. G02: Watermelon, Melon, Zucchini

Pengguna melanjutkan G02 setelah revisi G05. Batch ini memiliki Seed, Sprout, Young, Flowering, Ready, dan Harvest untuk masing-masing jenis: 18 tampilan memakai 14 model dasar baru serta sprout runtime G05 yang sudah tersedia. Ada varian bibit pipih hitam untuk Watermelon dan varian krem bersama untuk Melon/Zucchini.

Watermelon dan Melon memakai sulur rendah dengan bentuk daun berbeda. Zucchini memakai rumpun kompak, bunga besar dan tiga buah memanjang. Salinan tubuh buah pack Watermelon/Zucchini dipakai sebagai dasar yang diperhalus dan dipetakan ulang. Melon memakai turunan tubuh Watermelon. Semua model memakai atlas Produce existing; gambar atlas dan hasil G05 dipertahankan. Katalog art diperbarui untuk status G02 dan slot daun existing yang dipilih.

Source: `ArtSource/Crops/G02/Blender/G02_Cucurbits_Master.blend`.
Panduan: `ArtSource/Crops/G02/README.md`.
Preview: `ArtSource/Crops/Previews/G02_GrowthStages.png` dan `G02_Closeup.png`.
Aset visual: `mesh/Crops/G02/` dan `Prefabs/Crops/G02/` di root project Nature Paradaise.
Laporan: `ArtSource/Crops/G02/Review/ArtAudit.json` dan `BlenderReopenAudit.json`.

Total art tanaman yang disiapkan: 6 dari 31 referensi bergambar (G05 + G02). Profil/balance/gameplay aktif belum ditambah oleh batch model ini.
