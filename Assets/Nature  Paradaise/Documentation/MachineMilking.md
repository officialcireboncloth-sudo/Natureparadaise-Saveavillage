# Animasi alat perah vakum

Sumber utama: `Source~/Player/Player_Farming.blend`, Action **Player | Milking Animal**. Semua 64 Action lama lainnya dipertahankan. Source memakai 10 key utama dengan Bezier Auto Clamped; FBX runtime disampling melalui exporter utama seperti animasi lainnya.

| Frame | Gerakan |
| --- | --- |
| 1 | Berdiri, membawa mesin |
| 10–22 | Turunkan badan dan jangkau cup dengan tangan kiri |
| 32 | Cup terpasang, bukaannya menghadap ke atas |
| 40–64 | Tahan tabung dan cup stabil selama vakum bekerja |
| 72 | Lepaskan cup |
| 82–91 | Berdiri, kembali membawa alat |

Model tabung, cup dan UV berasal dari Milker yang sudah ada. Cup dipisah agar bisa mengikuti tangan kiri; tabung tetap mengikuti tangan kanan. Selang menggunakan satu mesh 66 vertex yang dipakai ulang tanpa alokasi per frame. Tidak ada tekstur baru, kamera runtime tambahan, atau script per hewan untuk selang. Percikan susu terbuka dihapus dari fase vakum karena penampungnya tertutup; kilau sukses tetap tampil setelah hasil masuk inventory.

Sapi dan kambing memakai proses 3 detik. Batal/berganti alat melepas kontrol gerakan, mengembalikan cup, dan mempertahankan produk. Jenis kelamin, kondisi produksi dan jumlah hasil tetap melalui sistem gameplay yang sudah ada.

Referensi prinsip pemakaian: [Kurtsan — Mobile Milking Master](https://kurtsantarim.com/en/product-details/.html?I=179) dan [Agroelectro — User Manual](https://www.agroelectro.ro/file/user-manual-vacuum-milking-machine). Alur visual game disederhanakan untuk model satu cup; durasinya bukan durasi pemerahan sebenarnya.

Pengujian native: sampling 91 frame untuk sambungan Idle dan kontinuitas rotasi; pengujian Unity mencakup clip, orientasi alat/cup, selang, koleksi susu sapi/kambing, batal, pelepasan lock, tanpa percikan manual, serta regresi endpoint dan binding animasi kuda. Bukti dan backup di `ArtSource/MilkerMachineAnimation`.
