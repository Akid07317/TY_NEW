using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEditor;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestEnemyAssetWiringTests
    {
        private const string GatekeeperArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Gatekeeper.asset";
        private const string GatekeeperBurstAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Burst.asset";
        private const string GatekeeperArcAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Arc.asset";

        [Test]
        public void GatekeeperArchetype_UsesTwoDistinctRangedAttacks()
        {
            EnemyArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(GatekeeperArchetypePath);
            AttackDefinitionSO burstAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperBurstAttackPath);
            AttackDefinitionSO arcAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperArcAttackPath);

            Assert.IsNotNull(archetype);
            Assert.IsNotNull(burstAttack);
            Assert.IsNotNull(arcAttack);
            Assert.That(archetype.Attacks, Has.Length.EqualTo(4));
            Assert.AreSame(burstAttack, archetype.Attacks[2]);
            Assert.AreSame(arcAttack, archetype.Attacks[3]);
            Assert.IsNotNull(burstAttack.ProjectilePrefab);
            Assert.IsNotNull(arcAttack.ProjectilePrefab);
            Assert.AreEqual(ProjectileTrajectoryMode.Straight, burstAttack.ProjectileTrajectoryMode);
            Assert.AreEqual(ProjectileTrajectoryMode.Arc, arcAttack.ProjectileTrajectoryMode);
            Assert.Less(burstAttack.Range, arcAttack.Range);
            Assert.Less(burstAttack.StartupSeconds, arcAttack.StartupSeconds);
        }
    }
}
