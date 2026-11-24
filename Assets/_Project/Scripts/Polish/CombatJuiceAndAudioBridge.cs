using UnityEngine;
using UnityEngine.Events;
using NeoVeil.City;
using NeoVeil.Combat;
using NeoVeil.Player;
using NeoVeil.Quest;
using NeoVeil.Squad;

namespace NeoVeil.Polish
{
    /// <summary>
    /// Connects Fase 1-5 gameplay events to Anime Combat SFX, Dynamic Urban/Battle BGM crossfading,
    /// and Camera Shake (via CinemachineImpulseSource UnityEvent or fallback camera trauma).
    /// </summary>
    public class CombatJuiceAndAudioBridge : MonoBehaviour
    {
        [Header("Gameplay References (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController movementController;
        [SerializeField] private CombatController combatController;
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private ChainAttackController chainAttackController;

        [Header("Audio Sources (Auto-Created if Empty)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource cityBgmSource;
        [SerializeField] private AudioSource battleBgmSource;
        [SerializeField, Range(0f, 1f)] private float bgmMaxVolume = 0.45f;
        [SerializeField, Min(0.2f)] private float bgmCrossfadeSpeed = 2.0f;

        [Header("Combat & City SFX Clips (Optional)")]
        [SerializeField] private AudioClip[] lightSlashClips;
        [SerializeField] private AudioClip[] heavySlashClips;
        [SerializeField] private AudioClip dodgeWhooshClip;
        [SerializeField] private AudioClip perfectDodgeClip;
        [SerializeField] private AudioClip assistParryClangClip;
        [SerializeField] private AudioClip switchAgentClip;
        [SerializeField] private AudioClip chainAttackClip;
        [SerializeField] private AudioClip missionClearClip;

        [Header("BGM Clips (Optional)")]
        [SerializeField] private AudioClip cityExploreBgm;
        [SerializeField] private AudioClip riftBattleBgm;

        [Header("Camera Shake Bridge")]
        [Tooltip("Wire CinemachineImpulseSource.GenerateImpulseWithForce here in Unity 6 Inspector!")]
        [SerializeField] private UnityEvent<float> onCameraShakeRequested;

        private void Awake()
        {
            if (movementController == null) movementController = FindFirstObjectByType<PlayerMovementController>();
            if (combatController == null) combatController = FindFirstObjectByType<CombatController>();
            if (squadManager == null) squadManager = FindFirstObjectByType<SquadManager>();
            if (chainAttackController == null) chainAttackController = FindFirstObjectByType<ChainAttackController>();

            EnsureAudioSources();
        }

        private void Start()
        {
            if (movementController != null)
            {
                movementController.OnDodgeStarted += HandleDodgeStarted;
                movementController.OnPerfectDodgeTriggered += HandlePerfectDodge;
            }

            if (combatController != null)
            {
                combatController.OnAttackPerformed += HandleAttackPerformed;
            }

            if (squadManager != null)
            {
                squadManager.OnAgentSwitched += HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry += HandleAssistParry;
            }

            if (chainAttackController != null)
            {
                chainAttackController.OnChainAttackExecuted += HandleChainExecuted;
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
            }

            StartBgmTracks();
        }

        private void OnDestroy()
        {
            if (movementController != null)
            {
                movementController.OnDodgeStarted -= HandleDodgeStarted;
                movementController.OnPerfectDodgeTriggered -= HandlePerfectDodge;
            }

            if (combatController != null)
            {
                combatController.OnAttackPerformed -= HandleAttackPerformed;
            }

            if (squadManager != null)
            {
                squadManager.OnAgentSwitched -= HandleAgentSwitched;
                squadManager.OnDefensiveAssistParry -= HandleAssistParry;
            }

            if (chainAttackController != null)
            {
                chainAttackController.OnChainAttackExecuted -= HandleChainExecuted;
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        private void Update()
        {
            UpdateDynamicBgmCrossfade();
        }

        private void HandleAttackPerformed(AttackDataSO attack, int comboStep)
        {
            bool heavy = attack != null && attack.isHeavyAttack;
            PlayRandomClip(heavy ? heavySlashClips : lightSlashClips, heavy ? 0.95f : 0.72f, 0.92f, 1.12f);
            TriggerShake(heavy ? 0.45f : 0.16f);
        }

        private void HandleDodgeStarted()
        {
            PlayClip(dodgeWhooshClip, 0.65f, 0.95f, 1.08f);
        }

        private void HandlePerfectDodge()
        {
            PlayClip(perfectDodgeClip, 1.0f, 1.0f, 1.0f);
            TriggerShake(0.35f);
        }

        private void HandleAgentSwitched(int _, AgentDataSO __)
        {
            PlayClip(switchAgentClip, 0.6f, 0.96f, 1.06f);
        }

        private void HandleAssistParry(AgentDataSO _, HealthAndDazeComponent __)
        {
            PlayClip(assistParryClangClip, 1.0f, 0.98f, 1.04f);
            TriggerShake(0.95f);
        }

        private void HandleChainExecuted(AgentDataSO _, HealthAndDazeComponent __)
        {
            PlayClip(chainAttackClip, 1.0f, 1.0f, 1.0f);
            TriggerShake(0.85f);
        }

        private void HandleMissionCompleted(MissionDataSO _)
        {
            PlayClip(missionClearClip, 0.9f, 1.0f, 1.0f);
        }

        private void TriggerShake(float force)
        {
            onCameraShakeRequested?.Invoke(force);
        }

        private void PlayRandomClip(AudioClip[] clips, float volume, float minPitch, float maxPitch)
        {
            if (clips == null || clips.Length == 0) return;
            AudioClip chosen = clips[Random.Range(0, clips.Length)];
            PlayClip(chosen, volume, minPitch, maxPitch);
        }

        private void PlayClip(AudioClip clip, float volume, float minPitch, float maxPitch)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = Random.Range(minPitch, maxPitch);
            sfxSource.PlayOneShot(clip, volume);
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
            }

            if (cityBgmSource == null && cityExploreBgm != null)
            {
                cityBgmSource = gameObject.AddComponent<AudioSource>();
                cityBgmSource.loop = true;
                cityBgmSource.playOnAwake = false;
            }

            if (battleBgmSource == null && riftBattleBgm != null)
            {
                battleBgmSource = gameObject.AddComponent<AudioSource>();
                battleBgmSource.loop = true;
                battleBgmSource.playOnAwake = false;
            }
        }

        private void StartBgmTracks()
        {
            if (cityBgmSource != null && cityExploreBgm != null)
            {
                cityBgmSource.clip = cityExploreBgm;
                cityBgmSource.volume = bgmMaxVolume;
                cityBgmSource.Play();
            }

            if (battleBgmSource != null && riftBattleBgm != null)
            {
                battleBgmSource.clip = riftBattleBgm;
                battleBgmSource.volume = 0f;
                battleBgmSource.Play();
            }
        }

        private void UpdateDynamicBgmCrossfade()
        {
            bool inBattle = AnomalyRiftZone.ActiveEncounter != null;
            float dt = Time.unscaledDeltaTime * bgmCrossfadeSpeed;

            if (cityBgmSource != null && cityBgmSource.isPlaying)
            {
                float target = inBattle ? 0f : bgmMaxVolume;
                cityBgmSource.volume = Mathf.MoveTowards(cityBgmSource.volume, target, dt);
            }

            if (battleBgmSource != null && battleBgmSource.isPlaying)
            {
                float target = inBattle ? bgmMaxVolume : 0f;
                battleBgmSource.volume = Mathf.MoveTowards(battleBgmSource.volume, target, dt);
            }
        }
    }
}
