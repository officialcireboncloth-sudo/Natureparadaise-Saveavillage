# Pembuat Pupuk dan kualitas panen

Implementasi awal 7 November 2026; model Blender menggantikan kotak dummy pada
8 November 2026. UI mengikuti
susunan referensi 1672 x 941: header, empat pilihan di kiri, rincian di kanan,
hasil, jumlah, tombol produksi, serta petunjuk keyboard di bawah. Dunia game tetap
terlihat di tengah; gambar ilustrasi referensi tidak dijadikan latar farm palsu.

## Interaksi dan icon slot

- Dekati mesin dan tekan **E**, atau klik prompt interaksi.
- Klik pilihan / **W-S** / panah atas-bawah untuk memilih pupuk.
- Klik **−/+** atau panah kiri-kanan untuk mengubah jumlah 1-99.
- **Enter** / **Buat Pupuk** memproduksi secara langsung ke tas. Bahan tidak lengkap
  atau hasil tidak muat menonaktifkan tombol. **Esc** / **Tutup** menutup UI.
- Jumlah bahan dan hasil mengikuti jumlah produksi. Mengganti resep mereset jumlah ke 1.
- Baris bahan yang kurang diredupkan; hitungan merah untuk kurang, hijau untuk cukup.
- Premium memiliki enam baris yang dipadatkan dalam area bahan yang sama.
- Semua posisi icon sudah tersedia: empat kartu kiri, header detail, tiap bahan,
  dan hasil produksi. `Resources/UI/Fertilizer Maker Theme.asset` menyediakan slot
  sprite. Delapan ilustrasi transparan 128 x 128 sekarang terpasang di
  `Textures/UI/Icons/FertilizerMaker/`: empat karung berlabel biru, hijau,
  terakota dan emas, serta ikan, ember kotoran, kerang dan mineral.
  Sprite item diprioritaskan untuk bahan; grafis pengganti tetap tersedia jika
  tema dilepas. Empat item hasil juga memakai ikon yang sama di inventory.
  Ilustrasi dibuat dengan image generation, lalu dipisah, dipangkas transparansinya,
  dinormalisasi ukuran dan diekspor dengan GIMP (NoHalo). Sumber asli berada di
  `ArtSource/UIIcons/Originals/FertilizerMaker_Sheet.png`; master delapan layer
  bernama ada di `ArtSource/UIIcons/FertilizerMaker_Icons.xcf`, dengan skrip ekspor
  `ArtSource/UIIcons/export_fertilizer_gimp.py`. Latar dunia tetap terlihat.
- UI mengunci movement, menahan input dunia, menyembunyikan HUD lain sementara,
  menghentikan waktu, dan mengembalikan state semula ketika ditutup/destroy.

## Resep

| Hasil per produksi | Bahan |
| --- | --- |
| 1 Pupuk Dasar | 1 Tilapia **Small**, 1 kotoran hewan, 1 kerang laut |
| 1 Pupuk Plus | 1 Koi ukuran apa pun, 2 kotoran hewan, 1 Ca, 1 P |
| 1 Pupuk Super | 1 Tuna **Medium**, 5 kotoran hewan, 1 Ca, 1 P, 1 Urea |
| 1 Pupuk Premium | 1 Marlin **Large**, 10 kotoran hewan, 1 Ca, 1 P, 1 Urea, 1 K |

Kotoran ternak dan unggas dapat dicampur untuk memenuhi kotoran hewan. Tier ukuran
ikan memakai batas ukuran species yang sudah dipakai inventory/kolam; ikan tanpa
metadata ukuran tidak memenuhi bahan yang mensyaratkan ukuran tertentu.

Transaksi menyimulasikan konsumsi dan penempatan hasil pada salinan stack, lalu
melakukan satu commit inventory. Gagal tidak mengurangi bahan, mengubah ukuran
ikan atau menghilangkan kualitas stack. Slot yang kosong setelah bahan dikonsumsi
bisa dipakai hasil; ruang tas diperiksa sesudah simulasi konsumsi.

Catalog: `Resources/Catalogs/FertilizerCatalog.asset`, field `Maker Recipes`.
Resep dan antrean legacy tetap disimpan terpisah untuk kompatibilitas save lama.
Jika antrean lama masih ada, UI menyediakan tombol mengambil hasil lamanya.

## Efek dan kualitas

Syarat pemupukan untuk bintang 5 adalah **efek pupuk aktif pada setiap hari masa
pertumbuhan sejak ditanam**, bukan memakai item pupuk baru setiap hari. Super dan
Premium tetap mencatat hari-hari berikutnya sebagai hari terpupuk selama durasinya
belum habis. Efek yang habis perlu diperbarui tanpa melewatkan hari. Memupuk kembali
setelah satu hari kosong tidak menghapus kekurangan kualitas pada hari tersebut.
Kesuburan tanah dan pemenuhan air tetap menyumbang skor sesuai formula di bawah.

Durasi default yang dipilih sementara karena belum ditentukan: Dasar 1 hari,
Plus 1 hari, Super 3 hari, Premium 7 hari. Ubah `cropBoosterDurationDays` pada ItemSO
atau kolom opsional `crop_booster_duration_days` di Items.csv. Hari penggunaan
termasuk durasi. Efek aktif tidak boleh ditimpa; efek tetap aktif setelah save/load
dan dapat berlanjut ke siklus regrow. Pupuk tidak mempercepat growth atau yield.
Dengan durasi saat ini, untuk tanaman 7 hari: Super dapat dipakai pada hari 1, 4
dan 7; Premium cukup pada hari 1. Keduanya mencakup 7 hari penuh jika tidak ada
hari tanpa efek. Contoh ini mengasumsikan durasi balance default tetap dipakai.

Kualitas = pupuk (maks. 40) + tanah (maks. 40) + air (maks. 20).
Setiap pergantian hari sebelum tanaman matang mencatat efek aktif, pemenuhan air,
dan kesuburan 0-80 pada hari yang selesai. Kesuburan dicatat sebelum penalti tanah
untuk hari selanjutnya. Tanaman matang tidak menambah skor dengan menunggu panen.

Untuk N hari: pupuk aktif menambah 40/N, kesuburan menambah (nilai/80)*40/N,
air terpenuhi menambah 20/N. Riwayat disimpan sebagai hitungan dan total integer;
pembulatan hanya pada teks UI. Jika pertumbuhan tertunda sehingga lebih lama dari
target, seluruh hari tumbuh tetap masuk rata-rata menggunakan max(target, hari
aktual), sehingga hari tambahan tidak dapat menutupi satu hari perawatan yang hilang.

| Kualitas | Bintang |
| --- | --- |
| 0 sampai kurang dari 40 | 1 |
| 40 sampai kurang dari 60 | 2 |
| 60 sampai kurang dari 80 | 3 |
| 80 sampai kurang dari 100 | 4 |
| Tepat 100, semua hari tercatat sempurna | 5 |

Tujuh hari sempurna = 100%. Satu hari tanpa pupuk = 94.285714...%, bintang 4.
Nilai 99.999% yang ditampilkan mendekati 100 tetap tidak mendapat bintang 5.
Save lama tanpa riwayat tanah tidak diberi riwayat sempurna yang tidak pernah dicatat.
Formula ini menggantikan peluang acak dan persyaratan tier booster legacy.

Mutasi memakai peluang 1% per panen dan item tersendiri di `CropDataSO.mutationProduceItem`,
ditandai `isMutatedCrop`. Harga mutasi = 3x harga normal pada bintang yang sama melalui
`mutationSourceItem`. Panen tanaman bintang 5 juga memiliki peluang 20% menggandakan
jumlah hasil; pohon buah tidak mendapat bonus ini. Undian panen disimpan untuk mencegah
pengundian ulang ketika tas penuh. Lihat `SystemsDataAudit.md` untuk aturan pohon,
status data Excel, dan verifikasi terbaru.

**Interaksi dengan tanah:** penurunan kesuburan harian existing tetap berlaku.
Tanah yang menurun dari 80 mengurangi kontribusi tanah secara proporsional. Aturan
Terra saat ini tetap mensyaratkan field kosong. Karena itu, tanpa perubahan balance
tanah atau pemeliharaan tambahan, kondisi tanah 80 setiap hari tidak otomatis
dapat dipertahankan. Tidak ada bonus tersembunyi yang membulatkannya menjadi 100.

## Lokasi dan data

- Prefab mesin: `Resources/World/FertilizerMaker_Dummy.prefab`. Nama file dan GUID
  dipertahankan agar referensi Resources/scene tetap tersambung.
- Runtime installer menambahkan satu mesin di Map/Map_GameplayPreview dekat scanner
  Field 1 jika belum ada. Placeholder kosong lama di lokasi pengujian dipakai ulang,
  diberi visual/collider box, dan dipindahkan ke farm dengan ID save tetap sama.
  Editor: **Nature Paradise > Soil > Place Fertilizer Maker
  In Current Scene**, lalu simpan scene secara normal.
- Empat item keluaran mempertahankan GUID/ID booster lama agar save kompatibel.
- Koi, Marlin, kerang, Ca, P, Urea, K ditambahkan sebagai data ItemSO. Koi/Marlin
  mempunyai definisi memancing untuk pool ikan berbasis Resources; pool manual
  khusus harus memasukkan definisinya bila ingin species tersebut tersedia.
- Kalsium (Ca), Fosfat (P), Urea dan Kalium (K) dijual di supermarket
  (`Resources/Shops/Minimarket Catalog.asset`), tab **Perlengkapan Pertanian**.
  Deskripsi mengikuti kegunaan pada resep. Harga sementara masing-masing **100 G**
  karena harga belum ditentukan; ubah `buyPrice` dan `Items.csv` untuk balance.
  Keempat bahan memakai ikon kemasan tersendiri (`icon_chemical_calcium`,
  `icon_chemical_phosphate`, `icon_chemical_urea`, `icon_chemical_potassium`) di folder
  ikon Fertilizer Maker yang sama. Label Ca, P, UREA dan K dibedakan warnanya.
  Master berlayer dan skrip gambar GIMP berada di `ArtSource/UIIcons/HandPainted/`.
  UI menambahkan tombol tab keenam pada storefront lama dan menyediakan lebar lebih
  untuk nama kategori; tab Bahan, Umpan dan Jual tetap tersedia.
  Bahan juga dapat dicoba melalui F9 debug > Kebun > **Bahan tes: [nama pupuk]**.
  Pemberian bahan ini hanya tersedia di menu debug, tidak pada UI produksi normal.
- Material dummy memakai material yang ada di folder `Material/`, tidak membuat
  folder `Materials` baru.

## Model Blender dan atlas bersama

Master: `ArtSource/FertilizerMaker/Blender/FertilizerMaker_Cozy_Master.blend`.
Model mengikuti referensi mesin merah: tangki membulat, hopper krem berdinding
ganda dan tutup terbuka, engsel, ventilasi bertopi, panel meter analog serta dua
tombol, pintu servis berbingkai aluminium, corong berlubang, baki krem dan butiran
pupuk. Belakang memiliki panel perawatan dan kisi ventilasi. Sebanyak 154 bagian
bernama serta pivot tutup dan pintu tetap dapat diedit di master; gambar atlas
dipack ke file Blender. Mesh runtime adalah gabungan statis dengan tiga LOD,
bukan animasi engsel. Meter dan tombol model merupakan detail visual statis.

Atlas bangunan/interior/prop `NP_CozySurfaces_01` dipakai untuk krem, aluminium,
besi, indikator dan butiran. Halaman tambahan bersama `NP_CozySurfaces_05`
berukuran 512 x 256 menyediakan enamel merah dan muka meter, dilukis di GIMP.
Warna, normal dan surface mask (R metallic / A smoothness) punya master XCF
berlayer di `ArtSource/FertilizerMaker/Textures/`. Halaman atlas 01-04 tidak diedit.
Material tetap di `Material/Shared/`, texture di `Textures/Shared/Cozy/`.

FBX, JSON pertukaran dan native Mesh berada di `mesh/Props/FertilizerMaker/`.
Prefab mempunyai tiga renderer LOD dengan dua material atlas per mesh, satu
BoxCollider dan FertilizerProcessor yang sama. Arah depan Unity -Z, satuan meter,
pivot di pusat dasar. Penempatan di map tetap dilakukan installer yang sudah ada.
Placeholder lama dikenali dan diganti; penggantian kotak yang sudah ditempatkan
mempertahankan posisi, rotasi dan identitas save.

Urutan rebuild: `Scripts/paint_fertilizer_gimp.py` melalui GIMP `python-fu-eval`,
kemudian `Scripts/build_fertilizer_blender.py` melalui Blender 4.5 background.
Di Unity jalankan **Nature Paradise > Soil > Apply Blender Fertilizer Maker Art**
untuk membangun native Mesh dan memasang visual dari JSON. Skrip setup resep juga
memakai art tersebut ketika tersedia, sehingga tidak mengembalikan kotak dummy.
`prepare_unity_assets.py` menyediakan penyiapan file Unity di luar editor; editor
tetap menyediakan jalur import native untuk membangun dan memeriksa ulang aset.
Preview depan, depan-kanan dan belakang-kiri ada di folder `Previews/`.

## Pemeriksaan

Tes kualitas murni: `Tests/FertilizerQualityTests.cs`. Pemeriksaan native Unity:
**Nature Paradise > Soil > Validate Fertilizer Maker**. Fixture memakai Preview
Scene sementara tanpa SaveManager atau akses file save pemain. Meliputi empat
resep/batch, bahan kurang, ukuran ikan salah, dua jenis kotoran, tas penuh, slot
yang dibebaskan konsumsi, notifikasi transaksi, perawatan 7 hari, durasi efek,
JSON save/load, binding item mutasi, layout dan state modal. Render native tiap
resep dan hasil audit lokal berada di `ArtSource/FertilizerMaker/Review/`.

Menu **Setup Fertilizer Maker** menerapkan kembali resep/durasi default dan prefab;
gunakan menu Validate untuk pemeriksaan tanpa mereset data balance yang diedit.
