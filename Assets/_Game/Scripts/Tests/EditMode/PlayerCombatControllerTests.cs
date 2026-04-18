using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests
{
    public sealed class PlayerCombatControllerTests
    {
        [Test]
        public void LightCombo_StopsAtThirdHit_AndResetsToFirst()
        {
            GameObject gameObject = new GameObject("PlayerCombat");
            gameObject.AddComponent<AttackExecutor>();
            gameObject.AddComponent<HitboxController>();

            AttackDefinitionSO attack1 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO attack2 = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO attack3 = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                PlayerCombatController controller = gameObject.AddComponent<PlayerCombatController>();
                SetPrivateField(controller, "lightAttackCombo", new[] { attack1, attack2, attack3 });

                Assert.AreSame(attack1, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsTrue(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack2, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsTrue(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack3, controller.ResolveAttack(PlayerAttackRequest.Light));
                Assert.IsFalse(controller.CanQueueNextLightAttack);

                controller.NotifyAttackFinished(PlayerAttackRequest.Light);
                Assert.AreSame(attack1, controller.ResolveAttack(PlayerAttackRequest.Light));
            }
            finally
            {
                Object.DestroyImmediate(attack1);
                Object.DestroyImmediate(attack2);
                Object.DestroyImmediate(attack3);
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AnimationEventActivation_UsesPreparedHitboxAndTracksCurrentAttack()
        {
            GameObject attacker = new GameObject("PlayerCombat");
            AttackExecutor executor = attacker.AddComponent<AttackExecutor>();
            HitboxController hitboxController = attacker.AddComponent<HitboxController>();
            PlayerCombatController controller = attacker.AddComponent<PlayerCombatController>();
            GameObject target = CreateTarget("Target", new Vector3(0f, 0f, 1.1f));
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(executor, "attackOrigin", attacker.transform);
                SetPrivateField(hitboxController, "attackExecutor", executor);
                SetPrivateField(controller, "attackExecutor", executor);
                SetPrivateField(controller, "hitboxController", hitboxController);
                SetPrivateField(attack, "animationStateName", "Light_Anim");
                SetPrivateField(attack, "damageMultiplier", 1f);
                SetPrivateField(attack, "hitboxShape", AttackHitboxShape.Box);
                SetPrivateField(attack, "hitboxLocalCenter", new Vector3(0f, 0f, 1.1f));
                SetPrivateField(attack, "hitboxHalfExtents", new Vector3(0.45f, 0.45f, 0.45f));
                SetPrivateField(attack, "hitboxActivationMode", AttackHitboxActivationMode.AnimationEvent);

                hitboxController.Prepare(attack, 12f, attacker);
                controller.NotifyAttackStarted(attack);

                Assert.AreSame(attack, controller.CurrentAttackDefinition);
                Assert.AreEqual("Light_Anim", controller.CurrentAttackAnimationStateName);
                Assert.IsFalse(controller.ActivatePreparedHitboxFromAnimationEvent());

                hitboxController.OpenActivationWindow();

                Assert.IsTrue(controller.ActivatePreparedHitboxFromAnimationEvent());
                Assert.AreEqual(12f, target.GetComponent<TestDamageable>().TotalDamageReceived);

                controller.ClearPreparedHitboxFromAnimationEvent();
                controller.NotifyAttackFinished(PlayerAttackRequest.Heavy);

                Assert.IsNull(controller.CurrentAttackDefinition);
                Assert.AreEqual(string.Empty, controller.CurrentAttackAnimationStateName);
            }
            finally
            {
                Object.DestroyImmediate(attack);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(attacker);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private static GameObject CreateTarget(string name, Vector3 position)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            target.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
            target.AddComponent<TestDamageable>();
            return target;
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public float TotalDamageReceived { get; private set; }

            public void ReceiveDamage(float amount, Vector3 hitPoint, GameObject source)
            {
                TotalDamageReceived += amount;
            }
        }
    }
}
