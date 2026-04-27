using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public readonly struct EnemyAttackSelection
    {
        public EnemyAttackSelection(AttackDefinitionSO attack, int index)
        {
            Attack = attack;
            Index = index;
        }

        public AttackDefinitionSO Attack { get; }

        public int Index { get; }
    }

    public static class EnemyAttackSelectionResolver
    {
        public static EnemyAttackSelection ResolveNextSelection(EnemyArchetypeSO archetype, int nextAttackIndex)
        {
            int resolvedIndex = ResolveAttackIndex(archetype, nextAttackIndex);
            return new EnemyAttackSelection(ResolveAttackDefinition(archetype, resolvedIndex), resolvedIndex);
        }

        public static EnemyAttackSelection ResolveAttackSelection(
            Transform target,
            Transform attackOrigin,
            EnemyArchetypeSO archetype,
            int nextAttackIndex,
            int lastAttackIndex,
            bool includeFallbackRange,
            float repeatSelectionSlack,
            global::System.Func<AttackDefinitionSO, bool> canUseAttack)
        {
            EnemyAttackSelection baseline = ResolveNextSelection(archetype, nextAttackIndex);

            if (!ShouldUseTargetAwareSelection(target, archetype))
            {
                return baseline;
            }

            float targetDistance = ResolveFlatTargetDistance(attackOrigin, target);
            EnemyTargetResponseType targetResponse = ResolveTargetResponseType(attackOrigin, target);

            if (TryResolveTargetResponseSelection(
                archetype,
                baseline,
                targetDistance,
                targetResponse,
                lastAttackIndex,
                repeatSelectionSlack,
                canUseAttack,
                out EnemyAttackSelection responseSelection))
            {
                return responseSelection;
            }

            EnemyAttackSelection fallback = baseline;
            bool hasFallback = baseline.Attack != null && IsAttackUsableForTargetResponse(baseline.Attack, targetResponse);
            float fallbackRange = hasFallback ? ResolveAttackRange(archetype, baseline.Attack) : float.MinValue;
            EnemyAttackSelection bestViable = baseline;
            bool hasBestViable = false;
            float bestScore = float.MaxValue;
            int bestOffset = int.MaxValue;
            EnemyAttackSelection bestAlternate = baseline;
            bool hasBestAlternate = false;
            float bestAlternateScore = float.MaxValue;
            int bestAlternateOffset = int.MaxValue;

            for (int offset = 0; offset < archetype.Attacks.Length; offset++)
            {
                int candidateIndex = (baseline.Index + offset) % archetype.Attacks.Length;
                AttackDefinitionSO candidateAttack = archetype.Attacks[candidateIndex];

                if (candidateAttack == null)
                {
                    continue;
                }

                if (!IsAttackUsableForTargetResponse(candidateAttack, targetResponse))
                {
                    continue;
                }

                if (canUseAttack != null && !canUseAttack(candidateAttack))
                {
                    continue;
                }

                float candidateRange = ResolveAttackRange(archetype, candidateAttack);

                if (includeFallbackRange && (!hasFallback || candidateRange > fallbackRange))
                {
                    fallback = new EnemyAttackSelection(candidateAttack, candidateIndex);
                    fallbackRange = candidateRange;
                    hasFallback = true;
                }

                if (targetDistance <= candidateRange)
                {
                    float candidateScore = ResolveBossAttackPreferenceScore(archetype, candidateAttack, targetDistance);

                    if (IsPreferredCandidate(candidateScore, offset, bestScore, bestOffset))
                    {
                        bestViable = new EnemyAttackSelection(candidateAttack, candidateIndex);
                        bestScore = candidateScore;
                        bestOffset = offset;
                        hasBestViable = true;
                    }

                    if (candidateIndex != lastAttackIndex
                        && IsPreferredCandidate(candidateScore, offset, bestAlternateScore, bestAlternateOffset))
                    {
                        bestAlternate = new EnemyAttackSelection(candidateAttack, candidateIndex);
                        bestAlternateScore = candidateScore;
                        bestAlternateOffset = offset;
                        hasBestAlternate = true;
                    }
                }
            }

            if (hasBestViable)
            {
                if (bestViable.Index == lastAttackIndex
                    && hasBestAlternate
                    && bestAlternateScore <= bestScore + Mathf.Max(0f, repeatSelectionSlack))
                {
                    return bestAlternate;
                }

                return bestViable;
            }

            return includeFallbackRange && hasFallback ? fallback : baseline;
        }

        public static EnemyTargetResponseType ResolveTargetResponseType(Transform attackOrigin, Transform target)
        {
            if (target == null)
            {
                return EnemyTargetResponseType.None;
            }

            PlayerStateMachine stateMachine = target.GetComponentInParent<PlayerStateMachine>();

            if (stateMachine != null)
            {
                if (stateMachine.CurrentEvasiveActionType == PlayerEvasiveActionType.AirDodge)
                {
                    return EnemyTargetResponseType.AntiAir;
                }

                if (stateMachine.CurrentEvasiveActionType == PlayerEvasiveActionType.CombatRoll)
                {
                    return EnemyTargetResponseType.ChaseRoll;
                }
            }

            float verticalDelta = ResolveTargetVerticalDelta(attackOrigin, target);

            if (verticalDelta >= 0.75f)
            {
                return EnemyTargetResponseType.AntiAir;
            }

            PlayerMotor motor = target.GetComponentInParent<PlayerMotor>();

            if (motor != null && !motor.IsGrounded && verticalDelta >= 0.35f)
            {
                return EnemyTargetResponseType.AntiAir;
            }

            return EnemyTargetResponseType.None;
        }

        public static int ResolveAttackIndex(EnemyArchetypeSO archetype, int attackIndex)
        {
            if (archetype == null || archetype.Attacks == null || archetype.Attacks.Length == 0)
            {
                return 0;
            }

            return Mathf.Clamp(attackIndex, 0, archetype.Attacks.Length - 1);
        }

        public static AttackDefinitionSO ResolveAttackDefinition(EnemyArchetypeSO archetype, int attackIndex)
        {
            if (archetype == null || archetype.Attacks == null || archetype.Attacks.Length == 0)
            {
                return null;
            }

            int resolvedIndex = ResolveAttackIndex(archetype, attackIndex);
            return archetype.Attacks[resolvedIndex];
        }

        public static float ResolveAttackRange(EnemyArchetypeSO archetype, AttackDefinitionSO attack)
        {
            float attackRange = archetype.AttackDistance;

            if (attack != null)
            {
                attackRange = Mathf.Max(attackRange, attack.Range + attack.Radius);
            }

            return attackRange;
        }

        private static bool ShouldUseTargetAwareSelection(Transform target, EnemyArchetypeSO archetype)
        {
            return target != null
                && archetype != null
                && archetype.ArchetypeType == EnemyArchetypeType.Boss
                && archetype.Attacks != null
                && archetype.Attacks.Length > 1;
        }

        private static bool TryResolveTargetResponseSelection(
            EnemyArchetypeSO archetype,
            EnemyAttackSelection baseline,
            float targetDistance,
            EnemyTargetResponseType targetResponse,
            int lastAttackIndex,
            float repeatSelectionSlack,
            global::System.Func<AttackDefinitionSO, bool> canUseAttack,
            out EnemyAttackSelection selection)
        {
            selection = baseline;

            if (targetResponse == EnemyTargetResponseType.None)
            {
                return false;
            }

            EnemyAttackSelection bestViable = baseline;
            bool hasBestViable = false;
            float bestScore = float.MaxValue;
            int bestOffset = int.MaxValue;
            EnemyAttackSelection bestAlternate = baseline;
            bool hasBestAlternate = false;
            float bestAlternateScore = float.MaxValue;
            int bestAlternateOffset = int.MaxValue;

            for (int offset = 0; offset < archetype.Attacks.Length; offset++)
            {
                int candidateIndex = (baseline.Index + offset) % archetype.Attacks.Length;
                AttackDefinitionSO candidateAttack = archetype.Attacks[candidateIndex];

                if (candidateAttack == null || candidateAttack.EnemyTargetResponse != targetResponse)
                {
                    continue;
                }

                if (canUseAttack != null && !canUseAttack(candidateAttack))
                {
                    continue;
                }

                float candidateRange = ResolveAttackRange(archetype, candidateAttack);

                if (targetDistance > candidateRange)
                {
                    continue;
                }

                float candidateScore = ResolveBossAttackPreferenceScore(archetype, candidateAttack, targetDistance);

                if (IsPreferredCandidate(candidateScore, offset, bestScore, bestOffset))
                {
                    bestViable = new EnemyAttackSelection(candidateAttack, candidateIndex);
                    bestScore = candidateScore;
                    bestOffset = offset;
                    hasBestViable = true;
                }

                if (candidateIndex != lastAttackIndex
                    && IsPreferredCandidate(candidateScore, offset, bestAlternateScore, bestAlternateOffset))
                {
                    bestAlternate = new EnemyAttackSelection(candidateAttack, candidateIndex);
                    bestAlternateScore = candidateScore;
                    bestAlternateOffset = offset;
                    hasBestAlternate = true;
                }
            }

            if (!hasBestViable)
            {
                return false;
            }

            selection = bestViable.Index == lastAttackIndex
                && hasBestAlternate
                && bestAlternateScore <= bestScore + Mathf.Max(0f, repeatSelectionSlack)
                    ? bestAlternate
                    : bestViable;
            return true;
        }

        private static bool IsAttackUsableForTargetResponse(
            AttackDefinitionSO attack,
            EnemyTargetResponseType targetResponse)
        {
            return attack == null
                || attack.EnemyTargetResponse == EnemyTargetResponseType.None
                || attack.EnemyTargetResponse == targetResponse;
        }

        private static float ResolveBossAttackPreferenceScore(
            EnemyArchetypeSO archetype,
            AttackDefinitionSO attack,
            float targetDistance)
        {
            if (archetype == null)
            {
                return float.MaxValue;
            }

            float preferredDistance = archetype.AttackDistance;

            if (attack != null)
            {
                preferredDistance = Mathf.Max(preferredDistance, attack.Range);
            }

            return Mathf.Abs(targetDistance - preferredDistance);
        }

        private static bool IsPreferredCandidate(
            float candidateScore,
            int candidateOffset,
            float bestScore,
            int bestOffset)
        {
            return candidateScore < bestScore
                || (Mathf.Approximately(candidateScore, bestScore) && candidateOffset < bestOffset);
        }

        private static float ResolveFlatTargetDistance(Transform attackOrigin, Transform target)
        {
            if (attackOrigin == null || target == null)
            {
                return float.MaxValue;
            }

            Vector3 flatDirection = target.position - attackOrigin.position;
            flatDirection.y = 0f;
            return flatDirection.magnitude;
        }

        private static float ResolveTargetVerticalDelta(Transform attackOrigin, Transform target)
        {
            if (attackOrigin == null || target == null)
            {
                return 0f;
            }

            return target.position.y - attackOrigin.position.y;
        }
    }
}
