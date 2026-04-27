using UnityEngine;

namespace CampusRPG.Combat
{
    public enum AttackHitboxShape
    {
        LegacyForwardSphere = 0,
        Sphere = 1,
        Box = 2
    }

    public enum AttackHitboxActivationMode
    {
        TimedWindow = 0,
        AnimationEvent = 1
    }

    public enum EnemyTargetResponseType
    {
        None = 0,
        AntiAir = 1,
        ChaseRoll = 2,
        GuardBreak = 3
    }

    [CreateAssetMenu(fileName = "SO_AttackDefinition", menuName = "CampusRPG/Combat/Attack Definition")]
    public sealed class AttackDefinitionSO : ScriptableObject
    {
        [SerializeField] private string attackId = "Attack_Id";
        [SerializeField] private string displayName = "Attack";
        [SerializeField] private string animationStateName = "Attack";
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float startupSeconds = 0.15f;
        [SerializeField] private float activeSeconds = 0.1f;
        [SerializeField] private float recoverySeconds = 0.25f;
        [SerializeField] private float animationDurationSeconds;
        [SerializeField] private float hitStopSeconds = 0.05f;
        [SerializeField] private bool breaksGuard;
        [SerializeField] private float blockStunSeconds;
        [SerializeField] private float guardBreakHitStunSeconds = 0.12f;
        [SerializeField] private float forwardMovement = 0.5f;
        [SerializeField] private float movementSpeedScale = 0.65f;
        [SerializeField] private float range = 2f;
        [SerializeField] private float radius = 0.5f;
        [SerializeField] private AttackHitboxShape hitboxShape = AttackHitboxShape.LegacyForwardSphere;
        [SerializeField] private Vector3 hitboxLocalCenter = new Vector3(0f, 0f, 1f);
        [SerializeField] private Vector3 hitboxHalfExtents = new Vector3(0.5f, 0.5f, 1f);
        [SerializeField] private float hitboxRadius = 0.5f;
        [SerializeField] private AttackHitboxActivationMode hitboxActivationMode = AttackHitboxActivationMode.TimedWindow;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileLifetimeSeconds = 1.25f;
        [SerializeField] private float projectileSpawnOffset = 0.35f;
        [SerializeField] private ProjectileTrajectoryMode projectileTrajectoryMode = ProjectileTrajectoryMode.PrefabDefault;
        [SerializeField] private float projectileArcHeight;
        [SerializeField] private EnemyTargetResponseType enemyTargetResponse;

        public string AttackId => attackId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? attackId : displayName;

        public string AnimationStateName => animationStateName;

        public float DamageMultiplier => damageMultiplier;

        public float StartupSeconds => startupSeconds;

        public float ActiveSeconds => activeSeconds;

        public float RecoverySeconds => recoverySeconds;

        public float AnimationDurationSeconds => animationDurationSeconds;

        public float HitStopSeconds => hitStopSeconds;

        public bool BreaksGuard => breaksGuard;

        public float BlockStunSeconds => Mathf.Max(0f, blockStunSeconds);

        public float GuardBreakHitStunSeconds => Mathf.Max(0f, guardBreakHitStunSeconds);

        public float ForwardMovement => forwardMovement;

        public float MovementSpeedScale => movementSpeedScale > 0f ? Mathf.Clamp(movementSpeedScale, 0.1f, 1.25f) : 1f;

        public float Range => range;

        public float Radius => radius;

        public AttackHitboxShape HitboxShape => hitboxShape;

        public Vector3 HitboxLocalCenter => hitboxLocalCenter;

        public Vector3 HitboxHalfExtents => hitboxHalfExtents;

        public float HitboxRadius => hitboxRadius;

        public AttackHitboxActivationMode HitboxActivationMode => hitboxActivationMode;

        public GameObject ProjectilePrefab => projectilePrefab;

        public float ProjectileSpeed => projectileSpeed;

        public float ProjectileLifetimeSeconds => projectileLifetimeSeconds;

        public float ProjectileSpawnOffset => projectileSpawnOffset;

        public ProjectileTrajectoryMode ProjectileTrajectoryMode => projectileTrajectoryMode;

        public float ProjectileArcHeight => projectileArcHeight;

        public EnemyTargetResponseType EnemyTargetResponse => enemyTargetResponse;
    }
}
