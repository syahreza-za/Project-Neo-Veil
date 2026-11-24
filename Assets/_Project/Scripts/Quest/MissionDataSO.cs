using UnityEngine;

namespace NeoVeil.Quest
{
    public enum MissionObjectiveType
    {
        TalkToNpc,
        ClearAnomalyRift,
        OpenAnomalyCrate,
        PerformAssistParries
    }

    /// <summary>
    /// Data-driven District 07 Contract / Mission definition (Main Story or Side Contract).
    /// </summary>
    [CreateAssetMenu(fileName = "MissionData", menuName = "NeoVeil/Quest/Mission Data")]
    public class MissionDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string missionId = "contract_rift_01";
        public string title = "Stabilisasi Neon Alley";
        [TextArea(2, 3)] public string description = "Kalahkan semua gelombang anomali di Anomaly Rift terdekat.";
        public bool isMainStory = true;

        [Header("Objective")]
        public MissionObjectiveType objectiveType = MissionObjectiveType.ClearAnomalyRift;
        [Tooltip("Optional filter (e.g., specific NPC ID or Rift Name). Leave empty to accept any matching target.")]
        public string targetIdFilter = "";
        [Min(1)] public int requiredCount = 1;

        [Header("Completion Rewards")]
        [Min(0)] public int rewardCredits = 500;
        [Min(0)] public int rewardAnomalyCores = 3;
        [Min(0)] public int rewardDataDisks = 1;

        public static MissionDataSO CreateRuntimeContract(
            string id,
            string titleVal,
            string descVal,
            bool mainStory,
            MissionObjectiveType objType,
            int count,
            int credits,
            int cores,
            int disks,
            string filter = "")
        {
            var m = CreateInstance<MissionDataSO>();
            m.missionId = id;
            m.title = titleVal;
            m.description = descVal;
            m.isMainStory = mainStory;
            m.objectiveType = objType;
            m.requiredCount = Mathf.Max(1, count);
            m.rewardCredits = credits;
            m.rewardAnomalyCores = cores;
            m.rewardDataDisks = disks;
            m.targetIdFilter = filter;
            return m;
        }
    }
}
