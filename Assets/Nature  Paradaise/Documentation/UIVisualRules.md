# Aturan visual UI Nature Paradise

Berlaku untuk seluruh UI yang sudah ada dan penambahan berikutnya: main menu,
HUD/hotbar, inventory, storage/kulkas, shop, stand pasar, pause/quest, dialogue,
peta, memancing, interaksi hewan, lonceng kandang, kolam, feed maker, tidur/save,
dan ramalan cuaca. Asset gambar yang belum tersedia tidak boleh membuat struktur
UI terlihat belum selesai.

## Layout dan tepi layar

- Background dan scrim layar penuh berbentuk persegi, tanpa radius, border,
  shadow atau sprite panel nine-slice. Stretch ke viewport, offset nol.
- Safe area hanya membatasi konten; background tetap sampai tepi layar.
- Panel konten memiliki radius 12; padding minimum 24 dan jarak antar kelompok
  16–24 pada referensi 1920×1080. Gunakan kelipatan 8 untuk penempatan baru.
- Align judul, isi dan footer pada garis yang sama. Jangan menyisakan garis,
  potongan sudut, label di luar panel atau panel kosong sebagai placeholder.
- Simpan ruang ilustrasi sesuai proporsi aslinya. Gambar kosong disembunyikan;
  layout, nama, data, aksi dan status tetap lengkap.

## Hierarki, warna dan teks

- Modal memakai charcoal biru gelap yang cukup solid; HUD memakai kaca hijau
  lebih transparan. Token berada di `GameplayHUDStyle` dan `CommerceUIStyle`.
- Teks utama cream, teks sekunder muted, harga emas, aksi utama sage.
  Warna status/error/progress tetap bermakna; jangan meratakan semua warna.
- Judul layar 28–36, judul bagian 22–26, isi/tombol 20–24, caption 16–18.
  HUD ringkas boleh lebih kecil sesuai skala HUD. Hindari body text terlalu besar.
- Autosize turun paling jauh ke 85% ukuran rancangan. Jangan mengecilkan teks
  sampai tidak terbaca untuk menutupi masalah layout.
- Nama/label panjang memakai ellipsis; deskripsi penuh diberi ruang atau scroll.
  Jangan memotong data penting: perbesar ruang bila overflow ditemukan saat QA.
- Tidak memakai outline terang/dekorasi pada setiap panel dan teks.
  Bingkai penanda pilihan maksimum 1.5; slot biasa tanpa bingkai mencolok.

## Kontrol dan feedback

- Tombol punya normal, hover, pressed dan disabled; transisi 0.12 detik.
  Pilihan kategori/slot dibedakan lewat fill dan penanda pilihan.
- Aksi utama jelas dan lebih kuat daripada aksi sekunder. Tombol yang tidak
  dapat dijalankan harus disabled dengan alasan yang bisa dibaca.
- Tampilkan kondisi kosong, kapasitas, jumlah, harga dan hasil transaksi dari
  data sebenarnya. Jangan menampilkan angka contoh sebagai data runtime.
- Petunjuk keyboard, tombol tutup dan feedback mendapat area sendiri.
- Modal mengunci input gameplay; drag/drop, keyboard dan klik tetap berjalan.

## Penerapan dan pemeriksaan

`GameplayHUDStyle` memasang aturan typography dan state tombol pada canvas
scene yang dimuat, termasuk objek inactive. Builder runtime menggunakan helper
yang sama. Fallback panel inventory, storage, kolam, feed maker, ramalan cuaca,
lonceng, tidur dan pause memakai token modal/card bersama. Artwork theme tetap
dipertahankan. Background shop/stand dipisahkan dari safe area dan dibuat datar.

Sebelum menyebut UI selesai, cek di Play Mode: semua sudut layar, nama panjang,
deskripsi panjang, item kosong/penuh, disabled/selected, hover, pergantian tab,
drag/drop, buka/tutup serta rasio 16:9 dan 16:10. Lolos compile belum membuktikan
semua layout lolos pemeriksaan visual. Debug interior hanya muncul di interior.
