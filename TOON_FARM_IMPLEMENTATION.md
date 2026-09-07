# Toon Farm — implementasi awal

6 September 2026. Scope: pemindahan pack, sapi, ayam, dan tanaman kubis.

## Lokasi

- Sumber vendor: `Assets/Nature  Paradaise/Pack/Toon Series/`. Seluruh pack dan `Shared` dipindahkan melalui Unity AssetDatabase; 5.610 GUID aset/folder yang diindeks tetap sama. Aset vendor tidak diseleksi/dihapus pada tahap ini.
- Prefab gameplay hewan: `Assets/Nature  Paradaise/Prefabs/Animals/Cow.prefab` dan `Chicken.prefab`.
- Model hewan: `Prefabs/Animals/Visuals/` di bawah folder game. Ada Cow Adult, Cow Calf, Chicken Adult, Chicken Chick, Chicken Egg, dengan controller idle pada hewan.
- Tanaman: `Prefabs/Crops/Cabbage Small.prefab`, `Cabbage Growing.prefab`, `Cabbage Mature.prefab`, `Cabbage Crop.prefab`, `Cabbage Harvest.prefab`.
- Scene yang diperbarui: `Map/Scenes/Testing/TestingScene.unity`. Scene Map/interior tidak dirombak.
- Profil yang terhubung: Cow Growth Profile, Chicken Growth Profile, Cabbage Crop; prefab hasil panen juga dipasang pada item Cabbage.

## Mengedit dan menghapus objek

Sapi dan ayam memakai **Use Scene Stage Visuals** pada `AnimalGrowthSystem`. Model tiap tahap tersimpan sebagai child di `Visuals_Editable`, terlihat dari Edit Mode. Script hanya mengaktifkan tahap yang sesuai; tidak menginstansiasi model/dummy dan tidak mengembalikan transform child pada mode ini.

- Pindahkan root Cow/Chicken untuk mengubah posisi hewan.
- Edit child `Visuals_Editable/Young`, `Adult`, dan tahap lain untuk mengatur posisi, ukuran, atau bentuk visual per tahap.
- Hapus seluruh root bila hewan tidak diinginkan. Hapus child tahap tertentu bila ingin tahap itu tidak memiliki model. Referensi kosong tidak menghasilkan pengganti otomatis.
- Menghapus model dari prefab gameplay akan berlaku pada instance yang mewarisi prefab tersebut. Cow existing di scene mempertahankan komponen/ID lama dan memiliki child tahap scene sendiri; edit langsung di scene untuk instance ini.
- Perubahan permanen dilakukan saat **bukan Play Mode**, lalu Save Scene / Save Prefab. Perubahan biasa selama Play tetap mengikuti perilaku Unity: dibatalkan saat Stop.
- Pergantian tahap masih diatur pertumbuhan. Untuk mengubah model tahap yang sudah disimpan, edit prefab/child yang digunakan; mengganti `modelPrefab` pada Growth Profile tidak otomatis membangun ulang child scene yang sudah dibuat.

Hewan scene yang dihapus tidak lagi dibuat ulang oleh `RestoreAll` hanya karena ada dalam save lama. Hewan hasil pembelian tetap boleh dipulihkan saat Load, karena itu data gameplay.

Kubis tetap dibuat ketika pemain menanam dan dipulihkan dari data ladang ketika Load. Keenam tahap pertumbuhannya memakai tiga prefab tanaman Toon dengan skala berbeda. `Cabbage Crop.prefab` adalah container view tanpa mesh dummy. Template Crop lama yang tidak aktif di scene tetap dipertahankan sebagai referensi/preview, dengan renderer dummy dan script prototype pertumbuhan dinonaktifkan.

## Batas tahap ini

- Controller hewan baru memakai idle; sinkronisasi animasi berjalan/makan dengan `AnimalRoutine` belum ditambahkan.
- Mode scene visual tidak memunculkan model pengganti otomatis untuk kondisi sakit. Perhitungan kesehatan/care tetap berjalan; profil kondisi lama yang diperiksa kosong.
- Ikon inventory, rumah, barn, alat, dan spesies selain sapi/ayam tidak diganti pada tahap ini.
- Alat migrasi Editor sudah dibuang setelah verifikasi; tidak ada bootstrap pemasang scene yang berjalan saat Play.

Backup scene sebelum integrasi serta empat asset data tersedia secara lokal di `Library/ToonFarmMigration/Backup`. Folder Library tidak masuk Git dan bukan backup jangka panjang.
