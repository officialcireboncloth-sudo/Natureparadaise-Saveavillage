# Bug Fix Report

## Pohon tetap berdiri setelah rubuh — 12 Oktober 2026

Visual pohon Terrain tetap berdiri tanpa interaksi setelah pohon interaktif rubuh.
`TerrainLayerVegetation` mengganti TerrainData yang ditampilkan dengan salinan sendiri,
sedangkan `TerrainTreeManager` menyembunyikan pohon pada salinan milik host. Reproduksi
Play Mode membuktikan data berbeda dan visual Terrain masih memiliki scale setelah
pohon berstatus `Depleted`.

Vegetasi kini memakai `TerrainRuntimeDataHost` yang sama dengan pohon dan rumput liar.
Ketiga konsumen memastikan host tersedia pada Terrain target, termasuk ketika host
belum dipasang. Vegetasi tidak lagi menghapus atau mengembalikan data saat dinonaktifkan.

Tes Play Mode terisolasi memakai prefab Oak dan jalur `Chop`/jatuh/drop asli: tiga
urutan inisialisasi lulus, tidak ada visual duplikat, kayu hanya muncul sekali,
capture/restore tidak menghidupkan pohon atau mengulang reward, disable/re-enable
vegetasi aman, jadwal regrow bekerja, serta TerrainData sumber tidak berubah.
Map dan save pemain tidak dipakai dalam tes. Bukti sebelum/sesudah ada di
`Documentation/VisualReviews/Tree-Fall-2026-10-12-before.txt` dan `...-after.txt`.

## Market Stand

### Bug

Barang yang dipajang di Market Stand tidak terbeli secara otomatis sehingga dapat tertahan tanpa batas waktu.

### Perbaikan

- Market Stand menjalankan pemeriksaan calon pembeli secara acak pada jam operasional.
- Barang tidak langsung terjual pada hari pemasangan dan baru masuk proses random buying setelah tersimpan minimal satu hari.
- Sebagian atau seluruh stack dapat terjual saat pemeriksaan pembeli berhasil.
- Setiap listing menyimpan waktu pemasangannya melalui Save/Load.
- Seluruh listing yang mencapai usia tujuh hari dijamin terjual pada tick yang sama.
- UI menampilkan estimasi batas hari penjualan untuk setiap listing.

### Hasil

Barang dapat terjual secara acak dalam rentang satu sampai tujuh hari dan tidak dapat tertahan lebih dari tujuh hari. Faktor probabilitas berdasarkan perkembangan atau popularitas desa disiapkan untuk progres berikutnya.
