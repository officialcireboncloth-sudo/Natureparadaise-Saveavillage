# Commerce UI

Stand market memakai UGUI dan TextMeshPro, menggantikan window IMGUI. Panel di sisi kanan memuat barang di tas, enam slot pajangan per halaman, detail barang yang dipilih, harga otomatis, jumlah, tombol Pajang/Ambil Kembali, peluang pembeli, jam buka, dan total pendapatan. Total pendapatan berasal dari lifetimeRevenue; tidak dilabeli pendapatan harian karena backend belum mencatat statistik harian.

A/D memilih barang di tas; W/S memilih listing; +/- mengubah jumlah; Enter memajang; R mengambil listing terpilih; E/Esc menutup. Klik kartu juga mengganti sumber pilihan detail. Barang, kualitas, jumlah, dan ukuran ikan tetap ditransfer melalui API MarketStand yang ada. Slot kosong, stand penuh, inventory penuh, serta tombol disabled memiliki tampilan dan penjelasan tersendiri. Waktu, pergerakan, pointer dunia, dan shortcut gameplay terkunci selama panel terbuka; cursor dikembalikan ketika ditutup.

Shop Crops/Animal/Minimarket menggunakan tipografi, penekanan harga, ritme jarak, penanda kartu aktif, stepper jumlah, status transaksi, dan tombol disabled yang diperbarui. Posisi elemen disimpan pada prefab masing-masing. Slot gambar tetap tersedia; tidak perlu memasang artwork untuk menggunakan UI.

CommerceUIStyle mengatur panel, teks, dan tombol stand. ShopTheme mengatur warna dan artwork shop. Background stand tersedia pada komponen MarketStandUI/backgroundImageSlot; komponen dibangun ketika pertama kali panel dibuka. Harga stand mengikuti harga pasar item dan kualitasnya. UI tidak menyediakan pengaturan harga manual sebelum aturan harga tersebut diimplementasikan pada backend.

Aturan visual umum semua UI ada di [UIVisualRules.md](UIVisualRules.md). Background/scrim commerce harus rectangular sampai tepi viewport; safe area hanya untuk konten.
