# Lavender G13

Lavender mengikuti gambar pertumbuhan pengguna: tiga biji cokelat lonjong, sprout dua daun, tanaman muda, lima kepala kuncup, sembilan tangkai bunga siap panen, dan empat tangkai potong. Daun sage berbentuk lensa tertutup yang melengkung dan menyebar dalam ruang. Setiap kepala bunga siap panen memiliki enam tingkat kuntum dan tiga kuntum ujung; kuntum mengelilingi tangkai secara 3D.

Master editable: `Blender/G13_Lavender_Master.blend`. Tekstur warna, normal dan surface mask beserta tiga master XCF berada di `Textures`. Dua permukaan utama mempertahankan resolusi sumber 512px. Halaman sumber tetap di luar Assets; semua mesh, prefab, LOD dan hasil panen memakai `NP_Plants_Unified_01` bersama tanaman lain. 130 alokasi UV dan seluruh piksel peta sebelumnya dipertahankan.

Runtime: `Assets/Nature  Paradaise/mesh/Crops/G13` dan `Assets/Nature  Paradaise/Prefabs/Crops/G13`. Batas mesh dekat 26.000 segitiga, LOD1 maksimal 2.800. Profil `Lavender.asset` terhubung ke lima fase field; item normal/mutasi memakai model panen. Bibit dijual pada tab Benih yang sudah ada. Ikon 256px memakai paper bag yang sama, dengan master GIMP di `ArtSource/UIIcons/Seeds/Masters`.

Balance awal mengikuti bunga sebelumnya: panen pertama 5 hari, bibit 5 G, hasil 12 G, nonmakanan. Baris 27 crop sebelumnya tetap. Manifest dan pemeriksaan terakhir tercatat di `Review/Completion.json` dan `../Integration/G13_Completion.json`.

Preview: `../Previews/G13_GrowthStages.png`, `../Previews/G13_SpatialReview.png`, `../Previews/G13_FlowerAndSeed.png`, dan `../Previews/G13_AppliedPreview.png`. Preview berasal dari mesh ekspor dan material atlas runtime. Kompilasi dan pemeriksaan file/aturan crop terpisah dari Unity Editor, Play Mode dan profiling HP; tiga pemeriksaan terakhir belum dijalankan oleh agent.

Build: `prepare_g13.py`, GIMP `paint_g13_gimp.py`, Blender `build_g13_blender.py`, `capture_g13_exports.py`, lalu `prepare_plant_unity.py --group G13`. Jangan menjalankan capture setelah UV menjadi atlas bersama. `verify_g13.py` memeriksa source geometry dan preservasi atlas; `verify_unified_plant_atlas.py` memeriksa native/FBX dan seluruh crop.
