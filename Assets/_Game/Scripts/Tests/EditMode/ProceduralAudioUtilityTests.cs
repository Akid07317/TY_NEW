using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Core;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ProceduralAudioUtilityTests
    {
        [Test]
        public void ResolveSfxVolume_UsesActiveSceneAudioSettings()
        {
            GameObject contextObject = null;
            AudioSettingsSO audioSettings = null;

            try
            {
                contextObject = new GameObject("SceneRuntimeContext");
                SceneRuntimeContext sceneContext = contextObject.AddComponent<SceneRuntimeContext>();
                audioSettings = ScriptableObject.CreateInstance<AudioSettingsSO>();
                SetPrivateField(audioSettings, "masterVolume", 0.5f);
                SetPrivateField(audioSettings, "sfxVolume", 0.4f);
                SetPrivateField(sceneContext, "audioSettings", audioSettings);
                SetActiveContext(sceneContext);

                Assert.AreEqual(0.1f, ProceduralAudioUtility.ResolveSfxVolume(0.5f), 0.001f);
            }
            finally
            {
                SetActiveContext(null);

                if (audioSettings != null)
                {
                    Object.DestroyImmediate(audioSettings);
                }

                if (contextObject != null)
                {
                    Object.DestroyImmediate(contextObject);
                }
            }
        }

        [Test]
        public void ResolveSfxVolume_FallsBackToRequestedVolume_WithoutActiveContext()
        {
            SetActiveContext(null);
            Assert.AreEqual(0.35f, ProceduralAudioUtility.ResolveSfxVolume(0.35f), 0.001f);
        }

        [Test]
        public void ResolveEvasiveActionCue_DifferentiatesRollAndAirDodge()
        {
            ProceduralActionAudioPlan rollPlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.CombatRoll);
            ProceduralActionAudioPlan airDodgePlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.AirDodge);

            Assert.IsTrue(rollPlan.HasAudio);
            Assert.AreEqual("CombatRoll", rollPlan.CueId);
            Assert.AreEqual(ProceduralActionAudioMixGroup.Movement, rollPlan.MixGroup);
            Assert.AreEqual(ProceduralAudioUtility.ActionAudioPriorityMovement, rollPlan.Priority);
            Assert.Greater(rollPlan.DominanceSeconds, 0f);
            Assert.Greater(rollPlan.CooldownSeconds, rollPlan.DurationSeconds);
            Assert.Less(rollPlan.SpatialBlend, 0.6f);
            Assert.Greater(rollPlan.MaxDistance, rollPlan.MinDistance);
            Assert.Greater(rollPlan.StartFrequency, rollPlan.EndFrequency);
            Assert.IsTrue(airDodgePlan.HasAudio);
            Assert.AreEqual("AirDodge", airDodgePlan.CueId);
            Assert.AreEqual(ProceduralActionAudioMixGroup.Movement, airDodgePlan.MixGroup);
            Assert.AreEqual(rollPlan.Priority, airDodgePlan.Priority);
            Assert.GreaterOrEqual(airDodgePlan.CooldownSeconds, airDodgePlan.DurationSeconds);
            Assert.Greater(airDodgePlan.EndFrequency, airDodgePlan.StartFrequency);
            Assert.IsFalse(ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.GroundDodge).HasAudio);
        }

        [Test]
        public void ResolvePlayerAttackCue_UsesSwordArtIdentity()
        {
            AttackDefinitionSO crossStepAttack = null;
            AttackDefinitionSO fallingStarAttack = null;
            AttackDefinitionSO moonSeverAttack = null;
            AttackDefinitionSO lightAttack = null;

            try
            {
                crossStepAttack = CreateAttack("SwordArt_CrossStep", "Cross Step", "SwordArt_CrossStep");
                fallingStarAttack = CreateAttack("SwordArt_FallingStar", "Falling Star", "SwordArt_FallingStar");
                moonSeverAttack = CreateAttack("SwordArt_MoonSever", "Moon Sever", "SwordArt_MoonSever");
                lightAttack = CreateAttack("Light_01", "Light", "Light_01");

                ProceduralActionAudioPlan crossStepPlan = ProceduralAudioUtility.ResolvePlayerAttackCue(
                    crossStepAttack);
                ProceduralActionAudioPlan fallingStarPlan = ProceduralAudioUtility.ResolvePlayerAttackCue(
                    fallingStarAttack);
                ProceduralActionAudioPlan moonSeverPlan = ProceduralAudioUtility.ResolvePlayerAttackCue(
                    moonSeverAttack);

                Assert.IsTrue(crossStepPlan.HasAudio);
                Assert.AreEqual("CrossStep", crossStepPlan.CueId);
                Assert.IsTrue(fallingStarPlan.HasAudio);
                Assert.AreEqual("FallingStar", fallingStarPlan.CueId);
                Assert.IsTrue(moonSeverPlan.HasAudio);
                Assert.AreEqual("MoonSever", moonSeverPlan.CueId);
                Assert.AreEqual(ProceduralActionAudioMixGroup.SwordArt, crossStepPlan.MixGroup);
                Assert.AreEqual(ProceduralActionAudioMixGroup.SwordArt, fallingStarPlan.MixGroup);
                Assert.AreEqual(ProceduralActionAudioMixGroup.SwordArt, moonSeverPlan.MixGroup);
                Assert.AreEqual(ProceduralAudioUtility.ActionAudioPrioritySwordArt, crossStepPlan.Priority);
                Assert.AreEqual(ProceduralAudioUtility.ActionAudioPriorityHeavyRead, fallingStarPlan.Priority);
                Assert.AreEqual(ProceduralAudioUtility.ActionAudioPrioritySwordArt, moonSeverPlan.Priority);
                Assert.Greater(fallingStarPlan.DominanceSeconds, crossStepPlan.DominanceSeconds);
                Assert.Greater(fallingStarPlan.Volume, crossStepPlan.Volume);
                Assert.Greater(fallingStarPlan.DurationSeconds, crossStepPlan.DurationSeconds);
                Assert.Greater(fallingStarPlan.CooldownSeconds, crossStepPlan.CooldownSeconds);
                Assert.GreaterOrEqual(moonSeverPlan.CooldownSeconds, moonSeverPlan.DurationSeconds);
                Assert.Greater(moonSeverPlan.MaxDistance, moonSeverPlan.MinDistance);
                Assert.Greater(moonSeverPlan.StartFrequency, moonSeverPlan.EndFrequency);
                Assert.IsFalse(ProceduralAudioUtility.ResolvePlayerAttackCue(lightAttack).HasAudio);
            }
            finally
            {
                DestroyAttack(lightAttack);
                DestroyAttack(moonSeverAttack);
                DestroyAttack(fallingStarAttack);
                DestroyAttack(crossStepAttack);
            }
        }

        [Test]
        public void ResolvePlayerAttackCue_UsesGuardBreakFallback_ForBreakingAttacks()
        {
            AttackDefinitionSO gateSlamAttack = null;

            try
            {
                gateSlamAttack = CreateAttack("Enemy_Gatekeeper", "Gate Slam", "Enemy_Gatekeeper");
                SetPrivateField(gateSlamAttack, "breaksGuard", true);

                ProceduralActionAudioPlan plan = ProceduralAudioUtility.ResolvePlayerAttackCue(gateSlamAttack);

                Assert.IsTrue(plan.HasAudio);
                Assert.AreEqual("GuardBreakAttack", plan.CueId);
                Assert.AreEqual(ProceduralActionAudioMixGroup.Impact, plan.MixGroup);
                Assert.AreEqual(ProceduralAudioUtility.ActionAudioPriorityGuardBreak, plan.Priority);
                Assert.Greater(plan.CooldownSeconds, plan.DurationSeconds);
                Assert.Greater(plan.SpatialBlend, 0.6f);
                Assert.Greater(plan.StartFrequency, plan.EndFrequency);
            }
            finally
            {
                DestroyAttack(gateSlamAttack);
            }
        }

        [Test]
        public void ResolveHitReactionCue_OnlyPlaysForGuardBreak()
        {
            ProceduralActionAudioPlan guardBreakPlan = ProceduralAudioUtility.ResolveHitReactionCue(
                PlayerHitReactionType.GuardBreak);

            Assert.IsTrue(guardBreakPlan.HasAudio);
            Assert.AreEqual("GuardBreakHit", guardBreakPlan.CueId);
            Assert.AreEqual(ProceduralActionAudioMixGroup.Impact, guardBreakPlan.MixGroup);
            Assert.AreEqual(ProceduralAudioUtility.ActionAudioPriorityGuardBreak, guardBreakPlan.Priority);
            Assert.Greater(guardBreakPlan.CooldownSeconds, guardBreakPlan.DurationSeconds);
            Assert.Greater(guardBreakPlan.Volume, 0.3f);
            Assert.IsFalse(ProceduralAudioUtility.ResolveHitReactionCue(PlayerHitReactionType.Standard).HasAudio);
        }

        [Test]
        public void ResolveEnemyResponseCue_DifferentiatesAntiAirAndChaseRoll()
        {
            AttackDefinitionSO antiAirAttack = null;
            AttackDefinitionSO chaseRollAttack = null;
            AttackDefinitionSO normalAttack = null;

            try
            {
                antiAirAttack = CreateAttack("Enemy_Gatekeeper_SkyHook", "Sky Hook", "Enemy_Gatekeeper_SkyHook");
                chaseRollAttack = CreateAttack(
                    "Enemy_Gatekeeper_RollCatcher",
                    "Pursuit Slam",
                    "Enemy_Gatekeeper_RollCatcher");
                normalAttack = CreateAttack("Enemy_Gatekeeper_Reach", "Hall Sweep", "Enemy_Gatekeeper_Reach");
                SetPrivateField(antiAirAttack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);
                SetPrivateField(chaseRollAttack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

                ProceduralActionAudioPlan antiAirPlan = ProceduralAudioUtility.ResolveEnemyResponseCue(antiAirAttack);
                ProceduralActionAudioPlan chaseRollPlan = ProceduralAudioUtility.ResolveEnemyResponseCue(
                    chaseRollAttack);

                Assert.IsTrue(antiAirPlan.HasAudio);
                Assert.AreEqual("SkyHook", antiAirPlan.CueId);
                Assert.AreEqual(ProceduralActionAudioMixGroup.BossResponse, antiAirPlan.MixGroup);
                Assert.AreEqual(ProceduralAudioUtility.ActionAudioPriorityHeavyRead, antiAirPlan.Priority);
                Assert.Greater(antiAirPlan.SpatialBlend, 0.8f);
                Assert.Greater(antiAirPlan.CooldownSeconds, antiAirPlan.DurationSeconds);
                Assert.Greater(antiAirPlan.EndFrequency, antiAirPlan.StartFrequency);
                Assert.IsTrue(chaseRollPlan.HasAudio);
                Assert.AreEqual("PursuitSlam", chaseRollPlan.CueId);
                Assert.AreEqual(ProceduralActionAudioMixGroup.BossResponse, chaseRollPlan.MixGroup);
                Assert.AreEqual(antiAirPlan.Priority, chaseRollPlan.Priority);
                Assert.Greater(chaseRollPlan.SpatialBlend, antiAirPlan.SpatialBlend - 0.01f);
                Assert.Greater(chaseRollPlan.StartFrequency, chaseRollPlan.EndFrequency);
                Assert.IsFalse(ProceduralAudioUtility.ResolveEnemyResponseCue(normalAttack).HasAudio);
            }
            finally
            {
                DestroyAttack(normalAttack);
                DestroyAttack(chaseRollAttack);
                DestroyAttack(antiAirAttack);
            }
        }

        [Test]
        public void CanPlayActionCue_RejectsRepeatInsideCooldown()
        {
            ProceduralActionAudioPlan plan = new ProceduralActionAudioPlan(
                "RepeatedSwordArt",
                800f,
                420f,
                0.08f,
                0.22f,
                ProceduralActionAudioMixGroup.SwordArt,
                0.16f,
                0.55f,
                1.5f,
                14f);

            Assert.IsTrue(ProceduralAudioUtility.CanPlayActionCue(plan, -1f, 3f));
            Assert.IsFalse(ProceduralAudioUtility.CanPlayActionCue(plan, 3f, 3.08f));
            Assert.IsTrue(ProceduralAudioUtility.CanPlayActionCue(plan, 3f, 3.16f));
            Assert.IsFalse(ProceduralAudioUtility.CanPlayActionCue(ProceduralActionAudioPlan.None, -1f, 3f));
        }

        [Test]
        public void CanPassActionCueDominance_RejectsMovementCueDuringHeavyReadWindow()
        {
            ProceduralActionAudioPlan rollPlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.CombatRoll);
            ProceduralActionAudioPlan pursuitSlamPlan = new ProceduralActionAudioPlan(
                "PursuitSlam",
                310f,
                95f,
                0.13f,
                0.32f,
                ProceduralActionAudioMixGroup.BossResponse,
                0.24f,
                0.85f,
                2f,
                18f,
                ProceduralAudioUtility.ActionAudioPriorityHeavyRead,
                0.12f);

            Assert.IsFalse(ProceduralAudioUtility.CanPlayActionCue(
                rollPlan,
                -1f,
                4.05f,
                pursuitSlamPlan.Priority,
                4.12f));
            Assert.IsTrue(ProceduralAudioUtility.CanPlayActionCue(
                rollPlan,
                -1f,
                4.13f,
                pursuitSlamPlan.Priority,
                4.12f));
        }

        [Test]
        public void CanPassActionCueDominance_AllowsHigherPriorityCueToInterruptMovementWindow()
        {
            ProceduralActionAudioPlan rollPlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.CombatRoll);
            ProceduralActionAudioPlan guardBreakPlan = ProceduralAudioUtility.ResolveHitReactionCue(
                PlayerHitReactionType.GuardBreak);

            Assert.IsTrue(ProceduralAudioUtility.CanPlayActionCue(
                guardBreakPlan,
                -1f,
                7.02f,
                rollPlan.Priority,
                7.04f));
        }

        [Test]
        public void EvaluateActionCueDecision_ReportsPlayCooldownAndDominanceReasons()
        {
            ProceduralActionAudioPlan rollPlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.CombatRoll);
            ProceduralActionAudioPlan pursuitSlamPlan = new ProceduralActionAudioPlan(
                "PursuitSlam",
                310f,
                95f,
                0.13f,
                0.32f,
                ProceduralActionAudioMixGroup.BossResponse,
                0.24f,
                0.85f,
                2f,
                18f,
                ProceduralAudioUtility.ActionAudioPriorityHeavyRead,
                0.12f);

            ProceduralActionAudioDecision playedDecision = ProceduralAudioUtility.EvaluateActionCueDecision(
                pursuitSlamPlan,
                -1f,
                3f,
                0,
                0f,
                0.32f);
            ProceduralActionAudioDecision cooldownDecision = ProceduralAudioUtility.EvaluateActionCueDecision(
                rollPlan,
                5f,
                5.04f,
                0,
                0f,
                0.2f);
            ProceduralActionAudioDecision dominanceDecision = ProceduralAudioUtility.EvaluateActionCueDecision(
                rollPlan,
                -1f,
                6.04f,
                pursuitSlamPlan.Priority,
                6.12f,
                0.2f);
            ProceduralActionAudioDecision mutedDecision = ProceduralAudioUtility.EvaluateActionCueDecision(
                rollPlan,
                -1f,
                7f,
                0,
                0f,
                0f);
            ProceduralActionAudioDecision batchDecision = ProceduralAudioUtility.EvaluateActionCueDecision(
                rollPlan,
                -1f,
                8f,
                0,
                0f,
                0.2f,
                isBatchMode: true);

            Assert.AreEqual(ProceduralActionAudioDecisionKind.Played, playedDecision.Kind);
            Assert.IsTrue(playedDecision.ShouldPlay);
            Assert.AreEqual("PursuitSlam", playedDecision.CueId);
            Assert.AreEqual(pursuitSlamPlan.Priority, playedDecision.Priority);
            Assert.AreEqual(3.12f, playedDecision.BlockUntilSeconds, 0.001f);
            Assert.AreEqual(ProceduralActionAudioDecisionKind.Cooldown, cooldownDecision.Kind);
            Assert.AreEqual(5f + rollPlan.CooldownSeconds, cooldownDecision.BlockUntilSeconds, 0.001f);
            Assert.Greater(cooldownDecision.SecondsRemaining, 0f);
            Assert.AreEqual(ProceduralActionAudioDecisionKind.DominanceBlocked, dominanceDecision.Kind);
            Assert.AreEqual(pursuitSlamPlan.Priority, dominanceDecision.ActiveDominantPriority);
            Assert.AreEqual(6.12f, dominanceDecision.BlockUntilSeconds, 0.001f);
            Assert.AreEqual(ProceduralActionAudioDecisionKind.Muted, mutedDecision.Kind);
            Assert.AreEqual(ProceduralActionAudioDecisionKind.BatchMode, batchDecision.Kind);
        }

        [Test]
        public void TryPlayActionCue_HandlesNoAudioPlanWithoutDictionaryLookup()
        {
            ProceduralAudioUtility.ResetActionCueStateForTests();

            Assert.IsFalse(ProceduralAudioUtility.TryPlayActionCue(
                Vector3.zero,
                ProceduralActionAudioPlan.None));
            Assert.AreEqual(
                ProceduralActionAudioDecisionKind.NoAudio,
                ProceduralAudioUtility.LastActionCueDecision.Kind);
            Assert.IsFalse(ProceduralAudioUtility.LastActionCueDecision.IsVisible);
        }

        [Test]
        public void ResolveUnityAudioSourcePriority_MapsImportantCuesToHigherUnityPriority()
        {
            int movementPriority = ProceduralAudioUtility.ResolveUnityAudioSourcePriority(
                ProceduralAudioUtility.ActionAudioPriorityMovement);
            int swordArtPriority = ProceduralAudioUtility.ResolveUnityAudioSourcePriority(
                ProceduralAudioUtility.ActionAudioPrioritySwordArt);
            int guardBreakPriority = ProceduralAudioUtility.ResolveUnityAudioSourcePriority(
                ProceduralAudioUtility.ActionAudioPriorityGuardBreak);

            Assert.Less(guardBreakPriority, swordArtPriority);
            Assert.Less(swordArtPriority, movementPriority);
            Assert.Greater(movementPriority, 0);
            Assert.LessOrEqual(guardBreakPriority, 80);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static AttackDefinitionSO CreateAttack(string attackId, string displayName, string animationStateName)
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SetPrivateField(attack, "attackId", attackId);
            SetPrivateField(attack, "displayName", displayName);
            SetPrivateField(attack, "animationStateName", animationStateName);
            return attack;
        }

        private static void DestroyAttack(AttackDefinitionSO attack)
        {
            if (attack != null)
            {
                Object.DestroyImmediate(attack);
            }
        }

        private static void SetActiveContext(SceneRuntimeContext context)
        {
            PropertyInfo property = typeof(SceneRuntimeContext).GetProperty(
                "Active",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property);
            property.SetValue(null, context);
        }
    }
}
