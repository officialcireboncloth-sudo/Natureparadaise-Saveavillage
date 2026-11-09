# Tanaman G02-G05 dan toko bibit

Dua belas tanaman sekarang memakai profil gameplay, item bibit, item hasil panen/mutasi, dan prefab yang sudah dibuat. Katalog `Crops Catalog` pada toko di Map memuat semuanya di tab **Benih**. Tata letak toko mengikuti UI yang sudah ada.

| Grup | Tanaman |
| --- | --- |
| G02 | Semangka, Melon, Zucchini |
| G03 | Timun, Ercis, Kiwi |
| G04 | Buncis, Cabai, Terong |
| G05 | Kubis, Brokoli, Kembang Kol |

Setiap profil terhubung ke Seed, Sprout, Young, Flowering/Head Forming dan Ready. Model Harvest adalah objek hasil panen untuk item/drop, bukan pengganti tanaman siap panen di field. Kubis mempertahankan enam slot dan ID lamanya untuk kompatibilitas; dua slot terakhir memakai model Ready. Ukuran model disesuaikan menjadi maksimum 85% lebar petak, dengan pivot yang menapak. Bibit menunjuk kembali ke profil crop yang sama sehingga SeedTool dapat menanam jenis bibit yang dipilih.

Benih tampil di atas soil saat aksi tanam berhasil, lalu berganti ke model kecambah mengikuti growth stage. Posisi crop mengikuti tinggi overlay tanah cangkul dengan jarak 2 mm agar model benih yang tipis tidak tertutup soil kering maupun basah; jalur yang sama dipakai saat load dan pemakaian ulang view. Selama stage Seed, LOD termurah tetap terlihat pada jarak kamera gameplay; LOD jarak seluruh field tetap berlaku.

## Ikon

G04 memakai revisi R02 dengan daun, cabang dan buah yang tersebar mengelilingi batang. Model Young/Flowering/Ready sudah diperbarui pada prefab yang sama; skala serta offset profil G04 disesuaikan kembali. Preview empat arah berada di `ArtSource/Crops/Previews/G04_R02_VolumeReview.png`.

`Textures/UI/Icons/Seeds` berisi dua belas paket kertas **256×256 RGBA**. `Textures/UI/Icons/Crops` berisi dua belas hasil panen dan dua belas varian dengan penanda mutasi. Icon dan inventoryIllustration telah diisi sehingga toko, inventory dan detail item memakai artwork yang sama.

Ikon digambar dengan seleksi, layer, bentuk dan teks GIMP. Master yang dapat diedit berada di `ArtSource/UIIcons/Seeds/Masters`; preview berada di `ArtSource/UIIcons/Seeds/SeedPackets_Preview.png`. Ukuran impor maksimum 256 pada semua platform, transparansi aktif dan mipmap UI dimatikan.

## Data yang dapat diedit

Harga bibit ada di `Data/Balance/Items.csv`; musim, waktu tumbuh, yield dan regrow ada di `Data/Balance/Crops.csv`. Gunakan import CSV yang sudah tersedia. Import balance mempertahankan referensi ikon dan model pada asset.

Data sebelumnya baru memuat Kubis. Sebelas crop baru memakai nilai awal dari Kubis: **semua musim, panen 5 hari, bibit 5 G, hasil normal 12 G**, beserta aturan perawatan/kualitasnya. Ini nilai awal untuk integrasi, belum balancing per spesies. Nilai lama Kubis, `crop.cabbage`, `item.seed`, `item.cabbage` dan GUID-nya dipertahankan. Item mutasi menunjuk hasil normal masing-masing dan mengikuti aturan harga mutasi yang sudah ada.

## Validasi

Audit integrasi memeriksa sambungan profil/item/toko, ID unik, footprint, model panen, ukuran/transparansi/importer sprite, data CSV dan perlindungan aset sebelumnya. Laporan berada di `ArtSource/Crops/Integration/IntegrationAudit.json`; kompilasi runtime dan Editor ada di `Compilation.json`.

Uji Editor/Play Mode masih tertahan layanan lisensi Unity pada sesi eksekusi ini. Setelah membuka proyek lewat Unity Hub dan menunggu import/kompilasi, jalankan **Nature Paradise > Crops > Validate Applied Crops and Seed Store** untuk pemeriksaan Resources, icon dan transaksi toko dengan inventory sementara. Menu **Validate G04 (Import + Play Mode)** juga menjalankan pemeriksaan toko itu sebelum pengujian render/LOD. Hasilnya ditulis terpisah; keberhasilan kompilasi/audit file tidak dihitung sebagai keberhasilan Play Mode.
