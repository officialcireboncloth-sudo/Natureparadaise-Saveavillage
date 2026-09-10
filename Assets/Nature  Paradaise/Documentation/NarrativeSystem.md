# Modular Dialogue, UI, and Quest System

Sistem narrative dibagi menjadi empat lapisan agar konten, aturan, object dunia, dan tampilan dapat diganti secara terpisah.

## 1. Definition Assets

- `QuestDefinitionSO`: identitas quest, prerequisite, objective, completion mode, dan reward.
- `DialogueSpeakerSO`: nama, portrait, dan warna nama pembicara.
- `DialogueConversationSO`: node, choice, condition, dan command.

Asset ini berisi data desain. Asset tidak menyimpan progres saat permainan berjalan.

## 2. Runtime Services

- `QuestService`: status quest, progres objective, reward, dan save/load.
- `DialogueService`: conversation aktif, flag cerita, condition, command, dan modal lock.
- `QuestEventHub`: penghubung ringan dari gameplay ke objective quest.

Tambahkan tepat satu `QuestService` dan satu `DialogueService` pada object scene, misalnya:

```text
00_SYSTEMS
└── Narrative
    ├── QuestService
    └── DialogueService
```

Isi `Quest Catalog` pada `QuestService` dengan quest yang dipakai build. Jika kosong, service mencari `QuestDefinitionSO` yang tersedia melalui Resources.

## 3. World Adapters

Pasang `DialogueTrigger` pada root NPC. Isi `Speaker Target Id`, default conversation, serta conditional conversation dari prioritas tertinggi ke terendah. Trigger memakai tombol E dan menerbitkan objective `Talk` setelah dialog berhasil dibuka.

Gameplay lain dapat mengirim progres tanpa mengenal UI atau isi quest:

```csharp
QuestEventHub.Publish(QuestObjectiveType.Defeat, enemyId, 1);
QuestEventHub.Publish(QuestObjectiveType.Build, buildingId, 1);
QuestEventHub.Publish(QuestObjectiveType.Custom, eventId, amount);
```

Harvest, Market Sale, dan Shipping sudah terhubung ke event hub.

## 4. UI Presenters

- `DialogueUIController` menampilkan speaker, portrait, isi node, tombol lanjut, dan pilihan.
- `QuestJournalUI` menampilkan daftar quest, detail objective, reward, status, dan Turn In.

Presenter tidak membuat desain panel. Buat Canvas/prefab UI sesuai art direction, lalu isi semua reference lewat Inspector. `Panel Root` harus berupa child dari object controller supaya controller tetap aktif ketika panel disembunyikan. `Choice Button Prefab` dan `Quest Button Prefab` harus berupa template Button dengan child TMP Text.

Hierarchy yang disarankan:

```text
50_UI
└── NarrativeCanvas
    ├── DialogueUIController
    │   └── DialoguePanel
    └── QuestJournalUI
        └── QuestJournalPanel
```

## Authoring Flow

1. Buat speaker melalui `Create > Nature Paradise > Narrative > Dialogue Speaker`.
2. Buat quest melalui `Create > Nature Paradise > Narrative > Quest Definition`.
3. Buat conversation melalui `Create > Nature Paradise > Narrative > Dialogue Conversation`.
4. Hubungkan quest ke node lewat `StartQuest`, `AddQuestProgress`, atau `CompleteQuest`.
5. Pasang conversation ke `DialogueTrigger` milik NPC.
6. Jalankan `Nature Paradise > Narrative > Validate Content` sebelum Play/build.

ID harus stabil setelah konten masuk save game. Gunakan pola seperti `main.restore_village.01`, `npc.martha`, dan `talk.martha.intro`.

## Mina Test Flow

Jalankan `Nature Paradise > Narrative > Create Test NPC and UI in Map` bila fixture test belum ada. Menu ini memasang Mina dan UI narrative langsung ke `Map.scene`.

1. Play `Map.scene`, dekati Mina, lalu tekan `E`.
2. Terima quest **Kayu untuk Mina**. Mina meminta 3 Wood.
3. Pegang Axe, tebang pohon yang valid, lalu ambil 3 Wood hasil tebangan.
4. Tracker di sisi kiri menampilkan objective dan progres `0/3` sampai `3/3`.
5. Tekan `J` kapan saja di luar dialog untuk melihat daftar quest, objective, hadiah, dan status saat ini.
6. Setelah tracker menampilkan **SIAP DILAPORKAN**, kembali ke Mina dan tekan `E`.
7. Pilih jawaban penyerahan quest. Quest selesai dan player menerima 250 Gold.
8. Interaksi berikutnya menampilkan dialog Mina versi Completed.

Status dan progres quest ikut tersimpan melalui `SaveManager`. Quest memakai ID `test.wood_for_mina`; jangan mengubah ID setelah dipakai dalam save yang ingin dipertahankan.
