# Inventory UI

Inventory memakai grid lima kolom yang bisa discroll, filter Semua / Alat / Tanaman / Ikan / Material, dan detail item di kanan. Kapasitas dan posisi slot save tetap milik Inventory; delapan slot pertama adalah hotbar dan diberi nomor kecil. Slot kosong tetap terlihat pada Semua. Tanaman mencakup Seed serta RefrigeratorCategory Crop/Fruit; Ikan mencakup Fish/Bait. Item kategori lain tetap tersedia pada Semua.

Buka/tutup dengan Tab, tutup dengan Escape atau tombol Tutup. Klik slot untuk memilih; slot hotbar juga mengaktifkan item. Drag menukar dua slot, termasuk tas dan hotbar. Klik kanan atau tombol Pindah memindahkan ke slot kosong pertama pada tas/hotbar tujuan. Shift atau Bagi memisahkan separuh stack ke slot kosong sambil mempertahankan kualitas serta ukuran/berat ikan. R mengurutkan kategori. Delete atau Buang membuka konfirmasi hapus seluruh stack. Key Item, Quest, dan Tool dilindungi dari Buang. Inventory mengunci gerak dan tool yang sudah dikunci oleh UI lama; jam dunia tetap berjalan seperti sebelumnya. Pause menu tidak terbuka pada frame Escape menutup inventory.

Tombol normal abu-abu, hover hijau. Filter aktif memakai garis bawah dan teks tebal; item terpilih memakai garis tepi. Harga memakai ItemSO.GetMarketSellPrice dan data kualitas/ukuran asli, bukan angka contoh pada referensi. Total memakai jumlah stack. Uang memakai ScoreManager.

## Tangan Kosong

Hotbar menyediakan tombol **Tangan Kosong** di samping delapan slot, dengan shortcut PC **0**. Klik/tap slot aktif atau tekan kembali angka slotnya untuk melepas item; pilih kembali slot untuk memakainya lagi. Klik slot hotbar di dalam inventory memakai toggle yang sama. Status aktif pindah ke tombol Tangan Kosong, tanpa mengambil satu slot inventory atau mengubah item, jumlah, kualitas, dan posisi stack.

Pilihan kosong memakai indeks -1 dan tool None sehingga refresh HUD, perubahan inventory, dan pembaruan pengaturan UI tidak memasang kembali alat. API SelectSlot/SelectTool tetap idempotent untuk pemindahan item dan startup; input pengguna memakai ToggleSlot/ToggleTool. Tangan Kosong membatalkan kontak alat yang belum terjadi, menghentikan aliran penyiram, mengakhiri pose kerja, dan melepas lock milik aksi tersebut. Lock dari sistem lain tetap berlaku. Animator kembali ke locomotion biasa setelah aksi dibatalkan. Kontak yang sudah berhasil tetap tersimpan.

## Slot image

Buka menu Unity **Nature Paradise > UI > Inventory > Select Image Slots**. Asset **Resources/UI/InventoryTheme.asset** menyediakan slot Background, Panel, Slot, Button, lima ikon kategori, Coin Icon, Trash Icon, Filled Star, dan Empty Star. Panel/slot/button bisa memakai Sprite dengan border 9-slice. Tanpa sprite, hanya panel/layout dasar dibuat dengan UI; ikon dekoratif dan bintang dibiarkan kosong. Background opsional; dunia scene tetap terlihat melalui overlay gelap.

Pada setiap **ItemSO**, isi **Icon** untuk grid/hotbar, **Inventory Illustration** untuk ilustrasi besar, dan **Inventory Description** untuk deskripsi. Ilustrasi besar boleh memakai Icon yang sama bila tersedia. Jika kedua image kosong, area image tetap kosong dan nama item tetap terlihat. Tidak ada bentuk kapak, ikan, wortel, koin, atau bintang yang digambar paksa melalui kode.

## Collected material notifications

Collection feedback now uses the shared bottom information dock described in BottomInformation.md. Item icons and xN share the same fixed area as tool/action hints. Up to three visible notifications last 2.5 seconds and fade; the rest enter a FIFO queue. Same-item receipts merge while visible or queued. The old top-center collection toast and selected-slot info card are hidden/replaced.

An item is reported only after a successful inventory transfer. Hammer rewards, loose wood pickup and storing a carried item retain PlayerPickupNotification.ShowItem. Failed/full-bag transfers do not report collection. Wood and Stone use shared 128px transparent PNG artwork from Textures/UI/Icons/Materials.