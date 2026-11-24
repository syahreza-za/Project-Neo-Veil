using UnityEngine;
using NeoVeil.AI;
using NeoVeil.City;
using NeoVeil.Combat;
using NeoVeil.Core;
using NeoVeil.Quest;
using NeoVeil.Squad;

namespace NeoVeil.UI
{
    /// <summary>
    /// Plug-and-play ZZZ + NTE HUD overlay for greybox testing (Fase 2, 3, 4 & 5):
    /// - 3-Agent Squad HP/Energy/Level & Decibel Gauge
    /// - District Clock (Day/Dusk/Neon Night) & Urban Inventory (Credits/Cores/Disks)
    /// - Active Contract / Mission Tracker
    /// - Active Anomaly Rift Wave tracker, Golden/Red Telegraph alert & Chain Attack prompt
    /// - [F] Interaction Prompt, Anime Dialogue Box, Metro Fast Travel Menu & Cafe 07 Hub Terminal
    /// Disable showDebugHud once your custom UGUI/UI Toolkit HUD is built.
    /// </summary>
    public class CombatHUDDebugOverlay : MonoBehaviour
    {
        [Header("Combat References (Auto-Found if Empty)")]
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private CombatController combatController;
        [SerializeField] private ChainAttackController chainAttackController;
        [SerializeField] private SoftLockOnSystem softLockOn;

        [Header("City & Quest References (Auto-Found if Empty)")]
        [SerializeField] private DayNightNeonController dayNightController;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private FastTravelManager fastTravelManager;

        [Header("Toggle")]
        [SerializeField] private bool showDebugHud = true;

        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bigBannerStyle;
        private GUIStyle _dialogueTextStyle;

        private string _toastText;
        private float _toastTimer;

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

        private void Start()
        {
            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.OnRewardNotification += ShowToast;
            }
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.OnSaveStatusMessage += ShowToast;
            }
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
            }
        }

        private void OnDestroy()
        {
            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.OnRewardNotification -= ShowToast;
            }
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.OnSaveStatusMessage -= ShowToast;
            }
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        private void Update()
        {
            if (_toastTimer > 0f) _toastTimer -= Time.unscaledDeltaTime;
        }

        private void HandleMissionCompleted(MissionDataSO mission)
        {
            if (mission != null) ShowToast($"★ CONTRACT CLEAR: {mission.title}!");
        }

        private void ShowToast(string msg)
        {
            _toastText = msg;
            _toastTimer = 2.8f;
        }

        private void OnGUI()
        {
            if (!showDebugHud) return;
            EnsureStyles();

            DrawSquadHud();
            DrawDistrictClockAndInventory();
            DrawComboTargetAndMissions();
            DrawTelegraphAndRiftStatus();
            DrawToastBanner();
            DrawInteractionPrompt();
            DrawChainAttackPrompt();
            DrawDialogueBox();
            DrawFastTravelMenu();
            DrawCafeHubMenu();
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
                var rect = new Rect(16f, y, isActive ? 268f : 242f, 54f);
                GUI.Box(rect, GUIContent.none, _boxStyle);

                string prefix = isActive ? "▶ " : "  ";
                GUI.color = st.Data.accentColor;
                GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 20),
                    $"{prefix}[{i + 1}] {st.Data.agentName.ToUpper()} Lv.{st.Level} ({st.Data.role})", _titleStyle);
                GUI.color = Color.white;

                GUI.Label(new Rect(rect.x + 8, rect.y + 24, rect.width - 16, 18),
                    $"HP: {st.CurrentHP:F0}/{st.EffectiveMaxHP:F0}   EN: {st.CurrentEnergy:F0}/{st.Data.maxEnergy:F0}");

                y += 60f;
            }

            GUI.Box(new Rect(16f, y, 268f, 34f), GUIContent.none, _boxStyle);
            string buffText = squadManager.HasTeamAttackBuff ? " [+25% ATK]" : "";
            GUI.Label(new Rect(24f, y + 7, 252f, 22),
                $"DECIBEL [Q]: {squadManager.Decibel:F0}%  PARRY: {squadManager.ParryCount}{buffText}");
        }

        private void DrawDistrictClockAndInventory()
        {
            bool hasClock = dayNightController != null;
            bool hasInv = CityEconomyInventory.Instance != null;
            if (!hasClock && !hasInv) return;

            var rect = new Rect(Screen.width * 0.5f - 190f, 12f, 380f, hasClock && hasInv ? 46f : 26f);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            float lineY = rect.y + 4f;
            if (hasClock)
            {
                GUI.Label(new Rect(rect.x + 8, lineY, rect.width - 16, 20),
                    $"DISTRICT 07 · {dayNightController.FormattedTime} ({dayNightController.CurrentPeriodName}) [F1-F3]",
                    _bigBannerStyle);
                lineY += 20f;
            }

            if (hasInv)
            {
                var inv = CityEconomyInventory.Instance;
                GUI.Label(new Rect(rect.x + 8, lineY, rect.width - 16, 20),
                    $"Credits: {inv.Credits} | Cores: {inv.AnomalyCores} | Disks: {inv.DataDisks}  [F5 Save/F9 Load]",
                    _bigBannerStyle);
            }
        }

        private void DrawComboTargetAndMissions()
        {
            float rightY = 16f;

            if (combatController != null && combatController.ComboHitCount > 0)
            {
                GUI.Box(new Rect(Screen.width - 260f, rightY, 244f, 40f), GUIContent.none, _boxStyle);
                GUI.color = new Color(1f, 0.82f, 0.2f);
                GUI.Label(new Rect(Screen.width - 252f, rightY + 10f, 228f, 24f),
                    $"{combatController.ComboHitCount} HIT COMBO", _titleStyle);
                GUI.color = Color.white;
                rightY += 48f;
            }

            if (softLockOn != null && softLockOn.HasTarget)
            {
                var targetHealth = softLockOn.CurrentTarget.GetComponentInParent<HealthAndDazeComponent>();
                if (targetHealth != null)
                {
                    var tRect = new Rect(Screen.width - 260f, rightY, 244f, 54f);
                    GUI.Box(tRect, GUIContent.none, _boxStyle);
                    string status = targetHealth.IsStunned
                        ? $"STUNNED ({targetHealth.StunTimeRemaining:F1}s) ×1.5 DMG"
                        : $"DAZE: {targetHealth.NormalizedDaze * 100f:F0}%";
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 6, 228f, 20),
                        $"TARGET: {targetHealth.gameObject.name}", _titleStyle);
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 26, 228f, 20),
                        $"HP: {targetHealth.CurrentHP:F0}/{targetHealth.MaxHP:F0}  |  {status}");
                    rightY += 62f;
                }
            }

            if (MissionManager.Instance != null && MissionManager.Instance.Missions.Count > 0)
            {
                var missions = MissionManager.Instance.Missions;
                int shown = Mathf.Min(3, missions.Count);
                var mRect = new Rect(Screen.width - 260f, rightY, 244f, 28f + shown * 22f);
                GUI.Box(mRect, GUIContent.none, _boxStyle);

                GUI.color = new Color(0.25f, 0.9f, 1f);
                GUI.Label(new Rect(mRect.x + 8, mRect.y + 4, 228f, 20), "ACTIVE CONTRACTS", _titleStyle);
                GUI.color = Color.white;

                float my = mRect.y + 24f;
                for (int i = 0; i < shown; i++)
                {
                    MissionRuntimeState m = missions[i];
                    GUI.color = m.IsCompleted ? new Color(0.3f, 0.95f, 0.55f) : Color.white;
                    string prefix = m.IsCompleted ? "✓ " : "• ";
                    GUI.Label(new Rect(mRect.x + 8, my, 228f, 20),
                        $"{prefix}{m.Data.title} ({m.ProgressLabel})");
                    my += 22f;
                }
                GUI.color = Color.white;
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

        private void DrawToastBanner()
        {
            if (_toastTimer <= 0f || string.IsNullOrEmpty(_toastText)) return;
            var rect = new Rect(Screen.width * 0.5f - 230f, 122f, 460f, 30f);
            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.color = new Color(0.35f, 1f, 0.72f);
            GUI.Label(new Rect(rect.x + 8, rect.y + 5, rect.width - 16, 22), _toastText, _bigBannerStyle);
            GUI.color = Color.white;
        }

        private void DrawInteractionPrompt()
        {
            if (playerInteractor == null || !playerInteractor.HasInteractable) return;
            if (IsAnyModalOpen()) return;

            var pRect = new Rect(Screen.width * 0.5f - 200f, Screen.height - 148f, 400f, 36f);
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

        private void DrawDialogueBox()
        {
            if (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueOpen) return;

            DialogueLine line = DialogueManager.Instance.CurrentLine;
            var rect = new Rect(Screen.width * 0.5f - 310f, Screen.height - 150f, 620f, 124f);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            GUI.color = line.speakerColor.a > 0.05f ? line.speakerColor : new Color(0.25f, 0.9f, 1f);
            GUI.Label(new Rect(rect.x + 16, rect.y + 10, 420f, 24f),
                $"{line.speakerName}  ({DialogueManager.Instance.CurrentLineNumber}/{DialogueManager.Instance.TotalLines})",
                _titleStyle);
            GUI.color = Color.white;

            GUI.Label(new Rect(rect.x + 16, rect.y + 38, rect.width - 32, 52f), line.text, _dialogueTextStyle);

            if (GUI.Button(new Rect(rect.x + rect.width - 156f, rect.y + rect.height - 34f, 140f, 24f), "Lanjut [Space]"))
            {
                DialogueManager.Instance.AdvanceLine();
            }
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

        private void DrawCafeHubMenu()
        {
            CafeHubTerminal cafe = CafeHubTerminal.ActiveOpenTerminal;
            if (cafe == null || !cafe.IsMenuOpen || squadManager == null) return;

            float panelH = 190f + squadManager.AgentCount * 42f;
            var rect = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - panelH * 0.5f, 460f, panelH);
            GUI.Box(rect, GUIContent.none, _boxStyle);

            GUI.color = new Color(1f, 0.8f, 0.25f);
            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 26f), "CAFE 07 HUB TERMINAL — SQUAD & SAVE", _bigBannerStyle);
            GUI.color = Color.white;

            float y = rect.y + 46f;
            if (GUI.Button(new Rect(rect.x + 20f, y, rect.width - 40f, 30f), "☕ Rest & Espresso Boost (Pulihkan 100% HP Squad)"))
            {
                cafe.RestAndHealSquad();
                ShowToast("Seluruh Squad pulih 100% HP!");
            }
            y += 40f;

            for (int i = 0; i < squadManager.AgentCount; i++)
            {
                AgentRuntimeState st = squadManager.GetAgentState(i);
                if (st == null || st.Data == null) continue;

                int credCost = cafe.GetUpgradeCreditsCost(st.Level);
                int coreCost = cafe.GetUpgradeCoresCost(st.Level);
                string btnLabel = $"Upgrade {st.Data.agentName} (Lv.{st.Level} → Lv.{st.Level + 1})  [{credCost} Cr, {coreCost} Core]";
                if (GUI.Button(new Rect(rect.x + 20f, y, rect.width - 40f, 32f), btnLabel))
                {
                    if (cafe.TryUpgradeAgent(i))
                    {
                        ShowToast($"{st.Data.agentName} naik ke Lv.{st.Level}!");
                    }
                    else
                    {
                        ShowToast($"Butuh {credCost} Credits & {coreCost} Cores!");
                    }
                }
                y += 40f;
            }

            y += 6f;
            float halfW = (rect.width - 50f) * 0.5f;
            if (GUI.Button(new Rect(rect.x + 20f, y, halfW, 30f), "💾 Save Game [F5]"))
            {
                cafe.SaveFromCafe();
            }
            if (GUI.Button(new Rect(rect.x + 30f + halfW, y, halfW, 30f), "📂 Load Game [F9]"))
            {
                cafe.LoadFromCafe();
            }

            if (GUI.Button(new Rect(rect.x + 150f, rect.y + rect.height - 38f, 160f, 28f), "Tutup Terminal"))
            {
                cafe.CloseCafeMenu();
            }
        }

        private bool IsAnyModalOpen()
        {
            if (fastTravelManager != null && fastTravelManager.IsMenuOpen) return true;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueOpen) return true;
            if (CafeHubTerminal.ActiveOpenTerminal != null && CafeHubTerminal.ActiveOpenTerminal.IsMenuOpen) return true;
            return false;
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

            if (_dialogueTextStyle == null)
            {
                _dialogueTextStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 14
                };
            }
        }
    }
}
