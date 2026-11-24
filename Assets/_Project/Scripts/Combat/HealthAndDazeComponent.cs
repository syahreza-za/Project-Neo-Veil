using System;
using UnityEngine;

namespace NeoVeil.Combat
{
    public enum AgentRole
    {
        Striker,    // Fast DPS, highest bonus damage vs Stunned targets
        Breaker,    // Heavy Daze buildup & Defensive Assist Parry specialist
        Channeler   // Anomaly buildup & squad ATK buff support
    }

    public struct DamagePayload
    {
        public float RawDamage;
        public float RawDaze;
        public float DazeMultiplier;
        public float StunDamageMultiplier;
        public AgentRole AttackerRole;
        public bool IsHeavyAttack;
        public bool TriggersChainOnStun;
        public Vector3 SourcePosition;
        public string Tag;
    }

    public struct DamageResult
    {
        public float FinalDamage;
        public bool WasStunnedHit;
        public bool TriggeredStun;
        public bool TriggeredAnomalyBurst;
        public bool KilledTarget;
    }

    /// <summary>
    /// Enemy (or target) HP, ZZZ-style Daze/Stun meter, and Channeler Anomaly buildup component.
    /// Broadcasts clean C# events so UI bars, VFX, Audio, and ChainAttackController stay decoupled.
    /// </summary>
    public class HealthAndDazeComponent : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField, Min(1f)] private float maxHP = 600f;

        [Header("ZZZ Daze & Stun Settings")]
        [SerializeField, Min(1f)] private float maxDaze = 100f;
        [SerializeField, Min(1f)] private float stunDuration = 6.5f;
        [SerializeField, Min(1f)] private float baseStunDamageMultiplier = 1.5f;

        [Header("Channeler Anomaly Burst")]
        [SerializeField, Min(1f)] private float anomalyThreshold = 100f;
        [SerializeField, Min(0.5f)] private float anomalyBurstBonusMultiplier = 2.2f;

        [Header("Optional Greybox Hit Flash")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color stunnedTintColor = new Color(1f, 0.75f, 0.15f);

        private Material _runtimeMat;
        private Color _originalColor = Color.gray;
        private float _hitFlashTimer;

        public float CurrentHP { get; private set; }
        public float MaxHP => maxHP;
        public float NormalizedHP => maxHP > 0f ? CurrentHP / maxHP : 0f;

        public float CurrentDaze { get; private set; }
        public float MaxDaze => maxDaze;
        public float NormalizedDaze => maxDaze > 0f ? CurrentDaze / maxDaze : 0f;

        public float CurrentAnomaly { get; private set; }
        public bool IsStunned { get; private set; }
        public float StunTimeRemaining { get; private set; }
        public bool IsDead => CurrentHP <= 0f;

        public event Action<float, float> OnHealthChanged;            // current, max
        public event Action<float, float> OnDazeChanged;              // current, max
        public event Action<DamageResult, string> OnDamaged;
        public event Action<HealthAndDazeComponent> OnStunStarted;
        public event Action<HealthAndDazeComponent> OnStunEnded;
        public event Action<HealthAndDazeComponent, float> OnAnomalyBurst;
        public event Action<HealthAndDazeComponent> OnDied;

        private void Awake()
        {
            CurrentHP = maxHP;
            CurrentDaze = 0f;
            CurrentAnomaly = 0f;

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                _runtimeMat = targetRenderer.material;
                _originalColor = _runtimeMat.color;
            }
        }

        private void Update()
        {
            if (IsDead) return;

            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.unscaledDeltaTime;
                if (_hitFlashTimer <= 0f)
                {
                    RestoreVisualTint();
                }
            }

            if (IsStunned)
            {
                StunTimeRemaining -= Time.deltaTime;
                CurrentDaze = Mathf.Max(0f, (StunTimeRemaining / stunDuration) * maxDaze);
                OnDazeChanged?.Invoke(CurrentDaze, maxDaze);

                if (StunTimeRemaining <= 0f)
                {
                    EndStun();
                }
            }
        }

        public DamageResult ApplyDamage(DamagePayload payload)
        {
            var result = new DamageResult();
            if (IsDead) return result;

            bool wasStunned = IsStunned;
            float stunMult = wasStunned
                ? Mathf.Max(baseStunDamageMultiplier, payload.StunDamageMultiplier)
                : 1f;

            float finalDmg = Mathf.Max(1f, Mathf.Round(payload.RawDamage * stunMult));
            CurrentHP = Mathf.Max(0f, CurrentHP - finalDmg);

            result.FinalDamage = finalDmg;
            result.WasStunnedHit = wasStunned;

            TriggerHitFlash();
            OnHealthChanged?.Invoke(CurrentHP, maxHP);

            // Channeler role builds Anomaly toward an explosive Anomaly Burst
            if (payload.AttackerRole == AgentRole.Channeler && CurrentHP > 0f)
            {
                CurrentAnomaly += 34f;
                if (CurrentAnomaly >= anomalyThreshold)
                {
                    CurrentAnomaly = 0f;
                    float burstDmg = Mathf.Round(payload.RawDamage * anomalyBurstBonusMultiplier);
                    CurrentHP = Mathf.Max(0f, CurrentHP - burstDmg);
                    result.FinalDamage += burstDmg;
                    result.TriggeredAnomalyBurst = true;
                    OnHealthChanged?.Invoke(CurrentHP, maxHP);
                    OnAnomalyBurst?.Invoke(this, burstDmg);
                }
            }

            // Build Daze while not stunned
            if (!IsStunned && CurrentHP > 0f && payload.RawDaze > 0f)
            {
                float dazeGain = payload.RawDaze * Mathf.Max(0.1f, payload.DazeMultiplier);
                CurrentDaze = Mathf.Min(maxDaze, CurrentDaze + dazeGain);
                OnDazeChanged?.Invoke(CurrentDaze, maxDaze);

                if (CurrentDaze >= maxDaze)
                {
                    StartStun();
                    result.TriggeredStun = true;
                }
            }

            if (CurrentHP <= 0f)
            {
                result.KilledTarget = true;
                IsStunned = false;
                OnDied?.Invoke(this);
            }

            OnDamaged?.Invoke(result, payload.Tag);
            return result;
        }

        public void ResetState()
        {
            CurrentHP = maxHP;
            CurrentDaze = 0f;
            CurrentAnomaly = 0f;
            IsStunned = false;
            StunTimeRemaining = 0f;
            RestoreVisualTint();
            OnHealthChanged?.Invoke(CurrentHP, maxHP);
            OnDazeChanged?.Invoke(CurrentDaze, maxDaze);
        }

        private void StartStun()
        {
            IsStunned = true;
            StunTimeRemaining = stunDuration;
            if (_runtimeMat != null)
            {
                _runtimeMat.color = stunnedTintColor;
            }
            OnStunStarted?.Invoke(this);
        }

        private void EndStun()
        {
            IsStunned = false;
            CurrentDaze = 0f;
            RestoreVisualTint();
            OnDazeChanged?.Invoke(CurrentDaze, maxDaze);
            OnStunEnded?.Invoke(this);
        }

        private void TriggerHitFlash()
        {
            if (_runtimeMat == null) return;
            _runtimeMat.color = hitFlashColor;
            _hitFlashTimer = 0.08f;
        }

        private void RestoreVisualTint()
        {
            if (_runtimeMat == null) return;
            _runtimeMat.color = IsStunned ? stunnedTintColor : _originalColor;
        }
    }
}
