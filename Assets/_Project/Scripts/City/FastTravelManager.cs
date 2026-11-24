using System;
using System.Collections.Generic;
using UnityEngine;
using NeoVeil.Player;

namespace NeoVeil.City
{
    /// <summary>
    /// Coordinates all MetroFastTravelStop nodes in the district and safely teleports the player
    /// via PlayerMovementController.SnapToPositionAndFace without physics glitches.
    /// </summary>
    public class FastTravelManager : MonoBehaviour
    {
        public static FastTravelManager Instance { get; private set; }

        private static readonly List<MetroFastTravelStop> RegisteredStops = new List<MetroFastTravelStop>();

        [Header("Player Reference (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController playerMovement;

        public IReadOnlyList<MetroFastTravelStop> AllStops => RegisteredStops;
        public bool IsMenuOpen { get; private set; }
        public MetroFastTravelStop OriginStop { get; private set; }

        public event Action<bool> OnMenuStateChanged;
        public event Action<MetroFastTravelStop> OnFastTravelCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovementController>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void RegisterStop(MetroFastTravelStop stop)
        {
            if (stop != null && !RegisteredStops.Contains(stop))
            {
                RegisteredStops.Add(stop);
            }
        }

        public static void UnregisterStop(MetroFastTravelStop stop)
        {
            RegisteredStops.Remove(stop);
        }

        public void OpenMenu(MetroFastTravelStop fromStop)
        {
            OriginStop = fromStop;
            IsMenuOpen = true;
            if (playerMovement != null) playerMovement.SetMovementLocked(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            OnMenuStateChanged?.Invoke(true);
        }

        public void CloseMenu()
        {
            IsMenuOpen = false;
            OriginStop = null;
            if (playerMovement != null) playerMovement.SetMovementLocked(false);
            OnMenuStateChanged?.Invoke(false);
        }

        public void TeleportTo(MetroFastTravelStop destination)
        {
            if (destination == null || !destination.IsUnlocked || playerMovement == null) return;

            playerMovement.SnapToPositionAndFace(destination.ArrivalPosition, destination.ArrivalForward);
            Debug.Log($"[NeoVeil Metro] Fast Travel ke '{destination.StopDisplayName}' selesai!");
            CloseMenu();
            OnFastTravelCompleted?.Invoke(destination);
        }
    }
}
