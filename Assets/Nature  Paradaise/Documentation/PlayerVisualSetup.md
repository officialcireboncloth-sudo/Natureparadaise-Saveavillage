# Player Visual dan Animasi Mixamo

Jalankan `Nature Paradise > Player > Setup or Update Player Visual` ketika Map terbuka dan Play Mode berhenti. Setup membuat prefab `PlayerVisual`, material URP, Animator Controller, dan `Player Animation Set`. Capsule renderer lama dinonaktifkan, sedangkan CharacterController dan seluruh komponen gameplay tetap berada pada root Player.

Model utama sekarang dibaca dari `Assets/Nature  Paradaise/mesh/Dummy/PlayerDummy/Player_Dummy.fbx`. Saat FBX atau teksturnya diganti, Unity mempertahankan GUID lewat file `.meta`, mengatur rig sebagai Humanoid, lalu memperbarui model di prefab `PlayerVisual`. Model lama di dalam prefab diganti sehingga tidak terjadi visual ganda. Transform `PlayerVisual_Editable` memakai offset Y `0.5` untuk menyamakan pivot visual FBX dengan titik kaki CharacterController; object visual tidak memiliki collider atau Rigidbody.

Jika FBX baru sudah memiliki skeleton Humanoid, Avatar dari FBX otomatis dipasang ke Animator pada root prefab. Untuk menambahkan animasi Mixamo:

1. Jika model belum memiliki skeleton, upload `Player_Dummy.fbx` ke Mixamo dan selesaikan Auto-Rig. Download satu kali dengan `Skin: With Skin`, lalu gunakan hasilnya sebagai `Player_Dummy.fbx`.
2. Download animasi berikutnya menggunakan `Skin: Without Skin` dan simpan di `Assets/Nature  Paradaise/Player/Animations/Mixamo`.
3. Pilih seluruh FBX animasi, lalu jalankan `Assets > Nature Paradise > Configure Selected Mixamo FBX`. Tool mengatur Rig Humanoid dan menyalin Avatar player.
4. Masukkan clip Idle, Walk, Run, Sprint, Jump, dan Use Tool ke `Player Animation Set`, kemudian jalankan `Nature Paradise > Player > Setup or Update Player Visual`.

Jika model diganti ketika Play Mode aktif, hentikan Play Mode lalu jalankan menu setup satu kali. Asset postprocessor sengaja tidak mengubah prefab selama Play Mode agar scene runtime tidak rusak.

Animator menggunakan parameter `Speed`, `Grounded`, `Sprint`, `Carry`, `Jump`, dan `UseTool`. Root Motion dimatikan karena posisi player dikendalikan oleh `PlayerController` dan `CharacterController`. FBX Mixamo Without Skin dibuat sebagai Humanoid `Create From This Model`; Unity melakukan retarget clip ke Avatar Player saat Animator memutarnya.
