# Animal detail UI

Kartu kiri atas hanya muncul saat **membuka detail** dengan I di dekat hewan, atau membuka informasi kandang dari interior. Mendekati hewan, menggosok, dan memerah susu tidak otomatis membuka kartu. AnimalCarePanel sekarang hanya menangani state serta aksi gameplay; seluruh tampilan GUILayout/OnGUI debug lama dihapus dan diganti UI Unity pada AnimalInteractionHUD. Label dummy hunger/produk pada AnimalController juga disembunyikan, termasuk saat master debug aktif. Prompt dekat hewan hanya menampilkan nama dan kontrol singkat.

Bagian atas panel berisi portrait, nama, umur tahun/musim, heart, kelamin, serta ukuran susu/status. Bagian bawah panel kiri bisa discroll dan memuat kesehatan, kenyang, mood, pertumbuhan, status perawatan harian, produk, nilai hewan, kandang, rename, beri makan/treat/obat, gosok, interaksi sapi, koleksi produk, breeding/inkubasi, serta pemindahan kandang. Detail kandang memuat kapasitas, reservasi, stok pakan, kebutuhan/stok harian, Auto Feeder, bell state, dan daftar penghuni yang dapat dipilih. Semua informasi memakai data game yang sudah ada.

Detail mengunci gerak dan menjeda TimeManager seperti panel lama. Tutup dengan Esc atau Tutup Detail. Tab inventory diblokir selama detail terbuka. Memulai gosok, memerah, atau cukur dari detail menutup panel dan melepas pause milik detail, lalu progress aksi tampil sendiri tanpa kartu kiri. E/Esc membatalkan aksi; input penutupan dikonsumsi agar tidak sekaligus membuka menu lain. Perawatan/produk baru dihitung setelah selesai. Membatalkan atau mematikan komponen melepas lock dan mempertahankan produk yang belum diambil.

## Slot image

Gunakan **Nature Paradise > UI > Animal Interaction > Select Image Slots** untuk **Resources/UI/AnimalInteractionTheme.asset**. Panel, portrait frame/default/tiap tipe, ikon umur/heart/kelamin/susu/status, Full/Half/Empty Heart, Sun/Coin/Brush/Shears/Product Icon, dan Brush Reaction Bubble tetap slot Sprite opsional. Image yang kosong tetap kosong; layout, teks, garis, tombol dan progress memakai UI Unity. Lima heart mewakili data 0–1000 poin; jika gambar belum tersedia, nilai tampil sebagai angka / 5. Tidak ada perubahan skala heart gameplay.

Kontrol aksi langsung tetap R untuk gosok, G untuk mengambil produk, F untuk item care yang dipegang, dan T untuk interaksi sapi. Milking Action Duration default 3 detik pada AnimalController. Durasi gosok/cukur mengikuti komponen. Pause menghentikan progress. Kalender menampilkan nomor hari; tidak ada hari pekan buatan.
