# Project Neo-Veil

**Indie 3D Urban Supernatural Anime Action-RPG** yang menggabungkan eksplorasi distrik kota modern bergaya *Neverness to Everness (NTE)* dengan sistem pertarungan cepat & responsif bergaya *Zenless Zone Zero (ZZZ)*.

Dibangun untuk **Unity 6 LTS (6000.0 LTS)** dengan **URP (Forward+)**, **Unity New Input System**, dan **Cinemachine 3**.

---

## Status Pengembangan

- [x] **GDD & Arsitektur Teknis** — lihat [`docs/GDD.md`](docs/GDD.md)
- [x] **Fase 1 — Movement, Dodge, Soft Lock-On & Combat Feel** — lihat [`SETUP_FASE1.md`](SETUP_FASE1.md)
- [ ] **Fase 2 — ZZZ Core Combat & Squad Switch (3 Agen)**
- [ ] **Fase 3 — Enemy AI, Golden/Red Telegraph, Defensive Assist Parry & Daze/Chain Attack**
- [ ] **Fase 4 — Distrik Kota Semi-Open World, Siklus Neon Siang-Malam & Anomaly Rift**
- [ ] **Fase 5 — Markas Kafe, NPC, Misi & Save/Load JSON**
- [ ] **Fase 6 — Anime Polish (VRoid + lilToon + URP Bloom + SFX)**

---

## Struktur Script Fase 1

```text
Assets/_Project/Scripts/
├── Core/
│   ├── InputReader.cs               # Wrapper Unity New Input System (default ZZZ/NTE bindings)
│   └── TimeScaleManager.cs          # Hitstop (45ms) & Perfect Dodge Slow-Motion (0.25x)
├── Combat/
│   ├── SoftLockOnSystem.cs          # Auto-targeting halus tanpa GC allocation
│   └── Phase1SandboxDummy.cs        # Musuh dummy untuk tes Red Flash Perfect Dodge & Hitstop
└── Player/
    └── PlayerMovementController.cs  # Third-person controller, 2-charge Dodge + i-frame, Coyote Jump
```

---

## Cara Mulai di Unity 6 LTS

1. Clone repositori ini:
   ```bash
   git clone https://github.com/syahreza-za/Project-Neo-Veil.git
   ```
2. Ikuti panduan lengkap di [`SETUP_FASE1.md`](SETUP_FASE1.md) untuk membuat proyek **Unity 6 LTS (Universal 3D URP Forward+)** dan menyalin folder `Assets/_Project/`.
