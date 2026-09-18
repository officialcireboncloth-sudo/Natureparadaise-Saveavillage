# Bug Fix Report

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
