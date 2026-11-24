# Terrain Paint Trees — setup dan test

## Setup

1. Buka scene Map, pilih tile Terrain yang akan dipakai.
2. Terrain > Paint Trees > Edit Trees > Add Tree: pilih prefab visual yang memiliki
   Renderer, kemudian paint dengan brush Unity. PineTree.prefab saat audit masih
   kosong (Transform saja), jadi belum bisa digunakan sebagai model.
3. Buka Nature Paradise > Trees > Setup Terrain Trees.
4. Isi Target Terrain, Wood Drop, dan opsional Copy Chop Settings dengan WorldTree
   yang sudah bekerja. Player boleh kosong: dicari dari PlayerController.
5. Klik Setup Selected Terrain. Setup membuat prefab interaktif baru dan mapping;
   tidak menimpa prefab visual, menghapus object manual, atau melakukan paint otomatis.
6. Periksa collider prefab interaktif, kemudian Save Scene.
7. Ulangi pada Terrain lain. Gunakan cap/budget sama; default 50 aktif dan 5 switches
   per frame dibagi antar-manager. Pemilihan kandidat terdekat dilakukan per tile;
   pohon aktif pada tile tetangga dipertahankan sampai keluar radius/hysteresis.

## Inspector

- Activate Distance: 15 meter.
- Deactivate Distance: 18 meter, hysteresis supaya tidak bolak-balik di tepi radius.
- Max Active Trees: 50 kandidat terdekat (bias 1m untuk mempertahankan object aktif).
- Max Switches Per Frame: 5 total aktivasi + deaktivasi + update visual regrow/load.
- Max Record Checks Per Frame: 256; pencarian memakai spatial cells dan dibagi frame.
- Max Pooled Trees: 50 inactive object untuk dipakai ulang.
- Prototype Settings / Choppable: false = dekorasi Terrain, tidak pernah interaktif.
- Remove Stump After Felling: true = roboh lalu hilang; false = tunggul memberi bonus.
- Prefab WorldTree / Can Respawn + Minimum/Maximum Respawn Days: default 4–7 hari.
  Set keduanya ke 5 untuk regrow tepat 5 daily resets setelah ditebang.

Pohon lebih jauh tetap dirender Terrain. Pohon di dalam radius tetapi melewati cap
tetap visual Terrain sampai mendapat slot. Pergantian butuh beberapa frame.
Model, material, root scale, pivot dan shader Terrain/prefab harus cocok agar transisi
tidak terlihat popping. Prototype tint/LOD/billboard Terrain tidak otomatis identik
dengan renderer GameObject. Collider hasil setup adalah perkiraan batang; sesuaikan
untuk model khusus. Jangan gunakan gameplay script lain pada prefab visual brush.

## Persistence dan keamanan asset

State manual WorldTree dan semua TerrainTreeManager digabung dalam SaveManager.
Pohon yang sedang jatuh diselesaikan sebelum snapshot agar drop ikut tersimpan.
ID memakai ID manager + nama prototype + posisi normalized, bukan hanya index array.
Jangan mengganti ID manager/nama prefab atau memindahkan titik pohon jika ingin save
lama tetap cocok. Duplikasi Terrain manager harus diberi Terrain Id berbeda.
Fallback ID lama berbasis nama/index masih diterima untuk save prototype sebelumnya.

TerrainData dan TerrainCollider memakai salinan runtime. Stop Play mengembalikan
referensi asli, sehingga scale pohon tersembunyi tidak tersimpan ke asset.
Daily reset diproses bertahap, termasuk pohon yang jauh atau tidak punya GameObject.
Waktu kalender saja tidak cukup: TimeManager harus ada dan memicu OnDay.

## Checklist Play Mode (belum otomatis dijalankan)

1. Paint >100 pohon dan aktifkan Profiler. Pastikan TotalActiveTreeCount <=50
   dan jumlah LastFrameSwitches semua manager <=5, termasuk lari atau teleport.
2. Tebang pohon dekat sampai roboh: kayu hanya keluar sekali, model/collider hilang.
3. Jauh lalu kembali: pohon masih hilang, bukan muncul kembali karena pooling.
4. Set respawn min=max=2. Lewati 2 hari di luar radius: pohon kembali di titik sama,
   scale/rotation sama, durability penuh.
5. Rusakkan tanpa menumbangkan, keluar radius lalu kembali: durability tidak reset.
6. Save/load dengan pohon manual dan dua Terrain: semua state tetap cocok.
7. Load save sebelum penebangan setelah pernah menebang: state kembali berdiri.
8. Choppable=false tetap Terrain dan tidak masuk daftar target kapak.
9. Disable/enable manager dan Stop Play: Terrain asli tetap utuh, tidak ada clone ganda.
10. Periksa Physics Debugger: collider Terrain tidak tertinggal ketika scale tree nol.
    Ukur waktu SetTreeInstance dan Instantiate pada target hardware; budget jumlah
    operasi mengurangi spike tetapi tidak menjamin frame time tanpa profiling.

Unity references:
- https://docs.unity3d.com/6000.0/Documentation/Manual/terrain-Trees.html
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TerrainData.SetTreeInstance.html
