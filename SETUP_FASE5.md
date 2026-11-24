# Panduan Setup Unity 6 LTS — Fase 5 (Cafe 07 Hub, NPC, Dialog, Kontrak Misi, Upgrade Agen & Save/Load JSON)

Fase 5 melengkapi seluruh **Core RPG Loop** dari *Project Neo-Veil*: kamu sekarang bisa bicara dengan NPC di Cafe 07, menjalankan kontrak misi, menaikkan level agen menggunakan loot dari Anomaly Rift & Crate, dan menyimpan/memuat seluruh progres game ke file JSON lokal!

---

## Script Baru pada Fase 5
- `Assets/_Project/Scripts/Quest/DialogueDataSO.cs` & `DialogueManager.cs` — Sistem dialog gaya visual-novel.
- `Assets/_Project/Scripts/Quest/MissionDataSO.cs` & `MissionManager.cs` — Pelacak **1 Main Contract + 2 Side Contract** otomatis (Rift, Crate, Assist Parry, NPC).
- `Assets/_Project/Scripts/Quest/CityNpcInteractable.cs` — NPC interaktif kota/kafe (`[F]`).
- `Assets/_Project/Scripts/City/CafeHubTerminal.cs` — Terminal Markas **Cafe 07** (*Rest & Full Heal*, *Upgrade Level Agen*, *Save & Load*).
- `Assets/_Project/Scripts/Core/SaveSystem.cs` — Penyimpanan & pemuatan JSON lokal (`neoveil_save.json`, hotkey **`F5`** Save / **`F9`** Load).

---

## Langkah 1 — Tambahkan Manager Quest & Save pada `DistrictManagers`
Pilih GameObject **`DistrictManagers`** (yang sudah dibuat di Fase 4), lalu tambahkan 3 komponen ini:
1. **`DialogueManager`**
2. **`MissionManager`** *(biarkan array `Starting Missions` kosong dulu agar 3 kontrak bawaan otomatis aktif!)*
3. **`SaveSystem`**

---

## Langkah 2 — Buat Area Markas `Cafe 07 Hub` di Scene
1. **Terminal Kafe (`Cafe07_Terminal`):**
   - Buat **3D Object → Cube** (seperti mesin kasir/komputer kafe), posisikan di dekat `MetroStop_CafeHub`.
   - Attach **`CafeHubTerminal`**.
2. **NPC Barista (`NPC_BaristaRen`):**
   - Buat **3D Object → Capsule** di sebelah terminal kafe, beri warna berbeda.
   - Attach **`CityNpcInteractable`**.
   - *(Opsional)* Drag `Cafe07_Terminal` ke slot **Linked Cafe Terminal** agar setelah dialog selesai, menu Cafe 07 langsung terbuka!

---

## Langkah 3 — Tekan Play & Rasakan Loop Indie Action-RPG Lengkap!
1. **Bicara dengan Barista Ren (`F`)**: Kotak dialog muncul di bagian bawah layar (tekan `Space` atau klik **Lanjut**).
2. **Lihat Daftar `ACTIVE CONTRACTS`** di kanan atas:
   - Selesaikan **Anomaly Rift** → kontrak utama otomatis `✓` dan memberi bonus Credits + Cores + Disks!
   - Lakukan **2× Defensive Assist Parry** (`Golden Flash` + `Tab`/`1-3`) → side contract otomatis selesai!
   - Buka **Anomaly Crate** → side contract ketiga selesai!
3. **Kembali ke `Cafe07_Terminal` (`F`)**:
   - Klik **Rest & Espresso Boost** untuk memulihkan 100% HP seluruh squad.
   - Klik **Upgrade Kira / Rex / Vera** (`Lv.1 → Lv.2`) untuk menaikkan `MaxHP` (+110) & `ATK` (+12) secara permanen!
4. **Uji Save & Load (`F5` & `F9` atau tombol di Cafe Terminal)**:
   - Pindah ke sudut lain kota, ubah waktu ke malam (`F3`), upgrade agen, lalu tekan **`F5`** (*Quick Save*).
   - Stop Play lalu tekan **Play** lagi, tekan **`F9`** (*Quick Load*) → posisi karakter, jam kota, saldo Credits/Cores, level agen, peti terbuka, halte metro, dan progres kontrak misi langsung kembali utuh!
