# Project Neo-Veil

**Indie 3D Urban Supernatural Anime Action-RPG** yang menggabungkan eksplorasi distrik kota modern bergaya *Neverness to Everness (NTE)* dengan sistem pertarungan cepat & responsif bergaya *Zenless Zone Zero (ZZZ)*.

Dibangun untuk **Unity 6 LTS (6000.0 LTS)** dengan **URP (Forward+)**, **Unity New Input System**, dan **Cinemachine 3**.

---

## Status Pengembangan — Seluruh 6 Fase Fondasi Selesai!

- [x] **GDD & Arsitektur Teknis** — lihat [`docs/GDD.md`](docs/GDD.md)
- [x] **Fase 1 — Movement, Dodge, Soft Lock-On & Combat Feel** — lihat [`SETUP_FASE1.md`](SETUP_FASE1.md)
- [x] **Fase 2 — ZZZ Core Combat & Squad Switch (3 Agen)** — lihat [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md)
- [x] **Fase 3 — Enemy AI, Golden/Red Telegraph, Defensive Assist Parry & Daze/Chain Attack** — lihat [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md)
- [x] **Fase 4 — Distrik Kota Semi-Open World, Siklus Neon Siang-Malam, Anomaly Rift & Fast Travel** — lihat [`SETUP_FASE4.md`](SETUP_FASE4.md)
- [x] **Fase 5 — Markas Cafe 07, NPC, Dialog, Kontrak Misi, Upgrade Agen & Save/Load JSON** — lihat [`SETUP_FASE5.md`](SETUP_FASE5.md)
- [x] **Fase 6 — Anime Polish (1-Click Bootstrap, Audio/BGM, VFX, Animator VRoid+Mixamo, URP Bloom & Build Demo)** — lihat [`SETUP_FASE6_POLISH.md`](SETUP_FASE6_POLISH.md)

---

## Struktur Lengkap Script (`Assets/_Project/Scripts/`)

```text
Assets/_Project/Scripts/
├── Core/
│   ├── InputReader.cs               # Wrapper Unity New Input System (default ZZZ/NTE bindings + 1/2/3)
│   ├── TimeScaleManager.cs          # Hitstop (45–65ms) & Perfect Dodge / Chain Slow-Motion
│   └── SaveSystem.cs                # Save/Load JSON lokal (posisi, waktu, loot, squad, misi — F5/F9)
├── Player/
│   └── PlayerMovementController.cs  # Third-person controller, 2-charge Dodge + i-frame, Attack Lunge
├── Squad/
│   ├── AgentDataSO.cs               # ScriptableObject data Agen (Striker / Breaker / Channeler)
│   ├── SquadManager.cs              # Squad 3 Agen, Quick Switch, Assist Parry, Leveling & Decibel
│   └── ChainAttackController.cs     # Jendela slow-mo Chain Attack saat Daze musuh 100%
├── Combat/
│   ├── AttackDataSO.cs              # ScriptableObject data serangan (damage, daze, hitstop, lunge)
│   ├── SoftLockOnSystem.cs          # Auto-targeting halus tanpa GC allocation
│   ├── HealthAndDazeComponent.cs    # HP musuh, Daze Bar (0-100%), Stun ×1.5 DMG, Anomaly Burst
│   ├── CombatController.cs          # 4-Hit Combo, Dodge Counter, EX Special (E), Ultimate (Q)
│   └── Phase1SandboxDummy.cs        # Dummy pengujian Fase 1
├── AI/
│   ├── TelegraphSystem.cs           # Sinyal GoldenFlash (Parryable) & RedFlash (Must Dodge)
│   └── EnemyAIController.cs         # FSM Musuh (Chase, Telegraph, Strike, Recover, Stunned, Dead)
├── City/
│   ├── IInteractable.cs             # Kontrak interaksi universal [F]
│   ├── PlayerInteractor.cs          # Deteksi objek interaktif terdekat tanpa GC allocation
│   ├── CityEconomyInventory.cs      # Penyimpanan Credits, Anomaly Cores & Data Disks
│   ├── DayNightNeonController.cs    # Siklus Siang–Sore–Malam + lampu & papan neon URP Forward+
│   ├── AnomalyRiftZone.cs           # Encounter multi-wave Anomaly Rift di jalanan kota
│   ├── AnomalyCrate.cs              # Peti loot kota (terkunci sampai Rift selesai)
│   ├── MetroFastTravelStop.cs       # Node halte bus/stasiun metro Fast Travel
│   ├── FastTravelManager.cs         # Koordinator teleport aman antar halte distrik
│   └── CafeHubTerminal.cs           # Terminal Markas Cafe 07 (Rest/Heal, Upgrade Agen, Save/Load)
├── Quest/
│   ├── DialogueDataSO.cs            # ScriptableObject baris dialog NPC
│   ├── DialogueManager.cs           # Pemutar dialog visual-novel & pengunci gerak sementara
│   ├── MissionDataSO.cs             # ScriptableObject kontrak misi utama & sampingan
│   ├── MissionManager.cs            # Pelacak kontrak otomatis (Rift, Crate, Assist Parry, NPC)
│   └── CityNpcInteractable.cs       # NPC interaktif kota & Cafe 07
├── Polish/
│   ├── UrbanAnimeSceneBootstrap.cs  # Generator 1-klik layout greybox District 07
│   ├── CombatJuiceAndAudioBridge.cs # SFX combat, dynamic City↔Battle BGM & Camera Shake bridge
│   ├── AgentAnimatorBridge.cs       # Jembatan otomatis Animator VRoid + Mixamo (1.25x ZZZ speed)
│   └── CombatVfxSpawner.cs          # Spawner VFX Slash, Parry, Dodge & Chain + fallback pulsa neon
└── UI/
    └── CombatHUDDebugOverlay.cs     # HUD lengkap greybox (Squad, Clock, Contracts, Dialogue, Fast Travel & Cafe Hub)
```

---

## Cara Mulai di Unity 6 LTS

1. Clone repositori ini:
   ```bash
   git clone https://github.com/syahreza-za/Project-Neo-Veil.git
   ```
2. Ikuti panduan berurutan:
   [`SETUP_FASE1.md`](SETUP_FASE1.md) → [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md) → [`SETUP_FASE4.md`](SETUP_FASE4.md) → [`SETUP_FASE5.md`](SETUP_FASE5.md) → [`SETUP_FASE6_POLISH.md`](SETUP_FASE6_POLISH.md).
