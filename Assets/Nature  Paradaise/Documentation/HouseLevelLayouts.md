# Interior rumah berdasarkan level

## Kasur dan rak alat

Rak `Bookshelf_Editable` di dekat kasur sekarang memiliki `ToolStorageChest` dan memakai data tool storage yang sama pada semua level. Peti alat lama nonaktif agar area tidur lebih lega.

`PlayerBed` dan collider berada pada wrapper `Bed_MeshSlot`, sesuai bounds model yang terlihat. Interaksi memakai jarak ke permukaan kasur, tanpa syarat arah hadap. Bila kasur dan rak sama-sama terjangkau, objek yang lebih dekat diprioritaskan.

Di setiap `LivingBedroom_Editable/Bed_MeshSlot`, `WakeStandPoint_Editable` adalah titik berdiri di lantai kosong, terpisah dari `SleepPose_Editable` untuk animasi di kasur. Titik bangun divalidasi terhadap lantai dasar dan ruang capsule sebelum CharacterController diaktifkan. Durasi bangun mengikuti clip animasi; pose kembali ke locomotion sebelum player dipindahkan ke titik berdiri.

Pengujian tidur penuh level 1–4: masing-masing 16 kombinasi sisi/arah dapat berinteraksi, titik keluar memiliki lantai, player selesai tidur tanpa overlap collider kasur dan kaki tetap di atas lantai. Laporan lokal: `Library/BedAccessVerification.txt`.

Semua geometri dan furniture tersimpan di `Map/Scenes/Interiors/HouseInterior.unity`. Runtime hanya memilih layout sesuai level rumah yang selesai di-upgrade, tanpa membuat mesh atau furniture.

| Level | Ukuran ruangan | Fitur |
|---|---|---|
| 1 | 12 × 10 | Tempat tidur, ruang duduk, kamar mandi, storage umum; tanpa dapur dan kulkas |
| 2 | 16 × 11 | Dapur, meja makan, kulkas |
| 3 | 20 × 13 | Ruangan lebih luas, area storage dengan dua peti dan lemari; storage umum 40 slot |
| 4 | 22 × 15 | Ruang duduk tambahan, tiga peti; storage umum 48 slot |

Ukuran furniture sama pada seluruh level. Lantai memiliki collider dasar yang menerus; tembok belakang setinggi 3,6 unit. Bagian depan dan partisi dekat kamera memakai visual cutaway agar ruangan terlihat, sementara collider tembok tetap membatasi ruangan. Preset lama disimpan sebagai referensi nonaktif di `OriginalPreset_Reference_Inactive`.

## Preview dan pengeditan

Pilih root `HouseInterior_Editable` yang memiliki `HouseInteriorController`, lalu gunakan tombol **Preview Lv.1–4** di Inspector. Gunakan **Fokus Preview Interior** untuk mengisolasi rumah dari terrain Map di Scene View; tombol Exit pada Isolation View mengembalikan tampilan. Edit objek di `InteriorLayout_LvN_Editable` dalam Hierarchy. Preview editor terpisah dari level runtime; saat Play, rumah memakai level yang sudah selesai. Override debug `testHouseLevel` sekarang 1, sehingga tidak lagi memaksa level 4.

Setiap layout memiliki `EntryPoint_Editable` dan `ExitPoint_Editable`. Marker bersama untuk perpindahan scene mengikuti marker level aktif. Menu `Nature Paradise/House/Author Progressive Level Layouts` menyediakan setup editor; scene yang sudah dibuat tidak dibangun ulang agar perubahan manual terjaga.

## Debug layout saat Play

Menu `DEBUG INTERIOR` muncul otomatis di kiri layar hanya saat player berada di interior rumah. Tombol Lv.1–4 mengganti layout, marker masuk/keluar, dan kamera level aktif; player dipindahkan ke titik masuk yang tervalidasi agar aman saat ruangan mengecil. Tombol `Kembali ke level asli` mengembalikan layout sesuai progression. Pergantian ditolak selama transisi, pause, atau interaksi modal.

Pilihan debug bersifat sementara dan dihapus saat keluar rumah. Level rumah, unlock fitur, isi storage, dan save tidak diubah. Menu juga tersedia jika scene interior dijalankan langsung untuk pengujian. Verifikasi Play Mode terpisah memeriksa visibility, keempat layout/kamera, posisi player, modal guard, reset saat keluar, dan cleanup ketika rumah dinonaktifkan.

## Penyimpanan item

Level 1 memiliki storage umum pada Bookshelf_Editable untuk seluruh item (kecuali item yang secara eksplisit ditandai Never). Level 2 menambahkan kulkas dan memakai storage rumah untuk tools, seed, hasil panen mentah, ikan mentah, dan material. Makanan matang serta minuman disimpan di kulkas. Semua layout menyimpan komponen HouseStorageChest langsung di Hierarchy; peti tambahan level 3–4 mengakses data storage rumah yang sama.

Saat upgrade selesai, HouseStorageService.SortForHouseLevel memindahkan makanan matang dari storage lama ke kulkas. ToolStorageService lama dimigrasikan ke storage rumah; bahan mentah dalam kulkas save lama dikembalikan ke storage rumah. Jumlah, kualitas, ukuran, dan berat ikan dipertahankan. Kapasitas deposit baru adalah 24/32/40/48 slot untuk level 1/2/3/4; data migrasi melebihi kapasitas tetap dipertahankan dan dapat diambil. Bila kulkas penuh, makanan yang belum muat tetap tersedia di storage rumah dan sortir dicoba kembali saat membuka storage/kulkas. Sortir berulang tidak menggandakan item.

Save memakai field houseStorage dan refrigerator yang sudah ada. Sortir ditunda selama restore level rumah dan kedua isi storage agar migrasi tidak tertimpa restore berikutnya. Isi bertahan setelah keluar rumah, upgrade, serta save/load. Drag memindahkan satu stack, tombol jumlah tetap untuk transfer sebagian.

### Pengaturan Items.csv

Kolom opsional storage_destination menerima Auto, House, Refrigerator, atau Never. Auto memakai refrigerator_category dan food_preparation: CookedFood/Drink masuk kulkas; kategori hasil tani mentah (Crop, Fruit), ikan (Fish), bahan (Ingredient), dan hasil hewan (AnimalProduct) tetap ke storage rumah. Item tanpa kategori kulkas dengan ReadyToEat juga masuk kulkas. House memaksa ke storage rumah; Refrigerator memaksa ke kulkas dengan tetap menolak tool, seed, key item, dan quest; Never melarang deposit baru. Item lama yang aturannya berubah tetap dapat diambil.

Edit Data/Balance/Items.csv lalu jalankan Nature Paradise > Data CSV > Import Items and Crops. Kolom tidak ada/kosong pada CSV lama mempertahankan setting asset. Export Items and Crops menyertakan setting ini. Tidak perlu mengubah kode untuk aturan per item.

Verifikasi migrasi, save roundtrip, metadata ikan, filter bahan mentah, legacy tool/kulkas, kulkas penuh, serta validasi CSV: Library/UnifiedStorageAudit.txt.

## Verifikasi

Pengujian Unity Play Mode memeriksa keempat level, tepat satu layout aktif, dapur/kulkas mulai level 2, transfer dua arah kulkas dan peti, serta capture/restore metadata item. Pemeriksaan raycast grid seluruh footprint tidak menemukan lubang lantai pada keempat level. Laporan pengujian lokal: `Library/HouseRuntimeVerification.txt` dan `Library/HouseLevelVerification.txt`.

## Kamera dan gaya frontal

Setiap level memiliki kamera perspektif nonaktif InteriorPerspectiveCamera_Editable sebagai pengaturan yang dapat diedit di Hierarchy. HouseInteriorView menyalin posisi dan proyeksinya ke kamera gameplay selama di rumah, lalu mengembalikan kamera world saat keluar. Sudut menghadap lurus dari depan dengan pitch 38 derajat dan FOV 40 derajat. Framing diperluas untuk layar sempit. Rak, lemari, tempat tidur berada di tepi, dengan karpet dan area duduk di tengah; furniture tidak ikut diperkecil saat ukuran bangunan dirapatkan.

Menu editor Nature Paradise/House/Apply Frontal Interior Style menerapkan migrasi sekali; marker mencegah pengeditan manual ditimpa oleh pengulangan menu.

Titik bangun kasur berada di depan ujung kaki kasur, di luar collider kasur. Level 2–4 menghadap ke ruang terbuka dengan kepala kasur di sisi tembok kanan. SleepPose_Editable dan WakeStandPoint_Editable tersimpan sebagai child Bed_MeshSlot pada masing-masing layout dan dapat diedit di Hierarchy. Menu Apply Bed Footboard Exits menerapkan penyesuaian ini sekali. Runtime memvalidasi lantai dan ruang kapsul sebelum menempatkan player.

Pengujian gaya frontal: semua level memakai perspektif yaw 0, posisi kamera gameplay sama dengan marker kamera level, collider lantai tidak berlubang, dan projection/follow kamera world kembali setelah keluar rumah. Laporan lokal: Library/HouseFrontalVerification.txt dan Library/HouseViewRuntimeVerification.txt.

Kamera world dipulihkan beserta posisi dan rotasinya saat keluar interior. TopDownCameraFollow menerapkan kembali pitch/yaw world dan proyeksi zoom saat diaktifkan, sehingga arah follow tidak memakai rotasi kamera rumah yang tertinggal. Verifikasi transisi nyata: masuk/keluar pada 08:00, tidur tanpa save lalu keluar pada 06:00, dan masuk/keluar ulang pada 06:00. Rotasi, zoom/FOV, arah follow, serta directional light world kembali; ambient pada jam yang sama konsisten. Pagi 06:00 tetap lebih redup daripada 08:00 sesuai DayNightCycle. Laporan lokal: Library/HouseCameraReturnAudit.txt.
