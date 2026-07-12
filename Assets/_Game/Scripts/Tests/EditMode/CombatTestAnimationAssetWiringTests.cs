using System.Reflection;
using System.Linq;
using CampusRPG.AI;
using CampusRPG.Editor;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestAnimationAssetWiringTests
    {
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private static readonly string[] ImportedSourceRoots =
        {
            "Assets/Kevin Iglesias/",
            "Assets/DoubleL/",
            "Assets/ithappy/",
            "Assets/JC_LP_MedievalCharacters_LITE/",
            "Assets/GhostSamurai_Animset/"
        };
        private static readonly string[] EnemyPrefabPaths =
        {
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab",
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab",
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab"
        };
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";
        private const string PlayerIdleClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Idle_CombatTest.anim";
        private const string PlayerBlockClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Block_CombatTest.anim";
        private const string PlayerDodgeClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Dodge_CombatTest.anim";
        private const string PlayerCombatRollClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_CombatRoll_CombatTest.anim";
        private const string PlayerAirDodgeClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_AirDodge_CombatTest.anim";
        private static readonly string[] PlayerBaselineClipPaths =
        {
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Idle_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Walk_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Walk_Backward_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Walk_Left_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Walk_Right_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_Backward_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_Left_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_Right_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_ForwardLeft_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_ForwardRight_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_BackwardLeft_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Run_BackwardRight_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Airborne_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Block_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Dodge_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_CombatRoll_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_AirDodge_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Hit_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_GuardBreak_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Death_CombatTest.anim"
        };
        private const string LightAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Light_01.asset";
        private const string HeavyAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string EnemyMeleeAttackAssetPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Melee.asset";
        private const string Light02ClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Light_02_CombatTest.anim";
        private const string Light02AttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Light_02.asset";
        private const string Light03AttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Light_03.asset";
        private const string CounterClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Counter_CombatTest.anim";
        private const string CounterAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Counter.asset";
        private const string EnhancedCounterAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Counter_Enhanced.asset";
        private const string DodgeFollowUpAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp.asset";
        private const string EnhancedDodgeFollowUpAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp_Enhanced.asset";
        private const string SidewindCutAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_SidewindCut.asset";
        private const string CrossStepAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_CrossStep.asset";
        private const string RisingCleaveAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_RisingCleave.asset";
        private const string IronGateBreakAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_IronGateBreak.asset";
        private const string FallingStarAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_FallingStar.asset";
        private const string MoonSeverAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_MoonSever.asset";
        private const string SpellBoltSkillAssetPath = "Assets/_Game/Data/Skills/SO_Skill_SpellBolt.asset";
        private const string ForceBurstSkillAssetPath = "Assets/_Game/Data/Skills/SO_Skill_ForceBurst.asset";
        private static readonly string[] PlayerAnimatorParameterNames =
        {
            "GroundSpeed",
            "MoveX",
            "MoveY",
            "IsGrounded",
            "IsBlocking",
            "VerticalSpeed"
        };

        private static readonly string[] PlayerBaseStateNames =
        {
            "Locomotion",
            "Block",
            "Airborne",
            "Dodge",
            "CombatRoll",
            "AirDodge",
            "Hit",
            "GuardBreak",
            "Death"
        };
        private static readonly string[] AttackClipPaths =
        {
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Light_01_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Light_02_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Light_03_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Heavy_01_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_DodgeFollowUp_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Counter_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Counter_Enhanced_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_DodgeFollowUp_Enhanced_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_SidewindCut_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_CrossStep_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_RisingCleave_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_IronGateBreak_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_FallingStar_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_MoonSever_CombatTest.anim"
        };
        private static readonly string[] PlayerTimedWindowAttackAssetPaths =
        {
            LightAttackAssetPath,
            Light02AttackAssetPath,
            Light03AttackAssetPath,
            HeavyAttackAssetPath,
            DodgeFollowUpAttackAssetPath,
            CounterAttackAssetPath,
            EnhancedCounterAttackAssetPath,
            EnhancedDodgeFollowUpAttackAssetPath,
            SidewindCutAttackAssetPath,
            CrossStepAttackAssetPath,
            RisingCleaveAttackAssetPath,
            IronGateBreakAttackAssetPath,
            FallingStarAttackAssetPath,
            MoonSeverAttackAssetPath
        };

        [Test]
        public void PlayerCombatTestPrefab_UsesPublicProxyBaseline()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.IsNotNull(prefab);

            Animator animator = prefab.GetComponent<Animator>();
            PlayerCombatAnimationRelay relay = prefab.GetComponent<PlayerCombatAnimationRelay>();
            PlayerCombatController combatController = prefab.GetComponent<PlayerCombatController>();

            Assert.IsNotNull(animator);
            Assert.IsNotNull(relay);
            Assert.IsNotNull(combatController);
            Assert.IsNotNull(animator.runtimeAnimatorController);
            Assert.AreEqual(PlayerControllerPath, AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            Assert.IsNull(animator.avatar);
            Assert.IsNull(prefab.transform.Find("ImportedVisualRoot"));
            Assert.IsNotNull(prefab.transform.Find("CombatProxyVisualRoot"));
            Assert.IsNull(GetPrivateField<Transform>(relay, "proxyWeaponGrip"));
            CollectionAssert.IsEmpty(
                AssetDatabase.GetDependencies(PlayerPrefabPath, true)
                    .Where(IsImportedSourcePath),
                "PF_Player_CombatTest should not depend on local imported preview directories in the public baseline.");
            Assert.AreSame(animator, GetPrivateField<Animator>(relay, "animator"));
            Assert.AreSame(prefab.GetComponent<PlayerCharacter>(), GetPrivateField<PlayerCharacter>(relay, "playerCharacter"));
            Assert.AreSame(combatController, GetPrivateField<PlayerCombatController>(relay, "combatController"));
            Assert.AreSame(prefab.GetComponent<PlayerStateMachine>(), GetPrivateField<PlayerStateMachine>(relay, "stateMachine"));
            Assert.AreSame(prefab.GetComponent<PlayerMotor>(), GetPrivateField<PlayerMotor>(relay, "motor"));
            Assert.AreSame(relay, GetPrivateField<PlayerCombatAnimationRelay>(combatController, "animationRelay"));
            Assert.LessOrEqual(GetPrivateValueField<float>(relay, "crossFadeSeconds"), 0.04f);
            Assert.LessOrEqual(GetPrivateValueField<float>(relay, "locomotionDampSeconds"), 0.05f);
            Assert.Greater(GetPrivateValueField<float>(relay, "dodgeAnimationDurationSeconds"), 0.4f);
            Assert.That(GetPrivateValueField<float>(relay, "combatRollAnimationDurationSeconds"), Is.InRange(0.5f, 0.56f));
            Assert.That(GetPrivateValueField<float>(relay, "airDodgeAnimationDurationSeconds"), Is.InRange(0.3f, 0.38f));
            Assert.That(GetPrivateValueField<float>(relay, "hitAnimationDurationSeconds"), Is.InRange(0.24f, 0.3f));
            Assert.That(GetPrivateValueField<float>(combatController.GetComponent<DamageableReceiver>(), "playerHitStunSeconds"), Is.InRange(0.06f, 0.1f));
        }

        [Test]
        public void EnemyCombatTestPrefabs_UsePublicProxyBaseline()
        {
            for (int i = 0; i < EnemyPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPaths[i]);

                Assert.IsNotNull(prefab, EnemyPrefabPaths[i]);
                Transform proxyRoot = prefab.transform.Find("CombatProxyVisualRoot");

                Assert.IsNotNull(proxyRoot, EnemyPrefabPaths[i]);
                Renderer[] proxyRenderers = proxyRoot.GetComponentsInChildren<Renderer>(true);
                Assert.That(proxyRenderers, Is.Not.Empty, EnemyPrefabPaths[i]);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled), EnemyPrefabPaths[i]);
                Assert.IsNull(prefab.GetComponent<Animator>(), EnemyPrefabPaths[i]);
                Assert.IsNull(prefab.GetComponent<EnemyCombatAnimationRelay>(), EnemyPrefabPaths[i]);
                Assert.IsNull(prefab.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), EnemyPrefabPaths[i]);
                Assert.IsNull(proxyRoot.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), EnemyPrefabPaths[i]);
                CapsuleCollider capsuleCollider = prefab.GetComponent<CapsuleCollider>();
                NavMeshAgent navMeshAgent = prefab.GetComponent<NavMeshAgent>();
                Assert.IsNotNull(capsuleCollider, EnemyPrefabPaths[i]);
                Assert.IsNotNull(navMeshAgent, EnemyPrefabPaths[i]);
                Assert.AreEqual(1f, capsuleCollider.center.y, 0.001f, EnemyPrefabPaths[i]);
                Assert.AreEqual(0f, navMeshAgent.baseOffset, 0.001f, EnemyPrefabPaths[i]);
                CollectionAssert.IsEmpty(
                    AssetDatabase.GetDependencies(EnemyPrefabPaths[i], true).Where(IsImportedSourcePath),
                    EnemyPrefabPaths[i]);
            }
        }

        [Test]
        public void PlayerAnimatorController_ContainsExpectedBaseStatesAndParameters()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);

            Assert.IsNotNull(controller);
            Assert.That(controller.layers, Is.Not.Empty);
            Assert.That(controller.layers[0].stateMachine.defaultState, Is.Not.Null);
            Assert.AreEqual("Locomotion", controller.layers[0].stateMachine.defaultState.name);

            for (int i = 0; i < PlayerAnimatorParameterNames.Length; i++)
            {
                Assert.That(
                    controller.parameters,
                    Has.Some.Matches<AnimatorControllerParameter>(parameter => parameter.name == PlayerAnimatorParameterNames[i]),
                    PlayerAnimatorParameterNames[i]);
            }

            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;

            for (int i = 0; i < PlayerBaseStateNames.Length; i++)
            {
                Assert.That(
                    states,
                    Has.Some.Matches<ChildAnimatorState>(state => state.state != null && state.state.name == PlayerBaseStateNames[i]),
                    PlayerBaseStateNames[i]);
            }

            AnimatorState locomotionState = FindState(controller.layers[0].stateMachine, "Locomotion");

            Assert.IsNotNull(locomotionState);
            Assert.IsInstanceOf<BlendTree>(locomotionState.motion);
            BlendTree locomotionBlendTree = (BlendTree)locomotionState.motion;
            Assert.AreEqual(BlendTreeType.FreeformCartesian2D, locomotionBlendTree.blendType);
            Assert.AreEqual("MoveX", locomotionBlendTree.blendParameter);
            Assert.AreEqual("MoveY", locomotionBlendTree.blendParameterY);
            Assert.That(locomotionBlendTree.children, Has.Length.EqualTo(13));

            AnimatorState hitState = FindState(controller.layers[0].stateMachine, "Hit");
            AnimatorState guardBreakState = FindState(controller.layers[0].stateMachine, "GuardBreak");

            Assert.IsNotNull(hitState);
            Assert.IsNotNull(guardBreakState);
            Assert.IsNotNull(hitState.motion);
            Assert.IsNotNull(guardBreakState.motion);
            Assert.AreNotSame(hitState.motion, guardBreakState.motion);
            Assert.AreEqual("AN_Player_GuardBreak_CombatTest", guardBreakState.motion.name);
            Assert.AreEqual(1f, guardBreakState.speed, 0.001f);
        }

        [Test]
        public void PlayerIdleClip_ContainsUsableAnimationData()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlayerIdleClipPath);

            Assert.IsNotNull(clip);
            Assert.Greater(clip.length, 0f);
            Assert.Greater(clip.frameRate, 0f);
        }

        [Test]
        public void PlayerTimedWindowAttackClips_DoNotExposeHitboxAnimationEvents()
        {
            Assert.AreEqual(PlayerTimedWindowAttackAssetPaths.Length, AttackClipPaths.Length);

            for (int i = 0; i < AttackClipPaths.Length; i++)
            {
                AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(PlayerTimedWindowAttackAssetPaths[i]);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPaths[i]);

                Assert.IsNotNull(attack, PlayerTimedWindowAttackAssetPaths[i]);
                Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, attack.HitboxActivationMode, PlayerTimedWindowAttackAssetPaths[i]);
                Assert.IsNotNull(clip, AttackClipPaths[i]);
                Assert.Greater(clip.length, 0f, AttackClipPaths[i]);
                AssertNoHitboxAnimationEvents(clip, AttackClipPaths[i]);
            }
        }

        [Test]
        public void PlayerAttackClips_ContainAnimationData()
        {
            for (int i = 0; i < AttackClipPaths.Length; i++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPaths[i]);

                Assert.IsNotNull(clip, AttackClipPaths[i]);
                Assert.Greater(clip.length, 0f, AttackClipPaths[i]);
                Assert.Greater(clip.frameRate, 0f, AttackClipPaths[i]);
            }
        }

        [Test]
        public void Light02AttackAssets_UsePublicProxyPreviewWindow()
        {
            AttackDefinitionSO attackDefinition = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light02AttackAssetPath);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Light02ClipPath);

            Assert.IsNotNull(attackDefinition);
            Assert.IsNotNull(clip);
            Assert.AreEqual(0.10f, attackDefinition.StartupSeconds, 0.001f);
            Assert.AreEqual(0.08f, attackDefinition.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.24f, attackDefinition.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.4956f, attackDefinition.AnimationDurationSeconds, 0.001f);

            SerializedObject serializedObject = new SerializedObject(clip);
            SerializedProperty clipSettings = serializedObject.FindProperty("m_AnimationClipSettings");

            Assert.IsNotNull(clipSettings);

            SerializedProperty stopTimeProperty = clipSettings.FindPropertyRelative("m_StopTime");

            Assert.IsNotNull(stopTimeProperty);
            Assert.AreEqual(attackDefinition.AnimationDurationSeconds, stopTimeProperty.floatValue, 0.001f);
        }

        [Test]
        public void CounterAttackAssets_RemainTrimmedAndDistinctFromLight02()
        {
            AttackDefinitionSO counterAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CounterAttackAssetPath);
            AnimationClip counterClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CounterClipPath);
            AnimationClip light02Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Light02ClipPath);

            Assert.IsNotNull(counterAttack);
            Assert.IsNotNull(counterClip);
            Assert.IsNotNull(light02Clip);

            float counterStopTime = GetClipStopTime(counterClip);
            float light02StopTime = GetClipStopTime(light02Clip);

            Assert.AreEqual(counterStopTime, counterAttack.AnimationDurationSeconds, 0.001f);
            Assert.Less(counterStopTime, 1f, "Counter should stay trimmed instead of reverting to the full imported source duration.");
            Assert.That(
                Mathf.Abs(counterStopTime - light02StopTime),
                Is.GreaterThan(0.04f),
                "Counter should remain distinct from Light_02 in the public proxy baseline.");
            Assert.Greater(counterStopTime, light02StopTime, "Counter should keep a heavier readable follow-through than Light_02.");
        }

        [Test]
        public void PlayerClips_UseExpectedCurveSource()
        {
            foreach (string clipPath in PlayerBaselineClipPaths.Concat(AttackClipPaths))
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

                Assert.IsNotNull(clip, clipPath);
                Assert.That(
                    curveBindings,
                    Has.Some.Matches<EditorCurveBinding>(binding => binding.path.StartsWith("CombatProxyVisualRoot/")),
                    clipPath);
            }
        }

        [Test]
        public void PlayerEvasiveClips_KeepReadableProxyMotionAndGrounding()
        {
            AnimationClip dodgeClip = LoadClip(PlayerDodgeClipPath);
            AnimationClip combatRollClip = LoadClip(PlayerCombatRollClipPath);
            AnimationClip airDodgeClip = LoadClip(PlayerAirDodgeClipPath);

            float dodgeTorsoY = EvaluateFloatCurve(dodgeClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.y", 0.21f);
            float dodgeTorsoZ = EvaluateFloatCurve(dodgeClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.z", 0.21f);
            float dodgeTellScale = EvaluateFloatCurve(dodgeClip, "CombatProxyVisualRoot/ForwardMarker", "m_LocalScale.z", 0.21f);
            float rollTorsoY = EvaluateFloatCurve(combatRollClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.y", 0.26f);
            float airDodgeTorsoY = EvaluateFloatCurve(airDodgeClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.y", 0.17f);

            Assert.GreaterOrEqual(dodgeTorsoY, 0.72f, "Dodge should stay visually above ground in the public proxy baseline.");
            Assert.Less(dodgeTorsoZ, -0.45f, "Dodge should read as a retreat/evade, not a generic forward attack.");
            Assert.Greater(dodgeTellScale, 1.25f, "Dodge should keep a visible directional tell.");
            Assert.That(rollTorsoY, Is.InRange(0.5f, 0.7f), "CombatRoll should be low but not buried.");
            Assert.Greater(airDodgeTorsoY, 1.1f, "AirDodge should lift clearly away from the grounded roll silhouette.");
        }

        [Test]
        public void PlayerBlockClip_KeepsReadableGuardPoseInsteadOfAttackSwing()
        {
            AnimationClip blockClip = LoadClip(PlayerBlockClipPath);

            float torsoY = EvaluateFloatCurve(blockClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.y", 0.4f);
            float torsoZ = EvaluateFloatCurve(blockClip, "CombatProxyVisualRoot/Torso", "m_LocalPosition.z", 0.4f);
            float guardZ = EvaluateFloatCurve(blockClip, "CombatProxyVisualRoot/Guard", "m_LocalPosition.z", 0.4f);
            float bladeZ = EvaluateFloatCurve(blockClip, "CombatProxyVisualRoot/Blade", "m_LocalPosition.z", 0.4f);
            float tellScale = EvaluateFloatCurve(blockClip, "CombatProxyVisualRoot/ForwardMarker", "m_LocalScale.z", 0.4f);

            Assert.That(torsoY, Is.InRange(0.78f, 0.86f), "Block should brace low without burying the player proxy.");
            Assert.Less(torsoZ, 0f, "Block should lean back instead of advancing like a generic attack.");
            Assert.Greater(guardZ, 0.52f, "Block should visibly bring the guard forward.");
            Assert.Less(bladeZ, 0.38f, "Block should keep the blade close instead of showing an attack extension.");
            Assert.Less(tellScale, 0.7f, "Block should not display the long strike tell used by attack clips.");
        }

        [Test]
        public void PlayerAttackAssets_UseTimedActivation_ToKeepCombatHitsReliable()
        {
            AttackDefinitionSO lightAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(LightAttackAssetPath);
            AttackDefinitionSO heavyAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyAttackAssetPath);
            AttackDefinitionSO enemyMeleeAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnemyMeleeAttackAssetPath);

            Assert.IsNotNull(lightAttack);
            Assert.IsNotNull(heavyAttack);
            Assert.IsNotNull(enemyMeleeAttack);
            Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, lightAttack.HitboxActivationMode);
            Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, heavyAttack.HitboxActivationMode);
            Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, enemyMeleeAttack.HitboxActivationMode);
            Assert.Greater(lightAttack.AnimationDurationSeconds, lightAttack.StartupSeconds + lightAttack.ActiveSeconds + lightAttack.RecoverySeconds);
            Assert.Greater(heavyAttack.AnimationDurationSeconds, heavyAttack.StartupSeconds + heavyAttack.ActiveSeconds + heavyAttack.RecoverySeconds);
        }

        [Test]
        public void PlayerAttackAssets_UseActionMovementScales()
        {
            AssertAttackMovementScale(LightAttackAssetPath, 0.78f);
            AssertAttackMovementScale(Light02AttackAssetPath, 0.76f);
            AssertAttackMovementScale(Light03AttackAssetPath, 0.72f);
            AssertAttackMovementScale(HeavyAttackAssetPath, 0.55f);
            AssertAttackMovementScale(DodgeFollowUpAttackAssetPath, 0.82f);
            AssertAttackMovementScale(CounterAttackAssetPath, 0.7f);
            AssertAttackMovementScale(EnhancedCounterAttackAssetPath, 0.65f);
            AssertAttackMovementScale(EnhancedDodgeFollowUpAttackAssetPath, 0.82f);
            AssertAttackMovementScale(SidewindCutAttackAssetPath, 0.82f);
            AssertAttackMovementScale(CrossStepAttackAssetPath, 0.84f);
            AssertAttackMovementScale(RisingCleaveAttackAssetPath, 0.58f);
            AssertAttackMovementScale(IronGateBreakAttackAssetPath, 0.62f);
            AssertAttackMovementScale(FallingStarAttackAssetPath, 0.52f);
            AssertAttackMovementScale(MoonSeverAttackAssetPath, 0.72f);
        }

        [Test]
        public void PlayerBasicAttackAssets_UseDistributedForwardMovement()
        {
            AssertDistributedForwardMovement(LightAttackAssetPath, 0.30f, 0.04f, 0.13f);
            AssertDistributedForwardMovement(Light02AttackAssetPath, 0.34f, 0.05f, 0.13f);
            AssertDistributedForwardMovement(Light03AttackAssetPath, 0.45f, 0.07f, 0.17f);
            AssertDistributedForwardMovement(HeavyAttackAssetPath, 0.55f, 0.12f, 0.22f);
        }

        [Test]
        public void PlayerAttackAssets_UseReadableHitStopTiers()
        {
            AttackDefinitionSO lightAttack = AssertAttackHitStop(LightAttackAssetPath, 0.05f);
            AttackDefinitionSO heavyAttack = AssertAttackHitStop(HeavyAttackAssetPath, 0.08f);

            Assert.Greater(heavyAttack.HitStopSeconds, lightAttack.HitStopSeconds);
        }

        [Test]
        public void PlayerSkillAssets_UseCastMovementProfiles()
        {
            SkillDefinitionSO spellBolt = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(SpellBoltSkillAssetPath);
            SkillDefinitionSO forceBurst = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(ForceBurstSkillAssetPath);

            Assert.IsNotNull(spellBolt);
            Assert.IsNotNull(forceBurst);
            Assert.IsTrue(spellBolt.AllowsMovementDuringCast);
            Assert.AreEqual(0.55f, spellBolt.MovementSpeedScale, 0.001f);
            Assert.IsFalse(forceBurst.AllowsMovementDuringCast);
            Assert.AreEqual(1f, forceBurst.MovementSpeedScale, 0.001f);
        }

        [Test]
        public void PlayerCombatBaselineAssets_DoNotDependOnImportedSourceDirectories()
        {
            foreach (string assetPath in new[] { PlayerPrefabPath, PlayerControllerPath }.Concat(PlayerBaselineClipPaths).Concat(AttackClipPaths))
            {
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(assetPath), assetPath);
                CollectionAssert.IsEmpty(
                    AssetDatabase.GetDependencies(assetPath, true).Where(IsImportedSourcePath),
                    assetPath);
            }
        }

        private static TField GetPrivateField<TField>(object instance, string fieldName) where TField : class
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(instance) as TField;
        }

        private static TValue GetPrivateValueField<TValue>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return (TValue)field.GetValue(instance);
        }

        private static float GetClipStopTime(AnimationClip clip)
        {
            SerializedObject serializedObject = new SerializedObject(clip);
            SerializedProperty clipSettings = serializedObject.FindProperty("m_AnimationClipSettings");
            Assert.IsNotNull(clipSettings);

            SerializedProperty stopTimeProperty = clipSettings.FindPropertyRelative("m_StopTime");
            Assert.IsNotNull(stopTimeProperty);
            return stopTimeProperty.floatValue;
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            Assert.IsNotNull(clip, assetPath);
            return clip;
        }

        private static float EvaluateFloatCurve(AnimationClip clip, string path, string propertyName, float time)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

            Assert.IsNotNull(curve, $"{clip.name} missing {path}.{propertyName}");
            return curve.Evaluate(time);
        }

        private static bool IsImportedSourcePath(string path)
        {
            return ImportedSourceRoots.Any(root => path.StartsWith(root));
        }

        private static void AssertNoHitboxAnimationEvents(AnimationClip clip, string context)
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);

            Assert.That(
                events,
                Has.None.Matches<AnimationEvent>(animationEvent => IsHitboxAnimationEvent(animationEvent)),
                context);
        }

        private static bool IsHitboxAnimationEvent(AnimationEvent animationEvent)
        {
            return string.Equals(animationEvent.functionName, "AnimationEvent_OpenAttackHitbox", System.StringComparison.Ordinal)
                || string.Equals(animationEvent.functionName, "AnimationEvent_CloseAttackHitbox", System.StringComparison.Ordinal);
        }

        private static void AssertAttackMovementScale(string assetPath, float expected)
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(assetPath);

            Assert.IsNotNull(attack, assetPath);
            Assert.AreEqual(expected, attack.MovementSpeedScale, 0.001f, assetPath);
        }

        private static void AssertDistributedForwardMovement(
            string assetPath,
            float expectedForwardMovement,
            float expectedStartSeconds,
            float expectedDurationSeconds)
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(assetPath);

            Assert.IsNotNull(attack, assetPath);
            Assert.AreEqual(expectedForwardMovement, attack.ForwardMovement, 0.001f, assetPath);
            Assert.AreEqual(expectedStartSeconds, attack.ForwardMovementStartSeconds, 0.001f, assetPath);
            Assert.AreEqual(expectedDurationSeconds, attack.ForwardMovementDurationSeconds, 0.001f, assetPath);
            Assert.IsTrue(attack.UsesDistributedForwardMovement, assetPath);
        }

        private static AttackDefinitionSO AssertAttackHitStop(string assetPath, float expected)
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(assetPath);

            Assert.IsNotNull(attack, assetPath);
            Assert.AreEqual(expected, attack.HitStopSeconds, 0.001f, assetPath);
            return attack;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }
    }

    public sealed class GuardBreakAnimationStateWiringTests
    {
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";
        private const string PlayerGuardBreakClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_GuardBreak_CombatTest.anim";

        [Test]
        public void PlayerAnimatorController_HasDedicatedGuardBreakStateBoundToDedicatedMotion()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
            AnimationClip guardBreakClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(PlayerGuardBreakClipPath);

            Assert.IsNotNull(controller);
            Assert.IsNotNull(guardBreakClip);

            AnimatorState hitState = FindState(controller.layers[0].stateMachine, PlayerCombatAnimationRelay.HitStateName);
            AnimatorState guardBreakState = FindState(controller.layers[0].stateMachine, PlayerCombatAnimationRelay.GuardBreakHitStateName);

            Assert.IsNotNull(hitState);
            Assert.IsNotNull(guardBreakState);
            Assert.IsNotNull(hitState.motion);
            Assert.AreNotSame(hitState.motion, guardBreakState.motion);
            Assert.AreSame(guardBreakClip, guardBreakState.motion);
            Assert.AreEqual(1f, guardBreakState.speed, 0.001f);
            Assert.That(guardBreakState.transitions, Has.Some.Matches<AnimatorStateTransition>(
                transition => transition.destinationState != null
                    && transition.destinationState.name == PlayerCombatAnimationRelay.LocomotionStateName));
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }
    }

    public sealed class CombatTestSwordArtAssetWiringTests
    {
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";
        private const string HeavyAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string SidewindCutAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_SidewindCut.asset";
        private const string CrossStepAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_CrossStep.asset";
        private const string RisingCleaveAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_RisingCleave.asset";
        private const string IronGateBreakAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_IronGateBreak.asset";
        private const string FallingStarAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_FallingStar.asset";
        private const string MoonSeverAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_MoonSever.asset";
        private const string SidewindCutClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_SidewindCut_CombatTest.anim";
        private const string CrossStepClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_CrossStep_CombatTest.anim";
        private const string RisingCleaveClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_RisingCleave_CombatTest.anim";
        private const string IronGateBreakClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_IronGateBreak_CombatTest.anim";
        private const string FallingStarClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_FallingStar_CombatTest.anim";
        private const string MoonSeverClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_SwordArt_MoonSever_CombatTest.anim";
        private const string SidewindCutSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_SidewindCut.asset";
        private const string CrossStepSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_CrossStep.asset";
        private const string RisingCleaveSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_RisingCleave.asset";
        private const string IronGateBreakSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_IronGateBreak.asset";
        private const string FallingStarSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_FallingStar.asset";
        private const string MoonSeverSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_MoonSever.asset";

        [Test]
        public void CombatTestSwordArtAssets_UseDedicatedCombatTestAttackBindings()
        {
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(SidewindCutSwordArtPath),
                "SwordArt_SidewindCut",
                "Sidewind Cut",
                SwordArtTriggerAction.LightAttack,
                SwordArtDirectionMask.Left | SwordArtDirectionMask.Right,
                SwordArtContextTags.AfterDodge,
                SwordArtContextTags.None,
                SidewindCutAttackAssetPath);
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(CrossStepSwordArtPath),
                "SwordArt_CrossStep",
                "Cross Step",
                SwordArtTriggerAction.LightAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll,
                SwordArtContextTags.None,
                CrossStepAttackAssetPath);
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(RisingCleaveSwordArtPath),
                "SwordArt_RisingCleave",
                "Rising Cleave",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.None,
                SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne,
                RisingCleaveAttackAssetPath);
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(IronGateBreakSwordArtPath),
                "SwordArt_IronGateBreak",
                "Iron Gate Break",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.None,
                SwordArtContextTags.AfterBlock | SwordArtContextTags.AfterHeavy,
                IronGateBreakAttackAssetPath);
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(FallingStarSwordArtPath),
                "SwordArt_FallingStar",
                "Falling Star",
                SwordArtTriggerAction.HeavyAttack,
                SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward,
                SwordArtContextTags.Airborne,
                SwordArtContextTags.None,
                FallingStarAttackAssetPath);
            AssertSwordArt(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(MoonSeverSwordArtPath),
                "SwordArt_MoonSever",
                "Moon Sever",
                SwordArtTriggerAction.LightAttack,
                SwordArtDirectionMask.Any,
                SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge,
                SwordArtContextTags.None,
                MoonSeverAttackAssetPath);
        }

        [Test]
        public void CombatTestSwordArtAttackAssets_HaveDedicatedAnimatorStatesAndHitboxClips()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);

            Assert.IsNotNull(controller);
            AssertSwordArtAttack(controller, SidewindCutAttackAssetPath, "SwordArt_SidewindCut", "Sidewind Cut", SidewindCutClipPath);
            AssertSwordArtAttack(controller, CrossStepAttackAssetPath, "SwordArt_CrossStep", "Cross Step", CrossStepClipPath);
            AssertSwordArtAttack(controller, RisingCleaveAttackAssetPath, "SwordArt_RisingCleave", "Rising Cleave", RisingCleaveClipPath);
            AssertSwordArtAttack(controller, IronGateBreakAttackAssetPath, "SwordArt_IronGateBreak", "Iron Gate Break", IronGateBreakClipPath);
            AssertSwordArtAttack(controller, FallingStarAttackAssetPath, "SwordArt_FallingStar", "Falling Star", FallingStarClipPath);
            AssertSwordArtAttack(controller, MoonSeverAttackAssetPath, "SwordArt_MoonSever", "Moon Sever", MoonSeverClipPath);
        }

        [Test]
        public void CombatTestSwordArtAttackAssets_KeepReadableTimingAndMovementProfiles()
        {
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(SidewindCutSwordArtPath),
                SidewindCutAttackAssetPath,
                minStartup: 0.06f,
                maxStartup: 0.12f,
                minAnimationDuration: 0.45f,
                maxAnimationDuration: 0.6f,
                minMovementScale: 0.78f,
                maxMovementScale: 0.95f,
                minForwardMovement: 0.6f,
                maxForwardMovement: 0.85f,
                minCancelWindow: 0.14f,
                maxCancelWindow: 0.22f);
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(CrossStepSwordArtPath),
                CrossStepAttackAssetPath,
                minStartup: 0.08f,
                maxStartup: 0.12f,
                minAnimationDuration: 0.46f,
                maxAnimationDuration: 0.62f,
                minMovementScale: 0.8f,
                maxMovementScale: 0.9f,
                minForwardMovement: 0.8f,
                maxForwardMovement: 0.95f,
                minCancelWindow: 0.14f,
                maxCancelWindow: 0.22f);
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(RisingCleaveSwordArtPath),
                RisingCleaveAttackAssetPath,
                minStartup: 0.15f,
                maxStartup: 0.24f,
                minAnimationDuration: 0.72f,
                maxAnimationDuration: 0.9f,
                minMovementScale: 0.5f,
                maxMovementScale: 0.68f,
                minForwardMovement: 0.5f,
                maxForwardMovement: 0.75f,
                minCancelWindow: 0.16f,
                maxCancelWindow: 0.25f);
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(IronGateBreakSwordArtPath),
                IronGateBreakAttackAssetPath,
                minStartup: 0.12f,
                maxStartup: 0.2f,
                minAnimationDuration: 0.62f,
                maxAnimationDuration: 0.82f,
                minMovementScale: 0.55f,
                maxMovementScale: 0.72f,
                minForwardMovement: 0.45f,
                maxForwardMovement: 0.7f,
                minCancelWindow: 0.18f,
                maxCancelWindow: 0.27f);
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(FallingStarSwordArtPath),
                FallingStarAttackAssetPath,
                minStartup: 0.14f,
                maxStartup: 0.2f,
                minAnimationDuration: 0.76f,
                maxAnimationDuration: 0.9f,
                minMovementScale: 0.45f,
                maxMovementScale: 0.6f,
                minForwardMovement: 0.3f,
                maxForwardMovement: 0.5f,
                minCancelWindow: 0.14f,
                maxCancelWindow: 0.22f);
            AssertSwordArtFeelProfile(
                AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(MoonSeverSwordArtPath),
                MoonSeverAttackAssetPath,
                minStartup: 0.08f,
                maxStartup: 0.13f,
                minAnimationDuration: 0.48f,
                maxAnimationDuration: 0.62f,
                minMovementScale: 0.68f,
                maxMovementScale: 0.78f,
                minForwardMovement: 0.5f,
                maxForwardMovement: 0.68f,
                minCancelWindow: 0.12f,
                maxCancelWindow: 0.2f);
        }

        [Test]
        public void CombatTestIronGateBreak_FromHeavyOpensOnlyInLateRecovery()
        {
            AttackDefinitionSO heavyAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyAttackAssetPath);
            SwordArtDefinitionSO ironGateBreak = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(IronGateBreakSwordArtPath);

            Assert.IsNotNull(heavyAttack);
            Assert.IsNotNull(ironGateBreak);

            float heavyRuntimeDuration = heavyAttack.StartupSeconds
                + heavyAttack.ActiveSeconds
                + PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(heavyAttack);
            float heavyActiveEndSeconds = heavyAttack.StartupSeconds + heavyAttack.ActiveSeconds;
            float cancelWindowStartSeconds = Mathf.Max(0f, heavyRuntimeDuration - ironGateBreak.CancelWindowSeconds);

            Assert.AreEqual(0.90f, heavyRuntimeDuration, 0.001f);
            Assert.AreEqual(0.68f, cancelWindowStartSeconds, 0.001f);
            Assert.GreaterOrEqual(cancelWindowStartSeconds - heavyActiveEndSeconds, 0.25f);
        }

        [Test]
        public void CombatTestSwordArtExecution_UsesDedicatedAttackAssetsForAllEntryContexts()
        {
            GameObject gameObject = new GameObject("CombatTestSwordArtPlayer");

            try
            {
                PlayerStateMachine stateMachine = BuildSwordArtRuntime(
                    gameObject,
                    out PlayerCombatController combatController,
                    out SwordArtDefinitionSO sidewindCut,
                    out SwordArtDefinitionSO crossStep,
                    out SwordArtDefinitionSO risingCleave,
                    out SwordArtDefinitionSO ironGateBreak,
                    out SwordArtDefinitionSO fallingStar,
                    out SwordArtDefinitionSO moonSever);

                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Right,
                    SwordArtContextTags.AfterDodge);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                Assert.AreSame(sidewindCut.AttackDefinition, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToLocomotion();
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                Assert.AreSame(crossStep.AttackDefinition, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToLocomotion();
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Forward);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
                Assert.AreSame(risingCleave.AttackDefinition, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToLocomotion();
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.AfterBlock);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Counter);
                Assert.AreSame(ironGateBreak.AttackDefinition, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToLocomotion();
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.HeavyAttack,
                    SwordArtInputDirection.Backward,
                    SwordArtContextTags.Airborne);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
                Assert.AreSame(fallingStar.AttackDefinition, combatController.CurrentAttackDefinition);

                stateMachine.SwitchToLocomotion();
                combatController.BufferSwordArtCommand(
                    SwordArtTriggerAction.LightAttack,
                    SwordArtInputDirection.Neutral,
                    SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                Assert.AreSame(moonSever.AttackDefinition, combatController.CurrentAttackDefinition);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CombatTestSwordArtExecution_HeavyChainWaitsForIronGateBreakCancelWindow()
        {
            GameObject gameObject = new GameObject("CombatTestSwordArtPlayer");

            try
            {
                PlayerStateMachine stateMachine = BuildSwordArtRuntime(
                    gameObject,
                    out PlayerCombatController combatController,
                    out _,
                    out _,
                    out _,
                    out SwordArtDefinitionSO ironGateBreak,
                    out _,
                    out _);
                AttackDefinitionSO heavyAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(
                    "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset");

                Assert.IsNotNull(heavyAttack);

                SetPrivateField(combatController, "heavyAttack", heavyAttack);
                stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
                Assert.AreSame(heavyAttack, combatController.CurrentAttackDefinition);

                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.AreSame(heavyAttack, combatController.CurrentAttackDefinition);
                Assert.IsTrue(combatController.HasBufferedSwordArtCommand);

                float heavyDuration = heavyAttack.StartupSeconds
                    + heavyAttack.ActiveSeconds
                    + PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(heavyAttack);
                float cancelWindowEntry = Mathf.Max(0f, heavyDuration - ironGateBreak.CancelWindowSeconds + 0.01f);
                TickInSteps(stateMachine, cancelWindowEntry, 0.05f);
                InvokePrivateMethod(stateMachine, "OnHeavyAttackPressed");

                Assert.AreSame(ironGateBreak.AttackDefinition, combatController.CurrentAttackDefinition);
                Assert.IsFalse(combatController.HasBufferedSwordArtCommand);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void AssertSwordArt(
            SwordArtDefinitionSO swordArt,
            string expectedArtId,
            string expectedDisplayName,
            SwordArtTriggerAction expectedTriggerAction,
            SwordArtDirectionMask expectedDirections,
            SwordArtContextTags expectedRequiredTags,
            SwordArtContextTags expectedAnyTags,
            string expectedAttackPath)
        {
            Assert.IsNotNull(swordArt, expectedArtId);
            Assert.AreEqual(expectedArtId, swordArt.ArtId);
            Assert.AreEqual(expectedDisplayName, swordArt.DisplayName);
            Assert.AreEqual(expectedTriggerAction, swordArt.TriggerAction);
            Assert.AreEqual(expectedDirections, swordArt.AcceptedDirections);
            Assert.AreEqual(expectedRequiredTags, swordArt.RequiredContextTags);
            Assert.AreEqual(expectedAnyTags, swordArt.AnyContextTags);
            Assert.IsNotNull(swordArt.AttackDefinition, expectedArtId);
            Assert.AreEqual(expectedAttackPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
            Assert.Greater(swordArt.TriggerWindowSeconds, 0f);
            Assert.Greater(swordArt.CancelWindowSeconds, 0f);
        }

        private static void AssertSwordArtAttack(
            AnimatorController controller,
            string attackPath,
            string expectedStateName,
            string expectedDisplayName,
            string expectedClipPath)
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(attackPath);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(expectedClipPath);
            AnimatorState state = FindState(controller.layers[0].stateMachine, expectedStateName);

            Assert.IsNotNull(attack, attackPath);
            Assert.AreEqual(expectedStateName, attack.AttackId);
            Assert.AreEqual(expectedDisplayName, attack.DisplayName);
            Assert.AreEqual(expectedStateName, attack.AnimationStateName);
            Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, attack.HitboxActivationMode);
            Assert.Greater(attack.AnimationDurationSeconds, 0f);
            Assert.IsNotNull(clip, expectedClipPath);
            Assert.Greater(clip.length, 0f, expectedClipPath);
            Assert.That(
                AnimationUtility.GetAnimationEvents(clip),
                Has.None.Matches<AnimationEvent>(animationEvent =>
                    string.Equals(animationEvent.functionName, "AnimationEvent_OpenAttackHitbox", System.StringComparison.Ordinal)
                    || string.Equals(animationEvent.functionName, "AnimationEvent_CloseAttackHitbox", System.StringComparison.Ordinal)),
                expectedClipPath);
            Assert.IsNotNull(state, expectedStateName);
            Assert.IsNotNull(state.motion, expectedStateName);
            Assert.AreEqual(expectedClipPath, AssetDatabase.GetAssetPath(state.motion));
        }

        private static void AssertSwordArtFeelProfile(
            SwordArtDefinitionSO swordArt,
            string attackPath,
            float minStartup,
            float maxStartup,
            float minAnimationDuration,
            float maxAnimationDuration,
            float minMovementScale,
            float maxMovementScale,
            float minForwardMovement,
            float maxForwardMovement,
            float minCancelWindow,
            float maxCancelWindow)
        {
            Assert.IsNotNull(swordArt, attackPath);
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(attackPath);

            Assert.IsNotNull(attack, attackPath);
            Assert.That(attack.StartupSeconds, Is.InRange(minStartup, maxStartup), attackPath);
            Assert.That(attack.AnimationDurationSeconds, Is.InRange(minAnimationDuration, maxAnimationDuration), attackPath);
            Assert.That(attack.MovementSpeedScale, Is.InRange(minMovementScale, maxMovementScale), attackPath);
            Assert.That(attack.ForwardMovement, Is.InRange(minForwardMovement, maxForwardMovement), attackPath);
            Assert.That(swordArt.CancelWindowSeconds, Is.InRange(minCancelWindow, maxCancelWindow), attackPath);
            Assert.LessOrEqual(swordArt.CancelWindowSeconds, swordArt.TriggerWindowSeconds, attackPath);
        }

        private static PlayerStateMachine BuildSwordArtRuntime(
            GameObject gameObject,
            out PlayerCombatController combatController,
            out SwordArtDefinitionSO sidewindCut,
            out SwordArtDefinitionSO crossStep,
            out SwordArtDefinitionSO risingCleave,
            out SwordArtDefinitionSO ironGateBreak,
            out SwordArtDefinitionSO fallingStar,
            out SwordArtDefinitionSO moonSever)
        {
            sidewindCut = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(SidewindCutSwordArtPath);
            crossStep = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(CrossStepSwordArtPath);
            risingCleave = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(RisingCleaveSwordArtPath);
            ironGateBreak = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(IronGateBreakSwordArtPath);
            fallingStar = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(FallingStarSwordArtPath);
            moonSever = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(MoonSeverSwordArtPath);

            Assert.IsNotNull(sidewindCut);
            Assert.IsNotNull(crossStep);
            Assert.IsNotNull(risingCleave);
            Assert.IsNotNull(ironGateBreak);
            Assert.IsNotNull(fallingStar);
            Assert.IsNotNull(moonSever);
            Assert.IsNotNull(sidewindCut.AttackDefinition);
            Assert.IsNotNull(crossStep.AttackDefinition);
            Assert.IsNotNull(risingCleave.AttackDefinition);
            Assert.IsNotNull(ironGateBreak.AttackDefinition);
            Assert.IsNotNull(fallingStar.AttackDefinition);
            Assert.IsNotNull(moonSever.AttackDefinition);

            PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();
            PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
            AttackExecutor attackExecutor = gameObject.AddComponent<AttackExecutor>();
            HitboxController hitboxController = gameObject.AddComponent<HitboxController>();
            combatController = gameObject.AddComponent<PlayerCombatController>();

            SetPrivateField(hitboxController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "attackExecutor", attackExecutor);
            SetPrivateField(combatController, "hitboxController", hitboxController);
            SetPrivateField(combatController, "swordArts", new[] { sidewindCut, crossStep, risingCleave, ironGateBreak, fallingStar, moonSever });
            SetPrivateField(player, "stateMachine", stateMachine);
            SetPrivateField(player, "combatController", combatController);
            stateMachine.Initialize(player);
            return stateMachine;
        }

        private static void TickInSteps(PlayerStateMachine stateMachine, float duration, float stepSeconds)
        {
            float remaining = Mathf.Max(0f, duration);

            while (remaining > 0f)
            {
                float step = Mathf.Min(stepSeconds, remaining);
                stateMachine.Tick(step);
                remaining -= step;
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static void InvokePrivateMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }
    }

    public sealed class CombatTestIronGateBreakContractTests
    {
        private const string IronGateBreakAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_IronGateBreak.asset";
        private const string IronGateBreakSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_IronGateBreak.asset";

        [Test]
        public void IronGateBreakAttackAsset_UsesGuardBreakLungeContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(IronGateBreakAttackAssetPath);

            Assert.IsNotNull(attack);
            Assert.AreEqual("Iron Gate Break", attack.DisplayName);
            Assert.IsTrue(attack.BreaksGuard);
            Assert.AreEqual(0.16f, attack.GuardBreakHitStunSeconds, 0.001f);
            Assert.AreEqual(0.08f, attack.HitStopSeconds, 0.001f);
            Assert.AreEqual(0.55f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.08f, attack.ForwardMovementStartSeconds, 0.001f);
            Assert.AreEqual(0.22f, attack.ForwardMovementDurationSeconds, 0.001f);
            Assert.IsTrue(attack.UsesDistributedForwardMovement);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.125f), attack.HitboxLocalCenter, "HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.92f, 1f, 0.9f), attack.HitboxHalfExtents, "HitboxHalfExtents");
        }

        [Test]
        public void IronGateBreakSwordArtAsset_UsesGuardPressureManaCost()
        {
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(IronGateBreakSwordArtPath);

            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Iron Gate Break", swordArt.DisplayName);
            Assert.AreEqual(0.35f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.22f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(15f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(IronGateBreakAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 0.001f, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 0.001f, label + ".z");
        }
    }

    public sealed class CombatTestMoonSeverContractTests
    {
        private const string MoonSeverAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_MoonSever.asset";
        private const string MoonSeverSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_MoonSever.asset";
        private const string FallingStarAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_FallingStar.asset";

        [Test]
        public void MoonSeverAttackAsset_UsesAirDodgeSlashContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(MoonSeverAttackAssetPath);

            Assert.IsNotNull(attack);
            Assert.AreEqual("Moon Sever", attack.DisplayName);
            Assert.AreEqual(0.10f, attack.StartupSeconds, 0.001f);
            Assert.AreEqual(0.10f, attack.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.26f, attack.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.065f, attack.HitStopSeconds, 0.001f);
            Assert.IsFalse(attack.BreaksGuard);
            Assert.AreEqual(0.58f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.72f, attack.MovementSpeedScale, 0.001f);
            Assert.AreEqual(2.15f, attack.Range, 0.001f);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.075f), attack.HitboxLocalCenter, "HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.782f, 0.85f, 0.86f), attack.HitboxHalfExtents, "HitboxHalfExtents");
        }

        [Test]
        public void MoonSeverSwordArtAsset_RequiresAirDodgeWindowAndUsesDistinctManaCost()
        {
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(MoonSeverSwordArtPath);

            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Moon Sever", swordArt.DisplayName);
            Assert.AreEqual(SwordArtTriggerAction.LightAttack, swordArt.TriggerAction);
            Assert.AreEqual(SwordArtDirectionMask.Any, swordArt.AcceptedDirections);
            Assert.AreEqual(
                SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge,
                swordArt.RequiredContextTags);
            Assert.AreEqual(SwordArtContextTags.None, swordArt.AnyContextTags);
            Assert.AreEqual(0.28f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.16f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(12f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(MoonSeverAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        [Test]
        public void MoonSeverPreviewChain_RemainsDistinctFromFallingStar()
        {
            AttackDefinitionSO moonSeverAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(MoonSeverAttackAssetPath);
            AttackDefinitionSO fallingStarAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(FallingStarAttackAssetPath);

            Assert.IsNotNull(moonSeverAttack);
            Assert.IsNotNull(fallingStarAttack);

            MethodInfo candidateMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedAttackClipCandidatePaths",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo durationOverrideMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedPreviewAttackDurationOverride",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(candidateMethod);
            Assert.IsNotNull(durationOverrideMethod);

            string[] candidatePaths = candidateMethod.Invoke(null, new object[] { "SwordArt_MoonSever" }) as string[];
            float moonSeverPreviewDuration = (float)durationOverrideMethod.Invoke(null, new object[] { "SwordArt_MoonSever" });
            float fallingStarPreviewDuration = (float)durationOverrideMethod.Invoke(null, new object[] { "SwordArt_FallingStar" });

            Assert.IsNotNull(candidatePaths);
            Assert.That(candidatePaths, Is.Not.Empty);
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack03_Inplace.FBX",
                candidatePaths[0]);
            Assert.That(
                candidatePaths,
                Has.Some.EqualTo("Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack05_Inplace.FBX"));
            Assert.AreEqual(0.72f, moonSeverPreviewDuration, 0.001f);
            Assert.AreEqual(1.05f, fallingStarPreviewDuration, 0.001f);
            Assert.Less(moonSeverPreviewDuration, fallingStarPreviewDuration);
            Assert.Less(moonSeverAttack.StartupSeconds, fallingStarAttack.StartupSeconds);
            Assert.Less(moonSeverAttack.RecoverySeconds, fallingStarAttack.RecoverySeconds);
            Assert.Less(moonSeverAttack.HitStopSeconds, fallingStarAttack.HitStopSeconds);
        }

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 0.001f, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 0.001f, label + ".z");
        }
    }

    public sealed class CombatTestAirHeavySwordArtContractTests
    {
        private const string RisingCleaveAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_RisingCleave.asset";
        private const string FallingStarAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_FallingStar.asset";
        private const string RisingCleaveSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_RisingCleave.asset";
        private const string FallingStarSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_FallingStar.asset";

        [Test]
        public void RisingCleaveAssets_UseForwardAirChaseContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(RisingCleaveAttackAssetPath);
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(RisingCleaveSwordArtPath);

            Assert.IsNotNull(attack);
            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Rising Cleave", attack.DisplayName);
            Assert.AreEqual(0.18f, attack.StartupSeconds, 0.001f);
            Assert.AreEqual(0.12f, attack.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.38f, attack.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.05f, attack.HitStopSeconds, 0.001f);
            Assert.IsFalse(attack.BreaksGuard);
            Assert.AreEqual(0.62f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.58f, attack.MovementSpeedScale, 0.001f);
            Assert.AreEqual(2.35f, attack.Range, 0.001f);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.175f), attack.HitboxLocalCenter, "Rising.HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.851f, 0.925f, 0.94f), attack.HitboxHalfExtents, "Rising.HitboxHalfExtents");

            Assert.AreEqual("Rising Cleave", swordArt.DisplayName);
            Assert.AreEqual(SwordArtTriggerAction.HeavyAttack, swordArt.TriggerAction);
            Assert.AreEqual(SwordArtDirectionMask.Any, swordArt.AcceptedDirections);
            Assert.AreEqual(SwordArtContextTags.None, swordArt.RequiredContextTags);
            Assert.AreEqual(
                SwordArtContextTags.ForwardInput | SwordArtContextTags.Airborne,
                swordArt.AnyContextTags);
            Assert.AreEqual(0.3f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.2f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(0f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(RisingCleaveAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        [Test]
        public void FallingStarAssets_UseNeutralAirSlamContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(FallingStarAttackAssetPath);
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(FallingStarSwordArtPath);

            Assert.IsNotNull(attack);
            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Falling Star", attack.DisplayName);
            Assert.AreEqual(0.16f, attack.StartupSeconds, 0.001f);
            Assert.AreEqual(0.14f, attack.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.42f, attack.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.09f, attack.HitStopSeconds, 0.001f);
            Assert.IsFalse(attack.BreaksGuard);
            Assert.AreEqual(0.38f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.52f, attack.MovementSpeedScale, 0.001f);
            Assert.AreEqual(2.05f, attack.Range, 0.001f);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.025f), attack.HitboxLocalCenter, "Falling.HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.989f, 1.075f, 0.86f), attack.HitboxHalfExtents, "Falling.HitboxHalfExtents");

            Assert.AreEqual("Falling Star", swordArt.DisplayName);
            Assert.AreEqual(SwordArtTriggerAction.HeavyAttack, swordArt.TriggerAction);
            Assert.AreEqual(SwordArtDirectionMask.Neutral | SwordArtDirectionMask.Backward, swordArt.AcceptedDirections);
            Assert.AreEqual(SwordArtContextTags.Airborne, swordArt.RequiredContextTags);
            Assert.AreEqual(SwordArtContextTags.None, swordArt.AnyContextTags);
            Assert.AreEqual(0.32f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.18f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(0f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(FallingStarAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        [Test]
        public void AirHeavySwordArts_KeepForwardChaseAndNeutralSlamDistinct()
        {
            AttackDefinitionSO risingCleaveAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(RisingCleaveAttackAssetPath);
            AttackDefinitionSO fallingStarAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(FallingStarAttackAssetPath);

            Assert.IsNotNull(risingCleaveAttack);
            Assert.IsNotNull(fallingStarAttack);

            MethodInfo candidateMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedAttackClipCandidatePaths",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo durationOverrideMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedPreviewAttackDurationOverride",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(candidateMethod);
            Assert.IsNotNull(durationOverrideMethod);

            string[] risingCandidatePaths = candidateMethod.Invoke(null, new object[] { "SwordArt_RisingCleave" }) as string[];
            string[] fallingCandidatePaths = candidateMethod.Invoke(null, new object[] { "SwordArt_FallingStar" }) as string[];
            float risingPreviewDuration = (float)durationOverrideMethod.Invoke(null, new object[] { "SwordArt_RisingCleave" });
            float fallingPreviewDuration = (float)durationOverrideMethod.Invoke(null, new object[] { "SwordArt_FallingStar" });

            Assert.IsNotNull(risingCandidatePaths);
            Assert.IsNotNull(fallingCandidatePaths);
            Assert.That(risingCandidatePaths, Is.Not.Empty);
            Assert.That(fallingCandidatePaths, Is.Not.Empty);
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX",
                risingCandidatePaths[0]);
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_JumpAttack04_Inplace.FBX",
                fallingCandidatePaths[0]);
            Assert.AreEqual(1f, risingPreviewDuration, 0.001f);
            Assert.AreEqual(1.05f, fallingPreviewDuration, 0.001f);
            Assert.That(
                risingCandidatePaths,
                Has.Some.EqualTo("Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack06_Inplace.FBX"));
            Assert.That(
                fallingCandidatePaths,
                Has.Some.EqualTo("Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Air_Attack03_Start_Inplace.FBX"));
            Assert.Greater(risingCleaveAttack.ForwardMovement, fallingStarAttack.ForwardMovement);
            Assert.Greater(risingCleaveAttack.Range, fallingStarAttack.Range);
            Assert.Less(risingCleaveAttack.RecoverySeconds, fallingStarAttack.RecoverySeconds);
            Assert.Less(risingCleaveAttack.HitStopSeconds, fallingStarAttack.HitStopSeconds);
            Assert.Greater(fallingStarAttack.HitboxHalfExtents.y, risingCleaveAttack.HitboxHalfExtents.y);
        }

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 0.001f, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 0.001f, label + ".z");
        }
    }

    public sealed class CombatTestFlankSwordArtContractTests
    {
        private const string SidewindCutAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_SidewindCut.asset";
        private const string CrossStepAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_SwordArt_CrossStep.asset";
        private const string SidewindCutSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_SidewindCut.asset";
        private const string CrossStepSwordArtPath = "Assets/_Game/Data/Combat/SO_SwordArt_CrossStep.asset";

        [Test]
        public void SidewindCutAssets_UseShortFlankFollowUpContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(SidewindCutAttackAssetPath);
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(SidewindCutSwordArtPath);

            Assert.IsNotNull(attack);
            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Sidewind Cut", attack.DisplayName);
            Assert.AreEqual(0.08f, attack.StartupSeconds, 0.001f);
            Assert.AreEqual(0.10f, attack.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.25f, attack.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.72f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.82f, attack.MovementSpeedScale, 0.001f);
            Assert.AreEqual(2.05f, attack.Range, 0.001f);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.025f), attack.HitboxLocalCenter, "Sidewind.HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.759f, 0.825f, 0.82f), attack.HitboxHalfExtents, "Sidewind.HitboxHalfExtents");
            Assert.IsFalse(attack.BreaksGuard);

            Assert.AreEqual("Sidewind Cut", swordArt.DisplayName);
            Assert.AreEqual(SwordArtTriggerAction.LightAttack, swordArt.TriggerAction);
            Assert.AreEqual(SwordArtDirectionMask.Left | SwordArtDirectionMask.Right, swordArt.AcceptedDirections);
            Assert.AreEqual(SwordArtContextTags.AfterDodge, swordArt.RequiredContextTags);
            Assert.AreEqual(SwordArtContextTags.None, swordArt.AnyContextTags);
            Assert.AreEqual(0.25f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.18f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(0f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(SidewindCutAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        [Test]
        public void CrossStepAssets_UseLongerRollCounterContract()
        {
            AttackDefinitionSO attack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CrossStepAttackAssetPath);
            SwordArtDefinitionSO swordArt = AssetDatabase.LoadAssetAtPath<SwordArtDefinitionSO>(CrossStepSwordArtPath);

            Assert.IsNotNull(attack);
            Assert.IsNotNull(swordArt);
            Assert.AreEqual("Cross Step", attack.DisplayName);
            Assert.AreEqual(0.09f, attack.StartupSeconds, 0.001f);
            Assert.AreEqual(0.10f, attack.ActiveSeconds, 0.001f);
            Assert.AreEqual(0.27f, attack.RecoverySeconds, 0.001f);
            Assert.AreEqual(0.06f, attack.HitStopSeconds, 0.001f);
            Assert.AreEqual(0.86f, attack.ForwardMovement, 0.001f);
            Assert.AreEqual(0.84f, attack.MovementSpeedScale, 0.001f);
            Assert.AreEqual(2.25f, attack.Range, 0.001f);
            Assert.AreEqual(AttackHitboxShape.Box, attack.HitboxShape);
            AssertVector3Approximately(new Vector3(0f, 0f, 1.125f), attack.HitboxLocalCenter, "CrossStep.HitboxLocalCenter");
            AssertVector3Approximately(new Vector3(0.828f, 0.9f, 0.9f), attack.HitboxHalfExtents, "CrossStep.HitboxHalfExtents");
            Assert.IsFalse(attack.BreaksGuard);

            Assert.AreEqual("Cross Step", swordArt.DisplayName);
            Assert.AreEqual(SwordArtTriggerAction.LightAttack, swordArt.TriggerAction);
            Assert.AreEqual(SwordArtDirectionMask.Any, swordArt.AcceptedDirections);
            Assert.AreEqual(
                SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll,
                swordArt.RequiredContextTags);
            Assert.AreEqual(SwordArtContextTags.None, swordArt.AnyContextTags);
            Assert.AreEqual(0.30f, swordArt.TriggerWindowSeconds, 0.001f);
            Assert.AreEqual(0.18f, swordArt.CancelWindowSeconds, 0.001f);
            Assert.AreEqual(0f, swordArt.ResourceCost, 0.001f);
            Assert.IsNotNull(swordArt.AttackDefinition);
            Assert.AreEqual(CrossStepAttackAssetPath, AssetDatabase.GetAssetPath(swordArt.AttackDefinition));
        }

        [Test]
        public void FlankSwordArts_KeepShortDodgeAndLongRollReadDistinct()
        {
            AttackDefinitionSO sidewindCut = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(SidewindCutAttackAssetPath);
            AttackDefinitionSO crossStep = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CrossStepAttackAssetPath);

            Assert.IsNotNull(sidewindCut);
            Assert.IsNotNull(crossStep);
            Assert.LessOrEqual(sidewindCut.StartupSeconds, crossStep.StartupSeconds);
            Assert.Less(sidewindCut.RecoverySeconds, crossStep.RecoverySeconds);
            Assert.Less(sidewindCut.ForwardMovement, crossStep.ForwardMovement);
            Assert.Less(sidewindCut.Range, crossStep.Range);
        }

        private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, string label)
        {
            Assert.AreEqual(expected.x, actual.x, 0.001f, label + ".x");
            Assert.AreEqual(expected.y, actual.y, 0.001f, label + ".y");
            Assert.AreEqual(expected.z, actual.z, 0.001f, label + ".z");
        }
    }
}
