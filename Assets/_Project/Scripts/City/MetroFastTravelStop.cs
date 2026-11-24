using UnityEngine;

namespace NeoVeil.City
{
    /// <summary>
    /// Urban Bus Stop / Metro Station Fast Travel node (e.g., Neon Plaza, Cafe 07 Hub, Metro West).
    /// Interact with [F] to unlock the stop and open the Fast Travel destination overlay.
    /// </summary>
    public class MetroFastTravelStop : MonoBehaviour, IInteractable
    {
        [Header("Station Info")]
        [SerializeField] private string stopId = "metro_neon_plaza";
        [SerializeField] private string stopDisplayName = "Neon Plaza Metro";
        [SerializeField] private bool unlockedByDefault = true;

        [Header("Arrival Point (Optional Child Transform)")]
        [SerializeField] private Transform arrivalPoint;

        [Header("Optional Visual Indicator")]
        [SerializeField] private Renderer beaconRenderer;
        [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color unlockedColor = new Color(0.15f, 0.85f, 1.0f);

        private Material _beaconMat;

        public string StopId => stopId;
        public string StopDisplayName => stopDisplayName;
        public bool IsUnlocked { get; private set; }

        public Vector3 ArrivalPosition => arrivalPoint != null
            ? arrivalPoint.position
            : transform.position + transform.forward * 2.2f;

        public Vector3 ArrivalForward => arrivalPoint != null
            ? arrivalPoint.forward
            : transform.forward;

        public string InteractionPrompt => IsUnlocked
            ? $"[F] {stopDisplayName} — Fast Travel"
            : $"[F] Aktifkan Halte {stopDisplayName}";

        public bool CanInteract => true;
        public Transform InteractionTransform => transform;

        private void Awake()
        {
            IsUnlocked = unlockedByDefault;
            if (beaconRenderer == null) beaconRenderer = GetComponentInChildren<Renderer>();
            if (beaconRenderer != null) _beaconMat = beaconRenderer.material;
            RefreshVisual();
        }

        private void OnEnable()
        {
            FastTravelManager.RegisterStop(this);
        }

        private void OnDisable()
        {
            FastTravelManager.UnregisterStop(this);
        }

        public void UnlockStop()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;
            RefreshVisual();
            Debug.Log($"[NeoVeil Metro] Halte '{stopDisplayName}' aktif!");
        }

        public void Interact(GameObject interactor)
        {
            if (!IsUnlocked)
            {
                UnlockStop();
            }

            if (FastTravelManager.Instance != null)
            {
                FastTravelManager.Instance.OpenMenu(this);
            }
            else
            {
                Debug.LogWarning("[NeoVeil Metro] Tidak ada FastTravelManager di scene!");
            }
        }

        private void RefreshVisual()
        {
            if (_beaconMat == null) return;
            _beaconMat.color = IsUnlocked ? unlockedColor : lockedColor;
        }
    }
}
