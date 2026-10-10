# Kitchen / Kompor UI

Interaksi E pada Kitchen_MeshSlot di modular house membuka panel MEMASAK. Komponen KitchenSet sudah terpasang lewat HouseFiveLevelSetup dan EnsureInteractiveFixtures. Unlock mengikuti sistem lama: House Lv.3. UI memakai Canvas uGUI, panel kanan, daftar resep scroll, hasil masakan, bahan scroll, jumlah batch, Masak, dan key hints. W/S memilih resep; A/D atau minus/plus mengatur batch 1–99; Enter memasak; Esc menutup. Resep belum dipelajari tampil ???. Tombol normal/selected abu-abu, hover hijau, outline selection cyan.

## CSV data

`Assets/Nature  Paradaise/Data/Balance/Items.csv` adalah master item: item_id, nama, kategori, stack limit, sell/buy price, food_preparation, pemulihan health/stamina/hunger, refrigerator_category dan storage_destination. Makanan hasil masak menggunakan ReadyToEat dan CookedFood/Drink. Bahan yang belum dimasak memakai Raw jika sesuai aturan item. Tidak ada gambar dalam CSV.

`Assets/Nature  Paradaise/Data/Balance/Recipes.csv`:

| Kolom | Isi |
| --- | --- |
| recipe_id | ID resep unik/stabil |
| asset_path | Path .asset dalam Resources/Cooking/Recipes |
| display_name / description | Nama dan deskripsi di UI |
| ingredients | item_id:jumlah, dipisah tanda pipe. Contoh item.cabbage:2|item.milk:1 |
| result_item_id | ID master item hasil |
| result_amount | Jumlah hasil per batch, 1–999 |
| required_kitchen_level | Level dapur 1–4 |
| required_equipment | Stove, Pot, FryingPan, Oven, CuttingBoard, Blender; gabungkan dengan pipe |
| base_cooking_minutes | 0–1440 menit; batch tambahan +5 menit per batch, mengikuti sistem lama |
| learned_by_default | TRUE/FALSE |
| output_quality_mode | IngredientAverage (rata-rata grade bahan yang benar-benar dipakai) atau Fixed |
| fixed_output_quality | Grade tetap 0–5 untuk mode Fixed |

Menu import: Nature Paradise → Data CSV → Import Items and Crops (untuk item baru), kemudian Recipes → Validate / Import. Menu Export Items and Crops / Recipes Export mengekspor data yang ada. Pastikan item_id bahan dan hasil sudah terdaftar sebelum import resep. Recipe importer memvalidasi seluruh tabel sebelum mengubah aset; ID/path duplikat, bahan kosong/tidak dikenal, jumlah invalid, grade invalid dan path di luar folder ditolak. CSV lama tanpa kolom grade tetap didukung dan tidak menghapus grade yang sudah diatur. Import tidak menimpa Sprite.

Contoh baru Sup Sayur menghasilkan item.vegetable_soup x1 dari item.cabbage x2 + item.milk x1, memakai Stove + Pot, 10 menit dan IngredientAverage. Ini data awal memakai item yang sudah tersedia, bukan angka dari screenshot yang dipaksakan. Lima resep lama tetap tersedia. Tambah baris untuk makanan lain, lalu import.

## Satu referensi gambar

Nature Paradise → UI → Kitchen → Shared Item Image Slots menampilkan setiap ItemSO bahan/hasil satu kali. Isi Shared Icon pada item: seluruh ingredient row, recipe list, hasil, inventory, hotbar dan storage mengambil ItemSO.icon yang sama. Ilustrasi detail opsional memakai ItemSO.inventoryIllustration; jika kosong, memakai icon yang sama. Tidak ada instantiate Sprite, duplikat Texture2D atau potongan screenshot dalam runtime UI. Editor mendukung Undo dan Save Item Images.

UI → Kitchen → Select Theme Image Slots memilih KitchenTheme untuk panel/leaf/cook/status/locked-recipe artwork. Slot kosong tidak menggambar ikon tiruan. Row bahan dipakai ulang ketika angka berubah.

## Sistem memasak

Bahan otomatis dibaca dari Inventory + Refrigerator. Aturan item kulkas tidak diubah. Result quantity dan grade ditampilkan sebelum memasak; validasi level, equipment, learned state, bahan dan ruang hasil mengikuti KitchenService. Hasil masuk tas, collection/quest cooking dan waktu memasak memakai service lama. Pause menu memasak mengunci gerak, jam game dan timeScale; lock milik menu dilepas saat tutup/disable/destroy. Nilai timeScale dan cursor sebelumnya dipulihkan.


Validasi: runtime dan editor compile lulus; tes Unity terisolasi memeriksa CSV import/legacy/atomic validation, referensi item, batch Tas + Kulkas, grade IngredientAverage/Fixed, missing/full inventory, shared Sprite dan cleanup modal. Preview 1920×1080 diperiksa.
