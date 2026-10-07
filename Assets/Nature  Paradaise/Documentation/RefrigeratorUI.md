# Refrigerator UI

UI uGUI mengikuti referensi kulkas: panel kanan, lima tab, grid tas/kulkas empat kolom, detail item dan jumlah transfer. Semua gambar berasal dari Sprite yang diisi pengguna; tidak ada potongan screenshot. Menu `Nature Paradise/UI/Refrigerator/Select Image Slots` memilih `Resources/UI/RefrigeratorTheme.asset`. Gunakan `ItemSO.icon` dan `inventoryIllustration` untuk gambar item.

Normal/selected button abu-abu; hover hijau; tab/slot dipilih memakai outline cyan. Kategori Sayuran mencakup Crop/Fruit, Protein = Fish, Produk Hewan = AnimalProduct, Masakan = CookedFood/Drink/Ingredient atau ReadyToEat. Filter tidak mengubah aturan CanStoreInRefrigerator; item ditolak tetap bisa dilihat pada Semua. Kapasitas memakai level kulkas aktual, bukan angka contoh screenshot.

Klik / WASD memilih, Tab mengganti panel, E simpan, R ambil, Shift+E/R satu stack, Q simpan semua makanan yang diterima, F ambil semua, T rapikan kulkas, Esc tutup. Drag memindahkan satu stack dan memakai indeks asli meskipun filter aktif. Tombol minus/plus mengatur jumlah transfer. Batch memakai seluruh isi, tidak dibatasi tab yang aktif.

Transfer, metadata kualitas/ukuran ikan, save, kapasitas, sort, dan KitchenIngredientService memakai API lama. Modal menghentikan waktu serta gerak pemain dan memulihkan lock saat ditutup atau kulkas dinonaktifkan.

Validasi: runtime compile lulus; tes Unity terisolasi lulus untuk filtered indices, transfer jumlah/kualitas, drag, filter kosong, penolakan material, batch transfer, sort, integrasi bahan memasak dan cleanup modal. Preview 1920×1080 diperiksa.
