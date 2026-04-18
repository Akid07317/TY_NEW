using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    public enum EnemyArchetypeType
    {
        Melee,
        Mobile,
        Ranged,
        Boss
    }

    [CreateAssetMenu(fileName = "SO_EnemyArchetype", menuName = "CampusRPG/AI/Enemy Archetype")]
    public sealed class EnemyArchetypeSO : ScriptableObject
    {
        [SerializeField] private EnemyArchetypeType archetypeType;
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private float baseAttack = 10f;
        [SerializeField] private float hitStunSeconds = 0.2f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float aggroDistance = 10f;
        [SerializeField] private float engageDurationSeconds;
        [SerializeField] private float attackDistance = 2f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float preferredCombatDistance = 1.5f;
        [SerializeField] private float strafeDistance = 1.25f;
        [SerializeField] private float strafeDurationSeconds = 0.45f;
        [SerializeField] private AttackDefinitionSO[] attacks = new AttackDefinitionSO[0];
        [SerializeField] private string dropTableId = "Default";

        public EnemyArchetypeType ArchetypeType => archetypeType;

        public float MaxHealth => maxHealth;

        public float BaseAttack => baseAttack;

        public float HitStunSeconds => hitStunSeconds;

        public float MoveSpeed => moveSpeed;

        public float AggroDistance => aggroDistance;

        public float EngageDurationSeconds => engageDurationSeconds;

        public float AttackDistance => attackDistance;

        public float AttackCooldown => attackCooldown;

        public float PreferredCombatDistance => preferredCombatDistance;

        public float StrafeDistance => strafeDistance;

        public float StrafeDurationSeconds => strafeDurationSeconds;

        public AttackDefinitionSO[] Attacks => attacks;

        public string DropTableId => dropTableId;
    }
}
