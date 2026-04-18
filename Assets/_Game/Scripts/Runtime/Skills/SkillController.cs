using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Skills
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ManaComponent))]
    public sealed class SkillController : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter owner;
        [SerializeField] private ManaComponent mana;
        [SerializeField] private AttackExecutor attackExecutor;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private SkillDefinitionSO skill1;
        [SerializeField] private SkillDefinitionSO skill2;
        [SerializeField] private float spawnedEffectLifetimeSeconds = 1.5f;

        private readonly float[] cooldownRemaining = new float[2];

        public Transform CurrentLockedTarget => lockOnTargetSelector != null ? lockOnTargetSelector.CurrentTarget : null;

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponent<PlayerCharacter>();
            }

            if (mana == null)
            {
                mana = GetComponent<ManaComponent>();
            }

            if (attackExecutor == null)
            {
                attackExecutor = GetComponent<AttackExecutor>();
            }

            if (lockOnTargetSelector == null)
            {
                lockOnTargetSelector = GetComponent<LockOnTargetSelector>();
            }

            if (castOrigin == null)
            {
                castOrigin = transform;
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < cooldownRemaining.Length; i++)
            {
                cooldownRemaining[i] = Mathf.Max(0f, cooldownRemaining[i] - deltaTime);
            }
        }

        public bool TryBeginCast(int slotIndex, out SkillDefinitionSO skillDefinition)
        {
            skillDefinition = GetSkill(slotIndex);

            if (skillDefinition == null || mana == null)
            {
                return false;
            }

            if (GetRemainingCooldown(slotIndex) > 0f)
            {
                return false;
            }

            if (!mana.TrySpend(skillDefinition.ManaCost))
            {
                return false;
            }

            cooldownRemaining[slotIndex] = skillDefinition.CooldownSeconds;
            return true;
        }

        public float GetRemainingCooldown(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < cooldownRemaining.Length
                ? cooldownRemaining[slotIndex]
                : 0f;
        }

        public SkillDefinitionSO GetSkill(int slotIndex)
        {
            return slotIndex switch
            {
                0 => skill1,
                1 => skill2,
                _ => null
            };
        }

        public void CommitCast(SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition == null)
            {
                return;
            }

            Transform origin = castOrigin != null ? castOrigin : transform;
            Vector3 aimDirection = SkillCastUtility.ResolveAimDirection(
                skillDefinition,
                transform,
                owner != null ? owner.CameraTransform : null,
                CurrentLockedTarget);
            Vector3 impactPoint = SkillCastUtility.ResolveImpactPoint(
                skillDefinition,
                origin,
                transform,
                CurrentLockedTarget,
                aimDirection);
            float attackPower = owner?.BaseStats != null ? owner.BaseStats.Attack : 20f;
            float damage = SkillCastUtility.ResolveDamage(attackPower, skillDefinition);
            bool launchedProjectile = SkillCastUtility.TryBuildProjectileLaunchPlan(origin, skillDefinition, aimDirection, damage, out SkillProjectileCastPlan projectilePlan)
                && SkillCastUtility.TryLaunchProjectile(gameObject, projectilePlan);

            if (!launchedProjectile && attackExecutor != null)
            {
                attackExecutor.ExecuteSphere(
                    impactPoint,
                    Mathf.Max(0.1f, skillDefinition.ImpactRadius),
                    damage,
                    gameObject);
            }

            if (!launchedProjectile && skillDefinition.EffectPrefab != null)
            {
                Quaternion effectRotation = aimDirection.sqrMagnitude > Mathf.Epsilon
                    ? Quaternion.LookRotation(aimDirection, Vector3.up)
                    : Quaternion.identity;
                GameObject effectInstance = Instantiate(skillDefinition.EffectPrefab, impactPoint, effectRotation);
                Destroy(effectInstance, spawnedEffectLifetimeSeconds);
            }
        }
    }
}
