# Kitchen update — 9 October 2026

- KitchenSet pada kompor modular house membuka UI uGUI MEMASAK menggantikan IMGUI. Layout panel kanan, recipe/ingredient scroll, output image slots, jumlah batch, kebutuhan equipment/level, hasil dan grade preview.
- KitchenTheme memberi slot artwork dekorasi/status; Shared Item Image Slots mengedit ikon/ilustrasi ItemSO asli yang dipakai bersama seluruh UI. Row bahan dipakai ulang saat count berubah.
- Recipes.csv ditambah output_quality_mode dan fixed_output_quality (legacy CSV tetap kompatibel). Import memvalidasi seluruh tabel sebelum melakukan perubahan, termasuk ID/path duplikat dan batas nilai; gambar tidak ditimpa.
- Items.csv + asset item/recipe baru Sup Sayur tersedia sebagai contoh. Bahan awal memakai item.cabbage x2 + item.milk x1, hasil item.vegetable_soup x1. Lima resep lama dipertahankan.
- KitchenService mempertahankan bahan Tas + Kulkas, unlock House Lv.3, equipment, waktu, collection/quest. Output quality dapat memakai IngredientAverage atau Fixed 0–5.
- Modal memblokir gameplay, menjaga owner locks, memulihkan timeScale/cursor/map/menu saat close/disable/destroy.

Validasi: runtime + editor compile lulus. Unity terisolasi: CSV import/referensi item, invalid import tanpa perubahan parsial, legacy grade, interaksi kompor, batch cooking, dua mode grade, kekurangan bahan/tas penuh, shared Sprite dan cleanup modal lulus. Preview 1920×1080 diperiksa. Primary Unity project tidak diluncurkan ulang oleh tes.
