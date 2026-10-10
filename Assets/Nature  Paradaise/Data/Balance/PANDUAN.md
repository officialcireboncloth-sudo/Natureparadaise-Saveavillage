# Panduan data CSV untuk designer

CSV adalah tabel untuk mengedit angka balancing tanpa mengubah kode. Unity tetap menjalankan data asset; perubahan spreadsheet baru masuk game setelah Import. Buka **Nature Paradise > Data CSV > Balance CSV Panel**.

## Mulai dari sini

1. Keluar dari Play Mode. Commit perubahan project yang ingin kamu simpan.
2. Klik Export pada jenis data yang ingin diedit. Export mengambil nilai aktual dari asset; akan menimpa CSV lama, jadi jangan export sebelum mengimpor edit spreadsheet yang belum diterapkan.
3. Klik Buka folder CSV. Buka file di Excel/Google Sheets sebagai tabel, delimiter koma, encoding UTF-8. Jangan membuka ulang lalu menyimpan ID sebagai angka.
4. Untuk harga item, gunakan Items.csv: cari display_name, ubah buy_price / sell_price. Jangan mengubah item_id atau asset_path lama.
5. Untuk hewan/drop/upgrade gunakan folder Settings. Cari asset_name dan label, baca description. Ubah **hanya value**; kolom lain adalah identitas/petunjuk.
6. Simpan sebagai CSV UTF-8 dengan pemisah koma. Desimal memakai titik (0.25), boolean TRUE/FALSE. Tutup file di Excel.
7. Klik Validate untuk Items/Crops/Fish/Recipes, atau Preview perubahan settings. Perbaiki error yang menyebut file/baris; tidak ada perubahan asset jika validasi gagal.
8. Klik Import jenis data tersebut. Cek Inspector dan mainkan game. Commit CSV dan asset yang berubah bersama-sama.

Contoh: harga Carrot Seed 120 → 150 di buy_price. Drop rumput 1–1 → 2–4: pada Settings/Gatherables.csv cari asset Wild Grass Balance, ubah value minimumAmount menjadi 2 dan maximumAmount menjadi 4. minimum tidak boleh lebih besar dari maximum. Peluang 0.3 berarti 30%, bukan 30 unit.

Settings menyimpan satu setting per baris. asset_guid + object_id + setting menunjuk asset/component asli. Array.data[0] berarti entri pertama pada daftar Inspector. Jumlah baris bukan jumlah hewan di map. Menghapus baris tidak menghapus asset. Menambah jenis hewan/produk/drop entry dilakukan di Inspector dahulu, lalu Export ulang agar identitasnya benar. Import angka tidak mengganti model, ikon atau material.

## Isi setiap tugas pada daftar perencanaan

| Tugas | Deskripsi / apa yang diedit | Status jalur CSV |
|---|---|---|
| Master item | Nama, kategori, harga beli/jual, stack maksimum, pemulihan HP/stamina/hunger, syarat unlock, relasi bibit | Items.csv; exporter/importer yang sudah ada |
| Hewan | Durasi tumbuh, interval produksi, harga jual, aturan penyakit, pakan; jumlah susu/produk dan wool pada prefab AnimalController | Settings/Animals.csv; harga beli hewan di Shops.csv |
| Weather schedule 1–10 years | Override kondisi cuaca per tanggal; Auto mengikuti generator cuaca game | Schedules/WeatherSchedule.csv, 1120 tanggal |
| Festival schedule 1 year | ID acara, tanggal, jam, lokasi, judul/deskripsi; year 0 berulang setiap tahun | Schedules/FestivalSchedule.csv; tampil di kalender, bukan sistem minigame/hadiah festival |
| Resep makanan | Bahan dan jumlah, hasil, jumlah hasil, waktu masak, syarat kitchen/peralatan | Recipes.csv |
| Menu café | Kategori, ItemSO makanan siap konsumsi, harga, quality, batas jumlah pesanan, penyajian, gambar | Schedules/CafeCategories.csv dan CafeMenu.csv; jam buka di Cafe Catalog Inspector |
| Tanaman | Hari panen/regrow, yield, kebutuhan air/fertilitas, cuaca, kualitas | Crops.csv |
| Fish | Habitat, musim/cuaca/jam, rarity, level rod, kesulitan, rentang ukuran | Fish.csv |
| Pohon | Lama pertumbuhan/buah; durability batang/tunggul, kebutuhan axe, rentang drop Wood dan respawn prefab | Settings/Trees.csv |
| Kalender/tanggal 1–10 tahun | 28 hari/musim, 4 musim/tahun; catatan per tanggal dan relasi acara | Schedules/CalendarDates.csv, 1120 tanggal; edit note, jangan identitas tanggal |
| Feed maker | Jumlah bahan, jumlah output, waktu produksi dalam jam | Settings/FeedMaker.csv |
| Quest | Jumlah objective, reward angka dan aturan quest yang tersedia pada asset | Settings/Quests.csv; struktur objective/referensi/teks tetap di Inspector |
| Upgrade tools | Level maksimum, gold, jumlah material dan unlock | Settings/ToolUpgrades.csv |
| Property/village upgrade | Biaya gold/material, hari konstruksi, kapasitas/unlock level | Settings/Buildings.csv dan Progression.csv jika asset tersedia |
| Mining | Durability batu, damage hammer, syarat level, chance dan jumlah drop, hari respawn | Settings/Gatherables.csv untuk prefab batu WorldGatherable; jadwal/dungeon mining belum tersedia |
| Tumbuhan liar | Chance/rentang drop dan respawn Wild Grass Balance untuk gulma otomatis; prefab WorldGatherable jika tersedia | Settings/Gatherables.csv |

File settings hanya dibuat jika asset jenis tersebut benar-benar ditemukan. Data scene tanpa prefab tidak ikut diubah. Instance yang punya override di Inspector tetap memakai override tersebut; Apply override ke prefab bila ingin memakai default bersama. Mengubah prefab tidak mengubah hewan yang sudah tersimpan di save secara otomatis.

## Jangan menyamakan istilah

- **HP hewan:** sistem sekarang memakai sehat/sakit dan tahapan penyakit, bukan HP combat 0–100. CSV menampilkan aturan penyakit yang memang dipakai; tidak membuat kolom HP fiktif. HP restore item adalah pemulihan HP player saat makan.
- **Quantity hewan:** kapasitas kandang ada di Buildings; harga hewan di Shops; hasil susu/wool di Animals. Jumlah hewan hidup adalah state save dan pembelian, bukan angka balancing tunggal.
- **Drop amount:** hasil resource di Gatherables/Trees; hasil panen di base_yield Crops; hasil resep di result_amount Recipes. max_stack hanya batas satu slot inventory.
- **Harga shop:** override lokal dalam Shops.csv dapat mengalahkan harga Items.csv. -1 pada override berarti mengikuti harga item. AnimalShopOffer memakai harga sendiri.
- **Icon/image:** tetap gunakan slot Inspector. CSV angka tidak mengubah artwork.

## Batas dan pemulihan

Settings memvalidasi identitas asli, duplikat, angka finite, TRUE/FALSE, Min/Range Inspector, chance 0–1 dan pasangan minimum/maximum sebelum menulis. Preview menampilkan nilai lama → baru. Struktur list harus sama dengan asset saat export. Import membuat salinan asset + meta di BalanceBackups sebelum perubahan dan menyediakan Undo. Backup ini lokal, bukan asset Unity. Jika ingin memulihkan setelah menutup Unity, gunakan commit Git sebelumnya atau salinan backup (jangan mengganti GUID).

Ini validasi angka dan struktur, bukan pengganti playtest ekonomi. Kolom array/enum/referensi visual pada Settings tidak diedit lewat CSV; importer khusus Items/Crops/Fish/Recipes tetap mengelola enum dan referensinya sesuai schema masing-masing.

## Kalender dan café: langkah pengisian

Di Balance CSV Panel gunakan **Export / Validate / Import kalender / cuaca / festival / cafe**. Kelima file dalam folder Schedules divalidasi bersama sebelum kedua asset ditulis. Export menimpa CSV: import edit yang belum diterapkan dahulu. Import menyediakan Undo dan backup asset+meta lokal di BalanceBackups.

- **CalendarDates.csv**: sudah terisi tahun 1–10. `absolute_day` dihitung game; `season` 0=Semi, 1=Panas, 2=Gugur, 3=Dingin; `weekday` 0=Senin .. 6=Minggu. Edit `note` untuk catatan di panel detail. Jumlah hari mengikuti game yang sudah ada, tidak diubah hanya lewat CSV.
- **WeatherSchedule.csv**: edit `weather` menjadi Auto, Sunny, PartlyCloudy, Heatwave, Drizzle, Rain, HeavyRain, WindRainStorm, Cyclone, Thunderstorm, Snow atau Blizzard. Auto mempertahankan generator berbobot musim. Override dipakai saat forecast hari tersebut dibuat; forecast yang sudah tersimpan dalam save tidak diganti diam-diam. Debug season tetap mengikuti mode debug.
- **FestivalSchedule.csv**: awalnya hanya header. Tambah baris `event_id` unik, `year` 0 untuk setiap tahun atau 1–10 khusus, `season` 0–3, `day` 1–28, `type` Festival/Birthday/SeasonDay, judul, deskripsi, lokasi dan jam desimal 0–24. `start_hour` harus <= `end_hour`. Acara muncul di tanggal dan detail kalender; Enter menggeser detail jika satu hari memiliki beberapa acara. Jadwal ini tidak membuat gameplay festival, peserta atau hadiah.
- **CafeCategories.csv**: kategori awal food/Makanan, drink/Minuman, set/Paket. Bisa tambah/kurangi baris; UI tab mengikuti daftar dan urutannya. Jangan hapus kategori yang masih dirujuk CafeMenu.
- **CafeMenu.csv**: kosong sesuai permintaan. Tambahkan `offer_id` unik, `category_id`, `item_id` dari Items.csv, `price` (-1 ikut buyPrice, 0 gratis), `quality` 0–5, `maximum_quantity` 1–99, `enabled`, `allow_dine_in`, `allow_takeaway` TRUE/FALSE. Item harus siap konsumsi (`ItemSO.CanConsume`), bukan bahan mentah. Menu mengacu data aktual, tidak membuat nama/harga contoh. Makan di sini satu porsi memulihkan status; bawa pulang masuk inventory sesuai quantity/quality. Gold, kapasitas tas, jam buka dan ketersediaan penyajian diperiksa sebelum transaksi.

Kolom `image_guid` / `icon_guid` opsional: kosong berarti tidak memakai gambar. Isi GUID asset Sprite tunggal; untuk atlas dengan banyak sub-sprite pilih gambar langsung di Inspector agar referensinya tepat. Export mempertahankan referensi sub-sprite yang sudah dipilih bila GUID tidak berubah. Gambar menu memakai illustration, lalu inventoryIllustration/icon ItemSO sebagai fallback. Slot background, portrait, ikon shop/gold tetap di Cafe Catalog; artwork acara di Game Schedules. Jangan menaruh path file PNG di kolom GUID.

**Akses UI:** di Map, buka hierarchy SHOPS_TESTING → 06_Cafe_Shop, mendekat lalu E atau gunakan Open Cafe For Testing di Inspector saat Play. Di HouseInterior, setiap InteriorLayout_Lv memiliki Calendar_Interactable_Editable di dinding; mendekat lalu E. Kedua Inspector punya Preview UI In Hierarchy untuk melihat layout dan Close untuk menutupnya. Prefab/canvas/teks/slot bisa diedit di hierarchy. Menu setup ada di Nature Paradise > Shop > Create Cafe and House Calendar. Refresh Cafe and Calendar Layouts membangun ulang layout bawaan, jadi jangan pakai itu untuk mempertahankan perubahan layout manual.

Hover hanya memberi highlight sementara. Klik/keyboard memilih produk/kategori/hari; pilihan aktif memakai hijau sage dan pilihan sebelumnya kembali ke warna normal. Tombol Pesan dan quantity kosong dinonaktifkan. Menutup modal mengembalikan kontrol player dan waktu.
