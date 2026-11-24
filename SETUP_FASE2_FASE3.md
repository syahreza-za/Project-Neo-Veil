# Panduan Setup Unity 6 LTS — Fase 2 & 3 (ZZZ Combat, Squad 3 Agen, Assist Parry, Daze & Chain Attack)

Setelah menyelesaikan [`SETUP_FASE1.md`](SETUP_FASE1.md), ikuti panduan singkat ini (~5 menit) untuk mengaktifkan seluruh sistem pertarungan gaya **Zenless Zone Zero** di proyek Unity 6 LTS-mu!

---

## Script Baru pada Fase 2 & 3
- `Assets/_Project/Scripts/Combat/AttackDataSO.cs` — Data tiap jurus (combo 1–4, Dodge Counter, EX, Ult).
- `Assets/_Project/Scripts/Combat/HealthAndDazeComponent.cs` — HP musuh, **Daze Bar (0–100%)**, Stun `×1.5` damage, dan *Anomaly Burst*.
- `Assets/_Project/Scripts/Combat/CombatController.cs` — 4-Hit Combo, *Attack Lunge*, Dodge Counter, EX Special (`E`), Ultimate (`Q`).
- `Assets/_Project/Scripts/Squad/AgentDataSO.cs` — Data 3 Agen (*Striker*, *Breaker*, *Channeler*).
- `Assets/_Project/Scripts/Squad/SquadManager.cs` — Squad 3 Agen, Quick Switch (`Tab`/`C`/`1-3`), dan **Defensive Assist Parry**.
- `Assets/_Project/Scripts/Squad/ChainAttackController.cs` — Jendela slow-mo **Chain Attack** saat musuh *Stunned*.
- `Assets/_Project/Scripts/AI/TelegraphSystem.cs` — Sinyal **Golden Flash** (Parryable) & **Red Flash** (Must Dodge).
- `Assets/_Project/Scripts/AI/EnemyAIController.cs` — AI Musuh (NavMeshAgent + fallback otomatis jika belum di-bake).
- `Assets/_Project/Scripts/UI/CombatHUDDebugOverlay.cs` — HUD siap pakai (Squad HP/Energy, Decibel, Combo, Daze, Alert Flash & Chain Attack).

---

## Langkah 1 — Tambahkan Komponen Combat & Squad pada GameObject `Player`
Buka scene greybox dari Fase 1, pilih **`Player`**, lalu tambahkan 3 komponen berikut:
1. **`SquadManager`**
   - Drag `DefaultInputReader` ke **Input Reader**.
   - *(Opsional)* Biarkan array **Squad Agents** kosong dulu jika ingin langsung mencoba 3 agen preset otomatis (**Kira/Striker [Cyan]**, **Rex/Breaker [Amber]**, **Vera/Channeler [Magenta]**), atau buat aset kustom lewat **Klik Kanan → Create → NeoVeil → Squad → Agent Data**.
2. **`ChainAttackController`**
   - Atur **Enemy Layer** ke **`Enemy`**.
3. **`CombatController`**
   - Drag `DefaultInputReader` ke **Input Reader**.
   - Atur **Enemy Layer** ke **`Enemy`**.

---

## Langkah 2 — Tambahkan HUD Debug Overlay
1. Buat Empty GameObject bernama **`CombatHUD`** (atau attach langsung di `TimeScaleManager`).
2. Attach **`CombatHUDDebugOverlay`** — komponen akan otomatis mencari `SquadManager`, `CombatController`, `ChainAttackController`, dan `SoftLockOnSystem`.

---

## Langkah 3 — Upgrade Musuh Menjadi Musuh ZZZ Lengkap
1. Pilih **`DummyEnemy`** (atau buat 2–3 **3D Object → Capsule** baru, beri nama `RiftStalker_01`, `RiftStalker_02`, `AbyssEnforcer`).
2. Pastikan **Layer** semua musuh diset ke **`Enemy`**!
3. Hapus `Phase1SandboxDummy` (opsional), lalu attach **`EnemyAIController`** — Unity akan otomatis ikut menambahkan **`HealthAndDazeComponent`** dan **`TelegraphSystem`**.
4. *(Opsional untuk Elite Boss)*: Pada `AbyssEnforcer`, ubah **Max HP** ke `1000`, **Max Daze** ke `140`, dan **Scale** ke `(1.4, 1.4, 1.4)`.
5. *(Opsional NavMesh)*: Kamu belum wajib bake NavMesh untuk mengetes — `EnemyAIController` punya pergerakan fallback otomatis. Jika sudah siap pakai NavMesh, tambahkan komponen **`NavMeshAgent`** pada musuh dan **`NavMeshSurface`** pada lantai kota lalu klik **Bake**.

---

## Langkah 4 — Tekan Play & Uji Seluruh Mekanik ZZZ!
- **`Klik Kiri`**: 4-Hit Basic Combo dengan *Soft Lock-On*, *Attack Lunge*, dan *Hitstop*.
- **Tunggu musuh berkedip EMAS (`GOLDEN FLASH`)** → tekan **`Tab` / `C` / `1` / `2` / `3`**:
  - Agen berikutnya langsung melompat ke depan musuh, memicu **Defensive Assist Parry** (`65ms` Hitstop + damage besar + lonjakan Daze Bar!).
- **Tunggu musuh berkedip MERAH (`RED FLASH`)** → tekan **`Klik Kanan` / `Shift`**:
  - Memicu **Perfect Dodge Slow-Motion** → segera tekan **`Klik Kiri`** untuk melancarkan **Dodge Counter**!
- **Penuhi Daze Bar musuh hingga 100%** (pakai **Rex [2]** untuk Daze 2× lebih cepat!):
  - Musuh masuk status **STUNNED** (`×1.5–1.75` damage) dan jendela slow-mo **CHAIN ATTACK** muncul → tekan **`Tab` / `1` / `2` / `3`** untuk serangan AoE besar!
- **`E` (EX Special)**: Saat Energy ≥ 50, lepaskan serangan AoE kuat dengan *i-frame*.
- **`Q` (Ultimate)**: Saat Decibel mencapai 100%, lepaskan serangan Ultimate!
