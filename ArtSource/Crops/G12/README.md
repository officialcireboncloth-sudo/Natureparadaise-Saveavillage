# G12 - Anggrek / Orchid

Model mengikuti referensi Phalaenopsis pengguna: biji ivory, tunas dua daun, tanaman muda empat daun, lima kuncup pada tangkai melengkung, empat bunga dewasa beserta dua kuncup ujung, dan potongan panen dua bunga beserta dua kuncup. Rumpun memperlihatkan tujuh akar udara mint/cream.

Daun adalah mesh tertutup dan berdaging dengan centerline melengkung, putaran terpisah, serta arah depan/belakang yang berasal dari arah tumbuh terhadap batang dan world-up. Tidak ada orientasi daun yang mengikuti kamera. Setiap bunga mempunyai dua petal lateral, tiga sepal, bibir tiga lobus yang benar-benar melengkung, pusat oranye dan column cream. Sambungan leher bunga/kuncup dengan pedicel diperiksa geometrinya.

Master: `Blender/G12_Orchid_Master.blend`. Enam tampilan, dua LOD per tampilan, batas near 26.000 triangle dan LOD1 2.800. Source GIMP/PNG di `Textures` adalah bahan editable di luar Assets. Runtime memakai **NP_Plants_Unified_01** bersama semua tanaman lain, bukan material baru. Empat permukaan utama (petal, sepal, bibir dan daun) mempertahankan resolusi 512px.

Workflow: `build_g12_blender.py`, `capture_g12_exports.py`, lalu `prepare_plant_unity.py --group G12`. Jangan memindahkan UV yang sudah tercatat di `../Unified/AtlasRegistry.json`. `verify_g12.py` memeriksa struktur sesuai referensi, volume daun/kelopak, sambungan bunga, dan pixel atlas 26 tanaman sebelumnya.

Ikon bibit memakai renderer paper bag lama dengan ilustrasi Anggrek. Item hasil panen/mutasi menggunakan ilustrasi sprig yang sama; ikon UI tetap berupa sprite 256px terpisah dari atlas mesh. Binding baru menggunakan nilai awal bunga yang sudah ada: 5 hari, benih 5 G, hasil 12 G, nonmakanan. Semua 26 profil/ID/balance/icon/hint sebelumnya dipertahankan.

Review: `../Previews/G12_GrowthStages.png`, `G12_SpatialReview.png`, `G12_FlowerAndSeed.png`, `G12_AppliedPreview.png`. Laporan penyelesaian dan hasil file/compiler berada di `Review`; Editor/Play Mode/profiling HP dicatat terpisah.
