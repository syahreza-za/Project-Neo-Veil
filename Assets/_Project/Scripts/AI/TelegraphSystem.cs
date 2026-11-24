using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoVeil.AI
{
    public enum TelegraphType
    {
        None,
        GoldenFlash, // Parryable via Squad Switch -> Defensive Assist Parry!
        RedFlash     // Unparryable -> Must Perfect Dodge!
    }

    /// <summary>
    /// Broadcasts ZZZ-style Golden Flash (Parryable) and Red Flash (Unparryable) enemy attack windows.
    /// Maintains a lightweight static list of active telegraphs so SquadManager can check
    /// Defensive Assist Parry eligibility with zero physics queries or allocations.
    /// </summary>
    public class TelegraphSystem : MonoBehaviour
    {
        private static readonly List<TelegraphSystem> ActiveTelegraphs = new List<TelegraphSystem>(16);

        [Header("Optional Visual Feedback (Greybox / VFX)")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private GameObject goldenFlashVfx;
        [SerializeField] private GameObject redFlashVfx;
        [SerializeField] private Color goldenColor = new Color(1f, 0.78f, 0.12f);
        [SerializeField] private Color redColor = new Color(1f, 0.14f, 0.28f);

        private Material _runtimeMat;
        private Color _defaultColor = Color.gray;

        public TelegraphType ActiveType { get; private set; } = TelegraphType.None;
        public float TimeRemaining { get; private set; }
        public float TotalDuration { get; private set; }
        public bool IsActive => ActiveType != TelegraphType.None && TimeRemaining > 0f;
        public bool IsParryable => IsActive && ActiveType == TelegraphType.GoldenFlash;

        public event Action<TelegraphType, float> OnTelegraphStarted;
        public event Action OnTelegraphParried;
        public event Action OnTelegraphEnded;

        private void Awake()
        {
            if (indicatorRenderer == null)
            {
                indicatorRenderer = GetComponentInChildren<Renderer>();
            }

            if (indicatorRenderer != null)
            {
                _runtimeMat = indicatorRenderer.material;
                _defaultColor = _runtimeMat.color;
            }

            ToggleVfx(TelegraphType.None);
        }

        private void OnDisable()
        {
            CancelTelegraph();
        }

        private void Update()
        {
            if (!IsActive) return;

            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                EndTelegraphInternal(wasParried: false);
            }
        }

        public void StartTelegraph(TelegraphType type, float duration)
        {
            if (type == TelegraphType.None || duration <= 0f) return;

            ActiveType = type;
            TotalDuration = duration;
            TimeRemaining = duration;

            if (!ActiveTelegraphs.Contains(this))
            {
                ActiveTelegraphs.Add(this);
            }

            ApplyVisual(type);
            OnTelegraphStarted?.Invoke(type, duration);
        }

        /// <summary>
        /// Called by SquadManager when the player presses Switch during a GoldenFlash window.
        /// Returns true if the parry succeeded and cancels the enemy attack.
        /// </summary>
        public bool TryConsumeParry()
        {
            if (!IsParryable) return false;

            EndTelegraphInternal(wasParried: true);
            return true;
        }

        public void CancelTelegraph()
        {
            if (ActiveType != TelegraphType.None)
            {
                EndTelegraphInternal(wasParried: false);
            }
        }

        /// <summary>
        /// Finds the closest enemy currently in a GoldenFlash parry window within maxRange of playerPosition.
        /// </summary>
        public static TelegraphSystem FindClosestParryable(Vector3 playerPosition, float maxRange)
        {
            TelegraphSystem best = null;
            float bestSqr = maxRange * maxRange;

            for (int i = ActiveTelegraphs.Count - 1; i >= 0; i--)
            {
                TelegraphSystem t = ActiveTelegraphs[i];
                if (t == null || !t.gameObject.activeInHierarchy || !t.IsParryable)
                {
                    if (t == null) ActiveTelegraphs.RemoveAt(i);
                    continue;
                }

                float sqr = (t.transform.position - playerPosition).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = t;
                }
            }

            return best;
        }

        /// <summary>
        /// Returns the closest active telegraph (Golden or Red) for HUD warning indicators.
        /// </summary>
        public static TelegraphSystem GetAnyActiveTelegraph()
        {
            for (int i = ActiveTelegraphs.Count - 1; i >= 0; i--)
            {
                TelegraphSystem t = ActiveTelegraphs[i];
                if (t != null && t.IsActive) return t;
            }
            return null;
        }

        private void EndTelegraphInternal(bool wasParried)
        {
            ActiveType = TelegraphType.None;
            TimeRemaining = 0f;
            ActiveTelegraphs.Remove(this);
            ApplyVisual(TelegraphType.None);

            if (wasParried)
            {
                OnTelegraphParried?.Invoke();
            }
            else
            {
                OnTelegraphEnded?.Invoke();
            }
        }

        private void ApplyVisual(TelegraphType type)
        {
            ToggleVfx(type);
            if (_runtimeMat == null) return;

            _runtimeMat.color = type switch
            {
                TelegraphType.GoldenFlash => goldenColor,
                TelegraphType.RedFlash => redColor,
                _ => _defaultColor
            };
        }

        private void ToggleVfx(TelegraphType type)
        {
            if (goldenFlashVfx != null) goldenFlashVfx.SetActive(type == TelegraphType.GoldenFlash);
            if (redFlashVfx != null) redFlashVfx.SetActive(type == TelegraphType.RedFlash);
        }
    }
}
