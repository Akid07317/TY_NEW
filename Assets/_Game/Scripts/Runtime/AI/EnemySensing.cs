using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemySensing : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask = ~0;

        public Transform FindTarget(Vector3 origin, float radius)
        {
            Collider[] colliders = Physics.OverlapSphere(origin, radius, targetMask, QueryTriggerInteraction.Ignore);
            Transform bestTarget = null;
            float bestSqrDistance = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].transform.root == transform.root)
                {
                    continue;
                }

                if (!TryResolveLivingPlayerTarget(colliders[i], out Transform candidate))
                {
                    continue;
                }

                if (!EnemyAttackLineOfSight.HasClearShot(transform.root, origin, candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.position - origin).sqrMagnitude;

                if (sqrDistance >= bestSqrDistance)
                {
                    continue;
                }

                bestSqrDistance = sqrDistance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static bool TryResolveLivingPlayerTarget(Collider collider, out Transform candidate)
        {
            candidate = null;

            if (collider == null)
            {
                return false;
            }

            PlayerCharacter playerCharacter = collider.GetComponentInParent<PlayerCharacter>();

            if (playerCharacter == null)
            {
                return false;
            }

            HealthComponent health = playerCharacter.GetComponentInParent<HealthComponent>();

            if (health != null && health.IsDead)
            {
                return false;
            }

            candidate = playerCharacter.transform;
            return candidate != null;
        }
    }
}
