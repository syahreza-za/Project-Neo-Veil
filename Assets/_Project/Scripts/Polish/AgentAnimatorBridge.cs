using System.Collections.Generic;
using UnityEngine;
using NeoVeil.Combat;
using NeoVeil.Player;
using NeoVeil.Squad;

namespace NeoVeil.Polish
{
    /// <summary>
    /// Safe Animator bridge for VRoid + Mixamo humanoid character prefabs.
    /// Automatically rebinds whenever SquadManager switches the active 3D agent visual,
    /// speeds up combat clips (1.25x) for snappy ZZZ feel, and checks parameter existence
    /// before writing so missing Animator parameters never spam Console warnings.
    /// </summary>
    public class AgentAnimatorBridge : MonoBehaviour
    {
        [Header("Gameplay References (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private CombatController combatController;
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private ChainAttackController chainAttackController;

        [Header("Snappy ZZZ Animation Tuning")]
        [SerializeField, Min(1f)] private float referenceSprintSpeed = 9.0f;
        [SerializeField, Range(1.0f, 1.6f)] private float combatPlaybackSpeed = 1.25f;
        [SerializeField, Min(0.01f)] private float speedDampTime = 0.08f;

        [Header("Animator Parameter Names")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string groundedParam = "IsGrounded";
        [SerializeField] private string sprintingParam = "IsSprinting";
        [SerializeField] private string attackingParam = "IsAttacking";
        [SerializeField] private string comboStepParam = "ComboStep";
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string dodgeTrigger = "Dodge";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string heavyAttackTrigger = "HeavyAttack";
        [SerializeField] private string parryTrigger = "AssistParry";
        [SerializeField] private string chainTrigger = "ChainAttack";

        private Animator _activeAnimator;
        private readonly HashSet<int> _validParamHashes = new HashSet<int>();

        private int _speedHash;
        private int _groundedHash;
        private int _sprintingHash;
        private int _attackingHash;
        private int _comboStepHash;
        private int _jumpHash;
        private int _dodgeHash;
        private int _attackHash;
        private int _heavyAttackHash;
        private int _parryHash;
        private int _chainHash;

        private void Awake()
        {
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();
            if (combatController == null) combatController = GetComponent<CombatController>();
            if (squadManager == null) squadManager = GetComponent<SquadManager>();
            if (chainAttackController == null) chainAttackController = GetComponent<ChainAttackController>();

            CacheParameterHashes();
        }

        private void Start()
        {
            if (movementController != null)
            {
                movementController.OnJumpExecuted += HandleJumpExecuted;
                movementController.OnDodgeStarted += HandleDodgeStarted;
            }

            if (combatController != null)
            {
                combatController.OnAttackPerformed += HandleAttackPerformed;
            }

            if (squadManager != null)
            {
                squadManager.OnAgentSwitched += HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry += HandleAssistParry;
            }

            if (chainAttackController != null)
            {
                chainAttackController.OnChainAttackExecuted += HandleChainAttack;
            }

            RebindActiveAnimator();
        }

        private void OnDestroy()
        {
            if (movementController != null)
            {
                movementController.OnJumpExecuted -= HandleJumpExecuted;
                movementController.OnDodgeStarted -= HandleDodgeStarted;
            }

            if (combatController != null)
            {
                combatController.OnAttackPerformed -= HandleAttackPerformed;
            }

            if (squadManager != null)
            {
                squadManager.OnAgentSwitched -= HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry -= HandleAssistParry;
            }

            if (chainAttackController != null)
            {
                chainAttackController.OnChainAttackExecuted -= HandleChainAttack;
            }
        }

        private void HandleJumpExecuted() => SafeSetTrigger(_jumpHash);
        private void HandleDodgeStarted() => SafeSetTrigger(_dodgeHash);
        private void HandleAssistParry(AgentDataSO _, HealthAndDazeComponent __) => SafeSetTrigger(_parryHash);
        private void HandleChainAttack(AgentDataSO _, HealthAndDazeComponent __) => SafeSetTrigger(_chainHash);

        private void Update()
        {
            if (_activeAnimator == null || movementController == null) return;

            float normSpeed = Mathf.Clamp01(movementController.CurrentHorizontalSpeed / Mathf.Max(0.1f, referenceSprintSpeed));
            if (_validParamHashes.Contains(_speedHash))
            {
                _activeAnimator.SetFloat(_speedHash, normSpeed, speedDampTime, Time.deltaTime);
            }

            SafeSetBool(_groundedHash, movementController.IsGrounded);
            SafeSetBool(_sprintingHash, movementController.IsSprinting);

            bool attacking = combatController != null && combatController.IsAttacking;
            SafeSetBool(_attackingHash, attacking);
            _activeAnimator.speed = attacking ? combatPlaybackSpeed : 1.0f;
        }

        public void RebindActiveAnimator()
        {
            _activeAnimator = null;
            _validParamHashes.Clear();

            if (squadManager != null && squadManager.ActiveState != null && squadManager.ActiveState.VisualInstance != null)
            {
                _activeAnimator = squadManager.ActiveState.VisualInstance.GetComponentInChildren<Animator>();
            }

            if (_activeAnimator == null)
            {
                _activeAnimator = GetComponentInChildren<Animator>();
            }

            if (_activeAnimator != null && _activeAnimator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter param in _activeAnimator.parameters)
                {
                    _validParamHashes.Add(param.nameHash);
                }
            }
        }

        private void HandleAgentSwitched(int _, AgentDataSO __)
        {
            RebindActiveAnimator();
        }

        private void HandleAttackPerformed(AttackDataSO attack, int comboStep)
        {
            if (_activeAnimator == null) return;

            if (_validParamHashes.Contains(_comboStepHash))
            {
                _activeAnimator.SetInteger(_comboStepHash, comboStep);
            }

            bool heavy = attack != null && attack.isHeavyAttack;
            SafeSetTrigger(heavy ? _heavyAttackHash : _attackHash);
        }

        private void SafeSetBool(int hash, bool val)
        {
            if (_activeAnimator != null && _validParamHashes.Contains(hash))
            {
                _activeAnimator.SetBool(hash, val);
            }
        }

        private void SafeSetTrigger(int hash)
        {
            if (_activeAnimator != null && _validParamHashes.Contains(hash))
            {
                _activeAnimator.SetTrigger(hash);
            }
        }

        private void CacheParameterHashes()
        {
            _speedHash = Animator.StringToHash(speedParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            _sprintingHash = Animator.StringToHash(sprintingParam);
            _attackingHash = Animator.StringToHash(attackingParam);
            _comboStepHash = Animator.StringToHash(comboStepParam);
            _jumpHash = Animator.StringToHash(jumpTrigger);
            _dodgeHash = Animator.StringToHash(dodgeTrigger);
            _attackHash = Animator.StringToHash(attackTrigger);
            _heavyAttackHash = Animator.StringToHash(heavyAttackTrigger);
            _parryHash = Animator.StringToHash(parryTrigger);
            _chainHash = Animator.StringToHash(chainTrigger);
        }
    }
}
