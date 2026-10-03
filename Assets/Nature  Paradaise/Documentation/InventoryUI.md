# Inventory UI

Inventory memakai grid lima kolom yang bisa discroll, filter Semua / Alat / Tanaman / Ikan / Material, dan detail item di kanan. Kapasitas dan posisi slot save tetap milik Inventory; delapan slot pertama adalah hotbar dan diberi nomor kecil. Slot kosong tetap terlihat pada Semua. Tanaman mencakup Seed serta RefrigeratorCategory Crop/Fruit; Ikan mencakup Fish/Bait. Item kategori lain tetap tersedia pada Semua.

Buka/tutup dengan Tab, tutup dengan Escape atau tombol Tutup. Klik slot untuk memilih; slot hotbar juga mengaktifkan item. Drag menukar dua slot, termasuk tas dan hotbar. Klik kanan atau tombol Pindah memindahkan ke slot kosong pertama pada tas/hotbar tujuan. Shift atau Bagi memisahkan separuh stack ke slot kosong sambil mempertahankan kualitas serta ukuran/berat ikan. R mengurutkan kategori. Delete atau Buang membuka konfirmasi hapus seluruh stack. Key Item, Quest, dan Tool dilindungi dari Buang. Inventory mengunci gerak dan tool yang sudah dikunci oleh UI lama; jam dunia tetap berjalan seperti sebelumnya. Pause menu tidak terbuka pada frame Escape menutup inventory.

Tombol normal abu-abu, hover hijau. Filter aktif memakai garis bawah dan teks tebal; item terpilih memakai garis tepi. Harga memakai ItemSO.GetMarketSellPrice dan data kualitas/ukuran asli, bukan angka contoh pada referensi. Total memakai jumlah stack. Uang memakai ScoreManager.

## Slot image

Buka menu Unity **Nature Paradise > UI > Inventory > Select Image Slots**. Asset **Resources/UI/InventoryTheme.asset** menyediakan slot Background, Panel, Slot, Button, lima ikon kategori, Coin Icon, Trash Icon, Filled Star, dan Empty Star. Panel/slot/button bisa memakai Sprite dengan border 9-slice. Tanpa sprite, hanya panel/layout dasar dibuat dengan UI; ikon dekoratif dan bintang dibiarkan kosong. Background opsional; dunia scene tetap terlihat melalui overlay gelap.

Pada setiap **ItemSO**, isi **Icon** untuk grid/hotbar, **Inventory Illustration** untuk ilustrasi besar, dan **Inventory Description** untuk deskripsi. Ilustrasi besar boleh memakai Icon yang sama bila tersedia. Jika kedua image kosong, area image tetap kosong dan nama item tetap terlihat. Tidak ada bentuk kapak, ikan, wortel, koin, atau bintang yang digambar paksa melalui kode.
