# Panduan Setup Unity 6 LTS — Fase 6 (Anime Visual Polish, VRoid + Mixamo, Audio/VFX & Build Demo `.exe`)

Fase 6 mengubah prototipe fungsional dari Fase 1–5 menjadi **demo indie game anime yang siap dimainkan dan dibagikan** (gaya kota neon *Neverness to Everness* + combat *Zenless Zone Zero*).

---

## Script Baru pada Fase 6 (`Assets/_Project/Scripts/Polish/`)
- `UrbanAnimeSceneBootstrap.cs` — Generator **1-Klik Layout District 07** lewat ContextMenu Inspector (lantai aspal, 18 gedung + papan neon bercahaya, lampu jalan URP Forward+, Cafe 07 Hub, 2 Halte Metro, Anomaly Rift & Crate).
- `CombatJuiceAndAudioBridge.cs` — Jembatan SFX combat (Slash, Parry *CLANG*, Perfect Dodge, Switch, Chain), crossfade otomatis **City BGM ↔ Battle BGM**, dan event **Camera Shake** untuk Cinemachine 3 Impulse.
- `AgentAnimatorBridge.cs` — Jembatan otomatis dari gerak & combat ke `Animator` humanoid **VRoid + Mixamo** (dengan percepatan `1.25×` saat menyerang agar terasa *snappy* ala ZZZ).
- `CombatVfxSpawner.cs` — Spawner VFX tebasan, percikan emas Parry, riak Perfect Dodge, dan Chain Attack (lengkap dengan fallback pulsa neon otomatis jika belum punya prefab VFX).

---

## Langkah 1 — 1-Klik Generate Kota District 07 (Opsional jika Scene Masih Kosong)
1. Buat Empty GameObject bernama **`CityBootstrap`**, attach **`UrbanAnimeSceneBootstrap`**.
2. Klik kanan judul komponen **`UrbanAnimeSceneBootstrap`** di Inspector → pilih **`Generate District 07 Greybox Layout`**.
3. Hubungkan `Crate_NeonPlaza` ke slot **Linked Reward Crate** di `AnomalyRift_NeonPlaza`, dan `Cafe07_Terminal` ke **Linked Cafe Terminal** di `NPC_BaristaRen`.

---

## Langkah 2 — Pasang Audio, VFX & Camera Shake (Cinemachine 3)
1. Pada **`Player`**, attach **`CombatVfxSpawner`** dan **`AgentAnimatorBridge`**.
2. Pada **`DistrictManagers`**, attach **`CombatJuiceAndAudioBridge`**:
   - Isi clip SFX gratis dari [Freesound.org](https://freesound.org/) / [Pixabay Audio](https://pixabay.com/sound-effects/) (*sword slash*, *metal parry clang*, *sci-fi slow-mo bass*, *synth-pop BGM*).
   - **Camera Shake:** Tambahkan komponen **`CinemachineImpulseSource`** pada `DistrictManagers` dan **`CinemachineImpulseListener`** pada kamera Cinemachine-mu, lalu hubungkan event **`On Camera Shake Requested`** → `CinemachineImpulseSource.GenerateImpulseWithForce`.

---

## Langkah 3 — Ubah Kapsul Menjadi Karakter Anime 3D (100% Gratis!)
1. **Buat 3 Agen Streetwear Anime di VRoid Studio (Gratis):**
   - Ekspor 3 karakter (**Kira**, **Rex**, **Vera**) dalam format `.vrm`.
2. **Import ke Unity 6 dengan UniVRM + lilToon Shader:**
   - Install **UniVRM** (`com.vrmc.univrm`) dan **lilToon** (GitHub `lilxyzw/lilToon`) via Unity Package Manager (`Add package from git URL`).
   - Drag file `.vrm` ke `Assets/_Project/Art/Characters/` → buat jadi Prefab.
   - Ganti material karakter ke shader **`lilToon`**, aktifkan **Outline** (`Width: 0.08`) dan **Rim Light** untuk gaya anime tajam seperti *ZZZ/NTE*.
3. **Tambahkan Animasi Combat dari Mixamo (Gratis):**
   - Download animasi humanoid FBX (*Idle, Run, Sprint, Jump, Sword Slash 1–4, Dodge Roll, Block/Parry*) dari [Mixamo.com](https://www.mixamo.com/) (pilih *Without Skin*, lalu di Unity Import Settings ubah **Rig → Animation Type: Humanoid**).
   - Buat **Animator Controller** dengan parameter sesuai `AgentAnimatorBridge`: `Speed (Float)`, `IsGrounded (Bool)`, `IsAttacking (Bool)`, `ComboStep (Int)`, `Jump`, `Dodge`, `Attack`, `HeavyAttack`, `AssistParry`, `ChainAttack`.
   - Assign Prefab karakter masing-masing ke slot **Visual Prefab** pada 3 aset `AgentDataSO` di `SquadManager`!

---

## Langkah 4 — Aktifkan URP Bloom & Post-Processing Neon (Kunci Nuansa NTE!)
1. Klik kanan di Hierarchy → **Volume → Global Volume**.
2. Klik **New** pada *Profile*, lalu klik **Add Override**:
   - **Bloom**: `Threshold = 0.9`, `Intensity = 0.55`, `Scatter = 0.65` → semua papan neon, lampu jalan, dan VFX Parry langsung bercahaya dramatis!
   - **Tonemapping**: `Mode = ACES` (warna malam neon lebih sinematik).
   - **Color Adjustments**: `Post Exposure = 0.15`, `Saturation = 15`.
3. Pastikan pada `Main Camera`, centang **Rendering → Post Processing**.

---

## Langkah 5 — Build Demo `.exe` Pertama!
1. Buka **File → Build Profiles** (Unity 6) → pilih **Windows** → klik **Add Open Scenes**.
2. Buka **Player Settings**:
   - Company Name & Product Name: `Project Neo-Veil`
   - Fullscreen Mode: `Fullscreen Window`
3. Klik **Build** → pilih folder `Builds/ProjectNeoVeil_Demo/` → selesai! Demo indie game anime pertamamu siap dimainkan di luar Unity Editor atau dibagikan ke teman / Itch.io.
