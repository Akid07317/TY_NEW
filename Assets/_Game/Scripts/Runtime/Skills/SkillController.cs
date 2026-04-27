using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Skills
{
    public enum SkillBeginCastBlockReason
    {
        None = 0,
        MissingSkill = 1,
        MissingManaComponent = 2,
        Cooldown = 3,
        NotEnoughMana = 4,
        OtherPendingCast = 5,
    }

    public enum SkillSlotRuntimeStatus
    {
        MissingSkill = 0,
        Pending = 1,
        Cooldown = 2,
        Blocked = 3,
        Ready = 4,
    }

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
        private int pendingSlotIndex = -1;
        private SkillDefinitionSO pendingSkill;

        public Transform CurrentLockedTarget => lockOnTargetSelector != null ? lockOnTargetSelector.CurrentTarget : null;

        public bool HasPendingCast => pendingSlotIndex >= 0 && pendingSkill != null;

        public int PendingSlotIndex => HasPendingCast ? pendingSlotIndex : -1;

        public SkillDefinitionSO PendingSkill => HasPendingCast ? pendingSkill : null;

        public float CurrentMana => mana != null ? mana.CurrentValue : 0f;

        public float MaxMana => mana != null ? mana.MaxValue : 0f;

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
            if (!CanBeginCast(slotIndex, out skillDefinition))
            {
                skillDefinition = null;
                return false;
            }

            if (!TryRegisterPendingCast(slotIndex, skillDefinition))
            {
                skillDefinition = null;
                return false;
            }

            return true;
        }

        public bool CanBeginCast(int slotIndex, out SkillDefinitionSO skillDefinition)
        {
            return GetBeginCastBlockReason(slotIndex, out skillDefinition) == SkillBeginCastBlockReason.None;
        }

        public SkillBeginCastBlockReason GetBeginCastBlockReason(int slotIndex, out SkillDefinitionSO skillDefinition)
        {
            skillDefinition = GetSkill(slotIndex);

            if (skillDefinition == null)
            {
                return SkillBeginCastBlockReason.MissingSkill;
            }

            if (mana == null)
            {
                return SkillBeginCastBlockReason.MissingManaComponent;
            }

            if (GetRemainingCooldown(slotIndex) > 0f)
            {
                return SkillBeginCastBlockReason.Cooldown;
            }

            if (!CanAfford(skillDefinition))
            {
                return SkillBeginCastBlockReason.NotEnoughMana;
            }

            if (HasPendingCast && (pendingSlotIndex != slotIndex || pendingSkill != skillDefinition))
            {
                return SkillBeginCastBlockReason.OtherPendingCast;
            }

            return SkillBeginCastBlockReason.None;
        }

        public SkillSlotRuntimeStatus GetSlotRuntimeStatus(
            int slotIndex,
            out SkillDefinitionSO skillDefinition,
            out SkillBeginCastBlockReason blockReason)
        {
            blockReason = GetBeginCastBlockReason(slotIndex, out skillDefinition);

            if (skillDefinition == null)
            {
                return SkillSlotRuntimeStatus.MissingSkill;
            }

            if (HasPendingCast && pendingSlotIndex == slotIndex && pendingSkill == skillDefinition)
            {
                return SkillSlotRuntimeStatus.Pending;
            }

            if (IsOnCooldown(slotIndex))
            {
                return SkillSlotRuntimeStatus.Cooldown;
            }

            if (blockReason != SkillBeginCastBlockReason.None)
            {
                return SkillSlotRuntimeStatus.Blocked;
            }

            return SkillSlotRuntimeStatus.Ready;
        }

        public bool TryCommitCast(int slotIndex, SkillDefinitionSO skillDefinition)
        {
            if (!CanCommitCast(slotIndex, skillDefinition))
            {
                return false;
            }

            if (!mana.TrySpend(Mathf.Max(0f, skillDefinition.ManaCost)))
            {
                return false;
            }

            cooldownRemaining[slotIndex] = Mathf.Max(0f, skillDefinition.CooldownSeconds);
            ClearPendingCast();
            CommitCast(skillDefinition);
            return true;
        }

        public bool CancelPendingCast(int slotIndex, SkillDefinitionSO skillDefinition = null)
        {
            if (!HasPendingCast || pendingSlotIndex != slotIndex)
            {
                return false;
            }

            if (skillDefinition != null && pendingSkill != skillDefinition)
            {
                return false;
            }

            ClearPendingCast();
            return true;
        }

        public float GetRemainingCooldown(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < cooldownRemaining.Length
                ? cooldownRemaining[slotIndex]
                : 0f;
        }

        public bool IsOnCooldown(int slotIndex)
        {
            return GetRemainingCooldown(slotIndex) > 0f;
        }

        public float GetCooldownProgressNormalized(int slotIndex)
        {
            SkillDefinitionSO skill = GetSkill(slotIndex);

            if (skill == null)
            {
                return 0f;
            }

            float duration = Mathf.Max(0f, skill.CooldownSeconds);

            if (duration <= Mathf.Epsilon)
            {
                return 1f;
            }

            float remaining = Mathf.Clamp(GetRemainingCooldown(slotIndex), 0f, duration);
            return 1f - (remaining / duration);
        }

        public void ResetRuntimeState()
        {
            for (int i = 0; i < cooldownRemaining.Length; i++)
            {
                cooldownRemaining[i] = 0f;
            }

            ClearPendingCast();
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

        private bool CanCommitCast(int slotIndex, SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition == null || mana == null)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= cooldownRemaining.Length)
            {
                return false;
            }

            if (GetSkill(slotIndex) != skillDefinition || GetRemainingCooldown(slotIndex) > 0f)
            {
                return false;
            }

            if (!HasPendingCast || pendingSlotIndex != slotIndex || pendingSkill != skillDefinition)
            {
                return false;
            }

            return CanAfford(skillDefinition);
        }

        private bool TryRegisterPendingCast(int slotIndex, SkillDefinitionSO skillDefinition)
        {
            if (skillDefinition == null)
            {
                return false;
            }

            if (!HasPendingCast)
            {
                pendingSlotIndex = slotIndex;
                pendingSkill = skillDefinition;
                return true;
            }

            return pendingSlotIndex == slotIndex && pendingSkill == skillDefinition;
        }

        private void ClearPendingCast()
        {
            pendingSlotIndex = -1;
            pendingSkill = null;
        }

        private bool CanAfford(SkillDefinitionSO skillDefinition)
        {
            return mana != null
                && skillDefinition != null
                && mana.CurrentValue >= Mathf.Max(0f, skillDefinition.ManaCost);
        }
    }
}
