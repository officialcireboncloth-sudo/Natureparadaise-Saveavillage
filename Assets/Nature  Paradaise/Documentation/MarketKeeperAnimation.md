# Penjaga market: rig dan animasi

Penjaga memakai sistem **Unity Humanoid yang sama dengan NPC lain**, dengan klip Idle dan Walk player dari pustaka bersama. Ia tetap memakai skeleton deform/skin miliknya sendiri melalui Humanoid Avatar; rig kontrol authoring player tidak diganti dan tidak ditempel ulang ke mesh penjaga.

## Aset yang dipakai

- Prefab: `Resources/Characters/MarketKeeper.prefab`.
- Pustaka: `Resources/Characters/MarketKeeper Actions.asset`.
- Model dan tekstur: `mesh/Character/MarketKeeper/MarketKeeper.fbx` dan `Textures/MarketKeeper_BaseColor.jpg` (1024×1024).
- Komponen animasi: `NPCCharacterMotion`, sama seperti Mina.
- Referensi Idle dan Walk: `Player/Animations/Shared/Builder Humanoid Actions.asset`. Tidak ada duplikasi klip.
- Penempatan: `MarketStandVisual/KeeperAnchor/MarketKeeper`, di belakang gerobak menghadap ke depan. Tinggi dievaluasi dari pose idle menjadi 1,5 m mengikuti `PlayerDimensions.Height`; collider kapsul mengikuti ukuran yang sama.

Penjaga idle saat diam dan memakai Walk saat posisinya digerakkan. Animasi tidak menggerakkan root sendiri. Pemasangan ini belum menambahkan rute patroli/jalan otomatis; ia tetap menjaga gerobak sampai sistem lain memberinya tujuan berjalan.

## Menambahkan satu animasi berikutnya

Pada **MarketKeeper Actions.asset**, baris **MarketAction** sudah tersedia dengan `clip` kosong, `loop` mati, dan `speed` 1. Setelah gerakannya ditentukan:

1. Buat animasi di rig player atau rig Humanoid yang kompatibel, lalu ekspor/import sebagai Humanoid.
2. Pasang klip tersebut pada `MarketAction`. Atur `loop` jika perlu.
3. Panggil `NPCCharacterMotion.PlayAction("MarketAction")`. Gerakan tidak akan ditimpa Idle/Walk setiap frame. Gerakan satu kali otomatis kembali ke locomotion; gerakan looping menunggu `ResumeLocomotion()`.

Memanggil slot yang masih kosong akan gagal dengan aman tanpa mengganti pose aktif. Pemanggilan ulang memulai klip dari awal. Menu **Nature Paradise > Narrative > Apply Animated Market Keeper** mempertahankan klip tambahan yang sudah ditetapkan. Rebuild gerobak juga memasang ulang prefab penjaga di anchor tanpa membuat duplikat.

## Validasi

Pemeriksaan native Unity mencakup Avatar dan pemetaan tangan/kaki, deformasi saat Idle/Walk, langkah kaki yang nyata, tinggi hasil retarget, posisi kaki, penempatan di gerobak dan pemisahan collider. Uji Play Mode mencakup Idle–Walk–Idle, slot kosong, override animasi satu kali, selesai otomatis, restart dan pelepasan override; Mina tetap berjalan dengan perilaku sebelumnya.

Klip uji untuk MarketAction hanya dibuat sementara di memori. Tidak ada animasi tambahan palsu yang disimpan ke pustaka produksi. Hasil audit dan preview ada di `MarketKeeperAnimationReview/`. Scene Map, FBX penjaga, tekstur 1K, prefab Mina dan pustaka klip player dipertahankan.
