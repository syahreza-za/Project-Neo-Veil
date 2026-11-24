# Panduan Setup Unity 6 LTS — Fase 4 (Distrik Kota Semi-Open World, Siklus Neon, Anomaly Rift & Fast Travel)

Setelah menyelesaikan [`SETUP_FASE1.md`](SETUP_FASE1.md) dan [`SETUP_FASE2_FASE3.md`](SETUP_FASE2_FASE3.md), ikuti panduan singkat ini untuk menghidupkan **District 07** bergaya *Neverness to Everness* di Unity 6 LTS (URP Forward+)!

---

## Script Baru pada Fase 4 (`Assets/_Project/Scripts/City/`)
- `IInteractable.cs` & `PlayerInteractor.cs` — Sistem interaksi universal (`[F]`) untuk seluruh objek kota.
- `CityEconomyInventory.cs` — Penyimpanan Credits, Anomaly Cores, dan Data Disks dari eksplorasi & Rift.
- `DayNightNeonController.cs` — Siklus Siang → Sore (*Cyber Dusk*) → Malam (*Neon Night*) yang otomatis menyalakan lampu jalan & papan neon URP Forward+ (plus hotkey uji `F1`/`F2`/`F3`).
- `AnomalyRiftZone.cs` — Portal anomali di jalanan kota dengan encounter multi-wave (otomatis memunculkan musuh greybox jika prefab belum diisi) dan membuka peti hadiah saat distabilkan.
- `AnomalyCrate.cs` — Peti loot kota di gang/rooftop (bisa terkunci sampai Rift terdekat selesai).
- `MetroFastTravelStop.cs` & `FastTravelManager.cs` — Halte Bus / Stasiun Metro untuk Fast Travel instan antar sudut kota.

---

## Langkah 1 — Tambahkan Interaksi pada `Player`
1. Pilih GameObject **`Player`** di Hierarchy.
2. Attach **`PlayerInteractor`**, lalu drag `DefaultInputReader` ke slot **Input Reader**.

---

## Langkah 2 — Tambahkan Manager Kota (1 GameObject `DistrictManagers`)
1. Buat Empty GameObject bernama **`DistrictManagers`**.
2. Attach 3 komponen berikut:
   - **`CityEconomyInventory`**
   - **`FastTravelManager`**
   - **`DayNightNeonController`**:
     - Drag `Directional Light` di scene ke slot **Sun Light**.
     - *(Opsional lampu neon kota)*: Tambahkan beberapa `Point Light` (warna Cyan/Magenta) di dekat blok gedung, lalu drag ke array **City Neon Lights**.
     - *(Opsional papan neon)*: Buat beberapa `Cube` tipis dengan Material URP Lit yang properti **Emission**-nya aktif, lalu drag ke array **Neon Sign Renderers**.

---

## Langkah 3 — Tempatkan POI di Distrik Kota Greybox
1. **Anomaly Rift + Peti Hadiah Terkunci:**
   - Buat **3D Object → Cylinder** pipih (Scale `3, 0.1, 3`), beri nama `AnomalyRift_Plaza`, posisikan di tengah jalan.
   - Attach **`AnomalyRiftZone`** (otomatis menggunakan 2 wave default dengan musuh greybox yang sudah punya AI + Golden/Red Telegraph!).
   - Di dekatnya, buat **3D Object → Cube** kecil bernama `Crate_Plaza`, attach **`AnomalyCrate`**, lalu drag `Crate_Plaza` ke slot **Linked Reward Crate** milik `AnomalyRift_Plaza`!
2. **2–3 Halte Metro Fast Travel:**
   - Buat 2 atau 3 **3D Object → Cube** tiang halte di sudut berbeda kota:
     - `MetroStop_NeonPlaza` (`Stop Display Name`: `Neon Plaza Metro`, centang `Unlocked By Default`).
     - `MetroStop_CafeHub` (`Stop Display Name`: `Cafe 07 Hub`, centang `Unlocked By Default`).
     - `MetroStop_Rooftop` (`Stop Display Name`: `Skyline Overpass`, *uncheck* `Unlocked By Default` agar pemain harus menemukannya dulu!).
   - Attach **`MetroFastTravelStop`** ke masing-masing objek.

---

## Langkah 4 — Tekan Play & Jelajahi District 07!
- **Tekan `F1` / `F2` / `F3`**: Lihat transisi langsung antara **Anime Day**, **Cyber Dusk**, dan **Neon Night** lengkap dengan lampu jalan & neon yang menyala otomatis!
- **Dekati `AnomalyRift_Plaza` → tekan `F`**: Mulai encounter multi-wave ZZZ; kalahkan semua wave untuk menstabilkan Rift dan membuka kunci `Crate_Plaza`!
- **Dekati `Crate_Plaza` → tekan `F`**: Klaim **Credits, Anomaly Cores, dan Data Disks** (langsung tampil di HUD atas).
- **Dekati Halte Metro → tekan `F`**: Buka menu **Metro Fast Travel** dan klik tujuan untuk teleport instan antar distrik!
