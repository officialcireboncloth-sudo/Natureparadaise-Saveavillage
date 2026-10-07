# UI Kolam Ikan

Interaksi kolam membuka panel uGUI di kanan sesuai referensi: ikan di tas, isi kolam, detail ikan terpilih, dan pakan. Artwork kosong dapat diisi lewat **Nature Paradise/UI/Fish Pond/Select Image Slots**, yang memilih `Resources/UI/FishPondTheme.asset`. Ikan memakai `ItemSO.icon` bila tersedia; placeholder dan dekorasi memiliki slot Sprite. Screenshot tidak dipotong menjadi aset.

- Klik ikan atau A/D untuk memilih; W/S berpindah antara pilihan tas dan kolam.
- E memasukkan satu ikan dari stack tas; R mengambil satu ikan dari kelompok kolam.
- F memasukkan satu pack Fish Feed ke tempat pakan menggunakan aturan sistem lama.
- Ambil Ikan Siap mengambil ikan sehat berukuran Large/Jumbo sampai tas penuh. Ikan yang belum besar atau sakit tetap berada di kolam.
- Obati Ikan menggunakan transaksi medicine yang sudah ada, satu ikan per tindakan.
- Tombol panah menyediakan halaman berikutnya untuk tas/kelompok kolam dengan lebih dari empat entri.
- Esc atau Tutup menutup panel. Tombol normal abu-abu dan hijau saat hover; pilihan memakai border biru muda.

Kapasitas dan pertumbuhan memakai data FishPondService: level 1/2/3/4 memiliki kapasitas 99/300/999/999, satu pack pakan memberi makan seluruh kolam pada reset harian, Small → Medium 30 hari dan Medium → Large 40 hari. Hari menuju Large menghitung kedua tahap jika ikan masih Small. Detail mempertahankan kualitas, ukuran, berat, progres, dan status sakit. Pengelompokan hanya menggabungkan ikan dengan metadata identik.

Kebersihan air belum mempunyai model data; header menampilkan kesehatan ikan, tanpa mengarang status Air Bersih. Pembukaan panel menghentikan waktu gameplay dan simulasi, mengunci gerak serta prompt world. Penutupan, disable kolam, atau pergantian scene melepaskan lock dan memulihkan time scale sebelumnya. Debug pertumbuhan lama tersedia pada editor/development build. Save dan simulasi harian tetap memakai service yang sama.
