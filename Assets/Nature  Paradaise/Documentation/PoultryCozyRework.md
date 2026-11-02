# Chicken Store dan coop pendamping — rework cozy

1 November 2026. Target adalah bangunan sisi kanan referensi perbandingan:
toko ayam dan coop kecil di samping toko. **Coop yang dibangun player tidak
termasuk rework ini.** Sesuai arahan terakhir, semua bukaan coop pendamping
dibiarkan kosong; tidak ada model ayam tetap atau hewan gameplay yang ditambahkan.

## Berkas utama

Lokasi relatif terhadap `Assets/Nature  Paradaise/` (dua spasi).

| Hasil | Lokasi |
| --- | --- |
| Master toko Blender | `Source~/PoultryCozy/ChickenStore_Cozy_Master.blend` |
| Master coop pendamping Blender | `Source~/PoultryCozy/ChickenCoop_Companion_Cozy_Master.blend` |
| Skrip pembentukan ulang dan manifest | `Source~/PoultryCozy/` |
| LOD FBX dan pintu toko terpisah | `mesh/Buildings/PoultryCozy/` |
| Prefab toko yang dipakai | `Prefabs/ShopsMobile/Modules/ChickenStore_Mobile_A.prefab` |
| Prefab coop pendamping yang dipakai | `Prefabs/ShopsMobile/Modules/ChickenCoop_Mobile_A.prefab` |
| Area toko dan pagar yang sudah ada | `Prefabs/ShopsMobile/ChickenStore_Mobile_Group.prefab` |
| Atlas bersama warna, normal, mask, foliage | `Textures/Shared/Cozy/` |
| Sumber GIMP dan revisi genteng slate | `Source~/SharedAtlas/` |
| Preview dan laporan Unity | `Documentation/PoultryCozyReview/` |

Master menyimpan modul yang bisa diedit terpisah. Koleksi `02 GAME LODS` berisi
mesh gabungan ekspor, dan koleksi studio/anchor tidak ikut FBX. `Source~` tidak
diimpor Unity, namun perlu ikut Git agar sumber tetap tersedia di PC lain.

## Detail yang dirework

Toko memakai rangka madu dan lis lebih tebal, gable depan berlapis, dua cerobong,
papan nama `LIVE CHICKEN STORE` dengan lambang ayam dan ornamen daun, kanopi
merah-krem melengkung dengan hem scallop, rak etalase berisi telur, keranjang,
dua peti telur di bawah meja, papan kecil, karung, dan pot/planter bunga.
Pintu berada pada hinge terpisah, termasuk collider daunnya.

Coop pendamping memakai papan merah vertikal pada sisi, cream pada gable,
trim merah, dua nest kosong, satu pintu tengah kosong, ramp pendek, bukaan
loteng melengkung dengan lampu, barrel, ember aluminium berisi air, karung,
hay bale, pot, serta satu rooster weather vane. Ornamen optional lama pada
group dilepas supaya tidak bertumpuk dengan vane baru.

Referensi depan menjadi acuan proporsi dan susunan detail; sumber juga
menyediakan sisi/belakang tertutup untuk pemakaian di dunia game.
Preview Unity adalah render pemeriksaan aset terisolasi; pencahayaan Map
tetap mengikuti jam/cuaca dan sistem lighting game yang sudah ada.

## Atlas dan mobile

Semua bangunan memakai tiga material cozy bersama yang juga digunakan rumah.
Tidak ada tekstur khusus per objek. Slot `RoofSlate` dibenahi menjadi kepingan
genteng persegi berlapis; warna dan normal di luar slot tersebut tidak berubah.
42 slot beserta koordinat UV lama dipertahankan, termasuk genteng rumah zaitun.
Sumber XCF dan `rectangular_slate_gimp.py` ikut disimpan.
Alpha smoothness pada surface mask juga dibetulkan: foreground fill GIMP
sebelumnya menghasilkan alpha 1. Layer opacity kini secara eksplisit menghasilkan
0,12 untuk kayu/kain/genteng, 0,22 besi, dan 0,45 aluminium, sehingga permukaan
tidak berkilau penuh. Ini merupakan koreksi peta material bersama.

| Aset | LOD0 | LOD1 | LOD2 |
| --- | ---: | ---: | ---: |
| Toko, tanpa daun pintu | 25.694 tris | 8.252 tris | 3.762 tris |
| Daun pintu terpisah | 568 tris | 568 tris | 568 tris |
| Coop pendamping | 8.576 tris | 3.988 tris | 1.852 tris |

LOD dekat memakai tiga material; LOD jauh mengurangi ornamen dan submesh.
Pintu ikut LODGroup agar ikut cull pada jarak jauh. Ambang layar 0,23 / 0,085 /
0,012, dengan transisi crossfade. Import FBX menonaktifkan animation, lights,
cameras dan mesh CPU-readable. Pengaturan atlas Android/iOS tetap maksimum
1024 dan ASTC 6×6. FPS pada perangkat mobile fisik belum diukur.

## Integrasi dan pemasangan ulang

GUID prefab module dan group dipertahankan; Map tetap merujuk aset yang sama.
Posisi/skala root module, collider badan, susunan pagar, dan referensi toko lama
dipertahankan. Hanya instance ornamen optional lama yang dilepas dari group.
Definisi building, biaya/kapasitas, prefab coop player, dan scene Map tidak diedit.
Lampu point hangat pada model mulai nonaktif dan menggunakan
`OutdoorNightLighting` yang sudah ada, tanpa shadow map tambahan.

Untuk menerapkan ekspor baru, hentikan Play Mode lalu pilih
**Nature Paradise → Art → Apply Cozy Chicken Store and Companion Coop**.
Prefab hasil produksi sudah dipasang; clone tidak wajib menjalankan menu ini.
Menu lama `Apply Mobile Chicken Store and MiniMarket to Map` adalah setup
aset lama dan akan membuat ulang model lama; gunakan menu cozy untuk rework ini.

Skrip Blender memerlukan helper yang sudah disertakan pada
`Source~/HouseCozy/build_house_cozy.py`. Skrip membangun ulang basis dari nol;
jangan menjalankannya di atas hasil edit manual yang belum dicadangkan.
Pengeditan manual pada master harus diekspor ke FBX, dan XCF harus diekspor ke PNG.

## Validasi

Unity 6000.0.81f1 mengimpor model, material dan texture, mencocokkan skala/orientasi,
jumlah segitiga, batas UV, referensi atlas, hinge pintu, collider badan dan
pewarisan dua module pada group toko. Render depan, samping depan, belakang
dan LOD2 tersedia. Pengecekan atlas memastikan slot lain tetap utuh. Berkas
hasil teruji dicocokkan dengan proyek utama sebelum diserahkan; save tidak dipakai.
