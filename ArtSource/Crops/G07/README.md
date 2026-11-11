# G07 R02 - Bayam dan kangkung

Master aktif: [G07_LeafyVegetables_Master.blend](Blender/G07_LeafyVegetables_Master.blend).

R02 memperbaiki masukan pengguna: bentuk bayam belum mengikuti referensi, tekstur kurang rinci dan warna kusam; kangkung terlihat seperti bambu. Perubahan dibuat pada geometri, albedo, normal dan material yang diekspor.

| Bagian | Perubahan R02 |
| --- | --- |
| Bayam | Roset lebih rapat, 16 daun matang, pangkal lebih pendek, daun lebih lebar dan berisi dengan cekungan urat serta permukaan menggembung |
| Kangkung | 9 batang utama berlengkung, diameter utama sekitar 41% lebih kecil, 54 daun matang, percabangan tipis dan bekas ruas halus |
| Tekstur | Lembar sumber daun baru, siluet dan UV terdaftar, urat sekunder/tersier, butiran permukaan dan relief |
| Warna/material | Hijau lebih hidup dari albedo; smoothness daun 0.52/0.46; normal lebih terbaca pada material Unity |
| Panen | Buket 8 tangkai bayam, buket 9 batang kangkung dengan rongga asli, daun lepas dan ranting 2 daun |

## Atlas

G07 sekarang memakai satu material dan halaman tersendiri: NP_Plants_Leafy_01. Halaman BaseColor, Normal dan SurfaceMask masing-masing 2048. Dua daerah utama daun masing-masing 1024; delapan daerah pendukung masing-masing 512. Daerah pendukung mencakup daun muda, batang, rongga, permukaan potong dan biji.

Siluet PNG dibaca menjadi profil geometri; UV memakai posisi gambar yang sama. Kurva makro dan gelembung permukaan dibentuk pada mesh, sementara detail halus tetap berada pada warna/normal. Source Blender menyimpan tiga map secara packed.

Daun sumber, profil kontur dan provenance berada di ArtSource/Crops/Textures/Generated/G07_R02/. Source GIMP berlapis berada di ArtSource/Crops/Textures/NP_Plants_Leafy_01_*.xcf. Atlas Trellis beserta source GIMP dikembalikan byte-identik ke keadaan sebelum G07; grup sebelumnya mempertahankan permukaannya.

## Keluaran aktif

- 12 tampilan tahap dan panen, ditambah dua model lepas.
- 14 model dasar serta 14 LOD1; FBX, JSON dan native Mesh di Assets/Nature  Paradaise/mesh/Crops/G07/.
- 14 prefab visual di Assets/Nature  Paradaise/Prefabs/Crops/G07/.
- GUID aset G07 dipertahankan saat mengganti mesh dan material.

| Preview | ArtSource/Crops/Previews/ |
| --- | --- |
| Sebelum dan sesudah | G07_R02_Comparison.png |
| Tanaman matang | G07_Closeup.png |
| Seluruh tahap | G07_GrowthStages.png |
| Empat arah | G07_VolumeReview.png |
| Panen dan model lepas | G07_HarvestStudy.png |
| Warna dan normal | G07_SurfaceDetail.png |

## Pemeriksaan

Review/ArtAudit.json memeriksa aset/grup sebelumnya, pemulihan atlas lama, GUID G07, daerah UV baru, warna/normal/mask, volume kanopi, jumlah daun, rongga batang, buffer native, LOD dan 28 roundtrip FBX. Review/GimpReopenAudit.json memeriksa 10 layer daerah warna/mask serta 2 layer normal/height. Review/FinalReview.json mencatat versi file yang diberikan.

Perbandingan gambar adalah render model sebenarnya dalam studio netral yang sama. Kemiripan dengan dua gambar referensi diperiksa secara visual; angka pemeriksaan file tidak mengukur kesamaan artistik atau pixel-identik.

Import melalui Unity Editor dan Play Mode belum dijalankan. Profil crop, item, toko, balance dan scene gameplay belum diubah pada revisi ini.

## Pembuatan ulang

Dari root repository:

1. GIMP python-fu-eval: paint_g07_r02_gimp.py melalui runpy.run_path dengan run_name='__main__'.
2. Blender background: extract_g07_r02_contours.py.
3. Python: prepare_g07_r02_unity.py.
4. Blender background: build_g07_blender.py.
5. Python: prepare_plant_unity.py --group G07, lalu finalize_g07.py.
6. Blender background: verify_g07_blender.py, kemudian render_g07.py.
7. GIMP: review_g07_gimp.py; periksa hasil gambar sebelum menulis review final.

Modul g07_r02_layout mengatur UV dan lookup LOD tanpa mengubah helper atau grup sebelumnya. Konstruksi daun/batang aktif berada di g07_r02_geometry. prepare_g07_r02.py menyimpan snapshot R01 sekali; jangan menimpa snapshot untuk meloloskan perubahan tidak disengaja. Audit preservasi dan gambar perbandingan memerlukan snapshot lokal Before_G07_R02 dan Before_G07. Master aktif dapat dibuka langsung dengan map packed.