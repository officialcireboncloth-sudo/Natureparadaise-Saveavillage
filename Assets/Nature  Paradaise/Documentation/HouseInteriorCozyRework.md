# Interior rumah Lv.1 — cozy

1 November 2026. Gambar kanan pada HouseInterior_Lv1_Target.png menjadi acuan
susunan dan detail interior. Rework hanya menyentuh prefab interior Lv.1.

## Berkas utama

Lokasi relatif terhadap `Assets/Nature  Paradaise/` (dua spasi).

| Isi | Lokasi |
| --- | --- |
| Master Blender modular | `Source~/HouseInteriorCozy/HouseInterior_Cozy_Lv1_Master.blend` |
| Skrip pembentukan ulang, manifest, acuan | `Source~/HouseInteriorCozy/` |
| Enam FBX sumber dan mesh runtime | `mesh/Buildings/HouseInteriorCozy/` |
| Prefab yang dipakai scene | `Prefabs/PlayerHouse/Interior/HouseInteriorLayout_Lv1.prefab` |
| Preview dan audit native Unity | `Documentation/HouseInteriorCozyReview/` |
| Menu penerapan ulang | `Nature Paradise > Art > Apply Cozy House Interior Level 1` |

Master menyimpan modul Architecture, Furniture, Bed, SaveBook, Radio, dan Chest
pada koleksi editable. Koleksi ekspor berisi enam mesh gabungan. Lampu studio,
kamera preview dan anchor tidak ikut FBX. Tekstur dikemas di dalam .blend;
sumber menggunakan helper bersama `Source~/PoultryCozy/build_poultry_cozy.py`
dan `Source~/HouseCozy/build_house_cozy.py`.

## Perubahan visual

- Rangka kayu gelap lebih tebal, sambungan dan cap logam di sudut; dinding plester
  krem dan cutaway samping menurun ke depan mengikuti acuan.
- Lantai berupa papan kayu staggered dengan variasi potongan UV dan lapisan gelap
  di bawah celah. Alas batu abu-abu, ambang, serta pintu terbuka dibuat 3D.
- Ranjang berkepala melengkung, bantal lembut, selimut sage dengan lipatan dan
  drape, karpet linen halus beserta fringe.
- Meja kecil dan buku SAVE, rak radio dengan buku/tanaman, radio berknob/speaker
  dan antena, meja makan dua kursi dengan runner, cangkir, dan bunga putih 3D.
- Peti berpapan dengan lid, pita besi, rivet dan pengunci. Framed botanical print
  di kanan belakang berfungsi sebagai kalender.
- Jendela recessed dengan kusen, mullion dan sill tebal, pemandangan daun, serta
  satu spot light hangat. Cahaya jendela mengikuti jam dan cuaca melalui
  HouseInteriorCozyLighting: redup saat hujan dan biru lembut pada malam hari.

Kamera Lv.1 dimundurkan sedikit agar seluruh ruangan dan balok atas masuk frame;
lensa 40 derajat dan kemiringan 54 derajat tetap. Kursi/table digeser 0,20 m ke
kiri agar kursi kanan tidak menembus dinding. Ukuran dasar ruangan tetap 5,2 x 5 m.

## Gameplay dan performa

Komponen serta posisi bed/sleep/wake, save book, radio, storage, entry dan exit
dipertahankan. Kalender memakai UI dan data yang sudah ada, dengan area interaksi
ke sisi kiri rak agar tidak selalu kalah dari prompt radio. Collider lantai,
dinding dan pintu lama tetap; collider meja/kursi mengikuti pergeseran visual.

Enam mesh renderer memakai tiga material atlas cozy yang sama (sembilan slot
material total); tidak ada tekstur unik baru. Jumlah segitiga ada di manifest.
Import FBX tidak menyimpan salinan CPU; mesh runtime kasur tetap readable untuk
pencarian permukaan kasur oleh PlayerBed. Satu spot light tidak memakai shadow
pada perangkat mobile. Override atlas mobile tetap 1024 ASTC 6x6. FPS pada
perangkat fisik belum diukur.

## Verifikasi

Unity 6000.0.81f1 menjalankan 55 pemeriksaan pada salinan terisolasi: orientasi
unmirrored, ukuran meter dan jumlah segitiga cocok dengan Blender, UV berada
di atlas, material/shader tersedia, area masuk/bangun/tengah/samping kasur
bebas untuk capsule player, permukaan kasur dapat diambil, bed masih interaktif,
kalender dan radio memiliki area pilihan masing-masing, cahaya merespons hujan
serta malam, dan scene HouseInterior benar-benar merujuk prefab/camera Lv.1 baru.

Preview menampilkan aset dalam lighting pemeriksaan terisolasi. Scene utama,
level rumah 2–5, atlas dan material bersama diverifikasi melalui hash sebelum
serta sesudah pemasangan. Save dan sesi Play pengguna tidak digunakan.

Paket final: 20.760 segitiga, enam mesh renderer, dan tiga material bersama.
Seluruh 55 pemeriksaan Unity lolos pada ekspor final yang dipasang.
