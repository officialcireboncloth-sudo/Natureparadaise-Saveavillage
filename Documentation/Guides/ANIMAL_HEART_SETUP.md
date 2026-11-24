# Animal Heart

Heart = hubungan jangka panjang, Happiness = mood/stres saat ini, Quality = grade produk. Tidak memakai satu angka untuk ketiganya.

## Inspector dan kontrol

- AnimalGrowthSystem: **Animal Name**, **Heart → Points** (0–1000), dan **Heart Rules** untuk balancing. 100 points = 1 heart; maksimum 10. Nilai Friendship lama tetap dipertahankan untuk migrasi save.
- Dekati satu hewan: informasi nama, Heart, Happiness, Health, Fed, Pet, Growth, dan Production tampil tanpa debug. Hanya hewan terdekat menerima input.
- **F** feed, **P** pet, **G** ambil produk (binding lama dipertahankan).
- AnimalController: **Favorite Treat Item** dan **Medicine Item** otomatis diisi dari AnimalCareCatalog jika kosong. **Y** memberi treat dan **O** memberi obat. Item tersedia di tab Hewan pada toko, dan berkurang satu hanya saat kondisi valid.
- Pet, feeding, treat, dan pengobatan memberi reward maksimal sekali per hari per hewan. Telur/prenatal belum menerima interaksi care tersebut.
- AnimalRoutine sekarang mengatur perjalanan ke rumput dan kembali ke AnimalHome. Grazing mengonsumsi WorldGatherable Grass yang benar-benar tersedia, bukan reward gratis dari zona pasture. AnimalCareArea lama tidak lagi memberi makanan otomatis.
- Hewan di luar saat rain/storm kehilangan sedikit points; risiko sakit badai tetap berlaku. Panduan kandang, menu rename, pakan, dan pengujian ada di [ANIMAL_CARE_SETUP.md](ANIMAL_CARE_SETUP.md).

## Balancing awal

Pet +8, feed +5, grazing +3, fed+healthy day +2, favorite treat +10, medicine +8. Care lengkap tiga hari berturut-turut memberi bonus +3/hari. Belum makan satu hari masih grace; berikutnya -8/hari. Tidak dielus lebih dari tiga hari -3/hari. Sakit lebih dari satu hari -5/hari. Di luar saat rain -3 atau storm -10 (tidak ditumpuk). Semua bisa diubah pada Heart Rules.

## Produk dan nilai hewan

Heart menentukan bobot peluang grade 1–5, Happiness memodifikasinya. Tidak menjamin grade tinggi. Produksi tetap membutuhkan Adult, Fed, Healthy, Happiness minimal 20, shelter/cuaca grazing baik, serta interval produksi yang valid. Timer produksi lama tetap dipakai.

Grade ditetapkan sekali saat produk siap, disimpan, dan dimasukkan ke inventory sebagai qualityStars. Inventory penuh tidak menghabiskan produk. Produk yang sudah dihasilkan tetap bisa dikoleksi walaupun hewan kemudian sakit; penyakit menghalangi produksi baru.

Harga jual hewan dihitung melalui `AnimalSellPrice`, dan ditampilkan sebagai Sell Value pada info/menu hewan. Atur **Animal Growth Profile → Animal Sale Price → Base Sell Price / Bonus Per Heart**. Untuk satu hewan, aktifkan **AnimalGrowthSystem → Override Sale Price** lalu isi **Local Sale Price**. Tanpa profile, harga lokal juga dipakai.

Rumus: harga dasar × (1 + Heart Level × bonus per Heart), dibulatkan ke Gold terdekat. Default 1.000 G dan bonus 0,1: 0 Heart = 1.000 G; 5 Heart = 1.500 G; 10 Heart = 2.000 G. Bonus 0 menonaktifkan pengaruh Heart. Harga dasar/bonus negatif diperlakukan sebagai 0 dan hasil dibatasi int.MaxValue. Ini konfigurasi balancing asset/komponen, bukan salinan harga di save; harga dihitung ulang dari Heart yang dipulihkan. Harga beli toko dan harga produk tidak berubah. `AnimalValueMultiplier` memakai konfigurasi yang sama. Transaksi menjual hewan belum ditambahkan.

## Save/Load dan tes

AnimalSaveData kini mencakup nama, HeartState lengkap (points, streak, hari reward), dan grade produk siap, selain health, happiness, fed, pet, age/growth dan posisi yang sudah ada. Heart Level diturunkan dari points agar tidak bertentangan. Save lama friendship 0–100 dimigrasikan ke points 0–1000. Grade produk legacy yang belum tersimpan memakai grade 1. Nama kosong menampilkan spesies.

Tes manual: pet dua kali (sekali reward), save/load pada hari yang sama lalu pet lagi (tidak bertambah), tidur lalu pet (bertambah); feed/treat/obat berulang; lupakan makan beberapa hari; pasture cerah vs badai; produksi Adult dibanding Baby/Sick; koleksi saat tas penuh; save/load produk siap dengan grade tetap.

`Tests/AnimalHeartTests.cs` menguji batas points, anti-spam, grace/penalti, copy save, dan distribusi kualitas. Pengujian ini bukan Unity Play Mode.
