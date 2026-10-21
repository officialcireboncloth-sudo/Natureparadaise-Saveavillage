# Barn System

Revisi terbaru (21 Oktober 2026): exterior Barn B memakai **atap papan kayu,
dinding kayu horizontal hangat, logo kepala sapi dan lonceng bertali di fasad**.
LOD statis 4.446 / 3.812 / 2.966 segitiga, plus mesh lonceng/tali 240 segitiga
yang berayun saat perintah hewan berhasil. Semua berbagi material/atlas yang sama.
Lonceng menempel pada prefab aktif, tidak lagi membuat kotak/tombol dummy.
Radius pintu dan lonceng mengikuti jarak marker setelah skala placement sehingga
tidak tumpang-tindih. E atau tap prompt menjalankan target yang sedang terlihat;
panel lonceng memakai tombol keluar/masuk hewan yang sudah ada.
Validasi: 429 cek gameplay barn + 65 regresi interaksi rumah + 271 normal/shader
mobile PASS. Laporan/render: `Documentation/BarnB-WoodBell-2026-10-21`.
Sumber lengkap: `ArtSource/BarnStyle_B/Blender/BarnStyle_B.blend`.

Catatan integrasi awal sebelum revisi atap kayu:

Exterior barn sekarang memakai **Barn B dengan tekstur kayu/genteng GIMP**
(21 Oktober 2026): satu renderer dan satu material atlas per LOD. Collider
mengikuti badan bangunan dan tepi serambi; marker pintu berada di depan model.
Prefab exterior Lv1–5 berbeda uniform scale saja. Building Definition yang
sudah ada menghubungkan Lv1–4; prefab Lv5 siap dipakai setelah data biaya dan
kapasitas gameplay Lv5 ditentukan. LOD0/1/2 dibuat di Blender dan dipilih oleh
LODGroup Unity otomatis berdasarkan tinggi bangunan di layar, termasuk culling.
LOD0/1/2 memiliki 5.002 / 3.500 / 2.500 segitiga. Arah genteng diperbaiki pada
ekspor Unity agar sesuai shader yang hanya menggambar sisi depan. File gabungan
ada di `ArtSource/BarnStyle_B/Blender/BarnStyle_B.blend`. Aset runtime ada di
`mesh/Buildings/BarnStyle_B`. Untuk apply ulang pakai
`Nature Paradise > Barn > Apply Barn B Wood and Roof`.

Interior Lv1–4 juga memakai Barn B dan atlas yang sama. Ukuran 1,75×, referensi
pakan, mesin, hewan, collider dan prefab root/GUID dipertahankan. Pemasangan
tidak menulis ulang Map, scene interior, Building Definition, harga, kapasitas
atau save player. Uji Play Mode memakai scene interior tersimpan dan jalur
portal sebenarnya: 213 pemeriksaan PASS, termasuk rotasi barn 0/90/180/270°,
semua level interior, isi pakan, bagian pakan bersama dan masuk/keluar barn.
Laporan dan render native Unity: `Documentation/BarnB-Unity-2026-10-21`.

Kayu dan genteng Barn B sekarang memakai satu normal atlas GIMP 1024×1024
bersama, strength 1, dengan shader khusus `Nature Paradise/Barn Atlas Normal`.
Atlas warna dan shader Toonpack asli tetap. Normal map linear, mipmap aktif,
kompresi PC BC5 / Android ETC2 / iOS ASTC 6×6; tidak menambah polygon atau material.
Tangent mesh dihitung ulang di Editor dari UV, tanpa perubahan geometri/referensi.
241 pemeriksaan native PASS termasuk A/B kamera gameplay dan kompilasi GLES3/Vulkan/Metal;
FPS perangkat mobile fisik belum diuji. Apply ulang lewat
`Nature Paradise > Barn > Apply Shared Normal Atlas`.
Laporan: `Documentation/BarnB-Normal-2026-10-21`.

Barn System menyediakan kapasitas kandang per bangunan, reservasi slot kelahiran, Universal Animal Feed, Feed Maker, Feed Silo, dan scene interior kandang tersendiri.

## Kapasitas

| Level | Barn | Coop |
| --- | ---: | ---: |
| 1 | 4 | 6 |
| 2 | 8 | 12 |
| 3 | 14 | 20 |
| 4 | 20 | 30 |

Anak hewan dan proses breeding/incubation memakai satu slot. Slot prenatal langsung direservasi agar kapasitas tidak terlampaui saat hewan lahir. Kapasitas berlaku per kandang.

## Feed Maker

Mesin test berada di `Map > 90_TESTING_WORKSPACE > FeedMaker_Editable`, dekat posisi awal player.

1. Potong rumput dengan Sickle untuk memperoleh Grass/Fodder.
2. Dekati mesin dan tekan `E`.
3. Pilih resep. Proses tetap berjalan ketika waktu game maju atau player tidur.
4. Setelah status Ready, buka mesin dan ambil Animal Feed.
5. Animal Feed dapat diberikan langsung atau disimpan di trough melalui panel kandang.

Resep awal:

- Grass/Fodder x5 menjadi Animal Feed x5.
- Corn x3 menjadi Animal Feed x5.
- Wheat x3 menjadi Animal Feed x5.
- Campuran Cabbage, Corn, atau Wheat x3 menjadi Animal Feed x4.

Level mesin memberi kapasitas bahan 10/20/40/60, jumlah slot proses 1/2/3/4, dan kecepatan yang meningkat. Level 4 dapat mengirim hasil ke Feed Silo atau stok kandang yang dihubungkan melalui Inspector.

## Interior Barn

Interior memakai scene tersendiri `Assets/Nature  Paradaise/Map/Scenes/Interiors/BarnInterior.unity`, seperti interior rumah. Map utama hanya menyimpan portal tiap Barn di `30_WORLD > Buildings > BarnPortals_Editable`.

Setup satu kali di Editor:

1. Hentikan Play Mode.
2. Saat scene Map terbuka, jalankan `Nature Paradise > Barn > Setup or Update Barn System`. Menu ini membuat prefab modular, portal, scene interior, dan memasukkan scene interior ke Build Settings jika belum ada.
3. Simpan Map.
4. Jalankan `Nature Paradise > Barn > Open Barn Interior Scene` untuk membuka dan mengedit interior secara manual.

Jalankan `Nature Paradise > Barn > Setup or Update Barn System` satu kali. Menu ini membuat dan menghubungkan prefab berikut tanpa mengubah prefab asli Toon Farm Pack:

- `Prefabs/Barn/Exterior/BarnExterior_Lv1` sampai `BarnExterior_Lv4`.
- `Prefabs/Barn/Interior/BarnInteriorLayout_Lv1` sampai `BarnInteriorLayout_Lv4`.

Prefab exterior memiliki tiga bagian utama. Ubah scale/rotation model melalui `Model_Editable`, bentuk badan collision melalui `Collision_Editable > Box Collider`, dan geser `Entrance_Editable` ke titik tempat prompt masuk harus muncul. Runtime hanya membaca konfigurasi ini dan tidak menggesernya ketika Play. Collider portal lama dinonaktifkan saat prefab modular aktif agar collision tidak ganda.

Scene berisi empat prefab instance `Layout_Lv1_Editable` sampai `Layout_Lv4_Editable`. Semuanya menempati ruang interior yang sama dan controller hanya mengaktifkan layout sesuai level Barn yang sedang dimasuki. Buka prefab interior level yang ingin diubah melalui Prefab Mode. Layout level tinggi dibuat lebih luas. Object dengan akhiran `_ReplaceMe` adalah dummy lantai, dinding, posisi Feed Maker, posisi Feed Silo, dan area hewan yang aman diganti dengan model final.

Lantai dummy memakai collider, sehingga player tidak jatuh ke void. `BarnInteriorEntrySpawn`, `BarnExitDoor_Editable`, dan `AnimalSpots_Editable` merupakan marker sistem; pertahankan object tersebut atau pindahkan posisinya mengikuti layout final.

Generator hanya berjalan melalui menu Editor. Object dummy yang dihapus dari scene tidak dibuat kembali saat Play. Saat migrasi, layout lama di Map dinonaktifkan dan portalnya dipindah ke `BarnPortals_Editable`; layout lama dapat dihapus setelah scene interior baru sudah diuji.

Menu modular sekaligus merapikan hierarchy Map. Untuk setiap Property Site hanya disimpan satu `BarnAccess_<site-id>`. Child `Room_Editable` lama beserta duplikasi 30 animal slot, lantai, mesin, dan dinding di masing-masing portal dihapus karena interior sekarang berasal dari scene/prefab bersama. Container `Interiors_Editable` lama juga dihapus bila kosong.

Scene interior berisi 30 titik tampilan hewan, posisi cadangan untuk Feed Maker dan Feed Silo, pintu masuk, dan pintu keluar. Portal baru aktif ketika Property Site sudah menjadi Barn. Tekan `E` di pintu untuk masuk/keluar dan `I` di pintu interior untuk membuka pengelolaan hewan.

Barn Lv.1–Lv.4 menggunakan wrapper prefab Nature Paradise yang berisi model Toon Farm Pack `TFP_Barn_01A` sampai `TFP_Barn_04A`. Saat berada di interior, tekan `Y` untuk mengeluarkan hewan Barn tersebut; tekan `Y` lagi untuk memanggil hewan kembali masuk.

`BarnEntrance_Editable` pada Property Site tetap menjadi fallback untuk proyek/save lama. Setelah prefab modular dipasang, `Entrance_Editable` di prefab exterior menjadi posisi prompt `E: Masuk`. Kamera langsung berpindah ke player saat masuk/keluar sehingga perpindahan map dan interior tidak memakai camera smoothing.

## Shortcut Debug Konstruksi

Shortcut hanya aktif di Unity Editor dan Development Build:

- `C` atau `Enter`: build/upgrade normal dengan biaya dan waktu konstruksi.
- `J` pada Build Menu atau preview: biaya Gold dan material diabaikan, bangunan langsung selesai.
- `J` pada konstruksi yang sedang berjalan: langsung menyelesaikan konstruksi.
- `J` di dekat bangunan selesai: upgrade satu level secara gratis dan instan.

Mode debug tetap memeriksa validitas lokasi dan footprint bangunan.

## Layout Interior per Level

Dummy Lv.1 berukuran 22×24 unit, Lv.2 28×30, Lv.3 36×38, dan Lv.4 46×48. Ganti child visual pada masing-masing prefab level tanpa mengubah root prefab. Posisi, hierarchy, model, collider, dan material bebas diedit; object yang dihapus tidak dibuat kembali saat Play. Menu modular tidak menimpa prefab yang sudah ada, sehingga edit manual tetap aman ketika menu dijalankan lagi.
