using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.Skills
{
    public enum SkillTargetMode
    {
        Self,
        Forward,
        LockedTarget
    }

    [CreateAssetMenu(fileName = "SO_SkillDefinition", menuName = "CampusRPG/Skills/Skill Definition")]
    public sealed class SkillDefinitionSO : ScriptableObject
    {
        [SerializeField] private string skillId = "Skill_Id";
        [SerializeField] private string displayName = "New Skill";
        [SerializeField] private float manaCost = 20f;
        [SerializeField] private float cooldownSeconds = 6f;
        [SerializeField] private float castDurationSeconds = 0.25f;
        [SerializeField] private float range = 8f;
        [SerializeField] private float damageMultiplier = 1.6f;
        [SerializeField] private float impactRadius = 1f;
        [SerializeField] private SkillTargetMode targetMode = SkillTargetMode.Forward;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float projectileLifetimeSeconds = 1.5f;
        [SerializeField] private float projectileSpawnOffset = 0.35f;
        [SerializeField] private ProjectileTrajectoryMode projectileTrajectoryMode = ProjectileTrajectoryMode.PrefabDefault;
        [SerializeField] private float projectileArcHeight;
        [SerializeField] private GameObject effectPrefab;

        public string SkillId => skillId;

        public string DisplayName => displayName;

        public float ManaCost => manaCost;

        public float CooldownSeconds => cooldownSeconds;

        public float CastDurationSeconds => castDurationSeconds;

        public float Range => range;

        public float DamageMultiplier => damageMultiplier;

        public float ImpactRadius => impactRadius;

        public SkillTargetMode TargetMode => targetMode;

        public GameObject ProjectilePrefab => projectilePrefab;

        public float ProjectileSpeed => projectileSpeed;

        public float ProjectileLifetimeSeconds => projectileLifetimeSeconds;

        public float ProjectileSpawnOffset => projectileSpawnOffset;

        public ProjectileTrajectoryMode ProjectileTrajectoryMode => projectileTrajectoryMode;

        public float ProjectileArcHeight => projectileArcHeight;

        public GameObject EffectPrefab => effectPrefab;
    }
}
