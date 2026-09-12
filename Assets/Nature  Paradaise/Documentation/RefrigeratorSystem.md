# Refrigerator System

Refrigerator berada pada `Refrigerator_MeshSlot` di scene `HouseInterior` dan aktif
setelah `house.refrigerator` terbuka pada House Lv.2. Isi storage dimiliki
`RefrigeratorService`, sehingga tidak hilang saat scene interior ditutup.

## Kapasitas

| Level | Slot |
| --- | ---: |
| 1 | 24 |
| 2 | 40 |
| 3 | 60 |
| 4 | 80 |

Satu slot menyimpan satu kombinasi `Item + Quality + Fish Size`. Stack yang identik
digabung sampai `ItemSO.StackLimit`; quality atau ukuran ikan yang berbeda memakai
slot berbeda. Tidak ada timer spoilage.

## Authoring item

Isi `ItemSO.refrigeratorCategory` untuk mengizinkan item masuk. `None` menolak item.
Pilihan yang tersedia adalah Crop, Fruit, Fish, AnimalProduct, Ingredient,
CookedFood, dan Drink. Metadata ini ikut ditulis ke kolom
`refrigerator_category` ketika menjalankan menu `Data CSV/Export Items and Crops`.
CSV lama tanpa kolom tersebut tetap dapat di-import dan tidak mengubah metadata asset.

## Integrasi Kitchen

Kitchen memakai `KitchenIngredientService.GetAvailableCount` untuk membaca jumlah
gabungan Inventory dan Refrigerator. Setelah memastikan slot output tersedia,
panggil `KitchenIngredientService.TryConsume`; transaksi memvalidasi seluruh resep
lebih dahulu, memakai bahan Inventory terlebih dahulu, lalu Refrigerator.

## Setup interior

Runtime memasang komponen ke mesh slot lama secara otomatis. Untuk menyimpannya
langsung ke scene agar dapat diedit di Inspector, jalankan:

`Nature Paradise > House > Install Refrigerator`
