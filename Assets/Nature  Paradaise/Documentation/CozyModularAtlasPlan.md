# Rencana rework rumah dan atlas modular cozy

Tanggal: 31 Oktober 2026. Status: atlas modular dan eksterior rumah Lv.1 sudah diproduksi,
diuji di Unity, dan dipasang pada prefab utama. Pada 1 November, toko ayam dan coop
pendamping di samping toko juga dirework; lihat [panduan poultry](PoultryCozyRework.md).
Coop yang dibangun player, interior, level rumah lain, serta bangunan lain masih
mengikuti daftar pekerjaan di bawah. Lihat [panduan rumah](CozyHouseRework.md).

## Target dan urutan

Prioritas pertama adalah eksterior rumah pada sisi kanan referensi pertama:
genteng hijau zaitun, plester krem, rangka kayu madu, pintu lengkung, cerobong bata,
pondasi batu, tangga, lampu, kotak bunga, tanaman rambat, dan kursi teras.
Tampak depan, siluet, proporsi, susunan ornamen, serta karakter tekstur menjadi
patokan pencocokan. Gambar tunggal belum menetapkan ukuran dunia, tampak samping,
dan belakang; bagian tersebut perlu dirancang konsisten dan diperiksa dari kamera
game. Target visual 1:1 tidak berarti memasukkan pencahayaan render referensi ke albedo.

Setelah eksterior rumah menjadi patokan kualitas, keluarga material yang sama
dipakai untuk interior rumah, toko ayam, coop, barn, barn store, stand sayur,
bin panen, serta interior kandang. Screenshot kedua dan ketiga adalah referensi
yang sama, sehingga tidak dihitung sebagai dua set pekerjaan.

## Audit aset yang tersedia

- `Material/Shared Wood Atlas.mat` memakai atlas warna 2048×2048 dan normal
  1024×1024 di `Textures/Buildings/BarnStyle_B`. Android/iOS saat ini dibatasi
  1024 untuk atlas warna. Ada sumber XCF yang dapat diedit.
- Manifest `SharedWoodSlots.json` mencatat kayu Warm, Dark, dan PaintedCream.
  Atlas juga berisi kayu merah/atap kayu merah, palet warna, daun/bunga, dan
  elemen pack lain. Slot yang sudah terpakai tidak boleh ditimpa/digeser.
- Serat kayu yang tersedia menjadi bahan awal, tetapi perlu ditata ulang untuk
  kebutuhan baru: variasi papan, warna, sambungan, dan skala serat yang konsisten.
- House A memiliki master `ArtSource/House_A/Blender/House_A_Masters.blend`,
  empat desain unik untuk lima level, serta tiga LOD per desain di Unity.
  Rework harus menjaga kaitan level, pintu, collision, serta skala terhadap player.
- Pack memiliki prefab karung/sack, hay bale, dan crate yang bisa menjadi bahan
  properti. Keberadaan file belum berarti bentuk/materialnya sudah cocok dengan
  referensi; pilih dan adaptasi ketika mengerjakan aset tersebut.

## Daftar keluarga tekstur

Nomor berikut adalah ID kebutuhan, bukan jumlah file PNG. Satu keluarga dapat
memiliki beberapa potongan/variasi dalam atlas yang sama.

| ID | Keluarga / variasi | Pemakaian | Tahap |
| --- | --- | --- | --- |
| T01 | Kayu madu: serat lebar, mata kayu sedikit, 3 variasi | Tiang/lis rumah, pintu, kusen, kursi; kemudian peti, stand, bin | Rumah exterior |
| T02 | Kayu cokelat tua: 2 variasi | Balok, lis kontras, bagian dalam bin, furnitur | Rumah exterior |
| T03 | Kayu bercat krem dan merah, serat tetap terbaca | Trim rumah, papan barn/coop/toko | Krem untuk rumah; merah untuk revisi berikutnya |
| T04 | Ujung potongan kayu / end grain | Ujung balok, tiang, tutup peti | Rumah exterior |
| T05 | Papan lantai: warna sedang, sambungan selang-seling, beberapa crop | Interior rumah, panggung/meja stand | Interior rumah |
| T06 | Genteng/shingle stylized: zaitun, abu kebiruan, merah bata | Zaitun untuk rumah; variasi lain untuk toko, coop, barn | Zaitun dahulu |
| T07 | Plester krem hangat: bercak lembut, pori halus | Dinding exterior dan interior rumah/toko | Rumah exterior |
| T08 | Batu pondasi blok: beige hangat dan abu tua | Pondasi, alas tiang, bawah dinding interior | Beige dahulu |
| T09 | Batu pijakan / paving: muka lebih rata, garis nat | Teras, anak tangga, ambang pintu, jalur pendek | Rumah exterior |
| T10 | Bata oranye kecokelatan dengan nat krem | Cerobong dan bagian masonry toko | Rumah exterior |
| T11 | Besi gelap: goresan lebar ringan, permukaan tidak hitam polos | Lampu, engsel, gagang pintu, bracket, paku; bin dan tools | Rumah exterior |
| T12 | Aluminium/stainless: abu netral, variasi roughness lembut | Bak pakan/air, ember, kaleng susu, mesin | Revisi barn/coop |
| T13 | Terakota: 2 warna tanah bakar | Pot bunga rumah, toko, stand | Rumah exterior |
| T14 | Kain katun: krem, sage, merah; pola tenun lembut | Selimut, bantal, taplak, kanopi hijau-krem/merah-krem | Interior rumah, stand/toko |
| T15 | Goni kasar: warna karung dan jahitan | Karung pakan/hasil kebun, dekorasi barn/toko | Revisi barn/toko |
| T16 | Serat tali dan anyaman: potongan pola berbeda | Ikatan kanopi, hay bale, keranjang telur | Revisi stand/toko |
| T17 | Tanah padat kecokelatan, variasi gumpal halus | Lantai barn/coop, tanah dalam pot | Revisi barn/coop |
| T18 | Jerami: padat untuk bale dan helai jarang untuk kartu/decal | Bale, pojok kandang, isi tempat pakan dan penetas | Revisi barn/coop |
| T19 | Biji/pakan: butiran gandum atau pelet yang terbaca | Isi karung, bak pakan, mesin, display toko | Revisi barn/toko |
| T20 | Daun dan rambatan: hijau gelap/sedang/terang | Kotak bunga, semak, tanaman pot, rambatan tiang rumah | Rumah exterior |
| T21 | Bunga kecil: putih-kuning utama, aksen tambahan terpisah | Planter rumah, pot, semak, hiasan toko | Rumah exterior |

Sediakan juga pita palet kecil untuk keramik putih, telur krem, warna hasil kebun,
kaca, cahaya lampu, kertas/label, layar mesin, dan warna logo. Papan nama/logonya
merupakan elemen grafis tersendiri di atlas; teks toko tidak dibuat sebagai pola
berulang di kayu. Bentuk logo ayam/sapi/sayur tetap dapat berupa mesh sederhana.

## Susunan atlas yang diusulkan

1. **`NP_CozySurfaces_01`**, satu atlas permukaan opaque bersama.
   Sumber warna 2048×2048; rumah menggunakan slot kayu, genteng zaitun, plester,
   batu, bata, besi, terakota dan palet kecil dahulu. Ruang untuk lantai, kain,
   karung, aluminium dan properti lain disiapkan sejak awal. Ukuran ini target
   awal; detail di kamera game menentukan apakah distribusi slot sudah cukup.
2. **`NP_CozyFoliage_01`**, atlas transparansi cutout bersama, target awal
   1024×1024. Memuat T20/T21 dan helai jerami T18 yang memerlukan alpha.
   Penggunaan cutout membatasi bidang transparan yang saling menumpuk.
3. Normal map mengikuti koordinat slot atlas permukaan yang sama. Target awal
   1024×1024 untuk varian mobile; gunakan normal ringan untuk serat, nat,
   sambungan genteng, kain dan logam. Surface mask hanya dibuat bila shader
   terpilih membacanya, terutama untuk membedakan besi, aluminium, dan kayu.
   Keduanya bukan pengganti mesh tebal/bevel.
4. Varian warna genteng, kayu cat, dan kain ditempatkan sebagai slot UV dalam
   atlas. Jangan membuat material unik untuk setiap warna/objek. Kaca bercahaya
   dan lampu dapat memakai palet plus mask emission bersama; air/kaca transparan
   memakai material khusus jika perlu. Satu atlas tidak otomatis berarti satu
   draw call untuk seluruh bangunan.

Detail utama kayu dan genteng mendapat ruang lebih besar daripada palet kecil.
UV plank mengikuti arah serat; potongan tetangga memakai crop berbeda agar tidak
terlihat menyalin pola yang sama. Tiap slot perlu padding/dilasi warna untuk
mipmap dan kompresi. Uji normal serta warna setelah downscale mobile, terutama
genteng dan serat yang bisa hilang pada kamera jauh.

Atlas lama tetap menjadi sumber aset lama selama perpindahan bertahap. Atlas
cozy baru tidak menimpa atlas vendor maupun area UV lama yang sudah dipakai tools,
barn dan interior. Manifest final mencatat nama slot, koordinat pixel, UV aman,
arah serat, pemakai, versi, dan lokasi sumber XCF.

## Daftar perubahan model dari referensi

| Aset | Revisi model yang dibutuhkan |
| --- | --- |
| Rumah exterior | Siluet/kemiringan atap, lis dan tiang lebih tebal, bevel tipis, tepi genteng/ridge, pintu atas lengkung, kusen dalam, cerobong bata, tangga/pondasi, lampu, planter, rambatan dan kursi teras |
| Rumah interior | Balok/kusen tebal, bevel furnitur, lantai bervariasi, bentuk selimut berbobot/lipatan lembut, bracket peti dan detail radio; jalur kasur/pintu tetap dapat dipakai |
| Toko ayam | Atap lebih seimbang, rangka menonjol, kanopi lentur, display telur/keranjang/peti, etalase dan pot |
| Coop | Bentuk lebih padat, bukaan/nest jelas, ramp, trim, bell/lampu, jerami dan wadah |
| Barn | Proporsi atap/lis, pintu serta loteng lebih jelas, rak hay, pot, tools dan wadah sebagai dekorasi; logo sapi tetap dipertahankan sesuai revisi terdahulu |
| Barn store | Rangka lebih tebal, fasad dan papan nama jelas, area display kanan terbuka, karung/pakan/peti |
| Stand sayur | Kanopi melengkung turun dengan gelombang tepi, tiang dan sambungan tebal, tali, peti dan hasil kebun |
| Bin panen | Bevel, tebal papan, bingkai tutup, engsel dan pelindung sudut logam; tutup tetap terpisah untuk animasi |
| Interior kandang | Rangka tebal, bentuk bak dengan bibir dan kaki, mesin terbaca, tekstur tanah/jerami, karung dan lampu; jumlah slot pakan mengikuti level gameplay, bukan jumlah pada ilustrasi |

Susunan genteng di tengah bidang dibantu warna/normal; tepi, nok, dan lapisan yang
mempengaruhi siluet dibuat mesh. Lengkung kanopi dan lipatan besar selimut dibuat
mesh. Serat, bekas kuas, jahitan kecil, dan pori dibantu tekstur. Cahaya jendela,
bayangan panjang dan suasana hangat dikerjakan di Unity agar tetap mengikuti
jam/cuaca; jangan melukis sorotan matahari tetap pada albedo.

## Pelaksanaan setelah daftar kebutuhan disepakati

Urutan: palet/slot atlas dan contoh material pada kubus/papan → blockout rumah Lv1
dan proporsi terhadap player → model rumah sesuai tampak referensi → UV/normal
dan detail dekorasi modular → perbandingan Blender dan Unity pada sudut kamera
yang sama → LOD, collision, akses pintu, lighting siang/malam dan pemeriksaan mobile.

Rumah menjadi aset pertama yang selesai. Revisi aset lain memakai slot dan modul
yang sudah disediakan, tanpa membuat ulang set tekstur per bangunan. Level rumah
berikutnya harus mengikuti sistem upgrade yang sudah ada; ukuran pada screenshot
bukan ukuran collider atau luas tapak yang boleh diterapkan begitu saja.

Status 1 November 2026: Barn Store: rework exterior selesai memakai atlas cozy bersama;
master modular, tiga LOD dan audit Unity tersedia pada BarnStoreCozyRework.md.
Bangunan kandang ternak player tetap terpisah dari rework toko ini.

Status 1 November 2026: Interior rumah Lv.1: rework cozy selesai; memakai atlas
bersama, 20.760 segitiga, enam mesh renderer, master modular dan 55 cek Unity.
Lihat HouseInteriorCozyRework.md serta HouseInteriorCozyReview/.
