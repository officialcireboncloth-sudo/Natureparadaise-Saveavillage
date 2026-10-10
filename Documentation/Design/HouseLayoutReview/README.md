# Denah interior Lv3–5 · A · diterapkan 10 Oktober 2026

Status: **disetujui pengguna dan diterapkan ke HouseInterior.unity**. Posisi, ukuran tapak, dan arah hadap mengikuti manifest/sketsa A yang di-ACC dengan pesan “nah oke acc”. Bentuk sayap, pintu kamar, material, model, dan komponen interaksi tetap menggunakan aset sebelumnya.

`level-3-applied.png`, `level-4-applied.png`, dan `level-5-applied.png` adalah screenshot aktual Game View pada Play Mode, bukan gambar konsep. `applied-native-checks.txt` mencatat 86 pemeriksaan pusat bounds, ukuran, arah muka, dan posisi collider native. `applied-play-checks.txt` mencatat CharacterController berjalan melalui jalur masuk, koridor kamar, akses dapur, dan kamar anak Lv4/5; menu kasur, kitchen, TV, aquarium, storage, dan kulkas dibuka/ditutup pada ketiga level. Transisi masuk rumah dan kembali ke world juga lolos. Tidak ada transaksi inventory, tidur, save game, atau perubahan progres selama tes.

## Yang perlu dilihat

- `level-3-sketch.png`, `level-4-sketch.png`, `level-5-sketch.png`: denah tampak atas. Semua digambar pada skala 46 px per unit, sehingga rumah yang lebih besar memang memiliki gambar lebih besar.
- `levels-3-4-5-overview.png`: ketiganya berdampingan pada skala yang sama.
- Panah menunjukkan **arah hadap tampilan furnitur**, bukan sumbu impor FBX.
- Jalur hijau menandai area yang harus kosong, lebar 1,5 unit. Pintu kamar utama tetap selebar 2,6 unit; jalur di dalam kamar menuju kaki kasur.
- Warna hanya membedakan fungsi pada sketsa; material/texture furnitur tetap yang sekarang.

## Penempatan

TV dan rak ditempel sisi kiri ruang tamu, menghadap kanan ke sofa. Sofa menghadap kiri ke TV, dengan meja tamu di tengah; kursi santai menghadap meja. Kelompok ruang tamu bergeser ke belakang pada Lv4/5 mengikuti ukuran rumah, dengan jalur ke kamar tetap bebas.

Dapur memakai satu baris kompor, counter prep A (counter return lama diputar dan dipindahkan), sink, counter prep B, dan kulkas di dinding belakang kanan. Tidak ada counter yang menutup sisi island. Jarak bersih counter kompor/sink ke island 2,15 unit, dengan akses dari sisi kiri island. Island tetap 3,5×1,3 pada Lv3 dan 4,4×1,3 pada Lv4/5.

Meja makan berada di kanan depan, empat kursi menghadap pusat meja. Aquarium/rak pindah ke dinding kanan tengah menghadap kiri: tidak lagi berada di depan partisi kamar. Lv5 memakai ukuran aquarium jumbo dan TV max yang sudah ada. Lemari perkakas menghadap kiri, berada di sisi kanan kamar utama; nightstand dan buku save dipindahkan bersama. Kursi meja anak menghadap meja, bukan membelakanginya.

## Acuan 1:1 yang sudah diterapkan

`proposed-layouts.json` adalah sumber posisi yang sama dengan sketsa, bukan koordinat perkiraan dari gambar. `center` adalah pusat bounds visual dalam koordinat lokal layout; X ke kanan, Z ke belakang/atas denah. `boundsSize` memuat ukuran tapak dan tinggi. Posisi pivot FBX tidak boleh digunakan sebagai pengganti pusat bounds. `facing` adalah arah muka visual [X,Z]; arah prefab harus dikalibrasi saat apply, karena prefab tidak semuanya menghadap sumbu lokal yang sama.

Menu editor `Nature Paradise > House > Apply Approved Layout A (Lv3-5)` menerapkan manifest yang sama. `Verify Approved Layout A` memeriksa hasil native. Keduanya adalah tindakan editor eksplisit; tidak ada pemindahan furniture setiap frame saat gameplay. Props bertumpuk mengikuti pusat/tinggi surface pendukung: buku di nightstand, TV di rak, aquarium di rak, bunga di meja makan. Anchor tidur/bangun dan spawn/exit sudah diperbarui. Jangan menjalankan ulang setup lama yang mengembalikan penempatan sebelumnya.

Arah depan pack telah dicek melalui render empat sisi: front model umumnya +X, sedangkan aquarium custom -Z. Lemari, TV/rak, sofa, counter return, island, dan kursi mengikuti arah muka tersebut. Storage chest yang sebelumnya difit menyamping sudah difit kembali ke tapak yang disetujui setelah diputar. Ukuran kecil stool dan overhang bingkai aquarium juga disesuaikan dengan tapak manifest. Material dan file mesh sumber tidak diedit.

## Pemeriksaan saat review

`read_scene.py` membaca scene teks tanpa mengubahnya dan menyimpan `current-scene-snapshot.json`. `build_proposal.py` menghasilkan manifest/gambar dan `proposal-checks.txt`. Pemeriksaan geometris mencakup tapak furnitur lantai terhadap furnitur/dinding dan sampel jalur lebar 1,5 unit terhadap keduanya. Props bertumpuk dan karpet sengaja tidak dianggap penghalang tersendiri. Pemeriksaan ini **bukan Unity Play Mode**, collision gameplay dan arah mesh native belum diuji pada layout usulan.

Hash scene sebelum apply tetap tersimpan sebagai `sourceSceneSha256`. Bagian `application` menyimpan hash scene yang sudah diterapkan dan lokasi bukti validasi; status sekarang `APPLIED_VALIDATED`. Jangan menjalankan `build_proposal.py` untuk reapply: generator itu membuat usulan baru dan dapat menimpa manifest yang telah di-ACC.

Kode tes sementara diarsipkan di `validation/HouseLayoutPlayReview.cs`, di luar Assets sehingga tidak ikut build atau berjalan otomatis. Untuk mengulang tes, salin sementara ke Assets, buat `Library/HouseLayoutPlayReview.request`, buka Game View, lalu Play dengan Map aktif. Tes otomatis keluar dari Play Mode; setelah selesai hapus helper dari Assets lagi. Simpan screenshot aktual ini; menu verifikasi hanya memeriksa bounds, tidak menimpa gambar.
