# Data stamina — revisi 14 Oktober 2026

Sumber: daftar kegiatan dan keputusan berry dari pengguna pada 14 Oktober 2026.

## File yang diedit

- StaminaActions.csv: 20 kegiatan. `quantity` adalah jumlah kegiatan/jam game; `stamina_delta` adalah TOTAL pengurangan untuk jumlah tersebut. Nilai negatif berarti stamina berkurang. Contoh mencangkul: quantity=9, stamina_delta=-2. Mengubah nilai ke -4 berarti sembilan kegiatan mengurangi empat stamina. Jangan membaca -2 sebagai biaya per satu kegiatan.
- StaminaLevels.csv: kapasitas maksimum empat tingkatan: 50, 75, 100, 150. Kolom `skill_requirement` kosong karena ambang skill belum diberikan; kosong bukan nol atau otomatis unlock.
- StaminaBerry.csv: satu berry menaikkan kapasitas maksimum secara permanen ke tingkat berikutnya, tanpa menunggu skill. `tiers_per_berry=1`. Berry bukan makanan pemulih stamina biasa. `current_stamina_refill` belum diisi karena efek pengisian stamina saat ini belum ditentukan. `consume_at_max_tier=FALSE` adalah default agar buah tidak terbuang pada tingkat terakhir.
- StaminaBerrySources.csv: empat sumber yang diminta: pickup desa, supermarket, reward kebun dan reward desa. Semua memakai item_id yang sama; posisi, harga, jumlah dan syarat reward belum diberikan. Kolom kosong adalah slot data yang belum diisi, bukan nilai gratis/tanpa syarat.

## Aturan pembacaan

Berlari: -5 untuk satu jam game berlari aktif; jangan mengurangi ketika player diam. Untuk aksi berkelompok, pertahankan totalnya; jika runtime membagi per aksi, gunakan float atau akumulator, bukan pembulatan integer (contoh mencangkul -2/9). Jumlah aksi dalam satu ayunan/AOE dan pembebanan per hit versus per resource belum diubah pada runtime.

Memancing memiliki biaya cast terpisah dari hasil: cast -2, lalu berhasil -2 ATAU tidak berhasil -1. Jangan menerapkan kedua biaya hasil pada sesi yang sama.

Tingkatan kapasitas berbeda dari stamina yang sedang tersedia. Berry tidak menunggu syarat skill dan tidak menaikkan melewati 150 pada data sekarang. Semua sumber harus menggunakan item berry yang sama agar perilaku/ikon konsisten.

## Runtime aktif — 14 Oktober 2026

StaminaBalance membaca tiga CSV pertama melalui TextAsset asli pada Resources/Player/Stamina Balance.asset. Angka CSV berlaku saat memulai Play berikutnya; tidak ada salinan CSV atau biaya Inspector lama yang harus disinkronkan. Menu Nature Paradise → Data CSV → Stamina → Validate Runtime CSV memeriksa data sebelum Play. StaminaBerrySources masih merupakan slot distribusi; berry belum diletakkan di map/toko/reward.

Game baru selalu mulai dengan stamina 50. Skill dan berry memiliki tier tersimpan, dengan kapasitas memakai tier tertinggi: 50/75/100/150. Hook skill: PlayerStatusSystem.SetStaminaSkillTier(tier), tier 0–3; belum ada ambang XP otomatis karena nilainya belum diberikan. Berry menaikkan tier efektif satu tingkat secara permanen, menjaga stamina yang tersedia saat ini (tidak mengisi ulang). Pada 150 berry ditolak tanpa konsumsi. Save lama tanpa data tier dimigrasikan ke tier awal 50; save baru menyimpan tier skill dan berry.

Aksi 9x/5x dibagi menjadi biaya pecahan per aksi sukses. Run -5 per satu jam game berlari aktif memakai durasi kalender TimeManager, tidak berkurang ketika diam/menu pause. Pohon/batu menghabiskan total sesuai ukuran, dibagi proporsional damage per pukulan sampai durability habis; tunggul dihitung sebagai resource terpisah. Pilihan Stamina Size di resource bisa Auto/Small/Large. Auto batu memakai durability >1 sebagai besar; Auto pohon memakai profil bernama Small sebagai kecil, lainnya dengan durability >3 sebagai besar. Isi override untuk prefab yang kategorinya berbeda. Sickle dihitung per ayunan; cangkul multi-tile dihitung per tile valid. Modifier cuaca yang sudah ada tetap berlaku di atas biaya dasar CSV.

## Berry siap diisi artwork

Item: Resources/Items/Food/Stamina Berry.asset. Icon, Inventory Illustration, dan World Prefab sengaja kosong. Semua UI memakai referensi ItemSO yang sama. Prefab: Prefabs/Items/Stamina Berry Pickup.prefab. Isi icon/model di item lalu drag prefab ke lokasi yang diinginkan; pickup E masuk tas, konsumsi memakai PlayerEatingSystem/C atau UI makanan yang sama.

Hook reward: StaminaBerryPickup.GrantReward(inventory, amount). Nilai false berarti tas penuh/tidak valid; pengirim reward harus tetap mempertahankan reward yang belum masuk. Toko dapat mereferensikan ItemSO berry yang sama. Belum ada berry yang disebar atau dimasukkan otomatis ke katalog toko. Asset sudah dapat ditemukan saat Export Items and Crops; field increasesStaminaTier pada ItemSO tidak ditimpa oleh import CSV item lama.
