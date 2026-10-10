# Tampilan field

Profil: `Assets/Nature  Paradaise/Resources/Profiles/Field Soil Visuals.asset`.
Profil ini dipakai field di Map saat Play. Dirt untuk jalan tetap berasal dari Terrain.

Material aktif memakai tekstur, tint, penggelapan basah, dan smoothness versi sebelumnya. Palet percobaan dimatikan; perubahan aktif berfokus pada bentuk petak, tiling lokal, blending tepi, dan pengeringan permukaan.

Urutan warna di dalam field:

1. **Field belum dicangkul:** paling terang, warna tanah hangat.
2. **Dicangkul, kering:** sedikit lebih gelap dari dasar field.
3. **Baru disiram:** paling gelap.
4. **Mulai mengering:** warna di antara basah dan cangkul kering; akhirnya kembali ke warna cangkul kering.

## Pengaturan Inspector

- **Field Tint / Hoed Tint:** warna dasar field dan petak cangkul.
- **Wet Multiplier:** penggelapan saat baru disiram. Nilai mendekati 1 menghasilkan warna basah lebih terang.
- **Use Cozy Palette:** default **mati**, agar warna dan detail asli material sebelumnya tetap dipakai. Aktifkan hanya jika sengaja ingin mencoba palet warna baru.
- **Texture Contrast / Soil Variation:** kekuatan detail kecil dan variasi warna tanah. Hindari kontras tinggi supaya field tidak terlihat ramai dari kamera gameplay.
- **Tile Inset:** jarak dari tepi cell ke petak tanah. Dalam fraksi ukuran cell, bukan skala mesh acak.
- **Tile Feather / Tile Corner Radius:** transisi tepi dan sudut petak; juga dalam fraksi ukuran cell.
- **Field Feather:** lebar transisi field ke terrain, dalam meter.
- **Furrows:** detail alur kecil pada petak cangkul; dasar field tidak memiliki alur.
- **Field Texture Size / Hoed Texture Size:** ukuran pengulangan tekstur dalam meter lokal field, mengikuti rotasi grid.
- **Dry Moisture / Fully Wet Moisture:** kompatibilitas save lama dan rentang pengeringan permukaan. Durasi pengeringan = selisih kedua nilai dibagi `Hourly Evaporation` pada Soil Profile. Default `(70 - 35) / 2 = 17,5 jam game`. Perubahan warna diproses setiap pergantian jam game. Evaporation 0 menahan pengeringan.

## Data dan gameplay

Air di permukaan (`surfaceWetness`, 0–1) dipisahkan dari kelembapan untuk akar (`moisture`). Penyiraman dengan air, hujan, sprinkler, dan efek moisture positif menggelapkan permukaan langsung, bahkan pada tanah sangat kering. Permukaan mengering bertahap dan tersimpan di save; pergantian hari atau masuk/keluar rumah tidak mereset warnanya. Aturan kebutuhan air, growth, panen, dan kualitas crop tidak berubah.

Save lama tanpa `hasSurfaceWetness` memakai kelembapan lama untuk memperkirakan tampilan awal. Tidak perlu menghapus save.

Mesh mengikuti ketinggian terrain. Petak tetap sejajar grid gameplay, dengan tepi membulat dan sedikit tidak beraturan yang stabil. Material dibagi bersama; kondisi setiap petak memakai MaterialPropertyBlock. Tidak ada prefab tanaman atau petak contoh yang ditambahkan ke save pemain.
