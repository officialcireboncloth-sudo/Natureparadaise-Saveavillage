# Gameplay HUD

HUD dibuat oleh controller yang sudah dipakai gameplay. Buka Map lalu Play;
GameplayUIBootstrap memuat scene GameplayUI secara additive. Tidak perlu
memasang canvas lain atau mengganti scene Map.

- Kiri atas: health merah, stamina hijau, nilai current/max. Hunger tetap
  tersedia sebagai bar ketiga bila fitur hunger aktif. Buff/debuff di bawahnya.
- Kanan atas: hari, nama musim Indonesia, jam game, dan uang dengan pemisah
  ribuan Indonesia. Ikon matahari menjadi dekorasi kalender, bukan indikator
  cuaca. Detail cuaca tetap tersedia lewat Debug Clues/F9.
- Menu: F7 di Game View editor, ESC di build. Lima tab dan slot artwork tersedia
  melalui PauseMenuTheme; lihat PauseMenuUI.md. Menggunakan pause clock,
  movement lock, suppression prompt, dan menyimpan/mengembalikan timeScale.
- Kanan: quest aktif dan progress dari QuestService. Tombol minus/plus melipat
  isi tracker; J membuka journal yang sudah ada. Quest tanpa data tenggat tidak
  menampilkan deadline contoh dari gambar. Tracker tersembunyi saat tidak ada
  quest aktif atau saat modal interaksi terbuka.
- Bawah tengah: hotbar dengan nomor slot, icon ItemSO, jumlah stack, dan garis
  mint pada slot terpilih. Jumlah slot mengikuti inventory (sekarang 8).
  Slot kosong tetap kosong; item tanpa icon memakai nama singkat sebagai fallback.
- Di atas slot aktif: nama item, level alat dari PlayerStatusSystem, dan hint
  kontrol gameplay yang sebenarnya. Watering Can menampilkan current/max water
  serta gauge mint; label water lama di atas karakter tidak ditampilkan bersamaan.
- Prompt interaksi dunia memakai surface membulat biru transparan, mengikuti
  target dunia serta binding kontrol yang sudah digunakan gameplay.

Surface dan ikon dasar dibentuk dari mesh Unity tanpa file gambar tambahan.
Ikon item memakai `ItemSO.icon`; artwork yang belum ada bisa diisi di asset item.
Sprite panel/slot opsional pada controller tetap tersedia. Nonaktifkan
`Reference HUD Style` pada InventoryHotbarUI untuk memakai warna custom hotbar.

Canvas status dan hotbar menggunakan SafeAreaFitter serta resolusi referensi
1920 x 1080. Runtime canvas status dimiliki scene HUD; hotbar mengikuti player,
agar lifetime UI tidak terlepas dari pemiliknya saat pergantian scene.

Validasi: seluruh source runtime dikompilasi dengan compiler dan reference
assembly Unity 6000.0.81f1. Render dan pengujian data dilakukan pada project
sementara terisolasi. Playthrough Map, save/load, dan perjalanan interior tetap
perlu diuji dalam sesi gameplay sebenarnya.
