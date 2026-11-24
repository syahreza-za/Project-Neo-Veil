using System;
using UnityEngine;

namespace NeoVeil.Combat
{
    /// <summary>
    /// ZZZ-style Soft Lock-On targeting system.
    /// Automatically selects the best enemy target based on distance and directional intent
    /// (player move input or camera forward), with zero per-frame GC allocations.
    /// </summary>
    public class SoftLockOnSystem : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField, Min(1f)] private float detectionRadius = 12f;
        [SerializeField, Range(0f, 1f)] private float directionWeight = 0.65f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.08f;

        [Header("Line of Sight (Optional)")]
        [SerializeField] private bool requireLineOfSight = false;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float eyeHeight = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly Collider[] _hitBuffer = new Collider[24];
        private float _scanTimer;

        public Transform CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        public event Action<Transform> OnTargetChanged;

        /// <summary>
        /// Refreshes the best soft-lock target using the given world-space intent direction
        /// (pass movement direction when moving, or camera forward when idle).
        /// </summary>
        public Transform UpdateTarget(Vector3 worldIntentDirection, bool forceImmediate = false)
        {
            _scanTimer -= Time.deltaTime;
            if (!forceImmediate && _scanTimer > 0f && IsTargetStillValid(CurrentTarget))
            {
                return CurrentTarget;
            }

            _scanTimer = scanInterval;

            Vector3 intent = worldIntentDirection;
            intent.y = 0f;
            if (intent.sqrMagnitude > 0.001f)
            {
                intent.Normalize();
            }
            else
            {
                intent = transform.forward;
            }

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectionRadius,
                _hitBuffer,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            Transform bestTarget = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = _hitBuffer[i];
                if (col == null || !col.gameObject.activeInHierarchy) continue;

                Transform candidate = col.transform;
                Vector3 toCandidate = candidate.position - transform.position;
                toCandidate.y = 0f;
                float dist = toCandidate.magnitude;
                if (dist < 0.01f) continue;

                if (requireLineOfSight && !HasClearLineOfSight(candidate))
                {
                    continue;
                }

                Vector3 dir = toCandidate / dist;
                float alignment = Vector3.Dot(intent, dir); // -1 to 1
                float normalizedDistScore = 1f - Mathf.Clamp01(dist / detectionRadius); // 0 to 1

                float score = (alignment * directionWeight) + (normalizedDistScore * (1f - directionWeight));

                // Small sticky bonus to avoid rapid jittering between two similar targets
                if (candidate == CurrentTarget)
                {
                    score += 0.1f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            if (bestTarget != CurrentTarget)
            {
                CurrentTarget = bestTarget;
                OnTargetChanged?.Invoke(CurrentTarget);
            }

            return CurrentTarget;
        }

        /// <summary>
        /// Returns flattened normalized direction from player to CurrentTarget, or transform.forward if none.
        /// </summary>
        public Vector3 GetDirectionToTarget()
        {
            if (CurrentTarget == null) return transform.forward;

            Vector3 dir = CurrentTarget.position - transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
        }

        public float GetDistanceToTarget()
        {
            if (CurrentTarget == null) return float.PositiveInfinity;
            return Vector3.Distance(transform.position, CurrentTarget.position);
        }

        private bool IsTargetStillValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return false;
            float sqrDist = (target.position - transform.position).sqrMagnitude;
            return sqrDist <= detectionRadius * detectionRadius;
        }

        private bool HasClearLineOfSight(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 dest = target.position + Vector3.up * eyeHeight;
            return !Physics.Linecast(origin, dest, obstacleLayer, QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos) return;

            Gizmos.color = new Color(0f, 0.85f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (CurrentTarget != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
                Gizmos.DrawLine(transform.position + Vector3.up, CurrentTarget.position + Vector3.up);
                Gizmos.DrawWireSphere(CurrentTarget.position + Vector3.up, 0.5f);
            }
        }
    }
}
