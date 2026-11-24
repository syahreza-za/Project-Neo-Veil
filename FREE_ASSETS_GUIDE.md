# Panduan Aset 3D Anime & Shader Gratis (100% Legal) — Project Neo-Veil

Dokumen ini merangkum **custom shader anime bawaan repositori** dan **daftar link aset 3D, animasi, VFX, serta audio gratis** yang tinggal kamu pasang ke proyek Unity 6 LTS (URP Forward+) agar greybox langsung berubah menjadi game anime urban bergaya *Neverness to Everness × Zenless Zone Zero*.

---

## 1. Custom Anime Shader Bawaan Repo (Sudah Tersedia!)

Kamu tidak perlu membuat shader dari nol. Dua shader URP Forward+ berikut sudah ada di `Assets/_Project/Art/Shaders/`:

1. **`AnimeCelOutlineURP.shader`** (`NeoVeil/Anime Cel Toon + Outline (URP)`)
   - **Fitur:** 2-band *Cel-Shading*, *Anime Rim Light* tajam, *HDR Neon Emission* (terhubung otomatis ke URP Bloom & `DayNightNeonController`), dukungan *Forward+ Additional Lights* (lampu jalan & neon), *Shadow Caster*, dan *Inverted-Hull Outline*!
   - **Cara Pakai:** Buat Material baru → pilih Shader **`NeoVeil/Anime Cel Toon + Outline (URP)`** → terapkan ke gedung kota, peti, atau karakter!
2. **`UrbanAnimeSkyboxURP.shader`** (`NeoVeil/Urban Anime Skybox (URP)`)
   - **Fitur:** Gradien langit anime, pita cahaya neon di cakrawala, bintang malam berkedip, dan cakram matahari/bulan yang mengikuti Directional Light.
   - **Cara Pakai:** Buat Material baru bernama `Mat_UrbanAnimeSkybox` → pilih Shader **`NeoVeil/Urban Anime Skybox (URP)`** → buka **Window → Rendering → Lighting → Environment → Skybox Material** → drag `Mat_UrbanAnimeSkybox` ke sana. **`DayNightNeonController` akan otomatis mengubah warna langit & bintangnya saat tekan `F1`/`F2`/`F3`!**

---

## 2. Karakter Anime 3D (Gratis, Tanpa Bisa 3D Modeling!)

| Alat / Plugin | Link Resmi | Kegunaan |
| :--- | :--- | :--- |
| **VRoid Studio** | [vroid.com/en/studio](https://vroid.com/en/studio) · [Steam](https://store.steampowered.com/app/1486350/VRoid_Studio/) | Buat 3 Agen anime streetwear (**Kira, Rex, Vera**) layaknya *character creator*, lalu ekspor `.vrm`. |
| **UniVRM (Plugin Unity)** | [github.com/vrm-c/UniVRM/releases](https://github.com/vrm-c/UniVRM/releases) | Import file `.vrm` langsung menjadi Prefab Humanoid siap pakai di Unity 6. |
| **lilToon Shader** *(opsional khusus wajah/rambut anime)* | [github.com/lilxyzw/lilToon](https://github.com/lilxyzw/lilToon) | Shader anime paling populer untuk karakter VRoid (punya outline, rim light, matcap mata). |
| **Unity Toon Shader (Resmi Unity)** | [github.com/Unity-Technologies/com.unity.toonshader](https://github.com/Unity-Technologies/com.unity.toonshader) | Alternatif resmi Unity untuk shading cel anime di URP. |

**Pasang ke Kode Kita:** Drag Prefab karakter hasil import VRM ke slot **Visual Prefab** pada 3 aset `AgentDataSO` di `SquadManager`!

---

## 3. Animasi Combat & Eksplorasi (Gratis)

| Sumber | Link Resmi | Rekomendasi Keyword Animasi |
| :--- | :--- | :--- |
| **Adobe Mixamo** | [mixamo.com](https://www.mixamo.com/) | `Idle`, `Running`, `Fast Run`, `Jump`, `Sword And Shield Slash` (3–4 variasi), `Dodge Roll`, `Sword And Shield Block` (untuk Assist Parry), `Spin Attack` (untuk EX/Ult). |

**Tips ZZZ Feel:** Di Unity Import Settings FBX dari Mixamo, ubah **Rig → Animation Type: Humanoid**. Script **`AgentAnimatorBridge.cs`** kita sudah otomatis mempercepat animasi serangan `1.25×` agar terasa *snappy*!

---

## 4. Latar Kota Jepang / Modern (Gratis)

| Paket Aset | Link Resmi | Catatan |
| :--- | :--- | :--- |
| **Downtown City MegaKit (Quaternius)** | [quaternius.itch.io/downtown-city-megakit](https://quaternius.itch.io/downtown-city-megakit) | **300+ modul bangunan & jalanan kota**, lisensi **CC0** (100% gratis komersial!). |
| **Free Low Poly Simple Urban City (Unity Asset Store)** | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/urban/free-low-poly-simple-urban-city-3d-asset-pack-239474) | Paket kota modern ringan, cocok untuk digabung dengan shader `AnimeCelOutlineURP`. |
| **Japanese Otaku City (ZENRIN)** | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/urban/japanese-otaku-city-20359) | Kota Akihabara / distrik anime Jepang asli untuk nuansa *Neverness to Everness*! |
| **Kenney Game Assets (CC0)** | [kenney-assets.itch.io](https://kenney-assets.itch.io/) | Props kota (mobil, lampu jalan, halte bus, kafe) berlisensi CC0. |
| **Katalog Kota Gratis Unity** | [assetstore.unity.com/popular-assets/cities](https://assetstore.unity.com/popular-assets/cities) | Filter *Free* untuk tambahan toko, stasiun metro, dan props jalan. |

---

## 5. VFX Tebasan, Percikan Parry & Partikel URP (Gratis)

| Paket VFX | Link Resmi | Pasang ke Slot `CombatVfxSpawner.cs` |
| :--- | :--- | :--- |
| **Free Game VFX Collection (URP)** | [Unity Asset Store](https://assetstore.unity.com/packages/vfx/particles/free-game-vfx-collection-urp-345212) | Slash VFX, Hit Impact, Burst. |
| **Free Slash VFX** | [Unity Asset Store](https://assetstore.unity.com/packages/vfx/shaders/free-slash-vfx-309498) | `lightSlashVfxPrefab` & `heavySlashVfxPrefab`. |
| **Free Quick Effects Vol. 1 (Gabriel Aguiar)** | [Unity Asset Store](https://assetstore.unity.com/packages/vfx/particles/free-quick-effects-vol-1-304424) | `assistParryVfxPrefab`, `perfectDodgeVfxPrefab`, `chainBurstVfxPrefab`. |
| **Unity Particle Pack (Starter Assets)** | [Unity Asset Store](https://assetstore.unity.com/packages/vfx/particles/particle-pack-starter-assets-127325) | Efek api, listrik, asap, dan portal Anomaly Rift. |

---

## 6. SFX Combat & BGM Kota Neon (Gratis)

- **SFX Tebasan, Parry CLANG, Dodge Whoosh:** [freesound.org](https://freesound.org/) & [pixabay.com/sound-effects](https://pixabay.com/sound-effects/) — pasang ke **`CombatJuiceAndAudioBridge.cs`**.
- **BGM Synth-Pop / Lo-Fi Kota & Electronic Battle:** [pixabay.com/music](https://pixabay.com/music/) (cari *cyberpunk synth*, *anime electronic battle*, *city pop lo-fi*).
