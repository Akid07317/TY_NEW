using CampusRPG.AI;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEditor;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestEnemyAssetWiringTests
    {
        private const string GatekeeperArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Gatekeeper.asset";
        private const string MeleeAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Melee.asset";
        private const string MobileAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Mobile.asset";
        private const string RangedAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Ranged.asset";
        private const string GatekeeperSlamAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper.asset";
        private const string GatekeeperReachAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Reach.asset";
        private const string GatekeeperBurstAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Burst.asset";
        private const string GatekeeperArcAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Arc.asset";
        private const string GatekeeperSkyHookAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_SkyHook.asset";
        private const string GatekeeperRollCatcherAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_RollCatcher.asset";
        private const string GatekeeperTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";

        [Test]
        public void GatekeeperArchetype_UsesDistinctRangedAttacks_AndTargetResponses()
        {
            EnemyArchetypeSO archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(GatekeeperArchetypePath);
            AttackDefinitionSO burstAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperBurstAttackPath);
            AttackDefinitionSO arcAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperArcAttackPath);
            AttackDefinitionSO antiAirAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperSkyHookAttackPath);
            AttackDefinitionSO rollCatcherAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(GatekeeperRollCatcherAttackPath);
            BossTelegraphStyleSO telegraphStyle = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(GatekeeperTelegraphStylePath);

            Assert.IsNotNull(archetype);
            Assert.IsNotNull(burstAttack);
            Assert.IsNotNull(arcAttack);
            Assert.IsNotNull(antiAirAttack);
            Assert.IsNotNull(rollCatcherAttack);
            Assert.IsNotNull(telegraphStyle);
            Assert.That(archetype.Attacks, Has.Length.EqualTo(6));
            Assert.AreSame(burstAttack, archetype.Attacks[2]);
            Assert.AreSame(arcAttack, archetype.Attacks[3]);
            Assert.AreSame(antiAirAttack, archetype.Attacks[4]);
            Assert.AreSame(rollCatcherAttack, archetype.Attacks[5]);
            Assert.IsNotNull(burstAttack.ProjectilePrefab);
            Assert.IsNotNull(arcAttack.ProjectilePrefab);
            Assert.IsNotNull(antiAirAttack.ProjectilePrefab);
            Assert.IsNull(rollCatcherAttack.ProjectilePrefab);
            Assert.AreEqual(ProjectileTrajectoryMode.Straight, burstAttack.ProjectileTrajectoryMode);
            Assert.AreEqual(ProjectileTrajectoryMode.Arc, arcAttack.ProjectileTrajectoryMode);
            Assert.AreEqual(ProjectileTrajectoryMode.Straight, antiAirAttack.ProjectileTrajectoryMode);
            Assert.AreEqual(EnemyTargetResponseType.AntiAir, antiAirAttack.EnemyTargetResponse);
            Assert.AreEqual(EnemyTargetResponseType.ChaseRoll, rollCatcherAttack.EnemyTargetResponse);
            Assert.Less(burstAttack.Range, arcAttack.Range);
            Assert.Less(burstAttack.StartupSeconds, arcAttack.StartupSeconds);
            Assert.Greater(antiAirAttack.Range, burstAttack.Range);
            Assert.Greater(antiAirAttack.ProjectileSpeed, burstAttack.ProjectileSpeed);
            Assert.Greater(rollCatcherAttack.Range, archetype.Attacks[0].Range);
            Assert.GreaterOrEqual(rollCatcherAttack.StartupSeconds, 0.28f);
            Assert.AreNotEqual(telegraphStyle.DefaultCueAccentColor, telegraphStyle.AntiAirCueAccentColor);
            Assert.AreNotEqual(telegraphStyle.DefaultCueAccentColor, telegraphStyle.ChaseRollCueAccentColor);
            Assert.AreNotEqual(telegraphStyle.AntiAirCueAccentColor, telegraphStyle.ChaseRollCueAccentColor);
        }

        [Test]
        public void Chapter01EnemyAttacks_CoverGuardDodgeAndMovementSolutions()
        {
            AttackDefinitionSO guardSwing = LoadAttack(MeleeAttackPath);
            AttackDefinitionSO feintDash = LoadAttack(MobileAttackPath);
            AttackDefinitionSO arcBolt = LoadAttack(RangedAttackPath);
            AttackDefinitionSO gateSlam = LoadAttack(GatekeeperSlamAttackPath);
            AttackDefinitionSO hallSweep = LoadAttack(GatekeeperReachAttackPath);
            AttackDefinitionSO gateLance = LoadAttack(GatekeeperBurstAttackPath);
            AttackDefinitionSO coreBolt = LoadAttack(GatekeeperArcAttackPath);
            AttackDefinitionSO skyHook = LoadAttack(GatekeeperSkyHookAttackPath);
            AttackDefinitionSO pursuitSlam = LoadAttack(GatekeeperRollCatcherAttackPath);

            Assert.AreEqual("Guard Swing", guardSwing.DisplayName);
            Assert.GreaterOrEqual(guardSwing.StartupSeconds, 0.16f);
            Assert.LessOrEqual(guardSwing.Range, 1.8f);
            Assert.LessOrEqual(guardSwing.Radius, 0.45f);
            Assert.AreEqual(AttackHitboxShape.Box, guardSwing.HitboxShape);
            Assert.IsFalse(guardSwing.BreaksGuard);
            Assert.GreaterOrEqual(guardSwing.BlockStunSeconds, 0.06f);

            Assert.AreEqual("Feint Dash", feintDash.DisplayName);
            Assert.LessOrEqual(feintDash.StartupSeconds, 0.14f);
            Assert.LessOrEqual(feintDash.Radius, guardSwing.Radius);
            Assert.GreaterOrEqual(feintDash.RecoverySeconds, 0.25f);

            Assert.AreEqual("Arc Bolt", arcBolt.DisplayName);
            Assert.IsNotNull(arcBolt.ProjectilePrefab);
            Assert.AreEqual(ProjectileTrajectoryMode.Arc, arcBolt.ProjectileTrajectoryMode);
            Assert.GreaterOrEqual(arcBolt.Range, 4f);
            Assert.AreEqual(0f, arcBolt.ForwardMovement, 0.001f);

            Assert.Greater(gateSlam.StartupSeconds, guardSwing.StartupSeconds);
            Assert.Greater(gateSlam.Range, guardSwing.Range);
            Assert.IsTrue(gateSlam.BreaksGuard);
            Assert.GreaterOrEqual(gateSlam.GuardBreakHitStunSeconds, 0.14f);
            Assert.Greater(hallSweep.StartupSeconds, gateSlam.StartupSeconds);
            Assert.Greater(hallSweep.Range, gateSlam.Range);
            Assert.IsFalse(hallSweep.BreaksGuard);
            Assert.GreaterOrEqual(hallSweep.BlockStunSeconds, guardSwing.BlockStunSeconds);

            Assert.AreEqual(ProjectileTrajectoryMode.Straight, gateLance.ProjectileTrajectoryMode);
            Assert.AreEqual(ProjectileTrajectoryMode.Arc, coreBolt.ProjectileTrajectoryMode);
            Assert.AreEqual(ProjectileTrajectoryMode.Straight, skyHook.ProjectileTrajectoryMode);
            Assert.AreEqual(EnemyTargetResponseType.AntiAir, skyHook.EnemyTargetResponse);
            Assert.AreEqual("Pursuit Slam", pursuitSlam.DisplayName);
            Assert.AreEqual(EnemyTargetResponseType.ChaseRoll, pursuitSlam.EnemyTargetResponse);
            Assert.IsNull(pursuitSlam.ProjectilePrefab);
            Assert.IsFalse(pursuitSlam.BreaksGuard);
            Assert.Greater(gateLance.ProjectileSpeed, coreBolt.ProjectileSpeed);
            Assert.Greater(coreBolt.ProjectileArcHeight, gateLance.ProjectileArcHeight);
            Assert.Greater(skyHook.ProjectileSpeed, gateLance.ProjectileSpeed);
            Assert.Greater(skyHook.Range, gateLance.Range);
            Assert.Greater(pursuitSlam.Range, gateSlam.Range);
            Assert.Greater(pursuitSlam.ForwardMovement, gateSlam.ForwardMovement);
            Assert.GreaterOrEqual(pursuitSlam.BlockStunSeconds, guardSwing.BlockStunSeconds);
        }

        private static AttackDefinitionSO LoadAttack(string assetPath)
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(assetPath);
            Assert.IsNotNull(attack, assetPath);
            return attack;
        }
    }
}
