# Minimarket — sketch A

Status: ACC 14 Oktober 2026. Pengguna mengotorisasi eksekusi Blender dengan geometri ringan untuk HP. Model dan FBX tersedia di `ArtSource/MiniMarket_A`; integrasi Unity tahap berikutnya. Referensi pengguna disimpan sebagai `reference.png`.

Bangunan satu lantai dengan atap datar, parapet hijau zaitun, crest lengkung di tengah fasad, papan kayu horizontal, pilar krem dengan kaki batu, kanopi panel hijau, pintu kaca ganda dan jendela etalase. Tulisan mengikuti referensi: **SUPERMARKET**, dengan emblem tiga daun. Satu unit HVAC sederhana di atap. Sisi belakang dan kiri yang tersembunyi merupakan usulan rekonstruksi.

Lingkup exterior: struktur, parapet/coping, roof deck, kanopi dan bracket, bukaan, pintu terpisah dengan engsel, lis, kaki batu, lampu permanen, papan nama, logo dan unit HVAC. Tanaman, planter, bunga, pohon, terrain, paving/jalan, peti buah, dagangan, rak dan interior lengkap tidak termasuk. Kaca diusulkan amber opaque seperti asset sebelumnya; detail dagangan yang terlihat melalui kaca pada referensi tidak dimodelkan pada lingkup exterior.

## Ukuran usulan untuk Blender

| Bagian | Ukuran |
|---|---|
| Badan W × D | 10,00 × 7,00 m |
| Fondasi / ambang pintu | 0,25 / 0,15 m |
| Elevasi roof deck | 3,95 m |
| Tinggi coping parapet | 4,30 m |
| Puncak crest lengkung tengah | 4,85 m |
| Parapet di atas roof deck | 0,35 m, tebal sekitar 0,18 m |
| Pintu ganda total W × H | 1,80 × 2,40 m, dasar Z=0,15 m |
| Jendela depan kiri dan kanan | masing-masing 2,30 × 1,80 m, dasar Z=0,55 m |
| Kanopi utama tengah | lebar 5,40 m, proyeksi depan 1,10 m |
| Dua kanopi kecil ujung depan | masing-masing lebar 1,80 m, proyeksi 1,10 m |
| Elevasi belakang / tepi depan kanopi | 2,95 / 2,65 m |
| Kanopi sisi kanan | panjang 5,20 m, proyeksi samping 0,90 m |
| Bank jendela kanan | panjang total 3,80 m × tinggi 1,80 m |
| Papan nama kayu utama | sekitar 5,40 × 0,90 m, crest daun di atas |
| HVAC atap | sekitar 1,10 × 0,85 × 0,70 m, puncak Z=4,65 m |
| Pintu servis belakang | 0,90 × 2,05 m, dasar Z=0,15 m |
| Ventilasi tinggi belakang | 0,80 × 0,40 m |
| Shutter sisi kiri | 1,00 × 1,00 m, dasar sekitar Z=1,10 m |

Semua angka merupakan usulan proporsi game, bukan ukuran fisik yang diambil dari gambar. Dokumen ini menjadi acuan ukuran jika label dalam PNG generatif berbeda. Envelope dengan kanopi depan dan kanan sekitar **10,90 × 8,10 m**, sebelum tambahan tebal lis/coping/lampu. Tinggi keseluruhan 4,85 m. Usulan ini sedikit lebih lebar dari Chicken Store (badan 8 × 6 m, tinggi 5,60 m), dan lebih rendah karena atap datar.

Sumbu Blender: X kanan, Y belakang, Z atas, depan -Y; 1 unit = 1 m. Origin tengah tapak di tanah. Fasad Y=-3,50 m, belakang Y=+3,50 m. Pintu di X=0; jendela depan di X≈±3,00 m. Pilar/tebal kusen disesuaikan di dalam lebar badan. HVAC di belakang kiri sekitar X=-2,80, Y=+1,80 m. Pintu servis belakang di tengah X=0, ventilasi di X≈-1,80 m. Roof deck memiliki kemiringan drainase halus tersembunyi di balik parapet; bukan atap pelana.

Catatan pembacaan sketch: angka 1,80 m pada tampak kiri mengacu lebar kanopi kecil; shutter tetap 1,00 × 1,00 m. Panah tinggi pada perspektif bersifat ilustratif; tinggi keseluruhan dihitung dari tanah sampai puncak crest, 4,85 m. Roof plan memperlihatkan atap dan proyeksi kanopi, bukan denah interior.

## Atlas Toonpack

`atlas-audit.json` mencatat hash PSD asli, GUID, material, koordinat pixel dan UV aman. Warna krem, kayu, kaki batu, logam, lampu dan amber memakai swatch yang sudah diverifikasi pada Chicken Store. Warna hijau coping/kanopi/logo dicari ulang pada palette atlas yang sama.

Atlas cukup untuk exterior bergaya toon dengan warna yang mendekati referensi. Serat kayu, sambungan panel kanopi, blok batu dan kisi HVAC dibuat melalui geometri sederhana. Tulisan SUPERMARKET dan emblem tiga daun berupa mesh; tidak memerlukan atlas baru. Warna hijau bisa sedikit berbeda dari referensi karena memilih swatch yang tersedia. Tidak mengklaim atlas menyediakan gambar papan nama persis atau interior transparan.

## Bahan eksekusi setelah ACC

- `reference.png`: gambar sumber pengguna.
- `A-MiniMarket-sketch.png`: lembar visual multi tampak untuk review.
- Dokumen ini: ukuran dan lingkup yang mengikat saat modeling.
- `atlas-audit.json` / `audit-atlas.ps1`: data UV serta pemeriksaan yang bisa diulang.
- `generation-prompt.txt`: prompt sketch; generated with built-in image_gen.

Model dibuat di scene baru pada Blender yang sedang terbuka. Hasil runtime: 2.910 triangle, 2.106 vertex mesh, satu material atlas, tiga mesh (bangunan statis dan dua daun pintu). Detail panel memakai bidang tipis, huruf planar, lampu enam sisi, tanpa subdivision atau bevel menyeluruh. Scene lampu/lantai preview dipisahkan dari asset runtime. Integrasi Unity dilakukan sesuai instruksi berikutnya. Pekerjaan Blender yang sudah terbuka disimpan sebagai snapshot sebelum menambah scene.
