using System;
using UnityEngine;

namespace NeoVeil.City
{
    /// <summary>
    /// Single-player offline urban currency and material inventory (Credits, Anomaly Cores, Data Disks).
    /// Feeds exploration rewards from Anomaly Rifts & Crates into Fase 5 Cafe Hub Upgrades & Save/Load JSON.
    /// </summary>
    public class CityEconomyInventory : MonoBehaviour
    {
        public static CityEconomyInventory Instance { get; private set; }

        [Header("Starting Resources")]
        [SerializeField, Min(0)] private int credits = 500;
        [SerializeField, Min(0)] private int anomalyCores = 0;
        [SerializeField, Min(0)] private int dataDisks = 0;

        public int Credits => credits;
        public int AnomalyCores => anomalyCores;
        public int DataDisks => dataDisks;

        public event Action OnInventoryChanged;
        public event Action<string> OnRewardNotification;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddReward(int addCredits, int addCores = 0, int addDisks = 0, string sourceLabel = "")
        {
            credits += Mathf.Max(0, addCredits);
            anomalyCores += Mathf.Max(0, addCores);
            dataDisks += Mathf.Max(0, addDisks);

            string summary = $"+{addCredits} Credits";
            if (addCores > 0) summary += $", +{addCores} Core";
            if (addDisks > 0) summary += $", +{addDisks} Disk";
            if (!string.IsNullOrEmpty(sourceLabel)) summary = $"{sourceLabel}: {summary}";

            Debug.Log($"[NeoVeil Loot] {summary}");
            OnRewardNotification?.Invoke(summary);
            OnInventoryChanged?.Invoke();
        }

        public bool TrySpendCredits(int amount)
        {
            if (amount <= 0 || credits < amount) return false;
            credits -= amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void LoadFromSave(int savedCredits, int savedCores, int savedDisks)
        {
            credits = Mathf.Max(0, savedCredits);
            anomalyCores = Mathf.Max(0, savedCores);
            dataDisks = Mathf.Max(0, savedDisks);
            OnInventoryChanged?.Invoke();
        }
    }
}
