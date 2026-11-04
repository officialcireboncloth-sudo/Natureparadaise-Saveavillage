# Jadwal gerobak pasar

Gerobak mulai terparkir bila sesi dimulai saat pasar buka. Ketika tutup, penjaga meraih pegangan, menarik mundur sebentar, kemudian mendorong gerobak keluar kamera. Jam buka **06.00 sampai sebelum 20.00**. Jika cuaca berubah kembali menjadi cuaca buka dalam sesi berjalan, gerobak dapat kembali dari luar kamera.

| Cuaca | Operasional |
| --- | --- |
| Cerah, Cerah Berawan, Mendung, Panas | Buka pada jam operasional |
| Gerimis, Hujan, Hujan Lebat, semua badai, Salju | Tutup |

Pasar baru dapat diinteraksikan setelah gerobak selesai tiba. Saat cuaca berubah menjadi hujan atau jam mencapai 20.00, panel perdagangan ditutup, seluruh kunci input/pause dilepas, dan transaksi dihentikan. Keberangkatan tidak menunggu inventory kosong.

Stok, kualitas barang, identitas stand, dan pendapatan tetap memakai sistem save MarketStand yang sama. Menyembunyikan gerobak tidak menghapus atau menonaktifkan penyimpan stok. Collider dan animasi penjaga dimatikan saat tidak terlihat, sehingga tidak ada penghalang tak kasatmata. Load memulihkan stok/pendapatan persis dari save; tidak menghitung penjualan susulan berdasarkan cuaca hari yang berbeda. Barang yang telah melewati tujuh hari baru diselesaikan pada jam pasar berikutnya saat gerobak sudah terparkir dan cuaca mengizinkan.

Perjalanan mendorong berjalan pada **1,8 meter/detik** (sebelumnya 0,75). Tiga Action asli pada `Source~/Characters/MarketKeeper/MarketKeeper.blend`: **Cart_Grip**, **Cart_Pull**, **Cart_Push**. Masing-masing 30 frame pada 30 fps; Grip sekali, Pull/Push loop. Source memakai keyframe utama Bézier, sedangkan ekspor Unity dibake. Kecepatan langkah mengikuti gerakan gerobak; 1,8 m/detik masih di bawah batas pengali animasi yang ada. Penjaga tetap memakai Idle/Walk bersama ketika terparkir. Putaran kedua roda mengikuti jarak dan arah perjalanan, bukan timer. Langkah berhenti ketika gerobak terhalang.

Ukuran gerobak telah diperkecil 25% dan dibake ke mesh: lebar 1,95 m, tinggi sekitar 1,60 m, panjang termasuk pegangan 2,32 m. Penjaga tetap 1,50 m. Pegangan lurus, berjarak 0,70 m, berada pada tinggi sekitar 0,95 m. Tidak ada pengecilan NPC atau skala negatif. Lima renderer per LOD memisahkan badan, dua roda, produce dan kaca, dengan material atlas bersama yang sama.

MarketCartSchedule menyimpan lokasi parkir asli saat Awake. Pengaturan kecepatan, arah keluar pilihan, jarak pencarian, dan ruang aman gerobak tersedia pada komponen prefab. Instance lama yang belum memiliki komponen ini tetap dipasang otomatis oleh MarketStand. Perubahan stok tetap melalui MarketStand, tidak melalui visual gerobak.

Gerobak datang dari **atas layar** dan berputar untuk kembali ke arah atas ketika pulang. Arah atas dihitung dari kamera gameplay pada bidang tanah, sehingga tidak terbalik karena rotasi prefab. Jalur lurus diutamakan; kandidat penghindaran halangan tetap berada dalam sektor atas layar (maksimum 60 derajat ke kiri/kanan). Jalur diperiksa terhadap collider solid serta kemiringan/batas terrain. Jika semua jalur terhalang bangunan, gerobak menunggu lalu mencoba lagi, tanpa menembus bangunan. Player yang menghalangi digeser ke sisi kosong terdekat melalui CharacterController, termasuk saat gerobak menarik mundur. Sisi satunya dipilih bila terhalang pagar, tembok atau aktor lain. Input gerak diberi jeda singkat selama bergeser lalu kembali normal; tidak ada teleport atau perubahan layer collision global. Jika kedua sisi tertutup atau player sedang terkunci dalam aktivitas, gerobak fade setelah 1,5 detik terhalang agar player tidak dipaksa masuk ke objek solid. Hewan/NPC tetap ditunggu. Tidak membutuhkan NavMesh tambahan. Rute keluar diuji pada lokasi stand Map asli dengan zoom 5, 8, dan 12. Bangunan baru dapat menghalangi rute; sisakan jalur di sekitar stand.

## Penataan buah dan sayur

Model display tetap memakai mesh dan UV asli Toonpack. Pisang direbahkan dalam kelompok berkipas, wortel berbaring dengan arah bervariasi, buah dibentuk menjadi tumpukan berselang dengan ukuran/kemiringan kecil yang berbeda. Isi keranjang memakai lapisan dasar dan lapisan atas; sayuran berdaun tetap keluar sedikit dari bibir keranjang. Kentang ditumpuk bertingkat di peti bawah. Semua penataan disimpan pada `Source~/MarketStand/MarketStand_A.blend` dan builder `Source~/MarketCart/build_market_cart.py`, lalu diekspor ke prefab Unity yang sama.

Atlas/material bersama, ukuran gerobak, pegangan, identitas stok dan jumlah renderer per LOD tetap sama. Geometri LOD0 sekitar 23,4 ribu segitiga; LOD1/LOD2 tetap berkurang untuk jarak kamera. Tidak ditambahkan tekstur buah/sayur baru.

Penyembunyian memeriksa seluruh ruang gerobak, pegangan, dan penjaga beserta margin di luar frustum kamera gameplay. Jika pemain terus mengejar atau zoom diperlebar, fade tetap dimulai setelah gerobak berjarak **18 meter dari stand**. Fade berjalan halus selama satu detik sambil gerobak terus bergerak bila jalurnya bebas. Dengan demikian perjalanan tutup tidak berjalan tanpa akhir karena kamera terus mengikuti. Saat Load, tampilan dipulihkan di balik layar loading yang sudah ada. Gerobak tidak diposisikan ulang secara terlihat di tengah perjalanan.

## Testing F12

Di **F12 > Gerobak Pasar**, tersedia tombol klik/sentuh:

- **Tes pergi / tutup**: meraih, menarik, mendorong keluar kamera, lalu fade-out. Dapat dipicu ketika jam pasar masih buka.
- **Tes datang dari atas layar**: memulai gerobak dari luar kamera pada sisi atas lalu memarkirkannya kembali.
- **Preview fade-out di tempat · 1 detik**: memperlihatkan fade pada gerobak dan penjaga tanpa harus mengejar keluar kamera.
- **Tampilkan ulang di posisi stand**: memulihkan posisi awal, seluruh material dan collider untuk mengulang tes.
- **Sembunyikan langsung**: mengecek kondisi gerobak yang tidak tampil.
- **Kembali ke jadwal normal**: menghapus override tes dan mengikuti jam/cuaca game kembali.

Menu otomatis menutup dan melepas pause miliknya ketika tombol tes dijalankan. Override hanya berlaku selama sesi, tidak ditulis ke save; Load membatalkannya. Jam, cuaca, stok, dan pendapatan tidak diubah oleh tombol ini. Kategori tersedia pada Editor/development build dan ketika gerobak ada di scene luar. Bila jalur tertutup, tes pergi tetap menunggu jalur kosong.

Fade normal dan preview berlangsung **1 detik**. Fade normal dimulai setelah seluruh gerobak keluar kamera atau jaraknya dari stand mencapai 18 meter, mana yang terjadi lebih dahulu. Fade memakai property block per renderer dan dither pada material opaque; kaca mengikuti alpha. Material penjaga sementara hanya dipakai selama fade dan dipulihkan/dilepas saat selesai atau dibatalkan. Material atlas bersama dan objek lain tidak dimodifikasi.

Validasi geser player dan susunan display: **26 pemeriksaan collision lulus**, **68 pemeriksaan penerapan aset lulus**, dan **69 pemeriksaan jadwal diulang dan lulus**. Termasuk memilih sisi kosong, menarik mundur, kedua sisi tertutup, kontrol pulih, NPC tetap ditunggu, UV/material, LOD, collider serta identitas prefab. Hasil: [collision](MarketDepartureReview/PlayerClearanceAudit.txt), [aset](MarketDepartureReview/NaturalProduceAudit.txt), [preview](MarketDepartureReview/NaturalProduce.png).

Validasi tambahan sebelumnya: **48 pemeriksaan debug/fade lulus**. Termasuk klik tombol F12, pelepasan pause, kondisi setengah memudar, material bersama tetap utuh, pembatalan/ulang tanpa material sementara tertinggal, stok tetap ada dan override tes dibersihkan saat Load. Hasil: [DebugAudit.txt](MarketDepartureReview/DebugAudit.txt).

Pengujian Unity 6000.0.81f1 pada proyek salinan: **69 pemeriksaan jadwal lulus**, termasuk jam batas, seluruh jenis cuaca, datang/pergi, perubahan cuaca, kamera mengikuti, pause, penghalang, transaksi, penjualan barang lama, dan capture/restore stok. **27 pemeriksaan tambahan animasi/ukuran lulus**, termasuk tiga clip Humanoid, jarak tangan ke pegangan, loop kaki, urutan reach/pull/push, putaran roda, hilang di luar kamera dan stok terisi tetap tersimpan. SaveManager tidak dibuat dalam scene pengujian sehingga save pemain tidak ditulis. Hasil: [jadwal](MarketCartScheduleAudit.txt) dan [animasi](MarketDepartureReview/Audit.txt).

Pengujian dapat diulang dengan menyalin `Source~/MarketCart/Tests/MarketScheduleAudit.cs` dan `MarketDepartureAudit.cs` ke `Assets/Editor` pada salinan proyek bernama CleanClone, lalu menjalankan `MarketScheduleAudit.Run` atau `MarketDepartureAudit.Run`. Helper ini sengaja menolak proyek utama. Untuk menerapkan ulang ekspor pada editor yang sudah berhenti Play, gunakan menu **Nature Paradise > Narrative > Apply Market Cart Departure**. Hasil yang diuji sudah dipasang pada prefab utama; menu ini hanya untuk ekspor berikutnya.
