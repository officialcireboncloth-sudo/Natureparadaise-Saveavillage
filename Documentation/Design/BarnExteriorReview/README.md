# Barn exterior A — spesifikasi untuk review

**Status: sketsa disetujui; model Blender gabungan dan FBX sudah diterapkan ke prefab Unity di `Prefabs/Barn/Exterior`.** File sumber ada di `ArtSource/Barn_A`. Exterior Lv1–5 siap; data gameplay saat ini menghubungkan Lv1–4. Acuan bentuk adalah gambar barn merah yang diberikan pengguna. Semua ukuran di sini adalah keputusan desain, bukan pengukuran yang diklaim berasal dari foto. Satu unit Unity sama dengan satu meter.

Pengguna meminta exterior level 1–5 dibedakan **hanya ukuran**. Karena itu kelima level menggunakan satu master mesh, UV, material, proporsi atap, pintu, jumlah shutter, dan detail yang sama. Seluruh model mendapat uniform scale XYZ; tidak dipanjangkan hanya satu sumbu, tidak ditambah sayap, dekorasi, lantai, atau varian warna per level.

## Gambar yang bisa diperiksa

- `barn-volume-front-right.png` dan `barn-volume-rear-left.png`: sketsa volume dari koordinat master yang sama, menggunakan warna yang disampling dari atlas pack. Ini gambar perencanaan, bukan render model Blender final.
- `barn-four-views.png`: depan, kanan, belakang, kiri pada ukuran master level 3. Tampak individu tersedia dalam `barn-front-elevation.png`, `barn-right-elevation.png`, `barn-rear-elevation.png`, dan `barn-left-elevation.png`.
- `barn-top-and-levels.png`: tampak atas, envelope atap, posisi pintu, pivot, dan ukuran level 1–5.
- `barn-atlas-and-materials.png`: palet atlas asli, warna terpilih, koordinat piksel, dan area UV aman.
- `barn-proposal-A.json`: acuan produksi yang menyimpan ukuran master, 87 komponen sketsa, posisi, ukuran, arah, skala, UV, dan budget. Setelah disetujui, ini menjadi sumber ukuran; jangan mengukur ulang perkiraan dari PNG.

## Ukuran utama

Lebar adalah X, kedalaman adalah Z, tinggi adalah Y. Depan menghadap **-Z**, belakang +Z. Origin berada di tengah tapak tepat di tanah `(0,0,0)`. Level 3 menjadi master sebelum scale.

| Level | Uniform scale XYZ | Badan dinding W × D | Batas visual W × D × H, termasuk atap |
|---|---:|---:|---:|
| 1 | 0,750 | 6,00 × 7,50 m | 6,675 × 8,175 × 5,10 m |
| 2 | 0,875 | 7,00 × 8,75 m | 7,7875 × 9,5375 × 5,95 m |
| 3 | 1,000 | 8,00 × 10,00 m | 8,90 × 10,90 × 6,80 m |
| 4 | 1,125 | 9,00 × 11,25 m | 10,0125 × 12,2625 × 7,65 m |
| 5 | 1,250 | 10,00 × 12,50 m | 11,125 × 13,625 × 8,50 m |

Ukuran badan adalah persegi panjang dinding, sedangkan batas visual mencakup overhang, ridge cap, trim, lampu, dan threshold. Scale akan membesarkan pintu dan seluruh detail juga. Pintu utama paling kecil pada Lv1 menjadi 2,55 × 2,40 m; pintu samping hanya detail exterior dalam scope ini.

## Ukuran bagian master Lv3

| Bagian | Ukuran / posisi |
|---|---|
| Fondasi | Tinggi 0,50 m; dua baris batu sederhana, lebar blok sekitar 0,55–0,90 m |
| Dinding merah | X -4 sampai +4; Z -5 sampai +5; Y 0,50–4,20 m |
| Atap pelana | Ridge Y 6,60 m, eave struktur Y 4,20 m; slope 30,96° |
| Overhang | 0,45 m pada depan, belakang, kiri, kanan; tebal bidang atap 0,12 m |
| Eave luar / cap | Eave luar Y 3,93 m; ridge cap puncak Y 6,80 m |
| Lis atap / fascia | Sekitar 0,22 m lebar, 0,20 m tebal; bevel 0,025 m satu segmen |
| Tiang sudut | 0,26 × 3,70 × 0,26 m; empat tiang; permukaan tersembunyi dibuang saat final merge |
| Pintu depan | Dua daun; clear rectangle total 3,40 × 3,20 m; bawah Y 0,18 m |
| Frame pintu depan | Lebar total 3,84 m; jamb 0,22 m; lintel Y 3,49 m |
| X-brace pintu | Dua diagonal setiap daun, lebar 0,14 m; bevel 0,015 m |
| Handle dan engsel | Handle 0,075 × 0,32 m; plat engsel sekitar 0,38 × 0,11 m |
| Loft shutter depan | Dua panel dalam clear rectangle 1,80 × 1,40 m; pusat `(0,5.05,-5.055)` |
| Lampu depan | Frame sekitar 0,34 × 0,48 × 0,22 m, pusat X 0 / Y 3,67 m; glow hanya core kecil |
| Pintu kanan | 1,30 × 2,45 m, pusat Z +1,80 m; satu diagonal; frame 0,18 m |
| Shutter kiri | Dua shutter 1,20 × 1,20 m; pusat Z -2,50 / +2,50 m, Y 2,55 m |
| Vent loft belakang | 1,20 × 1,00 m; pusat X 0 / Y 4,95 m; empat louver |
| Brace samping | Lima per sisi, pada Z -4 / -2 / 0 / +2 / +4 m |
| Threshold depan | 3,84 × 0,40 × 0,18 m; pusat Z -5,20 m |
| Threshold samping | 0,40 × 1,66 × 0,18 m pada sisi kanan; ujung tidak melewati atap |
| Marker interaksi depan | `(0,0,-5.90)` sebelum scale; player mendekati muka pintu dari -Z |

Tampak belakang sengaja sederhana: dinding merah, fondasi batu, trim dan satu vent loft. Kiri memakai dua shutter tertutup. Kanan memakai satu pintu samping seperti referensi. Siluet tetap satu volume atap pelana tanpa sambungan bangunan tambahan. Portal menuju scene BarnInterior tetap di pintu utama; pintu samping dan shutter belum memiliki animasi atau ruangan baru.

## Audit atlas dan material

File sumber yang benar-benar diperiksa: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd`, composite **2048 × 2048**, GUID `522fcb2b5717c7640b460ee9ff73a78a`. Bagian kiri bawah memiliki palet warna/gradient; bagian lain berisi vegetation, tanah, potret, dan tulisan. Palet dapat dipakai untuk merah cat, krem/coklat kayu, abu hangat atap, batu, besi gelap, dan kuning lampu.

Atlas ini **tidak memiliki texture tileable khusus papan barn / atap shingle / susunan batu sesuai referensi**. Kita tidak perlu menambahnya untuk pendekatan low poly ini: bentuk dan pemisahan panel dibuat di mesh, lalu muka-mukanya diarahkan ke area palet. Serat kayu, paku kecil dan goresan realistis pada referensi tidak dibuat sebagai texture baru atau mesh mikro. Yang dipertahankan adalah bentuk besar, warna dan keterbacaan dari kamera game.

Material opaque memakai **`TFP_Atlas_1A.mat`**, yang sekarang menunjuk shader pack `CustomToon` dan atlas tersebut. Material lampu **`TFP_Atlas_Lights_1A.mat`** juga menunjuk atlas yang sama. Rencana maksimum dua material: seluruh bangunan opaque dan core lampu emissive. Tidak mengubah material/PSD/import settings bersama milik pack karena akan memengaruhi aset lain. Tidak memerlukan texture albedo, normal, AO atau salinan atlas baru per level.

Koordinat UV per warna sudah dicatat dalam JSON. Piksel PSD dihitung dari kiri atas, UV dari kiri bawah. Gunakan margin 4 px di dalam sel 32 × 64 px; UV setiap face diarahkan ke gradient yang dipilih, tidak melakukan repeat terhadap seluruh atlas. PNG di folder review hanya dokumentasi inspeksi; tidak diimport sebagai pengganti texture runtime. Warna sketsa berasal dari sampel atlas; tampilan akhir tetap dipengaruhi shader dan lighting game.

## Low poly dan cara produksi Blender setelah ACC

Target LOD0 sekitar **3.200 triangles**, batas **4.500 triangles** setelah triangulasi; LOD1 sekitar 1.800, LOD2 sekitar 450. Ini budget usulan, belum hasil hitung model yang belum dibuat. Seluruh level memakai topology dan LOD yang sama.

1. Bangun master pada ukuran Lv3, unit meter. Di Blender gunakan X kanan, Y belakang, Z atas; muka depan -Y. Simpan origin di tengah tanah. Konversi sumbu FBX menuju Unity X kanan / Y atas / Z belakang; verifikasi arah muka setelah import.
2. Bentuk shell besar dari box dinding, dua gable dan dua bidang atap. Semua bidang bawah/tersembunyi yang tidak terlihat dari luar dibuang sebelum export.
3. Papan memakai pitch 0,25 m dan celah dangkal sekitar 0,008 m. Detail rendah cukup dari face/panel tipis dan perbedaan UV; tidak menjadikan setiap papan sebagai objek solid dengan enam muka.
4. Atap memakai 10 baris per slope dan 16 kolom sepanjang kedalaman sebagai subdivisi bidang. Buat lip baris setinggi sekitar 0,025 m, seam 0,018 m; tidak membuat setiap shingle sebagai mesh solid terpisah. LOD1 mengurangi subdivisi/lip, LOD2 memakai dua bidang sederhana dengan siluet ridge tetap.
5. Tiang, fascia, frame dan X-brace menggunakan balok bevel satu segmen pada bagian yang terbaca. Batu menggunakan dua baris blok bersudut lembut dengan satu segmen bevel, jitter maksimal 0,03 m; tidak displacement/subdivision sculpt. Tidak membuat paku mikro.
6. Lampu 6–8 sisi, metal matte; emission hanya core, tidak seluruh frame. Point light opsional diuji pada malam hari, terpisah dari mesh dan tidak lima lampu aktif sekaligus untuk satu barn.
7. UV manual ke palet yang telah diaudit; material sama untuk semua opaque face. Source objects dipisah rapi untuk edit, export static final digabung per LOD agar tidak menjadi puluhan renderer. Smooth shading secukupnya pada bevel, batas tajam pada atap/frame; hindari permukaan glossy realistis.
8. Apply transform master lalu export satu set mesh LOD. Buat lima wrapper/prefab yang mereferensikan mesh dan material bersama, dengan scale 0,750 / 0,875 / 1 / 1,125 / 1,250. Collider dan marker entrance ikut acuan dimensi yang sama; ukuran interaksi boleh memakai collider terpisah agar mudah didekati player.
9. Ukur triangles aktual, jumlah material/renderer, bounds, tinggi pintu Lv1, UV bleeding, arah depan, shading pagi/sore/malam, dan jarak culling di Unity. Cocokkan bentuk/tapak setiap level dengan JSON; jangan mengubah proporsi sewaktu fit prefab.

Atur culling melalui bounds renderer/LODGroup standar pada prefab. Jarak transisi LOD dituning terhadap kamera world saat implementasi; angka jarak belum diklaim diuji. Bangunan menggunakan collider box sederhana, bukan MeshCollider untuk setiap batu/shingle. Karena interior terpisah, bangunan exterior tertutup secara visual; trigger pintu berada di luar badan collision.

## Penempatan, level 5 dan organisasi file

Rencanakan area untuk ukuran maksimum Lv5 sejak penempatan: atap 11,125 × 13,625 m, dengan rekomendasi rectangle cadangan 12 × 16 m agar ada ruang pendekatan pintu. Jika menggunakan FieldArea, jumlah sel harus berasal dari ukuran dunia dibagi `FieldArea.CellSize`, dibulatkan ke atas. Asset Barn sekarang mempunyai footprint 4 × 4 sel; itu tidak otomatis mencukupi untuk model usulan ini.

Asset `Barn Building.asset`, menu setup dan interior controller yang diperiksa sekarang masih mengatur level 1–4. Setelah model disetujui, tahap apply perlu menambah dukungan level 5 pada rantai data/preview/portal yang relevan. Desain exterior ini tidak menetapkan biaya upgrade atau kapasitas hewan Lv5 secara sepihak, dan tidak mengubah interior kandang dalam tahap sketch ini.

Folder yang akan dipakai nanti:

```text
ArtSource/Barn_A/
  Blender/Barn_A_Master.blend
  Scripts/                 # script produksi/rebuild bila dipakai
  References/              # referensi dan spesifikasi yang di-ACC
  Previews/                # render Blender dan audit bentuk
Assets/Nature  Paradaise/Mesh/Buildings/Barn_A/
  Barn_A_Master_LOD0.fbx
  Barn_A_Master_LOD1.fbx
  Barn_A_Master_LOD2.fbx
Assets/Nature  Paradaise/Prefabs/Barn/Exterior/
  BarnExterior_Lv1 ... BarnExterior_Lv5
```

Tidak membuat folder texture duplikat di sana: material Unity tetap mengacu ke atlas Toon Farm Pack. Raw `.blend` di ArtSource tidak diimport otomatis oleh Unity. Folder review ini tetap menjadi arsip sketsa yang disetujui beserta manifestnya. Saat pengguna ACC, bentuk, ukuran dan posisi ini menjadi acuan produksi; revisi sebelum ACC tetap dilakukan pada usulan, bukan langsung mengganti barn di map.
