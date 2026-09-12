# Balance CSV

Folder ini adalah sumber data balancing yang dapat diedit melalui Excel, LibreOffice, atau text editor.

## Workflow

1. Di Unity pilih `Nature Paradise > Data CSV > Export Items and Crops`.
2. Bagikan `Items.csv` dan `Crops.csv` kepada game designer.
3. Edit nilai tanpa mengubah nama header, `item_id`, `crop_id`, atau `asset_path` milik baris lama.
4. Tutup file dari Excel agar Unity dapat membacanya.
5. Pilih `Validate Items and Crops`. Jika valid, pilih `Import Items and Crops`.
6. Review perubahan ScriptableObject melalui Git diff sebelum commit.

Import tidak mengubah icon, prefab dunia, model pertumbuhan, material, audio, animator, maupun visual stage. Data tersebut tetap diatur melalui Inspector.

## Menambah item

Salin satu baris item yang paling mirip, lalu:

- ganti `item_id` dengan ID baru berformat `item.nama_item`;
- kosongkan `asset_path` agar importer membuat asset di folder `Resources/Items/CSV`;
- isi `display_name`, kategori, harga, stack, dan aturan gameplay;
- untuk bibit tanaman, isi `seed_crop_id` dengan ID dari `Crops.csv`.

Import tidak menghapus asset ketika sebuah baris dihapus dari CSV.

## Menambah tanaman

Tambahkan item bibit dan hasil panen ke `Items.csv`, lalu tambahkan baris ke `Crops.csv`:

- `crop_id`: ID stabil berformat `crop.nama_tanaman`;
- `seed_item_id`: ID item bibit;
- `produce_item_id`: ID hasil panen;
- `days_until_first_harvest`: jumlah growth day sampai siap dipanen;
- `regrows_after_harvest` dan `regrow_days`: aturan panen berulang;
- `quality_weights`: lima bobot kualitas, dipisahkan karakter `|`;
- `minimum_booster_for_stars`: lima tier booster minimum, dipisahkan karakter `|`.

Setelah import, pasang model setiap growth stage pada CropDataSO baru melalui Inspector.

## Aturan nilai

- Boolean memakai `TRUE` atau `FALSE`.
- Angka desimal memakai titik, misalnya `0.35`.
- Harga `0` berarti item tidak tersedia pada transaksi tersebut.
- Item Tool, Quest, Key Item, atau `is_not_sellable=TRUE` tidak dapat dijual di Market Stand dan Shipping Bin.
- ID adalah identitas save data dan tidak boleh dipakai ulang.
