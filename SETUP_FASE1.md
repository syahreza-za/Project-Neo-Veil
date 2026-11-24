# Panduan Setup Unity 6 LTS — Fase 1 (Movement, Dodge, Soft Lock-On & Combat Feel)

Paket script Fase 1 ini sudah siap langsung dipakai di **Unity 6 LTS (6000.0 LTS)** dengan template **Universal 3D (URP)**.

---

## Langkah 1 — Buat Proyek Unity 6 URP & Install Paket
1. Buka **Unity Hub** → klik **New Project** → pilih versi **Unity 6 LTS (6000.0 LTS)** → pilih template **Universal 3D (URP)**.
2. Beri nama proyek (misalnya `ProjectNeoVeil`), lalu buka proyeknya.
3. Buka **Window → Package Manager → Unity Registry**, pastikan paket berikut sudah terpasang:
   - **Input System** (jika diminta restart Editor untuk mengaktifkan New Input System, pilih **Yes**).
   - **Cinemachine** (versi 3.x bawaan Unity 6).
4. *(Rekomendasi untuk Kota Neon NTE)*: Buka **Project Settings → Graphics → URP Renderer Asset** (`PC_Renderer`), ubah **Rendering Path** ke **Forward+**.

---

## Langkah 2 — Salin Script ke Proyek Unity
Salin folder `Assets/_Project/` dari paket ini ke dalam folder `Assets/` proyek Unity-mu:
- `Assets/_Project/Scripts/Core/InputReader.cs`
- `Assets/_Project/Scripts/Core/TimeScaleManager.cs`
- `Assets/_Project/Scripts/Combat/SoftLockOnSystem.cs`
- `Assets/_Project/Scripts/Combat/Phase1SandboxDummy.cs`
- `Assets/_Project/Scripts/Player/PlayerMovementController.cs`

---

## Langkah 3 — Buat Asset `InputReader`
1. Di jendela **Project**, masuk ke `Assets/_Project/ScriptableObjects/` (buat foldernya jika belum ada).
2. Klik kanan → **Create → NeoVeil → Core → Input Reader**.
3. Beri nama `DefaultInputReader`.
   - *Tidak perlu mengisi field `Custom Input Asset` dulu!* Script sudah memiliki binding default bergaya ZZZ: `WASD` (gerak), `Mouse` (kamera), `Space` (lompat), `Klik Kanan / Shift` (Dodge & Sprint), `Klik Kiri` (Attack), `E` (Special), `Q` (Ultimate), `Tab / C` (Switch/Parry), `F` (Interact).

---

## Langkah 4 — Setup Scene Greybox (5 Menit)
1. **Lantai Jalan Kota (Greybox):**
   - Buat **3D Object → Plane** (Scale `10, 1, 10`), beri warna abu-abu gelap agar seperti aspal.
   - Tambahkan beberapa **Cube** tinggi sebagai placeholder gedung kota dan 1–2 balok rendah untuk tes lompat.
2. **Layer Musuh:**
   - Di kanan atas Editor, klik **Layers → Edit Layers**, tambahkan Layer baru bernama `Enemy`.
3. **GameObject `TimeScaleManager`:**
   - Buat Empty GameObject bernama `TimeScaleManager`.
   - Attach komponen **`TimeScaleManager`**.
4. **GameObject `Player`:**
   - Buat **3D Object → Capsule**, beri nama `Player`, posisikan di `(0, 1, 0)`.
   - Hapus `Capsule Collider` bawaan (karena kita pakai `CharacterController`), lalu **Add Component → Character Controller** (Center `0, 0, 0`, Height `2`, Radius `0.5`).
   - Tambahkan **Cube** kecil sebagai child di depan (`Z = 0.5`) sebagai penanda "wajah/depan" karakter.
   - Attach **`SoftLockOnSystem`** → atur **Enemy Layer** ke `Enemy`.
   - Attach **`PlayerMovementController`** → drag `DefaultInputReader` ke slot **Input Reader**, drag `Main Camera` ke **Camera Transform**, dan pastikan **Soft Lock On** terisi.
5. **Kamera Third-Person (Cinemachine 3):**
   - Pastikan `Main Camera` memiliki komponen **`CinemachineBrain`**.
   - Klik kanan di Hierarchy → **Cinemachine → Targeted Cameras → FreeLook Camera** (atau **Cinemachine Camera** dengan **Orbital Follow** + **Rotation Composer** + **Input Axis Controller**).
   - Set **Tracking Target** ke `Player`.
6. **Musuh Dummy untuk Uji Perfect Dodge:**
   - Buat **3D Object → Capsule**, beri nama `DummyEnemy`, posisikan di `(0, 1, 5)`.
   - Ubah **Layer** objek ini menjadi **`Enemy`**!
   - Attach **`Phase1SandboxDummy`** dan drag `Player` ke slot **Player**.

---

## Langkah 5 — Tekan Play & Uji Rasa Gerakan!
- **`WASD` + `Mouse`**: Lari bebas mengelilingi blok kota.
- **Tap `Klik Kanan` atau `Shift`**: ZZZ-style Dash/Dodge (bisa 2× beruntun). **Tahan `Shift`/`Klik Kanan`** setelah Dodge agar langsung mengalir menjadi **Sprint**!
- **Dekati `DummyEnemy` (atau tekan `T`)**: Dummy akan berkedip **MERAH (Red Flash)** selama 0.45 detik. Tekan **`Klik Kanan` / `Shift`** tepat sebelum serangannya masuk → **Perfect Dodge Slow-Motion** aktif dan dummy menyala Cyan!
- **Tekan `H`**: Uji langsung rasa **Hitstop (50ms)** yang akan dipakai di Fase 2 untuk tiap tebasan & Parry.
- Lihat tab **Scene** saat Play: garis kuning dari `SoftLockOnSystem` akan otomatis menarget `DummyEnemy` terdekat!
