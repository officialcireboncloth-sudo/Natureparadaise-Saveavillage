# Rumput liar yang bisa disabit

`Resources/World/Wild Grass.prefab` memakai kombinasi tiga mesh berdaun lebar
dari Toon Farm Pack: dua Leafy Plant 04A dan satu Leafy Plant 01A. Bentuk gulma
ini khusus untuk resource panen; rumput pendek dan bush di terrain tetap dekorasi.
Material asli tanaman dipertahankan, termasuk shader dan efek anginnya.

Prefab ini dipakai oleh FieldArea dan WildGrassRuntimeSpawner. Ukuran patch
tetap dinormalisasi ke sekitar setengah cell field, dengan variasi rotasi/ukuran.
WorldGatherable tetap menggunakan sickle, menghasilkan item Grass, dan respawn
setelah 2–4 hari. Identitas prefab dipertahankan agar referensi FieldArea tetap valid.

Untuk menerapkan ulang visual: **Nature Paradise > Grass > Apply Distinct
Harvestable Weed**. Preset menyimpan backup prefab awal ke
`Library/WildGrassVisualBackups` dan tidak mengubah sebaran dekorasi terrain.
