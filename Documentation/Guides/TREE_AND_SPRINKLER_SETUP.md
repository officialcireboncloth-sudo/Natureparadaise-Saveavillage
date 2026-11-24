# Tree Growth + Sprinkler

## Coba di game

- Shop → **Equipment** → beli Sprinkler. Pilih di hotbar, arahkan player ke tile farm kosong/cangkul, tekan **P**. Preview hijau berarti valid, merah berarti terhalang/tile terisi/di luar farm. Garis menunjukkan tile yang terjangkau.
- **E** dekat sprinkler mengambilnya kembali jika inventory cukup. Pilih kembali lalu **P** untuk relocate. **G** hanya menjatuhkan item; sprinkler yang masih berupa drop tidak menyiram.
- Hanya satu unit dipasang per aksi, termasuk saat Shift ditahan. Tile pusat tidak dapat dicangkul, ditanami, atau dipakai bangunan.
- Lv.1: 4 arah mata angin; Lv.2: 8 tetangga; Lv.3: 24 tile; Lv.4: 48 tile. Coverage di tepi field dipotong oleh batas field; tidak menyeberang ke FieldArea lain.
- Penyiraman dilakukan saat dipasang, pagi setelah event cuaca, dan dicek sebelum crop growth. Tanah baru dicangkul dalam coverage juga mendapat air. Hujan memakai sumber air Rain, bukan sprinkler. Tidak ada biaya stamina atau resep crafting sprinkler.
- Shop → **Seeds** juga menyediakan tujuh bibit pohon. Pilih bibit pohon lalu **P** pada tile farm kosong/cangkul. Watering Can **F/klik kiri** atau shortcut watering yang sudah dikonfigurasi menyiram pohon pada tile target. Sprinkler dan hujan juga dihitung.
- Dekati pohon buah siap panen lalu **E**. Inventory penuh tidak menghabiskan buah. Buah dapat dijual melalui tab Sell.

## Konfigurasi Inspector

`Assets/Nature  Paradaise/Resources/FarmEquipmentCatalog.asset` menghubungkan empat sprinkler dan tujuh bibit pohon. `ShopManager.sellsFarmEquipment` menentukan apakah toko menyediakan katalog ini; daftar barang dapat disesuaikan per shop.

Harga awal sprinkler: 500 / 1.500 / 5.000 / 12.000 G. Village Level minimum: 1 / 2 / 3 / 4. Ini balancing sementara, ubah `buyPrice` dan `requiredVillageLevel` pada ItemSO. Unlock divalidasi kembali saat transaksi, bukan hanya tampilan tombol.

TreeDefinition berada di `Resources/Trees`:

| Jenis | Growth days | Musim buah awal |
| --- | ---: | --- |
| Wild Small | 28 | — |
| Pine | 42 | — |
| Oak | 56 | — |
| Orange | 56 | Winter |
| Apple | 56 | Autumn |
| Mango | 84 | Summer |
| Coconut | 84 | Spring + Summer |

Musim buah, harga bibit/buah, jumlah buah, dan waktu flowering/fruit growth adalah nilai awal yang bisa diubah. Default buah: flowering 2 hari + fruit growth 3 hari, hasil 1; bukan balancing final.

Setiap TreeDefinition menyediakan `matureDays`, kebutuhan air, toleransi kering, modifier musim, risiko damage cuaca ekstrem, item kayu/buah, serta array **Stages**. Setiap stage berisi **Model**, **Scale**, dan **From Growth Day**. Model kosong memakai visual dasar dengan skala dummy. Pohon yang baru dibeli memakai dummy kotak sampai artist mengisi model. Slot Model sebaiknya berisi prefab visual saja, tanpa script gameplay.

Apple default: Seed 0, Sprout 4, Sapling 8, Young 22, Growing 42, Mature 56. Saat mengubah matureDays, sesuaikan threshold tahap menengah; tahap Mature mengikuti matureDays.

Untuk pohon yang sudah ada, isi **WorldTree → Definition** sesuai jenisnya. Jika kosong, fallback Wild Small dipakai. Pohon scene lama tetap mulai dewasa. Pohon baru dari bibit mulai growth 0. Satu hari lupa air masih mendapat grace; hari kering beruntun memberi 0,25 growth/day. Air memulihkan health bertahap setelah kerusakan badai. Pohon liar mendapat kebutuhan air natural, sehingga tidak perlu disiram manual di hutan.

## Terrain brush dan regrowth

Manager Terrain yang sudah dikonfigurasi tetap memakai record ringan dan pooling. Isi Definition pada **interactive prefab** di mapping prototype. Growth/save berjalan untuk record jauh maupun dekat. Mesh tahap custom tampil saat pohon diaktifkan dekat player; **Terrain jauh memakai mesh prototype asli dengan skala tahap**, bukan berganti model prototype. Pergantian visibilitas Terrain tetap memakai budget switch yang ada.

Setelah pohon liar habis ditebang, jadwal respawn lama 4–7 hari menjadi waktu tunggu munculnya bibit. Sesudah itu pertumbuhan dimulai dari growth 0, bukan langsung dewasa. Tunggul dapat ditebang lagi untuk kayu tambahan. Untuk mempertahankan tunggul pada pohon Terrain, matikan **Remove Stump After Felling** pada mapping manager. Pohon buah yang ditanam tidak otomatis respawn setelah habis ditebang.

Fitur ini memakai titik pohon yang sudah terdaftar; belum menambah sistem menyebarkan pohon liar ke titik acak baru. Setup Terrain brush tetap mengikuti `TREE_STREAMING_SETUP.md`; tidak ada asset Terrain/prefab pengguna yang dipindah atau dihapus otomatis.

## Save dan debug

- Save menyimpan item sprinkler, posisi, tipe, serta status tree (chop, respawn, planted day, age, growth, water day, health, fruit progress).
- Stage/mature/season diperoleh dari growth + TreeDefinition, sehingga perubahan balancing diterapkan juga ke save.
- Save pohon lama tanpa progress dimigrasikan sebagai pohon dewasa; pohon yang sudah depleted tetap mengikuti jadwal respawn.
- Clue progress pohon mengikuti toggle debug HUD. Prompt interaksi panen/pickup tetap merupakan UI gameplay.

## Verifikasi

Build solusi melalui MSBuild berhasil tanpa error C#. `Tests/TreeSprinklerTests.cs` menguji source TreeDefinition/TreeProgress/FarmPlacement dengan stub Unity: pola 4/8/24/48, occupancy/pickup, hujan, growth berbagai durasi, drought, season, dan snapshot. Ini **bukan Play Mode**.

Uji manual Unity yang masih perlu dijalankan: beli → place → tidur → cek wet soil; pickup saat tas penuh; save/load sprinkler dan pohon; stage custom; panen buah; tebang/tunggul/regrowth; streaming Terrain dekat/jauh. Tampilan, physics, dan frame-time belum dapat dinyatakan terverifikasi dari build C# saja.
