using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using NeoVeil.City;
using NeoVeil.Player;
using NeoVeil.Quest;
using NeoVeil.Squad;

namespace NeoVeil.Core
{
    [Serializable]
    public class NeoVeilSaveData
    {
        public string savedAtUtc;
        public float playerX;
        public float playerY;
        public float playerZ;
        public float playerYaw;
        public float timeOfDay;

        // Economy
        public int credits;
        public int anomalyCores;
        public int dataDisks;

        // Squad
        public int activeAgentIndex;
        public float decibel;
        public int[] agentLevels;
        public float[] agentHPs;
        public float[] agentEnergies;

        // World & Contracts
        public List<string> openedCrateIds = new List<string>();
        public List<string> unlockedMetroStopIds = new List<string>();
        public List<MissionSaveRecord> missions = new List<MissionSaveRecord>();
    }

    /// <summary>
    /// Local JSON Save/Load manager writing to Application.persistentDataPath/neoveil_save.json.
    /// Persists player position, time of day, economy, 3-agent squad levels/HP/energy,
    /// opened Anomaly Crates, unlocked Metro Fast Travel stops, and Mission contracts.
    /// Quick hotkeys: F5 = Quick Save, F9 = Quick Load.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("File & Hotkeys")]
        [SerializeField] private string fileName = "neoveil_save.json";
        [SerializeField] private bool enableQuickHotkeys = true;

        [Header("Scene References (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController playerMovement;
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private DayNightNeonController dayNightController;

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, fileName);
        public bool HasSaveFile => File.Exists(SaveFilePath);

        public event Action<string> OnSaveStatusMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovementController>();
            if (squadManager == null) squadManager = FindFirstObjectByType<SquadManager>();
            if (dayNightController == null) dayNightController = FindFirstObjectByType<DayNightNeonController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!enableQuickHotkeys || Keyboard.current == null) return;

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveGame();
            }
            else if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                LoadGame();
            }
        }

        public void SaveGame()
        {
            var data = new NeoVeilSaveData
            {
                savedAtUtc = DateTime.UtcNow.ToString("o")
            };

            if (playerMovement != null)
            {
                Vector3 pos = playerMovement.transform.position;
                data.playerX = pos.x;
                data.playerY = pos.y;
                data.playerZ = pos.z;
                data.playerYaw = playerMovement.transform.eulerAngles.y;
            }

            if (dayNightController != null)
            {
                data.timeOfDay = dayNightController.TimeOfDay;
            }

            if (CityEconomyInventory.Instance != null)
            {
                data.credits = CityEconomyInventory.Instance.Credits;
                data.anomalyCores = CityEconomyInventory.Instance.AnomalyCores;
                data.dataDisks = CityEconomyInventory.Instance.DataDisks;
            }

            if (squadManager != null && squadManager.AgentCount > 0)
            {
                int n = squadManager.AgentCount;
                data.activeAgentIndex = squadManager.ActiveIndex;
                data.decibel = squadManager.Decibel;
                data.agentLevels = new int[n];
                data.agentHPs = new float[n];
                data.agentEnergies = new float[n];

                for (int i = 0; i < n; i++)
                {
                    AgentRuntimeState st = squadManager.GetAgentState(i);
                    if (st == null) continue;
                    data.agentLevels[i] = st.Level;
                    data.agentHPs[i] = st.CurrentHP;
                    data.agentEnergies[i] = st.CurrentEnergy;
                }
            }

            // Collect opened crates
            AnomalyCrate[] crates = FindObjectsByType<AnomalyCrate>(FindObjectsSortMode.None);
            for (int i = 0; i < crates.Length; i++)
            {
                if (crates[i].IsOpened && !string.IsNullOrEmpty(crates[i].CrateId))
                {
                    data.openedCrateIds.Add(crates[i].CrateId);
                }
            }

            // Collect unlocked Metro stops
            if (FastTravelManager.Instance != null)
            {
                var stops = FastTravelManager.Instance.AllStops;
                for (int i = 0; i < stops.Count; i++)
                {
                    if (stops[i] != null && stops[i].IsUnlocked)
                    {
                        data.unlockedMetroStopIds.Add(stops[i].StopId);
                    }
                }
            }

            // Collect Mission progress
            if (MissionManager.Instance != null)
            {
                data.missions = MissionManager.Instance.ExportSaveRecords();
            }

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);

            string msg = $"Game Saved! ({fileName})";
            Debug.Log($"[NeoVeil Save] {msg} -> {SaveFilePath}");
            OnSaveStatusMessage?.Invoke(msg);
        }

        public bool LoadGame()
        {
            if (!HasSaveFile)
            {
                OnSaveStatusMessage?.Invoke("Belum ada file save tersimpan.");
                return false;
            }

            string json = File.ReadAllText(SaveFilePath);
            NeoVeilSaveData data = JsonUtility.FromJson<NeoVeilSaveData>(json);
            if (data == null) return false;

            if (playerMovement != null)
            {
                Vector3 pos = new Vector3(data.playerX, data.playerY, data.playerZ);
                Vector3 fwd = Quaternion.Euler(0f, data.playerYaw, 0f) * Vector3.forward;
                playerMovement.SnapToPositionAndFace(pos, fwd);
            }

            if (dayNightController != null)
            {
                dayNightController.SetTimeOfDay(data.timeOfDay);
            }

            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.LoadFromSave(data.credits, data.anomalyCores, data.dataDisks);
            }

            if (squadManager != null)
            {
                squadManager.RestoreSquadFromSave(
                    data.activeAgentIndex,
                    data.decibel,
                    data.agentLevels,
                    data.agentHPs,
                    data.agentEnergies
                );
            }

            if (data.openedCrateIds != null && data.openedCrateIds.Count > 0)
            {
                var openedSet = new HashSet<string>(data.openedCrateIds);
                AnomalyCrate[] crates = FindObjectsByType<AnomalyCrate>(FindObjectsSortMode.None);
                for (int i = 0; i < crates.Length; i++)
                {
                    if (openedSet.Contains(crates[i].CrateId))
                    {
                        crates[i].MarkOpenedFromSave();
                    }
                }
            }

            if (FastTravelManager.Instance != null && data.unlockedMetroStopIds != null)
            {
                var unlockedSet = new HashSet<string>(data.unlockedMetroStopIds);
                var stops = FastTravelManager.Instance.AllStops;
                for (int i = 0; i < stops.Count; i++)
                {
                    if (stops[i] != null && unlockedSet.Contains(stops[i].StopId))
                    {
                        stops[i].UnlockStop();
                    }
                }
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ImportSaveRecords(data.missions);
            }

            string msg = "Game Loaded!";
            Debug.Log($"[NeoVeil Save] {msg} <- {SaveFilePath}");
            OnSaveStatusMessage?.Invoke(msg);
            return true;
        }
    }
}
