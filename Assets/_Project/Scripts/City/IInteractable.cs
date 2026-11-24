using UnityEngine;

namespace NeoVeil.City
{
    /// <summary>
    /// Unified interaction contract for all urban semi-open world objects:
    /// Anomaly Rifts, Anomaly Crates, Metro Fast Travel Stops, Vending Machines, and NPCs.
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract { get; }
        Transform InteractionTransform { get; }
        void Interact(GameObject interactor);
    }
}
