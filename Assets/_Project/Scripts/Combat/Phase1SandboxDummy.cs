using UnityEngine;
using UnityEngine.InputSystem;
using NeoVeil.Core;
using NeoVeil.Player;

namespace NeoVeil.Combat
{
    /// <summary>
    /// Fase 1 Greybox Sandbox Dummy so you can immediately test:
    /// - SoftLockOnSystem targeting
    /// - Perfect Dodge Slow-Motion (press 'T' or wait for periodic pulse, then Dodge right before impact!)
    /// - Hitstop feedback (press 'H' to feel a 45ms ZZZ impact freeze frame)
    /// </summary>
    public class Phase1SandboxDummy : MonoBehaviour
    {
        [Header("Test References")]
        [SerializeField] private PlayerMovementController player;
        [SerializeField] private Renderer visualRenderer;

        [Header("Simulated Attack Timing")]
        [SerializeField] private bool autoPulseAttack = true;
        [SerializeField, Min(1f)] private float pulseInterval = 3.5f;
        [SerializeField, Min(0.15f)] private float telegraphWarningTime = 0.45f;
        [SerializeField, Min(1f)] private float attackRange = 5f;

        [Header("Telegraph Visual Colors")]
        [SerializeField] private Color idleColor = new Color(0.25f, 0.25f, 0.35f);
        [SerializeField] private Color redFlashColor = new Color(1f, 0.15f, 0.25f);
        [SerializeField] private Color perfectDodgeColor = new Color(0.1f, 0.9f, 1f);

        private float _cooldownTimer;
        private float _warningTimer;
        private bool _isTelegraphing;
        private Material _runtimeMaterial;

        private void Awake()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerMovementController>();
            }

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }

            if (visualRenderer != null)
            {
                _runtimeMaterial = visualRenderer.material;
                SetColor(idleColor);
            }

            _cooldownTimer = pulseInterval;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tKey.wasPressedThisFrame && !_isTelegraphing)
                {
                    StartTelegraph();
                }

                if (kb.hKey.wasPressedThisFrame && TimeScaleManager.Instance != null)
                {
                    TimeScaleManager.Instance.TriggerHitstop(0.05f);
                    Debug.Log("[NeoVeil Test] Hitstop triggered (50ms)!");
                }
            }

            if (_isTelegraphing)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0f)
                {
                    ResolveSimulatedAttack();
                }
                return;
            }

            if (autoPulseAttack && player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= attackRange)
                {
                    _cooldownTimer -= Time.deltaTime;
                    if (_cooldownTimer <= 0f)
                    {
                        StartTelegraph();
                    }
                }
            }
        }

        private void StartTelegraph()
        {
            _isTelegraphing = true;
            _warningTimer = telegraphWarningTime;
            SetColor(redFlashColor);
            Debug.Log("[NeoVeil Test] RED FLASH! Tekan Klik Kanan / Shift untuk Perfect Dodge!");
        }

        private void ResolveSimulatedAttack()
        {
            _isTelegraphing = false;
            _cooldownTimer = pulseInterval;

            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                if (player.TryTriggerPerfectDodge())
                {
                    SetColor(perfectDodgeColor);
                    Debug.Log("[NeoVeil Test] PERFECT DODGE! Slow-Motion aktif + Dodge Counter siap!");
                    Invoke(nameof(ResetVisual), 0.5f);
                    return;
                }

                Debug.Log("[NeoVeil Test] Player terkena serangan simulasi (tidak sedang i-frame).");
            }

            SetColor(idleColor);
        }

        private void ResetVisual()
        {
            if (!_isTelegraphing)
            {
                SetColor(idleColor);
            }
        }

        private void SetColor(Color c)
        {
            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.color = c;
            }
        }
    }
}
