# Kesuburan per Field — tahap sistem

Satu instance `FieldArea` adalah satu field, dengan satu nilai kesuburan bersama (0–80). Jumlah tile tidak mengalikan nilai maupun penalti hariannya. Mesin dummy dan UI scanner sudah terhubung; pengujian aturan soil tetap tersedia lewat **F12 → Kesuburan Tanah**.

## Mesin dan UI scanner

Mesin dummy ditempatkan di sisi kiri field utama pada scene **Map** dan **Map_GameplayPreview**, dengan nama Hierarchy **Pemindai Tanah - Dummy**. Prefab: `Prefabs/Soil/SoilScanner_Dummy.prefab`. Body memakai primitive sederhana, material polos, dan satu collider fisik kecil. Ganti anak `Visual_Dummy_ReplaceLater` ketika model final siap; pertahankan komponen `SoilScannerMachine` dan `InteractionAnchor`.

Dekati mesin lalu tekan **E** atau ketuk prompt bawah. Kamera drone memenuhi layar dengan ladang asli dari sudut aerial 62 derajat, termasuk tanah, tanaman, dan objek sekitar. Header tipis, label Petak, legenda, dan panel detail kanan menggunakan kaca buram warna arang. Shader panel memakai lima sampel dari render texture ladang yang sama; tidak menambah kamera atau render pass. Kontrol keyboard digambar sebagai keycap kecil. Pada portrait, panel detail berada di bawah view ladang. Ladang dibingkai dalam area kosong agar tidak tertutup panel. Pilihan petak ditandai garis membulat hijau pucat dan tint lembut. Arah kamera sejajar dengan sumbu Field 1 agar ladang tidak terlihat diagonal; posisi, rotasi, dan data field di dunia tidak diubah. Garis serta warna transparan menandai field sesuai ID, posisi, dan ukuran sebenarnya. Klik/ketuk field untuk melihat nilai **0–80**, kondisi, serta status penyuburan dan sisa hari. Warna hijau/kuning/merah menunjukkan Bagus/Sedang/Buruk; abu-abu menunjukkan field yang sedang dikunci.

Klik **Pilih dari tas** untuk membuka pilihan Terra yang benar-benar dimiliki. Pilih item, lalu tekan **Gunakan**. Penggunaan menjalankan transaksi soil yang sama seperti debug; item hilang dari pilihan jika habis, dan tombol dinonaktifkan bila field penuh, terkunci, atau masih ditempati tanaman/perlengkapan. Membuka panel maupun memilih Terra belum mengurangi inventory.

PC mendukung mouse, WASD/panah untuk memilih field, **I** untuk pilihan Terra, **Enter** untuk menggunakan, dan **Esc** untuk kembali/menutup. Mobile menggunakan tombol sentuh, safe area, dan tata letak yang menyesuaikan landscape/portrait. Kamera tambahan hanya hidup selama scanner dibuka, memakai render texture maksimal 1536 piksel pada PC / 1024 pada mobile, tanpa HDR, MSAA, atau post-processing. View diperbarui maksimal 20 kali/detik di PC dan 12 di mobile; saat pilihan tas terbuka render dihentikan sementara. Visual tanaman pada field jauh tetap ditampilkan selama scan, lalu kembali mengikuti LOD normal setelah ditutup. Teks debug/toko tidak ikut masuk hasil kamera. Kamera gameplay tidak dipindah atau dirotasi. Modal menahan waktu, movement, prompt, dan shortcut gameplay lain; menutupnya memulihkan kondisi sebelumnya tanpa melepas pause milik sistem lain.

Menu Editor **Nature Paradise → Soil** dapat membuat ulang prefab dummy atau memasangnya pada scene aktif. Instalasi tidak menggandakan mesin yang sudah ada.

## Menyiapkan Field 1–4

Petak outdoor memakai **Cell Size = 1 unit** (sebelumnya 0,8). Field 1 dan 2 di Map tetap **15 × 15 petak**, kini berukuran **15 × 15 unit**. Semua fase tanaman mengikuti ukuran cell sehingga ikut membesar **25%**, termasuk tanaman dari save lama. Collider field, permukaan tanah, marker alat, dan jarak antartanaman memakai ukuran cell yang sama.

Kamera outdoor memakai **Outdoor Pitch = 30°** dan zoom terdekat **4,25**, dengan level lain tetap 6,5 dan 8. Pilihan zoom player tetap disimpan. Saat masuk interior, kamera kembali memakai sudut scene / marker interior dan zoom awal 5; rumah tetap memakai kamera perspektif yang sudah disusun. Dua nilai outdoor tersedia di komponen `TopDownCameraFollow` agar dapat disesuaikan tanpa mengubah framing interior.

1. Di scene Map, pilih objek yang memiliki komponen `FieldArea`. Field utama sudah menggunakan **Field Number = 1**.
2. Duplicate instance tersebut, pindahkan posisinya, lalu ubah **Field Number** menjadi 2, 3, atau 4. Hindari field saling bertumpuk.
3. Atur **Columns**, **Rows**, dan ukuran cell pada setiap instance. Scanner membaca ID yang diberikan, bukan urutan Hierarchy maupun posisi field.
4. Gunakan aset aturan bersama `Resources/Profiles/Farming/Field Soil Rules.asset` untuk tuning.

ID harus unik dan tetap setelah save dibuat. Inspector menampilkan kesalahan bila ID kembar; scanner, penggunaan Terra, dan save/load ditolak sampai diperbaiki. Field 1 mempertahankan save key lama `player-home-field`; Field 2 dan seterusnya memakai `field-2`, `field-3`, dst. Field Number = 0 mempertahankan sistem tile lama untuk area yang belum dimigrasikan, misalnya greenhouse.

Scene Map sudah memiliki Field 1 dan Field 2 sesuai dua instance yang disusun di Hierarchy. Field hasil duplikasi diberi ID 2 agar tidak berbenturan dengan Field 1. Map_GameplayPreview memiliki Field 1. Ukuran/posisi field yang ada dipertahankan; Field 3–4 bisa ditambahkan dengan duplicate sesuai kebutuhan tata letak.

## Aturan awal yang bisa diubah

| Pengaturan | Default |
| --- | --- |
| Kesuburan awal | 80 / 80 |
| Buruk | 0-19 |
| Sedang | 20-49 |
| Bagus | 50-80 |
| Kontribusi kualitas | Linear: (kesuburan / 80) x 40%, dicatat setiap hari tumbuh |
| Penurunan | 1 poin per hari jika field berisi tanaman |
| Field tanpa tanaman | Tidak berkurang; tidak pulih otomatis |

Batas kondisi dan laju penurunan merupakan tuning awal. Nilai kesuburan ditampilkan sebagai nilai absolut, misalnya **25 / 80**. Revisi Pembuat Pupuk menggunakan kualitas pasti: pupuk maks. 40%, kesuburan maks. 40%, air maks. 20%. Kontribusi tanah mengikuti (nilai/80)*40 secara linear; label kondisi tidak lagi menjadi bobot peluang acak. Bintang 5 memerlukan seluruh riwayat perawatan mencapai tepat 100% dan menjamin hasil mutasi. Kesuburan 80 saja tidak cukup, dan tanah buruk tetap bisa ditanami. Lihat `FertilizerMaker.md` untuk aturan harian dan durasi efek.

## Terra dan penguncian field

| Item | Hari game | Hasil |
| --- | --- | --- |
| Terra Natural | 30 | Kesuburan 80 |
| Terra Plus | 20 | Kesuburan 80 |
| Terra Ultra | 10 | Kesuburan 80 |
| Terra Miracle | 3 | Kesuburan 80 |

Item berada di `Resources/Items/Farming/Terra`. Harga, stok toko, serta jadwal Miracle setahun sekali belum dihubungkan pada tahap ini; item bisa diberikan lewat debug untuk pengujian.

Terra hanya bisa digunakan pada field kosong: tidak ada tanaman, bangunan, pohon tanam, atau perlengkapan tanam yang menempati grid. Kesuburan penuh, ID kembar, item tidak tersedia, dan proses yang sedang aktif ditolak tanpa mengurangi tas. Transaksi berhasil menghabiskan tepat satu item, termasuk ketika tombol ditekan berulang.

Selama penyuburan, cangkul, tanam, siram, sprinkler, pupuk tanaman, efek farming, dan penempatan bangunan/perlengkapan di field itu dikunci. Field lain tetap dapat digunakan. Berjalan melewati field tetap diperbolehkan. Setelah jumlah pergantian hari terpenuhi, kesuburan menjadi 80 dan field otomatis terbuka. Proses tidak menggunakan waktu nyata.

Penyubur tanah lama yang langsung memulihkan satu tile tidak berlaku pada field bernomor; gunakan Terra. **Crop Booster / pupuk tanaman** tetap terpisah dan bekerja pada tanaman tanpa memulihkan kesuburan seluruh field.

## Save/load

Kesuburan, item Terra yang digunakan, sisa hari, dan hari terakhir diproses tersimpan pada setiap `FieldSaveData.soil`. Memuat ulang pada hari yang sama tidak mengurangi timer lagi. Save lama tanpa data soil dimigrasikan dari rata-rata durability tile, termasuk tile kosong yang sebelumnya bernilai 80; koordinat dan tanaman tetap dipertahankan. Field baru yang belum tercatat dalam slot lama dimulai dari kondisi awal, sehingga tidak mewarisi proses dari slot yang sebelumnya dibuka.

## Pengujian langsung

1. Play scene Map, buka **F12 → Kesuburan Tanah**.
2. Pilih tombol kesuburan 10/25/60 untuk field yang ingin diuji.
3. Tambahkan satu Terra ke tas, lalu gunakan pada field tersebut.
4. Cek status **Dalam Penyuburan**, sisa hari, dan item yang berkurang satu.
5. Coba bercocok tanam pada field terkunci dan field lain.
6. Tidur atau gunakan **Waktu & Cuaca → Hari berikutnya**. Simpan dan muat ulang untuk mengecek timer.

## API scanner

- `SoilScannerService.GetFields(list)` — daftar instance bernomor, diurutkan berdasarkan ID.
- `TryScan(number, out snapshot, out reason)` — nilai, kondisi, kontribusi, dan status proses field terpilih.
- `TryUse(number, inventory, terra, out reason)` — transaksi penggunaan yang sama dengan debug.
- `FieldArea.SoilChanged` — pemberitahuan perubahan data untuk memperbarui UI.

Tidak ada polling per tile. Model final, atlas tambahan, harga/stock toko Terra, dan jadwal Miracle tahunan masih merupakan tahap berikutnya.

Audit komponen di Unity 6000.0.81f1 dilakukan pada salinan proyek terpisah. Sumber audit: `Source~/Soil/FieldSoilAudit.cs`. Mencakup ID unik, ukuran grid, pemisahan state, empat durasi Terra, penguncian, transaksi satu item, kalender, serialisasi JSON, migrasi save lama, dan kualitas panen. Audit tidak membuka atau menulis save pemain.

Audit mesin/UI: `Source~/Soil/SoilScannerAudit.cs`, dijalankan di Play Mode pada fixture terpisah. Mencakup jangkauan, prompt tunggal, modal tidak bertumpuk, pemilihan Field ID, tas kosong, pemilihan/pengurangan Terra, item yang sudah habis, ID kembar, durasi kalender, penutupan/disable mesin, pause ownership, dan render 1600×900 serta 900×1600. Revisi view drone juga memeriksa sudut aerial 62 derajat tanpa roll, penyelarasan ladang 37 derajat, perubahan piksel ketika objek dunia bergerak, kecocokan proyeksi dengan objek yang dirender, LOD tanaman jauh, resolusi terbatas, serta pembersihan kamera saat panel ditutup.
