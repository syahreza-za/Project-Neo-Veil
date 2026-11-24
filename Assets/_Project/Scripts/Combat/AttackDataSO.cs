using UnityEngine;

namespace NeoVeil.Combat
{
    /// <summary>
    /// Data-driven definition for a single ZZZ-style attack step (Basic Combo 1-4, Dodge Counter,
    /// EX Special, Ultimate, Defensive Assist Parry, or Chain Attack).
    /// Tweak all combat feel numbers in the Unity Inspector without touching C# code.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackData", menuName = "NeoVeil/Combat/Attack Data")]
    public class AttackDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string attackName = "Slash";
        public bool isHeavyAttack = false;
        public string animatorTrigger = "Attack";

        [Header("Damage & Daze")]
        [Min(0.1f)] public float damageMultiplier = 1.0f;
        [Min(0f)] public float baseDaze = 12f;
        [Min(0f)] public float energyGain = 7f;
        [Min(0f)] public float decibelGain = 4f;

        [Header("Hitbox Geometry")]
        [Min(0.5f)] public float range = 3.2f;
        [Range(30f, 360f)] public float hitArcDegrees = 140f;
        public bool isFullCircleAoE = false;

        [Header("Combat Feel (ZZZ Juice)")]
        [Min(0f)] public float lungeSpeed = 10f;
        [Min(0f)] public float lungeDuration = 0.09f;
        [Min(0.05f)] public float recoveryDuration = 0.18f;
        [Min(0f)] public float hitstopDuration = 0.03f;
        [Min(0f)] public float iFrameDuration = 0f;

        /// <summary>
        /// Creates a safe runtime fallback AttackDataSO so Fase 2 works immediately in greybox
        /// even before custom ScriptableObject assets are created in the Inspector.
        /// </summary>
        public static AttackDataSO CreateRuntimeDefault(
            string name,
            float dmgMult,
            float daze,
            float rangeVal,
            float recovery,
            float hitstop,
            bool heavy = false,
            bool aoe = false,
            float lungeSpd = 10f,
            float energy = 7f,
            float decibel = 4f,
            float iFrames = 0f)
        {
            var data = CreateInstance<AttackDataSO>();
            data.attackName = name;
            data.damageMultiplier = dmgMult;
            data.baseDaze = daze;
            data.range = rangeVal;
            data.recoveryDuration = recovery;
            data.hitstopDuration = hitstop;
            data.isHeavyAttack = heavy;
            data.isFullCircleAoE = aoe;
            data.hitArcDegrees = aoe ? 360f : (heavy ? 180f : 135f);
            data.lungeSpeed = lungeSpd;
            data.lungeDuration = lungeSpd > 0f ? 0.09f : 0f;
            data.energyGain = energy;
            data.decibelGain = decibel;
            data.iFrameDuration = iFrames;
            return data;
        }
    }
}
