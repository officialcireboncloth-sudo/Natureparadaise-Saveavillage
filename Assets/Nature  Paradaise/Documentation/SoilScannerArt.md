# Soil scanner - 6 November 2026

Model pengganti dummy mengikuti referensi: casing teal/krem berbevel, monitor recessed
dengan ilustrasi tanah/tunas dan tiga indikator, dua dial scalloped, lampu hijau,
panel servis aluminium, pegangan samping, probe bertangkai D, cradle terbuka,
socket dan kabel melengkung. Sisi belakang memiliki hatch, ventilasi dan pelat seri.

## Sumber yang dapat diedit

- `ArtSource/SoilScanner/Blender/SoilScanner_Cozy_Master.blend`: 65 bagian bernama, ukuran meter,
  material atlas dengan gambar packed, collection studio terpisah dari model.
- `ArtSource/SoilScanner/Textures/NP_CozySurfaces_04_BaseColor.xcf`: layer enamel, rubber, copper,
  grafik layar dan palet baru yang dilukis dengan native GIMP 3.
- XCF normal dan surface mask ada di folder sumber yang sama.
- Kedua script pembuat aset berada di `ArtSource/SoilScanner/Scripts/`.
- Script `paint_scanner_gimp.py` dijalankan melalui GIMP `python-fu-eval`;
  set `__file__` ke path script saat memakai `exec` dari batch.
- Script `build_scanner_blender.py` dijalankan Blender 4.5 dengan `--background --python`.

## Atlas dan Unity

`NP_CozySurfaces_01` tetap digunakan untuk PaintCream dan Aluminium. Slot yang
belum sesuai referensi ditambahkan pada halaman bersama `NP_CozySurfaces_04`
512x512: EnamelTeal, EnamelSage, Copper, Rubber, Screen dan palet indikator.
Koordinat dan padding 8 pixel tercatat di `Textures/Shared/Cozy/NP_CozyAtlasSlots_04.json`.
Normal enamel netral; mask R metallic, A smoothness. Tidak ada perubahan pada
warna/normal/mask atlas 01 yang dipakai barn dan bangunan lain.

Mesh runtime di `mesh/Props/SoilScanner/` memakai dua material bersama, 3 LOD:
16.392 / 7.836 / 3.526 segitiga. Ukuran keseluruhan termasuk probe kira-kira
1.044 x 1.499 x 0.725 meter; pivot di tanah dan depan Unity -Z.
FBX tersedia sebagai ekspor portabel. JSON menyimpan koordinat Unity eksplisit,
normal, UV dan submesh untuk membangun native Mesh .asset secara deterministik.

Jalankan **Nature Paradise > Soil > Apply Blender Scanner Art** setelah rebuild.
Prefab tetap di `Prefabs/Soil/SoilScanner_Dummy.prefab` agar GUID dan instance
yang sudah dipasang pada Map serta Map_GameplayPreview tetap terhubung.
Nama visual baru `Visual_SoilScanner_Cozy`; gameplay component, satu BoxCollider,
interaction range dan InteractionAnchor tetap menggunakan setelan sebelumnya.
Builder lama mengembalikan model baru ketika ekspor Blender tersedia.

## Pemeriksaan

Native Blender berhasil menyimpan master, mengekspor FBX/JSON dan merender depan
serta belakang. Native Unity berhasil membuat mesh, mencocokkan jumlah segitiga,
UV dan bounds, mempertahankan GUID prefab serta collider/anchor dan memeriksa
shader tanpa compile error. Instance Map diperiksa langsung di Scene View.
Scene di disk dan atlas 01 sama dengan hash sebelum pemasangan.
Preview: `ArtSource/SoilScanner/Previews/`. Laporan lokal: `ArtSource/SoilScanner/Review/`.

## Penataan folder sumber dan material

Sumber mentah dipusatkan di `ArtSource/SoilScanner/` mengikuti susunan aset lain:
`Blender/`, `Textures/`, `Scripts/` dan `Previews/`. Master Blender, manifest, XCF
dan script yang sebelumnya ada di `Assets/Nature  Paradaise/Source~/SoilScanner/`
dipindahkan ke sini. Salinan identik di `outputs/SoilScanner/` sudah disatukan.
Pengecualian Git hanya membuka subfolder sumber tersebut, sementara log, profil
aplikasi dan backup tetap diabaikan. Gambar atlas di Blender sudah packed.

Folder material utama tetap `Assets/Nature  Paradaise/Material/`.
Empat material dummy scanner lama dipindahkan dari `Materials/SoilScanner/`
ke `Material/SoilScanner/` bersama file `.meta`, sehingga GUID tetap sama.
Folder `Materials/` yang kosong dihapus. Builder dummy juga memakai path baru.
Scanner baru tetap menggunakan material atlas di `Material/Shared/`.

Ini penataan file: jumlah material dan shader runtime tidak berubah, tidak
menambah draw call atau mengubah kualitas grafis/FPS. Path string harus diperbarui
seperti pada builder, sedangkan referensi Unity yang berbasis GUID tetap valid.
Tidak ada material berbeda yang digabung atau dihapus karena nama mirip.

Layar pada model merupakan ilustrasi statis referensi. UI pemindai interaktif
tetap memakai SoilScannerMachine/SoilScannerUI yang sudah tersedia. Performa
pada perangkat Android/iOS belum diukur.
