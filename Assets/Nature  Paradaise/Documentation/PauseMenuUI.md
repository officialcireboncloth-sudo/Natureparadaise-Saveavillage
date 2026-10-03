# Pause menu UI

## Cara membuka

1. Masuk Play Mode di Map dan klik **Game View** agar fokus.
2. Tekan **F7** atau klik tombol **F7 Menu** di HUD. F7 juga menutup menu.
3. Dalam build game, tombolnya **ESC**. Tombol X dan Lanjutkan menutup menu.

Input keyboard menu hanya diterima ketika Game View fokus dalam editor. Scene
View tetap menerima shortcut viewportnya. F7 adalah default, bukan jaminan
terhadap shortcut kustom editor. Ganti `Editor Pause Key` bila editor memiliki
shortcut kustom yang memakai F7. `Allow Escape In Editor` dapat diaktifkan bila
ingin menguji ESC; default nonaktif agar ESC tidak dipakai menu saat pengujian.

## Mengisi gambar

Pilih **Nature Paradise > UI > Pause Menu > Select Image Slots and Keybind**.
Asset yang dibuka: `Assets/Nature  Paradaise/Resources/UI/PauseMenuTheme.asset`.
Semua slot gambar sengaja kosong:

- Background opsional; bila kosong, dunia game tetap menjadi latar dengan shade.
- Panel dan lima ikon tab.
- Default NPC portrait, animal portrait, dan animal illustration.
- Animals: portrait/ilustrasi per AnimalType.
- Quests: portrait pemberi quest per quest ID.

Import gambar sebagai Sprite (2D and UI), lalu drag ke field asset ini. Artwork
portrait menjaga rasio. UI tidak membuat gambar hewan/NPC baru. Panel dan layout
fallback tetap dibangun melalui elemen Unity, tanpa efek blur otomatis.

## Isi tab

- Ringkasan: hari, jam, health, stamina, uang, dan progress desa.
- Hewan: filter semua/unggas/hewan besar/hewan pribadi, daftar dan detail hewan
  yang ada di scene. Level, usia, kasih sayang, kenyang, kesehatan dan status
  kandang berasal dari sistem hewan. Kapasitas peternakan dan tier contoh tidak
  dibuat-buat bila belum tersedia di data gameplay.
- Kebunku: musim dan jumlah area kebun aktif.
- Misi: filter aktif/selesai/gagal, objective/progress dan reward QuestService.
  Lacak Quest memprioritaskan quest tersebut di tracker HUD. Tenggat tidak
  ditampilkan karena definisi quest saat ini tidak menyimpan deadline.
- Pengaturan: Umum, Audio, Grafis, Kontrol, serta Aksesibilitas.

Pengaturan yang tersedia: ukuran teks menu, ukuran UI, kontras menu, sensitivitas
zoom kamera, mengurangi impulse kamera, transparansi tombol menu, nama fallback
item hotbar, konfirmasi keluar, volume tiga bus, dan quality preset project.
Bahasa saat ini Indonesia. UI tidak menyertakan toggle getaran atau lari otomatis
yang belum didukung gameplay. Kontrol menampilkan shortcut yang sekarang dipakai;
keybind debug pause diatur melalui theme asset.

Perubahan pengaturan disimpan saat **Terapkan**. **Kembalikan Default** mereset
draft; klik Terapkan untuk menyimpannya. Menutup menu sebelum Terapkan membuang
draft. Menu Utama memberi konfirmasi sesuai opsi dan tidak otomatis menyimpan
progress game.

## Pause gameplay

Menu memakai Time.timeScale = 0, TimeManager pause owner, player movement owner,
dan suppression prompt. Navigasi tab tidak melepas lock. Penutupan, disable,
atau penghancuran pemilik menu mengembalikan timeScale semula dan melepas hanya
lock miliknya.

GameplayInput menahan key/mouse gameplay, modal lain, dan shortcut debug selama
menu terbuka, termasuk frame buka/tutup agar ESC tidak diterima dua controller.
Input menu dan UI Unity tetap aktif. Camera follow/zoom/impulse juga ditahan.

Validasi dilakukan dengan kompilasi seluruh source runtime Unity 6000.0.81f1
dan render lima tab pada project editor terisolasi; clock, input gate, tab
navigation, serta pelepasan pause diuji. Shortcut fokus Game View dan perjalanan
scene tetap perlu diuji pada sesi Play Mode project sebenarnya.
