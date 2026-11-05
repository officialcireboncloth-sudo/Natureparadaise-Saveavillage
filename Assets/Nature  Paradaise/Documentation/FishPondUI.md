# UI Kolam Ikan

Ringkasan kolam tampil di kiri ketika player berada dekat kolam: panel gelap transparan, header/footer abu-hijau, teks dan ikon terang, level, jumlah ikan, pembuat pakan, stok pakan, dan kesehatan. Warna memakai `GameplayHUDStyle.Modal`, `Card`, `TextColor`, dan `Muted` agar konsisten dengan UI game lainnya. Layout, ukuran, dan posisi panel tetap. Semua nilai dibaca dari FishPond/FeedMaker yang sama dengan panel pengelolaan. Teks debug world-space besar dimatikan, termasuk label lama pada save/scene. Petunjuk interaksi menjadi **E: Kelola Kolam**; klik/sentuh petunjuk juga membuka panel pengelolaan.

Panel ringkasan memakai safe area, berada di bawah HUD/status dan tombol F12, serta menghilang saat menjauh, masuk interior, atau membuka panel pengelolaan. Pada mobile atau layar sempit panel awalnya ringkas; ketuk header untuk membuka detail. Ikon memakai empat PNG siluet transparan **128×128** (ikan, mesin pakan, karung, hati), ditautkan lewat FishPondTheme. PNG memakai tint teks terang dari palet bersama; satu material UI menjaga matte tetap bersih saat diperkecil. Mobile memakai ASTC 4×4 tanpa mipmap/read-write. Pengujian native Unity dan preview: [BellsPondReview/Audit.txt](BellsPondReview/Audit.txt). Prompt generasi: [FishPondIconPrompts.md](../Source~/HousingPresentation/FishPondIconPrompts.md).

Kolam dibangun melalui Lumber Store atau debug F12 dengan penempatan bebas **di luar ladang**. Seluruh tapak harus berada di luar area tanam dan bebas dari bangunan, pohon, batu, atau penghalang lain. Preview yang menyentuh ladang ditolak. Ukuran tapak memakai meter dunia, bukan ukuran petak ladang. Kolam lama tetap disimpan di posisinya; perubahan aturan tidak menghapus kolam atau ikan yang sudah ada.

Interaksi kolam membuka panel uGUI di kanan sesuai referensi: ikan di tas, isi kolam, detail ikan terpilih, dan pakan. Artwork kosong dapat diisi lewat **Nature Paradise/UI/Fish Pond/Select Image Slots**, yang memilih `Resources/UI/FishPondTheme.asset`. Ikan memakai `ItemSO.icon` bila tersedia; placeholder dan dekorasi memiliki slot Sprite. Screenshot tidak dipotong menjadi aset.

- Klik ikan atau A/D untuk memilih; W/S berpindah antara pilihan tas dan kolam.
- E memasukkan satu ikan dari stack tas; R mengambil satu ikan dari kelompok kolam.
- F memasukkan satu pack Fish Feed ke tempat pakan menggunakan aturan sistem lama.
- Ambil Ikan Siap mengambil ikan sehat berukuran Large/Jumbo sampai tas penuh. Ikan yang belum besar atau sakit tetap berada di kolam.
- Obati Ikan menggunakan transaksi medicine yang sudah ada, satu ikan per tindakan.
- Tombol panah menyediakan halaman berikutnya untuk tas/kelompok kolam dengan lebih dari empat entri.
- Esc atau Tutup menutup panel. Tombol normal abu-abu dan hijau saat hover; pilihan memakai border biru muda.

Kapasitas memakai data level pada katalog Fish Pond Building melalui FishPondService; ringkasan tidak menetapkan angka kapasitas sendiri. Satu slot pakan memberi makan seluruh kolam pada reset harian, Small → Medium 30 hari dan Medium → Large 40 hari. Hari menuju Large menghitung kedua tahap jika ikan masih Small. Detail mempertahankan kualitas, ukuran, berat, progres, dan status sakit. Pengelompokan hanya menggabungkan ikan dengan metadata identik.

Kebersihan air belum mempunyai model data; header menampilkan kesehatan ikan, tanpa mengarang status Air Bersih. Pembukaan panel menghentikan waktu gameplay dan simulasi, mengunci gerak serta prompt world. Penutupan, disable kolam, atau pergantian scene melepaskan lock dan memulihkan time scale sebelumnya. Debug pertumbuhan lama tersedia pada editor/development build. Save dan simulasi harian tetap memakai service yang sama.
