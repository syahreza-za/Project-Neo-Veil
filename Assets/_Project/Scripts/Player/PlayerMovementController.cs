using System;
using UnityEngine;
using NeoVeil.Core;
using NeoVeil.Combat;

namespace NeoVeil.Player
{
    /// <summary>
    /// Responsive third-person controller tailored for Urban Semi-Open World exploration (NTE)
    /// and snappy action combat movement (ZZZ):
    /// - Camera-relative movement with smooth turning
    /// - 2-charge ZZZ-style Dodge with i-frames, cooldown, and automatic Dodge-into-Sprint transition
    /// - Perfect Dodge hook (triggers TimeScaleManager slow-motion + counter window)
    /// - Jump with Coyote Time & Jump Buffering for urban rooftop/platform traversal
    /// - External Attack Lunge hook ready for Fase 2 combat combos
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private SoftLockOnSystem softLockOn;

        [Header("Locomotion (Urban Exploration)")]
        [SerializeField, Min(1f)] private float walkSpeed = 5.5f;
        [SerializeField, Min(1f)] private float sprintSpeed = 9.0f;
        [SerializeField, Min(1f)] private float acceleration = 18f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.08f;

        [Header("Jump & Gravity")]
        [SerializeField, Min(0.5f)] private float jumpHeight = 1.6f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedStickForce = -3f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("ZZZ-Style Dodge & Perfect Dodge")]
        [SerializeField, Min(1f)] private float dodgeSpeed = 15f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.24f;
        [SerializeField, Min(0.05f)] private float iFrameDuration = 0.22f;
        [SerializeField, Min(1)] private int maxDodgeCharges = 2;
        [SerializeField, Min(0.1f)] private float dodgeChainResetDelay = 0.55f;
        [SerializeField, Min(0.1f)] private float dodgeCooldownAfterExhaust = 0.85f;
        [SerializeField, Min(0.1f)] private float perfectDodgeCounterWindow = 0.9f;

        private CharacterController _controller;

        // Movement state
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _rotationVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isMovementLocked;
        private bool _sprintFromDodge;

        // Dodge state
        private int _remainingDodgeCharges;
        private float _dodgeTimer;
        private float _iFrameTimer;
        private float _dodgeChainResetTimer;
        private float _dodgeCooldownTimer;
        private float _perfectDodgeCounterTimer;
        private Vector3 _dodgeDirection;

        // External combat lunge (used by Fase 2 CombatController)
        private float _lungeTimer;
        private float _lungeSpeed;
        private Vector3 _lungeDirection;

        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public bool IsDodging => _dodgeTimer > 0f;
        public bool IsInvulnerable => _iFrameTimer > 0f;
        public bool CanDodgeCounter => _perfectDodgeCounterTimer > 0f;
        public bool IsSprinting { get; private set; }
        public float CurrentHorizontalSpeed => _horizontalVelocity.magnitude;

        public event Action OnDodgeStarted;
        public event Action OnPerfectDodgeTriggered;
        public event Action OnJumpExecuted;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _remainingDodgeCharges = maxDodgeCharges;

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (softLockOn == null)
            {
                softLockOn = GetComponent<SoftLockOnSystem>();
            }
        }

        private void OnEnable()
        {
            if (inputReader == null) return;
            inputReader.OnJumpPressed += HandleJumpPressed;
            inputReader.OnDodgePressed += HandleDodgePressed;
        }

        private void OnDisable()
        {
            if (inputReader == null) return;
            inputReader.OnJumpPressed -= HandleJumpPressed;
            inputReader.OnDodgePressed -= HandleDodgePressed;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateTimers(dt);

            Vector3 moveIntent = GetCameraRelativeMoveDirection();
            if (softLockOn != null)
            {
                Vector3 lockIntent = moveIntent.sqrMagnitude > 0.01f
                    ? moveIntent
                    : GetCameraFlatForward();
                softLockOn.UpdateTarget(lockIntent);
            }

            HandleGravityAndJump(dt);
            HandleHorizontalMovement(dt, moveIntent);

            Vector3 finalMotion = _horizontalVelocity + (Vector3.up * _verticalVelocity);
            _controller.Move(finalMotion * dt);
        }

        private void UpdateTimers(float dt)
        {
            if (_controller.isGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= dt;
            }

            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;
            if (_iFrameTimer > 0f) _iFrameTimer -= dt;
            if (_perfectDodgeCounterTimer > 0f) _perfectDodgeCounterTimer -= dt;
            if (_lungeTimer > 0f) _lungeTimer -= dt;

            if (_dodgeTimer > 0f)
            {
                _dodgeTimer -= dt;
                if (_dodgeTimer <= 0f)
                {
                    // ZZZ-style feel: holding Shift/RMB through the end of a Dodge flows straight into Sprint
                    _sprintFromDodge = inputReader != null && inputReader.IsDodgeHeld;
                }
            }

            if (_dodgeCooldownTimer > 0f)
            {
                _dodgeCooldownTimer -= dt;
                if (_dodgeCooldownTimer <= 0f)
                {
                    _remainingDodgeCharges = maxDodgeCharges;
                }
            }
            else if (!IsDodging && _remainingDodgeCharges < maxDodgeCharges)
            {
                _dodgeChainResetTimer -= dt;
                if (_dodgeChainResetTimer <= 0f)
                {
                    _remainingDodgeCharges = maxDodgeCharges;
                }
            }
        }

        private void HandleHorizontalMovement(float dt, Vector3 moveIntent)
        {
            if (IsDodging)
            {
                _horizontalVelocity = _dodgeDirection * dodgeSpeed;
                RotateToward(_dodgeDirection, dt * 2.5f);
                IsSprinting = false;
                return;
            }

            if (_lungeTimer > 0f)
            {
                _horizontalVelocity = _lungeDirection * _lungeSpeed;
                RotateToward(_lungeDirection, dt * 3f);
                IsSprinting = false;
                return;
            }

            if (_isMovementLocked)
            {
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, acceleration * 2f * dt);
                IsSprinting = false;
                return;
            }

            bool hasMoveInput = moveIntent.sqrMagnitude > 0.01f;
            bool wantsSprint = hasMoveInput && (
                (inputReader != null && inputReader.IsSprintHeld) || _sprintFromDodge
            );

            if (!hasMoveInput || (inputReader != null && !inputReader.IsSprintHeld && !inputReader.IsDodgeHeld))
            {
                _sprintFromDodge = false;
                wantsSprint = false;
            }

            IsSprinting = wantsSprint;
            float targetSpeed = hasMoveInput ? (IsSprinting ? sprintSpeed : walkSpeed) : 0f;
            Vector3 targetVelocity = moveIntent * targetSpeed;

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                acceleration * dt
            );

            if (hasMoveInput)
            {
                RotateToward(moveIntent, dt);
            }
        }

        private void HandleGravityAndJump(float dt)
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = groundedStickForce;
            }

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !IsDodging && !_isMovementLocked)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                OnJumpExecuted?.Invoke();
            }

            _verticalVelocity += gravity * dt;
        }

        private void HandleJumpPressed()
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        private void HandleDodgePressed()
        {
            if (_isMovementLocked || IsDodging || _dodgeCooldownTimer > 0f || _remainingDodgeCharges <= 0)
            {
                return;
            }

            Vector3 moveIntent = GetCameraRelativeMoveDirection();
            _dodgeDirection = moveIntent.sqrMagnitude > 0.01f ? moveIntent : transform.forward;
            _dodgeDirection.y = 0f;
            _dodgeDirection.Normalize();

            _dodgeTimer = dodgeDuration;
            _iFrameTimer = iFrameDuration;
            _remainingDodgeCharges--;

            if (_remainingDodgeCharges <= 0)
            {
                _dodgeCooldownTimer = dodgeCooldownAfterExhaust;
                _dodgeChainResetTimer = 0f;
            }
            else
            {
                _dodgeChainResetTimer = dodgeChainResetDelay;
            }

            OnDodgeStarted?.Invoke();
        }

        /// <summary>
        /// Call this when an enemy attack overlaps the player while IsInvulnerable is true.
        /// Triggers ZZZ-style Perfect Dodge slow-motion and opens the Dodge Counter window.
        /// </summary>
        public bool TryTriggerPerfectDodge()
        {
            if (!IsInvulnerable) return false;

            _perfectDodgeCounterTimer = perfectDodgeCounterWindow;
            _remainingDodgeCharges = maxDodgeCharges;
            _dodgeCooldownTimer = 0f;

            if (TimeScaleManager.Instance != null)
            {
                TimeScaleManager.Instance.TriggerPerfectDodgeSlowMo();
            }

            OnPerfectDodgeTriggered?.Invoke();
            return true;
        }

        /// <summary>
        /// Consumes the Dodge Counter flag when the player presses Attack during the counter window.
        /// </summary>
        public bool ConsumeDodgeCounter()
        {
            if (!CanDodgeCounter) return false;
            _perfectDodgeCounterTimer = 0f;
            return true;
        }

        /// <summary>
        /// Applies a short forward lunge during melee combo attacks (used by Fase 2 CombatController).
        /// </summary>
        public void ApplyAttackLunge(Vector3 direction, float speed, float duration)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;

            _lungeDirection = direction.normalized;
            _lungeSpeed = speed;
            _lungeTimer = duration;
        }

        public void SetMovementLocked(bool locked)
        {
            _isMovementLocked = locked;
        }

        private Vector3 GetCameraRelativeMoveDirection()
        {
            if (inputReader == null) return Vector3.zero;

            Vector2 raw = inputReader.MoveInput;
            if (raw.sqrMagnitude < 0.001f) return Vector3.zero;

            Vector3 camForward = GetCameraFlatForward();
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 dir = (camForward * raw.y) + (camRight * raw.x);
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private Vector3 GetCameraFlatForward()
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private void RotateToward(Vector3 direction, float dt)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref _rotationVelocity,
                rotationSmoothTime,
                Mathf.Infinity,
                dt
            );
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }
    }
}
