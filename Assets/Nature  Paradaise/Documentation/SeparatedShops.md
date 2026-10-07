# Tiga shop untuk testing

Scene Map menyimpan `SHOPS_TESTING — 01 Crops • 02 Animal • 03 Minimarket`.
Tiap child memiliki ShopFront, ShopManager, dan Shop_UI_Editable. Penjual sementara menggunakan `Seller_Capsule_Editable` (capsule) dengan label di atasnya. Dekati penjual lalu E untuk membuka. Inspector ShopFront menyediakan Preview UI In Hierarchy / Open Shop For Testing. Objek dan UI dapat dipindah atau diedit seperti UI lain di scene; simpan scene untuk mempertahankan perubahan.

1. Crops: Benih, Pupuk, Alat, Lainnya (booster dan sprinkler).
2. Animal: Ayam, Bebek, Ternak (sapi, kambing, domba), Perlengkapan. Pembelian memerlukan slot kandang sesuai jenis; hewan dikirim melalui sistem husbandry yang sama.
3. Minimarket: Makanan, Minuman, Bahan, Umpan, Jual (barang inventory yang dapat dijual).

Prefab tersimpan di `Prefabs/Shops/Testing`. Menu `Nature Paradise > Shop > Create Three Testing Shops` membuat shop jika belum tersedia; menjalankannya lagi mempertahankan edit yang sudah ada.

## Katalog dan gambar

`Resources/Shops/Crops Catalog`, `Animal Catalog`, dan `Minimarket Catalog` mengatur kategori, daftar produk, dan Price Overrides. Harga default berasal dari ItemSO. Barang yang sebelumnya memiliki buyPrice 0 mendapat harga awal katalog, minimal 100 G atau tiga kali sellPrice, untuk testing; atur harga final melalui Price Overrides. Aset ItemSO tidak diubah.

`Resources/UI/Crops Shop Theme`, `Animal Shop Theme`, dan `Minimarket Shop Theme` menyediakan slot sprite untuk background, ikon shop, portrait penjual, ikon gold, placeholder produk, header, detail, transaksi, kartu produk, kartu terpilih, tab, dan tombol Beli. Background juga dapat diatur per tab katalog, misalnya unggas dan ternak.

Ilustrasi produk menggunakan ItemSO.inventoryIllustration lalu icon. Hewan menggunakan AnimalShopOffer.icon dalam katalog Animal. Semua ImageSlot juga terlihat di Hierarchy. Gambar utama diisi lewat theme/katalog/ItemSO agar konsisten saat kategori atau produk berubah. Preview ulang setelah mengisi theme untuk melihat hasil. Gambar referensi adalah acuan komposisi; artwork background/produk tetap perlu diisi di slot yang disediakan.

## Kontrol

A/D atau panah kiri/kanan memilih barang; W/S atau panah atas/bawah mengganti kategori. Klik kartu untuk memilih. +/- mengubah jumlah (1–99), Enter membeli/menjual, Esc/E menutup. Tombol UI menyediakan kontrol yang sama. Waktu dan gerakan player terkunci selama shop terbuka. Carousel menampilkan enam barang per halaman. Total harga, sisa gold, kapasitas kandang, dan hasil transaksi diperbarui sesuai pilihan.

Storefront NPCSeller dan ShopTester lama dinonaktifkan di Map supaya tidak lagi membuka shop campuran. Layanan lama dipertahankan untuk kompatibilitas; restore hewan dan breeding secara eksplisit memilih layanan shop Animal jika tersedia.

## Presentasi shop

Shop memakai panel membulat dengan bayangan lembut, teks krem, aksen hijau sage, dan backdrop gelap. Hanya kartu produk yang terisi ditampilkan. Posisi panel disimpan pada prefab sehingga edit RectTransform di Hierarchy tetap berlaku saat UI dibuka.

Resources/UI/ShopProducts menyimpan thumbnail transparan yang dirender dari mesh game untuk produk yang tersedia. AnimalShopOffer.icon sudah terisi untuk telur dan ternak. Item menggunakan thumbnail ini hanya ketika inventoryIllustration/icon dan productPlaceholder kosong; artwork yang diisi secara manual tetap diutamakan. Slot background dan portrait penjual tetap tersedia untuk artwork interior toko.
