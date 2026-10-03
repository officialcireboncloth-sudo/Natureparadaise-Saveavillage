# House Storage UI

Rak pada seluruh layout rumah memakai HouseStorageChest. Level 1 menerima semua item; level 2+ memakai storage untuk tools, seed, hasil panen mentah, dan material, serta kulkas untuk makanan matang/minuman. Aturan, kapasitas, migrasi, serta kolom storage_destination di Items.csv dijelaskan di HouseLevelLayouts.md. ToolStorageChest khusus alat masih tersedia untuk objek legacy; isinya dimigrasikan ke storage rumah.

Panel Unity UI memakai tas kiri, isi storage kanan, detail item dan jumlah transfer di bawah. Drag dari satu kolom ke kolom lainnya memindahkan seluruh stack. Drop di luar kolom membatalkan; tujuan penuh tidak menghilangkan item. Drag antar-slot tas menukar posisi. Isi lama yang tidak memenuhi aturan deposit baru tetap dapat diambil.

Klik slot atau WASD untuk memilih, Tab berganti kolom. E menyimpan, R mengambil; Shift + E/R memindahkan seluruh stack. Q/F menyimpan/mengambil semua item yang diterima. Tombol jumlah dipakai untuk transfer sebagian; Esc/Tutup menutup panel. Game, gerakan dan prompt ditahan selama panel terbuka; penutupan/disable melepaskan lock panel.

Gambar panel, logo, daun, peti, tas dan ikon transfer memakai Resources/UI/StorageChestTheme. Item memakai ItemSO.icon / inventoryIllustration. Gambar kosong tidak dibuat secara prosedural. Menu Nature Paradise > UI > Tool Storage > Select Image Slots memilih theme yang juga digunakan panel storage rumah dan kulkas.

Verifikasi transfer, metadata, pembatalan/stale drag, tujuan penuh serta routing EventSystem: Library/StorageFrontExitAudit.txt. Verifikasi storage bertingkat, migrasi save lama, overflow dan CSV: Library/UnifiedStorageAudit.txt.
