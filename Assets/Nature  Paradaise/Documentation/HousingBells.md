# Bel sapi dan ayam

Bel memakai tiang kayu kecil yang berdiri di tanah, di depan sebelah kanan pintu dilihat dari depan kandang. Barn dan Coop level 1–5 memakai mesh bel/tali/tiang serta atlas yang sama. Tinggi tiang Barn sekitar 1,51 meter, Coop sekitar 1,24 meter. Lebar bel sekitar 0,33 meter pada Barn dan 0,27 meter pada Coop; ukuran tiang tidak ikut membesar ketika bangunan di-upgrade.

Marker pendekatan dan prompt mengikuti posisi bel, terpisah dari pintu masuk. Ayunan, suara dan menu perintah hewan tetap memakai sistem AnimalBellStation. Prefab lama tanpa bel terpasang mendapat fallback bertiang dengan ukuran kecil yang sama di sisi depan kanan pintu; data hewan dan save tidak diubah.

Hasil sudah dipasang pada prefab exterior utama, tanpa mengubah ID prefab, marker pintu atau collider bangunan. Penerapan ulang setelah ekspor art: **Nature Paradise > Art > Apply Front Right Barn and Coop Bells**. Setup art Barn/Coop juga mempertahankan pemasangan ini.

Validasi native Unity di salinan proyek: [audit](BellsPondReview/Audit.txt), [bel sapi](BellsPondReview/BarnBell.png), [bel ayam](BellsPondReview/CoopBell.png).
