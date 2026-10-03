# Menu Istirahat

Interaksi E pada kasur (atau PlayerBed.Sleep dari tombol mobile) membuka menu dahulu.

- Tidur Tanpa Save: menjalankan animasi, pergantian waktu/hari, pemulihan, dan posisi bangun tanpa menulis save.
- Tidur & Save Game: menyimpan setelah bangun di posisi berdiri, memakai SaveManager yang sudah ada.
- Load Game: memuat save yang tersedia; nonaktif jika belum ada save, kalender save tidak valid, atau SaveManager tidak tersedia. Progress yang belum disimpan diganti oleh save.
- Batal / Esc: menutup menu tanpa tidur.

Panah atas/bawah memilih, Enter menjalankan pilihan. Penanda panah putih menunjukkan pilihan keyboard. Tombol berwarna abu-abu secara normal dan hijau hanya saat hover. Di editor, shortcut menu dibaca ketika Game View mendapat fokus.

Menu menghentikan time scale, jam game, gerakan, input gameplay, dan prompt dunia; semua lock miliknya dilepas saat ditutup atau dihancurkan. Pause menu tidak menumpuk di atas menu istirahat.

Isi Sprite melalui **Nature Paradise > UI > Bed Rest > Select Image Slots**, atau asset Resources/UI/BedRestTheme. Slot panel, sleepIcon, sleepAndSaveIcon, loadIcon, dan backIcon sengaja kosong; tidak ada gambar latar kamar atau ikon buatan kode. Latar adalah world game yang sedang dimainkan.

Footer membaca hari dan jam dari savegame.json yang sesungguhnya. Tidur paksa/debug dan faint tetap menggunakan aturan penyimpanan sebelumnya.
