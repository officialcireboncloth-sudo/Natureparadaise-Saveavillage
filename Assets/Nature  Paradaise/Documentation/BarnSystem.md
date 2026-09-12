# Barn System

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
