using System;
using UnityEngine;
using NeoVeil.Core;
using NeoVeil.Combat;
using NeoVeil.Player;
using NeoVeil.AI;

namespace NeoVeil.Squad
{
    [Serializable]
    public class AgentRuntimeState
    {
        public AgentDataSO Data;
        public int Level = 1;
        public float CurrentHP;
        public float CurrentEnergy;
        public GameObject VisualInstance;

        public float EffectiveMaxHP => Data != null ? Data.maxHP + (Level - 1) * 110f : 1000f;
        public float EffectiveAttackPower => Data != null ? Data.attackPower + (Level - 1) * 12f : 70f;
        public float NormalizedHP => EffectiveMaxHP > 0f ? CurrentHP / EffectiveMaxHP : 0f;
        public float NormalizedEnergy => Data != null && Data.maxEnergy > 0f ? CurrentEnergy / Data.maxEnergy : 0f;
        public bool IsAlive => CurrentHP > 0f;
    }

    /// <summary>
    /// Manages the 3-Agent ZZZ Squad (Striker, Breaker, Channeler):
    /// - Quick Switch (Tab/C or 1/2/3) preserving exact player position and rotation
    /// - Defensive Assist Parry when switching during an enemy's GoldenFlash telegraph
    /// - Routes switch presses into ChainAttackController when a Chain Attack prompt is active
    /// - Shared Squad Decibel (Ultimate) gauge, Agent Leveling/Upgrades, and Channeler Team ATK Buff
    /// </summary>
    public class SquadManager : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private ChainAttackController chainAttackController;

        [Header("Squad Roster (Up to 3 Agents — Auto-Populates Defaults if Empty)")]
        [SerializeField] private AgentDataSO[] squadAgents;

        [Header("Visual Swap (Optional)")]
        [SerializeField] private Transform visualAnchor;
        [Tooltip("Optional greybox Renderer that tints to the active agent's accentColor when no visualPrefab is set.")]
        [SerializeField] private Renderer greyboxBodyRenderer;

        [Header("Defensive Assist Parry Settings")]
        [SerializeField, Min(1f)] private float assistParryMaxRange = 8.5f;
        [SerializeField, Min(0.5f)] private float assistParryStandDistance = 2.0f;
        [SerializeField, Min(0.1f)] private float assistParryIFrames = 0.65f;
        [SerializeField, Min(0.01f)] private float assistParryHitstop = 0.065f;

        [Header("Team Buff (Channeler Anomaly Bonus)")]
        [SerializeField, Min(1f)] private float teamBuffMultiplier = 1.25f;

        private AgentRuntimeState[] _states;
        private Material _greyboxMat;
        private float _decibel;
        private float _teamBuffTimer;
        private AttackDataSO _fallbackParryAttack;

        public int ActiveIndex { get; private set; }
        public int AgentCount => _states != null ? _states.Length : 0;
        public AgentRuntimeState ActiveState => _states != null && _states.Length > 0 ? _states[ActiveIndex] : null;
        public AgentDataSO ActiveAgent => ActiveState?.Data;
        public float ActiveAttackPower => ActiveState != null ? ActiveState.EffectiveAttackPower : 70f;
        public float Decibel => _decibel;
        public float NormalizedDecibel => _decibel / 100f;
        public bool HasTeamAttackBuff => _teamBuffTimer > 0f;
        public float TeamAttackMultiplier => HasTeamAttackBuff ? teamBuffMultiplier : 1f;
        public int ParryCount { get; private set; }

        public event Action<int, AgentDataSO> OnAgentSwitched;
        public event Action<AgentDataSO, HealthAndDazeComponent> OnDefensiveAssistParry;
        public event Action<float> OnDecibelChanged;
        public event Action OnSquadStatsChanged;
        public event Action<AgentDataSO, float> OnActiveAgentDamaged;

        private void Awake()
        {
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();
            if (chainAttackController == null) chainAttackController = GetComponent<ChainAttackController>();
            if (visualAnchor == null) visualAnchor = transform;

            if (greyboxBodyRenderer == null)
            {
                greyboxBodyRenderer = GetComponentInChildren<Renderer>();
            }
            if (greyboxBodyRenderer != null)
            {
                _greyboxMat = greyboxBodyRenderer.material;
            }

            _fallbackParryAttack = AttackDataSO.CreateRuntimeDefault(
                "Defensive Assist Parry",
                dmgMult: 2.35f,
                daze: 42f,
                rangeVal: 4f,
                recovery: 0.25f,
                hitstop: assistParryHitstop,
                heavy: true,
                aoe: false,
                lungeSpd: 0f,
                energy: 18f,
                decibel: 16f,
                iFrames: assistParryIFrames
            );

            InitializeSquad();
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.OnSwitchNextPressed += HandleSwitchNextPressed;
            inputReader.OnSwitchSlotPressed += HandleSwitchSlotPressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.OnSwitchNextPressed -= HandleSwitchNextPressed;
            inputReader.OnSwitchSlotPressed -= HandleSwitchSlotPressed;
        }

        private void Update()
        {
            if (_teamBuffTimer > 0f)
            {
                _teamBuffTimer -= Time.deltaTime;
            }
        }

        public AgentRuntimeState GetAgentState(int index)
        {
            if (_states == null || index < 0 || index >= _states.Length) return null;
            return _states[index];
        }

        public void AddEnergyToActive(float amount)
        {
            if (ActiveState == null || amount <= 0f) return;
            ActiveState.CurrentEnergy = Mathf.Min(ActiveState.Data.maxEnergy, ActiveState.CurrentEnergy + amount);
            OnSquadStatsChanged?.Invoke();
        }

        public bool TryConsumeEnergyFromActive(float cost)
        {
            if (ActiveState == null || ActiveState.CurrentEnergy < cost) return false;
            ActiveState.CurrentEnergy -= cost;
            OnSquadStatsChanged?.Invoke();
            return true;
        }

        public void AddDecibel(float amount)
        {
            if (amount <= 0f) return;
            _decibel = Mathf.Clamp(_decibel + amount, 0f, 100f);
            OnDecibelChanged?.Invoke(NormalizedDecibel);
            OnSquadStatsChanged?.Invoke();
        }

        public bool TryConsumeFullDecibel()
        {
            if (_decibel < 100f) return false;
            _decibel = 0f;
            OnDecibelChanged?.Invoke(0f);
            OnSquadStatsChanged?.Invoke();
            return true;
        }

        public void ActivateTeamAttackBuff(float duration = 7f)
        {
            _teamBuffTimer = Mathf.Max(_teamBuffTimer, duration);
            OnSquadStatsChanged?.Invoke();
        }

        /// <summary>
        /// Fully restores HP and minimum starting Energy for all 3 Squad Agents (used at Cafe 07 Hub).
        /// </summary>
        public void HealAllAgentsToFull()
        {
            if (_states == null) return;
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].CurrentHP = _states[i].EffectiveMaxHP;
                _states[i].CurrentEnergy = Mathf.Max(_states[i].CurrentEnergy, _states[i].Data.startingEnergy);
            }
            OnSquadStatsChanged?.Invoke();
        }

        /// <summary>
        /// Levels up the chosen agent (+110 MaxHP, +12 ATK per level) and heals them to full.
        /// </summary>
        public bool UpgradeAgentLevel(int index)
        {
            if (_states == null || index < 0 || index >= _states.Length) return false;
            _states[index].Level++;
            _states[index].CurrentHP = _states[index].EffectiveMaxHP;
            OnSquadStatsChanged?.Invoke();
            return true;
        }

        public void RestoreSquadFromSave(int activeIdx, float savedDecibel, int[] levels, float[] hps, float[] energies)
        {
            if (_states == null) return;
            _decibel = Mathf.Clamp(savedDecibel, 0f, 100f);

            for (int i = 0; i < _states.Length; i++)
            {
                if (levels != null && i < levels.Length) _states[i].Level = Mathf.Max(1, levels[i]);
                if (hps != null && i < hps.Length) _states[i].CurrentHP = Mathf.Clamp(hps[i], 1f, _states[i].EffectiveMaxHP);
                if (energies != null && i < energies.Length) _states[i].CurrentEnergy = Mathf.Clamp(energies[i], 0f, _states[i].Data.maxEnergy);
            }

            if (activeIdx >= 0 && activeIdx < _states.Length && activeIdx != ActiveIndex)
            {
                ApplyActiveIndex(activeIdx);
            }
            else
            {
                OnDecibelChanged?.Invoke(NormalizedDecibel);
                OnSquadStatsChanged?.Invoke();
            }
        }

        public void DamageActiveAgent(float rawDamage)
        {
            if (ActiveState == null || rawDamage <= 0f) return;

            if (movementController != null && movementController.IsInvulnerable)
            {
                movementController.TryTriggerPerfectDodge();
                return;
            }

            ActiveState.CurrentHP = Mathf.Max(0f, ActiveState.CurrentHP - rawDamage);
            OnActiveAgentDamaged?.Invoke(ActiveAgent, rawDamage);
            OnSquadStatsChanged?.Invoke();

            if (!ActiveState.IsAlive)
            {
                int nextAlive = FindNextAliveIndex(ActiveIndex);
                if (nextAlive != ActiveIndex)
                {
                    SwitchToIndex(nextAlive, allowDefensiveAssist: false);
                }
            }
        }

        public void SwitchToIndex(int targetIndex, bool allowDefensiveAssist = true)
        {
            if (_states == null || _states.Length <= 1) return;
            if (targetIndex < 0 || targetIndex >= _states.Length) return;
            if (targetIndex == ActiveIndex || !_states[targetIndex].IsAlive) return;

            // 1. If a Chain Attack prompt is active, route straight to Chain Attack!
            if (chainAttackController != null && chainAttackController.IsChainPromptOpen)
            {
                chainAttackController.ExecuteChainAttack(targetIndex);
                return;
            }

            // 2. Check for ZZZ Golden Flash -> Defensive Assist Parry!
            if (allowDefensiveAssist)
            {
                TelegraphSystem parryTarget = TelegraphSystem.FindClosestParryable(transform.position, assistParryMaxRange);
                if (parryTarget != null && parryTarget.TryConsumeParry())
                {
                    ExecuteDefensiveAssistParry(targetIndex, parryTarget);
                    return;
                }
            }

            // 3. Standard Quick Switch
            ApplyActiveIndex(targetIndex);
        }

        private void HandleSwitchNextPressed()
        {
            int nextIdx = FindNextAliveIndex(ActiveIndex);
            SwitchToIndex(nextIdx, allowDefensiveAssist: true);
        }

        private void HandleSwitchSlotPressed(int slotIndex)
        {
            SwitchToIndex(slotIndex, allowDefensiveAssist: true);
        }

        private void ExecuteDefensiveAssistParry(int nextIndex, TelegraphSystem parriedTelegraph)
        {
            ApplyActiveIndex(nextIndex);
            ParryCount++;

            Vector3 enemyPos = parriedTelegraph.transform.position;
            Vector3 toEnemy = enemyPos - transform.position;
            toEnemy.y = 0f;
            Vector3 dir = toEnemy.sqrMagnitude > 0.001f ? toEnemy.normalized : transform.forward;

            if (movementController != null)
            {
                Vector3 snapPos = enemyPos - dir * assistParryStandDistance;
                snapPos.y = transform.position.y;
                movementController.SnapToPositionAndFace(snapPos, dir);
                movementController.GrantInvulnerability(assistParryIFrames);
            }

            AttackDataSO parryData = ActiveAgent.assistParryAttack != null
                ? ActiveAgent.assistParryAttack
                : _fallbackParryAttack;

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.TriggerHitstop(parryData.hitstopDuration);
                TimeScaleManager.Instance.TriggerSlowMotion(0.35f, 0.22f);
            }

            AddEnergyToActive(parryData.energyGain);
            AddDecibel(parryData.decibelGain);

            var enemyHealth = parriedTelegraph.GetComponentInParent<HealthAndDazeComponent>();
            if (enemyHealth != null)
            {
                var payload = new DamagePayload
                {
                    RawDamage = ActiveAttackPower * parryData.damageMultiplier * TeamAttackMultiplier,
                    RawDaze = parryData.baseDaze,
                    DazeMultiplier = ActiveAgent.dazeMultiplier,
                    StunDamageMultiplier = ActiveAgent.stunDamageMultiplier,
                    AttackerRole = ActiveAgent.role,
                    IsHeavyAttack = true,
                    TriggersChainOnStun = true,
                    SourcePosition = transform.position,
                    Tag = "PARRY!"
                };

                DamageResult result = enemyHealth.ApplyDamage(payload);
                if (result.TriggeredAnomalyBurst)
                {
                    ActivateTeamAttackBuff();
                }
                if (result.TriggeredStun && chainAttackController != null)
                {
                    chainAttackController.OpenChainPrompt(enemyHealth);
                }
            }

            Debug.Log($"[NeoVeil] DEFENSIVE ASSIST PARRY! {ActiveAgent.agentName} ({ActiveAgent.role}) menangkis serangan!");
            OnDefensiveAssistParry?.Invoke(ActiveAgent, enemyHealth);
        }

        private void ApplyActiveIndex(int newIndex)
        {
            if (_states[ActiveIndex].VisualInstance != null)
            {
                _states[ActiveIndex].VisualInstance.SetActive(false);
            }

            ActiveIndex = newIndex;

            if (_states[ActiveIndex].VisualInstance != null)
            {
                _states[ActiveIndex].VisualInstance.SetActive(true);
            }
            else if (_greyboxMat != null)
            {
                _greyboxMat.color = ActiveAgent.accentColor;
            }

            OnAgentSwitched?.Invoke(ActiveIndex, ActiveAgent);
            OnSquadStatsChanged?.Invoke();
        }

        private void InitializeSquad()
        {
            // Auto-create the 3 default ZZZ-role agents if none were assigned in Inspector
            if (squadAgents == null || squadAgents.Length == 0)
            {
                squadAgents = new[]
                {
                    AgentDataSO.CreateRuntimePreset("Kira", AgentRole.Striker, new Color(0.13f, 0.83f, 0.93f), 1000f, 78f, 0.9f, 1.75f),
                    AgentDataSO.CreateRuntimePreset("Rex", AgentRole.Breaker, new Color(0.96f, 0.62f, 0.04f), 1250f, 64f, 2.15f, 1.4f),
                    AgentDataSO.CreateRuntimePreset("Vera", AgentRole.Channeler, new Color(0.91f, 0.47f, 0.98f), 920f, 70f, 1.25f, 1.5f)
                };
            }

            _states = new AgentRuntimeState[squadAgents.Length];
            for (int i = 0; i < squadAgents.Length; i++)
            {
                AgentDataSO data = squadAgents[i];
                GameObject instance = null;
                if (data.visualPrefab != null)
                {
                    instance = Instantiate(data.visualPrefab, visualAnchor);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.SetActive(i == 0);
                }

                _states[i] = new AgentRuntimeState
                {
                    Data = data,
                    CurrentHP = data.maxHP,
                    CurrentEnergy = Mathf.Clamp(data.startingEnergy, 0f, data.maxEnergy),
                    VisualInstance = instance
                };
            }

            ActiveIndex = 0;
            if (_states[0].VisualInstance == null && _greyboxMat != null)
            {
                _greyboxMat.color = _states[0].Data.accentColor;
            }

            OnAgentSwitched?.Invoke(ActiveIndex, ActiveAgent);
            OnSquadStatsChanged?.Invoke();
        }

        private int FindNextAliveIndex(int fromIndex)
        {
            for (int offset = 1; offset <= _states.Length; offset++)
            {
                int candidate = (fromIndex + offset) % _states.Length;
                if (_states[candidate].IsAlive) return candidate;
            }
            return fromIndex;
        }
    }
}
