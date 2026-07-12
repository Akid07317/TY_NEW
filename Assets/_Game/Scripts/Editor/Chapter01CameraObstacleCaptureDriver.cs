#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CampusRPG.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter01CameraObstacleCaptureDriverMenu
    {
        private const string StartMenu = "CampusRPG/Debug/Chapter01/Start Camera Obstacle Gauntlet";
        private const string NextMenu = "CampusRPG/Debug/Chapter01/Next Camera Obstacle Case";
        private const string StopMenu = "CampusRPG/Debug/Chapter01/Stop Camera Obstacle Gauntlet";
        private const string ChapterSceneName = "Chapter01_Combined";
        private const string ChapterSaveFileName = "slot_auto_chapter01.json";
        private const string InteriorEncounterName = "Encounter_EN_A03_INTERIOR";
        private const string EntryBarrierName = "InteriorEncounterBarrier_Entry";
        private const string SigilBarrierName = "InteriorEncounterBarrier_Sigil";
        private const string TargetEnemyName = "Enemy_A03_Melee_A";
        private const string PendingKey = "TY_NEW.CameraGauntlet.Pending";
        private const string ActiveKey = "TY_NEW.CameraGauntlet.Active";
        private const string AbortAfterReloadKey = "TY_NEW.CameraGauntlet.AbortAfterReload";
        private const string OriginalSavePathKey = "TY_NEW.CameraGauntlet.OriginalSavePath";
        private const string BackupSavePathKey = "TY_NEW.CameraGauntlet.BackupSavePath";
        private const string OriginalSaveExistsKey = "TY_NEW.CameraGauntlet.OriginalSaveExists";
        private const string OriginalSaveHashKey = "TY_NEW.CameraGauntlet.OriginalSaveHash";

        private static readonly string[] InteriorEnemyNames =
        {
            TargetEnemyName,
            "Enemy_A03_Mobile_A",
            "Enemy_A03_Ranged_A"
        };

        private static readonly CameraGauntletCase[] Cases =
        {
            new CameraGauntletCase(
                "wide-wall",
                "1/5 Wall Back / Wide Wall",
                new Vector3(-5.8f, 0.1f, 48.8f),
                new Vector3(-3.5f, 0f, 54.5f),
                "Real input: Tab, hold S into the wall, strafe A/D, then Light."),
            new CameraGauntletCase(
                "pillar-orbit",
                "2/5 Pillar Orbit",
                new Vector3(0f, 0.1f, 54f),
                new Vector3(0f, 0f, 60f),
                "Real input: Tab, orbit with A for 2s and D for 2s, then Dodge or Light."),
            new CameraGauntletCase(
                "narrow-hall",
                "3/5 Narrow Hall Center",
                new Vector3(0f, 0.1f, 55.5f),
                new Vector3(0f, 0f, 60.5f),
                "Real input: Tab, move W/S on the center line, then directional Dodge."),
            new CameraGauntletCase(
                "back-left-corner",
                "4/5 Back-Left Corner",
                new Vector3(-8f, 0.1f, 48f),
                new Vector3(-3.5f, 0f, 54.5f),
                "Real input: Tab, press S+A/D against the corner, then Heavy or Dodge."),
            new CameraGauntletCase(
                "mantle-edge",
                "5/5 Mantle Edge",
                new Vector3(0f, 0.1f, 46.45f),
                new Vector3(0f, 0f, 52f),
                "Real input: Tab, press Space to mantle, then Light or Dodge after landing.")
        };

        private static readonly List<EnemyRuntimeSnapshot> EnemySnapshots = new List<EnemyRuntimeSnapshot>();

        private static bool driverActive;
        private static bool runtimeSnapshotCaptured;
        private static bool cleanupInProgress;
        private static int currentCaseIndex = -1;
        private static PlayerCharacter player;
        private static ThirdPersonCameraController cameraController;
        private static Chapter01CameraObstacleTelemetryOverlay telemetryOverlay;
        private static CheckpointRestoreCoordinator checkpointCoordinator;
        private static EncounterController interiorEncounter;
        private static GameObject entryBarrier;
        private static GameObject sigilBarrier;
        private static GameObject targetEnemy;
        private static bool checkpointCoordinatorWasEnabled;
        private static bool interiorEncounterWasEnabled;
        private static bool interiorEncounterWasActive;
        private static bool interiorEncounterWasCleared;
        private static bool entryBarrierWasActive;
        private static bool sigilBarrierWasActive;
        private static Vector3 originalPlayerPosition;
        private static Quaternion originalPlayerRotation;
        private static float originalPlayerHealth;
        private static float originalPlayerMana;
        private static float originalPlayerCounterGauge;
        private static float originalPlayerAgilityGauge;
        private static Transform originalPlayerLockTarget;
        private static Vector3 originalCameraPosition;
        private static Quaternion originalCameraRotation;
        private static float originalCameraYaw;
        private static float originalCameraPitch;
        private static Transform originalCameraFollowTarget;
        private static Transform originalCameraLockTarget;
        private static bool originalCameraLockOnActive;

        static Chapter01CameraObstacleCaptureDriverMenu()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.quitting -= HandleEditorQuitting;
            EditorApplication.quitting += HandleEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;

            if (SessionState.GetBool(AbortAfterReloadKey, false))
            {
                EditorApplication.delayCall += ExitPlayModeAfterUnexpectedReload;
            }
            else if (SessionState.GetBool(ActiveKey, false)
                && !SessionState.GetBool(PendingKey, false)
                && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += RecoverOrphanedSaveBackup;
            }
        }

        [MenuItem(StartMenu)]
        public static void StartGauntlet()
        {
            if (IsBatchOrTestRun())
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[TY_NEW CameraGauntlet] Start is only available from Edit Mode.");
                return;
            }

            if (!IsChapterSceneActive())
            {
                Debug.LogWarning(
                    "[TY_NEW CameraGauntlet] Open Chapter01_Combined before starting the camera obstacle gauntlet.");
                return;
            }

            if (SessionState.GetBool(ActiveKey, false)
                && !RestoreSaveBackup("stale session before start"))
            {
                Debug.LogError(
                    "[TY_NEW CameraGauntlet] A previous save backup could not be restored. Start aborted.");
                return;
            }

            try
            {
                CreateSaveBackup();
                SessionState.SetBool(PendingKey, true);
                SessionState.SetBool(AbortAfterReloadKey, false);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RestoreSaveBackup("start failure");
                ClearSessionState();
            }
        }

        [MenuItem(NextMenu)]
        public static void NextCase()
        {
            if (!EditorApplication.isPlaying || !driverActive)
            {
                Debug.LogWarning("[TY_NEW CameraGauntlet] Start the gauntlet before advancing cases.");
                return;
            }

            LogCurrentMetrics("advance");

            if (currentCaseIndex + 1 >= Cases.Length)
            {
                Debug.Log(
                    "[TY_NEW CameraGauntlet] All five cases were presented. " +
                    "No automatic PASS was produced; record the visual sign-off, then choose Stop.");
                return;
            }

            try
            {
                StageCase(currentCaseIndex + 1);
            }
            catch (Exception exception)
            {
                AbortAfterFailure("case staging", exception);
            }
        }

        [MenuItem(StopMenu)]
        public static void StopGauntlet()
        {
            LogCurrentMetrics("stop requested");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            CleanupRuntimeState("stop in Edit Mode");
            RestoreSaveBackup("stop in Edit Mode");
            ClearSessionState();
        }

        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode
                && SessionState.GetBool(PendingKey, false))
            {
                AttachDriver();
                return;
            }

            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                LogCurrentMetrics("play mode exit");
                CleanupRuntimeState("play mode exit");
                bool restored = RestoreSaveBackup("play mode exit");

                if (restored)
                {
                    ClearSessionState();
                }

                return;
            }

            if (change == PlayModeStateChange.EnteredEditMode
                && SessionState.GetBool(ActiveKey, false))
            {
                bool restored = RestoreSaveBackup("entered Edit Mode recovery");

                if (restored)
                {
                    ClearSessionState();
                }
            }
        }

        private static void AttachDriver()
        {
            SessionState.SetBool(PendingKey, false);

            try
            {
                if (!IsChapterSceneActive())
                {
                    throw new InvalidOperationException("Chapter01_Combined is not the active Play Mode scene.");
                }

                ResolveRuntimeObjects();
                CaptureRuntimeSnapshot();
                PrepareRuntimeGauntlet();
                driverActive = true;
                StageCase(0);
                Debug.Log(
                    "[TY_NEW CameraGauntlet] Started. This driver only stages geometry and records metrics; " +
                    "it never injects combat input and never emits an automatic PASS.");
            }
            catch (Exception exception)
            {
                AbortAfterFailure("attach", exception);
            }
        }

        private static void ResolveRuntimeObjects()
        {
            player = RequireComponent<PlayerCharacter>("Player");
            cameraController = RequireComponent<ThirdPersonCameraController>("Main Camera");
            checkpointCoordinator = RequireComponent<CheckpointRestoreCoordinator>("ChapterFlow");
            interiorEncounter = RequireComponent<EncounterController>(InteriorEncounterName);
            entryBarrier = RequireSceneObject(EntryBarrierName);
            sigilBarrier = RequireSceneObject(SigilBarrierName);
            targetEnemy = RequireSceneObject(TargetEnemyName);

            EnemySnapshots.Clear();

            for (int i = 0; i < InteriorEnemyNames.Length; i++)
            {
                EnemySnapshots.Add(new EnemyRuntimeSnapshot(RequireSceneObject(InteriorEnemyNames[i])));
            }
        }

        private static void CaptureRuntimeSnapshot()
        {
            checkpointCoordinatorWasEnabled = checkpointCoordinator.enabled;
            interiorEncounterWasEnabled = interiorEncounter.enabled;
            interiorEncounterWasActive = interiorEncounter.IsActive;
            interiorEncounterWasCleared = interiorEncounter.IsCleared;
            entryBarrierWasActive = entryBarrier.activeSelf;
            sigilBarrierWasActive = sigilBarrier.activeSelf;
            originalPlayerPosition = player.transform.position;
            originalPlayerRotation = player.transform.rotation;
            originalPlayerHealth = player.Health != null ? player.Health.CurrentValue : 0f;
            originalPlayerMana = player.Mana != null ? player.Mana.CurrentValue : 0f;
            originalPlayerCounterGauge = player.Gauges != null ? player.Gauges.CounterGauge : 0f;
            originalPlayerAgilityGauge = player.Gauges != null ? player.Gauges.AgilityGauge : 0f;
            originalPlayerLockTarget = player.LockOnTargetSelector != null
                ? player.LockOnTargetSelector.CurrentTarget
                : null;
            originalCameraPosition = cameraController.transform.position;
            originalCameraRotation = cameraController.transform.rotation;
            originalCameraYaw = GetPrivateField(cameraController, "yaw", 0f);
            originalCameraPitch = GetPrivateField(cameraController, "pitch", 10f);
            originalCameraFollowTarget = cameraController.FollowTarget;
            originalCameraLockTarget = cameraController.LockOnTarget;
            originalCameraLockOnActive = cameraController.IsLockOnActive;
            runtimeSnapshotCaptured = true;
        }

        private static void PrepareRuntimeGauntlet()
        {
            checkpointCoordinator.enabled = false;
            interiorEncounter.enabled = false;
            entryBarrier.SetActive(false);
            sigilBarrier.SetActive(false);

            for (int i = 0; i < EnemySnapshots.Count; i++)
            {
                EnemySnapshots[i].GameObject.SetActive(false);
            }

            targetEnemy.SetActive(true);
            FreezeEnemy(targetEnemy);

            Chapter01CameraObstacleTelemetryOverlay existingOverlay =
                cameraController.GetComponent<Chapter01CameraObstacleTelemetryOverlay>();

            if (existingOverlay != null)
            {
                Object.DestroyImmediate(existingOverlay);
            }

            telemetryOverlay = cameraController.gameObject.AddComponent<Chapter01CameraObstacleTelemetryOverlay>();
        }

        private static void StageCase(int caseIndex)
        {
            if (caseIndex < 0 || caseIndex >= Cases.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(caseIndex));
            }

            CameraGauntletCase captureCase = Cases[caseIndex];
            currentCaseIndex = caseIndex;

            player.LockOnTargetSelector?.ResetRuntimeState();
            player.CombatController?.ResetRuntimeState();
            player.Motor?.ResetMotion();
            player.StateMachine?.SwitchToLocomotion();

            if (player.Motor != null)
            {
                player.Motor.WarpTo(captureCase.PlayerPosition, Quaternion.identity);
            }
            else
            {
                player.transform.SetPositionAndRotation(captureCase.PlayerPosition, Quaternion.identity);
            }

            targetEnemy.SetActive(true);
            FreezeEnemy(targetEnemy);
            targetEnemy.transform.SetPositionAndRotation(
                captureCase.TargetPosition,
                ResolveFacingRotation(captureCase.TargetPosition, captureCase.PlayerPosition));
            targetEnemy.GetComponent<HealthComponent>()?.RestoreFull();

            cameraController.ResetRuntimeState();
            SetPrivateField(cameraController, "yaw", 0f);
            SetPrivateField(cameraController, "pitch", 14f);
            Vector3 desiredCameraPosition = ThirdPersonCameraOrbitUtility.ResolveDesiredPosition(
                captureCase.PlayerPosition,
                new Vector3(0f, 1.8f, -4.5f),
                0f,
                14f);
            cameraController.transform.position = desiredCameraPosition;
            Vector3 lookPoint = captureCase.PlayerPosition + Vector3.up * 1.5f;
            Vector3 lookDirection = lookPoint - desiredCameraPosition;

            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                cameraController.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            Physics.SyncTransforms();
            telemetryOverlay.Configure(
                player,
                cameraController,
                targetEnemy.transform,
                captureCase.Id,
                captureCase.Label,
                captureCase.Instructions,
                caseIndex + 1,
                Cases.Length);

            Debug.Log(
                $"[TY_NEW CameraGauntlet] CASE id={captureCase.Id} label=\"{captureCase.Label}\" " +
                $"player={FormatVector(captureCase.PlayerPosition)} target={FormatVector(captureCase.TargetPosition)} " +
                $"instruction=\"{captureCase.Instructions}\" manualSignoff=required.");
        }

        private static void FreezeEnemy(GameObject enemyObject)
        {
            EnemyBrain brain = enemyObject.GetComponent<EnemyBrain>();
            EnemyStateMachine stateMachine = enemyObject.GetComponent<EnemyStateMachine>();
            EnemyAttackController attackController = enemyObject.GetComponent<EnemyAttackController>();
            EnemyMotor motor = enemyObject.GetComponent<EnemyMotor>();
            NavMeshAgent agent = enemyObject.GetComponent<NavMeshAgent>();

            brain?.ClearTarget();
            attackController?.ResetRuntimeState();
            motor?.Stop();

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }

                agent.enabled = false;
            }

            if (motor != null)
            {
                motor.enabled = false;
            }

            if (attackController != null)
            {
                attackController.enabled = false;
            }

            if (stateMachine != null)
            {
                stateMachine.enabled = false;
            }

            if (brain != null)
            {
                brain.enabled = false;
            }
        }

        private static void LogCurrentMetrics(string reason)
        {
            if (telemetryOverlay == null || currentCaseIndex < 0 || currentCaseIndex >= Cases.Length)
            {
                return;
            }

            Debug.Log(
                $"[TY_NEW CameraGauntlet] METRICS reason=\"{reason}\" " +
                telemetryOverlay.BuildMetricLog() +
                " manualSignoff=required automaticPass=false.");
        }

        private static void AbortAfterFailure(string operation, Exception exception)
        {
            Debug.LogError($"[TY_NEW CameraGauntlet] Aborting after {operation} failure.");
            Debug.LogException(exception);
            CleanupRuntimeState(operation + " failure");
            bool restored = RestoreSaveBackup(operation + " failure");

            if (restored)
            {
                ClearSessionState();
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void CleanupRuntimeState(string reason)
        {
            if (cleanupInProgress)
            {
                return;
            }

            cleanupInProgress = true;

            try
            {
                Exception cleanupFailure = null;
                RunCleanupStep(
                    "telemetry overlay",
                    () =>
                    {
                        if (telemetryOverlay != null)
                        {
                            Object.DestroyImmediate(telemetryOverlay);
                        }

                        telemetryOverlay = null;
                    },
                    ref cleanupFailure);

                if (runtimeSnapshotCaptured)
                {
                    RunCleanupStep(
                        "player baseline",
                        () =>
                        {
                            if (player == null)
                            {
                                return;
                            }

                            player.LockOnTargetSelector?.ResetRuntimeState();
                            player.CombatController?.ResetRuntimeState();
                            player.Motor?.ResetMotion();
                            player.StateMachine?.SwitchToLocomotion();

                            if (player.Motor != null)
                            {
                                player.Motor.WarpTo(originalPlayerPosition, originalPlayerRotation);
                            }
                            else
                            {
                                player.transform.SetPositionAndRotation(originalPlayerPosition, originalPlayerRotation);
                            }

                            player.Health?.SetCurrent(originalPlayerHealth);
                            player.Mana?.SetCurrent(originalPlayerMana);
                            RestorePlayerGauges(
                                player.Gauges,
                                originalPlayerCounterGauge,
                                originalPlayerAgilityGauge);
                        },
                        ref cleanupFailure);

                    RunCleanupStep(
                        "camera baseline",
                        () =>
                        {
                            if (cameraController == null)
                            {
                                return;
                            }

                            cameraController.ResetRuntimeState();
                            cameraController.SetFollowTarget(originalCameraFollowTarget);
                            SetPrivateField(cameraController, "yaw", originalCameraYaw);
                            SetPrivateField(cameraController, "pitch", originalCameraPitch);
                            cameraController.transform.SetPositionAndRotation(
                                originalCameraPosition,
                                originalCameraRotation);
                        },
                        ref cleanupFailure);

                    RunCleanupStep(
                        "player and camera lock target",
                        () =>
                        {
                            RestorePlayerLockOnTarget(player?.LockOnTargetSelector, originalPlayerLockTarget);

                            if (cameraController != null)
                            {
                                cameraController.SetLockOnTarget(originalCameraLockTarget);
                                cameraController.SetLockOnActive(originalCameraLockOnActive);
                            }
                        },
                        ref cleanupFailure);

                    RunCleanupStep(
                        "checkpoint coordinator enabled state",
                        () =>
                        {
                            if (checkpointCoordinator != null)
                            {
                                checkpointCoordinator.enabled = checkpointCoordinatorWasEnabled;
                            }
                        },
                        ref cleanupFailure);

                    RunCleanupStep(
                        "interior encounter enabled state",
                        () =>
                        {
                            if (interiorEncounter != null)
                            {
                                interiorEncounter.enabled = interiorEncounterWasEnabled;
                            }
                        },
                        ref cleanupFailure);

                    for (int i = 0; i < EnemySnapshots.Count; i++)
                    {
                        EnemyRuntimeSnapshot snapshot = EnemySnapshots[i];
                        RunCleanupStep(
                            $"enemy snapshot {snapshot.GameObject?.name ?? i.ToString()}",
                            snapshot.Restore,
                            ref cleanupFailure);
                    }

                    RunCleanupStep(
                        "interior barriers",
                        () =>
                        {
                            entryBarrier?.SetActive(entryBarrierWasActive);
                            sigilBarrier?.SetActive(sigilBarrierWasActive);
                        },
                        ref cleanupFailure);

                    RunCleanupStep(
                        "interior encounter runtime flags",
                        () =>
                        {
                            if (interiorEncounter == null)
                            {
                                return;
                            }

                            SetRequiredPrivateField(interiorEncounter, "isActive", interiorEncounterWasActive);
                            SetRequiredPrivateField(interiorEncounter, "isCleared", interiorEncounterWasCleared);
                        },
                        ref cleanupFailure);

                    RunCleanupStep("physics transforms", Physics.SyncTransforms, ref cleanupFailure);
                }

                if (cleanupFailure == null)
                {
                    Debug.Log($"[TY_NEW CameraGauntlet] Runtime staging cleaned up ({reason}).");
                }
                else
                {
                    Debug.LogError(
                        $"[TY_NEW CameraGauntlet] Runtime cleanup completed with errors ({reason}). " +
                        "Play Mode will still exit and the save backup restore will still run.");
                }
            }
            finally
            {
                driverActive = false;
                runtimeSnapshotCaptured = false;
                currentCaseIndex = -1;
                player = null;
                cameraController = null;
                checkpointCoordinator = null;
                interiorEncounter = null;
                entryBarrier = null;
                sigilBarrier = null;
                targetEnemy = null;
                originalPlayerLockTarget = null;
                originalCameraFollowTarget = null;
                originalCameraLockTarget = null;
                EnemySnapshots.Clear();
                cleanupInProgress = false;
            }
        }

        private static void RunCleanupStep(string label, Action cleanupAction, ref Exception firstFailure)
        {
            try
            {
                cleanupAction?.Invoke();
            }
            catch (Exception exception)
            {
                firstFailure ??= exception;
                Debug.LogError($"[TY_NEW CameraGauntlet] Cleanup step failed: {label}.");
                Debug.LogException(exception);
            }
        }

        private static void RestorePlayerGauges(
            GaugeComponent gauges,
            float counterGauge,
            float agilityGauge)
        {
            if (gauges == null)
            {
                return;
            }

            gauges.ResetAll();
            gauges.AddCounter(counterGauge);
            gauges.AddAgility(agilityGauge);
        }

        private static void RestorePlayerLockOnTarget(LockOnTargetSelector selector, Transform target)
        {
            if (selector == null)
            {
                return;
            }

            MethodInfo setCurrentTarget = typeof(LockOnTargetSelector).GetMethod(
                "SetCurrentTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (setCurrentTarget == null)
            {
                throw new MissingMethodException(typeof(LockOnTargetSelector).FullName, "SetCurrentTarget");
            }

            setCurrentTarget.Invoke(selector, new object[] { target });
        }

        private static void CreateSaveBackup()
        {
            string originalPath = Path.Combine(
                Application.persistentDataPath,
                "Save",
                ChapterSaveFileName);
            bool originalExists = File.Exists(originalPath);
            string backupPath = string.Empty;
            string originalHash = "absent";

            if (originalExists)
            {
                backupPath = Path.Combine(
                    Path.GetTempPath(),
                    $"TY_NEW_chapter01_camera_gauntlet_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json.bak");
                File.Copy(originalPath, backupPath, false);
                originalHash = ComputeSha256(backupPath);
                string liveHash = ComputeSha256(originalPath);

                if (!string.Equals(liveHash, originalHash, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(backupPath);
                    throw new IOException(
                        "The Chapter01 save changed while its camera-gauntlet snapshot was being created.");
                }
            }

            SessionState.SetString(OriginalSavePathKey, originalPath);
            SessionState.SetString(BackupSavePathKey, backupPath);
            SessionState.SetBool(OriginalSaveExistsKey, originalExists);
            SessionState.SetString(OriginalSaveHashKey, originalHash);
            SessionState.SetBool(ActiveKey, true);

            Debug.Log(
                $"[TY_NEW CameraGauntlet] Chapter01 save snapshot created: " +
                $"exists={originalExists} sha256={originalHash} backup=\"{backupPath}\".");
        }

        private static bool RestoreSaveBackup(string reason)
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                return true;
            }

            string originalPath = SessionState.GetString(OriginalSavePathKey, string.Empty);
            string backupPath = SessionState.GetString(BackupSavePathKey, string.Empty);
            bool originalExists = SessionState.GetBool(OriginalSaveExistsKey, false);
            string expectedHash = SessionState.GetString(OriginalSaveHashKey, string.Empty);

            try
            {
                if (string.IsNullOrWhiteSpace(originalPath))
                {
                    throw new InvalidOperationException("The original Chapter01 save path is missing.");
                }

                if (originalExists)
                {
                    if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                    {
                        throw new FileNotFoundException("The Chapter01 save backup is missing.", backupPath);
                    }

                    string directory = Path.GetDirectoryName(originalPath);

                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(backupPath, originalPath, true);
                    string restoredHash = ComputeSha256(originalPath);

                    if (!string.Equals(restoredHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            $"Chapter01 save hash mismatch after restore. expected={expectedHash} actual={restoredHash}");
                    }

                    File.Delete(backupPath);
                    Debug.Log(
                        $"[TY_NEW CameraGauntlet] Chapter01 save restored byte-for-byte " +
                        $"({reason}), sha256={restoredHash}.");
                }
                else
                {
                    if (File.Exists(originalPath))
                    {
                        File.Delete(originalPath);
                    }

                    Debug.Log(
                        $"[TY_NEW CameraGauntlet] Chapter01 save absence restored ({reason}); " +
                        "the gauntlet-created slot was removed.");
                }

                SessionState.SetBool(ActiveKey, false);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[TY_NEW CameraGauntlet] Chapter01 save restore failed ({reason}). " +
                    $"Backup retained at \"{backupPath}\".");
                Debug.LogException(exception);
                return false;
            }
        }

        private static void HandleBeforeAssemblyReload()
        {
            bool isNormalPlayEntryReload = SessionState.GetBool(PendingKey, false)
                && !EditorApplication.isPlaying
                && EditorApplication.isPlayingOrWillChangePlaymode;

            if (isNormalPlayEntryReload)
            {
                return;
            }

            if (!SessionState.GetBool(ActiveKey, false) && !driverActive)
            {
                return;
            }

            LogCurrentMetrics("assembly reload");
            CleanupRuntimeState("assembly reload");
            bool restored = RestoreSaveBackup("assembly reload");

            if (restored)
            {
                ClearSessionState();
            }

            if (EditorApplication.isPlaying)
            {
                SessionState.SetBool(AbortAfterReloadKey, true);
            }
        }

        private static void HandleEditorQuitting()
        {
            LogCurrentMetrics("editor quit");
            CleanupRuntimeState("editor quit");
            bool restored = RestoreSaveBackup("editor quit");

            if (restored)
            {
                ClearSessionState();
            }
        }

        private static void ExitPlayModeAfterUnexpectedReload()
        {
            SessionState.SetBool(AbortAfterReloadKey, false);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "[TY_NEW CameraGauntlet] Play Mode is exiting because scripts reloaded during an active gauntlet.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void RecoverOrphanedSaveBackup()
        {
            bool restored = RestoreSaveBackup("orphaned editor session recovery");

            if (restored)
            {
                ClearSessionState();
            }
        }

        private static void ClearSessionState()
        {
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetBool(AbortAfterReloadKey, false);
            SessionState.SetString(OriginalSavePathKey, string.Empty);
            SessionState.SetString(BackupSavePathKey, string.Empty);
            SessionState.SetBool(OriginalSaveExistsKey, false);
            SessionState.SetString(OriginalSaveHashKey, string.Empty);
        }

        private static bool IsChapterSceneActive()
        {
            Scene scene = SceneManager.GetActiveScene();
            return scene.IsValid() && string.Equals(scene.name, ChapterSceneName, StringComparison.Ordinal);
        }

        private static GameObject RequireSceneObject(string objectName)
        {
            GameObject gameObject = FindSceneObject(objectName);

            if (gameObject == null)
            {
                throw new InvalidOperationException($"Required Chapter01 object '{objectName}' was not found.");
            }

            return gameObject;
        }

        private static TComponent RequireComponent<TComponent>(string objectName) where TComponent : Component
        {
            GameObject gameObject = RequireSceneObject(objectName);
            TComponent component = gameObject.GetComponent<TComponent>();

            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Required component {typeof(TComponent).Name} is missing on '{objectName}'.");
            }

            return component;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];

                if (candidate != null
                    && candidate.gameObject.scene == activeScene
                    && string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static Quaternion ResolveFacingRotation(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > Mathf.Epsilon
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        private static bool IsBatchOrTestRun()
        {
            if (Application.isBatchMode)
            {
                return true;
            }

            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-runTests", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ComputeSha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            byte[] digest = sha256.ComputeHash(stream);
            return BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static TValue GetPrivateField<TValue>(object instance, string fieldName, TValue fallback)
        {
            if (instance == null)
            {
                return fallback;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.GetValue(instance) is TValue value ? value : fallback;
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            if (instance == null)
            {
                return;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(instance, value);
        }

        private static void SetRequiredPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                throw new MissingFieldException(instance.GetType().FullName, fieldName);
            }

            field.SetValue(instance, value);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.00},{value.y:0.00},{value.z:0.00})";
        }

        private readonly struct CameraGauntletCase
        {
            public CameraGauntletCase(
                string id,
                string label,
                Vector3 playerPosition,
                Vector3 targetPosition,
                string instructions)
            {
                Id = id;
                Label = label;
                PlayerPosition = playerPosition;
                TargetPosition = targetPosition;
                Instructions = instructions;
            }

            public string Id { get; }

            public string Label { get; }

            public Vector3 PlayerPosition { get; }

            public Vector3 TargetPosition { get; }

            public string Instructions { get; }
        }

        private sealed class EnemyRuntimeSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly bool activeSelf;
            private readonly EnemyBrain brain;
            private readonly EnemyStateMachine stateMachine;
            private readonly EnemyAttackController attackController;
            private readonly EnemyMotor motor;
            private readonly NavMeshAgent agent;
            private readonly HealthComponent health;
            private readonly bool brainEnabled;
            private readonly bool stateMachineEnabled;
            private readonly bool attackControllerEnabled;
            private readonly bool motorEnabled;
            private readonly bool agentEnabled;
            private readonly float healthValue;
            private readonly Transform brainTarget;
            private readonly float attackCooldownTimer;
            private readonly int attackNextIndex;
            private readonly int attackLastIndex;
            private readonly Vector3 motorFallbackTargetPosition;
            private readonly float motorFallbackMoveSpeed;
            private readonly bool motorWasFallbackMoving;
            private readonly bool agentHadPath;
            private readonly bool agentWasStopped;
            private readonly NavMeshPath agentPath;
            private readonly Vector3 agentVelocity;

            public EnemyRuntimeSnapshot(GameObject gameObject)
            {
                GameObject = gameObject;
                position = gameObject.transform.position;
                rotation = gameObject.transform.rotation;
                activeSelf = gameObject.activeSelf;
                brain = gameObject.GetComponent<EnemyBrain>();
                stateMachine = gameObject.GetComponent<EnemyStateMachine>();
                attackController = gameObject.GetComponent<EnemyAttackController>();
                motor = gameObject.GetComponent<EnemyMotor>();
                agent = gameObject.GetComponent<NavMeshAgent>();
                health = gameObject.GetComponent<HealthComponent>();
                brainEnabled = brain != null && brain.enabled;
                stateMachineEnabled = stateMachine != null && stateMachine.enabled;
                attackControllerEnabled = attackController != null && attackController.enabled;
                motorEnabled = motor != null && motor.enabled;
                agentEnabled = agent != null && agent.enabled;
                healthValue = health != null ? health.CurrentValue : 0f;
                brainTarget = brain != null ? brain.CurrentTarget : null;
                attackCooldownTimer = GetPrivateField(attackController, "cooldownTimer", 0f);
                attackNextIndex = GetPrivateField(attackController, "nextAttackIndex", 0);
                attackLastIndex = GetPrivateField(attackController, "lastAttackIndex", -1);
                motorFallbackTargetPosition = GetPrivateField(motor, "fallbackTargetPosition", Vector3.zero);
                motorFallbackMoveSpeed = GetPrivateField(motor, "fallbackMoveSpeed", 0f);
                motorWasFallbackMoving = GetPrivateField(motor, "isFallbackMoving", false);
                bool agentWasOnNavMesh = agent != null && agent.enabled && agent.isOnNavMesh;
                agentHadPath = agentWasOnNavMesh && agent.hasPath;
                agentWasStopped = agentWasOnNavMesh && agent.isStopped;
                agentPath = agentHadPath ? agent.path : null;
                agentVelocity = agentWasOnNavMesh ? agent.velocity : Vector3.zero;
            }

            public GameObject GameObject { get; }

            public void Restore()
            {
                if (GameObject == null)
                {
                    return;
                }

                GameObject.transform.SetPositionAndRotation(position, rotation);
                health?.SetCurrent(healthValue);
                GameObject.SetActive(activeSelf);

                if (agent != null)
                {
                    agent.enabled = agentEnabled;
                }

                if (motor != null)
                {
                    motor.enabled = motorEnabled;
                    SetRequiredPrivateField(motor, "fallbackTargetPosition", motorFallbackTargetPosition);
                    SetRequiredPrivateField(motor, "fallbackMoveSpeed", motorFallbackMoveSpeed);
                    SetRequiredPrivateField(motor, "isFallbackMoving", motorWasFallbackMoving);
                }

                if (attackController != null)
                {
                    attackController.enabled = attackControllerEnabled;
                    SetRequiredPrivateField(attackController, "cooldownTimer", attackCooldownTimer);
                    SetRequiredPrivateField(attackController, "nextAttackIndex", attackNextIndex);
                    SetRequiredPrivateField(attackController, "lastAttackIndex", attackLastIndex);
                }

                if (stateMachine != null)
                {
                    stateMachine.enabled = stateMachineEnabled;
                }

                if (brain != null)
                {
                    brain.enabled = brainEnabled;

                    if (brainTarget != null)
                    {
                        brain.SetTarget(brainTarget);
                    }
                    else
                    {
                        brain.ClearTarget();
                    }
                }

                if (agent != null && agentEnabled && agent.isOnNavMesh)
                {
                    if (agentHadPath && agentPath != null)
                    {
                        agent.SetPath(agentPath);
                    }
                    else
                    {
                        agent.ResetPath();
                    }

                    agent.isStopped = agentWasStopped;
                    agent.velocity = agentVelocity;
                }
            }
        }
    }

    public sealed class Chapter01CameraObstacleTelemetryOverlay : MonoBehaviour
    {
        private static readonly FieldInfo FollowOffsetField = GetControllerField("followOffset");
        private static readonly FieldInfo FollowLookHeightField = GetControllerField("followLookHeight");
        private static readonly FieldInfo ProbeRadiusField = GetControllerField("obstacleProbeRadius");
        private static readonly FieldInfo PaddingField = GetControllerField("obstaclePadding");
        private static readonly FieldInfo ObstacleMaskField = GetControllerField("obstacleMask");
        private static readonly FieldInfo YawField = GetControllerField("yaw");
        private static readonly FieldInfo PitchField = GetControllerField("pitch");
        private static readonly FieldInfo ObstacleActiveField = GetControllerField("isObstacleAdjustmentActive");
        private static readonly FieldInfo OverheadBlendField = GetControllerField("obstructionOverheadBlend");
        private static readonly FieldInfo OwnerHiddenField = GetControllerField("ownerRenderersHidden");

        private readonly HashSet<string> observedStates = new HashSet<string>(StringComparer.Ordinal);
        private PlayerCharacter player;
        private ThirdPersonCameraController controller;
        private Transform target;
        private UnityEngine.Camera unityCamera;
        private string caseId = string.Empty;
        private string caseLabel = string.Empty;
        private string instructions = string.Empty;
        private int caseNumber;
        private int caseCount;
        private float configuredAt;
        private Vector3 previousCameraPosition;
        private float maxFrameMotionAfterSettle;
        private float minimumRetractionRatio = 1f;
        private int previousSideSign;
        private int sideFlipCount;
        private int ownerVisibilityTransitions;
        private bool previousOwnerHidden;
        private bool sawStaticObstruction;
        private bool sawNarrowSidestep;
        private bool sawLock;
        private bool cameraOccupiedEver;
        private bool targetInViewportThroughout = true;
        private CameraObstacleResolution latestResolution;
        private Vector3 latestDesiredPosition;
        private bool latestCameraOccupied;
        private bool latestTargetInViewport;
        private bool latestOwnerHidden;
        private bool latestObstacleActive;
        private float latestOverheadBlend;
        private GUIStyle labelStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;

        public void Configure(
            PlayerCharacter capturePlayer,
            ThirdPersonCameraController cameraController,
            Transform captureTarget,
            string captureCaseId,
            string captureCaseLabel,
            string captureInstructions,
            int captureCaseNumber,
            int captureCaseCount)
        {
            player = capturePlayer;
            controller = cameraController;
            target = captureTarget;
            unityCamera = controller != null ? controller.GetComponent<UnityEngine.Camera>() : null;
            caseId = captureCaseId ?? string.Empty;
            caseLabel = captureCaseLabel ?? string.Empty;
            instructions = captureInstructions ?? string.Empty;
            caseNumber = captureCaseNumber;
            caseCount = captureCaseCount;
            configuredAt = Time.unscaledTime;
            previousCameraPosition = controller != null ? controller.transform.position : Vector3.zero;
            maxFrameMotionAfterSettle = 0f;
            minimumRetractionRatio = 1f;
            previousSideSign = 0;
            sideFlipCount = 0;
            ownerVisibilityTransitions = 0;
            previousOwnerHidden = false;
            sawStaticObstruction = false;
            sawNarrowSidestep = false;
            sawLock = false;
            cameraOccupiedEver = false;
            targetInViewportThroughout = true;
            latestResolution = default;
            latestDesiredPosition = Vector3.zero;
            latestCameraOccupied = false;
            latestTargetInViewport = false;
            latestOwnerHidden = false;
            latestObstacleActive = false;
            latestOverheadBlend = 0f;
            observedStates.Clear();
        }

        public string BuildMetricLog()
        {
            string states = observedStates.Count > 0 ? string.Join(",", observedStates) : "none";
            return
                $"case={caseId} index={caseNumber}/{caseCount} " +
                $"lockSeen={sawLock} states=[{states}] " +
                $"staticSeen={sawStaticObstruction} sidestepSeen={sawNarrowSidestep} " +
                $"minRetraction={minimumRetractionRatio:0.000} " +
                $"occupiedEver={cameraOccupiedEver} targetInViewportThroughout={targetInViewportThroughout} " +
                $"maxFrameMotionAfterSettle={maxFrameMotionAfterSettle:0.000}m " +
                $"sideFlips={sideFlipCount} ownerVisibilityTransitions={ownerVisibilityTransitions} " +
                $"lastCamera={FormatVector(controller != null ? controller.transform.position : Vector3.zero)} " +
                $"lastDesired={FormatVector(latestDesiredPosition)}.";
        }

        private void LateUpdate()
        {
            if (player == null || controller == null)
            {
                return;
            }

            Vector3 followOffset = GetFieldValue(FollowOffsetField, controller, new Vector3(0f, 1.8f, -4.5f));
            float followLookHeight = GetFieldValue(FollowLookHeightField, controller, 1.5f);
            float probeRadius = GetFieldValue(ProbeRadiusField, controller, 0.25f);
            float padding = GetFieldValue(PaddingField, controller, 0.1f);
            LayerMask obstacleMask = GetFieldValue(ObstacleMaskField, controller, (LayerMask)~0);
            float yaw = GetFieldValue(YawField, controller, 0f);
            float pitch = GetFieldValue(PitchField, controller, 10f);
            Vector3 focusPoint = player.transform.position;
            Vector3 lookPoint = focusPoint + Vector3.up * followLookHeight;
            latestDesiredPosition = ThirdPersonCameraOrbitUtility.ResolveDesiredPosition(
                focusPoint,
                followOffset,
                yaw,
                pitch);
            latestResolution = CameraObstacleResolver.Resolve(
                lookPoint,
                latestDesiredPosition,
                controller.transform.position,
                player.transform,
                probeRadius,
                padding,
                obstacleMask);
            latestCameraOccupied = CameraObstacleResolver.IsPositionOccupied(
                controller.transform.position,
                player.transform,
                probeRadius,
                obstacleMask);
            latestOwnerHidden = GetFieldValue(OwnerHiddenField, controller, false);
            latestObstacleActive = GetFieldValue(ObstacleActiveField, controller, false);
            latestOverheadBlend = GetFieldValue(OverheadBlendField, controller, 0f);
            latestTargetInViewport = ResolveTargetInViewport();

            sawStaticObstruction |= latestResolution.HasStaticObstruction;
            sawNarrowSidestep |= latestResolution.UsedNarrowObstacleSidestep;
            sawLock |= player.LockOnTargetSelector != null && player.LockOnTargetSelector.HasTarget;
            cameraOccupiedEver |= latestCameraOccupied;
            minimumRetractionRatio = Mathf.Min(minimumRetractionRatio, latestResolution.RetractionRatio);

            if (player.StateMachine?.CurrentState != null)
            {
                observedStates.Add(player.StateMachine.CurrentState.GetType().Name);
            }

            bool settled = Time.unscaledTime - configuredAt >= 0.5f;

            if (settled)
            {
                maxFrameMotionAfterSettle = Mathf.Max(
                    maxFrameMotionAfterSettle,
                    Vector3.Distance(previousCameraPosition, controller.transform.position));
                targetInViewportThroughout &= latestTargetInViewport;
                TrackSideFlip(lookPoint);

                if (latestOwnerHidden != previousOwnerHidden)
                {
                    ownerVisibilityTransitions++;
                }
            }

            previousOwnerHidden = latestOwnerHidden;
            previousCameraPosition = controller.transform.position;
        }

        private void TrackSideFlip(Vector3 lookPoint)
        {
            Vector3 boom = latestDesiredPosition - lookPoint;

            if (boom.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 right = Vector3.Cross(Vector3.up, boom.normalized);
            float sideOffset = Vector3.Dot(controller.transform.position - latestDesiredPosition, right);
            int sideSign = Mathf.Abs(sideOffset) < 0.15f ? 0 : sideOffset > 0f ? 1 : -1;

            if (sideSign != 0 && previousSideSign != 0 && sideSign != previousSideSign)
            {
                sideFlipCount++;
            }

            if (sideSign != 0)
            {
                previousSideSign = sideSign;
            }
        }

        private bool ResolveTargetInViewport()
        {
            if (unityCamera == null || target == null)
            {
                return false;
            }

            Vector3 viewport = unityCamera.WorldToViewportPoint(target.position + Vector3.up * 1.25f);
            return viewport.z > 0f
                && viewport.x >= 0f
                && viewport.x <= 1f
                && viewport.y >= 0f
                && viewport.y <= 1f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            float panelWidth = Mathf.Min(620f, Mathf.Max(360f, Screen.width - 16f));
            Rect panelRect = new Rect(Mathf.Max(8f, Screen.width - panelWidth - 8f), 16f, panelWidth, 214f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            float x = panelRect.x + 10f;
            float y = panelRect.y + 8f;
            float width = panelRect.width - 20f;
            DrawLine(x, ref y, width, $"<b>TY_NEW camera obstacle GUI gauntlet</b>  {caseLabel}");
            DrawLine(x, ref y, width, instructions);
            DrawLine(
                x,
                ref y,
                width,
                $"state {ResolvePlayerState()}  lock {(player != null && player.LockOnTargetSelector != null && player.LockOnTargetSelector.HasTarget ? "yes" : "no")}");
            DrawLine(
                x,
                ref y,
                width,
                $"static {latestResolution.HasStaticObstruction}  sidestep {latestResolution.UsedNarrowObstacleSidestep}  ratio {latestResolution.RetractionRatio:0.000}");
            DrawLine(
                x,
                ref y,
                width,
                $"obstacleActive {latestObstacleActive}  overhead {latestOverheadBlend:0.00}  occupied {latestCameraOccupied}");
            DrawLine(
                x,
                ref y,
                width,
                $"camera {FormatVector(controller != null ? controller.transform.position : Vector3.zero)}  desired {FormatVector(latestDesiredPosition)}");
            DrawLine(
                x,
                ref y,
                width,
                $"maxFrame {maxFrameMotionAfterSettle:0.000}m  sideFlips {sideFlipCount}  ownerHidden {latestOwnerHidden}  targetInViewport {latestTargetInViewport}");
            DrawLine(x, ref y, width, "Use only real GUI input. Manual sign-off required; this driver never auto-passes.");
        }

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    richText = true,
                    clipping = TextClipping.Clip,
                    normal = { textColor = Color.white }
                };
            }

            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.025f, 0.90f));
                panelTexture.Apply();
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
            }
        }

        private void DrawLine(float x, ref float y, float width, string text)
        {
            GUI.Label(new Rect(x, y, width, 21f), text, labelStyle);
            y += 24f;
        }

        private string ResolvePlayerState()
        {
            return player?.StateMachine?.CurrentState != null
                ? player.StateMachine.CurrentState.GetType().Name
                : "None";
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Object.DestroyImmediate(panelTexture);
                panelTexture = null;
            }
        }

        private static FieldInfo GetControllerField(string fieldName)
        {
            return typeof(ThirdPersonCameraController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static TValue GetFieldValue<TValue>(FieldInfo field, object instance, TValue fallback)
        {
            return field != null && instance != null && field.GetValue(instance) is TValue value
                ? value
                : fallback;
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.00},{value.y:0.00},{value.z:0.00})";
        }
    }
}
#endif
