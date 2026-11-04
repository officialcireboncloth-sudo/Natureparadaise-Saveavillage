# Lumber Store: penjaga, label, dan interaksi

Tukang menggunakan `GroundDeparture` untuk mencari lokasi berdiri yang aman. Lokasi ini dapat bergeser beberapa meter dari pivot toko karena collider di Map, sementara label dan jarak interaksi sebelumnya tetap mengacu pada pivot lama. Akibatnya pemain bisa berdiri di dekat model tanpa mendapatkan petunjuk E.

Perbaikan:

- `UpgradeShopFront.InteractionAnchor` mengikuti model tukang saat berada di toko. Jarak, pemilihan prompt, dan validasi ulang aksi PC/klik/sentuh memakai anchor yang sama.
- Label Lumber Store mengikuti posisi berdiri yang sama. Saat tukang pergi, label tetap menandai titik layanan dan menampilkan status sedang mengerjakan pesanan.
- Titik keberangkatan konstruksi menggunakan lokasi aman pemilik yang sudah ditentukan. Titik layanan tetap aktif untuk antrean pesanan saat model tukang sedang pergi.
- Identitas toko dalam save, lokasi root toko, prefab, harga, dan isi menu bangunan tidak diubah.

**31 pemeriksaan Play Mode Unity 6000.0.81f1 lulus.** Scene pengujian menyalin instance toko beserta override UI dari Map, terrain, dan snapshot collider sekitar; tidak membuat SaveManager. Pengujian mencakup empat arah pendekatan, prompt yang benar, klik/sentuh, callback PC, penolakan aksi di luar jarak/saat terkunci, pelepasan pause, keberangkatan/kepulangan tukang, identitas save, serta collider pemilik.

Hasil tersedia pada `LumberOwnerInteractionAudit.txt`. Sumber pengujian: `Source~/MarketCart/Tests/LumberOwnerInteractionAudit.cs`; salin ke `Assets/Editor` dalam proyek salinan bernama CleanClone, lalu jalankan `LumberOwnerInteractionAudit.Run`.

Setelah mengganti skrip ketika editor masih Play, Stop Play lalu Play kembali agar objek runtime pemilik dan anchor dibuat ulang.
