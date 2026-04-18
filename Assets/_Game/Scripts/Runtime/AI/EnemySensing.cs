using CampusRPG.Character;
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

                PlayerCharacter playerCharacter = colliders[i].GetComponentInParent<PlayerCharacter>();

                if (playerCharacter == null)
                {
                    continue;
                }

                Transform candidate = playerCharacter.transform;
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
    }
}
