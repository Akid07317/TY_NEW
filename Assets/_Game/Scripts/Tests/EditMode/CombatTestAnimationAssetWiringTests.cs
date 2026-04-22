using System.Reflection;
using System.Linq;
using CampusRPG.AI;
using CampusRPG.Editor;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

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
            "Assets/JC_LP_MedievalCharacters_LITE/"
        };
        private static readonly string[] EnemyPrefabPaths =
        {
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab",
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab",
            "Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab"
        };
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";
        private const string PlayerIdleClipPath = "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Idle_CombatTest.anim";
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
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Hit_CombatTest.anim",
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_Death_CombatTest.anim"
        };
        private const string LightAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Light_01.asset";
        private const string HeavyAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string EnemyMeleeAttackAssetPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Melee.asset";
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
            "Hit",
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
            "Assets/_Game/Animations/Characters/CombatTest/AN_Player_DodgeFollowUp_Enhanced_CombatTest.anim"
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
            Assert.Greater(GetPrivateValueField<float>(relay, "dodgeAnimationDurationSeconds"), 0.4f);
            Assert.Greater(GetPrivateValueField<float>(relay, "hitAnimationDurationSeconds"), 0.3f);
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
                Assert.IsNull(prefab.GetComponent<Animator>(), EnemyPrefabPaths[i]);
                Assert.IsNull(prefab.GetComponent<EnemyCombatAnimationRelay>(), EnemyPrefabPaths[i]);
                Assert.IsNull(prefab.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), EnemyPrefabPaths[i]);
                Assert.IsNull(proxyRoot.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName), EnemyPrefabPaths[i]);
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
        public void PlayerAttackClips_HaveHitboxAnimationEvents()
        {
            for (int i = 0; i < AttackClipPaths.Length; i++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackClipPaths[i]);

                Assert.IsNotNull(clip, AttackClipPaths[i]);
                Assert.Greater(clip.length, 0f, AttackClipPaths[i]);

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);

                Assert.That(events, Has.Length.GreaterThanOrEqualTo(2), AttackClipPaths[i]);
                Assert.That(events, Has.Some.Matches<AnimationEvent>(animationEvent => animationEvent.functionName == "AnimationEvent_OpenAttackHitbox"), AttackClipPaths[i]);
                Assert.That(events, Has.Some.Matches<AnimationEvent>(animationEvent => animationEvent.functionName == "AnimationEvent_CloseAttackHitbox"), AttackClipPaths[i]);
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

        private static bool IsImportedSourcePath(string path)
        {
            return ImportedSourceRoots.Any(root => path.StartsWith(root));
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
}
