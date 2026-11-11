# Satu atlas dan satu material runtime untuk tanaman

Semua prefab tanaman yang dibuat untuk Nature Paradise memakai `NP_Plants_Unified_01.mat`, termasuk semua LOD, lima tahap pertumbuhan, dan objek panen. Material ini juga menjadi tujuan grup berikutnya. Ikon UI tetap menggunakan sistem sprite yang sudah ada.

Atlas runtime 4096 x 4096 terdiri dari BaseColor, Normal, dan SurfaceMask. Daun dan kelopak utama mempertahankan resolusi serta piksel sumber. Hanya bidang kecil seperti batang, serbuk sari, akar dan biji dari halaman detail yang dipadatkan. Bidang ini memperoleh gutter tambahan 16 piksel di luar padding asalnya. Mipmap, streaming mipmap, dan kompresi ASTC 6x6 Android/iOS aktif; Read/Write tekstur mati. Nilai importer 50 adalah ASTC 6x6, sesuai format mobile halaman sumber yang sudah dipakai.

SurfaceMask menggunakan R untuk metallic, G untuk kekuatan normal asal, B untuk saturasi asal dibagi dua, dan A untuk smoothness. Dengan ini satu material mempertahankan pengaturan visual grup berbeda tanpa material per grup. Shader khusus tanaman menyediakan tint lifecycle dan fade sebagai properti instanced. Shader Cozy objek lain tetap seperti sebelumnya.

`AtlasRegistry.json` adalah sumber koordinat yang dikunci. Penambahan slot tidak boleh memindahkan alokasi lama. Source atlas tiap grup beserta master GIMP/Blender tetap menjadi bahan editable di luar jalur binding runtime; runtime memakai satu atlas ini. Halaman/material sumber yang tidak direferensikan prefab tidak perlu dimuat oleh tanaman.

## Grup baru dan revisi berikutnya

1. Buat/revisi model dan tekstur grup menggunakan source editable seperti biasa.
2. Jalankan `prepare_plant_unity.py --group G12` (ganti grup sesuai kebutuhan). Script otomatis menggabungkan permukaan baru, remap UV JSON/FBX, dan membuat native mesh/prefab dengan material bersama.
3. Jalankan audit atlas dan binding gameplay. Jangan langsung menghubungkan prefab dengan material source per grup.
4. Pertahankan `AtlasRegistry.json`; jangan hapus/repack alokasi yang sudah dipakai. Atlas penuh menghasilkan error yang jelas dan tidak membuat material baru secara diam-diam.

Perintah audit: Blender background dengan `Scripts/verify_unified_plant_atlas.py`. Audit ini memeriksa bentuk/index/normal/tangent, remap UV native dan FBX, pixel permukaan utama, padding, importer, prefab, serta kapasitas simulasi grup yang belum dibuat. Seluruh 30 crop G02-G13 dan G01 Blueberry/Blackberry kini memakai 368 mesh dan 184 prefab dengan material yang sama. Raspberry masih menunggu referensi; simulasi menyisakan empat bidang 256px dan empat bidang 128px tanpa memindahkan UV yang sudah dipakai. Baseline dan ZIP sebelum migrasi berada di `Review`; baseline lama tidak ditulis ulang.

Menu Unity: **Nature Paradise > Crops > Validate Shared Atlas**. Ini membaca asset hasil impor dan mencoba varian shader instanced, lalu mencatat `Review/UnityImportAudit.json`. Laporan file/compiler, impor Editor, Play Mode dan profiling HP adalah hasil berbeda. Penyatuan material tidak menjamin semua mesh berbeda dirender dalam satu draw call; instancing mengelompokkan salinan mesh yang sama.

Dokumentasi GPU instancing Unity 6: https://docs.unity3d.com/6000.0/Documentation/Manual/GPUInstancing.html
