# Nature Paradise — Code Documentation Standard

Dokumen ini menjadi aturan dokumentasi untuk script yang ditambahkan atau diubah setelah ini.

## Bahasa dan penamaan

- Nama class, method, property, event, dan variable menggunakan bahasa Inggris.
- Komentar teknis menggunakan bahasa Indonesia yang singkat dan formal.
- Istilah kode seperti `Inventory`, `FieldArea`, `SaveData`, dan `ItemSO` tidak diterjemahkan.

## Wajib pada setiap type

- Setiap class, struct, enum, interface, dan data transfer object memiliki XML `<summary>`.
- Summary menjelaskan tanggung jawab dan batas sistem, bukan mengulang nama class.
- Method publik yang menjadi API antarsistem memiliki `<summary>` dan menjelaskan efek samping penting.

## Inspector

- Gunakan `[Header]` untuk mengelompokkan konfigurasi berdasarkan fungsi.
- Gunakan `[Tooltip]` jika arti nilai, satuan, fallback, atau risikonya tidak langsung jelas.
- Gunakan validasi seperti `[Min]` dan `[Range]` untuk mencegah konfigurasi tidak valid.
- Sebutkan jika ID tidak boleh diubah setelah dipakai save production.

## Komentar implementasi

- Jelaskan alasan keputusan, batasan, kompatibilitas, performa, dan urutan dependensi.
- Jangan mengomentari baris yang sudah jelas dari nama method/variable.
- Catat loop yang sengaja dibuat batch/interval untuk target mobile.
- Catat jalur migrasi save lama dan jangan menghapusnya tanpa keputusan versi data.

## Contoh

```csharp
/// <summary>
/// Mengurangi stamina secara atomik jika jumlah yang tersedia mencukupi.
/// </summary>
public bool TrySpendStamina(float amount)
```

```csharp
[Tooltip("Moisture yang hilang setiap pergantian jam game pada tile aktif.")]
[SerializeField, Min(0)] int hourlyEvaporation = 2;
```

```csharp
// Hanya tile aktif yang diproses agar field besar tidak melakukan simulasi
// terhadap ratusan tile kosong pada perangkat mobile.
```

