using UnityEngine;
using NeoVeil.AI;
using NeoVeil.Combat;
using NeoVeil.Squad;

namespace NeoVeil.UI
{
    /// <summary>
    /// Plug-and-play ZZZ Combat HUD overlay for Fase 2 & 3 greybox testing.
    /// Displays 3-Agent Squad HP/Energy, Decibel Gauge, Combo Counter, Golden/Red Telegraph warnings,
    /// and Chain Attack prompts immediately without needing a Canvas setup first.
    /// Disable showDebugHud once your custom UGUI/UI Toolkit HUD is ready.
    /// </summary>
    public class CombatHUDDebugOverlay : MonoBehaviour
    {
        [Header("References (Auto-Found if Empty)")]
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private CombatController combatController;
        [SerializeField] private ChainAttackController chainAttackController;
        [SerializeField] private SoftLockOnSystem softLockOn;

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
        }

        private void OnGUI()
        {
            if (!showDebugHud || squadManager == null) return;
            EnsureStyles();

            // 1. Top-Left: 3-Agent Squad HP, Energy & Decibel
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
            string buffText = squadManager.HasTeamAttackBuff ? "  [+25% ATK]" : "";
            GUI.Label(new Rect(24f, y + 7, 240f, 22),
                $"DECIBEL [Q]: {squadManager.Decibel:F0}%   PARRY: {squadManager.ParryCount}{buffText}");

            // 2. Top-Right: Combo Counter & Soft Lock-On Target HP/Daze
            if (combatController != null && combatController.ComboHitCount > 0)
            {
                GUI.Box(new Rect(Screen.width - 190f, 16f, 174f, 48f), GUIContent.none, _boxStyle);
                GUI.color = new Color(1f, 0.82f, 0.2f);
                GUI.Label(new Rect(Screen.width - 182f, 24f, 158f, 32f),
                    $"{combatController.ComboHitCount} HIT COMBO", _titleStyle);
                GUI.color = Color.white;
            }

            if (softLockOn != null && softLockOn.HasTarget)
            {
                var targetHealth = softLockOn.CurrentTarget.GetComponentInParent<HealthAndDazeComponent>();
                if (targetHealth != null)
                {
                    var tRect = new Rect(Screen.width - 260f, 74f, 244f, 54f);
                    GUI.Box(tRect, GUIContent.none, _boxStyle);
                    string status = targetHealth.IsStunned
                        ? $"STUNNED ({targetHealth.StunTimeRemaining:F1}s) ×1.5 DMG"
                        : $"DAZE: {targetHealth.NormalizedDaze * 100f:F0}%";
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 6, 228f, 20f),
                        $"TARGET: {targetHealth.gameObject.name}", _titleStyle);
                    GUI.Label(new Rect(tRect.x + 8, tRect.y + 26, 228f, 20f),
                        $"HP: {targetHealth.CurrentHP:F0}/{targetHealth.MaxHP:F0}  |  {status}");
                }
            }

            // 3. Top-Center: Active Telegraph Alert (Golden vs Red Flash)
            TelegraphSystem activeTelegraph = TelegraphSystem.GetAnyActiveTelegraph();
            if (activeTelegraph != null)
            {
                bool isGold = activeTelegraph.ActiveType == TelegraphType.GoldenFlash;
                GUI.color = isGold ? new Color(1f, 0.8f, 0.15f) : new Color(1f, 0.2f, 0.32f);
                string msg = isGold
                    ? "★ GOLDEN FLASH! TEKAN [TAB / C / 1-3] UNTUK ASSIST PARRY! ★"
                    : "★ RED FLASH! TEKAN [KLIK KANAN / SHIFT] UNTUK PERFECT DODGE! ★";
                GUI.Label(new Rect(Screen.width * 0.5f - 280f, 18f, 560f, 36f), msg, _bigBannerStyle);
                GUI.color = Color.white;
            }

            // 4. Bottom-Center: Chain Attack Slow-Mo Prompt
            if (chainAttackController != null && chainAttackController.IsChainPromptOpen)
            {
                var cRect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 96f, 520f, 64f);
                GUI.Box(cRect, GUIContent.none, _boxStyle);
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(cRect.x, cRect.y + 10f, cRect.width, 24f),
                    $"⚡ CHAIN ATTACK READY ({chainAttackController.PromptTimeRemaining:F1}s) ⚡", _bigBannerStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(cRect.x, cRect.y + 36f, cRect.width, 22f),
                    "Tekan [TAB / C / 1 / 2 / 3 / Klik Kiri] untuk mengeksekusi Chain Attack!", _bigBannerStyle);
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
                    fontSize = 15
                };
            }
        }
    }
}
