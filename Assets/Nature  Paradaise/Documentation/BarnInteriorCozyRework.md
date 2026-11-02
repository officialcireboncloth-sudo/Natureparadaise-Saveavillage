# Interior Barn cozy — level 1

Tanggal: 2 November 2026. Acuan: sisi kanan `Source~/BarnInteriorCozy/BarnInterior_Target.png`.

## Hasil

- Rangka kayu pucat lebih tebal, papan merah, penyangga diagonal, dan jendela berkusen.
- Lantai tanah bertekstur dengan jerami halus; jerami paling padat di tepi dan dekat penyimpanan.
- Dua bak aluminium, masing-masing **dua ruang**, sesuai kapasitas Barn level 1 sebanyak **4 pakan**.
- Bak air logam dengan permukaan biru dan riak tipis, tong, ember, peti, karung, dan tumpukan jerami.
- Mesin pencacah pakan bertudung terbuka, roda, engkol, corong, dan peti hasil. Ini artwork untuk FeedMaker yang sudah ada; proses produksi tetap dari sistem game.
- Lentera hangat dan cahaya jendela mengikuti jam serta cuaca. Bentuk empat panel cahaya menggunakan satu cookie 64×64 yang dipakai bersama.
- Semua bahan bangunan memakai atlas Cozy yang sudah ada. Tidak ada atlas baru atau perubahan warna atlas global.

## Berkas dan pemasangan

- Blender utama: `Source~/BarnInteriorCozy/BarnInterior_Cozy_Lv1_Master.blend`.
- Pembuat ulang: `Source~/BarnInteriorCozy/build_barn_interior_cozy.py`. Menggunakan helper versioned dari HouseCozy/PoultryCozy.
- Mesh dan cookie: `mesh/Buildings/BarnInteriorCozy/`.
- Prefab yang sudah dipasang: `Prefabs/Barn/Interior/BarnInteriorLayout_Lv1.prefab`.
- Menu pemasangan ulang: **Nature Paradise > Art > Apply Cozy Barn Interior Level 1**, saat Play berhenti.
- Koleksi Blender `01 EDITABLE BARN MODULES` berisi bagian satuan yang bisa diedit; `02 GAME MODULES` adalah hasil gabungan ekspor. Kamera/lampu review tidak diekspor.
- Shader Cozy Atlas mendapat dukungan varian light cookie agar pola cahaya jendela terlihat. Perilaku bahan tanpa cookie tetap sama.
- Menu penyesuaian pakan lama melewati geometri Cozy yang sudah sesuai kapasitas, sehingga tidak mengembalikan bak level 1 menjadi kayu.

## Aturan pengembangan sampai level maksimal

Katalog game sekarang mempunyai empat level interior: **4 → 8 → 14 → 20** slot. Rework visual baru hanya diterapkan pada **level 1**.

| Level | Kapasitas | Ruang lokal sistem sekarang | Prinsip rework berikutnya |
|---|---:|---|---|
| 1 | 4 | 12 × 10 m | Dua bak, masing-masing 2 ruang; sudah diterapkan |
| 2 | 8 | 16 × 12 m | Tambah bentang dinding/lantai dan ruang pakan |
| 3 | 14 | 20 × 14 m | Tambah baris/bagian bak dan jalur hewan |
| 4 | 20 | 24 × 16 m | Ruang maksimum, modul bertambah, jalur kerja tetap jelas |

Ukuran di atas adalah koordinat lokal authored layout; pengali root **1.4** yang sudah ada tetap dipertahankan. Jangan membesarkan seluruh model ketika upgrade. Ukuran sel bak **1.2 m**, ketebalan rangka, mesin, tong, dan karung tetap; yang bertambah adalah ruang serta jumlah modul. Posisi masuk tetap di tengah depan, mesin di zona belakang, jalur tengah dan akses kedua sisi bak harus bebas. Slot simpan lama dipertahankan; slot tambahan diberi ID baru secara stabil, tanpa mengurut ulang ID lama dari posisi player.

Pakan dan hewan tidak menjadi dekorasi tetap: empat isi bak default tersembunyi sampai stok benar-benar terisi. Tidak menambahkan hewan baru.

## Verifikasi

Unity 6000.0.81f1 dijalankan pada salinan uji terpisah. **88 pemeriksaan lulus** (`BarnInteriorCozyReview/Audit.txt`):

- Sembilan modul mesh sesuai koordinat Blender, UV atlas, material, dan jumlah segitiga.
- Empat slot dan empat renderer pakan tetap terpisah; ID objek slot, posisi titik hewan/mesin, skala root, dan GUID prefab tetap.
- Stok 0–4 menampilkan tepat 0–4 isi bak; penurunan stok menyembunyikan isi yang habis.
- Jangkauan pakan diuji dari depan, belakang, dan kedua sisi.
- Kapsul player/hewan bebas di titik masuk, jalur tengah, sisi bak, dan seluruh titik hewan.
- Scene Barn asli memuat artwork baru melalui prefab; empat referensi level tetap terhubung.
- Prefab level 2–4, katalog kapasitas, dan file scene tidak diubah.
- Cahaya siang meredup saat hujan lebat; malam menggunakan cahaya bulan rendah dan lampu hangat.

**31.398 segitiga**, termasuk semua isi bak, jerami, dan dekorasi; **9 renderer / 10 material submesh** ketika seluruh pakan tampil. Semuanya memakai dua bahan atlas bersama: surface dan glow. Mobile membatasi maksimal **2 lampu lokal aktif tanpa bayangan**; desktop memakai 4 lampu lokal dengan hanya dua jendela memakai bayangan 256. Belum diukur pada perangkat mobile fisik.

Preview Unity: `BarnInteriorCozyReview/Barn_Lv1_Empty.png`, `Barn_Lv1_Full.png`, `Barn_Lv1_Detail.png`, `Barn_Lv1_Night.png`. Gambar review menampilkan pakan penuh untuk pemeriksaan bentuk, bukan stok awal New Game.
