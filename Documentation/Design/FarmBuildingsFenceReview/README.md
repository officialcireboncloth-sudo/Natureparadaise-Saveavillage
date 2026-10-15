# Farm buildings + modular fence — proposal A

Status: SKETCH DISETUJUI PENGGUNA 13 Oktober 2026; MODEL BLENDER SELESAI. Sumber, preview dan sembilan FBX disimpan terpisah di `ArtSource/FarmBuildingsFence_A`; audit saved-file dan FBX roundtrip PASS. Ukuran tapak/tinggi proposal menjadi ukuran produksi. Lis/lampu menambah envelope visual, lihat MeshManifest.json. Tidak mengganti model Barn_A atau scene gameplay yang sudah ada. Tulisan papan BARN STORE dibuat sebagai geometry atlas-only, mengikuti sketch yang disetujui.

## Acuan dan lingkup

Acuan: gambar farm yang diberikan pengguna pada 13 Oktober 2026. Paket ini mengusulkan dua bangunan pada gambar: lumbung krem di kiri dan Barn Store merah di kanan, serta kit pagar kayu modular. Hanya exterior: badan, atap, lis, fondasi, pintu, jendela/shutter, kanopi, lampu fasad dan papan nama. Tidak termasuk hewan, tanaman, kebun, jalan, terrain, jerami, karung, peti, tong, alat, atau interior lengkap.

Gambar hanya menunjukkan depan dan sebagian sisi. Belakang, posisi bukaan di sisi tersembunyi, ukuran dan struktur sambungan merupakan usulan desain; bukan hasil pengukuran gambar. Bangunan Barn_A yang sudah ada tidak dianggap persetujuan untuk proposal baru ini dan tidak ditimpa.

Sketch PNG adalah acuan visual. Angka di dokumen ini adalah sumber ukuran proposal jika anotasi gambar generatif berbeda. Setelah ACC, ukuran harus dipakai bersama untuk semua view dan mesh, bukan ditafsir ulang dari perspektif PNG.

## Ukuran usulan

Satu unit = satu meter. Ukuran W/D/H menggunakan sumbu Unity X/Z/Y. Blender X/Y/Z = kanan/belakang/atas; depan Blender -Y. Origin bangunan di tengah tapak pada tanah. Orientasi penempatan gameplay diperiksa saat integrasi nanti.

| Komponen | A: lumbung krem | B: Barn Store merah |
|---|---:|---:|
| Badan tengah W × D | 4,00 × 5,00 m | 5,00 × 6,00 m |
| Sayap kiri W × D | 1,50 × 5,00 m, nook terbuka depan | 1,50 × 6,00 m, ruang tertutup |
| Sayap kanan W × D | 1,50 × 5,00 m, tertutup | 2,50 × 6,00 m, kanopi terbuka |
| Tapak total W × D | 7,00 × 5,00 m | 9,00 × 6,00 m |
| Envelope atap W × D | 7,70 × 5,70 m | 9,70 × 6,70 m |
| Tinggi puncak atap | 5,60 m | 5,40 m |
| Eave badan utama | 3,00 m | 3,10 m |
| Eave luar sayap | 2,20 m | 2,30 m |
| Fondasi | 0,25 m | 0,25 m |
| Overhang tepi luar | 0,35 m | 0,35 m |
| Bukaan pintu depan W × H | 2,40 × 2,60 m | 2,40 × 2,50 m |
| Bukaan loft W × H | 1,00 × 0,90 m, terbuka gelap | 1,40 × 1,00 m, shutter X tertutup |

Atap keduanya gambrel: dua kemiringan per sisi dengan patahan terlihat dari depan/belakang; sayap memakai atap lean-to. Usulan profil badan A: ridge (X=0,H=5,60), patahan (X=±1,25,H=4,60), eave (X=±2,00,H=3,00). B relatif pusat badan tengah: ridge (0,5,40), patahan (±1,55,4,60), eave (±2,50,3,10). Overhang melanjutkan bidang ujung; tinggi eave tabel adalah tepi struktur sebelum overhang. Lis dan ketebalan atap harus dihitung dalam batas final saat modeling.

A: dinding krem, atap merah bata, pintu madu, lis gading. Belakang diusulkan satu ventilasi loft tertutup; kiri satu shutter; kanan satu pintu servis 0,90 × 2,00 m. B: dinding merah, atap abu arang, rangka/lis krem, pintu madu dengan kaca atas. Sayap kiri memiliki jendela depan; sayap kanan rangka terbuka kosong. Belakang diusulkan satu vent loft dan satu jendela lantai bawah; kiri satu jendela servis. Papan nama B 2,80 × 0,65 m, teks BARN STORE hanya acuan bentuk; atlas yang diperiksa belum menyediakan tulisan persis itu. Untuk implementasi atlas-only, papan kayu kosong menjadi default; teks dapat berupa mesh terpisah jika disetujui. Ornamen ayam atap tidak masuk usulan awal.

## Kit pagar modular

Pagar mengikuti referensi: tiang tebal ujung bevel dan dua rail horizontal. Komponen memakai ukuran penuh, bukan scale nonuniform saat disusun.

| Nama usulan | Ukuran/fungsi |
|---|---|
| Fence_Post | 0,24 × 0,24 × 1,20 m; pivot tengah bawah; satu tiang untuk tiap titik sambungan |
| Fence_Rails_2m | Pitch 2,00 m antar pusat tiang; dua rail panjang bersih 1,76 m, tebal 0,10 m, tinggi 0,16 m |
| Fence_Rails_1m | Pitch 1,00 m; rail bersih 0,76 m, penampang sama |
| Fence_Corner_90 | Tiang sudut dengan socket dua arah; menggantikan satu Fence_Post |
| Fence_Junction_T | Tiang dengan socket tiga arah; menggantikan satu Fence_Post |
| Fence_Gate_2m | Pitch tiang 2,00 m; bukaan 1,76 m; daun 1,64 × 0,10 × 0,90 m; gap 0,06 m tiap sisi; bracing X |
| Fence_Gate_3m | Pitch tiang 3,00 m; bukaan 2,76 m; dua daun masing-masing lebar 1,29 m; gap sisi dan tengah masing-masing 0,06 m |

Rail pusat ketinggian 0,40 dan 0,85 m; daun gerbang bawah 0,15 m dan atas 1,05 m. Rail modules tidak membawa tiang supaya sambungan tidak menggandakan tiang. Pivot rail berada di pusat tiang awal pada tanah; socket akhir berada pada X=1 atau 2 m. Rail lokal sepanjang +X; putar 90° untuk sisi lainnya. Tiang sudut/T tetap terpisah. Gerbang diekspor sebagai root/socket serta daun dengan pivot engsel, sehingga siap dianimasikan nanti; animasi dan script bukan bagian modeling awal.

## Atlas yang tersedia

File tekstur sumber: `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Textures/TFP_Atlas_1A.psd`, GUID `522fcb2b5717c7640b460ee9ff73a78a`. Material yang diperiksa: `.../Models/Materials/TFP_Atlas_1A.mat`, `_MainTex` dan `_TextureSample` mengarah ke GUID tersebut. Preview audit tersedia di `Documentation/Design/BarnExteriorReview/atlas-inspection.png` (2048 × 2048).

Atlas memiliki palet warna/gradient di kiri bawah dan beberapa area gambar/teks. Gunakan palet untuk krem, merah bata, kayu madu/coklat, abu atap, batu abu hangat dan besi gelap. Tidak ditemukan tile khusus serat papan atau genteng sesuai gambar: pembagian papan, genteng besar, brace dan fondasi dibuat sebagai geometry sederhana. Warna watercolor pada sketch bersifat arahan, bukan janji tekstur baked baru.

Material opaque utama satu atlas bersama. Lampu memakai besi gelap; glow opsional memakai material lights pack yang sudah tersedia dan diperiksa ulang saat eksekusi. UV0 ke patch palet yang aman, dengan margin; tidak membuat atlas baru, texture wood/shingle baru, atau mengubah atlas asli. Koordinat UV spesifik dipilih dari sumber atlas saat modeling agar tidak menebak dari preview skala kecil. Blender menggunakan preview material; hasil akhir dinilai memakai material/shader project Unity.

## Bahan dan langkah sesudah ACC

Siapkan reference asli, turnaround A/B, kit pagar, angka ukuran dokumen ini, sumber PSD atlas dan nama material Unity. Collection Blender terpisah: A, B, FenceKit, Export, Preview. Atap, badan, fondasi, rangka, pintu dan hardware tetap dapat diedit pada sumber; daun gerbang memakai pivot engsel sendiri. Target awal stylized low poly: A sekitar 8–15k triangles, B 12–20k, panel rail 100–250, tiang 40–100, gate 300–800; ini budget usulan sebelum audit mesh.

Sesudah ACC: blockout skala → review siluet dan semua sisi → detail geometry → UV atlas → audit topology/normal/origin → export FBX → validasi skala/material di Unity. Collider memakai volume sederhana, bukan mesh detail pagar/bangunan. LOD dan integrasi prefab memerlukan pemeriksaan scope saat eksekusi. Tidak menjalankan langkah tersebut sebelum pengguna menyetujui sketch.
