# Interior rumah lima level

Semua layout tersimpan langsung dalam `Map/Scenes/Interiors/HouseInterior.unity`, di bawah `HouseInterior_Editable/InteriorLayout_LvN_Editable`. Furniture, collider, titik tidur/bangun, pintu, dan kamera dapat diedit dalam Hierarchy. Play Mode hanya memilih layout; tidak membangun ulang furniture.

| Level | Fasilitas |
| --- | --- |
| 1 | Kasur standar, buku save, radio ramalan cuaca, meja makan + 2 kursi, peti penyimpanan |
| 2 | Kasur standar, buku save, meja makan + 4 kursi, peti, TV, kulkas |
| 3 | Semua fasilitas level 2, sofa/kursi, kitchen set, aquarium, lemari perkakas |
| 4 | Kasur jumbo, semua fasilitas level 3, kamar anak untuk persiapan menikah |
| 5 max | Kasur jumbo, semua fasilitas level 4, TV max, kulkas max, aquarium jumbo |

Ukuran ruangan bertambah dari 10×9, 13×11, 18×13, 22×16, hingga 25×18 unit. Jalur utama dari pintu menuju meja, tempat tidur, kitchen, dan storage dipertahankan terbuka. Level 3+ memisahkan kamar tidur kiri belakang dan dapur kanan belakang. Kamar anak level 4+ berada di tengah belakang. Sayap kamar utama memanjang 1,4 unit ke belakang dan kamar anak 2,2 unit, sehingga outline rumah bertingkat; dapur memakai counter sambungan berbentuk L/U. TV dan kulkas memakai skala seragam dari prefab asli, dengan pintu kulkas menghadap ruang utama. Kamera perspective, pitch 48°, yaw 0°, FOV 40°, mengikuti komposisi cutaway referensi; setiap kamera level dapat diubah sendiri.

## Edit dan preview

1. Buka scene HouseInterior. Pilih `HouseInterior_Editable` yang memiliki HouseInteriorController.
2. Pilih Preview Lv.1–5 atau ubah Preview Level di Inspector.
3. Tekan Fokus Preview Interior untuk melihat layout. Edit furniture dan kamera lalu simpan scene (Ctrl+S).
4. `Use Preview Level In Editor Play` aktif secara default. Saat Play di Unity Editor, level preview dipakai untuk layout **dan** fasilitasnya. Jadi preview Lv2 dapat memakai kulkas meskipun save rumah masih Lv1.
5. Debug tidak muncul di luar rumah, termasuk ketika scene HouseInterior masih terbuka/aktif secara additive. Saat di dalam rumah, tombol debug Lv1–5 mengganti layout beserta feature level untuk sesi tersebut. Player dipindahkan ke titik masuk aman. Tombol Kembali ke level asli memakai progression rumah yang tersimpan.

Preview tidak menaikkan level rumah atau membeli upgrade. Override fitur berlaku hanya di dalam interior, dan tidak dipakai pada build game. Transaksi inventory/storage tetap transaksi biasa; item yang disimpan dalam sesi preview bukan item sementara. Edit pada saat Play mengikuti aturan Unity dan tidak tersimpan otomatis ke scene.

## Interaksi

- Buku save: E untuk menyimpan permainan tanpa harus tidur.
- Radio Lv1 / TV Lv2+: E membuka ramalan cuaca dan berita desa.
- Kasur: interaksi dari segala sisi berdasarkan jarak ke collider. WakeStandPoint_Editable terletak di depan footboard, di luar collider kasur.
- Kulkas: mulai House Lv2. Menyimpan makanan matang/minuman. Level kulkas 1/2/3/4 pada House Lv2/3/4/5, kapasitas 24/40/60/80 slot.
- Kitchen set: mulai House Lv3; level kitchen 1/3/4 pada House Lv3/4/5.
- Peti dan lemari perkakas: memakai penyimpanan rumah bersama, termasuk drag item. Lv1 menerima semua item; Lv2+ memisahkan makanan ke kulkas dan alat/bibit/bahan mentah ke storage. Kapasitas 24/32/40/48/64 slot. Upgrade dan save/load mempertahankan item serta metadata kualitas/ukuran ikan. Item overflow tetap dapat diambil.
- Aquarium Lv3–4: 10 ikan; aquarium jumbo Lv5: 30 ikan. Ketiganya memakai ID aquarium yang sama sehingga ikan tidak hilang ketika layout/upgradenya berubah.
- Kamar anak hanya persiapan ruang; tidak membuat sistem menikah/anak baru.

## Upgrade

Player House Building memiliki 5 level. Biaya/durasi Lv2–4 dipertahankan. Lv5 default: 4000 G, 10 hari, Village Lv4, material dua kali biaya material Lv4; editable di asset definition. Visual exterior tertinggi yang tersedia dipakai sebagai fallback untuk Lv5 sampai ada model exterior khusus.

Layout empat level sebelumnya disimpan sebagai `PreviousFourLayouts_Reference_Inactive`. Jangan mengaktifkan arsip saat gameplay. Menu Nature Paradise > House > Apply Five Reference Layouts bersifat sekali pasang dan tidak menimpa edit layout yang sudah ada.

Saat keluar rumah, HouseInteriorView mengembalikan posisi, rotasi, projection, FOV, dan follow kamera world. Cuaca interior tetap terlindungi oleh sistem yang sudah ada.
