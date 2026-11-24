using System;
using UnityEngine;

namespace NeoVeil.Core
{
    /// <summary>
    /// Centralized time-scale controller for ZZZ-style combat juice:
    /// - Hitstop (30-60ms near-freeze on heavy hits and Defensive Assist Parry)
    /// - Slow-Motion (e.g., 0.35s slow-mo on Perfect Dodge or Chain Attack prompt)
    /// Uses unscaled time so overlapping requests never corrupt Time.timeScale or Time.fixedDeltaTime.
    /// </summary>
    public class TimeScaleManager : MonoBehaviour
    {
        public static TimeScaleManager Instance { get; private set; }

        [Header("Default Feel Settings")]
        [SerializeField, Range(0.0f, 0.15f)] private float hitstopTimeScale = 0.02f;
        [SerializeField, Range(0.05f, 0.6f)] private float perfectDodgeTimeScale = 0.25f;
        [SerializeField, Min(0.05f)] private float perfectDodgeDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float recoverySpeed = 10f;

        private float _defaultFixedDeltaTime;
        private float _hitstopTimer;
        private float _slowMoTimer;
        private float _activeSlowMoScale = 1f;
        private bool _isPaused;
        private bool _wasModified;

        public bool IsInHitstop => _hitstopTimer > 0f;
        public bool IsInSlowMotion => _slowMoTimer > 0f;

        public event Action<float> OnHitstopTriggered;
        public event Action<float, float> OnSlowMotionTriggered;
        public event Action OnTimeScaleRestored;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            if (_isPaused)
            {
                ApplyTimeScale(0f);
                return;
            }

            // Hitstop has highest priority over slow-motion
            if (_hitstopTimer > 0f)
            {
                _hitstopTimer -= Time.unscaledDeltaTime;
                ApplyTimeScale(hitstopTimeScale);
                _wasModified = true;
                return;
            }

            if (_slowMoTimer > 0f)
            {
                _slowMoTimer -= Time.unscaledDeltaTime;
                ApplyTimeScale(_activeSlowMoScale);
                _wasModified = true;
                return;
            }

            // Smoothly return timeScale to 1.0 after slow-mo/hitstop ends
            if (_wasModified)
            {
                float nextScale = Mathf.MoveTowards(Time.timeScale, 1f, recoverySpeed * Time.unscaledDeltaTime);
                ApplyTimeScale(nextScale);

                if (Mathf.Approximately(nextScale, 1f))
                {
                    ApplyTimeScale(1f);
                    _wasModified = false;
                    OnTimeScaleRestored?.Invoke();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Time.timeScale = 1f;
                if (_defaultFixedDeltaTime > 0f)
                {
                    Time.fixedDeltaTime = _defaultFixedDeltaTime;
                }
                Instance = null;
            }
        }

        /// <summary>
        /// Triggers a snappy ZZZ-style impact freeze frame.
        /// </summary>
        public void TriggerHitstop(float duration = 0.045f)
        {
            if (_isPaused || duration <= 0f) return;

            _hitstopTimer = Mathf.Max(_hitstopTimer, duration);
            ApplyTimeScale(hitstopTimeScale);
            _wasModified = true;
            OnHitstopTriggered?.Invoke(duration);
        }

        /// <summary>
        /// Triggers Perfect Dodge slow-motion using default tuned values.
        /// </summary>
        public void TriggerPerfectDodgeSlowMo()
        {
            TriggerSlowMotion(perfectDodgeTimeScale, perfectDodgeDuration);
        }

        /// <summary>
        /// Triggers custom slow-motion (e.g., Perfect Dodge or Chain Attack prompt).
        /// </summary>
        public void TriggerSlowMotion(float targetScale, float duration)
        {
            if (_isPaused || duration <= 0f) return;

            _activeSlowMoScale = Mathf.Clamp(targetScale, 0.05f, 1f);
            _slowMoTimer = Mathf.Max(_slowMoTimer, duration);
            _wasModified = true;
            OnSlowMotionTriggered?.Invoke(_activeSlowMoScale, duration);
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (!paused)
            {
                ApplyTimeScale(1f);
            }
        }

        private void ApplyTimeScale(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = scale > 0f
                ? Mathf.Max(0.0005f, _defaultFixedDeltaTime * scale)
                : _defaultFixedDeltaTime;
        }
    }
}
