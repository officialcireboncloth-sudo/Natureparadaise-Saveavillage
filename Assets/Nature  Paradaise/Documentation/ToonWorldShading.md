# Toon Farm world shading

World menggunakan shader dari Toon Farm Pack yang sudah mendukung URP 17:
`Toon/CustomToon` untuk objek opaque, `CustomToonVegetation` untuk vegetasi,
dan `CustomToonGrass` untuk rumput/bunga. Lighting ramp dipilih
`TFP_Toon_Ramp_1F`, ramp lembut yang dipakai pack demo. Profil world menyimpan
pilihan ramp dan menu apply material mempertahankan pilihan tersebut.

Material opaque project yang sebelumnya URP Lit dipindahkan ke CustomToon.
Migrasi hanya mengubah aset `.mat`; material embedded FBX tetap mengikuti
importer model dan tidak ditulis ulang sebagai aset material.
Base texture, UV tiling/offset dan tint asli dipertahankan. Shader pack diberi
tambahan `[MainColor] _Color` dan `[MainTexture] _TextureSample`; tint diterapkan
di pass warna dan meta, dengan layout buffer konsisten di semua pass.
Material pack yang sudah menggunakan toon hanya diperbarui ramp-nya.
Specular dimatikan agar surface lebih matte; receive/cast shadow tetap tersedia.

Profil disimpan di `Resources/World/Toon World Style.asset`. Objek generated
opaque memakai `ToonWorldStyle.CreateMaterial`, termasuk pakan kandang,
lonceng hewan, board desa dan properti fishing. Helper editor rumah/player/kolam
tetap mempertahankan setup material dan tekstur ketika aset dibangun ulang.

Air, kaca/transparency, alpha-cutout yang bukan shader toon, particles, terrain
painting, indikator/soil overlays dan visual hujan mempertahankan shader khusus
agar fungsi render dan gameplay tidak hilang. Visual hujan tetap mengikuti
pengaturan disable yang sudah ada. Detail PBR normal/metallic pada material yang
dipindahkan tidak digunakan oleh shader toon pack.

Untuk menerapkan ulang pada material baru gunakan menu
`Nature Paradise > World > Apply Toon Farm Shading`. Operasi ini hanya berjalan
di editor; tidak melakukan scanning/conversion material pada tiap frame.
Backup pertama material dan laporan apply berada di
`Library/ToonWorldMaterialBackups` dan `Library/ToonWorldMaterialAudit.txt`.
Backup Library bersifat lokal dan tidak ikut build/Git.

Lighting outdoor memakai preset stylized di atas dasar shader/ramp `Demo_Gardens`:
sun hangat 1.5, shadow strength 0.65, ambient sky sejuk dan ground hangat,
reflection 0.15. Sudut matahari saat siang minimal 28 derajat supaya pagi
tetap terang. Cuaca dan malam tetap memodulasi intensitas dan warna.
Ambient probe diffuse diperbarui dari sky/equator/ground agar world runtime
tanpa bake menerima fill lighting yang konsisten. Fill malam memakai biru
dengan intensitas bulan 0.7 dan ambient biru muda (0.55, 0.68, 0.86).
Palet pagi menggunakan sinar kuning putih (1, 0.98, 0.9) dan fill krem cerah.
Palet sore menggunakan sinar oranye (1, 0.68, 0.43) dan fill peach cerah,
mulai blend tiga jam sebelum sunset menurut musim. Blend pagi ke siang dan
sore ke malam memakai SmoothStep; night shadow strength 0.4 agar lembut.
Palet ini dapat diatur melalui field `Soft time-of-day palette` pada profil lighting.

Puncak tengah hari dibentuk dengan smooth weight di antara 09:30–14:30,
berpusat pada 12:00. Direct sun mendapat multiplier 1.55 (base 1.5),
elevasi 82 derajat dan roll 0 agar bayangan pendek dekat kaki; shadow strength
naik menjadi 0.75. Fill siang lebih terang dengan sky sejuk dan ground hangat.
Bloom naik dari 0.18 ke 0.42, threshold turun dari 1.1 ke 0.85, dan exposure
bertambah 0.16 pada puncaknya. `GardenWorldLook` mengubah instance profil Volume
runtime, bukan aset; efek peak berkurang saat berawan/hujan dan hilang malam.
Detail grass mengurangi amplifikasi lighting tambahan pack ke 0.65 pada
puncak siang (`NP_TERRAIN_GRASS` saja), supaya hijau tetap terbaca ketika direct
sun lebih kuat. Nilai ini blend dengan peak, kembali ke 1 di luar jam siang,
dan tidak mengubah source material atau jenis/ukuran/density rumput.
PC URP menggunakan shadow distance 100 meter dan atlas utama 4096 agar shadow
player tetap terjangkau kamera world. Ini menambah biaya GPU pass bayangan
dibanding atlas 2048 sebelumnya. Mobile pipeline tidak diubah.
Data dapat diedit pada `Resources/World/Garden Lighting.asset`. Matahari tetap
bergerak mengikuti jam, cuaca, dan musim; orientasi demo adalah acuan jam 12.
Profil post-processing outdoor dinonaktifkan saat player di interior.
Chromatic aberration dimatikan, LUT demo contribution 0.25, vignette 0.08,
bloom 0.18 dengan threshold 1.1, exposure +0.08, contrast +4, saturation +8.
Lima material detail turunan memakai gradasi hijau segar/hijau kuning tanpa
tint hijau kedua dari musim semi. Material sumber pack tidak diubah preset ini.
Scene Map juga menyimpan daylight preset untuk preview Edit Mode. Kamera world
memakai SMAA High untuk merapikan tepi foliage, tanpa mengganti sudut/framing kamera.

Menu `Nature Paradise > World > Apply Stylized Garden Look` menerapkan ulang
warna, lighting dan grading; tidak membangun ulang populasi rumput atau paint mask.
Menu demo garden sebelumnya tetap tersedia untuk mengembalikan dasar demo,
lalu menu stylized dapat digunakan setelahnya. Ini pendekatan visual yang
terinspirasi anime/stylized; bukan implementasi shader internal Genshin.
