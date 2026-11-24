using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeoVeil.City
{
    /// <summary>
    /// Urban Day-Dusk-Neon Night controller designed for Unity 6 URP Forward+.
    /// Smoothly rotates the sun/moon directional light, shifts ambient/fog colors,
    /// and automatically powers on streetlights and emissive neon signboards at dusk/night!
    /// Quick test keys: F1 = Anime Day (11:00), F2 = Cyber Dusk (18:15), F3 = Neon Night (22:30).
    /// </summary>
    public class DayNightNeonController : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField, Range(0f, 24f)] private float timeOfDay = 21.5f;
        [Tooltip("Real-world seconds per 1 in-game hour. Set to 0 to freeze time.")]
        [SerializeField, Min(0f)] private float realSecondsPerGameHour = 25f;
        [SerializeField] private bool enableDebugHotkeys = true;

        [Header("Sun / Moon Directional Light")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Color daySunColor = new Color(1.0f, 0.96f, 0.88f);
        [SerializeField] private Color duskSunColor = new Color(1.0f, 0.45f, 0.42f);
        [SerializeField] private Color nightMoonColor = new Color(0.32f, 0.48f, 0.92f);
        [SerializeField, Min(0f)] private float daySunIntensity = 1.6f;
        [SerializeField, Min(0f)] private float nightMoonIntensity = 0.42f;

        [Header("Ambient & Fog Atmosphere")]
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.65f, 0.8f);
        [SerializeField] private Color duskAmbient = new Color(0.38f, 0.22f, 0.36f);
        [SerializeField] private Color nightAmbient = new Color(0.08f, 0.11f, 0.22f);
        [SerializeField] private bool controlFogColor = true;

        [Header("Urban Neon Lights & Signboards (URP Forward+)")]
        [SerializeField] private Light[] cityNeonLights;
        [SerializeField, Min(0f)] private float maxNeonLightIntensity = 3.5f;
        [SerializeField] private Renderer[] neonSignRenderers;
        [SerializeField] private Color defaultNeonEmissionColor = new Color(0.15f, 0.85f, 1.0f);
        [SerializeField, Min(0f)] private float maxEmissionBoost = 3.2f;

        [Header("Urban Anime Skybox Colors (Auto-Drives UrbanAnimeSkyboxURP.shader)")]
        [SerializeField] private Color daySkyTop = new Color(0.14f, 0.45f, 0.92f);
        [SerializeField] private Color daySkyHorizon = new Color(0.58f, 0.82f, 1.0f);
        [SerializeField] private Color duskSkyTop = new Color(0.16f, 0.10f, 0.30f);
        [SerializeField] private Color duskSkyHorizon = new Color(0.95f, 0.36f, 0.50f);
        [SerializeField] private Color nightSkyTop = new Color(0.03f, 0.05f, 0.14f);
        [SerializeField] private Color nightSkyHorizon = new Color(0.24f, 0.12f, 0.38f);

        private static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");
        private static readonly int SkyTopColorProp = Shader.PropertyToID("_TopColor");
        private static readonly int SkyHorizonColorProp = Shader.PropertyToID("_HorizonColor");
        private static readonly int SkyBottomColorProp = Shader.PropertyToID("_BottomColor");
        private static readonly int SkyStarIntensityProp = Shader.PropertyToID("_StarIntensity");
        private string _lastPeriodName = string.Empty;

        public float TimeOfDay => timeOfDay;
        public bool IsNightTime => timeOfDay >= 18.5f || timeOfDay < 5.5f;
        public string CurrentPeriodName { get; private set; } = "NEON NIGHT";

        public string FormattedTime
        {
            get
            {
                int hours = Mathf.FloorToInt(timeOfDay) % 24;
                int minutes = Mathf.FloorToInt((timeOfDay - Mathf.Floor(timeOfDay)) * 60f);
                return $"{hours:00}:{minutes:00}";
            }
        }

        public event Action<string> OnPeriodChanged;

        private void Awake()
        {
            if (sunLight == null)
            {
                sunLight = RenderSettings.sun;
            }
            ApplyAtmosphere();
        }

        private void Update()
        {
            if (enableDebugHotkeys && Keyboard.current != null)
            {
                if (Keyboard.current.f1Key.wasPressedThisFrame) SetTimeOfDay(11.0f);
                else if (Keyboard.current.f2Key.wasPressedThisFrame) SetTimeOfDay(18.25f);
                else if (Keyboard.current.f3Key.wasPressedThisFrame) SetTimeOfDay(22.5f);
            }

            if (realSecondsPerGameHour > 0f)
            {
                timeOfDay += Time.deltaTime / realSecondsPerGameHour;
                if (timeOfDay >= 24f) timeOfDay -= 24f;
                ApplyAtmosphere();
            }
        }

        public void SetTimeOfDay(float newHour)
        {
            timeOfDay = Mathf.Repeat(newHour, 24f);
            ApplyAtmosphere();
        }

        private void ApplyAtmosphere()
        {
            // Compute daylight factor (0 = full night, 1 = midday) and dusk factor (peaks at 6:00 and 18:15)
            float sunAngle = (timeOfDay / 24f) * 360f - 90f;
            float dayFactor = Mathf.Clamp01(Mathf.Sin(((timeOfDay - 6f) / 12f) * Mathf.PI));
            float eveningPeak = Mathf.Exp(-Mathf.Pow((timeOfDay - 18.2f) / 1.35f, 2f));
            float dawnPeak = Mathf.Exp(-Mathf.Pow((timeOfDay - 6.0f) / 1.25f, 2f));
            float duskFactor = Mathf.Clamp01(Mathf.Max(eveningPeak, dawnPeak));
            float neonPower = Mathf.Clamp01(1f - dayFactor * 1.45f);

            // 1. Directional Light
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, 155f, 0f);
                Color baseLight = Color.Lerp(nightMoonColor, daySunColor, dayFactor);
                sunLight.color = Color.Lerp(baseLight, duskSunColor, duskFactor * 0.85f);
                sunLight.intensity = Mathf.Lerp(nightMoonIntensity, daySunIntensity, dayFactor);
            }

            // 2. Ambient, Fog & Procedural Anime Skybox
            Color baseAmb = Color.Lerp(nightAmbient, dayAmbient, dayFactor);
            Color finalAmb = Color.Lerp(baseAmb, duskAmbient, duskFactor * 0.8f);
            RenderSettings.ambientLight = finalAmb;

            if (controlFogColor)
            {
                RenderSettings.fogColor = finalAmb;
            }

            Material skyMat = RenderSettings.skybox;
            if (skyMat != null && skyMat.HasProperty(SkyTopColorProp))
            {
                Color topCol = Color.Lerp(Color.Lerp(nightSkyTop, daySkyTop, dayFactor), duskSkyTop, duskFactor * 0.85f);
                Color horCol = Color.Lerp(Color.Lerp(nightSkyHorizon, daySkyHorizon, dayFactor), duskSkyHorizon, duskFactor * 0.9f);
                skyMat.SetColor(SkyTopColorProp, topCol);
                skyMat.SetColor(SkyHorizonColorProp, horCol);
                if (skyMat.HasProperty(SkyBottomColorProp)) skyMat.SetColor(SkyBottomColorProp, finalAmb * 0.6f);
                if (skyMat.HasProperty(SkyStarIntensityProp)) skyMat.SetFloat(SkyStarIntensityProp, neonPower * 1.4f);
            }

            // 3. Urban Streetlights & Neon Lamps
            if (cityNeonLights != null)
            {
                for (int i = 0; i < cityNeonLights.Length; i++)
                {
                    Light l = cityNeonLights[i];
                    if (l == null) continue;
                    bool shouldEnable = neonPower > 0.05f;
                    if (l.enabled != shouldEnable) l.enabled = shouldEnable;
                    if (shouldEnable) l.intensity = maxNeonLightIntensity * neonPower;
                }
            }

            // 4. Emissive Neon Signboards (URP Lit / Unlit Emission)
            if (neonSignRenderers != null)
            {
                Color emissive = defaultNeonEmissionColor * (neonPower * maxEmissionBoost);
                for (int i = 0; i < neonSignRenderers.Length; i++)
                {
                    Renderer rend = neonSignRenderers[i];
                    if (rend == null) continue;
                    Material mat = rend.material;
                    if (mat != null && mat.HasProperty(EmissionColorProp))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor(EmissionColorProp, emissive);
                    }
                }
            }

            // 5. Update Period Name
            CurrentPeriodName = timeOfDay switch
            {
                >= 5.5f and < 9.5f => "MORNING",
                >= 9.5f and < 17.0f => "ANIME DAY",
                >= 17.0f and < 19.5f => "CYBER DUSK",
                _ => "NEON NIGHT"
            };

            if (CurrentPeriodName != _lastPeriodName)
            {
                _lastPeriodName = CurrentPeriodName;
                OnPeriodChanged?.Invoke(CurrentPeriodName);
            }
        }
    }
}
