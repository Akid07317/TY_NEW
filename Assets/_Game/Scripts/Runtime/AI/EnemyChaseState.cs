using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyChaseState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;

        public EnemyChaseState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CurrentTarget == null)
            {
                stateMachine.SwitchToIdle();
                return;
            }

            if (Owner.Archetype == null)
            {
                return;
            }

            Vector3 targetPosition = Owner.CurrentTarget.position;
            Vector3 flatOffset = targetPosition - Owner.transform.position;
            flatOffset.y = 0f;
            float distanceToTarget = flatOffset.magnitude;
            float attackDistance = Owner.AttackController != null
                ? Owner.AttackController.GetAttackRangeForTarget(Owner.CurrentTarget, Owner.Archetype)
                : Owner.Archetype.AttackDistance;

            if (ShouldRetreatForSpacing(distanceToTarget, attackDistance))
            {
                Owner.Motor?.MoveTo(ResolveRetreatPosition(targetPosition, flatOffset, distanceToTarget, attackDistance));
                Owner.Motor?.FaceTarget(Owner.CurrentTarget);
                return;
            }

            if (distanceToTarget <= attackDistance)
            {
                Owner.Motor?.Stop();
                bool canAttack = Owner.AttackController == null || Owner.AttackController.CanAttack(Owner.Archetype.AttackCooldown);
                bool hasClearShot = Owner.AttackController == null || Owner.AttackController.HasAttackClearShotForTarget(Owner.CurrentTarget, Owner.Archetype);

                if (Owner.Archetype.ArchetypeType == EnemyArchetypeType.Mobile)
                {
                    stateMachine.SwitchToStrafe();
                    return;
                }

                if (!hasClearShot)
                {
                    if (Owner.Archetype.ArchetypeType == EnemyArchetypeType.Ranged)
                    {
                        stateMachine.SwitchToStrafe();
                    }
                    else
                    {
                        Owner.Motor?.MoveTo(targetPosition);
                        Owner.Motor?.FaceTarget(Owner.CurrentTarget);
                    }

                    return;
                }

                if (canAttack)
                {
                    stateMachine.SwitchToAttack();
                    return;
                }

                Owner.Motor?.FaceTarget(Owner.CurrentTarget);
                return;
            }

            Owner.Motor?.MoveTo(targetPosition);
            Owner.Motor?.FaceTarget(Owner.CurrentTarget);
        }

        private bool ShouldRetreatForSpacing(float distanceToTarget, float attackDistance)
        {
            if (Owner.Archetype == null || Owner.Archetype.ArchetypeType != EnemyArchetypeType.Ranged)
            {
                return false;
            }

            float preferredDistance = ResolvePreferredCombatDistance(attackDistance);
            return distanceToTarget + 0.05f < preferredDistance;
        }

        private Vector3 ResolveRetreatPosition(Vector3 targetPosition, Vector3 flatOffset, float distanceToTarget, float attackDistance)
        {
            Vector3 retreatDirection;

            if (distanceToTarget > Mathf.Epsilon)
            {
                retreatDirection = (-flatOffset / distanceToTarget);
            }
            else
            {
                retreatDirection = -Owner.transform.forward;
                retreatDirection.y = 0f;

                if (retreatDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    retreatDirection = Vector3.back;
                }

                retreatDirection.Normalize();
            }

            float preferredDistance = ResolvePreferredCombatDistance(attackDistance);
            float retreatDistance = Mathf.Max(0.35f, preferredDistance - distanceToTarget);
            Vector3 retreatPosition = Owner.transform.position + (retreatDirection * retreatDistance);
            retreatPosition.y = targetPosition.y;
            return retreatPosition;
        }

        private float ResolvePreferredCombatDistance(float attackDistance)
        {
            float preferredDistance = Owner.Archetype != null
                ? Owner.Archetype.PreferredCombatDistance
                : 0f;

            if (preferredDistance <= Mathf.Epsilon)
            {
                preferredDistance = attackDistance * 0.75f;
            }

            return Mathf.Clamp(preferredDistance, 0.5f, Mathf.Max(0.5f, attackDistance - 0.1f));
        }
    }
}
