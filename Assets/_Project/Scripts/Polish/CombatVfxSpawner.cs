using UnityEngine;
using NeoVeil.Combat;
using NeoVeil.Player;
using NeoVeil.Squad;

namespace NeoVeil.Polish
{
    /// <summary>
    /// Spawns anime combat VFX prefabs (slash trails, golden Parry sparks, cyan Perfect Dodge ripples,
    /// agent switch flashes, and Chain Attack bursts) with automatic fallback primitives for greybox scenes.
    /// </summary>
    public class CombatVfxSpawner : MonoBehaviour
    {
        [Header("Gameplay References (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private CombatController combatController;
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private ChainAttackController chainAttackController;

        [Header("Optional VFX Prefabs (ParticleSystem / VFX Graph)")]
        [SerializeField] private GameObject lightSlashVfxPrefab;
        [SerializeField] private GameObject heavySlashVfxPrefab;
        [SerializeField] private GameObject assistParryVfxPrefab;
        [SerializeField] private GameObject perfectDodgeVfxPrefab;
        [SerializeField] private GameObject switchFlashVfxPrefab;
        [SerializeField] private GameObject chainBurstVfxPrefab;

        [Header("Fallback & Lifetime")]
        [Tooltip("Spawns lightweight expanding neon rings when VFX prefabs are not assigned yet.")]
        [SerializeField] private bool useGreyboxFallbackFlash = true;
        [SerializeField, Min(0.2f)] private float defaultVfxLifetime = 1.2f;

        private void Awake()
        {
            if (movementController == null) movementController = GetComponent<PlayerMovementController>();
            if (combatController == null) combatController = GetComponent<CombatController>();
            if (squadManager == null) squadManager = GetComponent<SquadManager>();
            if (chainAttackController == null) chainAttackController = GetComponent<ChainAttackController>();
        }

        private void Start()
        {
            if (combatController != null) combatController.OnAttackPerformed += HandleAttackPerformed;
            if (movementController != null) movementController.OnPerfectDodgeTriggered += HandlePerfectDodge;
            if (squadManager != null)
            {
                squadManager.OnAgentSwitched += HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry += HandleAssistParry;
            }
            if (chainAttackController != null) chainAttackController.OnChainAttackExecuted += HandleChainAttack;
        }

        private void OnDestroy()
        {
            if (combatController != null) combatController.OnAttackPerformed -= HandleAttackPerformed;
            if (movementController != null) movementController.OnPerfectDodgeTriggered -= HandlePerfectDodge;
            if (squadManager != null)
            {
                squadManager.OnAgentSwitched -= HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry -= HandleAssistParry;
            }
            if (chainAttackController != null) chainAttackController.OnChainAttackExecuted -= HandleChainAttack;
        }

        private void HandleAttackPerformed(AttackDataSO attack, int _)
        {
            bool heavy = attack != null && attack.isHeavyAttack;
            Vector3 pos = transform.position + transform.forward * 1.35f + Vector3.up * 1.1f;
            Color col = squadManager != null && squadManager.ActiveAgent != null
                ? squadManager.ActiveAgent.accentColor
                : Color.cyan;

            SpawnOrFallback(heavy ? heavySlashVfxPrefab : lightSlashVfxPrefab, pos, transform.rotation, col, heavy ? 2.4f : 1.5f);
        }

        private void HandlePerfectDodge()
        {
            SpawnOrFallback(perfectDodgeVfxPrefab, transform.position + Vector3.up * 0.25f, Quaternion.identity, Color.cyan, 2.8f);
        }

        private void HandleAgentSwitched(int _, AgentDataSO agent)
        {
            Color col = agent != null ? agent.accentColor : Color.white;
            SpawnOrFallback(switchFlashVfxPrefab, transform.position + Vector3.up * 1.0f, Quaternion.identity, col, 1.8f);
        }

        private void HandleAssistParry(AgentDataSO _, HealthAndDazeComponent enemy)
        {
            Vector3 mid = enemy != null
                ? Vector3.Lerp(transform.position, enemy.transform.position, 0.5f) + Vector3.up * 1.1f
                : transform.position + transform.forward * 1.5f + Vector3.up * 1.1f;
            SpawnOrFallback(assistParryVfxPrefab, mid, transform.rotation, new Color(1f, 0.8f, 0.15f), 3.6f);
        }

        private void HandleChainAttack(AgentDataSO agent, HealthAndDazeComponent _)
        {
            Color col = agent != null ? agent.accentColor : Color.magenta;
            SpawnOrFallback(chainBurstVfxPrefab, transform.position + Vector3.up * 0.4f, Quaternion.identity, col, 4.5f);
        }

        private void SpawnOrFallback(GameObject prefab, Vector3 pos, Quaternion rot, Color fallbackColor, float scale)
        {
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, pos, rot);
                Destroy(instance, defaultVfxLifetime);
                return;
            }

            if (!useGreyboxFallbackFlash) return;

            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "VfxFallbackPulse";
            Destroy(flash.GetComponent<Collider>());
            flash.transform.SetPositionAndRotation(pos, rot);
            flash.transform.localScale = Vector3.one * (scale * 0.35f);

            var rend = flash.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = fallbackColor;
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", fallbackColor * 2.5f);
                }
            }

            Destroy(flash, 0.14f);
        }
    }
}
