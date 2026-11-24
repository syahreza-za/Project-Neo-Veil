using UnityEngine;
using NeoVeil.AI;
using NeoVeil.City;
using NeoVeil.Combat;
using NeoVeil.Squad;

namespace NeoVeil.UI
{
    /// <summary>
    /// Plug-and-play ZZZ + NTE HUD overlay for greybox testing (Fase 2, 3 & 4):
    /// - 3-Agent Squad HP/Energy & Decibel Gauge
    /// - District Clock (Day/Dusk/Neon Night) & Urban Inventory (Credits/Cores/Disks)
    /// - Active Anomaly Rift Wave tracker, Golden/Red Telegraph alert & Chain Attack prompt
    /// - [F] Interaction Prompt & Clickable Metro Fast Travel Menu
    /// Disable showDebugHud once your custom UGUI/UI Toolkit HUD is built.
    /// </summary>
    public class CombatHUDDebugOverlay : MonoBehaviour
    {
        [Header("Combat References (Auto-Found if Empty)")]
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private CombatController combatController;
        [SerializeField] private ChainAttackController chainAttackController;
        [SerializeField] private SoftLockOnSystem softLockOn;

        [Header("City References (Auto-Found if Empty)")]
        [SerializeField] private DayNightNeonController dayNightController;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private FastTravelManager fastTravelManager;

        [Header("Toggle")]
        [SerializeField] private bool showDebugHud = true;

        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bigBannerStyle;

        private void Awake()
        {
            if (squadManager == null) squadManager = FindFirstObjectByType<SquadManager>();
            if (combatController == null) combatController = FindFirstObjectByType<CombatController>();
            if (chainAttackController == null) chainAttackController = FindFirstObjectByType<ChainAttackController>();
            if (softLockOn == null) softLockOn = FindFirstObjectByType<SoftLockOnSystem>();
            if (dayNightController == null) dayNightController = FindFirstObjectByType<DayNightNeonController>();
            if (playerInteractor == null) playerInteractor = FindFirstObjectByType<PlayerInteractor>();
            if (fastTravelManager == null) fastTravelManager = FindFirstObjectByType<FastTravelManager>();
        }

        private void OnGUI()
        {
            if (!showDebugHud) return;
            EnsureStyles();

            DrawSquadHud();
            DrawDistrictClockAndInventory();
            DrawComboAndTargetHud();
            DrawTelegraphAndRiftStatus();
            DrawInteractionPrompt();
            DrawChainAttackPrompt();
            DrawFastTravelMenu();
        }

        private void DrawSquadHud()
        {
            if (squadManager == null) return;

            float y = 16f;
            for (int i = 0; i < squadManager.AgentCount; i++)
            {
                AgentRuntimeState st = squadManager.GetAgentState(i);
                if (st == null || st.Data == null) continue;

                bool isActive = i == squadManager.ActiveIndex;
                var rect = new Rect(16f, y, isActive ? 255f : 230f, 54f);
                GUI.Box(rect, GUIContent.none, _boxStyle);

                string prefix = isActive ? "▶ " : "  ";
                GUI.color = st.Data.accentColor;
                GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 20),
                    $"{prefix}[{i + 1}] {st.Data.agentName.ToUpper()} ({st.Data.role})", _titleStyle);
                GUI.color = Color.white;

                GUI.Label(new Rect(rect.x + 8, rect.y + 24, rect.width - 16, 18),
                    $"HP: {st.CurrentHP:F0}/{st.Data.maxHP:F0}   EN: {st.CurrentEnergy:F0}/{st.Data.maxEnergy:F0}");

                y += 60f;
            }

            GUI.Box(new Rect(16f, y, 255f, 34f), GUIContent.none, _boxStyle);
            string buffText = squadManager.HasTeamAttackBuff ? " [+25% ATK]" : "";
            GUI.Label(new Rect(24f, y + 7, 240f, 22),
                $"DECIBEL [Q]: {squadManager.Decibel:F0}%  PARRY: {squadManager.ParryCount}{buffText}");
        }

        private void DrawDistrictClockAndInventory()
        {
            bool hasClock = dayNightController != null;
            bool hasInv = CityEconomyInventory.Instance != null;
            if (!hasClock && !hasInv) return;

            var rect = new Rect(Screen.width * 0.5f - 175f, 12f, 350f, hasClock && hasInv ? 46f : 26f);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            float lineY = rect.y + 4f;
            if (hasClock)
            {
                GUI.Label(new Rect(rect.x + 8, lineY, rect.width - 16, 20),
                    $"DISTRICT 07 · {dayNightController.FormattedTime} ({dayNightController.CurrentPeriodName}) [F1/F2/F3]",
                    _bigBannerStyle);
                lineY += 20f;
            }

            if (hasInv)
            {
                var inv = CityEconomyInventory.Instance;
                GUI.Label(new Rect(rect.x + 8, lineY, rect.width - 16, 20),
                    $"Credits: {inv.Credits}  |  Cores: {inv.AnomalyCores}  |  Disks: {inv.DataDisks}",
                    _bigBannerStyle);
            }
        }

        private void DrawComboAndTargetHud()
        {
            if (combatController != null && combatController.ComboHitCount > 0)
            {
                GUI.Box(new Rect(Screen.width - 190f, 16f, 174f, 44f), GUIContent.none, _boxStyle);
                GUI.color = new Color(1f, 0.82f, 0.2f);
                GUI.Label(new Rect(Screen.width - 182f, 26f, 158f, 26f),
                    $"{combatController.ComboHitCount} HIT COMBO", _titleStyle);
                GUI.color = Color.white;
            }

            if (softLockOn != null && softLockOn.HasTarget)
            {
                var targetHealth = softLockOn.CurrentTarget.GetComponentInParent<HealthAndDazeComponent>();
                if (targetHealth != null)
                {
                    var tRect = new Rect(Screen.width - 260f, 70f, 244f, 54f);
                    GUI.Box(tRect, GUIContent.none, _boxStyle);
                    string status = targetHealth.IsStunned
                        ? $"STUNNED ({targetHealth.StunTimeRemaining:F1}s) ×1.5 DMG"
                        : $"DAZE: {targetHealth.NormalizedDaze * 100f:F0}%";
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 6, 228f, 20),
                        $"TARGET: {targetHealth.gameObject.name}", _titleStyle);
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 26, 228f, 20),
                        $"HP: {targetHealth.CurrentHP:F0}/{targetHealth.MaxHP:F0}  |  {status}");
                }
            }
        }

        private void DrawTelegraphAndRiftStatus()
        {
            if (AnomalyRiftZone.ActiveEncounter != null)
            {
                var rift = AnomalyRiftZone.ActiveEncounter;
                GUI.color = new Color(0.85f, 0.45f, 1f);
                GUI.Label(new Rect(Screen.width * 0.5f - 220f, 62f, 440f, 24f),
                    $"ANOMALY RIFT: {rift.RiftName} — Wave {rift.CurrentWaveNumber}/{rift.TotalWaves} (Sisa: {rift.AliveEnemyCount})",
                    _bigBannerStyle);
                GUI.color = Color.white;
            }

            TelegraphSystem activeTelegraph = TelegraphSystem.GetAnyActiveTelegraph();
            if (activeTelegraph != null)
            {
                bool isGold = activeTelegraph.ActiveType == TelegraphType.GoldenFlash;
                GUI.color = isGold ? new Color(1f, 0.8f, 0.15f) : new Color(1f, 0.2f, 0.32f);
                string msg = isGold
                    ? "★ GOLDEN FLASH! TEKAN [TAB / C / 1-3] UNTUK ASSIST PARRY! ★"
                    : "★ RED FLASH! TEKAN [KLIK KANAN / SHIFT] UNTUK PERFECT DODGE! ★";
                GUI.Label(new Rect(Screen.width * 0.5f - 280f, 88f, 560f, 32f), msg, _bigBannerStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawInteractionPrompt()
        {
            if (playerInteractor == null || !playerInteractor.HasInteractable) return;
            if (fastTravelManager != null && fastTravelManager.IsMenuOpen) return;

            var pRect = new Rect(Screen.width * 0.5f - 190f, Screen.height - 148f, 380f, 36f);
            GUI.Box(pRect, GUIContent.none, _boxStyle);
            GUI.color = new Color(0.25f, 0.95f, 0.9f);
            GUI.Label(new Rect(pRect.x + 8, pRect.y + 8, pRect.width - 16, 22),
                playerInteractor.CurrentInteractable.InteractionPrompt, _bigBannerStyle);
            GUI.color = Color.white;
        }

        private void DrawChainAttackPrompt()
        {
            if (chainAttackController == null || !chainAttackController.IsChainPromptOpen) return;

            var cRect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 96f, 520f, 64f);
            GUI.Box(cRect, GUIContent.none, _boxStyle);
            GUI.color = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(cRect.x, cRect.y + 10f, cRect.width, 24f),
                $"⚡ CHAIN ATTACK READY ({chainAttackController.PromptTimeRemaining:F1}s) ⚡", _bigBannerStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(cRect.x, cRect.y + 36f, cRect.width, 22f),
                "Tekan [TAB / C / 1 / 2 / 3 / Klik Kiri] untuk mengeksekusi Chain Attack!", _bigBannerStyle);
        }

        private void DrawFastTravelMenu()
        {
            if (fastTravelManager == null || !fastTravelManager.IsMenuOpen) return;

            var stops = fastTravelManager.AllStops;
            float panelH = 90f + stops.Count * 38f;
            var rect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - panelH * 0.5f, 360f, panelH);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            GUI.color = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 26f), "METRO FAST TRAVEL — DISTRICT 07", _bigBannerStyle);
            GUI.color = Color.white;

            float btnY = rect.y + 46f;
            for (int i = 0; i < stops.Count; i++)
            {
                MetroFastTravelStop stop = stops[i];
                if (stop == null) continue;

                bool isHere = stop == fastTravelManager.OriginStop;
                GUI.enabled = stop.IsUnlocked && !isHere;
                string label = !stop.IsUnlocked
                    ? $"{stop.StopDisplayName} (Terkunci)"
                    : (isHere ? $"{stop.StopDisplayName} (Lokasi Saat Ini)" : $"Teleport → {stop.StopDisplayName}");

                if (GUI.Button(new Rect(rect.x + 20f, btnY, rect.width - 40f, 30f), label))
                {
                    fastTravelManager.TeleportTo(stop);
                }
                GUI.enabled = true;
                btnY += 36f;
            }

            if (GUI.Button(new Rect(rect.x + 110f, rect.y + rect.height - 38f, 140f, 28f), "Tutup"))
            {
                fastTravelManager.CloseMenu();
            }
        }

        private void EnsureStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };
            }

            if (_bigBannerStyle == null)
            {
                _bigBannerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 13
                };
            }
        }
    }
}
