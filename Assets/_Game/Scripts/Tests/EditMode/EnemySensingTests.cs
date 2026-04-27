using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemySensingTests
    {
        [Test]
        public void FindTarget_IgnoresPlayerBehindWall()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            GameObject wallObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                EnemySensing sensing = enemyObject.AddComponent<EnemySensing>();

                targetObject = new GameObject("PlayerTarget");
                targetObject.transform.position = new Vector3(0f, 0f, 2f);
                targetObject.AddComponent<BoxCollider>();
                targetObject.AddComponent<HealthComponent>();
                targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObject.name = "Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 1f);
                wallObject.transform.localScale = new Vector3(2f, 2f, 0.25f);

                Physics.SyncTransforms();

                Assert.IsNull(sensing.FindTarget(enemyObject.transform.position, 4f));
            }
            finally
            {
                if (wallObject != null)
                {
                    Object.DestroyImmediate(wallObject);
                }

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
        public void FindTarget_ReturnsNearestVisiblePlayer()
        {
            GameObject enemyObject = null;
            GameObject nearTargetObject = null;
            GameObject farTargetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                EnemySensing sensing = enemyObject.AddComponent<EnemySensing>();

                nearTargetObject = BuildPlayerTarget("NearPlayerTarget", new Vector3(0f, 0f, 2f));
                farTargetObject = BuildPlayerTarget("FarPlayerTarget", new Vector3(0f, 0f, 3f));
                Physics.SyncTransforms();

                Assert.AreSame(nearTargetObject.transform, sensing.FindTarget(enemyObject.transform.position, 4f));
            }
            finally
            {
                if (farTargetObject != null)
                {
                    Object.DestroyImmediate(farTargetObject);
                }

                if (nearTargetObject != null)
                {
                    Object.DestroyImmediate(nearTargetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void FindTarget_IgnoresDeadPlayerAndReturnsLivingVisiblePlayer()
        {
            GameObject enemyObject = null;
            GameObject deadTargetObject = null;
            GameObject livingTargetObject = null;

            try
            {
                enemyObject = new GameObject("Enemy");
                enemyObject.transform.position = Vector3.zero;
                EnemySensing sensing = enemyObject.AddComponent<EnemySensing>();

                deadTargetObject = BuildPlayerTarget("DeadPlayerTarget", new Vector3(0.9f, 0f, 1.5f));
                deadTargetObject.GetComponent<HealthComponent>().SetCurrent(0f);

                livingTargetObject = BuildPlayerTarget("LivingPlayerTarget", new Vector3(0f, 0f, 2.5f));
                Physics.SyncTransforms();

                Assert.AreSame(livingTargetObject.transform, sensing.FindTarget(enemyObject.transform.position, 4f));
            }
            finally
            {
                if (livingTargetObject != null)
                {
                    Object.DestroyImmediate(livingTargetObject);
                }

                if (deadTargetObject != null)
                {
                    Object.DestroyImmediate(deadTargetObject);
                }

                if (enemyObject != null)
                {
                    Object.DestroyImmediate(enemyObject);
                }
            }
        }

        private static GameObject BuildPlayerTarget(string name, Vector3 position)
        {
            GameObject targetObject = new GameObject(name);
            targetObject.transform.position = position;
            targetObject.AddComponent<BoxCollider>();
            targetObject.AddComponent<HealthComponent>();
            targetObject.AddComponent<DamageableReceiver>();
            targetObject.AddComponent<PlayerCharacter>();
            return targetObject;
        }
    }
}
