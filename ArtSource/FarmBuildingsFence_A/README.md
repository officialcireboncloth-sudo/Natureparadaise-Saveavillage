# Farm buildings + modular fence A

Unity: sudah diterapkan dan disimpan di Map pada 14 Oktober 2026 sebagai satu prefab instance `FarmReference_Group`. Pilih parent itu untuk memindahkan/memutar seluruh bangunan, pagar keliling dan sekat kebun. Module child tetap terpisah dan bisa diedit. Prefab: `Assets/Nature  Paradaise/Prefabs/FarmReference/FarmReference_Group.prefab`. Audit native Unity PASS; rincian di `UNITY_LAYOUT.md`.

Dikerjakan di Blender 4.5.14 dari sketch yang disetujui pengguna pada 13 Oktober 2026.

File utama: `Blender/FarmBuildingsFence_A.blend`. Scene `00 Farm + Fence Overview` menampilkan dua bangunan dan kit pagar bersama. Scene `Farm_Lumbung_A Editable` dan `Farm_BarnStore_B Editable` memiliki collection sumber, mesh export, marker, dan studio preview terpisah. Scene `FenceKit Modular` menyimpan modul pada koordinat lokal masing-masing, sehingga beberapa modul akan saling menumpuk jika semuanya ditampilkan; pilih collection modul yang ingin diedit. Overview memakai salinan/instance untuk review perakitan.

Bangunan A memiliki tapak 7 × 5 m, tinggi 5,6 m. B memiliki tapak 9 × 6 m, tinggi 5,4 m. Atap memakai profil gambrel dan sayap lean-to; B memiliki kanopi kanan yang lebih lebar. Satu unit Blender = satu meter; depan -Y, atas Z, kanan X. Origin bangunan di tengah tapak pada tanah.

Envelope visual hasil mesh (termasuk lis dan lampu): A sekitar 7,86 × 5,96 × 5,60 m; B 9,86 × 6,96 × 5,40 m. Tapak tabel proposal adalah badan/fondasi, bukan reservasi penempatan gameplay. Triangle export: A 6.098; B sekitar 8.100, pagar 24–288 per modul. `Previews/Verification.json` mencatat PASS untuk saved Blender source dan sembilan FBX roundtrip.

Bagian dinding/papan, fondasi, genteng, atap dasar, timber, pintu, jendela dan hardware terpisah sebagai mesh sumber. Genteng dan papan adalah geometry dengan UV ke palet Toon Farm Pack; bukan tekstur baru. Mesh export bangunan digabung dan triangulated. Collection studio hanya untuk preview dan tidak diekspor ke FBX.

Kit: tiang, rail pitch 1/2 m tanpa tiang, tiang sudut 90°, tiang T, gerbang pitch 2/3 m. Rail panjang kayu 0,76/1,76 m; tiang 0,24 × 0,24 × 1,20 m. Gerbang 2 m satu daun 1,64 m; gerbang 3 m dua daun 1,29 m. Daun diparent ke empty engsel dengan sumbu putar lokal Z; root gerbang dan tiang tetap. Root/socket disertakan dalam FBX; rotasi animasi dan script gameplay belum dibuat.

Semua asset memakai material `TFP_Atlas_1A`. Remap nama tersebut ke material pack `Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack/Models/Materials/TFP_Atlas_1A.mat` saat import Unity. Atlas PSD asli GUID `522fcb2b5717c7640b460ee9ff73a78a` tidak diubah. PNG composite packed dalam .blend hanya untuk preview Blender, tidak menjadi texture runtime tambahan. Warna kaca dan lampu berupa warna opaque dari atlas; shader Blender merupakan preview, bukan shader toon project Unity.

Papan nama mengikuti ukuran proposal; tulisan BARN STORE dibuat sebagai geometry mesh terpisah dengan UV ke palet besi gelap pada atlas yang sama, sehingga tidak menambah texture. Hewan, tanaman, kebun, jalan, terrain, prop barang dagangan dan interior tidak dibuat.

`Exports/` berisi dua FBX bangunan dan tujuh FBX kit modular. `Previews/` berisi render, manifest ukuran/jumlah triangles, log build dan hasil verifikasi. `References/ApprovedSpecification.md` menyimpan proposal yang disetujui; sumber desain lengkap tetap di `Documentation/Design/FarmBuildingsFenceReview/`.

Build ulang dengan executable Blender melalui `--background --python Scripts/build_farm.py`; jalankan `Scripts/verify_farm.py` sesudahnya. Script bekerja pada proses terpisah, menghasilkan file di folder ini. Tidak membuat prefab/gameplay atau mengubah map Unity. FBX memakai forward -Z/up Y; hasil roundtrip diuji oleh verifier, sedangkan shader/collider/perilaku penempatan native Unity memerlukan integrasi berikutnya.
