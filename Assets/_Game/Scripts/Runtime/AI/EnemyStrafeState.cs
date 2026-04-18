using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyStrafeState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;
        private float remainingTime;
        private float strafeDirection;

        public EnemyStrafeState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public override void Enter()
        {
            remainingTime = Owner.Archetype != null
                ? Mathf.Max(0.1f, Owner.Archetype.StrafeDurationSeconds)
                : 0.35f;
            strafeDirection = ResolveInitialStrafeDirection(Owner);
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CurrentTarget == null || Owner.Archetype == null)
            {
                stateMachine.SwitchToIdle();
                return;
            }

            Vector3 targetPosition = Owner.CurrentTarget.position;
            Vector3 offsetFromTarget = Owner.transform.position - targetPosition;
            offsetFromTarget.y = 0f;

            if (offsetFromTarget.sqrMagnitude <= Mathf.Epsilon)
            {
                offsetFromTarget = -Owner.transform.forward;
                offsetFromTarget.y = 0f;

                if (offsetFromTarget.sqrMagnitude <= Mathf.Epsilon)
                {
                    offsetFromTarget = Vector3.back;
                }
            }

            float radius = Mathf.Max(ResolveOrbitRadius(), offsetFromTarget.magnitude);
            Vector3 radialDirection = offsetFromTarget.normalized;
            Vector3 lateralDirection = Vector3.Cross(Vector3.up, radialDirection) * strafeDirection;
            Vector3 desiredPosition = targetPosition
                + (radialDirection * radius)
                + (lateralDirection.normalized * ResolveStrafeDistance());

            desiredPosition.y = Owner.transform.position.y;
            Owner.Motor?.MoveTo(desiredPosition);
            Owner.Motor?.FaceTarget(Owner.CurrentTarget);

            remainingTime -= deltaTime;

            if (remainingTime > 0f)
            {
                return;
            }

            float attackDistance = Owner.AttackController != null
                ? Owner.AttackController.GetAttackRangeForTarget(Owner.CurrentTarget, Owner.Archetype)
                : Owner.Archetype.AttackDistance;
            float currentDistance = Vector3.Distance(Flatten(Owner.transform.position), Flatten(targetPosition));
            bool hasClearShot = Owner.AttackController == null || Owner.AttackController.HasAttackClearShotForTarget(Owner.CurrentTarget, Owner.Archetype);

            if (currentDistance <= attackDistance
                && (Owner.AttackController == null || Owner.AttackController.CanAttack(Owner.Archetype.AttackCooldown))
                && hasClearShot)
            {
                stateMachine.SwitchToAttack();
                return;
            }

            stateMachine.SwitchToChase();
        }

        private float ResolveOrbitRadius()
        {
            float preferredDistance = Owner.Archetype.PreferredCombatDistance;

            if (preferredDistance > Mathf.Epsilon)
            {
                return preferredDistance;
            }

            float attackDistance = Owner.AttackController != null
                ? Owner.AttackController.GetAttackRangeForTarget(Owner.CurrentTarget, Owner.Archetype)
                : Owner.Archetype.AttackDistance;
            return Mathf.Max(0.5f, attackDistance * 0.8f);
        }

        private float ResolveStrafeDistance()
        {
            if (Owner.Archetype == null || Owner.Archetype.StrafeDistance <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Owner.Archetype.StrafeDistance;
        }

        private static float ResolveInitialStrafeDirection(EnemyBrain owner)
        {
            if (owner == null)
            {
                return 1f;
            }

            unchecked
            {
                int hash = 17;
                string ownerName = owner.name;

                for (int i = 0; i < ownerName.Length; i++)
                {
                    hash = (hash * 31) + ownerName[i];
                }

                Vector3 position = owner.transform.position;
                hash = (hash * 31) + Mathf.RoundToInt(position.x * 100f);
                hash = (hash * 31) + Mathf.RoundToInt(position.z * 100f);

                return (hash & 1) == 0 ? 1f : -1f;
            }
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
