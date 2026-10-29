# Menu testing terpadu

Buka dengan **F12** atau tombol **DEBUG** di sisi kiri layar. Tersedia di Unity Editor dan Development Build, termasuk tombol sentuh. PageUp juga membuka kategori Waktu & Cuaca. Esc/F12 atau Tutup menutup menu. Gunakan scroll / geser pada daftar aksi.

Menu menghentikan simulasi dan input gameplay selama terbuka. Tombol yang membuka toko, placement, interior preview, atau pemilih save menutup debug dahulu, lalu menyerahkan kontrol ke sistem tersebut.

| Kategori | Kontrol |
| --- | --- |
| Player | Pulihkan HP/stamina/lapar, gold, tas, tier stamina, alat yang belum dimiliki, level tools (maks. 5) |
| Item & Tools | Semua ItemSO di Resources/Items; pencarian nama/ID, filter kategori, halaman, jumlah 1/10/99, ikon item asli |
| Hewan | Pilih hewan milik player; lihat/ubah Betina atau Jantan; susu/produk siap atau belum siap; sembuhkan, dewasa, kenyang/bahagia, sakit untuk tes obat; aksi massal sembuh/dewasa; akses toko hewan; perintah companion dan kedekatan |
| Waktu & Cuaca | Jam, next day dengan reset harian normal, preview empat musim, semua jenis cuaca, kembali otomatis mengikuti kalender |
| Kebun & Kolam | Siram/pupuk sekitar player (5 × 5 petak), pertumbuhan ikan +1 hari/naik ukuran, buka kolam |
| Bangunan | Preview bangun gratis dari katalog, preview upgrade gratis, relokasi, preview level interior rumah saat berada di dalam rumah |
| Progres & Aktivitas | Level desa, bypass requirement sesi, debug clues, toko/upgrade dan panel aktivitas yang tersedia di scene, mulai quest |
| Save / Load | Pemilih slot yang sudah ada; autosave tetap terpisah |

Daftar mengikuti data dan komponen aktif yang sudah tersedia. Kolam, toko, aktivitas interior, dan preview rumah muncul dalam konteks scene yang sesuai. Sistem gameplay aslinya tetap memeriksa syarat, kapasitas dan lokasi. Tidak ada hewan contoh scene yang diaktifkan sebagai hewan gratis.

## Bekal Milker

Player mendapatkan satu Milker saat Start. Hotbar delapan slot tetap sama; alat masuk ke slot tas kosong. Save lama yang belum memiliki Milker juga mendapatkannya setelah inventory dan kedua peti dipulihkan. Item di tas, tool chest, atau house storage dihitung agar tidak duplikat. Tas penuh tidak ditimpa; pemberian tetap pending untuk pemulihan berikutnya. Flag `hasStarterMilker` disimpan bersama data save.

## Kontrol hewan

- Sembuh: menghapus sakit, recovery dan akumulasi risiko penyakit; memperbarui visual kondisi.
- Dewasa: memperbarui growth day dan umur musim sapi, tanpa memajukan kalender global.
- Gender terlihat pada daftar dan ringkasan hewan. Tombol Betina/Jantan mengganti gender hewan terpilih; mengubah gender sapi/kambing juga mengosongkan kesiapan susu sebelumnya.
- Siap diperah ON / siapkan produk: dewasa + sehat + kenyang/bahagia dan produk langsung siap, termasuk di luar kandang saat hujan untuk testing. Sapi/kambing jantan tidak menghasilkan susu; pilih Betina dahulu. Aturan produksi normal di luar debug tetap berlaku.
- Belum siap OFF: mengosongkan produk tanpa menambahkan item, lalu memulai cooldown produksi normal hari ini sehingga tidak langsung siap lagi. Status dan alasan belum siap tampil di bawah pilihan gender.
- Telur yang masih dalam penetas tidak dapat langsung didewasakan lewat tombol ini. Gunakan alur penetasan yang sebenarnya.
- Perubahan testing adalah state gameplay biasa dan akan ikut tersimpan jika player menyimpan permainan. Membuka menu saja tidak memicu save.

Kontrol musim lama dan panel kecil debug interior rumah sudah digabung di sini. Tombol debug pertumbuhan ikan di panel kolam dipindahkan ke kategori Kebun & Kolam. Shortcut testing lama tetap kompatibel.

## Percakapan Mina

Progres & Aktivitas menyediakan **Dialog · Bicara dengan Mina**, mengikuti status misi yang sebenarnya. Sebelum menerima quest, Mina dan player saling menyapa, membicarakan kebun dan rak bibit, lalu player memilih membantu atau belum sekarang. Quest baru dimulai setelah memilih membantu. Balasan memakai portrait Player/Mina dan preset Happy, Thinking, atau Neutral. Percakapan saat misi aktif, pelaporan dan setelah selesai juga mempunyai balasan player. Quest tetap 3 Wood dengan hadiah 250 G sekali; tombol ini tidak mereset progres. Untuk memeriksa enam ekspresi secara terpisah, gunakan preview portrait yang sudah ada.

## Memerah susu di Barn

Pilih Milker di hotbar. Opsi **F / G: Perah susu** tetap terlihat ketika susu belum siap, disertai alasan: belum dewasa, sapi jantan, sakit/pemulihan, belum makan, stres, cuaca, atau cooldown setelah diperah. Klik/tap aksi yang belum siap menampilkan alasan di samping tools tanpa memakai stamina atau mengambil produk. Panel detail hewan juga menampilkan status yang sama.

Untuk tes cepat: **F12 → Hewan → pilih sapi betina → Siapkan susu / telur / wol**, tutup menu, lalu F/G atau tombol sentuh. Tombol ini memperbarui status hewan sungguhan; jadwal normal tetap memerlukan dewasa, sehat, pakan dan cooldown.

Ukuran Milker dikalibrasi langsung dari mesh ke ukuran dunia **0,34 m**. Perintah Editor **Nature Paradise → Tools → Correct Milker Held Size** dapat menerapkan ulang kalibrasi tanpa mengubah mesh, ikon atau material. Setup tools juga memakai kalibrasi yang sama.

## Petak pakan Barn sesuai kapasitas

Prefab Barn memakai satu kompartemen visual dan satu feed-fill unik untuk setiap slot sistem: Lv.1 **4 (2+2)**, Lv.2 **8 (3+2+3)**, Lv.3 **14 (4+3+3+4)**, Lv.4 **20 (4+4+4+4+4)**. Urutan isi dari depan ke belakang, lalu kiri ke kanan untuk posisi depan yang sama. Posisi player hanya menentukan jangkauan, bukan tujuan pengisian. Kapasitas gameplay dan data save tetap mengikuti Barn Building; Coop menggunakan kapasitasnya sendiri.

Penyusunan ulang mengikuti kapasitas katalog lewat **Nature Paradise → Art → Match Barn Troughs to Feed Capacity**. Penerapan revisi art interior juga menjalankan langkah ini. Wadah memakai atlas kayu bersama dan feed-fill lama, tanpa tekstur baru. Mesh petak dibagikan antarlevel.
