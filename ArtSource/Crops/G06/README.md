# G06 - Radish dan Onion

Source Blender: [G06_RootVegetables_Master.blend](Blender/G06_RootVegetables_Master.blend).

Dua gambar yang diunggah langsung pengguna menjadi acuan bentuk, warna dan tahap pertumbuhan. Setiap tanaman memiliki lima tahap pertumbuhan, panen utuh dan potongan setengah. Kemiripan diperiksa secara visual; pemeriksaan teknis tidak mengukur kesamaan pixel dengan ilustrasi.

| Tanaman | Tahap pertumbuhan | Ciri khas |
| --- | --- | --- |
| Radish | Seed, Sprout, Young, RootForming, Ready | Biji tan bulat; tunas dua daun; daun lebar berlekuk; umbi bulat merah-putih; akar runcing melengkung. Panen utuh mempertahankan daun dan diletakkan miring; potongan berdaging putih |
| Onion | Seed, Sprout, Young, BulbForming, Ready | Biji hitam bersudut; tunas melingkar; daun tabung dengan ujung berongga; daun luar melengkung dan berujung keemasan; kulit berserat; akar krem. Panen memakai leher kering tanpa daun hijau; potongan bercincin |

## Aset dan preview

| Keluaran | Lokasi |
| --- | --- |
| Master, manifest dan binding tahap | Blender/ |
| 14 model dasar dan 14 LOD1, FBX/JSON/native Mesh | Assets/Nature  Paradaise/mesh/Crops/G06/ |
| 14 prefab visual dengan dua LOD | Assets/Nature  Paradaise/Prefabs/Crops/G06/ |
| Tanaman matang | ArtSource/Crops/Previews/G06_Closeup.png |
| Seluruh tahap | ArtSource/Crops/Previews/G06_GrowthStages.png |
| Depan, samping, belakang dan atas | ArtSource/Crops/Previews/G06_VolumeReview.png |
| Panen utuh dan potongan | ArtSource/Crops/Previews/G06_HarvestStudy.png |
| Detail warna dan normal | ArtSource/Crops/Previews/G06_SurfaceDetail.png |

## Patokan revisi sebelumnya

Daun melengkung dan tersebar ke berbagai arah sejak tahap muda. Kedalaman kanopi matang sekitar 75% dari lebar untuk Radish dan 84% untuk Onion, diukur pada vertex daun asli dan diperiksa lewat render empat arah. Radish mempunyai 2/4/4/6 daun pada Sprout/Young/RootForming/Ready; Onion mempunyai 3/4/5 daun tabung pada Young/BulbForming/Ready.

Warna berasal dari albedo dan mask material sebenarnya. Urat daun, pori kulit Radish dan serat halus Onion memakai warna/normal dengan UV terdaftar. Detail urat tidak menggunakan mesh mikro. Preview memakai pencahayaan studio netral dengan view Standard.

G06 memakai atlas/material NP_Plants_Trellis_01 existing, berukuran 2048, tanpa halaman baru. Hanya tujuh slot yang sebelumnya tidak dipakai G03/G04 yang berubah: LeafLobed, LeafStrap, RadishGradient, OnionSkin, RadishCut, OnionCut, RootCream. Semua pixel di luar slot tersebut diperiksa terhadap snapshot sebelum G06. Koordinat UV, master Blender dan aset native grup sebelumnya dipertahankan.

Tiga source GIMP berlapis berada di ArtSource/Crops/Textures/NP_Plants_Trellis_01_*.xcf. Sumber vector ada di ArtSource/Crops/Textures/Generated/G06/. Blender menyimpan ketiga map secara packed dan koleksi tersembunyi kandidat pack untuk perbandingan.

## Validasi dan pemakaian

Review/ArtAudit.json memeriksa file sebelumnya, batas perubahan pixel, volume daun, UV, buffer native, GUID prefab, LOD dan roundtrip 28 FBX. BlenderReopenAudit.json mencatat pembukaan ulang master; GimpReopenAudit.json memeriksa source GIMP. Status gabungan ada di BatchStatus.json.

Prefab ini merupakan aset visual yang telah disiapkan. Profil crop, item, toko bibit, balance dan scene gameplay belum diubah untuk G06. Import langsung melalui Unity Editor dan Play Mode belum dijalankan pada batch ini.

## Pembuatan ulang

Jalankan dari root repository:

1. Python: ArtSource/Crops/Scripts/prepare_g06.py. Snapshot awal dibuat sekali; pertahankan snapshot untuk menilai batas perubahan.
2. GIMP python-fu-eval: paint_g06_gimp.py melalui runpy.run_path dengan run_name='__main__'.
3. Blender background: build_g06_blender.py.
4. Python: prepare_plant_unity.py --group G06, lalu finalize_g06.py.
5. Blender background: render_g06.py.
6. GIMP: review_g06_gimp.py.
7. Blender background: verify_g06_blender.py.

Modul bersama: plant_library.py, plant_blender_common.py dan unity_art_io.py. Pembuatan ulang memerlukan atlas, source GIMP, kandidat pack dan material existing. Master tersimpan dapat dibuka dengan ketiga map packed.