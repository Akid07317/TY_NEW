using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public sealed class EnemyAttackState : EnemyState
    {
        private readonly EnemyStateMachine stateMachine;
        private float attackLockTimer;
        private float attackStartupDuration;
        private float attackAdvanceElapsed;
        private float attackAdvanceDuration;
        private float attackAdvanceDistance;
        private float attackRecoveryDuration;
        private float recoveryTimer;
        private bool attackExecuted;
        private AttackDefinitionSO pendingAttack;

        public EnemyAttackState(EnemyBrain owner, EnemyStateMachine stateMachine) : base(owner)
        {
            this.stateMachine = stateMachine;
        }

        public EnemyAttackPresentationPhase PresentationPhase
        {
            get
            {
                if (pendingAttack == null)
                {
                    return EnemyAttackPresentationPhase.None;
                }

                if (!attackExecuted)
                {
                    if (attackLockTimer > Mathf.Epsilon && attackStartupDuration > Mathf.Epsilon)
                    {
                        return EnemyAttackPresentationPhase.Startup;
                    }

                    return EnemyAttackPresentationPhase.Advance;
                }

                return EnemyAttackPresentationPhase.Recovery;
            }
        }

        public float PresentationProgress
        {
            get
            {
                switch (PresentationPhase)
                {
                    case EnemyAttackPresentationPhase.Startup:
                        if (attackStartupDuration <= Mathf.Epsilon)
                        {
                            return 1f;
                        }

                        return 1f - Mathf.Clamp01(attackLockTimer / attackStartupDuration);

                    case EnemyAttackPresentationPhase.Advance:
                        if (attackAdvanceDuration <= Mathf.Epsilon)
                        {
                            return attackExecuted ? 1f : 0.82f;
                        }

                        return Mathf.Clamp01(attackAdvanceElapsed / attackAdvanceDuration);

                    case EnemyAttackPresentationPhase.Recovery:
                        if (attackRecoveryDuration <= Mathf.Epsilon)
                        {
                            return attackExecuted ? 1f : 0f;
                        }

                        return 1f - Mathf.Clamp01(recoveryTimer / attackRecoveryDuration);

                    default:
                        return 0f;
                }
            }
        }

        public override void Enter()
        {
            pendingAttack = Owner.AttackController != null && Owner.Archetype != null
                ? Owner.AttackController.PreviewAttackForTarget(Owner.CurrentTarget, Owner.Archetype)
                : null;
            attackExecuted = false;
            recoveryTimer = 0f;
            attackLockTimer = Mathf.Max(0.08f, pendingAttack != null ? pendingAttack.StartupSeconds : 0.18f);
            attackStartupDuration = attackLockTimer;
            attackAdvanceElapsed = 0f;
            attackAdvanceDistance = pendingAttack != null ? Mathf.Max(0f, pendingAttack.ForwardMovement) : 0f;
            attackAdvanceDuration = ResolveAttackAdvanceDuration();
            attackRecoveryDuration = 0f;
            Owner.Motor?.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner.CurrentTarget == null || Owner.Archetype == null)
            {
                stateMachine.SwitchToIdle();
                return;
            }

            if (!attackExecuted)
            {
                float remainingDelta = Mathf.Max(0f, deltaTime);

                if (attackLockTimer > 0f)
                {
                    Owner.Motor?.FaceTarget(Owner.CurrentTarget, ResolveStartupFacingSpeed());
                    float startupStep = Mathf.Min(attackLockTimer, remainingDelta);
                    attackLockTimer -= startupStep;
                    remainingDelta -= startupStep;

                    if (attackLockTimer > 0f)
                    {
                        return;
                    }
                }

                if (TryAdvanceIntoAttack(ref remainingDelta))
                {
                    return;
                }

                bool attackSucceeded = Owner.AttackController != null && Owner.Archetype != null
                    ? Owner.AttackController.TryAttack(Owner.CurrentTarget, Owner.Archetype)
                    : false;

                if (!attackSucceeded)
                {
                    SwitchAfterFailedAttack();
                    return;
                }

                attackExecuted = true;

                if (!ShouldHoldRecovery())
                {
                    stateMachine.SwitchToChase();
                    return;
                }

                recoveryTimer = ResolveRecoveryDuration();
                attackRecoveryDuration = recoveryTimer;

                if (recoveryTimer <= 0f)
                {
                    stateMachine.SwitchToChase();
                }

                return;
            }

            recoveryTimer -= deltaTime;

            if (recoveryTimer > 0f)
            {
                return;
            }

            stateMachine.SwitchToChase();
        }

        private bool ShouldHoldRecovery()
        {
            return pendingAttack != null;
        }

        private float ResolveRecoveryDuration()
        {
            if (pendingAttack == null)
            {
                return 0.12f;
            }

            if (Owner.Archetype != null && Owner.Archetype.ArchetypeType == EnemyArchetypeType.Boss)
            {
                return Mathf.Max(0.15f, pendingAttack.RecoverySeconds);
            }

            if (Owner.Archetype != null
                && Owner.Archetype.ArchetypeType == EnemyArchetypeType.Ranged
                && pendingAttack.ProjectilePrefab != null)
            {
                return Mathf.Clamp(pendingAttack.RecoverySeconds * 0.4f, 0.08f, 0.18f);
            }

            return Mathf.Clamp(pendingAttack.RecoverySeconds * 0.5f, 0.1f, 0.22f);
        }

        private void SwitchAfterFailedAttack()
        {
            if (Owner.Archetype != null
                && Owner.Archetype.ArchetypeType == EnemyArchetypeType.Ranged
                && pendingAttack != null
                && pendingAttack.ProjectilePrefab != null)
            {
                stateMachine.SwitchToStrafe();
                return;
            }

            stateMachine.SwitchToChase();
        }

        private bool TryAdvanceIntoAttack(ref float remainingDelta)
        {
            if (pendingAttack == null || attackAdvanceDistance <= Mathf.Epsilon || attackAdvanceDuration <= Mathf.Epsilon)
            {
                return false;
            }

            if (attackAdvanceElapsed >= attackAdvanceDuration)
            {
                return false;
            }

            if (remainingDelta <= 0f)
            {
                return true;
            }

            float previousNormalizedTime = attackAdvanceElapsed / attackAdvanceDuration;
            float advanceStep = Mathf.Min(remainingDelta, attackAdvanceDuration - attackAdvanceElapsed);
            attackAdvanceElapsed += advanceStep;
            remainingDelta -= advanceStep;
            float nextNormalizedTime = attackAdvanceElapsed / attackAdvanceDuration;
            float distanceStep = attackAdvanceDistance * (EaseAttackAdvance(nextNormalizedTime) - EaseAttackAdvance(previousNormalizedTime));

            if (distanceStep > 0f)
            {
                Owner.Motor?.AdvanceTowardsTarget(Owner.CurrentTarget, distanceStep);
            }

            return attackAdvanceElapsed < attackAdvanceDuration;
        }

        private float ResolveAttackAdvanceDuration()
        {
            if (pendingAttack == null || pendingAttack.ForwardMovement <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(Mathf.Max(0.06f, pendingAttack.ActiveSeconds), 0.08f, 0.12f);
        }

        private float ResolveStartupFacingSpeed()
        {
            if (Owner.Archetype == null)
            {
                return 240f;
            }

            switch (Owner.Archetype.ArchetypeType)
            {
                case EnemyArchetypeType.Ranged:
                    return 220f;
                case EnemyArchetypeType.Mobile:
                    return 280f;
                case EnemyArchetypeType.Boss:
                    return 200f;
                default:
                    return 240f;
            }
        }

        private static float EaseAttackAdvance(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return t * t * (3f - (2f * t));
        }
    }
}
