using System;
using UnityEngine;
using NeoVeil.Core;
using NeoVeil.Player;
using NeoVeil.Squad;

namespace NeoVeil.Combat
{
    /// <summary>
    /// ZZZ-style Combat Controller:
    /// - 4-Hit Basic Attack Combo with Soft Lock-On turning, Attack Lunge, Hitstop, and combo window
    /// - Automatic Dodge Counter when attacking during the Perfect Dodge counter window
    /// - EX Special (E) consuming Agent Energy with brief i-frames
    /// - Ultimate (Q) consuming 100% Squad Decibel gauge with slow-mo & AoE burst
    /// </summary>
    public class CombatController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private SoftLockOnSystem softLockOn;
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private ChainAttackController chainAttackController;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField, Min(0.3f)] private float comboResetTime = 1.15f;
        [SerializeField, Min(0.5f)] private float comboCounterDecayTime = 2.8f;

        private readonly Collider[] _hitBuffer = new Collider[20];
        private readonly System.Collections.Generic.HashSet<HealthAndDazeComponent> _hitTargets =
            new System.Collections.Generic.HashSet<HealthAndDazeComponent>();
        private AttackDataSO[] _defaultCombo;
        private AttackDataSO _defaultDodgeCounter;
        private AttackDataSO _defaultExSpecial;
        private AttackDataSO _defaultUltimate;

        private int _comboStepIndex;
        private float _recoveryTimer;
        private float _comboResetTimer;
        private float _comboDecayTimer;

        public int ComboHitCount { get; private set; }
        public int CurrentComboStep => _comboStepIndex;
        public bool IsAttacking => _recoveryTimer > 0f;

        public event Action<AttackDataSO, int> OnAttackPerformed;
        public event Action<int> OnComboHitCountChanged;

        private void Awake()
        {
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();
            if (softLockOn == null) softLockOn = GetComponent<SoftLockOnSystem>();
            if (squadManager == null) squadManager = GetComponent<SquadManager>();
            if (chainAttackController == null) chainAttackController = GetComponent<ChainAttackController>();

            BuildRuntimeDefaultAttacks();
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.OnAttackPressed += HandleAttackPressed;
            inputReader.OnSpecialPressed += HandleSpecialPressed;
            inputReader.OnUltimatePressed += HandleUltimatePressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.OnAttackPressed -= HandleAttackPressed;
            inputReader.OnSpecialPressed -= HandleSpecialPressed;
            inputReader.OnUltimatePressed -= HandleUltimatePressed;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_recoveryTimer > 0f) _recoveryTimer -= dt;

            if (_comboResetTimer > 0f)
            {
                _comboResetTimer -= dt;
                if (_comboResetTimer <= 0f) _comboStepIndex = 0;
            }

            if (_comboDecayTimer > 0f)
            {
                _comboDecayTimer -= Time.unscaledDeltaTime;
                if (_comboDecayTimer <= 0f && ComboHitCount > 0)
                {
                    ComboHitCount = 0;
                    OnComboHitCountChanged?.Invoke(0);
                }
            }
        }

        private void HandleAttackPressed()
        {
            if (squadManager == null || squadManager.ActiveAgent == null) return;

            // Pressing Attack during Chain Attack prompt launches the next agent automatically
            if (chainAttackController != null && chainAttackController.IsChainPromptOpen)
            {
                int nextIdx = (squadManager.ActiveIndex + 1) % squadManager.AgentCount;
                chainAttackController.ExecuteChainAttack(nextIdx);
                return;
            }

            if (_recoveryTimer > 0.04f) return;

            AgentDataSO agent = squadManager.ActiveAgent;

            // Check if player just performed a Perfect Dodge -> trigger Dodge Counter!
            if (movementController != null && movementController.ConsumeDodgeCounter())
            {
                AttackDataSO counter = agent.dodgeCounterAttack != null ? agent.dodgeCounterAttack : _defaultDodgeCounter;
                ExecuteAttack(counter, agent, "COUNTER!");
                _comboStepIndex = 0;
                return;
            }

            AttackDataSO[] comboList = (agent.basicCombo != null && agent.basicCombo.Length > 0)
                ? agent.basicCombo
                : _defaultCombo;

            AttackDataSO move = comboList[_comboStepIndex % comboList.Length];
            _comboStepIndex = (_comboStepIndex + 1) % comboList.Length;
            _comboResetTimer = comboResetTime;

            string tag = move.isHeavyAttack ? "FINISHER" : string.Empty;
            ExecuteAttack(move, agent, tag);
        }

        private void HandleSpecialPressed()
        {
            if (squadManager == null || squadManager.ActiveAgent == null || _recoveryTimer > 0.05f) return;

            AgentDataSO agent = squadManager.ActiveAgent;
            if (!squadManager.TryConsumeEnergyFromActive(agent.exSpecialEnergyCost))
            {
                Debug.Log($"[NeoVeil] Energy belum cukup untuk EX Special (butuh {agent.exSpecialEnergyCost}).");
                return;
            }

            AttackDataSO exMove = agent.exSpecialAttack != null ? agent.exSpecialAttack : _defaultExSpecial;
            ExecuteAttack(exMove, agent, "EX SPECIAL!");
            _comboStepIndex = 0;
        }

        private void HandleUltimatePressed()
        {
            if (squadManager == null || squadManager.ActiveAgent == null) return;
            if (!squadManager.TryConsumeFullDecibel())
            {
                Debug.Log($"[NeoVeil] Decibel belum 100% (saat ini {squadManager.Decibel:F0}%).");
                return;
            }

            AgentDataSO agent = squadManager.ActiveAgent;
            AttackDataSO ultMove = agent.ultimateAttack != null ? agent.ultimateAttack : _defaultUltimate;

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.TriggerSlowMotion(0.3f, 0.38f);
            }

            ExecuteAttack(ultMove, agent, "ULTIMATE!");
            _comboStepIndex = 0;
        }

        private void ExecuteAttack(AttackDataSO attack, AgentDataSO agent, string tag)
        {
            _recoveryTimer = attack.recoveryDuration;

            // Soft Lock-On facing & Attack Lunge
            Vector3 attackForward = transform.forward;
            if (softLockOn != null && softLockOn.HasTarget)
            {
                attackForward = softLockOn.GetDirectionToTarget();
                transform.rotation = Quaternion.LookRotation(attackForward, Vector3.up);
            }

            if (movementController != null)
            {
                if (attack.iFrameDuration > 0f)
                {
                    movementController.GrantInvulnerability(attack.iFrameDuration);
                }

                if (attack.lungeSpeed > 0f && attack.lungeDuration > 0f)
                {
                    // Avoid lunging past an already very close enemy
                    float distToTarget = softLockOn != null ? softLockOn.GetDistanceToTarget() : 99f;
                    if (distToTarget > 1.75f)
                    {
                        movementController.ApplyAttackLunge(attackForward, attack.lungeSpeed, attack.lungeDuration);
                    }
                }
            }

            // Detect enemies in hitbox
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                attack.range,
                _hitBuffer,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            bool landedAnyHit = false;
            HealthAndDazeComponent newlyStunnedEnemy = null;
            float halfArcCos = Mathf.Cos(attack.hitArcDegrees * 0.5f * Mathf.Deg2Rad);

            _hitTargets.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;

                var enemyHealth = col.GetComponentInParent<HealthAndDazeComponent>();
                if (enemyHealth == null || enemyHealth.IsDead || !_hitTargets.Add(enemyHealth)) continue;

                if (!attack.isFullCircleAoE)
                {
                    Vector3 toEnemy = enemyHealth.transform.position - transform.position;
                    toEnemy.y = 0f;
                    if (toEnemy.sqrMagnitude > 0.25f)
                    {
                        float dot = Vector3.Dot(attackForward, toEnemy.normalized);
                        if (dot < halfArcCos) continue;
                    }
                }

                var payload = new DamagePayload
                {
                    RawDamage = agent.attackPower * attack.damageMultiplier * squadManager.TeamAttackMultiplier,
                    RawDaze = attack.baseDaze,
                    DazeMultiplier = agent.dazeMultiplier,
                    StunDamageMultiplier = agent.stunDamageMultiplier,
                    AttackerRole = agent.role,
                    IsHeavyAttack = attack.isHeavyAttack,
                    TriggersChainOnStun = attack.isHeavyAttack,
                    SourcePosition = transform.position,
                    Tag = tag
                };

                DamageResult result = enemyHealth.ApplyDamage(payload);
                landedAnyHit = true;

                ComboHitCount++;
                _comboDecayTimer = comboCounterDecayTime;
                OnComboHitCountChanged?.Invoke(ComboHitCount);

                if (result.TriggeredAnomalyBurst)
                {
                    squadManager.ActivateTeamAttackBuff();
                }

                if (result.TriggeredStun && newlyStunnedEnemy == null)
                {
                    newlyStunnedEnemy = enemyHealth;
                }
            }

            if (landedAnyHit)
            {
                squadManager.AddEnergyToActive(attack.energyGain);
                squadManager.AddDecibel(attack.decibelGain);

                if (TimeScaleManager.Instance != null && attack.hitstopDuration > 0f)
                {
                    TimeScaleManager.Instance.TriggerHitstop(attack.hitstopDuration);
                }

                if (newlyStunnedEnemy != null && chainAttackController != null)
                {
                    chainAttackController.OpenChainPrompt(newlyStunnedEnemy);
                }
            }

            OnAttackPerformed?.Invoke(attack, _comboStepIndex);
        }

        private void BuildRuntimeDefaultAttacks()
        {
            _defaultCombo = new[]
            {
                AttackDataSO.CreateRuntimeDefault("Basic 1", 1.0f, 10f, 3.3f, 0.16f, 0.022f, heavy: false, aoe: false, lungeSpd: 9f),
                AttackDataSO.CreateRuntimeDefault("Basic 2", 1.1f, 11f, 3.4f, 0.17f, 0.025f, heavy: false, aoe: false, lungeSpd: 10f),
                AttackDataSO.CreateRuntimeDefault("Basic 3", 1.25f, 14f, 3.6f, 0.19f, 0.03f, heavy: false, aoe: false, lungeSpd: 11f),
                AttackDataSO.CreateRuntimeDefault("Basic 4 Finisher", 1.75f, 24f, 4.1f, 0.28f, 0.052f, heavy: true, aoe: false, lungeSpd: 13f, energy: 10f, decibel: 7f)
            };

            _defaultDodgeCounter = AttackDataSO.CreateRuntimeDefault(
                "Dodge Counter", 2.1f, 26f, 4.6f, 0.24f, 0.05f, heavy: true, aoe: false, lungeSpd: 16f, energy: 14f, decibel: 10f, iFrames: 0.35f
            );

            _defaultExSpecial = AttackDataSO.CreateRuntimeDefault(
                "EX Special", 2.75f, 36f, 6.0f, 0.34f, 0.06f, heavy: true, aoe: true, lungeSpd: 6f, energy: 0f, decibel: 14f, iFrames: 0.45f
            );

            _defaultUltimate = AttackDataSO.CreateRuntimeDefault(
                "Ultimate", 4.6f, 55f, 10.0f, 0.55f, 0.075f, heavy: true, aoe: true, lungeSpd: 0f, energy: 20f, decibel: 0f, iFrames: 0.9f
            );
        }
    }
}
