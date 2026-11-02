# Interior Coop cozy — level 1

Tanggal: 2 November 2026. Acuan: sisi kanan `Source~/CoopInteriorCozy/CoopInterior_Target.png`.

## Hasil

- Interior **Coop milik player**, level 1. Bukan companion coop dekorasi di samping Chicken Store.
- Rangka kayu madu tebal, bevel tipis, penyangga diagonal, papan merah, dan jendela berkusen.
- Lantai tanah hangat dengan relief lebih lembut; jerami stylized terutama di pinggir ruangan.
- **Satu bak aluminium panjang dengan enam sekat**, mengikuti kapasitas level 1. Setiap sekat memiliki renderer isi pakan terpisah.
- Mesin pakan berbentuk corong bundar, engkol, tombol, peti hasil, dan karung biji-bijian di belakang kiri.
- Penetas berkubah transparan di meja belakang kanan. Kubah membuka pada engsel belakang; sarang dan telur menggunakan titik interaksi penetas yang sudah ada.
- Tempat minum berbentuk tabung keramik dengan mangkuk air berwarna teal, di depan kanan.
- Cahaya jendela mengikuti cuaca dan jam, dengan pola empat panel; lentera tetap hangat pada malam hari.

## Data gameplay yang dipertahankan

Kapasitas level 1 tetap **6**, dengan ID serta urutan keenam slot pakan yang lama. Slot dipindah dari dua bak ke satu bak; tidak membuat data pakan baru. Slot yang kosong tidak menampilkan pakan. Pengurangan inventory dan penerimaan Pakan Unggas tetap menggunakan controller yang sudah ada.

Penetas tetap **satu telur**, durasi **3 hari**, dengan mekanisme penempatan, penetasan, mengambil anak ayam, dan mengeluarkannya yang sama. Titik sarang, posisi berdiri, titik keluar anak ayam, dan referensi EggIncubator dipertahankan. Tidak menambahkan ayam atau telur dekorasi ke New Game.

Titik hewan, titik FeedMaker, skala root, GUID prefab, scene, dan data kapasitas tidak diubah. Bentuk dekorasi dan collider disesuaikan agar tidak meninggalkan penghalang tak terlihat dari artwork lama.

## Sumber dan penerapan

- Blender editable: `Source~/CoopInteriorCozy/CoopInterior_Cozy_Lv1_Master.blend`.
- Builder: `Source~/CoopInteriorCozy/build_coop_interior_cozy.py`. Menggunakan helper versioned dari BarnInteriorCozy, PoultryCozy, dan HouseCozy.
- Ekspor dan mesh runtime: `mesh/Buildings/CoopInteriorCozy/`.
- Prefab terpasang: `Prefabs/Coop/Interior/CoopInteriorLayout_Lv1.prefab`.
- Menu pemasangan ulang: **Nature Paradise > Art > Apply Cozy Coop Interior Level 1**, ketika Play berhenti.

Kayu, tanah, logam, jerami, dan grain memakai atlas Cozy yang sudah ada. Lantai memakai satu variasi material `NP_CozyCoopEarth` dengan atlas sama, untuk tint hangat, saturasi, dan relief yang lebih lembut tanpa mengubah bangunan lain. Kubah menggunakan material transparan Coop yang sudah ada. Tidak membuat bitmap atlas baru. Cookie jendela berukuran 64×64.

## Level selanjutnya

Rework hanya diterapkan pada **level 1**. Sistem sekarang memiliki kapasitas **6 → 9 → 12 → 15**. Untuk level berikutnya, perluas bentang lantai/dinding dan tambahkan sel bak berukuran **0,5 m**, sementara ukuran mesin, penetas, tempat minum, dan ketebalan rangka tetap. Tambahkan ID baru secara stabil; jangan mengurut ulang ID slot lama atau memperbesar seluruh ruangan beserta peralatannya.

## Verifikasi

Pengujian native Unity 6000.0.81f1 dilakukan pada salinan proyek terpisah. **131 pemeriksaan lulus**. Audit ada di `CoopInteriorCozyReview/Audit.txt`.

- Koordinat, skala, segitiga, UV dan material seluruh 14 modul dibandingkan dengan Blender.
- Enam slot tetap memiliki ID lama, satu collider akses bak, dan enam isi independen.
- Stok 0–6 menampilkan tepat 0–6 isi; konsumsi menyembunyikan isi yang habis.
- Akses bak diperiksa dari depan, belakang, kiri, dan kanan.
- Jalur masuk, jalur tengah, seluruh titik ayam, posisi berdiri untuk penempatan telur, dan titik keluar anak ayam bebas collider.
- Penetas kosong secara default, telur tepat di tengah sarang, kubah dapat membuka dan kembali tepat ke posisi tertutup, serta tidak menembus tembok belakang saat terbuka.
- Pengaturan penempatan telur dan referensi EggIncubator tetap sama.
- Scene Coop asli memuat art baru dan masih menyimpan empat referensi level.
- Cuaca hujan meredupkan cahaya jendela dan malam memakai moonlight rendah.
- Prefab level 2–4, katalog, scene Coop, serta prefab dan kekuatan cahaya Barn tetap seperti sebelumnya.

Total **24.188 segitiga**, **14 renderer**, hingga **17 submesh material** ketika seluruh isi pakan dan telur review terlihat. Mobile dibatasi dua lampu lokal tanpa bayangan; belum diukur pada perangkat mobile fisik. Tidak menjalankan Play pada proyek utama dan tidak mengubah save pengguna.

Preview `Coop_Lv1_Full_IncubatingPreview.png` sengaja menampilkan pakan penuh dan satu telur untuk memeriksa bentuk. Keadaan awal sebenarnya ditunjukkan oleh `Coop_Lv1_Empty.png`.
