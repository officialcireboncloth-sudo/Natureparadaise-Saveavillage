# Sprinkler

Lima level menyiram 4, 6, 12, 24, dan 60 petak. Petak pusat ditempati sprinkler dan tidak dihitung.

- Lv1: empat tetangga (depan, belakang, kiri, kanan).
- Lv2: empat tetangga plus dua diagonal depan (+Z grid).
- Lv3 / Lv4 / Lv5: pola diamond dengan jarak grid 2 / 3 / 5.

Model `mesh/Prop/Prop_Garden_Sprinkle/Sprinkle.fbx` digunakan oleh prefab editable di `Prefabs/Farming/Sprinklers`. Warna level: abu-abu, hijau, biru, ungu, emas. Ukuran model dinormalisasi dengan lebar 0.65 unit dan dasar menapak tanah. Material memakai basecolor, normal, dan metallic dari folder model.

Pilih sprinkler di hotbar untuk melihat model ghost dan petak transparan. Tekan P untuk memasang, E untuk mengambil kembali. Preview dan penyiraman menggunakan `FarmPlacement.Offsets` yang sama. Area preview terpotong di batas field; petak bangunan, path, reserved, dan petak occupied diabaikan. Petak kosong menunjukkan calon area tanam; penyiraman hanya diterapkan pada tanah yang sudah dicangkul atau berisi tanaman.

Sprinkler menyiram otomatis mulai pukul 06:00 setiap hari tanpa stamina. Sistem menghitung growth hari sebelumnya sebelum reset, lalu menyiram hari baru sesudah event cuaca selesai. Hujan menangani penyiraman field luar sendiri. Overlap sprinkler tidak menambah air berulang pada petak yang sudah disiram hari itu. Pemasangan dan pencangkulan petak dalam jangkauan juga langsung menyiram petak tersebut.

Item tersedia lewat FarmEquipmentCatalog dan toko farm equipment. Unlock village level: 1 / 2 / 3 / 4 / 4; Lv5 tetap dapat dibeli pada batas progression desa saat ini (Lv4). Harga default: 500 / 1500 / 5000 / 12000 / 25000 G. Harga dan unlock dapat diedit di `Data/Balance/Items.csv`; kolom `sprinkler_level` menerima 0–5. Gunakan menu Nature Paradise > Data CSV > Import Items and Crops setelah mengedit.

Menu Nature Paradise > Farming > Apply Five Sprinkler Levels memasang ulang model, material, dan catalog. Menu ini mempertahankan harga item yang sudah ada.
