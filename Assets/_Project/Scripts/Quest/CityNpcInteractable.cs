using UnityEngine;
using NeoVeil.City;

namespace NeoVeil.Quest
{
    /// <summary>
    /// Interactive District 07 / Cafe 07 NPC (e.g., Barista Ren, Fixer Mei).
    /// Press [F] to start dialogue, optionally unlock a MissionDataSO contract,
    /// and advance TalkToNpc mission objectives!
    /// </summary>
    public class CityNpcInteractable : MonoBehaviour, IInteractable
    {
        [Header("NPC Identity")]
        [SerializeField] private string npcId = "npc_barista_ren";
        [SerializeField] private string npcDisplayName = "Barista Ren";
        [SerializeField] private Color speakerColor = new Color(0.25f, 0.9f, 1f);

        [Header("Dialogue & Optional Contract")]
        [Tooltip("Leave empty to use built-in Cafe 07 greeting lines.")]
        [SerializeField] private DialogueDataSO dialogueData;
        [SerializeField] private MissionDataSO missionToGrant;

        [Header("Optional Cafe Terminal Link")]
        [Tooltip("If assigned, finishing this NPC's dialogue automatically opens the Cafe 07 Hub Terminal.")]
        [SerializeField] private CafeHubTerminal linkedCafeTerminal;

        private DialogueDataSO _fallbackDialogue;

        public string InteractionPrompt => $"[F] Bicara dengan {npcDisplayName}";
        public bool CanInteract => DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueOpen;
        public Transform InteractionTransform => transform;

        private void Awake()
        {
            if (dialogueData == null)
            {
                _fallbackDialogue = DialogueDataSO.CreateRuntimeSample(
                    $"fallback_{npcId}",
                    npcDisplayName,
                    speakerColor,
                    "Selamat datang di Cafe 07, Proxy. Anomali di distrik malam ini mulai meningkat.",
                    "Perhatikan kilatan emas musuh untuk melakukan Defensive Assist Parry, lalu habisi saat Daze penuh!",
                    "Gunakan Terminal Kafe di sebelah untuk memulihkan Squad, upgrade Agen, atau menyimpan progresmu."
                );
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;

            DialogueDataSO toPlay = dialogueData != null ? dialogueData : _fallbackDialogue;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(toPlay, OnDialogueFinished);
            }
            else
            {
                Debug.Log($"[{npcDisplayName}] {toPlay.lines[0].text}");
                OnDialogueFinished();
            }
        }

        private void OnDialogueFinished()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.NotifyNpcTalked(npcId);
                if (missionToGrant != null)
                {
                    MissionManager.Instance.AddMission(missionToGrant);
                }
            }

            if (linkedCafeTerminal != null)
            {
                linkedCafeTerminal.OpenCafeMenu();
            }
        }
    }
}
