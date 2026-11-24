using System;
using UnityEngine;
using NeoVeil.Core;
using NeoVeil.Player;
using NeoVeil.Squad;

namespace NeoVeil.City
{
    /// <summary>
    /// Cafe 07 Hub Terminal (NTE/ZZZ home base station).
    /// Press [F] to Rest & Heal the full Squad, Upgrade Agent Levels using Credits + Anomaly Cores,
    /// or trigger manual Save/Load via SaveSystem!
    /// </summary>
    public class CafeHubTerminal : MonoBehaviour, IInteractable
    {
        public static CafeHubTerminal ActiveOpenTerminal { get; private set; }

        [Header("References (Auto-Found if Empty)")]
        [SerializeField] private SquadManager squadManager;
        [SerializeField] private PlayerMovementController playerMovement;

        [Header("Upgrade Cost Formula")]
        [SerializeField, Min(50)] private int baseCreditsPerLevel = 300;
        [SerializeField, Min(0)] private int baseCoresPerLevel = 2;

        public bool IsMenuOpen { get; private set; }
        public string InteractionPrompt => "[F] Cafe 07 Terminal — Rest, Upgrade Squad & Save";
        public bool CanInteract => !IsMenuOpen;
        public Transform InteractionTransform => transform;

        public event Action<string> OnCafeActionFeedback;

        private void Awake()
        {
            if (squadManager == null) squadManager = FindFirstObjectByType<SquadManager>();
            if (playerMovement == null) playerMovement = FindFirstObjectByType<PlayerMovementController>();
        }

        private void OnDestroy()
        {
            if (ActiveOpenTerminal == this) ActiveOpenTerminal = null;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            OpenCafeMenu();
        }

        public void OpenCafeMenu()
        {
            IsMenuOpen = true;
            ActiveOpenTerminal = this;
            if (playerMovement != null) playerMovement.SetMovementLocked(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void CloseCafeMenu()
        {
            IsMenuOpen = false;
            if (ActiveOpenTerminal == this) ActiveOpenTerminal = null;
            if (playerMovement != null) playerMovement.SetMovementLocked(false);
        }

        public int GetUpgradeCreditsCost(int currentLevel) => baseCreditsPerLevel * Mathf.Max(1, currentLevel);
        public int GetUpgradeCoresCost(int currentLevel) => baseCoresPerLevel * Mathf.Max(1, currentLevel);

        public void RestAndHealSquad()
        {
            if (squadManager == null) return;
            squadManager.HealAllAgentsToFull();
            Notify("Espresso Break! Seluruh Squad pulih 100% HP.");
        }

        public bool TryUpgradeAgent(int agentIndex)
        {
            if (squadManager == null || CityEconomyInventory.Instance == null) return false;

            AgentRuntimeState st = squadManager.GetAgentState(agentIndex);
            if (st == null || st.Data == null) return false;

            int credCost = GetUpgradeCreditsCost(st.Level);
            int coreCost = GetUpgradeCoresCost(st.Level);

            if (!CityEconomyInventory.Instance.TrySpendResources(credCost, coreCost))
            {
                Notify($"Resource kurang! Butuh {credCost} Credits & {coreCost} Cores.");
                return false;
            }

            squadManager.UpgradeAgentLevel(agentIndex);
            Notify($"{st.Data.agentName} naik ke Lv.{st.Level}! (ATK {st.EffectiveAttackPower:F0}, HP {st.EffectiveMaxHP:F0})");
            return true;
        }

        public void SaveFromCafe()
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SaveGame();
                Notify("Progres tersimpan ke neoveil_save.json!");
            }
            else
            {
                Notify("SaveSystem belum terpasang di scene!");
            }
        }

        public void LoadFromCafe()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.LoadGame())
            {
                CloseCafeMenu();
                Notify("Save data berhasil dimuat!");
            }
            else
            {
                Notify("Belum ada file save tersimpan.");
            }
        }

        private void Notify(string message)
        {
            Debug.Log($"[NeoVeil Cafe 07] {message}");
            OnCafeActionFeedback?.Invoke(message);
        }
    }
}
