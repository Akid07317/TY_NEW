using CampusRPG.AI;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.UI
{
    public static class BossPresentationRules
    {
        public static bool IsBossEligible(EnemyBrain bossEnemy)
        {
            if (bossEnemy == null || !bossEnemy.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (bossEnemy.Archetype == null || bossEnemy.Archetype.ArchetypeType != EnemyArchetypeType.Boss)
            {
                return false;
            }

            HealthComponent health = bossEnemy.Health;
            return health != null && !health.IsDead;
        }

        public static Vector3 ResolveFlatDirection(EnemyBrain bossEnemy, Vector3 fallbackForward)
        {
            Vector3 direction;

            if (bossEnemy == null)
            {
                direction = fallbackForward;
            }
            else if (bossEnemy.CurrentTarget == null)
            {
                direction = bossEnemy.transform.forward;
            }
            else
            {
                direction = bossEnemy.CurrentTarget.position - bossEnemy.transform.position;
            }

            direction.y = 0f;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }
    }
}
