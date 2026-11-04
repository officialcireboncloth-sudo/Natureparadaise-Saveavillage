# Rework market stand menjadi gerobak

4 November 2026. Mengikuti referensi `Source~/MarketCart/MarketCart_Target.png`, dengan buah/sayuran dari Toon Farm Pack sesuai permintaan.

## Perubahan model

- Stand berkanopi diganti gerobak kayu: dua roda berjari-jari, ban besi, hub berlapis, poros, dua pegangan panjang dengan grip, dan penguat diagonal.
- Meja atas berupa papan terpisah dengan bevel, rak bawah terbuka, dinding samping/belakang tertutup papan, dan pengikat logam.
- Lemari kaca dua tingkat memiliki rangka kayu, rak, pembagi pintu geser dan dua gagang bulat. Kaca depan, belakang, samping dan atas memakai bidang tipis terpisah dari bagian solid.
- Tiga keranjang di atas: sayur daun/kembang kol di kiri, tomat di tengah, pir hijau di kanan. Rak bawah berisi peti kentang dan keranjang anyaman bertutup.
- Keranjang dan gerobak dibuat di Blender. Buah/sayur memakai mesh serta UV Toonpack: pisang, jeruk, delima, tomat, wortel, terong ungu, pir hijau, kentang, kembang kol, Salad dan Leek.
- Delima menggantikan buah naga yang tidak tersedia. Salad/Leek menjadi alternatif sayur daun referensi; gaya buah tetap mengikuti model Toonpack yang diminta.

## Material dan ukuran

Kayu, besi, anyaman, tali serta end grain memakai atlas modular `NP_CozySurfaces_01` yang sudah ada. Grain kayu mengikuti arah papan dan ukurannya dipotong berdasarkan ukuran fisik. Aset atlas dan Toonpack tidak diubah.

Buah menggunakan atlas asli Toonpack. Kaca memakai material transparan URP Unlit sederhana, tanpa bayangan dan tanpa penulisan depth. Tidak ada tekstur runtime baru.

Ukuran luar sekitar **2,60 m lebar × 2,13 m tinggi × 3,10 m panjang**, termasuk roda dan pegangan. Lebar meja sekitar 2,24 m. Terdapat tiga LOD: **22.897 / 13.545 / 5.441 triangle**; masing-masing memakai dua renderer solid/cutout dan satu renderer transparan. Collider menggunakan lima kotak sederhana untuk badan, roda dan pegangan.

## Penerapan ke Unity

Prefab gameplay tetap `Prefabs/World/MarketStand.prefab`, dengan visual `Prefabs/World/MarketStand/MarketStandVisual.prefab`. Identitas `village-market-stand-01`, stok, harga, jam operasi dan koneksi scene dipertahankan. Display buah bersifat dekorasi dan tidak menggantikan data stok jual.

Master edit tetap di `Source~/MarketStand/MarketStand_A.blend`, dalam bagian-bagian terpisah. Builder baru berada di `Source~/MarketCart/build_market_cart.py`. Menu Apply Atlas Market Stand yang lama diarahkan ke rework terbaru agar tidak mengembalikan kanopi lama.

`KeeperAnchor` berada di antara pegangan belakang, dan `FrontInteractionAnchor` berada di depan. Penjaga sekarang dipasang pada anchor dengan rig Humanoid dan Idle/Walk player; detail serta slot animasi berikutnya dijelaskan di `MarketKeeperAnimation.md`.

## Pemeriksaan

- **50 pemeriksaan native Unity**: skala dan orientasi, mesh, UV dalam padding atlas, tiga LOD, shader kaca, collider, GUID prefab, pengaturan gameplay, dan koneksi Map.
- **13 pemeriksaan Play Mode**: identitas save, normalisasi ukuran, titip/ambil barang, capture/restore stok, buka/tutup UI market dan collider dari arah depan.
- Pengecekan dilakukan dalam salinan uji Unity 6000.0.81f1 tanpa penulis save produksi. Hasil yang lolos disalin ke proyek utama; scene Map tidak ditulis ulang.

Preview Unity: `MarketCartReview/MarketCart_ThreeQuarter.png`, `MarketCart_Front.png`, `MarketCart_Rear.png`, `MarketCart_Top.png`, serta LOD1/LOD2. Hasil pemeriksaan tersimpan di `MarketCartReview/Audit.txt` dan `PlayAudit.txt`.
