using System;
using UnityEngine;

namespace NeoVeil.City
{
    /// <summary>
    /// Collectible Urban Anomaly Crate (Chest) found in alleys, rooftops, or unlocked after clearing an AnomalyRiftZone.
    /// Ready for Fase 5 Save/Load JSON via unique crateId.
    /// </summary>
    public class AnomalyCrate : MonoBehaviour, IInteractable
    {
        [Header("Identity & State")]
        [SerializeField] private string crateId = "district07_crate_01";
        [SerializeField] private bool startLocked = false;

        [Header("Loot Rewards")]
        [SerializeField, Min(0)] private int rewardCredits = 250;
        [SerializeField, Min(0)] private int rewardAnomalyCores = 2;
        [SerializeField, Min(0)] private int rewardDataDisks = 1;

        [Header("Optional Visuals")]
        [SerializeField] private Renderer crateRenderer;
        [SerializeField] private Transform lidTransform;
        [SerializeField] private Color lockedColor = new Color(0.9f, 0.15f, 0.35f);
        [SerializeField] private Color unlockedColor = new Color(0.15f, 0.9f, 0.95f);
        [SerializeField] private Color openedColor = new Color(0.3f, 0.32f, 0.38f);

        private Material _runtimeMat;

        public string CrateId => crateId;
        public bool IsLocked { get; private set; }
        public bool IsOpened { get; private set; }

        public string InteractionPrompt => IsLocked
            ? "Terkunci — Stabilkan Anomaly Rift terdekat!"
            : "[F] Buka Anomaly Crate";

        public bool CanInteract => !IsOpened && !IsLocked;
        public Transform InteractionTransform => transform;

        public event Action<AnomalyCrate> OnOpened;

        private void Awake()
        {
            IsLocked = startLocked;
            if (crateRenderer == null) crateRenderer = GetComponentInChildren<Renderer>();
            if (crateRenderer != null) _runtimeMat = crateRenderer.material;
            RefreshVisual();
        }

        public void SetLocked(bool locked)
        {
            if (IsOpened) return;
            IsLocked = locked;
            RefreshVisual();
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;

            IsOpened = true;
            if (lidTransform != null)
            {
                lidTransform.localRotation = Quaternion.Euler(-75f, 0f, 0f);
            }
            RefreshVisual();

            if (CityEconomyInventory.Instance != null)
            {
                CityEconomyInventory.Instance.AddReward(
                    rewardCredits,
                    rewardAnomalyCores,
                    rewardDataDisks,
                    "Anomaly Crate"
                );
            }

            OnOpened?.Invoke(this);
        }

        public void MarkOpenedFromSave()
        {
            IsLocked = false;
            IsOpened = true;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_runtimeMat == null) return;
            _runtimeMat.color = IsOpened ? openedColor : (IsLocked ? lockedColor : unlockedColor);
        }
    }
}
