using UnityEngine;
using NeoVeil.Combat;

namespace NeoVeil.Squad
{
    /// <summary>
    /// Data-driven definition for a playable Squad Agent (Striker, Breaker, or Channeler).
    /// Create 3 assets in Assets/_Project/ScriptableObjects/Agents/ and assign them to SquadManager.
    /// </summary>
    [CreateAssetMenu(fileName = "AgentData", menuName = "NeoVeil/Squad/Agent Data")]
    public class AgentDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string agentName = "Kira";
        public AgentRole role = AgentRole.Striker;
        public Color accentColor = Color.cyan;

        [Header("Optional Visual Prefab")]
        [Tooltip("Optional 3D model prefab (e.g., VRoid character) spawned under SquadManager visual anchor.")]
        public GameObject visualPrefab;

        [Header("Core Combat Stats")]
        [Min(100f)] public float maxHP = 1000f;
        [Min(1f)] public float attackPower = 75f;
        [Min(0.1f)] public float dazeMultiplier = 1.0f;
        [Min(1f)] public float stunDamageMultiplier = 1.5f;

        [Header("Energy & EX Special")]
        [Min(10f)] public float maxEnergy = 100f;
        [Min(0f)] public float startingEnergy = 50f;
        [Min(10f)] public float exSpecialEnergyCost = 50f;

        [Header("Attack Moveset (Optional — Auto-Falls Back to ZZZ Defaults if Empty)")]
        public AttackDataSO[] basicCombo;
        public AttackDataSO dodgeCounterAttack;
        public AttackDataSO exSpecialAttack;
        public AttackDataSO ultimateAttack;
        public AttackDataSO assistParryAttack;
        public AttackDataSO chainAttack;

        public static AgentDataSO CreateRuntimePreset(string name, AgentRole presetRole, Color color, float hp, float atk, float dazeMult, float stunDmgMult)
        {
            var agent = CreateInstance<AgentDataSO>();
            agent.agentName = name;
            agent.role = presetRole;
            agent.accentColor = color;
            agent.maxHP = hp;
            agent.attackPower = atk;
            agent.dazeMultiplier = dazeMult;
            agent.stunDamageMultiplier = stunDmgMult;
            agent.startingEnergy = 50f;
            return agent;
        }
    }
}
