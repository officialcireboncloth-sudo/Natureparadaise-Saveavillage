# Builder dan progres konstruksi

Di Lumber Store pilih **Rumah**, **Bangun**, atau **Upgrade**. Bangun/upgrade struktur membuka preview yang sudah ada; biaya baru dipotong setelah lokasi proyek dikonfirmasi. Membatalkan preview tidak memesan worker atau memotong bahan.

Setelah transaksi berhasil, satu worker capsule bertopi berangkat dari Lumber, berjalan ke sisi proyek yang dapat dijangkau, lalu bekerja dengan palu di meja kerja. Bar dunia di atas area kerja menampilkan persentase dan status. Setelah pekerjaan selesai, bangunan membuka level/fasilitasnya dan worker kembali ke Lumber.

## Data yang bisa diedit

- `Resources/Buildings/Lumber Building Catalog.asset`: tambah/kurangi jenis bangunan pada daftar **Buildings**. `UpgradeShopFront.constructionCatalog` dapat memakai katalog lain per Lumber.
- `Resources/Buildings/Construction Worker Settings.asset`: ukuran/kecepatan worker, jam kerja, material, jarak tampil bar, worker prefab, dan hammer prefab opsional. Default jam kerja **08:00–17:00**. `barPixelWidth` menjaga lebar bar tetap terbaca saat kamera zoom; `barHeight` mengatur jarak bar di atas worker.
- Pada Lumber, `BuilderDeparture_Editable` adalah titik berangkat di permukaan tanah, di luar collider seller. Kosong memakai titik di depan Lumber terdekat.
- Pada setiap `BuildingDefinitionSO > Levels`, biaya, bahan, `constructionDays`, dan prefab final tetap berasal dari data bangunan / CSV. Durasi dihitung sebagai hari kerja: 2 hari = 18 jam kerja pada jam default. Perjalanan belum dihitung sebagai pekerjaan. Perubahan durasi/jam kerja setelah pesanan dibuat berlaku untuk pesanan berikutnya.
- `constructionStagePrefabs`: empat prefab **visual saja**, urutan fondasi, rangka, dinding/atap, finishing. Slot kosong memakai scaffolding otomatis dan prefab final. Collider dan skrip gameplay pada clone tahap dinonaktifkan/dihapus; bangunan belum dapat dipakai sebelum selesai.

## Tahap dan penyimpanan

0–<25% fondasi, 25–<50% rangka, 50–<75% dinding/atap, 75–<100% finishing. Rumah yang direnovasi tetap menampilkan exterior level lama dan interior tetap tersedia; scaffolding/platform/tangga menandai pekerjaannya.

Gerak capsule, kaki, dan palu menggunakan animasi prosedural. Prefab humanoid opsional dapat menggunakan Animator dengan parameter bool `Walking` dan `Working`. Mengganti worker prefab tidak otomatis membuat rig atau animasi humanoid baru.

Progres berasal dari jam game, bukan jumlah pukulan palu. Jeda modal/menu menahan waktu dan gerakan worker. Waktu tidur/pergantian hari dihitung hanya dalam jam kerja. World tetap dimuat ketika masuk interior, sehingga konstruksi tidak berhenti ketika tidak terlihat. Tidak ada progres berdasarkan waktu komputer ketika game ditutup.

Save menyimpan fase worker, posisi/rotasi, tujuan, titik Lumber, jam kerja, durasi pesanan, dan pekerjaan yang sudah selesai. Load tidak mengulang biaya atau perjalanan dari awal. Save lama tanpa data worker tetap dapat dipulihkan; proyek lama yang belum selesai dimigrasi sebagai pekerjaan yang sudah dimulai. Proyek lama yang sudah melewati tanggal selesai mengikuti perilaku lama.

Rute menggunakan NavMesh jika tersedia, atau pencarian di permukaan tanah jika belum dibake. Rute yang tidak terjangkau menampilkan **Jalur builder terhalang**, mencoba ulang berkala, dan tidak melakukan teleport/menyelesaikan proyek sebelum tiba. Letakkan titik berangkat dan site pada permukaan yang bisa dilewati; bake NavMesh untuk rute kompleks atau peta besar.

Menu setup: **Nature Paradise > Construction > Create Worker Settings and Lumber Catalog**. Setup mengisi asset yang belum ada, tidak membangun ulang scene atau memindahkan seller. Coop/Shed yang sebelumnya belum memiliki prefab final memakai wrapper model dari Toon Farm Pack, sehingga pesanan struktur baru memiliki exterior setelah selesai.
