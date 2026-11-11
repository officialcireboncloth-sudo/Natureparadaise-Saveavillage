# G09 - Rose, Hibiscus dan Jasmine

Sumber visual: tiga gambar tahap pertumbuhan mawar, kembang sepatu dan melati yang diberikan pengguna pada 10 November 2026. Arahan: bentuk jelas dan tegas, detail tekstur terbaca, warna hidup.

## Model

Setiap tanaman mempunyai Seed, Sprout, Young, BudForming, Ready dan Harvest. Total 18 model dasar, 18 LOD1 dan 18 prefab visual. Model dibuat khusus per jenis, termasuk biji dan tunas dua daun halus.

- Rose: daun majemuk bergerigi, tunas daun hangat, duri perunggu, tiga kuncup pada BudForming, tiga bunga merah dan satu kuncup pada Ready. Kelopak bertumpuk dan melengkung dengan hati spiral tergulung. Harvest berupa satu tangkai panjang berduri, daun dan bunga.
- Hibiscus: daun lebar bergerigi, empat kuncup pada BudForming, tiga bunga merah dan tiga kuncup pada Ready. Masing-masing bunga mempunyai lima kelopak berombak, corong merah, kolom benang sari, 36 anther kuning dan lima bantalan stigma merah. Harvest berupa satu bunga bertangkai dengan dua daun.
- Jasmine: semak rendah dengan daun oval mengilap, 14 kuncup pada BudForming, lima bunga putih berlapis dan sepuluh kuncup pada Ready. Setiap bunga mempunyai 18 kelopak dan tengah krem. Harvest berupa ranting berdaun, dua bunga dan tiga kuncup.

Tajuk disusun radial dengan kelengkungan dan ketebalan daun nyata. Daun yang menembus bagian tengah bunga terbuka dihilangkan bersama tangkai daunnya. Sambungan tangkai/calyx dibuat dari satu titik dan sumbu yang sama; hal ini juga diperiksa dari geometri asli.

### Revisi bentuk R02

R02 membangun ulang Young, BudForming, Ready dan Harvest pada tiga jenis. Kelopak terbuka memakai cangkang tertutup dua permukaan dengan ketebalan nyata dan ujung membulat. Mawar mempunyai lapisan yang semakin tegak menuju pusat spiral; hibiscus mempunyai lima lobus terpisah pada siluet; melati mempunyai kelopak putih yang lebih berisi dan pusat kecil. Cabang samping melengkung, ukuran serta arah daun bervariasi, dan daun tumbuh sepanjang cabang bunga/kuncup. Tajuk mawar dan hibiscus dipadatkan secara vertikal.

Seed dan Sprout tetap memakai enam sumber serta ekspor lama. Atlas, sumber XCF, material dan metadata GUID tetap. Snapshot R01 berada pada ArtSource/Crops/Revisions/Before_G09_R02. R02_Baseline.json dan R02_FormAudit.json mencatat batas revisi serta preservasi byte; dua gambar G09_R02_Comparison.png dan G09_R02_FlowerComparison.png memperlihatkan render sebelum/sesudah.

## File

| Isi | Lokasi relatif root project |
| --- | --- |
| Master Blender editable | ArtSource/Crops/G09/Blender/G09_FloweringShrubs_Master.blend |
| Manifest model, rencana tahap dan binding | ArtSource/Crops/G09/Blender/ |
| Sumber enam permukaan dan provenance | ArtSource/Crops/Textures/Generated/G09/ |
| Tiga XCF berlapis | ArtSource/Crops/Textures/NP_Plants_ShrubFlowers_01_*.xcf |
| Atlas PNG dan daerah UV | Assets/Nature  Paradaise/Textures/Shared/Plants/NP_Plants_ShrubFlowers_01_* |
| Material | Assets/Nature  Paradaise/Material/Shared/Plants/NP_Plants_ShrubFlowers_01.mat |
| Mesh native, FBX, JSON dan LOD | Assets/Nature  Paradaise/mesh/Crops/G09/ |
| Prefab dua LOD | Assets/Nature  Paradaise/Prefabs/Crops/G09/ |

Atlas independen 2048 x 2048 berisi 16 daerah 512px dengan inset UV 16px. BaseColor, Normal dan SurfaceMask mempunyai layout sama. Kanal R mask adalah metallic nol dan A adalah smoothness. Detail serat kelopak serta urat daun memakai sumber baru yang disusun dalam GIMP; pola pendukung disimpan sebagai sumber vektor. Kelopak dan siluet daun memakai geometri nyata. Ketiga map PNG dipack dalam master Blender.

Satu material dua sisi dipakai per tubuh tanaman, termasuk bagian dalam corong serta belakang kelopak. Shader Cozy membalik normal sisi belakang saat pencahayaan. LOD1 dibatasi 2800 triangle atau lebih rendah sesuai model. Halaman atlas dan keluaran kelompok sebelumnya dipertahankan.

## Preview dan pemeriksaan

| Gambar aktual pada ArtSource/Crops/Previews/ | Isi |
| --- | --- |
| G09_Closeup.png | Ketiga tanaman matang |
| G09_GrowthStages.png | Seluruh 18 tampilan tahap dan panen |
| G09_VolumeReview.png | Depan, samping, belakang dan atas |
| G09_HarvestStudy.png | Tiga bentuk bunga potong |
| G09_FlowerDetail.png | Geometri kelopak dan bagian tengah bunga |
| G09_SurfaceDetail.png | Warna dan normal terdaftar |

Preview merupakan render model sebenarnya dalam studio netral, tanpa retouch bentuk atau warna setelah render. Kemiripan artistik dengan referensi diperiksa secara visual; angka pemeriksaan teknis tidak mengukur kesamaan gambar.

Review/ArtAudit.json memeriksa preservasi aset lama, warna/normal/mask, tahap, jenis bunga/kuncup/kelopak, duri, anther/stigma, volume tajuk, sambungan calyx, UV, buffer native, LOD, prefab dan roundtrip seluruh FBX. GimpReopenAudit.json mencatat pembukaan ulang tiga sumber XCF. FinalReview.json mencatat hash master dan preview akhir.

Batch ini menyiapkan aset visual. Profil tanaman, item, toko, harga, balance dan scene gameplay dipertahankan. Import Unity Editor serta Play Mode belum dijalankan dan dicatat terpisah dari pemeriksaan file.

## Pembuatan ulang

Jalankan dari root repository menggunakan Python 3, Blender 4.5 dan GIMP 3.

1. Python: prepare_g09.py; baseline dibuat sekali dan tidak ditimpa.
2. GIMP python-fu-eval: paint_g09_gimp.py dengan __file__ menunjuk path script. Memerlukan ShrubFlowers_Source_Sheet.png yang disimpan bersama sumber.
3. Python: prepare_g09_unity_page.py.
4. Blender background: build_g09_blender.py. Gunakan --draft untuk menunda ekspor, render_g09.py --quick untuk review, kemudian export_g09_blender.py untuk ekspor dari master yang telah ditinjau.
5. Python: prepare_plant_unity.py --group G09 dan finalize_g09.py.
6. Blender background: verify_g09_blender.py dan render_g09.py.
7. GIMP: review_g09_gimp.py; periksa gambar, kemudian Python final_review_g09.py.

Untuk membangun ulang revisi aktif R02 setelah baseline R01 tersedia: Blender build_g09_r02.py (opsional --draft untuk tiga Ready saja), lalu export_g09_r02.py; Python prepare_plant_unity.py --group G09; Blender verify_g09_blender.py, verify_g09_r02_blender.py dan render_g09.py; GIMP review_g09_gimp.py serta review_g09_r02_gimp.py; terakhir Python final_review_g09.py. Draft terpisah dapat diperiksa dengan render_g09.py --r02-draft --quick atau --r02-draft --flower-only --clay. Ekspor R02 mengevaluasi modifier dan menggabungkan bagian per fase dalam satu batch; Seed/Sprout tidak diekspor ulang.

Konfigurasi berada di g09_config.py; bentuk R01 di g09_geometry.py dan revisi aktif di g09_r02_geometry.py. Helper tabung/biji berasal dari pustaka G08; tidak ada perubahan pada master atau aset G08.
