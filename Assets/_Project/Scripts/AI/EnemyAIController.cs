using UnityEngine;
using UnityEngine.AI;
using NeoVeil.Combat;
using NeoVeil.Player;
using NeoVeil.Squad;

namespace NeoVeil.AI
{
    /// <summary>
    /// ZZZ-style Enemy AI Finite State Machine (Chase -> Golden/Red Telegraph -> Strike -> Recover / Stunned / Dead).
    /// Supports Unity NavMeshAgent when baked, and automatically falls back to direct transform steering
    /// so greybox testing works immediately before NavMesh baking.
    /// </summary>
    [RequireComponent(typeof(HealthAndDazeComponent))]
    [RequireComponent(typeof(TelegraphSystem))]
    public class EnemyAIController : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle,
            Chase,
            Telegraph,
            Recover,
            Stunned,
            Dead
        }

        [Header("Target References (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController playerMovement;
        [SerializeField] private SquadManager squadManager;

        [Header("Movement & Aggro")]
        [SerializeField, Min(1f)] private float aggroRadius = 18f;
        [SerializeField, Min(1f)] private float moveSpeed = 4.2f;
        [SerializeField, Min(1f)] private float preferredAttackRange = 3.2f;
        [SerializeField, Min(1f)] private float strikeHitRadius = 3.8f;

        [Header("ZZZ Attack & Telegraph Timing")]
        [SerializeField, Min(1f)] private float attackDamage = 100f;
        [SerializeField, Min(0.2f)] private float attackCooldownMin = 1.1f;
        [SerializeField, Min(0.3f)] private float attackCooldownMax = 2.0f;
        [SerializeField, Min(0.25f)] private float telegraphDuration = 0.65f;
        [SerializeField, Range(0f, 1f)] private float goldenFlashChance = 0.62f;
        [SerializeField, Min(0.2f)] private float recoverDuration = 1.15f;
        [SerializeField, Min(0.5f)] private float parriedStaggerDuration = 1.75f;
        [SerializeField, Min(0f)] private float attackLungeDistance = 1.4f;

        private HealthAndDazeComponent _healthAndDaze;
        private TelegraphSystem _telegraph;
        private NavMeshAgent _navAgent;

        private float _stateTimer;
        private TelegraphType _pendingTelegraph;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

        private void Awake()
        {
            _healthAndDaze = GetComponent<HealthAndDazeComponent>();
            _telegraph = GetComponent<TelegraphSystem>();
            _navAgent = GetComponent<NavMeshAgent>();

            if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovementController>();
            if (squadManager == null) squadManager = FindFirstObjectByType<SquadManager>();

            if (_navAgent != null)
            {
                _navAgent.speed = moveSpeed;
                _navAgent.stoppingDistance = Mathf.Max(1.5f, preferredAttackRange - 0.5f);
            }
        }

        private void OnEnable()
        {
            _healthAndDaze.OnStunStarted += HandleStunStarted;
            _healthAndDaze.OnStunEnded += HandleStunEnded;
            _healthAndDaze.OnDied += HandleDied;
            _telegraph.OnTelegraphParried += HandleParried;
            _telegraph.OnTelegraphEnded += HandleTelegraphFinished;
        }

        private void OnDisable()
        {
            _healthAndDaze.OnStunStarted -= HandleStunStarted;
            _healthAndDaze.OnStunEnded -= HandleStunEnded;
            _healthAndDaze.OnDied -= HandleDied;
            _telegraph.OnTelegraphParried -= HandleParried;
            _telegraph.OnTelegraphEnded -= HandleTelegraphFinished;
        }

        private void Start()
        {
            _stateTimer = Random.Range(attackCooldownMin, attackCooldownMax);
            CurrentState = EnemyState.Chase;
        }

        private void Update()
        {
            if (CurrentState == EnemyState.Dead || CurrentState == EnemyState.Stunned || playerMovement == null)
            {
                StopMovement();
                return;
            }

            Vector3 toPlayer = playerMovement.transform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            switch (CurrentState)
            {
                case EnemyState.Idle:
                    if (dist <= aggroRadius)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    break;

                case EnemyState.Chase:
                    FaceDirection(toPlayer, Time.deltaTime * 10f);
                    if (dist > preferredAttackRange)
                    {
                        MoveToward(playerMovement.transform.position);
                    }
                    else
                    {
                        StopMovement();
                    }

                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f && dist <= preferredAttackRange + 1.4f)
                    {
                        BeginTelegraphAttack();
                    }
                    break;

                case EnemyState.Telegraph:
                    StopMovement();
                    FaceDirection(toPlayer, Time.deltaTime * 14f);
                    break;

                case EnemyState.Recover:
                    StopMovement();
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                    {
                        CurrentState = EnemyState.Chase;
                        _stateTimer = Random.Range(attackCooldownMin, attackCooldownMax);
                    }
                    break;
            }
        }

        private void BeginTelegraphAttack()
        {
            CurrentState = EnemyState.Telegraph;
            _pendingTelegraph = Random.value <= goldenFlashChance
                ? TelegraphType.GoldenFlash
                : TelegraphType.RedFlash;

            _telegraph.StartTelegraph(_pendingTelegraph, telegraphDuration);
        }

        private void HandleTelegraphFinished()
        {
            if (CurrentState != EnemyState.Telegraph || playerMovement == null) return;

            // Lunge forward slightly and resolve strike
            Vector3 toPlayer = playerMovement.transform.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            if (dist > 0.1f)
            {
                Vector3 step = toPlayer.normalized * Mathf.Min(attackLungeDistance, Mathf.Max(0f, dist - 1.3f));
                transform.position += step;
            }

            float finalDist = Vector3.Distance(transform.position, playerMovement.transform.position);
            if (finalDist <= strikeHitRadius)
            {
                if (playerMovement.IsInvulnerable)
                {
                    playerMovement.TryTriggerPerfectDodge();
                }
                else if (squadManager != null)
                {
                    squadManager.DamageActiveAgent(attackDamage);
                }
            }

            CurrentState = EnemyState.Recover;
            _stateTimer = recoverDuration;
        }

        private void HandleParried()
        {
            if (CurrentState == EnemyState.Dead || CurrentState == EnemyState.Stunned) return;

            StopMovement();
            CurrentState = EnemyState.Recover;
            _stateTimer = parriedStaggerDuration;
        }

        private void HandleStunStarted(HealthAndDazeComponent _)
        {
            _telegraph.CancelTelegraph();
            StopMovement();
            CurrentState = EnemyState.Stunned;
        }

        private void HandleStunEnded(HealthAndDazeComponent _)
        {
            if (CurrentState == EnemyState.Dead) return;
            CurrentState = EnemyState.Chase;
            _stateTimer = Random.Range(attackCooldownMin, attackCooldownMax);
        }

        private void HandleDied(HealthAndDazeComponent _)
        {
            _telegraph.CancelTelegraph();
            StopMovement();
            CurrentState = EnemyState.Dead;
            Destroy(gameObject, 0.35f);
        }

        private void MoveToward(Vector3 destination)
        {
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = false;
                _navAgent.SetDestination(destination);
                return;
            }

            Vector3 dir = destination - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
            }
        }

        private void StopMovement()
        {
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
            }
        }

        private void FaceDirection(Vector3 flatDir, float lerpSpeed)
        {
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Clamp01(lerpSpeed));
        }
    }
}
