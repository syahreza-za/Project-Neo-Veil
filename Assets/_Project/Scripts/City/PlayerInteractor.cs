using System;
using UnityEngine;
using NeoVeil.Core;

namespace NeoVeil.City
{
    /// <summary>
    /// Detects the closest IInteractable around the player using zero-GC OverlapSphereNonAlloc
    /// and triggers interaction when the player presses [F] (InputReader.OnInteractPressed).
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private InputReader inputReader;

        [Header("Detection Settings")]
        [SerializeField, Min(1f)] private float interactionRadius = 3.2f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.08f;

        private readonly Collider[] _hitBuffer = new Collider[16];
        private float _scanTimer;

        public IInteractable CurrentInteractable { get; private set; }
        public bool HasInteractable => CurrentInteractable != null && CurrentInteractable.CanInteract;

        public event Action<IInteractable> OnInteractableChanged;

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.OnInteractPressed += HandleInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.OnInteractPressed -= HandleInteractPressed;
            }
        }

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = scanInterval;
                ScanForClosestInteractable();
            }
        }

        private void HandleInteractPressed()
        {
            if (CurrentInteractable != null && CurrentInteractable.CanInteract)
            {
                CurrentInteractable.Interact(gameObject);
                ScanForClosestInteractable();
            }
        }

        private void ScanForClosestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactionRadius,
                _hitBuffer,
                interactableLayer,
                QueryTriggerInteraction.Collide
            );

            IInteractable best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null) continue;

                var candidate = col.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;

                Transform targetTransform = candidate.InteractionTransform != null
                    ? candidate.InteractionTransform
                    : col.transform;

                float sqr = (targetTransform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            if (!ReferenceEquals(best, CurrentInteractable))
            {
                CurrentInteractable = best;
                OnInteractableChanged?.Invoke(CurrentInteractable);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.7f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
