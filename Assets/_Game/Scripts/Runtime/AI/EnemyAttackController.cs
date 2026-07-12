using System;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public readonly struct EnemyAttackCommit
    {
        public EnemyAttackCommit(
            Transform target,
            EnemyArchetypeSO archetype,
            AttackDefinitionSO attack,
            float damage)
        {
            Target = target;
            Archetype = archetype;
            Attack = attack;
            Damage = damage;
        }

        public Transform Target { get; }

        public EnemyArchetypeSO Archetype { get; }

        public AttackDefinitionSO Attack { get; }

        public float Damage { get; }
    }

    public sealed class EnemyAttackController : MonoBehaviour
    {
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private float rangePadding = 0.08f;
        [SerializeField] private float maxHitAngle = 45f;
        [SerializeField] private float bossRepeatSelectionSlack = 0.3f;

        private float cooldownTimer;
        private int nextAttackIndex;
        private int lastAttackIndex = -1;

        public event Action<EnemyAttackCommit> AttackCommitted;

        private void Awake()
        {
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }

        public void Tick(float deltaTime)
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);
        }

        public bool CanAttack(float cooldown)
        {
            return cooldownTimer <= 0f;
        }

        public AttackDefinitionSO PreviewNextAttack(EnemyArchetypeSO archetype)
        {
            return EnemyAttackSelectionResolver.ResolveNextSelection(archetype, nextAttackIndex).Attack;
        }

        public float GetNextAttackRange(EnemyArchetypeSO archetype)
        {
            AttackDefinitionSO attack = PreviewNextAttack(archetype);
            return EnemyAttackSelectionResolver.ResolveAttackRange(archetype, attack);
        }

        public AttackDefinitionSO PreviewAttackForTarget(Transform target, EnemyArchetypeSO archetype)
        {
            return PreviewAttackSelectionForTarget(target, archetype).Attack;
        }

        public EnemyAttackSelection PreviewAttackSelectionForTarget(Transform target, EnemyArchetypeSO archetype)
        {
            return ResolveAttackSelection(target, archetype, includeFallbackRange: true);
        }

        public float GetAttackRangeForTarget(Transform target, EnemyArchetypeSO archetype)
        {
            AttackDefinitionSO attack = PreviewAttackForTarget(target, archetype);
            return EnemyAttackSelectionResolver.ResolveAttackRange(archetype, attack);
        }

        public bool HasNextAttackClearShot(Transform target, EnemyArchetypeSO archetype)
        {
            if (target == null || archetype == null)
            {
                return false;
            }

            return HasClearShot(target);
        }

        public bool HasAttackClearShotForTarget(Transform target, EnemyArchetypeSO archetype)
        {
            if (target == null || archetype == null)
            {
                return false;
            }

            return HasClearShot(target);
        }

        public void ResetRuntimeState()
        {
            cooldownTimer = 0f;
            nextAttackIndex = 0;
            lastAttackIndex = -1;
        }

        public bool TryAttack(Transform target, EnemyArchetypeSO archetype)
        {
            return TryAttack(target, archetype, ResolveAttackSelection(target, archetype, includeFallbackRange: false));
        }

        public bool TryAttack(Transform target, EnemyArchetypeSO archetype, EnemyAttackSelection selection)
        {
            AttackDefinitionSO attack = selection.Attack;
            Transform origin = ResolveAttackOrigin();
            bool hasClearShot = HasClearShot(target);
            bool canAttack = archetype != null && CanAttack(archetype.AttackCooldown);

            if (!EnemyAttackExecutionUtility.TryResolveAttackTarget(
                target,
                origin,
                archetype,
                attack,
                rangePadding,
                maxHitAngle,
                canAttack,
                hasClearShot,
                out IDamageable damageable))
            {
                return false;
            }

            float damage = EnemyAttackExecutionUtility.ResolveDamage(archetype.BaseAttack, attack);

            if (attack != null && attack.ProjectilePrefab != null)
            {
                if (!EnemyAttackExecutionUtility.TryBuildProjectileLaunchPlan(target, origin, attack, damage, out EnemyProjectileLaunchPlan launchPlan)
                    || !EnemyAttackExecutionUtility.TryLaunchProjectile(gameObject, launchPlan))
                {
                    return false;
                }
            }
            else
            {
                ApplyDamage(damageable, damage, origin.position, gameObject, attack);
            }

            cooldownTimer = archetype.AttackCooldown;
            AdvanceAttackIndex(archetype, selection.Index);
            AttackCommitted?.Invoke(new EnemyAttackCommit(target, archetype, attack, damage));
            return true;
        }

        public void RegisterCommittedMiss(EnemyArchetypeSO archetype, EnemyAttackSelection selection)
        {
            if (archetype == null)
            {
                return;
            }

            cooldownTimer = Mathf.Max(cooldownTimer, archetype.AttackCooldown);
            AdvanceAttackIndex(archetype, selection.Index);
        }

        public void RegisterServerAuthoritativeCommit(Transform target, EnemyArchetypeSO archetype)
        {
            if (archetype == null)
            {
                return;
            }

            EnemyAttackSelection selection = target != null
                ? PreviewAttackSelectionForTarget(target, archetype)
                : EnemyAttackSelectionResolver.ResolveNextSelection(archetype, nextAttackIndex);
            cooldownTimer = Mathf.Max(cooldownTimer, archetype.AttackCooldown);
            AdvanceAttackIndex(archetype, selection.Index);
        }

        private void AdvanceAttackIndex(EnemyArchetypeSO archetype, int usedAttackIndex)
        {
            if (archetype == null || archetype.Attacks == null || archetype.Attacks.Length == 0)
            {
                nextAttackIndex = 0;
                lastAttackIndex = -1;
                return;
            }

            int baselineIndex = usedAttackIndex >= 0 ? usedAttackIndex : nextAttackIndex;
            int attackIndex = EnemyAttackSelectionResolver.ResolveAttackIndex(archetype, baselineIndex);
            lastAttackIndex = attackIndex;
            nextAttackIndex = (attackIndex + 1) % archetype.Attacks.Length;
        }

        private EnemyAttackSelection ResolveAttackSelection(
            Transform target,
            EnemyArchetypeSO archetype,
            bool includeFallbackRange)
        {
            bool hasResolvedProjectilePath = false;
            bool projectilePathIsClear = false;

            return EnemyAttackSelectionResolver.ResolveAttackSelection(
                target,
                ResolveAttackOrigin(),
                archetype,
                nextAttackIndex,
                lastAttackIndex,
                includeFallbackRange,
                bossRepeatSelectionSlack,
                attack =>
                {
                    if (attack == null || attack.ProjectilePrefab == null)
                    {
                        return true;
                    }

                    if (!hasResolvedProjectilePath)
                    {
                        projectilePathIsClear = HasClearShot(target);
                        hasResolvedProjectilePath = true;
                    }

                    return projectilePathIsClear;
                });
        }

        private bool HasClearShot(Transform target)
        {
            return EnemyAttackLineOfSight.HasClearShot(transform.root, ResolveAttackOrigin(), target);
        }

        private Transform ResolveAttackOrigin()
        {
            return attackOrigin != null ? attackOrigin : transform;
        }

        private static void ApplyDamage(
            IDamageable damageable,
            float damage,
            Vector3 hitPoint,
            GameObject source,
            AttackDefinitionSO attack)
        {
            if (damageable is DamageableReceiver damageableReceiver)
            {
                damageableReceiver.ReceiveDamage(damage, hitPoint, source, attack);
                return;
            }

            damageable.ReceiveDamage(damage, hitPoint, source);
        }
    }
}
