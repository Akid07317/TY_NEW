using System.Collections.Generic;
using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Save;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CheckpointRestoreExecutorTests
    {
        [Test]
        public void Apply_RestoresProgressAndExecutesParticipantPhasesAroundPlayerRestore()
        {
            GameObject playerObject = null;
            GameObject checkpointServiceObject = null;
            GameObject progressServiceObject = null;
            GameObject interactionObject = null;
            GameObject encounterObject = null;
            GameObject enemyObject = null;
            DummyCheckpointRestoreParticipant interactionParticipant = null;
            DummyCheckpointRestoreParticipant encounterParticipant = null;
            DummyCheckpointRestoreParticipant enemyParticipant = null;
            List<string> restoreOrder = new List<string>();

            try
            {
                PlayerCharacter player = CreatePlayer(out playerObject);
                checkpointServiceObject = new GameObject("CheckpointService");
                CheckpointService checkpointService = checkpointServiceObject.AddComponent<CheckpointService>();
                progressServiceObject = new GameObject("ChapterProgressService");
                ChapterProgressService chapterProgressService = progressServiceObject.AddComponent<ChapterProgressService>();

                interactionParticipant = CreateParticipant(
                    CheckpointRestoreGroup.Interaction,
                    0,
                    "interaction",
                    restoreOrder,
                    out interactionObject);
                encounterParticipant = CreateParticipant(
                    CheckpointRestoreGroup.Encounter,
                    0,
                    "encounter",
                    restoreOrder,
                    out encounterObject);
                enemyParticipant = CreateParticipant(
                    CheckpointRestoreGroup.Enemy,
                    0,
                    "enemy",
                    restoreOrder,
                    out enemyObject);

                CheckpointRestoreSceneResetter.RegisterParticipant(interactionParticipant);
                CheckpointRestoreSceneResetter.RegisterParticipant(encounterParticipant);
                CheckpointRestoreSceneResetter.RegisterParticipant(enemyParticipant);

                player.Health.SetMax(150f, true);
                player.Mana.SetMax(80f, true);
                player.Health.SetCurrent(12f);
                player.Mana.SetCurrent(5f);
                player.Health.Changed += HandlePlayerHealthChanged;

                CheckpointRestoreExecutor.Apply(
                    "ChapterBoss",
                    player,
                    checkpointService,
                    chapterProgressService,
                    new ChapterSaveData
                    {
                        checkpointId = "CP_BOSS",
                        currentAreaId = "AREA_BOSS",
                        visitedAreaIds = new[] { "AREA_START" },
                        keyItemIds = new[] { "BOSS_KEY" },
                        clearedEncounterIds = new[] { "E_PRE" },
                        chapterCompleted = true
                    },
                    new CheckpointRestoreSnapshot(
                        "CP_BOSS",
                        new Vector3(12f, 0f, -6f),
                        Quaternion.Euler(0f, 90f, 0f),
                        75f,
                        42f));

                player.Health.Changed -= HandlePlayerHealthChanged;

                Assert.AreEqual("CP_BOSS", checkpointService.CurrentCheckpointId);
                Assert.AreEqual("AREA_BOSS", chapterProgressService.CurrentAreaId);
                Assert.IsTrue(chapterProgressService.HasVisitedArea("AREA_START"));
                Assert.IsTrue(chapterProgressService.HasVisitedArea("AREA_BOSS"));
                Assert.IsTrue(chapterProgressService.HasKeyItem("BOSS_KEY"));
                Assert.IsTrue(chapterProgressService.IsEncounterCleared("E_PRE"));
                Assert.IsTrue(chapterProgressService.IsChapterCompleted);
                Assert.That(Vector3.Distance(new Vector3(12f, 0f, -6f), player.transform.position), Is.LessThan(0.01f));
                Assert.That(
                    Quaternion.Angle(Quaternion.Euler(0f, 90f, 0f), player.transform.rotation),
                    Is.LessThan(0.01f));
                Assert.AreEqual(75f, player.Health.CurrentValue, 0.01f);
                Assert.AreEqual(42f, player.Mana.CurrentValue, 0.01f);
                CollectionAssert.AreEqual(
                    new[] { "interaction", "player", "encounter", "enemy" },
                    restoreOrder);
            }
            finally
            {
                if (interactionParticipant != null)
                {
                    CheckpointRestoreSceneResetter.UnregisterParticipant(interactionParticipant);
                }

                if (encounterParticipant != null)
                {
                    CheckpointRestoreSceneResetter.UnregisterParticipant(encounterParticipant);
                }

                if (enemyParticipant != null)
                {
                    CheckpointRestoreSceneResetter.UnregisterParticipant(enemyParticipant);
                }

                Cleanup(
                    enemyObject,
                    encounterObject,
                    interactionObject,
                    progressServiceObject,
                    checkpointServiceObject,
                    playerObject);
            }

            void HandlePlayerHealthChanged(float currentValue, float maxValue)
            {
                if (Mathf.Abs(currentValue - 75f) <= 0.01f)
                {
                    restoreOrder.Add("player");
                }
            }
        }

        private static PlayerCharacter CreatePlayer(out GameObject playerObject)
        {
            playerObject = new GameObject("Player");
            playerObject.AddComponent<CharacterController>();
            playerObject.AddComponent<DamageableReceiver>();
            HealthComponent health = playerObject.AddComponent<HealthComponent>();
            ManaComponent mana = playerObject.AddComponent<ManaComponent>();
            PlayerMotor motor = playerObject.AddComponent<PlayerMotor>();
            PlayerCharacter player = playerObject.AddComponent<PlayerCharacter>();
            SetPrivateField(player, "health", health);
            SetPrivateField(player, "mana", mana);
            SetPrivateField(player, "motor", motor);
            return player;
        }

        private static DummyCheckpointRestoreParticipant CreateParticipant(
            CheckpointRestoreGroup group,
            int priority,
            string label,
            List<string> restoreOrder,
            out GameObject participantObject)
        {
            participantObject = new GameObject(label);
            DummyCheckpointRestoreParticipant participant = participantObject.AddComponent<DummyCheckpointRestoreParticipant>();
            participant.Initialize(group, priority, label, restoreOrder);
            return participant;
        }

        private static void Cleanup(params Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    Object.DestroyImmediate(objects[i]);
                }
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }

        private sealed class DummyCheckpointRestoreParticipant : MonoBehaviour, ICheckpointRestoreParticipant
        {
            private List<string> restoreOrder;
            private string label;

            public CheckpointRestoreGroup RestoreGroup { get; private set; }

            public int RestorePriority { get; private set; }

            public void Initialize(
                CheckpointRestoreGroup restoreGroup,
                int restorePriority,
                string restoreLabel,
                List<string> order)
            {
                RestoreGroup = restoreGroup;
                RestorePriority = restorePriority;
                label = restoreLabel;
                restoreOrder = order;
            }

            public void ResetForCheckpointRestore()
            {
                restoreOrder.Add(label);
            }
        }
    }
}
