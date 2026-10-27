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

## Transisi kasur

Alur kasur memakai opsi duduk: player melepas alat, duduk di tepi kasur, layar perlahan menjadi hitam, waktu berganti, lalu layar dibuka dengan player sudah berdiri di lantai di sisi kasur. Pose duduk mengambil momen duduk dari clip Wake Up Bed yang sudah ada dan diblend selama 0.8 detik; asset animasi asli tidak diubah atau digandakan. Player tidak lagi memainkan seluruh animasi bangun di atas kasur. Fade memakai satu Canvas layar penuh yang dipakai ulang, dengan waktu unscaled dan langkah per frame dibatasi agar pergantian hari yang berat tidak melewati fade.

PlayerBed memeriksa kedua sisi panjang kasur serta beberapa posisi sepanjang sisinya dengan ukuran CharacterController. Titik harus berada di lantai rumah dan bebas dari dinding, tempat tidur, serta furnitur; permukaan kasur/meja tidak menjadi lantai bangun. Pilihan dicek lagi setelah event pergantian hari. Bila seluruh sisi sudah terhalang sebelum tidur, aksi ditolak tanpa mengganti hari. Root/capsule tetap di sisi lantai yang aman sementara hanya visual tubuh diposisikan ke tepi kasur. Interupsi transisi memulihkan animator, controller, overlay, serta lock milik transisi saja.

Pergantian hari tetap terjadi tepat sekali; tidur sesudah tengah malam hingga sebelum pukul 06:00 hanya memajukan jam bangun karena daily reset sudah terjadi pada tengah malam. Tidur & Save menyimpan setelah pose berdiri dipulihkan. Semua level rumah memakai alur yang sama tanpa perubahan layout atau save schema.
