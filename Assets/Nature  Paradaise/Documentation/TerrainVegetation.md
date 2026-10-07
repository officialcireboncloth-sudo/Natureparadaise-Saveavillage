# Terrain vegetation from paint

Atur `Resources/World/Terrain Vegetation.asset`. Setiap rule memilih Terrain Layer
atau diffuse texture, lalu daftar mesh prefab beserta bobot variasi. Grass layer
yang sudah digunakan Map diisi default; layer lainnya tersedia dengan rule disabled.
`minimumPaintWeight` mencegah rumput bocor ke jalan pada batas tekstur campuran.

Rule **Sparse Bushes on Grass** menambahkan bush dekoratif dari pack pada layer
grass yang sama, dengan minimum paint weight 0.85. Density default 0.006/m²
(sekitar satu bush per 167 m²), dibagi antara TFP_Bush_01A dan TFP_Bush_02A.
Skala lebar/tinggi 0.65–1.05 dan yaw acak ditangani native Terrain detail.
Bush memakai mesh LOD1 dan material pack dengan instancing, mengikuti culling
sesuai Draw Distance profil (saat ini 50 m) serta exclusion field/bangunan yang sama. Rule bush terpisah agar mengubah
density bush tidak mengubah density grass. Native bush tidak memiliki collider.
Menu **Nature Paradise > World > Apply Sparse Terrain Bushes** memasang preset
ini; setelah mengubah profil gunakan **Apply Rules to Loaded Terrains**.

Jika `useWorldDensity` aktif, `density` adalah total rumpun per m², dibagi
berdasarkan bobot variasi. Terrain yang seluruh detailnya milik sistem ini memakai
InstanceCountMode dan pembulatan acak dengan seed untuk mempertahankan rumpun
jarang. CoverageMode milik terrain dengan manual detail lain tetap dipertahankan,
dengan kalibrasi ukuran mesh; density rendah di mode itu bisa terkuantisasi.
Nonaktifkan flag hanya untuk preset lama yang memakai angka native coverage.
Width/height scale adalah pengali mesh; native Terrain mengacak skala, yaw dan
posisi. Seed tetap membuat hasil rebuild konsisten. Cluster size/variation memberi
kelompok kepadatan dan maximum slope membatasi rumput di tebing.

Pilih Terrain lalu tekan **Rebuild From Terrain Paint**, atau tekan **Apply Rules
to Loaded Terrains** pada profil setelah mengubah density/prefab. Paint texture
dan heightmap otomatis menjadwalkan rebuild area yang berubah dengan debounce
0.2 detik. Tidak ada scanning seluruh map setiap frame.

Grass dirender sebagai native Terrain instanced detail meshes. `drawDistance`
(default 80m) dan `densityMultiplier` mengatur culling dan kualitas. Terrain
membagi detail grid menjadi patch, melakukan distance/frustum culling dan
instancing; tidak ada GameObject per helai/patch rumput. Ini culling render,
bukan streaming/unloading data Terrain. Prefab detail hasil ekstraksi memakai
LOD0 grass pack dengan material instancing/wind asli; LODGroup sumber tidak
ikut digunakan oleh detail renderer.
Menu **Apply Demo Garden Vegetation Preset** membaca source demo: terrain grass
1A dan tiling 6x6m, material terrain asli, lima variasi 01A/03A/04A/05A/06A,
bobot dari jumlah prefab, dan skala persentil 5–95% dari penempatan aslinya.
Density dihitung dari jumlah rumpun dibagi luas grass paint demo (sekitar 0.14/m²),
bukan angka coverage 5 sebelumnya. Ini kepadatan rata-rata demo, bukan menyalin
posisi satu per satu. Spring terrain tint memakai putih agar warna texture asli
menjadi dasar; musim lain tetap punya warna musimnya.
Material detail mengaktifkan `NP_TERRAIN_GRASS` untuk
melewati dither LODGroup pada shader pack; culling tetap ditangani Terrain.
Material prefab demo biasa tidak mengaktifkan keyword ini sehingga LOD fade
sumber tetap bekerja. Detail yang lebih rapat memakai lebih banyak segitiga
daripada preset LOD1 awal; distance/density masih dapat disesuaikan pada profil.

Grid detail/manual detail yang sudah ada dipertahankan; sistem hanya mengubah
slot miliknya. Terrain kosong dibuatkan grid 512 dengan patch 16. Terrain yang
sebelumnya berbagi TerrainData diberi copy per tile untuk pengecualian berdasarkan
posisi world. Backup awal TerrainData disimpan lokal di `Library/GardenTerrainBackups`.
Saat masuk Play Mode, TerrainData dicopy runtime agar paint/detail runtime tidak ikut
menulis asset asli; perubahan paint tersebut mengikuti persistence milik sistem terrain yang
memanggilnya, bukan disimpan otomatis oleh vegetation.
Indeks layer paint tetap dipakai saat sistem musim mengganti layer dengan varian
warna/salju runtime, sehingga mask grass tidak hilang ketika musim berganti.

FieldArea otomatis dikecualikan. Kotak `Grass Exclusion` di building site bisa
digeser/diubah ukurannya. Untuk area lain tambahkan `TerrainVegetationExclusion`
pada BoxCollider; posisi grass diabaikan dalam footprint XZ dan padding-nya.
Setelah menggeser exclusion, rebuild atau panggil `RefreshWorldBounds` pada
terrain yang terdampak. Native detail bersifat dekorasi, tidak memiliki collider
atau fungsi harvest rumput.

Menu setup: **Nature Paradise > World > Apply Demo Garden Lighting and Grass**.
Setup memakai layer/paint Map yang sudah ada, tidak mengganti heightmap atau
mengecat ulang terrain.
