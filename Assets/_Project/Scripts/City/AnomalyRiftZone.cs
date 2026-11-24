using System;
using System.Collections.Generic;
using UnityEngine;
using NeoVeil.AI;
using NeoVeil.Combat;

namespace NeoVeil.City
{
    [Serializable]
    public class RiftWaveConfig
    {
        public string waveLabel = "Wave 1";
        [Tooltip("Optional Enemy prefab with EnemyAIController. Leave empty to auto-spawn greybox test enemies!")]
        public GameObject enemyPrefab;
        [Min(1)] public int enemyCount = 2;
        [Min(2f)] public float spawnRadius = 5.5f;
    }

    /// <summary>
    /// Semi-Open World Urban Anomaly Rift (NTE-style street distortion + ZZZ multi-wave encounter).
    /// Press [F] (or walk in if autoTriggerOnEnter is enabled) to launch multi-wave combat,
    /// unlock linked AnomalyCrates, and claim District rewards when stabilized!
    /// </summary>
    public class AnomalyRiftZone : MonoBehaviour, IInteractable
    {
        public static AnomalyRiftZone ActiveEncounter { get; private set; }

        [Header("Identity & Trigger")]
        [SerializeField] private string riftName = "Neon Alley Rift";
        [SerializeField] private bool allowReplayAfterClear = true;
        [SerializeField] private string enemyLayerName = "Enemy";

        [Header("Waves (Auto-Uses 2 Default Waves if Empty)")]
        [SerializeField] private RiftWaveConfig[] waves;

        [Header("Completion Rewards & Linked Crate")]
        [SerializeField] private AnomalyCrate linkedRewardCrate;
        [SerializeField, Min(0)] private int clearCredits = 450;
        [SerializeField, Min(0)] private int clearAnomalyCores = 3;
        [SerializeField, Min(0)] private int clearDataDisks = 1;

        [Header("Optional Visuals")]
        [SerializeField] private Renderer portalRenderer;
        [SerializeField] private Color unstableColor = new Color(0.68f, 0.22f, 0.98f);
        [SerializeField] private Color activeBattleColor = new Color(1.0f, 0.2f, 0.4f);
        [SerializeField] private Color stabilizedColor = new Color(0.15f, 0.88f, 0.95f);

        private readonly List<HealthAndDazeComponent> _aliveEnemies = new List<HealthAndDazeComponent>();
        private Material _portalMat;
        private int _currentWaveIndex = -1;

        public string RiftName => riftName;
        public bool IsEncounterActive { get; private set; }
        public bool IsCleared { get; private set; }
        public int CurrentWaveNumber => _currentWaveIndex + 1;
        public int TotalWaves => waves != null && waves.Length > 0 ? waves.Length : 2;
        public int AliveEnemyCount => _aliveEnemies.Count;

        public string InteractionPrompt => IsCleared
            ? $"[F] Tantang Ulang {riftName}"
            : $"[F] Stabilkan {riftName}";

        public bool CanInteract => !IsEncounterActive && (!IsCleared || allowReplayAfterClear);
        public Transform InteractionTransform => transform;

        public static event Action<AnomalyRiftZone> OnAnyRiftCleared;
        public event Action<AnomalyRiftZone, int> OnWaveStarted;
        public event Action<AnomalyRiftZone> OnRiftCleared;

        private void Awake()
        {
            if (waves == null || waves.Length == 0)
            {
                waves = new[]
                {
                    new RiftWaveConfig { waveLabel = "Wave 1 — Rift Stalkers", enemyCount = 2, spawnRadius = 5f },
                    new RiftWaveConfig { waveLabel = "Wave 2 — Abyss Enforcer", enemyCount = 3, spawnRadius = 6f }
                };
            }

            if (linkedRewardCrate != null && !IsCleared)
            {
                linkedRewardCrate.SetLocked(true);
            }

            if (portalRenderer == null) portalRenderer = GetComponentInChildren<Renderer>();
            if (portalRenderer != null) _portalMat = portalRenderer.material;
            RefreshVisual();
        }

        private void OnDestroy()
        {
            if (ActiveEncounter == this) ActiveEncounter = null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            StartEncounter();
        }

        public void StartEncounter()
        {
            if (IsEncounterActive) return;

            IsEncounterActive = true;
            ActiveEncounter = this;
            _currentWaveIndex = 0;
            RefreshVisual();
            SpawnCurrentWave();
        }

        private void SpawnCurrentWave()
        {
            _aliveEnemies.Clear();
            RiftWaveConfig cfg = waves[_currentWaveIndex];
            int enemyLayer = LayerMask.NameToLayer(enemyLayerName);

            for (int i = 0; i < cfg.enemyCount; i++)
            {
                float angle = (i / (float)Mathf.Max(1, cfg.enemyCount)) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * cfg.spawnRadius;
                Vector3 spawnPos = transform.position + offset + Vector3.up * 1.0f;

                GameObject enemyObj = cfg.enemyPrefab != null
                    ? Instantiate(cfg.enemyPrefab, spawnPos, Quaternion.identity)
                    : CreateFallbackGreyboxEnemy(spawnPos, $"{riftName}_W{CurrentWaveNumber}_E{i + 1}", enemyLayer);

                if (enemyLayer >= 0)
                {
                    enemyObj.layer = enemyLayer;
                }

                var health = enemyObj.GetComponentInChildren<HealthAndDazeComponent>();
                if (health != null)
                {
                    _aliveEnemies.Add(health);
                    health.OnDied += HandleEnemyDied;
                }
            }

            Debug.Log($"[NeoVeil Rift] {riftName} — {cfg.waveLabel} ({_aliveEnemies.Count} musuh muncul)!");
            OnWaveStarted?.Invoke(this, CurrentWaveNumber);
        }

        private void HandleEnemyDied(HealthAndDazeComponent deadEnemy)
        {
            deadEnemy.OnDied -= HandleEnemyDied;
            _aliveEnemies.Remove(deadEnemy);

            if (_aliveEnemies.Count > 0) return;

            // Advance to next wave or complete Rift
            _currentWaveIndex++;
            if (_currentWaveIndex < waves.Length)
            {
                SpawnCurrentWave();
            }
            else
            {
                CompleteEncounter();
            }
        }

        private void CompleteEncounter()
        {
            IsEncounterActive = false;
            IsCleared = true;
            if (ActiveEncounter == this) ActiveEncounter = null;

            if (linkedRewardCrate != null)
            {
                linkedRewardCrate.SetLocked(false);
            }

            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.AddReward(
                    clearCredits,
                    clearAnomalyCores,
                    clearDataDisks,
                    $"{riftName} Stabilized"
                );
            }

            RefreshVisual();
            Debug.Log($"[NeoVeil Rift] {riftName} STABILIZED!");
            OnRiftCleared?.Invoke(this);
            OnAnyRiftCleared?.Invoke(this);
        }

        private static GameObject CreateFallbackGreyboxEnemy(Vector3 pos, string enemyName, int enemyLayer)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = enemyName;
            go.transform.position = pos;
            if (enemyLayer >= 0) go.layer = enemyLayer;

            go.AddComponent<HealthAndDazeComponent>();
            go.AddComponent<TelegraphSystem>();
            go.AddComponent<EnemyAIController>();
            return go;
        }

        private void RefreshVisual()
        {
            if (_portalMat == null) return;
            _portalMat.color = IsEncounterActive
                ? activeBattleColor
                : (IsCleared ? stabilizedColor : unstableColor);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.75f, 0.25f, 1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, 5.5f);
        }
    }
}
