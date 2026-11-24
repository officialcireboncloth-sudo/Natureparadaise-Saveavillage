# Pembuat Pupuk dan Kualitas Panen

Implementasi lama berbasis peluang acak sudah diganti dengan aturan 7 November 2026:

- Empat resep Pembuat Pupuk: Dasar, Plus, Super, Premium.
- Kualitas deterministik: pupuk maksimal 40%, tanah maksimal 40%, air maksimal 20%.
- Durasi sementara efek pupuk: 1 / 1 / 3 / 7 hari, editable pada ItemSO/Items.csv.
- Bintang 5 membutuhkan kualitas tepat 100%. Mutasi kini memakai peluang 1% per panen; tanaman bintang 5 mendapat peluang bonus jumlah 2x sebesar 20%. Detail audit terbaru ada di Assets/Nature  Paradaise/Documentation/SystemsDataAudit.md.
- UI memakai pilihan resep, bahan tersedia/dibutuhkan, hasil, jumlah dan Buat Pupuk.
- Model mesin masih kotak dummy; antrean lama tetap dapat diambil jika ada pada save.
- Soil fertilizer/Terra tetap terpisah dari pupuk kualitas tanaman.

Panduan lengkap, lokasi asset, kontrol dan batasan balance ada di:
`Assets/Nature  Paradaise/Documentation/FertilizerMaker.md`.

Pengujian kualitas: `Tests/FertilizerQualityTests.cs`.
Pemeriksaan Unity: Nature Paradise > Soil > Validate Fertilizer Maker.
