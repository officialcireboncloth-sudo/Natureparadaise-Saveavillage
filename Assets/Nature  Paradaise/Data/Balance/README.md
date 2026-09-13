# Balance Data CSV

Folder ini adalah sumber data balancing yang dapat dibuka melalui Excel, LibreOffice,
Google Sheets, atau text editor. `Items.csv` menyimpan data item seperti harga beli/jual,
stack, kategori, tool, medicine, fertilizer, makanan, dan hubungan bibit. `Crops.csv`
menyimpan waktu panen dan aturan crop. `Fish.csv` menyimpan lokasi, waktu, cuaca, rarity,
kesulitan minigame, serta rentang ukuran ikan.

## Workflow aman

1. Di Unity pilih `Nature Paradise > Data CSV > Export Items and Crops` untuk mengambil data terbaru dari asset.
2. Edit CSV di spreadsheet tanpa mengubah nama header.
3. Jangan mengubah `item_id`, `crop_id`, atau `asset_path` pada baris asset lama karena ID dipakai oleh save data.
4. Simpan sebagai CSV UTF-8 dengan pemisah koma, lalu tutup file dari Excel.
5. Pilih `Nature Paradise > Data CSV > Validate Items and Crops`.
6. Jika valid, pilih `Nature Paradise > Data CSV > Import Items and Crops`.
7. Review perubahan ScriptableObject melalui Git diff sebelum commit.

Data ikan memakai menu terpisah pada grup yang sama: `Export Fish`, `Validate Fish`, dan
`Import Fish`. Jalankan import item lebih dahulu bila menambah ItemSO ikan baru.

Import dibatalkan seluruhnya jika satu baris tidak valid. Validator memeriksa kolom,
angka, enum, ID duplikat, serta referensi item dan crop. Import tidak mengubah icon,
prefab dunia, material, audio, animator, atau model growth stage.

## Menambah item

Salin baris yang paling mirip di `Items.csv`, lalu:

- isi `item_id` baru dengan format stabil seperti `item.nama_item`;
- kosongkan `asset_path` agar importer membuat ItemSO baru di `Resources/Items/CSV`;
- isi nama, kategori, harga, stack, dan aturan gameplay;
- untuk bibit, isi `seed_crop_id` menggunakan ID dari `Crops.csv`.

Menghapus baris CSV tidak menghapus asset Unity secara otomatis.

## Menambah tanaman

1. Tambahkan item bibit dan hasil panen ke `Items.csv`.
2. Tambahkan baris ke `Crops.csv` dengan `crop_id` baru.
3. Hubungkan `seed_item_id` dan `produce_item_id` ke ID dari `Items.csv`.
4. Isi `days_until_first_harvest` sebagai jumlah hari pertumbuhan yang tersiram.
5. Atur `regrows_after_harvest` dan `regrow_days` jika tanaman dapat tumbuh kembali.
6. Setelah import, pasang model setiap growth stage pada CropDataSO melalui Inspector.

## Aturan nilai

- Boolean memakai `TRUE` atau `FALSE`.
- Angka desimal memakai titik, misalnya `0.35`.
- Nilai harga `0` berarti transaksi tersebut tidak tersedia.
- Nilai majemuk seperti `quality_weights` dipisahkan dengan karakter `|`.
- Tool, Quest Item, Key Item, dan item dengan `is_not_sellable=TRUE` tidak dapat dijual.
- Item dengan quality berbeda tetap menjadi stack berbeda di sistem penyimpanan.
- `refrigerator_category` menentukan filter lemari es dan bersifat opsional saat membaca CSV lama.

## Menambah atau mengatur bait

Gunakan `category=Bait`, lalu isi `bait_level` dengan `Basic`, `Quality`, `Premium`, atau
`Deluxe`. `bait_bite_speed_bonus=0.1` berarti bite 10% lebih cepat. Kolom multiplier
menentukan bobot Uncommon, Rare, dan Legendary; `bait_junk_reduction` bernilai 0..1.
`required_fishing_level` dan `required_village_level` mengatur kapan bait muncul sebagai
produk yang dapat dibeli. Semua kolom bait bersifat opsional untuk item lama.

## Menambah ikan

1. Tambahkan item hasil tangkapan berkategori `Fish` ke `Items.csv`, lalu import item.
2. Tambahkan baris `Fish.csv` dan hubungkan `item_id` ke item tersebut.
3. Isi lokasi dengan kombinasi `River|Lake|Pond|Ocean`.
4. Isi season/weather dengan `All` atau gabungan nilai memakai `|`.
5. Atur rarity, rod level, bite window, catch zone, movement, serta ukuran minimum/maksimum.
