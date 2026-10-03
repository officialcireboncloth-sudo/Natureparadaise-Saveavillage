# Main menu UI

Scene: `Assets/Nature  Paradaise/Map/Scenes/UI/MainMenu.unity`.
Prefab: `Assets/Nature  Paradaise/Prefabs/UI/MainMenu.prefab`.

Menu mengikuti komposisi referensi: logo di tengah atas, tagline, lima tombol
di panel transparan, kutipan kanan atas, dan bar bahasa/grafik/versi di bawah.
Panel bersudut membulat, garis tepi, serta ikon dibuat dengan mesh UI Unity;
tidak memerlukan sprite tambahan. Transparansi panel bukan efek blur background.

## Mengisi artwork

1. Pilih prefab lewat **Nature Paradise > UI > Main Menu > Select Editable Prefab**.
2. Pada `MainMenuController`, isi **Background Sprite** dan **Title Sprite**.
   Import artwork dengan Texture Type **Sprite (2D and UI)**.
3. Biarkan **Title Mode = Image**. Slot logo kosong tampil transparan.
   Mode Text tersedia bila ingin judul teks sebagai pengganti.

Background memakai aspect envelope/cover: memenuhi layar dan memangkas sisi
gambar sesuai rasio layar. Logo memakai preserve aspect. Kedua artwork sengaja
kosong; warna hijau gelap hanya fallback saat background belum diisi.

## Interaksi

- **Mulai Game Baru**: langsung memulai Map; jika save ada, tampil konfirmasi
  sebelum menghapusnya. **Batal** kembali ke main menu.
- **Lanjutkan**: aktif hanya saat save tersedia, memakai SaveManager yang ada.
- **Pengaturan**: slider volume utama, musik/alam, dan efek suara.
- **Kredit**: panel teks yang bisa diganti pada child `Credits Content`.
- **Keluar**: keluar build atau menghentikan Play Mode di editor.
- **Bahasa**: menampilkan Indonesia dan membuka Pengaturan. Lokalisasi/pilihan
  bahasa lain belum ditambahkan.
- **Grafik**: beralih di antara quality level project dan menyimpan pilihan.
  Nama level berasal dari QualitySettings, sehingga mengikuti konfigurasi project.
- **Versi**: memakai Application.version dari Player Settings.
- Escape kembali ke menu utama; tombol mendukung navigasi Selectable Unity.

Canvas memakai referensi 1920 x 1080 dan SafeAreaFitter. Controller mengubah
anchor menjadi layout portrait saat aspek layar di bawah 1.2. Untuk mengatur
anchor manual, matikan **Responsive Layout** pada controller.

Setup editor memigrasikan prefab versi lama sekali ke layout versi 2 saat
script selesai dimuat. Root prefab dan field artwork/audio dipertahankan;
child UI diganti. Scene yang sudah ada tidak ditulis ulang.
