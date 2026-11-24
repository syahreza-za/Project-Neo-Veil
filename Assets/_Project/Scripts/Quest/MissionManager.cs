using System;
using System.Collections.Generic;
using UnityEngine;
using NeoVeil.City;
using NeoVeil.Combat;
using NeoVeil.Squad;

namespace NeoVeil.Quest
{
    [Serializable]
    public class MissionRuntimeState
    {
        public MissionDataSO Data;
        public int CurrentCount;
        public bool IsCompleted;

        public string ProgressLabel => Data != null
            ? $"{Mathf.Min(CurrentCount, Data.requiredCount)}/{Data.requiredCount}"
            : "0/0";
    }

    [Serializable]
    public class MissionSaveRecord
    {
        public string missionId;
        public int currentCount;
        public bool isCompleted;
    }

    /// <summary>
    /// Tracks active Main & Side Contracts across District 07.
    /// Automatically listens to AnomalyRiftZone, AnomalyCrate, SquadManager (Assist Parries),
    /// and CityNpcInteractable without requiring manual Inspector cross-wiring!
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Starting Contracts (Auto-Populates 3 Starter Contracts if Empty)")]
        [SerializeField] private MissionDataSO[] startingMissions;

        [Header("Optional Reference (Auto-Found if Empty)")]
        [SerializeField] private SquadManager squadManager;

        private readonly List<MissionRuntimeState> _missions = new List<MissionRuntimeState>();

        public IReadOnlyList<MissionRuntimeState> Missions => _missions;

        public event Action OnMissionsUpdated;
        public event Action<MissionDataSO> OnMissionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (squadManager == null)
            {
                squadManager = FindFirstObjectByType<SquadManager>();
            }

            InitializeStartingMissions();
        }

        private void OnEnable()
        {
            AnomalyRiftZone.OnAnyRiftCleared += HandleRiftCleared;
            AnomalyCrate.OnAnyCrateOpened += HandleCrateOpened;

            if (squadManager != null)
            {
                squadManager.OnDefensiveAssistParry += HandleAssistParry;
            }
        }

        private void OnDisable()
        {
            AnomalyRiftZone.OnAnyRiftCleared -= HandleRiftCleared;
            AnomalyCrate.OnAnyCrateOpened -= HandleCrateOpened;

            if (squadManager != null)
            {
                squadManager.OnDefensiveAssistParry -= HandleAssistParry;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddMission(MissionDataSO mission)
        {
            if (mission == null) return;
            for (int i = 0; i < _missions.Count; i++)
            {
                if (_missions[i].Data.missionId == mission.missionId) return;
            }

            _missions.Add(new MissionRuntimeState
            {
                Data = mission,
                CurrentCount = 0,
                IsCompleted = false
            });
            OnMissionsUpdated?.Invoke();
        }

        public void NotifyNpcTalked(string npcId)
        {
            AdvanceObjective(MissionObjectiveType.TalkToNpc, npcId, 1);
        }

        private void HandleRiftCleared(AnomalyRiftZone rift)
        {
            AdvanceObjective(MissionObjectiveType.ClearAnomalyRift, rift != null ? rift.RiftName : "", 1);
        }

        private void HandleCrateOpened(AnomalyCrate crate)
        {
            AdvanceObjective(MissionObjectiveType.OpenAnomalyCrate, crate != null ? crate.CrateId : "", 1);
        }

        private void HandleAssistParry(AgentDataSO agent, HealthAndDazeComponent enemy)
        {
            AdvanceObjective(MissionObjectiveType.PerformAssistParries, "", 1);
        }

        private void AdvanceObjective(MissionObjectiveType type, string targetId, int amount)
        {
            bool changed = false;

            for (int i = 0; i < _missions.Count; i++)
            {
                MissionRuntimeState st = _missions[i];
                if (st.IsCompleted || st.Data == null || st.Data.objectiveType != type) continue;

                if (!string.IsNullOrEmpty(st.Data.targetIdFilter) &&
                    !string.Equals(st.Data.targetIdFilter, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                st.CurrentCount = Mathf.Min(st.Data.requiredCount, st.CurrentCount + amount);
                changed = true;

                if (st.CurrentCount >= st.Data.requiredCount)
                {
                    st.IsCompleted = true;
                    GrantMissionReward(st.Data);
                    OnMissionCompleted?.Invoke(st.Data);
                }
            }

            if (changed) OnMissionsUpdated?.Invoke();
        }

        private void GrantMissionReward(MissionDataSO data)
        {
            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.AddReward(
                    data.rewardCredits,
                    data.rewardAnomalyCores,
                    data.rewardDataDisks,
                    $"Contract Clear: {data.title}"
                );
            }
            Debug.Log($"[NeoVeil Quest] CONTRACT COMPLETED: {data.title}!");
        }

        public List<MissionSaveRecord> ExportSaveRecords()
        {
            var records = new List<MissionSaveRecord>(_missions.Count);
            for (int i = 0; i < _missions.Count; i++)
            {
                records.Add(new MissionSaveRecord
                {
                    missionId = _missions[i].Data.missionId,
                    currentCount = _missions[i].CurrentCount,
                    isCompleted = _missions[i].IsCompleted
                });
            }
            return records;
        }

        public void ImportSaveRecords(List<MissionSaveRecord> records)
        {
            if (records == null) return;
            for (int i = 0; i < records.Count; i++)
            {
                MissionSaveRecord rec = records[i];
                for (int j = 0; j < _missions.Count; j++)
                {
                    if (_missions[j].Data.missionId == rec.missionId)
                    {
                        _missions[j].CurrentCount = rec.currentCount;
                        _missions[j].IsCompleted = rec.isCompleted;
                        break;
                    }
                }
            }
            OnMissionsUpdated?.Invoke();
        }

        private void InitializeStartingMissions()
        {
            _missions.Clear();

            if (startingMissions != null && startingMissions.Length > 0)
            {
                for (int i = 0; i < startingMissions.Length; i++)
                {
                    AddMission(startingMissions[i]);
                }
                return;
            }

            // Built-in District 07 vertical slice contracts (1 Main + 2 Side)
            AddMission(MissionDataSO.CreateRuntimeContract(
                "main_stabilize_rift_01",
                "MAIN: Stabilkan Anomaly Rift",
                "Kalahkan semua wave di Anomaly Rift District 07.",
                mainStory: true,
                objType: MissionObjectiveType.ClearAnomalyRift,
                count: 1,
                credits: 600,
                cores: 4,
                disks: 2
            ));

            AddMission(MissionDataSO.CreateRuntimeContract(
                "side_parry_mastery",
                "SIDE: Defensive Assist Parry",
                "Tangkis 2 serangan Golden Flash musuh dengan Switch.",
                mainStory: false,
                objType: MissionObjectiveType.PerformAssistParries,
                count: 2,
                credits: 350,
                cores: 2,
                disks: 1
            ));

            AddMission(MissionDataSO.CreateRuntimeContract(
                "side_open_crate",
                "SIDE: Buru Anomaly Crate",
                "Buka 1 Anomaly Crate di distrik kota.",
                mainStory: false,
                objType: MissionObjectiveType.OpenAnomalyCrate,
                count: 1,
                credits: 300,
                cores: 2,
                disks: 0
            ));
        }
    }
}
