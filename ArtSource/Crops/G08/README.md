# G08 - Rice dan Wheat

Sumber: dua gambar tahap pertumbuhan padi dan gandum yang diberikan pengguna pada 10 November 2026. Arah visual: bentuk tegas, detail bulir terbaca, hijau segar dan emas hangat.

Revisi R02 membetulkan Wheat Heading dan Harvest: sumbu kepala mengikuti arah ujung batang, pangkal serta pucuk bulir meruncing, dan tiga tangkai panen membentuk berkas yang membuka perlahan. Model/prefab lain dan seluruh atlas dipertahankan. Perbandingan sebelum-sesudah serta tiga sudut pandang ada pada `G08_Wheat_R02_Comparison.png` dan `G08_Wheat_R02_Alignment.png`.

## Keluaran

Setiap tanaman mempunyai Seed, Sprout, Young, Heading, Ready dan Harvest. Model tambahan: tumpukan sekam padi, beras putih dan biji gandum. Total 15 model dasar, 15 LOD1 dan 15 prefab visual. Tunas serealia memakai dua daun pita dan satu biji yang masih menempel.

- Padi: rumpun radial berdaun pita terlipat, lima malai bercabang, masing-masing 37 sekam dengan pedisel halus. Malai hijau pada Heading dan merunduk keemasan pada Ready. Harvest berisi tiga malai potong.
- Gandum: tiga kepala hijau pada Heading, enam kepala emas yang menyebar pada Ready. Setiap kepala mempunyai 19 spikelet/biji berisi, belahan tengah dan 18 serabut halus. Harvest berisi tiga tangkai potong.
- Daun pangkal mengisi rumpun; daun bagian atas mengikuti posisi batang yang melengkung. Susunan diperiksa dari depan, samping, belakang dan atas.

## Sumber dan runtime

| Isi | Lokasi relatif root project |
| --- | --- |
| Master editable | ArtSource/Crops/G08/Blender/G08_Cereals_Master.blend |
| Rencana tahap dan binding prefab | ArtSource/Crops/G08/Blender/ |
| Tiga XCF editable | ArtSource/Crops/Textures/NP_Plants_Cereal_01_*.xcf |
| Sumber serat, pola dan provenance | ArtSource/Crops/Textures/Generated/G08/ |
| Tiga map PNG dan manifest UV | Assets/Nature  Paradaise/Textures/Shared/Plants/NP_Plants_Cereal_01_* |
| Material | Assets/Nature  Paradaise/Material/Shared/Plants/NP_Plants_Cereal_01.mat |
| FBX, JSON dan Mesh native | Assets/Nature  Paradaise/mesh/Crops/G08/ |
| Prefab dua LOD | Assets/Nature  Paradaise/Prefabs/Crops/G08/ |

Atlas independen 2048 x 2048 berisi 16 daerah 512px, inset UV 16px. BaseColor, Normal dan SurfaceMask memakai layout sama. Kanal R mask adalah metallic nol, A adalah smoothness. Tiap tubuh tanaman memakai satu material. LOD1 dibatasi 2800 triangle atau lebih rendah sesuai ukuran model.

Bulir, alur utama dan serat daun memakai geometri serta tekstur terdaftar. Sumber tekstur dibuat khusus untuk batch ini, disusun dan dilengkapi di GIMP; provenance berada bersama sumber. Master Blender menyertakan ketiga map packed. Kandidat gandum vendor disimpan dalam koleksi tersembunyi sebagai pembanding dan dikeluarkan dari ekspor.

## Preview

Semua gambar model merupakan render langsung dari master, tanpa retouch bentuk atau warna setelah render.

| Pemeriksaan | File pada ArtSource/Crops/Previews/ |
| --- | --- |
| Tanaman matang | G08_ReadyReview.png dan G08_Closeup.png |
| Lima tahap dan hasil potong | G08_GrowthStages.png |
| Empat arah | G08_VolumeReview.png |
| Panen dan tiga tumpukan biji | G08_HarvestStudy.png |
| Warna dan normal permukaan | G08_SurfaceDetail.png |
| Detail sekam dan biji | G08_GrainDetail.png |

## Pemeriksaan dan batas cakupan

Review/ArtAudit.json memeriksa preservasi aset terdahulu, warna/normal/mask, struktur malai, bulir, serabut, volume rumpun, UV, buffer Mesh native, hubungan prefab, LOD dan roundtrip seluruh FBX. GimpReopenAudit.json mencatat pembukaan ulang tiga XCF. FinalReview.json mengikat laporan dan preview pada hash file terakhir. Pemeriksaan teknis tidak mengukur kesamaan artistik terhadap referensi.

Pembersihan ekspor membuang vertex terisolasi yang dapat tertinggal setelah decimation serabut tipis. Dengan demikian batas FBX dan buffer Mesh native sama; audit juga memeriksa tidak ada vertex tanpa face pada FBX yang dibuka ulang.

`Review/R02_AlignmentAudit.json` memeriksa sambungan permukaan dan kesinambungan sumbu tiga pasangan batang/kepala di setiap tahap yang direvisi. `R02_Baseline.json` mencatat hash aset lain dan metadata target; `R02_EditablePreservation.json` mencatat 13 root editable lain yang dipertahankan. Salinan sebelum revisi berada di `ArtSource/Crops/Revisions/Before_G08_R02`.

Batch ini menyiapkan aset visual. Profil tanaman, item, harga, toko, balance dan scene gameplay dipertahankan. Import Unity Editor dan Play Mode belum dijalankan; statusnya dicatat tersendiri.

## Pembuatan ulang

Jalankan dari root repository dengan Python 3, Blender 4.5 dan GIMP 3.

1. Python: ArtSource/Crops/Scripts/prepare_g08.py. Baseline dibuat sekali, jangan ditimpa untuk meloloskan perubahan tidak sengaja.
2. GIMP python-fu-eval: paint_g08_gimp.py, dengan __file__ menunjuk path script. Memerlukan Cereal_Source_Sheet.png yang disimpan bersama sumber.
3. Python: prepare_g08_unity_page.py.
4. Blender background: build_g08_blender.py.
5. Python: prepare_plant_unity.py --group G08, kemudian finalize_g08.py.
6. Blender background: verify_g08_blender.py dan render_g08.py.
7. GIMP python-fu-eval: review_g08_gimp.py. Periksa gambar sebelum menulis review final.

Konfigurasi atlas berada di g08_config.py, konstruksi model di g08_geometry.py. Grup sebelumnya tidak memakai halaman atlas baru ini.
