using System.Collections.Generic;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.UI;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatDebugHudActionFeedbackUtilityTests
    {
        [Test]
        public void BuildDebugHudLayout_ClampsWidthAndStopsBeforeSwordArtHud()
        {
            CombatDebugHudLayout narrowLayout = CombatDebugHudLayoutUtility.Build(240f, 240f);
            Rect firstLineRect = narrowLayout.BuildLineRect(narrowLayout.TopY);

            Assert.That(firstLineRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(firstLineRect.xMax, Is.LessThanOrEqualTo(240f));

            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            SwordArtHudLayout swordArtLayout = SwordArtHudLayoutUtility.Build(360f, 240f);

            Assert.IsTrue(shortLayout.CanDrawLine(shortLayout.TopY));
            Assert.IsFalse(shortLayout.CanDrawLine(swordArtLayout.PanelRect.yMin));
            Assert.That(shortLayout.MaxY, Is.LessThanOrEqualTo(swordArtLayout.PanelRect.yMin - 12f));

            int overflowingLineCount = shortLayout.MaxVisibleLineCount + 3;

            Assert.That(shortLayout.GetContentLineCount(overflowingLineCount), Is.EqualTo(shortLayout.MaxVisibleLineCount - 1));
            Assert.That(shortLayout.GetHiddenLineCount(overflowingLineCount), Is.EqualTo(4));
            Assert.AreEqual("+4 debug lines hidden", CombatDebugHudLayoutUtility.BuildOverflowLine(4));

            Rect panelRect = CombatDebugHudLayoutUtility.BuildPanelRect(shortLayout, overflowingLineCount);
            Rect firstShortLineRect = shortLayout.BuildLineRect(shortLayout.TopY);
            Assert.That(panelRect.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panelRect.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panelRect.xMax, Is.LessThanOrEqualTo(360f));
            Assert.That(panelRect.yMax, Is.LessThanOrEqualTo(swordArtLayout.PanelRect.yMin - 12f));
            Assert.That(panelRect.Contains(new Vector2(firstShortLineRect.xMin, firstShortLineRect.yMin)), Is.True);
            Assert.That(panelRect.Contains(new Vector2(firstShortLineRect.xMax, firstShortLineRect.yMax)), Is.True);
        }

        [Test]
        public void BuildDebugHudLayout_KeepsTargetAnimationInsideShortViewPriority()
        {
            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            List<string> prioritizedLines = new List<string>
            {
                "<b>State</b>: PlayerLocomotionState",
                "Anim Clip: Light_01 @ 0.42x w1.00",
                "Action Cue: Cross Step Ready - roll counter",
                "Target Anim: Attack_AntiAir @ 0.18x w1.00",
                "Boss Cue: Anti-Air Incoming - Sky Hook (Land or guard; avoid air hang)",
                "Lock Target: Gatekeeper",
                "Target HP: 180/180",
                "HP: 100/100",
                "MP: 50/50"
            };

            int contentLineCount = shortLayout.GetContentLineCount(prioritizedLines.Count);
            int targetAnimationIndex = prioritizedLines.IndexOf("Target Anim: Attack_AntiAir @ 0.18x w1.00");

            Assert.That(contentLineCount, Is.GreaterThan(0));
            Assert.That(targetAnimationIndex, Is.LessThan(contentLineCount));
            Assert.That(targetAnimationIndex, Is.LessThan(prioritizedLines.IndexOf("Lock Target: Gatekeeper")));
            Assert.That(targetAnimationIndex, Is.LessThan(prioritizedLines.IndexOf("Target HP: 180/180")));
            Assert.That(shortLayout.GetHiddenLineCount(prioritizedLines.Count), Is.GreaterThan(0));
        }

        [Test]
        public void BuildDebugHudLayout_KeepsAttackPhaseAndTargetAnimationInsideShortViewPriority()
        {
            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            List<string> prioritizedLines = new List<string>
            {
                "<b>State</b>: PlayerAttackState",
                "Anim Clip: SwordArt_MoonSever @ 0.42x w1.00",
                "Atk: MoonSever Act 0.25/0.72 hit .20-.32",
                "Target Anim: Attack_ChaseRoll @ 0.18x w1.00",
                "Action Cue: Moon Sever - air dodge slash",
                "Boss Cue: Roll Catch Incoming - Pursuit Slam (Delay dodge; lane catches rolls)",
                "Lock Target: Gatekeeper",
                "Target HP: 180/180",
                "HP: 100/100"
            };

            int contentLineCount = shortLayout.GetContentLineCount(prioritizedLines.Count);
            int attackPhaseIndex = prioritizedLines.IndexOf("Atk: MoonSever Act 0.25/0.72 hit .20-.32");
            int targetAnimationIndex = prioritizedLines.IndexOf("Target Anim: Attack_ChaseRoll @ 0.18x w1.00");

            Assert.That(contentLineCount, Is.GreaterThan(0));
            Assert.That(attackPhaseIndex, Is.LessThan(contentLineCount));
            Assert.That(targetAnimationIndex, Is.LessThan(contentLineCount));
            Assert.That(attackPhaseIndex, Is.LessThan(prioritizedLines.IndexOf("Action Cue: Moon Sever - air dodge slash")));
            Assert.That(targetAnimationIndex, Is.LessThan(prioritizedLines.IndexOf("Lock Target: Gatekeeper")));
            Assert.That(shortLayout.GetHiddenLineCount(prioritizedLines.Count), Is.GreaterThan(0));
        }

        [Test]
        public void BuildDebugHudLayout_KeepsTargetAttackTimingInsideShortViewPriority()
        {
            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            List<string> prioritizedLines = new List<string>
            {
                "<b>State</b>: PlayerAttackState",
                "Atk: MoonSever Act 0.25/0.72 hit .20-.32",
                "Target Anim: Attack_ChaseRoll @ 0.18x w1.00",
                "Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40",
                "Boss Cue: Roll Catch Incoming - Pursuit Slam (Delay dodge; lane catches rolls)",
                "Anim Clip: SwordArt_MoonSever @ 0.42x w1.00",
                "Action Cue: Moon Sever - air dodge slash",
                "Lock Target: Gatekeeper",
                "Target HP: 180/180",
                "HP: 100/100"
            };

            int contentLineCount = shortLayout.GetContentLineCount(prioritizedLines.Count);
            int targetAttackTimingIndex = prioritizedLines.IndexOf("Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40");

            Assert.That(contentLineCount, Is.GreaterThan(0));
            Assert.That(targetAttackTimingIndex, Is.LessThan(contentLineCount));
            int bossCueIndex = prioritizedLines.IndexOf(
                "Boss Cue: Roll Catch Incoming - Pursuit Slam (Delay dodge; lane catches rolls)");

            Assert.That(targetAttackTimingIndex, Is.LessThan(bossCueIndex));
            Assert.That(targetAttackTimingIndex, Is.LessThan(prioritizedLines.IndexOf("Anim Clip: SwordArt_MoonSever @ 0.42x w1.00")));
            Assert.That(shortLayout.GetHiddenLineCount(prioritizedLines.Count), Is.GreaterThan(0));
        }

        [Test]
        public void BuildDebugHudLayout_KeepsCoreReadLinesAheadOfSfxDecisionInShortView()
        {
            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            List<string> prioritizedLines = new List<string>
            {
                "<b>State</b>: PlayerAttackState",
                "Atk: MoonSever Act 0.25/0.72 hit .20-.32",
                "Target Anim: Attack_ChaseRoll @ 0.18x w1.00",
                "Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40",
                "Boss Cue: Roll Catch Incoming - Pursuit Slam (Delay dodge; lane catches rolls)",
                "Lock Target: Gatekeeper",
                "Target HP: 180/180",
                "Anim Clip: SwordArt_MoonSever @ 0.42x w1.00",
                "Action Cue: Moon Sever - air dodge slash",
                "SFX: Roll held p30 0.07s",
                "HP: 100/100"
            };

            int contentLineCount = shortLayout.GetContentLineCount(prioritizedLines.Count);
            int attackPhaseIndex = prioritizedLines.IndexOf("Atk: MoonSever Act 0.25/0.72 hit .20-.32");
            int targetAnimationIndex = prioritizedLines.IndexOf("Target Anim: Attack_ChaseRoll @ 0.18x w1.00");
            int targetAttackTimingIndex = prioritizedLines.IndexOf("Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40");
            int sfxDecisionIndex = prioritizedLines.IndexOf("SFX: Roll held p30 0.07s");

            Assert.That(contentLineCount, Is.GreaterThan(0));
            Assert.That(attackPhaseIndex, Is.LessThan(contentLineCount));
            Assert.That(targetAnimationIndex, Is.LessThan(contentLineCount));
            Assert.That(targetAttackTimingIndex, Is.LessThan(contentLineCount));
            Assert.That(sfxDecisionIndex, Is.GreaterThan(targetAttackTimingIndex));
            Assert.That(sfxDecisionIndex, Is.GreaterThanOrEqualTo(contentLineCount));
            Assert.That(shortLayout.GetHiddenLineCount(prioritizedLines.Count), Is.GreaterThan(0));
        }

        [Test]
        public void BuildDebugHudLayout_KeepsCompactBossCueReadableInShortView()
        {
            CombatDebugHudLayout shortLayout = CombatDebugHudLayoutUtility.Build(360f, 240f);
            const string compactBossCue = "Boss: RollCatch PursuitSlam - delay dodge";
            List<string> prioritizedLines = new List<string>
            {
                "Atk: MoonSever Act 0.25/0.72 hit .20-.32",
                "Target Anim: Attack_ChaseRoll @ 0.18x w1.00",
                "Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40",
                compactBossCue,
                "<b>State</b>: PlayerAttackState",
                "Lock Target: Gatekeeper",
                "Target HP: 180/180",
                "Anim Clip: SwordArt_MoonSever @ 0.42x w1.00",
                "Action Cue: Moon Sever - air dodge slash",
                "SFX: Roll held p30 0.07s",
                "HP: 100/100"
            };

            int contentLineCount = shortLayout.GetContentLineCount(prioritizedLines.Count);
            int compactBossCueIndex = prioritizedLines.IndexOf(compactBossCue);

            Assert.That(compactBossCue.Length, Is.LessThanOrEqualTo(48));
            Assert.That(compactBossCueIndex, Is.LessThan(contentLineCount));
            Assert.That(
                compactBossCueIndex,
                Is.GreaterThan(prioritizedLines.IndexOf("Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40")));
            Assert.That(shortLayout.GetHiddenLineCount(prioritizedLines.Count), Is.GreaterThan(0));
        }

        [Test]
        public void BuildActionAudioFeedbackLine_ReportsPlayCooldownAndDominanceState()
        {
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
            ProceduralActionAudioPlan rollPlan = ProceduralAudioUtility.ResolveEvasiveActionCue(
                PlayerEvasiveActionType.CombatRoll);

            ProceduralActionAudioDecision playedDecision = new ProceduralActionAudioDecision(
                ProceduralActionAudioDecisionKind.Played,
                pursuitSlamPlan,
                10f,
                10.12f);
            ProceduralActionAudioDecision cooldownDecision = new ProceduralActionAudioDecision(
                ProceduralActionAudioDecisionKind.Cooldown,
                rollPlan,
                11f,
                11.08f);
            ProceduralActionAudioDecision dominanceDecision = new ProceduralActionAudioDecision(
                ProceduralActionAudioDecisionKind.DominanceBlocked,
                rollPlan,
                12f,
                12.07f,
                pursuitSlamPlan.Priority);

            Assert.AreEqual(
                "SFX: PursuitSlam play p30 BossResponse",
                CombatDebugHudActionFeedbackUtility.BuildActionAudioFeedbackLine(playedDecision, 10.05f));
            Assert.AreEqual(
                "SFX: Roll cd 0.08s",
                CombatDebugHudActionFeedbackUtility.BuildActionAudioFeedbackLine(cooldownDecision, 11.02f));
            Assert.AreEqual(
                "SFX: Roll held p30 0.07s",
                CombatDebugHudActionFeedbackUtility.BuildActionAudioFeedbackLine(dominanceDecision, 12.01f));
            Assert.AreEqual(
                string.Empty,
                CombatDebugHudActionFeedbackUtility.BuildActionAudioFeedbackLine(playedDecision, 11.5f));
        }

        [Test]
        public void BuildDebugHudCollapsedHint_StaysSmallAndScreenBound()
        {
            Rect narrowPanel = CombatDebugHudLayoutUtility.BuildCollapsedPanelRect(120f);
            Rect narrowLabel = CombatDebugHudLayoutUtility.BuildCollapsedLabelRect(narrowPanel);

            Assert.That(narrowPanel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(narrowPanel.xMax, Is.LessThanOrEqualTo(120f));
            Assert.That(narrowLabel.xMin, Is.GreaterThanOrEqualTo(narrowPanel.xMin));
            Assert.That(narrowLabel.xMax, Is.LessThanOrEqualTo(narrowPanel.xMax));

            Rect regularPanel = CombatDebugHudLayoutUtility.BuildCollapsedPanelRect(1280f);

            Assert.That(regularPanel.width, Is.LessThanOrEqualTo(190f));
            Assert.That(regularPanel.height, Is.LessThan(40f));
        }

        [Test]
        public void DebugHudToggleInput_AdvertisesMacSafeFallbackShortcut()
        {
            Assert.IsFalse(CombatDebugHudInputUtility.ShouldToggleDebugPanel(null));
            StringAssert.Contains("F1", CombatDebugHudInputUtility.ToggleShortcutLabel);
            StringAssert.Contains("`", CombatDebugHudInputUtility.ToggleShortcutLabel);
            StringAssert.Contains("F1", CombatDebugHudInputUtility.CollapsedHintLabel);
            StringAssert.Contains("`", CombatDebugHudInputUtility.CollapsedHintLabel);
        }

        [Test]
        public void BuildAnimatorClipLine_ReportsReadableClipTimingAndBlend()
        {
            Assert.AreEqual(
                string.Empty,
                CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(string.Empty, 0.4f, 1f));

            Assert.AreEqual(
                "Light_01",
                CombatDebugHudAnimatorClipUtility.ShortenClipName("AN_Player_Light_01_CombatTest"));

            Assert.AreEqual(
                "SwordArt_MoonSever",
                CombatDebugHudAnimatorClipUtility.ShortenClipName("AN_Player_SwordArt_MoonSever_CombatTest"));

            Assert.AreEqual(
                "Attack_AntiAir",
                CombatDebugHudAnimatorClipUtility.ShortenClipName("AN_Enemy_Attack_AntiAir_CombatTest"));

            Assert.AreEqual(
                "Anim Clip: SwordArt_MoonSever @ 0.42x w0.75 blend",
                CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(
                    "AN_Player_SwordArt_MoonSever_CombatTest",
                    0.42f,
                    0.75f,
                    true));

            Assert.AreEqual(
                "Target Anim: Attack_ChaseRoll @ 0.18x w0.60 blend",
                CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(
                    "Target Anim",
                    "AN_Enemy_Attack_ChaseRoll_CombatTest",
                    0.18f,
                    0.6f,
                    true));

            Assert.AreEqual(
                "Anim Clip: Light_01 @ 1.20x w1.00",
                CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(
                    "AN_Player_Light_01_CombatTest",
                    1.2f,
                    1.8f));
        }

        [Test]
        public void BuildAttackTimingLine_ReportsStartupActiveRecoveryAndHitWindow()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO eventAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "displayName", "Cross Step");
                SetPrivateField(attack, "startupSeconds", 0.10f);
                SetPrivateField(attack, "activeSeconds", 0.08f);
                SetPrivateField(attack, "recoverySeconds", 0.34f);

                Assert.AreEqual(
                    "Attack Phase: Cross Step Startup 0.05/0.52s (hit 0.10-0.18s)",
                    CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(attack, 0.05f, 0.52f));

                Assert.AreEqual(
                    "Attack Phase: Cross Step Active 0.12/0.52s (hit 0.10-0.18s)",
                    CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(attack, 0.12f, 0.52f));

                Assert.AreEqual(
                    "Attack Phase: Cross Step Recovery 0.28/0.52s (hit 0.10-0.18s)",
                    CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(attack, 0.28f, 0.52f));

                Assert.AreEqual(
                    "Attack Phase: Cross Step Done 0.60/0.52s (hit 0.10-0.18s)",
                    CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(attack, 0.60f, 0.52f));

                SetPrivateField(eventAttack, "displayName", "Event Slash");
                SetPrivateField(eventAttack, "startupSeconds", 0.20f);
                SetPrivateField(eventAttack, "activeSeconds", 0.10f);
                SetPrivateField(eventAttack, "recoverySeconds", 0.40f);
                SetPrivateField(eventAttack, "hitboxActivationMode", AttackHitboxActivationMode.AnimationEvent);

                Assert.AreEqual(
                    "Attack Phase: Event Slash Active 0.25/0.70s (event hit)",
                    CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(eventAttack, 0.25f, 0.70f));
            }
            finally
            {
                Object.DestroyImmediate(eventAttack);
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void BuildAttackTimingLine_ReportsCompactLineForNarrowDebugHud()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO eventAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "displayName", "Moon Sever");
                SetPrivateField(attack, "startupSeconds", 0.20f);
                SetPrivateField(attack, "activeSeconds", 0.12f);
                SetPrivateField(attack, "recoverySeconds", 0.40f);

                string compactLine = CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(
                    attack,
                    0.25f,
                    0.72f,
                    compact: true);

                Assert.AreEqual("Atk: MoonSever Act 0.25/0.72 hit .20-.32", compactLine);
                Assert.That(compactLine.Length, Is.LessThanOrEqualTo(48));

                SetPrivateField(eventAttack, "displayName", "Event Slash");
                SetPrivateField(eventAttack, "startupSeconds", 0.20f);
                SetPrivateField(eventAttack, "activeSeconds", 0.10f);
                SetPrivateField(eventAttack, "recoverySeconds", 0.40f);
                SetPrivateField(eventAttack, "hitboxActivationMode", AttackHitboxActivationMode.AnimationEvent);

                string compactEventLine = CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(
                    eventAttack,
                    0.25f,
                    0.70f,
                    compact: true);

                Assert.AreEqual("Atk: EventSlash Act 0.25/0.70 event", compactEventLine);
                Assert.That(compactEventLine.Length, Is.LessThanOrEqualTo(48));
            }
            finally
            {
                Object.DestroyImmediate(eventAttack);
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void BuildAttackTimingLine_CompactsLongImportedNamesBeforeTimingDetails()
        {
            AttackDefinitionSO playerAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO targetAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(playerAttack, "displayName", "Imported Preview Sword Art Moon Sever Finisher Variant");
                SetPrivateField(playerAttack, "startupSeconds", 0.20f);
                SetPrivateField(playerAttack, "activeSeconds", 0.12f);
                SetPrivateField(playerAttack, "recoverySeconds", 0.40f);

                string playerLine = CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(
                    playerAttack,
                    0.25f,
                    0.72f,
                    compact: true);

                Assert.That(playerLine.Length, Is.LessThanOrEqualTo(48));
                Assert.That(playerLine, Does.StartWith("Atk: ImportedPrevie... Act "));
                Assert.That(playerLine, Does.EndWith("hit .20-.32"));

                SetPrivateField(targetAttack, "displayName", "Imported Preview Gatekeeper Pursuit Slam Roll Catcher Variant");
                SetPrivateField(targetAttack, "startupSeconds", 0.28f);
                SetPrivateField(targetAttack, "activeSeconds", 0.12f);
                SetPrivateField(targetAttack, "recoverySeconds", 0.44f);

                string targetLine = CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                    targetAttack,
                    EnemyAttackPresentationPhase.Startup,
                    0.5f,
                    compact: true);

                Assert.That(targetLine.Length, Is.LessThanOrEqualTo(48));
                Assert.That(targetLine, Does.StartWith("Tgt Atk: Imported... Start "));
                Assert.That(targetLine, Does.EndWith("hit .28-.40"));
            }
            finally
            {
                Object.DestroyImmediate(targetAttack);
                Object.DestroyImmediate(playerAttack);
            }
        }

        [Test]
        public void BuildTargetAttackTimingLine_ReportsEnemyPhaseAndHitWindow()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "displayName", "Pursuit Slam");
                SetPrivateField(attack, "startupSeconds", 0.28f);
                SetPrivateField(attack, "activeSeconds", 0.12f);
                SetPrivateField(attack, "recoverySeconds", 0.44f);

                Assert.AreEqual(
                    string.Empty,
                    CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                        null,
                        EnemyAttackPresentationPhase.Startup,
                        0.5f,
                        compact: true));

                Assert.AreEqual(
                    string.Empty,
                    CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                        attack,
                        EnemyAttackPresentationPhase.None,
                        0.5f,
                        compact: true));

                Assert.AreEqual(
                    "Tgt Atk: PursuitSlam Start 0.14/0.84 hit .28-.40",
                    CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                        attack,
                        EnemyAttackPresentationPhase.Startup,
                        0.5f,
                        compact: true));

                Assert.AreEqual(
                    "Tgt Atk: PursuitSlam Act 0.34/0.84 hit .28-.40",
                    CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                        attack,
                        EnemyAttackPresentationPhase.Advance,
                        0.5f,
                        compact: true));

                Assert.AreEqual(
                    "Target Attack: Pursuit Slam Recovery 0.62/0.84s (hit 0.28-0.40s)",
                    CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                        attack,
                        EnemyAttackPresentationPhase.Recovery,
                        0.5f));
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void BuildPlayerActionFeedbackLine_ReportsEvasiveActionsAndGuardBreak()
        {
            GameObject playerObject = new GameObject("Player");

            try
            {
                playerObject.transform.position = Vector3.up * 3f;
                playerObject.AddComponent<CharacterController>();
                PlayerMotor motor = playerObject.AddComponent<PlayerMotor>();
                PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
                PlayerStateMachine stateMachine = playerObject.AddComponent<PlayerStateMachine>();
                SetPrivateField(player, "motor", motor);
                SetPrivateField(player, "stateMachine", stateMachine);
                InvokeMethod(motor, "Awake");

                stateMachine.Initialize(player);
                stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);

                Assert.AreEqual(
                    "Action Cue: Combat Roll - commit, then counter",
                    CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(stateMachine, null));

                stateMachine.SwitchToLocomotion();
                stateMachine.SwitchToAirDodge();

                Assert.AreEqual(
                    "Action Cue: Air Dodge - one aerial follow-up",
                    CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(stateMachine, null));

                stateMachine.SwitchToHit(0.08f, PlayerHitReactionType.GuardBreak);

                Assert.AreEqual(
                    "Action Cue: Guard Break - recover, dodge slow heavies",
                    CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(stateMachine, null));
            }
            finally
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BuildPlayerActionFeedbackLine_ReportsSwordArtPreviewAndActiveArt()
        {
            GameObject playerObject = new GameObject("Player");
            AttackDefinitionSO crossStepAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO moonSeverAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            SwordArtDefinitionSO crossStep = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();
            SwordArtDefinitionSO moonSever = ScriptableObject.CreateInstance<SwordArtDefinitionSO>();

            try
            {
                AttackExecutor attackExecutor = playerObject.AddComponent<AttackExecutor>();
                HitboxController hitboxController = playerObject.AddComponent<HitboxController>();
                PlayerCombatController combatController = playerObject.AddComponent<PlayerCombatController>();
                SetPrivateField(hitboxController, "attackExecutor", attackExecutor);
                SetPrivateField(combatController, "attackExecutor", attackExecutor);
                SetPrivateField(combatController, "hitboxController", hitboxController);

                SetPrivateField(crossStep, "displayName", "Cross Step");
                SetPrivateField(crossStep, "attackDefinition", crossStepAttack);
                SetPrivateField(moonSever, "displayName", "Moon Sever");
                SetPrivateField(moonSever, "attackDefinition", moonSeverAttack);
                SetPrivateField(combatController, "previewSwordArt", crossStep);
                SetPrivateField(combatController, "previewSwordArtAttack", crossStepAttack);
                SetPrivateField(combatController, "previewSwordArtTimer", 0.5f);

                Assert.AreEqual(
                    "Action Cue: Cross Step Ready - roll counter",
                    CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(null, combatController));

                SetPrivateField(combatController, "currentSwordArt", moonSever);
                SetPrivateField(combatController, "currentSwordArtAttack", moonSeverAttack);
                SetPrivateField(combatController, "currentAttackDefinition", moonSeverAttack);

                Assert.AreEqual(
                    "Action Cue: Moon Sever - air dodge slash",
                    CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(null, combatController));
            }
            finally
            {
                Object.DestroyImmediate(moonSever);
                Object.DestroyImmediate(crossStep);
                Object.DestroyImmediate(moonSeverAttack);
                Object.DestroyImmediate(crossStepAttack);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void BuildBossResponseFeedbackLine_ReportsResponseAndGuardBreakOnlyDuringBossAttack()
        {
            EnemyArchetypeSO bossArchetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
            AttackDefinitionSO antiAirAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO chaseRollAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO guardBreakAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            GameObject bossObject = new GameObject("Boss_Gatekeeper");

            try
            {
                SetPrivateField(bossArchetype, "archetypeType", EnemyArchetypeType.Boss);
                SetPrivateField(bossArchetype, "attacks", new[] { antiAirAttack });
                SetPrivateField(antiAirAttack, "displayName", "Sky Hook");
                SetPrivateField(antiAirAttack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);
                SetPrivateField(chaseRollAttack, "displayName", "Pursuit Slam");
                SetPrivateField(chaseRollAttack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);
                SetPrivateField(guardBreakAttack, "displayName", "Gate Slam");
                SetPrivateField(guardBreakAttack, "breaksGuard", true);

                HealthComponent health = bossObject.AddComponent<HealthComponent>();
                health.SetMax(180f, true);
                EnemyAttackController attackController = bossObject.AddComponent<EnemyAttackController>();
                EnemyStateMachine stateMachine = bossObject.AddComponent<EnemyStateMachine>();
                EnemyBrain bossBrain = bossObject.AddComponent<EnemyBrain>();
                SetPrivateField(bossBrain, "archetype", bossArchetype);
                SetPrivateField(bossBrain, "health", health);
                SetPrivateField(bossBrain, "attackController", attackController);
                SetPrivateField(bossBrain, "stateMachine", stateMachine);

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyChaseState));

                Assert.AreEqual(
                    string.Empty,
                    CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(bossBrain));

                SetPrivateField(stateMachine, "currentStateName", nameof(EnemyAttackState));

                Assert.AreEqual(
                    "Boss Cue: Anti-Air Incoming - Sky Hook (Land or guard; avoid air hang)",
                    CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(bossBrain));
                Assert.AreEqual(
                    "Boss: AntiAir SkyHook - land/guard",
                    CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(bossBrain, compact: true));

                SetPrivateField(bossArchetype, "attacks", new[] { chaseRollAttack });

                Assert.AreEqual(
                    "Boss Cue: Roll Catch Incoming - Pursuit Slam (Delay dodge; lane catches rolls)",
                    CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(bossBrain));
                string compactRollCatchLine = CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(
                    bossBrain,
                    compact: true);
                Assert.AreEqual("Boss: RollCatch PursuitSlam - delay dodge", compactRollCatchLine);
                Assert.That(compactRollCatchLine.Length, Is.LessThanOrEqualTo(48));

                SetPrivateField(bossArchetype, "attacks", new[] { guardBreakAttack });

                Assert.AreEqual(
                    "Boss Cue: Guard Break Incoming - Gate Slam (Dodge heavy; guard breaks)",
                    CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(bossBrain));
                string compactGuardBreakLine = CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(
                    bossBrain,
                    compact: true);
                Assert.AreEqual("Boss: GuardBreak GateSlam - dodge; guard breaks", compactGuardBreakLine);
                Assert.That(compactGuardBreakLine.Length, Is.LessThanOrEqualTo(48));
            }
            finally
            {
                Object.DestroyImmediate(bossObject);
                Object.DestroyImmediate(guardBreakAttack);
                Object.DestroyImmediate(chaseRollAttack);
                Object.DestroyImmediate(antiAirAttack);
                Object.DestroyImmediate(bossArchetype);
            }
        }

        private static void InvokeMethod(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (method == null)
            {
                method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, arguments);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
