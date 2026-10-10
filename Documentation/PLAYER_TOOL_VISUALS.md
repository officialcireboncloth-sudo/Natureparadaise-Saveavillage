# Model tools pada player

Model tools impor sudah terhubung ke pilihan hotbar dan tangan kanan player. Komponen `PlayerHeldTools` ditambahkan otomatis oleh `PlayerController`, sehingga scene yang memakai controller tersebut tidak perlu dipasang satu per satu.

## Model yang dipakai

| Pilihan / aksi | Model |
| --- | --- |
| Hoe | Cangkul |
| Axe | Kapak |
| Hammer | Palu |
| Sickle | Sabit |
| WateringCan | Penyiram |
| FishingRod | Joran, melalui `FishingPresentation` |
| Shears | Gunting cukur |
| Menggosok hewan | Sikat, menggantikan alat hotbar selama aksi |
| Memerah sapi / kambing | Alat perah, menggantikan alat hotbar selama aksi |

`Held_Pickaxe.prefab` juga tersedia. Gameplay mining saat ini masih memakai tipe `Hammer`; Pickaxe belum menjadi pilihan hotbar tersendiri. Untuk menggunakan modelnya pada mining, ganti prefab pada entri Hammer di katalog.

Saat membawa barang/hewan atau menunggang, alat disembunyikan. Pilihan kosong, bibit dan barang lain tidak menampilkan alat lama. Joran memiliki satu pemilik visual agar tidak muncul dua kali, memakai ujung model sebagai titik tali, dan melengkung pada salinan mesh runtime tanpa mengubah asset sumber.

## Mengubah melalui Inspector

1. Pilih `Assets/Nature  Paradaise/Resources/Player/Player Tool Visual Catalog.asset`.
2. Buka entri alat yang ingin diubah. `prefab` memilih model, `handPosition` menggeser pivot gagang relatif pusat telapak dalam meter, `handEuler` mengatur rotasi relatif tangan, dan `scale` mengatur ukuran. Hoe/Axe/Hammer/Sickle memakai `carryHandEuler` untuk rotasi saat membawa dan `handEuler` untuk aksi. `secondHandGrip` menentukan posisi telapak kiri pada gagang; `twoHandsDuringAction` mengaktifkan genggaman kiri selama aksi, dan `twoHandsWhileCarrying` memberi dukungan tangan kiri saat diam dan titik tersebut masih dalam jangkauan.
3. Prefab tersedia di `Assets/Nature  Paradaise/Prefabs/Player/Tools/`. Pivot root berada di genggaman; anak `Model` membawa mesh/materialnya. Untuk joran, pertahankan anak `RodTip`: arah lokal +Z menunjuk dari pegangan menuju ujung. Pose joran dikendalikan `FishingSystem` / `FishingPresentation`.
4. Jalankan Play dan pilih alat di hotbar untuk melihat perubahan. Player memakai rig Generic dengan bone `mixamorig:RightHand`; ukuran alat tidak ikut membesar karena skala internal FBX rig.

Menu `Nature Paradise > Player > Create Imported Tool Visuals` membuat entri yang belum ada dan mempertahankan entri katalog yang sudah terpasang. Tidak perlu dijalankan untuk menggunakan asset yang sudah dibuat.

Menu `Nature Paradise > Player > Rebuild Hoe and Axe Grips` membuat ulang hanya mesh/prefab/pengaturan Hoe dan Axe menggunakan preset genggaman di script setup. Menu ini mengganti penyesuaian manual pada kedua entri tersebut.

Menu `Nature Paradise > Player > Rebuild Hoe Grip` membuat ulang hanya pacul, termasuk ukuran dan jarak genggaman kiri, tanpa mengganti pengaturan kapak.

Menu `Nature Paradise > Player > Rebuild Hammer and Sickle Grips` membuat ulang hanya dua alat tersebut, termasuk ukuran, pivot, rotasi membawa dan rotasi aksi yang dikalibrasi terhadap clip asli. Penyesuaian manual kedua entri tersebut akan diganti dengan preset.

Menu `Nature Paradise > Player > Calibrate Carried Tool Grips` menghitung ulang `carryHandEuler` semua entri `workGrip` dari pose idle player dan preset `carryDirection`, lalu mengembalikan offset telapaknya ke nol. `carryDirection` hanya dipakai untuk kalibrasi editor; runtime tidak mengunci alat ke arah badan.

## Titik pegangan saat berjalan / berlari

`ToolGrip_Right` dibuat sebagai anak bone `mixamorig:RightHand`, berada di pusat telapak, dengan skala internal rig dikompensasi. Prefab alat menjadi anak titik ini dan pivotnya tetap berada pada gagang. Posisi/rotasi mengikuti hierarki tulang langsung, termasuk sebelum update alat berikutnya; tidak memakai interpolasi atau arah badan untuk pose membawa.

Saat jalan/lari, tangan kanan mengikuti animasi asli tanpa IK yang menggeser atau menahan ayunan. Kapak dibawa satu tangan saat bergerak agar kedua lengan bisa berayun; genggaman dua tangan aktif saat menebang. Joran memakai titik telapak yang sama, termasuk saat dibawa, dengan garis pancing diperbarui setelah pose/bend joran pada frame yang sama.

Pitchfork yang masih memakai `ItemSO.worldPrefab` juga dipasang ke `ToolGrip_Right`, sehingga rig Generic tidak lagi memakai anchor tetap pada badan. Ukuran dan modelnya tetap mengambil data item.

## Koreksi genggaman Hoe / Axe

Kepala pacul pada FBX berada di ujung bawah sumbu Y, sedangkan kepala kapak di ujung atas. Keduanya sekarang memakai orientasi yang konsisten: +Z mengikuti gagang menuju kepala dan +Y menuju sisi mata alat. Pivot dipasang pada gagang yang benar, bukan pada pusat bounds/wrist. Ukuran panjang terbesar Hoe 1,8 meter dan Axe 1,65 meter, menyesuaikan tinggi model player yang terpasang.

Pose membawa pacul dikalibrasi dengan kepala ke bawah, kemudian mengikuti rotasi tangan. Kedua alat memakai dua titik genggaman selama aksi. Solver dua tulang bekerja pada lengan sesudah animasi Generic, mempertahankan tubuh/kaki dan membatasi target agar lengan tidak diregangkan. Tangan kanan dekat kepala dan tangan kiri di bagian belakang gagang. Posisi kedua telapak tetap pada gagang sepanjang ayunan memakai alat.

`HoeTool` mengirim sasaran tile dan `PlayerGatheringTool.UseAxe` mengirim titik collider batang ke `BeginWorkAction`. Mata alat diarahkan dalam bidang ayunan depan, sehingga tidak membelok ke samping karena offset wrist atau ketinggian target. Clip gerakan dan waktu impact gameplay tetap dipakai.

Orientasi mata pacul memakai bidang ayunan dengan sisi yang tetap (`Cross(player.right, shaft)`). Proyeksi arah depan pada gagang sebelumnya berubah tanda saat gagang melintasi arah depan, sehingga mata pacul berputar 180 derajat. Bidang tetap ini mempertahankan arah mata ke bawah saat menyentuh tanah dan ditarik kembali. Pacul diperkecil 10% dari preset 2 meter; jarak genggaman kiri ikut diperkecil secara proporsional.

## Koreksi Sickle / Hammer

Palu impor memiliki dua muka pemukul pada ujung kepala (sumbu X model sumber). Mesh gameplay menempatkan +Y sebagai normal muka tersebut, dan +Z sepanjang gagang menuju kepala. Preset dimensi terbesar palu 1,5 meter. Kedua telapak memegang gagang saat menghantam, dengan genggaman kiri 0,22 meter di belakang kanan. Pose aksi dikalibrasi pada 0,68 detik clip `Hammering Rock`: kepala mengayun ke bawah di depan player, bukan terangkat di belakang atau menghantam dengan sisi panjang kepala.

Preset dimensi terbesar sabit 1,05 meter dan memakai pivot pada gagang melengkung. +X merupakan normal bidang bilah; saat digunakan, bidang bilah diarahkan dalam bidang sapuan rendah. Rotasi dikalibrasi pada sapuan depan clip `Sickle` di 0,85 detik. Sabit memakai satu tangan, membiarkan lengan kiri mengikuti animasi asli.

Handler `UseHammer` dan `UseSickle` mengirim sasaran serta durasi ke `BeginWorkAction`. Damage palu menunggu 0,68 detik dan potongan sabit 0,85 detik, mengikuti fase kontak clip. Nilai tetap dapat diedit pada `PlayerGatheringTool` melalui Inspector. Scene FishingTestScene yang menyimpan nilai lama juga mengikuti timing baru. Saat jalan/lari, keduanya tetap mengikuti socket telapak tanpa interpolasi.

Joran yang tidak terlihat tidak lagi memindahkan socket telapak bersama ke offset joran. Hal ini sebelumnya menggeser alat lain sekitar 2,5 cm sesudah pose alat diperbarui. Solver aksi juga menempelkan pivot akhir pada telapak kanan aktual, termasuk saat pose transisi mencapai batas jangkauan lengan.

## Organisasi asset

- FBX dan texture asal game: `Assets/Nature  Paradaise/mesh/Prop/Tools/`.
- Mesh gameplay dengan pivot/ukuran yang disesuaikan: subfolder `Gameplay/`.
- Prefab genggaman: `Assets/Nature  Paradaise/Prefabs/Player/Tools/`.
- Katalog player: `Assets/Nature  Paradaise/Resources/Player/`.
- Sumber resolusi asli tetap berada di `ArtSource/Tools/Originals/`.

Texture 256 × 256 dan material model impor digunakan kembali. FBX sumber tidak diubah. Empat mesh yang sangat padat dibuatkan versi gameplay melalui penggabungan vertex dekat dengan batas UV dipertahankan:

| Model | Vertex impor | Vertex gameplay |
| --- | ---: | ---: |
| Axe | 51.114 | 19.867 |
| Sickle | 94.179 | 26.322 |
| WateringCan | 1.084.242 | 61.106 |
| Pickaxe | 968.294 | 34.363 |

## Validasi

Integrasi awal di Unity 6000.0.81f1: 37 pemeriksaan Play Mode pada player scene Map lolos, termasuk semua pilihan alat, genggaman bone animasi, ukuran world, tidak ada joran ganda, mesh/titik tali mengikuti bend, asset joran sumber tetap utuh, carry/riding, sikat/perah, dan disable/enable. Aksi perawatan diuji dengan fixture visual pada controller; ini bukan pengujian ulang seluruh siklus produksi hewan.

Hasil integrasi awal: `VisualReviews/Player-Imported-Tools-PlayMode.txt`. Snapshot `VisualReviews/Player-Imported-Tools.png` mencatat pose sebelum koreksi Hoe/Axe.

Koreksi Hoe/Axe diperiksa pada 488 sampel animasi native, mencakup seluruh clip di empat arah player. Kedua telapak tetap pada gagang, arah mata mengikuti bidang sasaran, dan solver tidak mengubah kaki. Hasil: `VisualReviews/Player-Hoe-Axe-Motion-Checks.txt`. Preview koreksi sebelum revisi socket tangan: `VisualReviews/Player-Hoe-Axe-Motion.png`, baris atas Hoe, baris bawah Axe; kolom idle, 0s, 0,25s, 0,55s, 0,72s, 0,9s, 1,15s.

Koreksi juga lolos 10 pemeriksaan Play Mode pada player Map, memakai prefab pohon interaktif sebagai fixture untuk handler UseAxe: kapak dua tangan saat membawa/menebang, target batang dikirim ke visual, pacul dua tangan saat ayunan ke tanah, pergantian alat, carry, dan hotbar kosong. Hasil: `VisualReviews/Player-Hoe-Axe-PlayMode.txt`.

Pegangan langsung pada bone tangan diperiksa lagi pada semua 10 prefab: 1.830 sampel native idle/walk/run, termasuk perubahan animasi dan rotasi player tanpa update alat di antaranya. Pivot gagang tetap mengikuti telapak langsung, tangan kanan tidak diubah saat membawa, dan ukuran world tetap benar. Regresi Hoe/Axe: 488 sampel aksi di empat arah tetap lolos. Hasil: `VisualReviews/Player-Tool-Hand-Follow-Checks.txt`.

Revisi Sickle/Hammer diperiksa pada 488 sampel aksi native di empat arah, serta 186 sampel idle/walk/run. Kedua telapak palu tetap pada gagang, muka palu mengarah ke bawah, dan kepala mengayun turun di depan pada frame impact. Sabit menjaga bidang bilah pada sapuan rendah tanpa mengubah lengan kiri. Hasil: `VisualReviews/Player-Sickle-Hammer-Motion-Checks.txt`. Preview: `VisualReviews/Player-Sickle-Hammer-Motion.png`; baris atas Hammer, bawah Sickle; kolom idle, 0s, 0,25s, 0,55s, 0,68s, 0,85s, 1,15s.

Regresi setelah perubahan solver: 488 sampel Hoe/Axe di empat arah kembali lolos, dengan kedua telapak tetap pada gagang dan posisi kaki tidak berubah. Hasil tercatat dalam report Sickle/Hammer yang sama.

Play Mode pada Map juga lolos 18 pemeriksaan, mencakup handler `UseHammer` / `UseSickle` asli, timing damage/cut dengan fixture batu/rumput, orientasi muka palu pada state Animator aktual, genggaman palu selama 59 frame dan sabit selama 87 frame, pergantian alat dan konflik socket joran tersembunyi. Save dinonaktifkan dan fixture dihapus sesudah tes; scene kembali ke Edit Mode. Hasil: `VisualReviews/Player-Sickle-Hammer-PlayMode.txt`.

Script runtime dan editor juga dikompilasi oleh Unity 6000.0.81f1 yang sedang membuka project, tanpa error kompilasi.

Ukuran kemudian diperkecil berdasarkan review visual: Hammer dari 2,2 menjadi 1,5 meter (sekitar 32% lebih kecil), Sickle dari 1,65 menjadi 1,05 meter (sekitar 36% lebih kecil). Jarak genggaman kiri palu menjadi 0,22 meter. Sebanyak 488 sampel aksi native di empat arah kembali lolos setelah perubahan ukuran. Hasil: `VisualReviews/Player-Sickle-Hammer-Size-Checks.txt`. Preview `Player-Sickle-Hammer-Motion.png` sudah menampilkan ukuran terbaru; report Play Mode sebelumnya mencatat pengujian pose sebelum pengecilan ini.

Koreksi arah mata pacul sesudah benturan dan ukuran 1,8 meter lolos 724 sampel animasi native di empat arah: kedua telapak tetap pada gagang, tidak ada putaran 180 derajat antarframe, dan mata mengarah ke bawah pada 0,68 / 0,72 / 0,85 / 0,95 detik. Hasil: `VisualReviews/Player-Hoe-Contact-Checks.txt`. Preview terbaru: `VisualReviews/Player-Hoe-Contact-Motion.png`; kolom idle, 0,25s, 0,55s, 0,68s, 0,72s, 0,85s, 0,95s, 1,15s. Ini pemeriksaan pose native di editor, bukan pengujian ulang gameplay Play Mode; waktu impact tetap 0,72 detik.
