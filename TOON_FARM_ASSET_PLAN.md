# Pilihan Toon Farm Pack untuk Nature Paradise

Audit file project: 6 September 2026. Pembaruan implementasi: seluruh Toon Series sudah dipindahkan lewat AssetDatabase ke `Assets/Nature  Paradaise/Pack/Toon Series`; 5.610 GUID aset/folder yang terindeks tetap sama. Sapi, ayam, dan kubis mulai diintegrasikan. Bagian prioritas di bawah adalah hasil audit sebelum implementasi; status terbaru dijelaskan di `TOON_FARM_IMPLEMENTATION.md`.

Folder game yang benar saat ini adalah `Assets/Nature  Paradaise` (dua spasi dan ejaan tersebut). Semua path sumber di bawah sekarang relatif terhadap `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/`. Kolom Source pada CSV sudah diperbarui ke lokasi baru.

Inventaris Toon Series berisi 2.407 prefab, 2.421 FBX, 488 controller, dan 47 material (tanpa menghitung `.meta`). Scan GUID pada `.unity`, `.prefab`, `.asset`, `.mat`, dan `.controller` di Nature Paradise menemukan nol referensi langsung ke Toon Series. Referensi Barn dan Cow yang diperiksa masih menuju `mesh/Dummy/BarnDummy.fbx` dan `mesh/Dummy/CowDummy.fbx`.

Checklist mesin tersedia di `TOON_FARM_MOVE_CANDIDATES.csv`: **38 prefab shortlist prioritas 1 + 88 dependency kandidat = 126 file**, masing-masing perlu `.meta` pasangannya. Kolom tujuan mempertahankan struktur sumber di dalam `Pack`. Semua nama shortlist ditemukan. Ini gabungan pilihan/alternatif, bukan berarti semuanya wajib dipakai. Daftar dihasilkan dari GUID rekursif file teks dan importer `.meta`; belum mencakup controller yang akan dipilih manual, pilihan prioritas 2, atau dependency yang hanya terdeteksi oleh importer Unity. Shader yang ikut terdeteksi: CustomToon, CustomToonVegetation, dan CustomWaterSimple dari folder Shared.

## Prioritas 1 — mengisi fitur yang sudah ada

| Fitur | Folder sumber | Pilihan awal |
| --- | --- | --- |
| Kubis tumbuh | `Prefabs/Vegetation/Agricultural Plants` | `TFP_Cabbage_Plant_01A.prefab`, `TFP_Cabbage_Plant_02A.prefab`, `TFP_Cabbage_Plant_03A.prefab` |
| Hasil panen kubis | `Prefabs/Props/Produce` | `TFP_Cabbage_01A.prefab` |
| Ayam dan anak ayam | `Prefabs/Animals/Chicken` | `TFP_Chicken_01A.prefab`, `TFP_Chick_01A.prefab` |
| Sapi dan anak sapi | `Prefabs/Animals/Cows` | `TFP_Cow_01A.prefab`, `TFP_Calf_01A.prefab` |
| Bebek dan anak bebek | `Prefabs/Animals/Ducks` | `TFP_Duck_01A.prefab`, `TFP_Duckling_01A.prefab` |
| Kambing dan anak kambing | `Prefabs/Animals/Goats` | `TFP_Doe_01A.prefab`, `TFP_Kid_01A.prefab`; `TFP_Goat_01A.prefab` bila perlu varian tambahan |
| Domba dan anak domba | `Prefabs/Animals/Sheep` | `TFP_Sheep_01A.prefab`, `TFP_Lamb_01A.prefab` |
| Lima alat pemain | `Prefabs/Props/Handheld Tools` | `TFP_Hoe_01A.prefab`, `TFP_Watering_Can_01A.prefab`, `TFP_Axe_01A.prefab`, `TFP_Hammer_01A.prefab`, `TFP_Sickle_01A.prefab` |
| Sprinkler | `Prefabs/Props/Farming Props` | `TFP_Sprinkler_01A.prefab` |
| Efek air sprinkler | `Particles` | `TFP_Sprinkler_01A.prefab`, `TFP_Rotating_Sprinkler_01A.prefab` sebagai pilihan efek; jangan tertukar dengan prefab alat bernama sama |
| Barn | `Prefabs/Buildings/Barn Presets` | `TFP_Barn_01A.prefab`; kandidat level berikutnya `TFP_Barn_02A.prefab` |
| Coop | `Prefabs/Buildings/Farm Structures` | `TFP_Chicken_Coop_01A.prefab`; alternatif lengkap `Prefabs/Buildings/Various Presets/TFP_Chicken_Coop_Preset_01.prefab` |
| Shed | `Prefabs/Buildings/Farm Structures` | `TFP_Shed_01A.prefab`; kandidat berikutnya `TFP_Shed_02A.prefab` |
| Rumah pemain | `Prefabs/Buildings/House Presets` | `TFP_House_01A_Preset_1.prefab`; kandidat berikutnya `TFP_House_02A_Preset_1.prefab` |
| Tidur dan ramalan cuaca | `Prefabs/Props/Furniture & Interior Props` | `TFP_Bed_01A.prefab`, `TFP_TV_01A.prefab` |
| Produk ternak | `Prefabs/Props/Food & Beverages` | `TFP_Egg_01A.prefab`, `TFP_Milk_Bottle_01A.prefab` |
| Perawatan ternak dan pupuk | `Prefabs/Props/Exterior Props`, `Prefabs/Vegetation/Hay`, `Prefabs/Props/Farming Props` | `TFP_Trough_01A.prefab`, `TFP_Hay_Bale_01A.prefab`, `TFP_Manure_01A.prefab` |

Pilihan nomor adalah shortlist untuk preview, bukan hasil penilaian ukuran/bentuk di Unity. Sesuaikan ukuran, pivot, collider, dan footprint bangunan sebelum dipasang.

### Sambungan ke gameplay

- `Resources/Profiles/Farming/Cabbage Crop.asset` memiliki enam `visualStages`, semua `modelPrefab` masih kosong. Pack menyediakan tiga kandidat model tanaman kubis; cek bentuknya sebelum membagi ke enam tahap. Tahap awal bisa memakai visual seed/sprout tersendiri atau model yang diskalakan.
- `AnimalGrowthProfileSO` mendukung Chicken, Duck, Goat, Sheep, Cow. Profil asset yang sudah tersedia adalah Chicken dan Cow; Duck, Goat, Sheep perlu profil yang sesuai. Masukkan model ke `stageSlots[].modelPrefab`, controller ke `stageSlots[].animatorController`. Bayi/dewasa saja belum mencakup seluruh tahap hidup maupun kondisi sakit.
- Ambil animasi spesies terpilih dari `Animations/Animal Animations`. Awali controller kecepatan `Regular` untuk idle/walk/eating yang tersedia. Controller contoh tidak otomatis menghubungkan animasi dengan rutinitas gameplay.
- Alat dan produk menggunakan `ItemSO.worldPrefab`. Sprite `icon` adalah slot terpisah; model 3D tidak otomatis menjadi ikon inventory.
- Bangunan menggunakan `Resources/Buildings/*.asset`, slot `levels[].completedPrefab`. Pertahankan komponen gameplay, portal, area interaksi, stable ID, serta footprint. Perubahan model tidak otomatis menambah kapasitas atau upgrade.
- Rumah interior ada di scene `Map/Scenes/Interiors/HouseInterior.unity`. Model bed/TV perlu disambungkan ke `PlayerBed` / `WeatherForecastTV` dan sistem rumah yang sudah ada.
- Sprinkler level 1–4 sudah memiliki item asset. Satu model sprinkler dapat menjadi dasar beberapa prefab variant; jangkauan tetap diatur data gameplay. Efek partikel air adalah visual tambahan.
- Prefab vendor adalah visual sumber. Buat prefab gameplay/variant terpisah untuk menambahkan atau mempertahankan komponen Nature Paradise.

## Prioritas 2 — mempercantik dunia dan toko

| Kebutuhan | Pilihan |
| --- | --- |
| Pohon bertahap | `Prefabs/Vegetation/Trees/TFP_Orchard_Tree_01A_Stage_1.prefab`, `_Stage_2`, `_Stage_3A`, `_Stage_4` beserta ekstensi `.prefab`. Gunakan sebagai kandidat visual `TreeDefinition`; nama Orchard tidak membuktikan spesies apel/jeruk/mangga/kelapa. Preview bentuk dan buahnya dahulu. |
| Pohon liar | `Prefabs/Vegetation/Trees/TFP_Beech_Tree_01A.prefab`, `TFP_Tree_Trunk_01A.prefab`. Beech bukan pengganti spesies pine/oak yang persis. |
| Batu | `Prefabs/Vegetation/Rocks/TFP_Rock_01A.prefab`, `TFP_Rock_02A.prefab`; tambahkan/pertahankan `WorldGatherable` bila dapat ditambang. |
| Rumput dan bunga | Beberapa pilihan dari `Prefabs/Vegetation/Grass`; bedakan dekorasi dengan rumput yang bisa dipanen. |
| Toko | `Prefabs/Props/Produce/TFP_Wooden_Produce_Stand_01A.prefab`; pilih peti dan produk yang memang dijual. `Prefabs/Market Presets` opsional jika membutuhkan set lengkap. |
| NPC | `Prefabs/Characters/TFP_Male_Worker_01A.prefab`, `TFP_Female_Gardener_01A.prefab` sebagai kandidat penjual/tukang. Cek rig/animasi sebelum mengganti karakter. |
| Pagar | Pilih satu gaya dari `Prefabs/Props/Fence Kits`, lalu ambil tiang, segmen, sudut, dan gerbang yang cocok. |
| Interior | Furniture terpilih dan interior preset yang sesuai rumah. Tidak perlu seluruh furniture pack. |

Data pohon game mencakup Apple, Orange, Mango, Coconut, Oak, Pine, Wild Small. Jangan menganggap semua spesies tersebut tersedia sebagai model bernama sama dalam pack. Aset PineTree sudah ada di game dan dapat dipertahankan.

## Bisa ditunda

- `Prefabs/Vehicles` (Agricultural, Regular, Broken): belum menjadi kebutuhan fitur alat/farming saat ini.
- Hewan Pigs, Horses, Dogs, Geese, Crows: di luar lima jenis `AnimalType` saat ini; bisa untuk dekorasi/fitur berikutnya.
- `Buildings/Greenhouse Kit`, `Silo Kit`: belum ada building definition khususnya di katalog yang diperiksa.
- `Props/Guns`, mayoritas `Clothing`, `Background`, variasi produce selain item yang tersedia.
- `Scenes`, lighting dan terrain data demo, `Animations/Demo Scenes Asset Animations`, `Post Processing`: referensi tampilan, tidak perlu dimasukkan ke scene gameplay.
- `Skyboxes`, terrain layers, efek kupu-kupu/lebah/daun: opsional sesuai arah visual dan sistem cuaca/day-night.
- `Documentation`: simpan sebagai referensi vendor, tidak perlu masuk Resources.
- Script demo/vendor jangan dipakai menggantikan sistem Nature Paradise. Tetap bawa jika merupakan dependency prefab pilihan, lalu tinjau kegunaannya.

## Dependensi yang wajib ikut

Memindahkan `.prefab` saja tidak mengumpulkan dependensinya. Sertakan sumber yang benar-benar dirujuk:

1. Nested prefab atau base prefab, termasuk yang ada di `Prefabs/Animals/Source` / `Characters/Source` dan building kit modular.
2. FBX/mesh dari `Models`, termasuk `Models/Colliders` bila digunakan.
3. Material dan texture yang dirujuk, walaupun satu material/atlas digunakan banyak prefab.
4. Shader yang dirujuk dari **`Assets/Toon Series/Shared`**, di luar folder Toon Farm Pack.
5. Controller, animation clips/FBX animasi, avatar, particle prefab dan texture/material efek yang dipakai.
6. Semua pasangan `.meta`; pertahankan GUID untuk menjaga referensi.

Project memakai URP 17.0.4 dan shader `CustomToon.shader` yang diperiksa memiliki dukungan `UniversalPipeline`. Tetap verifikasi shader/material dan animasi dalam scene Unity; audit file tidak membuktikan semuanya sudah tampil benar.

## Struktur folder yang disarankan

```text
Assets/Nature  Paradaise/
  Pack/
    Toon Series/
      Shared/
      Toon Farm Pack/
        Prefabs/       (pilihan vendor + nested/base dependencies)
        Models/        (mesh terpakai + colliders)
        Textures/      (material/texture terpakai; pertahankan subfolder)
        Animations/
        Particles/
        Documentation/
  Prefabs/             (prefab gameplay milik Nature Paradise)
    Animals/
    Crops/
    Buildings/
    Tools/
    WorldItems/
    NPC/
    Environment/
  Resources/           (pertahankan struktur data/runtime load yang ada)
  Map/Scenes/          (pertahankan scene game yang ada)
```

Gunakan `Pack` yang sudah ada untuk sumber vendor. Folder `Prefabs` di atas adalah usulan baru. Hindari memasukkan seluruh pack ke Resources; berbagai katalog game menggunakan `Resources.Load` dengan path tertentu, jadi jangan sekalian mengubah path data tersebut.

## Urutan pemindahan nanti

1. Preview shortlist dan tentukan gaya/varian warna. Dokumentasi vendor menyebut huruf terakhir A/B/C sebagai skema warna; bukan tahap pertumbuhan atau level upgrade.
2. Kumpulkan dependency rekursif di Unity melalui `AssetDatabase.GetDependencies(paths, true)`; sertakan controller yang dipilih manual meskipun belum terpasang pada prefab. Inventaris GUID dari audit file membantu, tetapi pemeriksaan AssetDatabase tetap diperlukan untuk dependensi importer/binary.
3. Pindahkan lewat Project window Unity atau `AssetDatabase.MoveAsset` agar `.meta` tetap ikut. Perpindahan path tidak membuat GUID baru.
4. Buat prefab gameplay/variant lalu assign pada profile/item/building definition yang sesuai. Jangan menimpa komponen gameplay dengan drag prefab vendor begitu saja.
5. Buka `TestingScene` dan `HouseInterior`, cek Console, material, collider, pertumbuhan tanaman/hewan, alat, bangunan, portal dan save/load setelah integrasi. Build Settings saat audit mengaktifkan dua scene itu; `Map` belum aktif dalam daftar build.
6. Aset yang ditunda baru dipertimbangkan untuk dibuang setelah dependency dan scene sudah diperiksa. Audit ini tidak menghapus aset apa pun.
