# Project Neo-Veil

**Indie 3D Urban Supernatural Anime Action-RPG** yang menggabungkan eksplorasi distrik kota modern bergaya *Neverness to Everness (NTE)* dengan sistem pertarungan cepat & responsif bergaya *Zenless Zone Zero (ZZZ)*.

Dibangun untuk **Unity 6 LTS (6000.0 LTS)** dengan **URP (Forward+)**, **Unity New Input System**, dan **Cinemachine 3**.

---

## Status Pengembangan

- [x] **GDD & Arsitektur Teknis** — lihat [`docs/GDD.md`](docs/GDD.md)
- [x] **Fase 1 — Movement, Dodge, Soft Lock-On & Combat Feel** — lihat [`SETUP_FASE1.md`](SETUP_FASE1.md)
- [x] **Fase 2 — ZZZ Core Combat & Squad Switch (3 Agen)** — lihat [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md)
- [x] **Fase 3 — Enemy AI, Golden/Red Telegraph, Defensive Assist Parry & Daze/Chain Attack** — lihat [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md)
- [ ] **Fase 4 — Distrik Kota Semi-Open World, Siklus Neon Siang-Malam & Anomaly Rift**
- [ ] **Fase 5 — Markas Kafe, NPC, Misi & Save/Load JSON**
- [ ] **Fase 6 — Anime Polish (VRoid + lilToon + URP Bloom + SFX)**

---

## Struktur Script (`Assets/_Project/Scripts/`)

```text
Assets/_Project/Scripts/
├── Core/
│   ├── InputReader.cs               # Wrapper Unity New Input System (default ZZZ/NTE bindings + 1/2/3)
│   └── TimeScaleManager.cs          # Hitstop (45–65ms) & Perfect Dodge / Chain Slow-Motion
├── Player/
│   └── PlayerMovementController.cs  # Third-person controller, 2-charge Dodge + i-frame, Attack Lunge
├── Squad/
│   ├── AgentDataSO.cs               # ScriptableObject data Agen (Striker / Breaker / Channeler)
│   ├── SquadManager.cs              # Squad 3 Agen, Quick Switch, Defensive Assist Parry, Decibel
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
└── UI/
    └── CombatHUDDebugOverlay.cs     # HUD siap pakai untuk uji greybox Fase 2 & 3
```

---

## Cara Mulai di Unity 6 LTS

1. Clone repositori ini:
   ```bash
   git clone https://github.com/syahreza-za/Project-Neo-Veil.git
   ```
2. Ikuti [`SETUP_FASE1.md`](SETUP_FASE1.md) untuk membuat proyek **Unity 6 LTS (Universal 3D URP Forward+)**, lalu lanjutkan dengan [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md).
