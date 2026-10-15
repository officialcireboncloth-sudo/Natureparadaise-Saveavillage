# Audit interior Barn — reuse Toonpack / adaptasi / buat baru

Sketch di `sketch-review.html` telah disetujui untuk modeling pada 14 Oktober 2026. Hasil empat scene Blender dan kit modular berada di `ArtSource/BarnInterior_A/`. Integrasi Unity juga telah selesai setelah persetujuan berikutnya: lihat `ArtSource/BarnInterior_A/Unity/README.md` dan laporan PASS. Kapasitas gameplay tetap 4/8/14/20.

Dokumen ini mencatat **audit sebelum modeling**, dilakukan pada prefab asli melalui Unity; hewan tidak termasuk. Empat referensi pengguna disimpan di `References`. Empat tahap visual telah dimodelkan sesuai sketch, sementara pemetaan upgrade gameplay tetap menunggu integrasi.

Kesimpulan: prop dasar banyak yang tersedia di **Toon Farm Pack**, bukan hanya atlas. Yang perlu dikerjakan khusus adalah mesin pengolah pakan, palung bersekat sesuai referensi, kit distribusi otomatis dan interior room modular yang cocok dengan kamera game. Beberapa asset bernama mirip ternyata bukan bentuk yang dimaksud, sehingga tidak semuanya dikategorikan siap pakai.

## Bentuk perkembangan menurut referensi

| Gambar | Isi utama selain hewan | Perkembangan visual |
|---|---|---|
| 1 | 2 palung pakan × 3 kompartemen; 1 bak air; pencacah kecil dengan engkol; jerami, karung, tong, ember, peti, garpu dan lampu | Kandang sederhana / proses manual |
| 2 | 3 palung × 3 kompartemen; 2 bak air; pencacah bermotor; rak penyimpanan; papan alat; lemari dan perlengkapan kecil | Kapasitas bertambah, peralatan lebih lengkap |
| 3 | 4 palung × 3 kompartemen; pencacah besar; hopper/mixer kedua; pipa melintang dan 4 dispenser; wastafel/air; lemari perlengkapan | Distribusi pakan otomatis |
| 4 | 5 palung × 3 kompartemen; mesin dan pipa serupa; 5 dispenser; sepasang tangki proses; meja/botol dan bak cuci | Tambahan lini pengolahan / tangki |

Fungsi tangki gambar 4 belum ditetapkan. Bentuknya dapat dibaca sebagai tangki proses/susu, tetapi gambar tidak menentukan mekanik produksinya. Tidak menyimpulkan mesin atau pipa sudah mempunyai fungsi gameplay hanya dari bentuk visual.

## Sudah ada dan bisa dipakai

Nama di bawah telah ditemukan sebagai prefab nyata. Bentuk kandidat bertanda preview sudah dirender di `asset-gallery.html`; jumlah triangle berasal dari mesh Unity, bukan perkiraan nama file.

| Kebutuhan | Kandidat Toonpack | Keputusan |
|---|---|---|
| Tong kayu | `TFP_Wood_Barrel_01A`, `02A`, `03A` | Reuse; susun dan sesuaikan skala |
| Ember metal | `TFP_Bucket_Metal_01A`, `01B` | Reuse |
| Peti kayu kosong | `TFP_Wooden_Crate_01A`, `02A`, `03A`, `04A` | Reuse untuk stok dan output mesin |
| Karung bahan pakan | `TFP_Cereal_Sack_01A`, `02A`; `TFP_Sack_Pile_01A`, `02A` | Reuse varian karung, susun seperlunya |
| Bal jerami | `TFP_Hay_Bale_01A`–`04C`, hay stack | Bentuk tersedia; pilih varian ringan atau buat versi sederhana bila dipakai banyak |
| Jerami lepas / isi pakan | `TFP_Hay_Clump_01A`–`03B`, `TFP_Hay_Patch_01A`–`02B`, `TFP_Hay_Silage_01A` | Reuse terbatas; perlu scale/pose/isi palung. Hay patch berupa rumpun 3D, bukan decal jerami datar seperti lantai referensi |
| Garpu, sekop, sapu, sikat, gunting | `TFP_Spading_Fork_01A`, `TFP_Shovel_01A`, `TFP_Broom_01A`, `TFP_Brush_01A`, `TFP_Shears_01A` | Reuse prop yang relevan; tidak harus memakai seluruh alat |
| Papan alat | `TFP_Tool_Rack_01A`–`19A` | Ada prefab papan + peralatan; pilih yang sesuai atau rakit subset ringan |
| Botol / wadah susu | `TFP_Bottle_01A` dan varian, `TFP_Milk_Bottle_01A`, `TFP_Milk_Chug_01A` | Reuse botol/can; bukan pengganti tangki proses besar |

Seluruh lokasi prefab, GUID, material, triangle dan vertex ada di `asset-inventory.json`. Inventory tidak menggandakan file pack dan tidak mengubah atlas/material.

## Ada bahan dasarnya, perlu adaptasi atau perakitan

| Kebutuhan referensi | Asset yang benar-benar tersedia | Yang masih harus dilakukan |
|---|---|---|
| Bak minum persegi panjang dengan kaki | `TFP_Trough_03A` kosong, `04A` berisi air, `05A` berisi pakan | Paling dekat untuk reuse; bentuk penyangga dan proporsi belum sama persis dengan referensi. Bisa dipakai sebagai pengganti visual atau tambah kaki/ubah badan |
| Palung tengah stainless 3 sekat | Jenis trough 01–09 ada, tetapi tidak ada kandidat yang dirender cocok persis | `01A` bak oval; `02A` bak setengah pipa 2 ruang; `03A` bak persegi tanpa 3 sekat. Untuk kemiripan referensi lebih baik satu kit palung baru daripada sekadar stretch asset lama |
| Lemari peralatan/obat biru | `TFP_Kitchen_Cabinet_02A` dan `TFP_Bathroom_Cabinet_01A`, juga `TFP_Tool_Storage_01A/02A` | Warna, top, kaki, pintu/handle dan isi disesuaikan; versi kitchen bukan lemari obat khusus |
| Wastafel metal mandiri | `TFP_Kitchen_Cabinet_Sink_01A`, `TFP_Bathroom_Sink_01A` | Model sink ada; melepas kabinet/menambah kaki dan remap warna dibutuhkan jika ingin bentuk industrial pada referensi |
| Rak jerami dan karung 2 tingkat | `TFP_Shelf_01A` papan lurus, `02A` papan sudut; kandidat `TFP_Bookshelf_*` | Susun papan + tiang/ubah rak yang cocok. `Shelf_01A/02A` bukan rak gudang lengkap |
| Lampu lentera dinding | Toonpack `TFP_Lamp_01A/02A/03A` merupakan lampu meja/dinding berkap/industrial | Bisa pakai sebagai substitusi. Untuk bentuk lentera reference, reuse bentuk lentera dari builder Chicken Store yang sudah dibuat, lalu jadikan satu prefab modular |
| Dinding barn, balok, jendela | Barn building kit/presets memang ada | Modul pack adalah potongan bangunan, sering menyertakan atap. Tidak langsung menjadi ruangan cutaway bersih; ekstraksi/perakitan atau kit interior baru dibutuhkan |
| Lantai tanah/batu dan jerami menyebar | Dirt/pavement ground props dan hay patches ada | Perlu lantai room datar, penyesuaian material serta pola jerami rendah geometri; bukan menutup seluruh lantai dengan rumpun 3D |

`TFP_Barn_05A_Interior` telah diperiksa secara visual: isinya lantai/struktur **loteng barn**, bukan paket lengkap interior sapi dengan mesin, palung dan dekorasi seperti referensi. `TFP_Water_Tank_01A/03A/04A` berupa tangki air luar ruangan berkaki tinggi, sehingga tidak dianggap tangki stainless proses yang siap dipakai.

## Yang perlu dibuat baru agar sesuai referensi

| ID | Asset/kit baru | Pemakaian |
|---|---|---|
| NEW-01 | **Kit shell interior modular**: lantai, dinding merah, balok krem, jendela, pintu/exit, bracket; depan terbuka/cutaway | Satu kit bersama untuk semua tahap, bukan empat bangunan terpisah. Reuse material dan potongan yang cocok bila efisien |
| NEW-02 | **Palung pakan modular**: segmen ujung, segmen tengah, sekat, kaki, isi pakan terpisah | Bentuk stainless tiga ruang sesuai gambar; panjang/kompartemen dapat dirakit ulang |
| NEW-03 | **Mesin pencacah pakan** dengan hopper, ruang pemotong, outlet dan varian engkol/motor | Rangka dasar dipakai ulang untuk versi kecil dan besar; peti output bisa memakai Toonpack |
| NEW-04 | **Hopper/mixer/distributor kedua** + panel kontrol | Untuk gambar 3–4; jangan mengganti dengan mesin kendaraan/baler yang fungsinya dan bentuknya berbeda |
| NEW-05 | **Kit pipa distribusi**: lurus, siku, T, flange, tiang penyangga, kepala dispenser | Satu kit yang bisa dirakit menjadi 4/5 titik output, bukan mesin terpisah untuk setiap layout |
| NEW-06 | **Tangki proses/susu modular**: satu model tangki dipakai dua kali, sambungan pipa, valve/panel | Untuk gambar 4; bukan tangki air tower dari pack |
| NEW-07 | **Tambahan kecil yang belum ada persis**: kotak P3K, label medis, penutup/handle/adapter mesin, bidang jerami lantai bila dibutuhkan | Dibuat seperlunya setelah daftar reuse dipilih; botol, ember dan meja bisa tetap reuse |

Dengan pendekatan ini ada **6 kelompok model/kit utama baru** plus satu kelompok detail kecil. Varian mesin manual/motor/industri memakai komponen bersama. Rak gudang, lemari dan wastafel masuk pekerjaan adaptasi/perakitan terlebih dahulu; baru dimodel ulang jika adaptasinya justru lebih berat atau tidak cocok.

## Catatan mobile dari hasil audit

| Prefab asli | Triangle |
|---|---:|
| `TFP_Wood_Barrel_01A` | 350 |
| `TFP_Bucket_Metal_01A` | 252 |
| `TFP_Wooden_Crate_01A` | 242 |
| `TFP_Cereal_Sack_01A` / `02A` | 381 / 332 |
| `TFP_Hay_Bale_01A` | 888 |
| `TFP_Hay_Bale_04A` | 2.366 |
| `TFP_Hay_Clump_03A` | 158 |
| `TFP_Trough_03A` / `04A` | 297 / 301 |
| `TFP_Tool_Rack_01A` | 1.699 |
| `TFP_Kitchen_Cabinet_02A` | 297 |
| `TFP_Milk_Bottle_01A` | 124 |

Asset tersedia tidak otomatis paling ringan. Contohnya 10 bal `Hay_Bale_01A` sendiri menjadi 8.880 triangle; varian `04A` jauh lebih berat lagi. Gunakan bal sederhana/LOD bila stok jerami banyak, sedikit clump sebagai isi, dan hindari serat jerami berupa batang geometri satu per satu. Papan alat dengan banyak perkakas dapat diganti subset yang memang tampak. Jumlah triangle prefab dalam inventory menjumlahkan semua MeshFilter; bila suatu prefab mempunyai LODGroup, jumlah itu mencakup semua LOD, bukan satu LOD aktif.

Target awal yang diusulkan, belum hasil model: shell 1–2k triangle; palung lengkap 150–250 triangle; pencacah kecil 500–800, versi besar 900–1.400; mixer 500–900; satu dispenser 100–180; satu tangki 300–500; pipa memakai penampang 6–8 sisi. Anggaran total ditentukan setelah layout dan jumlah instancenya dikunci, terpisah dari hewan. Gunakan material bersama dan lampu baked/emissive; jangan menambah point light untuk setiap lentera.

## Integrasi yang sudah ada di project

- Scene `Assets/Nature  Paradaise/Map/Scenes/Interiors/BarnInterior.unity` sudah tersedia, begitu juga `BarnInteriorLayout_Lv1` sampai `Lv4`.
- Layout itu masih berisi `Floor_ReplaceMe`, `Wall_*_ReplaceMe`, `AnimalArea_ReplaceMe`, `FeedMaker_Position_ReplaceMe` dan `FeedSilo_Position_ReplaceMe`.
- `BarnInteriorSceneController` saat ini membuat visual palung dan isi dari Cube, bukan prefab Toonpack. Saat implementasi nanti, visual perlu diganti agar tidak menumpuk dengan model baru.
- Kapasitas default slot pakan dalam controller adalah 4/8/14/20. Referensi memperlihatkan 2/3/4/5 palung dengan tiga ruang, yaitu 6/9/12/15 kompartemen. Pemetaan visual terhadap kapasitas gameplay harus ditetapkan saat layout; jangan otomatis mengubah kapasitas hanya karena mengikuti gambar.
- Marker exit, spawn, slot dan mesin dipertahankan saat mengganti visual. Controller, save, kapasitas, fungsi mesin dan scene gameplay belum diubah oleh audit ini.

## File review

- `asset-gallery.html`: preview prefab asli, nama, ukuran dan triangle.
- `asset-inventory.json`: hasil inventory native Unity beserta path/GUID/material/mesh.
- `asset-shortlist.csv`: 34 kandidat yang dipilih, dengan keputusan reuse/adaptasi/buat baru, path asli dan jumlah triangle. Kandidat bookshelves/ground tambahan dari pencarian nama belum semuanya masuk preview; jangan menganggapnya sudah cocok secara visual.
- `all-pack-prefabs.txt`: daftar nama/path seluruh prefab di Toon Farm Pack yang diperiksa untuk pencarian.
- `References/01-basic.png` sampai `04-processing.png`: empat referensi pengguna.

Langkah berikut setelah daftar disetujui: pilih kandidat reuse → sketch/blockout interior per tahap dan jalur jalan → tetapkan modul yang harus dibuat → modeling mobile → integrasi visual dengan marker/controller yang sudah ada. Audit ini belum menjalankan tahap tersebut.
