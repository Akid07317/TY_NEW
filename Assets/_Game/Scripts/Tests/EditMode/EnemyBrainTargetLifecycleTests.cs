using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyBrainTargetLifecycleTests
    {
        [Test]
        public void Update_ClearsCurrentTarget_WhenTrackedTargetIsDead()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("DeadTarget");
                targetObject.AddComponent<PlayerCharacter>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetHealth.SetMax(100f, refillCurrent: true);

                brain.SetTarget(targetObject.transform);
                Assert.AreSame(targetObject.transform, brain.CurrentTarget);

                targetHealth.SetCurrent(0f);
                InvokeMethod(brain, "Update");

                Assert.IsNull(brain.CurrentTarget);
            }
            finally
            {
                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void SetTarget_IgnoresDeadTarget()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("DeadTarget");
                targetObject.AddComponent<PlayerCharacter>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                targetHealth.SetCurrent(0f);

                brain.SetTarget(targetObject.transform);

                Assert.IsNull(brain.CurrentTarget);
            }
            finally
            {
                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void Update_ClearsCurrentTarget_WhenTrackedTargetBecomesInactive()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("InactiveTarget");
                targetObject.AddComponent<PlayerCharacter>();
                targetObject.AddComponent<HealthComponent>().SetMax(100f, refillCurrent: true);

                brain.SetTarget(targetObject.transform);
                Assert.AreSame(targetObject.transform, brain.CurrentTarget);

                targetObject.SetActive(false);
                InvokeMethod(brain, "Update");

                Assert.IsNull(brain.CurrentTarget);
            }
            finally
            {
                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void SetTarget_IgnoresInactiveTarget()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                EnemyBrain brain = enemyObject.AddComponent<EnemyBrain>();

                targetObject = new GameObject("InactiveTarget");
                targetObject.AddComponent<PlayerCharacter>();
                targetObject.AddComponent<HealthComponent>().SetMax(100f, refillCurrent: true);
                targetObject.SetActive(false);

                brain.SetTarget(targetObject.transform);

                Assert.IsNull(brain.CurrentTarget);
            }
            finally
            {
                if (targetObject != null)
                {
                    Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        private static void InvokeMethod(object instance, string methodName)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, methodName);
            method.Invoke(instance, null);
        }
    }
}
