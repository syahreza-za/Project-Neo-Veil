using System;
using UnityEngine;
using NeoVeil.Core;
using NeoVeil.Combat;
using NeoVeil.Player;

namespace NeoVeil.Squad
{
    /// <summary>
    /// Handles the signature ZZZ Chain Attack slow-motion window when an enemy's Daze reaches 100%.
    /// Pressing Switch (Tab/C or 1/2/3) or Attack during the prompt swaps in the chosen agent
    /// and delivers a high-impact AoE Chain Attack!
    /// </summary>
    public class ChainAttackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Chain Attack Settings")]
        [SerializeField, Min(0.5f)] private float promptDuration = 2.2f;
        [SerializeField, Range(0.05f, 0.4f)] private float promptSlowMoScale = 0.12f;
        [SerializeField, Min(1f)] private float chainAoERadius = 7.0f;
        [SerializeField, Min(0.5f)] private float snapStandDistance = 2.2f;

        private readonly Collider[] _hitBuffer = new Collider[16];
        private readonly System.Collections.Generic.HashSet<HealthAndDazeComponent> _hitTargets =
            new System.Collections.Generic.HashSet<HealthAndDazeComponent>();
        private AttackDataSO _fallbackChainAttack;

        public bool IsChainPromptOpen { get; private set; }
        public float PromptTimeRemaining { get; private set; }
        public float PromptDuration => promptDuration;
        public HealthAndDazeComponent StunnedTarget { get; private set; }

        public event Action<HealthAndDazeComponent> OnChainPromptOpened;
        public event Action OnChainPromptClosed;
        public event Action<AgentDataSO, HealthAndDazeComponent> OnChainAttackExecuted;

        private void Awake()
        {
            if (squadManager == null) squadManager = GetComponent<SquadManager>();
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();

            _fallbackChainAttack = AttackDataSO.CreateRuntimeDefault(
                "Chain Attack",
                dmgMult: 3.1f,
                daze: 0f,
                rangeVal: chainAoERadius,
                recovery: 0.35f,
                hitstop: 0.065f,
                heavy: true,
                aoe: true,
                lungeSpd: 0f,
                energy: 20f,
                decibel: 15f,
                iFrames: 0.75f
            );
        }

        private void Update()
        {
            if (!IsChainPromptOpen) return;

            PromptTimeRemaining -= Time.unscaledDeltaTime;
            if (PromptTimeRemaining <= 0f || StunnedTarget == null || StunnedTarget.IsDead)
            {
                ClosePrompt();
            }
        }

        public void OpenChainPrompt(HealthAndDazeComponent stunnedEnemy)
        {
            if (stunnedEnemy == null || stunnedEnemy.IsDead || squadManager == null || squadManager.AgentCount <= 1)
            {
                return;
            }

            IsChainPromptOpen = true;
            PromptTimeRemaining = promptDuration;
            StunnedTarget = stunnedEnemy;

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.TriggerSlowMotion(promptSlowMoScale, promptDuration);
            }

            Debug.Log("[NeoVeil] CHAIN ATTACK READY! Tekan Tab/C atau 1/2/3 untuk meluncurkan Chain Attack!");
            OnChainPromptOpened?.Invoke(stunnedEnemy);
        }

        public void ExecuteChainAttack(int targetAgentIndex)
        {
            if (!IsChainPromptOpen || squadManager == null) return;

            HealthAndDazeComponent target = StunnedTarget;
            ClosePrompt();

            // Perform swap without re-entering ChainAttackController or Assist Parry
            squadManager.SwitchToIndex(targetAgentIndex, allowDefensiveAssist: false);

            AgentDataSO agent = squadManager.ActiveAgent;
            if (agent == null) return;

            if (target != null && movementController != null)
            {
                Vector3 toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                Vector3 dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
                Vector3 snapPos = target.transform.position - dir * snapStandDistance;
                snapPos.y = transform.position.y;
                movementController.SnapToPositionAndFace(snapPos, dir);
            }

            AttackDataSO chainData = agent.chainAttack != null ? agent.chainAttack : _fallbackChainAttack;

            if (movementController != null)
            {
                movementController.GrantInvulnerability(chainData.iFrameDuration);
            }

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.TriggerHitstop(chainData.hitstopDuration);
            }

            squadManager.AddEnergyToActive(chainData.energyGain);
            squadManager.AddDecibel(chainData.decibelGain);

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                chainData.range,
                _hitBuffer,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            _hitTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                var enemyHealth = _hitBuffer[i].GetComponentInParent<HealthAndDazeComponent>();
                if (enemyHealth == null || enemyHealth.IsDead || !_hitTargets.Add(enemyHealth)) continue;

                var payload = new DamagePayload
                {
                    RawDamage = squadManager.ActiveAttackPower * chainData.damageMultiplier * squadManager.TeamAttackMultiplier,
                    RawDaze = 0f,
                    DazeMultiplier = agent.dazeMultiplier,
                    StunDamageMultiplier = agent.stunDamageMultiplier,
                    AttackerRole = agent.role,
                    IsHeavyAttack = true,
                    TriggersChainOnStun = false,
                    SourcePosition = transform.position,
                    Tag = "CHAIN!"
                };

                DamageResult res = enemyHealth.ApplyDamage(payload);
                if (res.TriggeredAnomalyBurst)
                {
                    squadManager.ActivateTeamAttackBuff();
                }
            }

            Debug.Log($"[NeoVeil] CHAIN ATTACK executed by {agent.agentName}!");
            OnChainAttackExecuted?.Invoke(agent, target);
        }

        public void ClosePrompt()
        {
            if (!IsChainPromptOpen) return;

            IsChainPromptOpen = false;
            PromptTimeRemaining = 0f;
            StunnedTarget = null;

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.ClearSlowMotion();
            }

            OnChainPromptClosed?.Invoke();
        }
    }
}
