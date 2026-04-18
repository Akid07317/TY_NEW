using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatTestAnimationAssetWiringTests
    {
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";
        private const string LightAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Light_01.asset";
        private const string HeavyAttackAssetPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string EnemyMeleeAttackAssetPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Melee.asset";
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
        public void PlayerCombatTestPrefab_HasAnimatorControllerAndRelayWired()
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
            Assert.AreSame(animator, GetPrivateField<Animator>(relay, "animator"));
            Assert.AreSame(combatController, GetPrivateField<PlayerCombatController>(relay, "combatController"));
            Assert.AreSame(relay, GetPrivateField<PlayerCombatAnimationRelay>(combatController, "animationRelay"));
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
        public void PlayerAttackAssets_UseAnimationEventActivation_WhileEnemyMeleeRemainsTimed()
        {
            AttackDefinitionSO lightAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(LightAttackAssetPath);
            AttackDefinitionSO heavyAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyAttackAssetPath);
            AttackDefinitionSO enemyMeleeAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnemyMeleeAttackAssetPath);

            Assert.IsNotNull(lightAttack);
            Assert.IsNotNull(heavyAttack);
            Assert.IsNotNull(enemyMeleeAttack);
            Assert.AreEqual(AttackHitboxActivationMode.AnimationEvent, lightAttack.HitboxActivationMode);
            Assert.AreEqual(AttackHitboxActivationMode.AnimationEvent, heavyAttack.HitboxActivationMode);
            Assert.AreEqual(AttackHitboxActivationMode.TimedWindow, enemyMeleeAttack.HitboxActivationMode);
        }

        private static TField GetPrivateField<TField>(object instance, string fieldName) where TField : class
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            return field.GetValue(instance) as TField;
        }
    }
}
