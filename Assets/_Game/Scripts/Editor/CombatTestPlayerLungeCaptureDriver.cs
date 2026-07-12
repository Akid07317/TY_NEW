#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.EditorTools
{
    [InitializeOnLoad]
    public static class CombatTestPlayerLungeCaptureDriverMenu
    {
        private const string PendingKey = "TY_NEW.CaptureDriver.Pending";
        private const string HideHudKey = "TY_NEW.CaptureDriver.HideHud";
        private const string FocusSceneViewKey = "TY_NEW.CaptureDriver.FocusSceneView";
        private const string ScenarioKey = "TY_NEW.CaptureDriver.Scenario";
        private const string LastRequestKey = "TY_NEW.CaptureDriver.LastRequest";
        private const string RequestPath = "/tmp/TY_NEW_lunge_capture_driver.request";
        private const float AirborneCaptureHeightOffset = 1.15f;
        private const float AirborneCaptureVerticalVelocity = 0.75f;

        private static readonly CaptureStep[] LungeCaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.LightAttack, "Light_01 whiff / no move", 4.20f, false, true, false),
            new CaptureStep(2.35f, CaptureCommand.LightAttack, "Light_01 hit / no move", 1.80f, false, true, false),
            new CaptureStep(4.10f, CaptureCommand.LightAttack, "Light combo whiff 1/3", 4.20f, false, true, false),
            new CaptureStep(4.82f, CaptureCommand.LightAttack, "Light combo whiff 2/3", 4.20f, false, false, false),
            new CaptureStep(5.65f, CaptureCommand.LightAttack, "Light combo whiff 3/3", 4.20f, false, false, false),
            new CaptureStep(7.55f, CaptureCommand.LightAttack, "Light combo hit 1/3", 2.05f, false, true, false),
            new CaptureStep(8.28f, CaptureCommand.LightAttack, "Light combo hit 2/3", 2.05f, false, false, false),
            new CaptureStep(9.12f, CaptureCommand.LightAttack, "Light combo hit 3/3", 2.05f, false, false, false),
            new CaptureStep(11.20f, CaptureCommand.HeavyAttack, "Heavy_01 whiff", 4.80f, false, true, false),
            new CaptureStep(13.45f, CaptureCommand.HeavyAttack, "Heavy_01 hit", 2.15f, false, true, false),
            new CaptureStep(15.70f, CaptureCommand.LightAttack, "Locked Light_01 hit", 2.05f, true, true, false),
            new CaptureStep(17.95f, CaptureCommand.LightAttack, "Non-locked Light_01 hit", 2.05f, false, true, false),
            new CaptureStep(20.20f, CaptureCommand.HeavyAttack, "Edge-distance Heavy_01", 2.75f, false, true, false),
            new CaptureStep(22.70f, CaptureCommand.LightAttack, "Wall-blocked Light_01", 2.05f, false, true, true)
        };

        private static readonly CaptureStep[] SwordArtCaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.AirDodgeOnly, "AirDodge only / spacing reset", 4.20f, false, true, false, true),
            new CaptureStep(3.10f, CaptureCommand.AirDodgeLightFollowUp, "Moon Sever hit / AirDodge + Light", 1.95f, false, true, false, true),
            new CaptureStep(6.30f, CaptureCommand.AirDodgeLightFollowUp, "Moon Sever whiff / AirDodge + Light", 4.40f, false, true, false, true),
            new CaptureStep(9.70f, CaptureCommand.AirDodgeHeavyFollowUp, "Falling Star hit / AirDodge + Heavy", 2.05f, false, true, false, true),
            new CaptureStep(13.40f, CaptureCommand.AirDodgeHeavyFollowUp, "Falling Star whiff / AirDodge + Heavy", 4.80f, false, true, false, true)
        };

        private static readonly CaptureStep[] AirHeavySwordArtCaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.AirborneForwardHeavy, "Rising Cleave hit / Airborne + Forward Heavy", 2.15f, false, true, false, true),
            new CaptureStep(4.10f, CaptureCommand.AirborneNeutralHeavy, "Falling Star hit / Airborne + Neutral Heavy", 1.95f, false, true, false, true),
            new CaptureStep(7.70f, CaptureCommand.AirDodgeForwardHeavyFollowUp, "Rising Cleave hit / AirDodge + Forward Heavy", 2.15f, false, true, false, true),
            new CaptureStep(11.40f, CaptureCommand.AirDodgeHeavyFollowUp, "Falling Star hit / AirDodge + Heavy", 2.05f, false, true, false, true),
            new CaptureStep(15.20f, CaptureCommand.AirDodgeHeavyFollowUp, "Falling Star whiff / AirDodge + Heavy", 4.80f, false, true, false, true)
        };

        private static readonly CaptureStep[] FlankSwordArtCaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.GroundDodgeOnly, "GroundDodge only / spacing reset", 4.20f, false, true, false),
            new CaptureStep(3.10f, CaptureCommand.DodgeLeftLightFollowUp, "Sidewind Cut hit / Dodge Left + Light", 1.95f, false, true, false),
            new CaptureStep(6.45f, CaptureCommand.DodgeRightLightFollowUp, "Sidewind Cut whiff / Dodge Right + Light", 4.40f, false, true, false),
            new CaptureStep(9.95f, CaptureCommand.CombatRollLightFollowUp, "Cross Step hit / Roll + Light", 2.05f, false, true, false),
            new CaptureStep(13.85f, CaptureCommand.CombatRollLightFollowUp, "Cross Step whiff / Roll + Light", 4.80f, false, true, false)
        };

        private static readonly CaptureStep[] IronGateBreakCaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.BlockIntoIronGateBreak, "Iron Gate Break hit / AfterBlock + Heavy", 1.95f, true, true, false),
            new CaptureStep(4.20f, CaptureCommand.HeavyAttack, "Heavy_01 hit / queue Iron Gate Break", 2.05f, true, true, false),
            new CaptureStep(4.98f, CaptureCommand.HeavyIntoIronGateBreak, "Iron Gate Break hit / AfterHeavy + Heavy", 2.05f, true, false, false),
            new CaptureStep(8.40f, CaptureCommand.HeavyAttack, "Heavy_01 whiff / queue Iron Gate Break", 4.65f, false, true, false),
            new CaptureStep(9.18f, CaptureCommand.HeavyIntoIronGateBreak, "Iron Gate Break whiff / AfterHeavy + Heavy", 4.65f, false, false, false)
        };

        private static bool driverActive;
        private static bool driverHideHud;
        private static bool driverFocusSceneView;
        private static CaptureScenario driverScenario;
        private static PlayerCharacter player;
        private static PlayerStateMachine stateMachine;
        private static double driverStartTime;
        private static int nextStepIndex;
        private static GameObject temporaryWall;
        private static bool hasPendingAirborneStep;
        private static CaptureStep pendingAirborneStep;
        private static double pendingAirborneStepDeadline;

        static CombatTestPlayerLungeCaptureDriverMenu()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.update -= HandleEditorUpdate;

            if (IsBatchOrTestRun())
            {
                return;
            }

            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.update += HandleEditorUpdate;
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player Lunge Capture Driver/Debug HUD")]
        public static void StartDebugHudDriver()
        {
            StartDriver(CaptureScenario.Lunge, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player Lunge Capture Driver/Clean HUD")]
        public static void StartCleanHudDriver()
        {
            StartDriver(CaptureScenario.Lunge, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player Lunge Capture Driver/Scene View")]
        public static void StartSceneViewDriver()
        {
            StartDriver(CaptureScenario.Lunge, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Debug HUD")]
        public static void StartSwordArtDebugHudDriver()
        {
            StartDriver(CaptureScenario.SwordArt, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Clean HUD")]
        public static void StartSwordArtCleanHudDriver()
        {
            StartDriver(CaptureScenario.SwordArt, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Scene View")]
        public static void StartSwordArtSceneViewDriver()
        {
            StartDriver(CaptureScenario.SwordArt, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Air Heavy Reads/Debug HUD")]
        public static void StartSwordArtAirHeavyDebugHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtAirHeavy, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Air Heavy Reads/Clean HUD")]
        public static void StartSwordArtAirHeavyCleanHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtAirHeavy, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Air Heavy Reads/Scene View")]
        public static void StartSwordArtAirHeavySceneViewDriver()
        {
            StartDriver(CaptureScenario.SwordArtAirHeavy, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Flank Reads/Debug HUD")]
        public static void StartSwordArtFlankDebugHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtFlank, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Flank Reads/Clean HUD")]
        public static void StartSwordArtFlankCleanHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtFlank, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Flank Reads/Scene View")]
        public static void StartSwordArtFlankSceneViewDriver()
        {
            StartDriver(CaptureScenario.SwordArtFlank, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Iron Gate Break/Debug HUD")]
        public static void StartSwordArtIronGateBreakDebugHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtIronGateBreak, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Iron Gate Break/Clean HUD")]
        public static void StartSwordArtIronGateBreakCleanHudDriver()
        {
            StartDriver(CaptureScenario.SwordArtIronGateBreak, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Player SwordArt Capture Driver/Iron Gate Break/Scene View")]
        public static void StartSwordArtIronGateBreakSceneViewDriver()
        {
            StartDriver(CaptureScenario.SwordArtIronGateBreak, hideHud: false, focusSceneView: true);
        }

        private static void StartDriver(CaptureScenario scenario, bool hideHud, bool focusSceneView = false)
        {
            if (IsBatchOrTestRun())
            {
                return;
            }

            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(HideHudKey, hideHud);
            SessionState.SetBool(FocusSceneViewKey, focusSceneView);
            SessionState.SetInt(ScenarioKey, (int)scenario);

            if (EditorApplication.isPlaying)
            {
                AttachDriver();
                return;
            }

            EditorApplication.isPlaying = true;
        }

        private static void HandleEditorUpdate()
        {
            PollRequestFile();
            TickDriver();
        }

        private static void PollRequestFile()
        {
            if (IsBatchOrTestRun())
            {
                return;
            }

            if (!File.Exists(RequestPath))
            {
                return;
            }

            string request;

            try
            {
                request = File.ReadAllText(RequestPath).Trim();
            }
            catch (IOException)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(request) || request == SessionState.GetString(LastRequestKey, string.Empty))
            {
                return;
            }

            SessionState.SetString(LastRequestKey, request);
            ParseRequest(request, out CaptureScenario scenario, out bool hideHud, out bool focusSceneView);
            StartDriver(scenario, hideHud, focusSceneView);
        }

        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (!IsBatchOrTestRun() && change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                AttachDriver();
            }
        }

        private static bool IsBatchOrTestRun()
        {
            if (Application.isBatchMode)
            {
                return true;
            }

            string[] args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-runTests", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AttachDriver()
        {
            SessionState.SetBool(PendingKey, false);
            driverHideHud = SessionState.GetBool(HideHudKey, false);
            driverFocusSceneView = SessionState.GetBool(FocusSceneViewKey, false);
            driverScenario = (CaptureScenario)SessionState.GetInt(ScenarioKey, (int)CaptureScenario.Lunge);
            PrepareSceneForCapture();
        }

        private static void TickDriver()
        {
            CaptureStep[] captureSteps = ResolveCaptureSteps();

            if (!driverActive || !EditorApplication.isPlaying || stateMachine == null || nextStepIndex >= captureSteps.Length)
            {
                return;
            }

            float elapsedSeconds = (float)(EditorApplication.timeSinceStartup - driverStartTime);

            if (hasPendingAirborneStep)
            {
                if (player?.Motor != null
                    && player.Motor.IsGrounded
                    && EditorApplication.timeSinceStartup < pendingAirborneStepDeadline)
                {
                    return;
                }

                ExecuteCaptureStep(pendingAirborneStep);
                Debug.Log(
                    $"[TY_NEW CaptureDriver] Triggered {pendingAirborneStep.Label} " +
                    $"({pendingAirborneStep.Command}) at {elapsedSeconds:0.00}s.");
                hasPendingAirborneStep = false;
                nextStepIndex++;
                return;
            }

            CaptureStep step = captureSteps[nextStepIndex];

            if (elapsedSeconds < step.TimeSeconds)
            {
                return;
            }

            if (step.ResetPose)
            {
                ResetPlayerPoseForCapture(step.EnemyDistance, step.LockTarget, step.UseTemporaryWall, step.StartAirborne);
            }
            else
            {
                PositionPrimaryEnemy(step.EnemyDistance);
                ApplyLockTarget(step.LockTarget);
            }

            LungeCaptureTelemetryOverlay.SetCaseLabel(step.Label);

            if (step.StartAirborne && player?.Motor != null && player.Motor.IsGrounded)
            {
                hasPendingAirborneStep = true;
                pendingAirborneStep = step;
                pendingAirborneStepDeadline = EditorApplication.timeSinceStartup + 0.25d;
                return;
            }

            ExecuteCaptureStep(step);
            Debug.Log($"[TY_NEW CaptureDriver] Triggered {step.Label} ({step.Command}) at {elapsedSeconds:0.00}s.");
            nextStepIndex++;
        }

        private static void PrepareSceneForCapture()
        {
            player = Object.FindObjectOfType<PlayerCharacter>();
            stateMachine = player != null ? player.StateMachine : null;

            if (player == null || stateMachine == null)
            {
                Debug.LogWarning("[TY_NEW CaptureDriver] Could not find PlayerCharacter/PlayerStateMachine.");
                driverActive = false;
                return;
            }

            DisableEnemyAI();
            CaptureStep[] captureSteps = ResolveCaptureSteps();
            ResetPlayerPoseForCapture(
                captureSteps[0].EnemyDistance,
                captureSteps[0].LockTarget,
                captureSteps[0].UseTemporaryWall,
                captureSteps[0].StartAirborne);
            EnsureTelemetryOverlay();
            ApplyHudVisibility();
            FocusSceneViewForCapture();
            driverStartTime = EditorApplication.timeSinceStartup;
            nextStepIndex = 0;
            hasPendingAirborneStep = false;
            driverActive = true;
            Debug.Log(
                $"[TY_NEW CaptureDriver] Started {driverScenario} {(driverHideHud ? "clean-HUD" : "debug-HUD")} capture.");
        }

        private static void DisableEnemyAI()
        {
            foreach (EnemyBrain brain in Object.FindObjectsOfType<EnemyBrain>())
            {
                brain.enabled = false;
            }

            foreach (EnemyStateMachine enemyStateMachine in Object.FindObjectsOfType<EnemyStateMachine>())
            {
                enemyStateMachine.enabled = false;
            }

            foreach (EnemyAttackController attackController in Object.FindObjectsOfType<EnemyAttackController>())
            {
                attackController.enabled = false;
            }

            foreach (NavMeshAgent agent in Object.FindObjectsOfType<NavMeshAgent>())
            {
                if (agent.enabled)
                {
                    agent.ResetPath();
                    agent.enabled = false;
                }
            }
        }

        private static void ResetPlayerPoseForCapture(float enemyDistance, bool lockTarget, bool useTemporaryWall, bool startAirborne = false)
        {
            Transform playerSpawn = GameObject.Find("PlayerSpawn")?.transform;
            Vector3 playerPosition = playerSpawn != null ? playerSpawn.position : player.transform.position;
            Quaternion playerRotation = playerSpawn != null ? playerSpawn.rotation : Quaternion.identity;

            if (startAirborne)
            {
                playerPosition += Vector3.up * AirborneCaptureHeightOffset;
            }

            ConfigureTemporaryWall(false, playerPosition, playerRotation);
            player.Motor?.WarpTo(playerPosition, playerRotation);
            player.Motor?.ResetMotion();
            if (startAirborne)
            {
                player.Motor?.ApplyActionVerticalVelocity(AirborneCaptureVerticalVelocity, onlyIfHigher: false);
                CharacterController characterController = player.GetComponent<CharacterController>();

                if (characterController != null && characterController.enabled)
                {
                    // WarpTo can leave CharacterController.isGrounded cached until the next Move.
                    // Refresh it now so an AirDodge capture does not fall back to a direct attack.
                    characterController.Move(Vector3.up * 0.001f);
                }
            }

            player.Health?.RestoreFull();
            player.Mana?.RestoreFull();
            player.Gauges?.ResetAll();
            player.CombatController?.ResetRuntimeState();
            player.LockOnTargetSelector?.ResetRuntimeState();
            stateMachine.SwitchToLocomotion();
            Physics.SyncTransforms();

            PositionPrimaryEnemy(enemyDistance);
            ConfigureTemporaryWall(useTemporaryWall, playerPosition, playerRotation);
            ApplyLockTarget(lockTarget);
        }

        private static void PositionPrimaryEnemy(float enemyDistance)
        {
            GameObject enemy = GameObject.Find("Enemy_Melee_A");
            if (enemy == null || player == null)
            {
                return;
            }

            Vector3 playerPosition = player.transform.position;
            Transform enemySpawn = GameObject.Find("EnemySpawn_Melee")?.transform;
            playerPosition.y = enemySpawn != null ? enemySpawn.position.y : enemy.transform.position.y;
            Vector3 forward = player.transform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            enemy.transform.SetPositionAndRotation(
                playerPosition + forward * Mathf.Max(0.25f, enemyDistance),
                Quaternion.LookRotation(-forward, Vector3.up));

            HealthComponent health = enemy.GetComponentInChildren<HealthComponent>();
            health?.RestoreFull();
        }

        private static void ApplyLockTarget(bool enabled)
        {
            LockOnTargetSelector selector = player != null ? player.LockOnTargetSelector : null;

            if (selector == null)
            {
                return;
            }

            selector.ClearTarget();

            if (enabled)
            {
                selector.AcquireTarget();
            }
        }

        private static void ConfigureTemporaryWall(bool enabled, Vector3 playerPosition, Quaternion playerRotation)
        {
            if (!enabled)
            {
                if (temporaryWall != null)
                {
                    Object.DestroyImmediate(temporaryWall);
                    temporaryWall = null;
                }

                return;
            }

            if (temporaryWall == null)
            {
                temporaryWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                temporaryWall.name = "TY_NEW_Capture_TemporaryWall";
            }

            Vector3 forward = playerRotation * Vector3.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            temporaryWall.transform.SetPositionAndRotation(
                playerPosition + forward * 0.95f + Vector3.up,
                Quaternion.LookRotation(forward, Vector3.up));
            temporaryWall.transform.localScale = new Vector3(2.2f, 2f, 0.25f);
        }

        private static void EnsureTelemetryOverlay()
        {
            if (player == null)
            {
                return;
            }

            LungeCaptureTelemetryOverlay overlay = player.GetComponent<LungeCaptureTelemetryOverlay>();

            if (overlay == null)
            {
                overlay = player.gameObject.AddComponent<LungeCaptureTelemetryOverlay>();
            }

            overlay.Configure(player);
            LungeCaptureTelemetryOverlay.SetCaseLabel("arming capture driver");
        }

        private static void ApplyHudVisibility()
        {
            FieldInfo field = typeof(CombatDebugHUD).GetField("showDebugPanel", BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                return;
            }

            foreach (CombatDebugHUD hud in Object.FindObjectsOfType<CombatDebugHUD>(includeInactive: true))
            {
                field.SetValue(hud, !driverHideHud);
            }
        }

        private static void FocusSceneViewForCapture()
        {
            if (!driverFocusSceneView || SceneView.lastActiveSceneView == null || player == null)
            {
                return;
            }

            Vector3 target = player.transform.position;
            GameObject enemy = GameObject.Find("Enemy_Melee_A");
            if (enemy != null)
            {
                target = Vector3.Lerp(target, enemy.transform.position, 0.45f);
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            sceneView.pivot = target + Vector3.up * 0.65f;
            sceneView.rotation = Quaternion.Euler(24f, 28f, 0f);
            sceneView.size = 3.4f;
            sceneView.Repaint();
        }

        private static void ParseRequest(
            string request,
            out CaptureScenario scenario,
            out bool hideHud,
            out bool focusSceneView)
        {
            string normalized = request.Trim().ToLowerInvariant();
            if (normalized.Contains("irongate"))
            {
                scenario = CaptureScenario.SwordArtIronGateBreak;
            }
            else if (normalized.Contains("airheavy"))
            {
                scenario = CaptureScenario.SwordArtAirHeavy;
            }
            else if (normalized.Contains("flank"))
            {
                scenario = CaptureScenario.SwordArtFlank;
            }
            else if (normalized.StartsWith("swordart"))
            {
                scenario = CaptureScenario.SwordArt;
            }
            else
            {
                scenario = CaptureScenario.Lunge;
            }

            hideHud = normalized.Contains("clean");
            focusSceneView = normalized.Contains("scene");
        }

        private static CaptureStep[] ResolveCaptureSteps()
        {
            return driverScenario switch
            {
                CaptureScenario.SwordArt => SwordArtCaptureSteps,
                CaptureScenario.SwordArtAirHeavy => AirHeavySwordArtCaptureSteps,
                CaptureScenario.SwordArtFlank => FlankSwordArtCaptureSteps,
                CaptureScenario.SwordArtIronGateBreak => IronGateBreakCaptureSteps,
                _ => LungeCaptureSteps
            };
        }

        private static void ExecuteCaptureStep(CaptureStep step)
        {
            switch (step.Command)
            {
                case CaptureCommand.LightAttack:
                    stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
                    break;
                case CaptureCommand.HeavyAttack:
                    stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
                    break;
                case CaptureCommand.GroundDodgeOnly:
                    stateMachine.SwitchToDodge();
                    break;
                case CaptureCommand.AirDodgeOnly:
                    stateMachine.SwitchToAirDodge();
                    if (stateMachine.CurrentState is not PlayerDodgeState dodgeState
                        || dodgeState.ActionType != PlayerEvasiveActionType.AirDodge)
                    {
                        Debug.LogWarning("[TY_NEW CaptureDriver] AirDodge did not start; player may still be grounded.");
                    }

                    break;
                case CaptureCommand.DodgeLeftLightFollowUp:
                    TriggerGroundDodgeSwordArtFollowUp(SwordArtInputDirection.Left);
                    break;
                case CaptureCommand.DodgeRightLightFollowUp:
                    TriggerGroundDodgeSwordArtFollowUp(SwordArtInputDirection.Right);
                    break;
                case CaptureCommand.CombatRollLightFollowUp:
                    TriggerCombatRollSwordArtFollowUp();
                    break;
                case CaptureCommand.AirDodgeLightFollowUp:
                    TriggerAirDodgeSwordArtFollowUp(PlayerAttackRequest.Light, SwordArtInputDirection.Neutral);
                    break;
                case CaptureCommand.AirDodgeHeavyFollowUp:
                    TriggerAirDodgeSwordArtFollowUp(PlayerAttackRequest.Heavy, SwordArtInputDirection.Neutral);
                    break;
                case CaptureCommand.AirborneForwardHeavy:
                    TriggerAirborneHeavySwordArt(SwordArtInputDirection.Forward);
                    break;
                case CaptureCommand.AirborneNeutralHeavy:
                    TriggerAirborneHeavySwordArt(SwordArtInputDirection.Neutral);
                    break;
                case CaptureCommand.AirDodgeForwardHeavyFollowUp:
                    TriggerAirDodgeSwordArtFollowUp(PlayerAttackRequest.Heavy, SwordArtInputDirection.Forward);
                    break;
                case CaptureCommand.BlockIntoIronGateBreak:
                    TriggerIronGateBreakFromBlock();
                    break;
                case CaptureCommand.HeavyIntoIronGateBreak:
                    TriggerIronGateBreakFromHeavyChain();
                    break;
            }
        }

        private static void TriggerAirborneHeavySwordArt(SwordArtInputDirection direction)
        {
            if (player?.CombatController == null || stateMachine == null)
            {
                return;
            }

            player.CombatController.BufferSwordArtCommand(
                SwordArtTriggerAction.HeavyAttack,
                direction,
                SwordArtContextTags.Airborne);
            stateMachine.SwitchToAttack(PlayerAttackRequest.Heavy);
        }

        private static void TriggerAirDodgeSwordArtFollowUp(PlayerAttackRequest request, SwordArtInputDirection direction)
        {
            if (player?.CombatController == null)
            {
                return;
            }

            SwordArtTriggerAction triggerAction = request == PlayerAttackRequest.Heavy
                ? SwordArtTriggerAction.HeavyAttack
                : SwordArtTriggerAction.LightAttack;
            player.CombatController.BufferSwordArtCommand(
                triggerAction,
                direction,
                SwordArtContextTags.Airborne | SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterAirDodge);
            stateMachine.SwitchToAirDodge();

            if (stateMachine.CurrentState is PlayerDodgeState dodgeState
                && dodgeState.ActionType == PlayerEvasiveActionType.AirDodge)
            {
                if (request == PlayerAttackRequest.Heavy)
                {
                    stateMachine.CurrentState.HandleHeavyAttack();
                }
                else
                {
                    stateMachine.CurrentState.HandleLightAttack();
                }

                return;
            }

            stateMachine.SwitchToAttack(request);
            Debug.LogWarning(
                "[TY_NEW CaptureDriver] AirDodge did not start cleanly; fell back to direct SwordArt attack context.");
        }

        private static void TriggerGroundDodgeSwordArtFollowUp(SwordArtInputDirection direction)
        {
            if (player?.CombatController == null || stateMachine == null)
            {
                return;
            }

            player.CombatController.TryRecordSwordArtPreviewCommand(
                SwordArtTriggerAction.LightAttack,
                direction,
                SwordArtContextTags.AfterDodge);
            stateMachine.SwitchToDodge();
            float dodgeWindowSeconds = player.CombatController.Balance != null
                ? player.CombatController.Balance.DodgeFollowUpWindowSeconds
                : 0.35f;
            player.CombatController.OpenDodgeFollowUpWindow(dodgeWindowSeconds);
            ForceGroundDodgeDirection(direction);

            if (stateMachine.CurrentState is PlayerDodgeState dodgeState
                && dodgeState.ActionType == PlayerEvasiveActionType.GroundDodge)
            {
                dodgeState.HandleLightAttack();
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
            Debug.LogWarning(
                "[TY_NEW CaptureDriver] Ground dodge did not start cleanly; fell back to direct Sidewind attack context.");
        }

        private static void TriggerCombatRollSwordArtFollowUp()
        {
            if (player?.CombatController == null || stateMachine == null)
            {
                return;
            }

            player.CombatController.TryRecordSwordArtPreviewCommand(
                SwordArtTriggerAction.LightAttack,
                SwordArtInputDirection.Neutral,
                SwordArtContextTags.AfterDodge | SwordArtContextTags.AfterCombatRoll);
            stateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);

            if (stateMachine.CurrentState is PlayerDodgeState dodgeState
                && dodgeState.ActionType == PlayerEvasiveActionType.CombatRoll)
            {
                dodgeState.HandleLightAttack();
                return;
            }

            stateMachine.SwitchToAttack(PlayerAttackRequest.Light);
            Debug.LogWarning(
                "[TY_NEW CaptureDriver] Combat roll did not start cleanly; fell back to direct Cross Step attack context.");
        }

        private static void ForceGroundDodgeDirection(SwordArtInputDirection direction)
        {
            if (player?.Motor == null || player.CombatController == null)
            {
                return;
            }

            CombatBalanceSO balance = player.CombatController.Balance;
            float dodgeDistance = balance != null ? balance.DodgeDistance : 2.8f;
            float dodgeDurationSeconds = balance != null ? balance.DodgeDurationSeconds : 0.25f;
            Vector3 localDirection = direction == SwordArtInputDirection.Left ? -player.transform.right : player.transform.right;
            localDirection.y = 0f;

            if (localDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            player.Motor.BeginDirectionalDodge(localDirection.normalized, dodgeDistance, dodgeDurationSeconds, keepFacingLockOnTarget: false);
        }

        private static void TriggerIronGateBreakFromBlock()
        {
            if (player?.CombatController == null || stateMachine == null)
            {
                return;
            }

            player.CombatController.NotifySuccessfulBlock();
            player.CombatController.BufferSwordArtCommand(
                SwordArtTriggerAction.HeavyAttack,
                SwordArtInputDirection.Neutral,
                SwordArtContextTags.AfterBlock);
            stateMachine.SwitchToBlock();
            stateMachine.CurrentState?.HandleHeavyAttack();
        }

        private static void TriggerIronGateBreakFromHeavyChain()
        {
            if (player?.CombatController == null || stateMachine == null)
            {
                return;
            }

            player.CombatController.BufferSwordArtCommand(
                SwordArtTriggerAction.HeavyAttack,
                SwordArtInputDirection.Neutral,
                SwordArtContextTags.AfterHeavy);
            stateMachine.CurrentState?.HandleHeavyAttack();
        }

        private readonly struct CaptureStep
        {
            public CaptureStep(
                float timeSeconds,
                CaptureCommand command,
                string label,
                float enemyDistance,
                bool lockTarget,
                bool resetPose,
                bool useTemporaryWall,
                bool startAirborne = false)
            {
                TimeSeconds = timeSeconds;
                Command = command;
                Label = label;
                EnemyDistance = enemyDistance;
                LockTarget = lockTarget;
                ResetPose = resetPose;
                UseTemporaryWall = useTemporaryWall;
                StartAirborne = startAirborne;
            }

            public float TimeSeconds { get; }

            public CaptureCommand Command { get; }

            public string Label { get; }

            public float EnemyDistance { get; }

            public bool LockTarget { get; }

            public bool ResetPose { get; }

            public bool UseTemporaryWall { get; }

            public bool StartAirborne { get; }
        }

        private enum CaptureScenario
        {
            Lunge = 0,
            SwordArt = 1,
            SwordArtAirHeavy = 2,
            SwordArtFlank = 3,
            SwordArtIronGateBreak = 4
        }

        private enum CaptureCommand
        {
            LightAttack = 0,
            HeavyAttack = 1,
            GroundDodgeOnly = 2,
            AirDodgeOnly = 3,
            DodgeLeftLightFollowUp = 4,
            DodgeRightLightFollowUp = 5,
            CombatRollLightFollowUp = 6,
            AirDodgeLightFollowUp = 7,
            AirDodgeHeavyFollowUp = 8,
            AirborneForwardHeavy = 9,
            AirborneNeutralHeavy = 10,
            AirDodgeForwardHeavyFollowUp = 11,
            BlockIntoIronGateBreak = 12,
            HeavyIntoIronGateBreak = 13
        }
    }

    public sealed class LungeCaptureTelemetryOverlay : MonoBehaviour
    {
        private const string CsvPath = "/tmp/TY_NEW_lunge_capture_telemetry.csv";
        private static readonly FieldInfo HitboxWindowField = typeof(HitboxController).GetField("activationWindowOpen", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo HitboxExecutedField = typeof(HitboxController).GetField("hasExecutedCurrentAttack", BindingFlags.Instance | BindingFlags.NonPublic);
        private static string currentCaseLabel = "idle";

        private PlayerCharacter player;
        private PlayerCombatController combatController;
        private GUIStyle labelStyle;
        private GUIStyle panelStyle;
        private AttackDefinitionSO previousAttack;
        private float previousElapsedSeconds;
        private float cumulativePlannedLunge;
        private float cumulativeActualPlanarDistance;
        private Vector3 previousPosition;
        private int hitboxOpenedFrame = -1;
        private int hitExecutedFrame = -1;
        private int lastLoggedFrame = -1;

        public static void SetCaseLabel(string label)
        {
            currentCaseLabel = string.IsNullOrWhiteSpace(label) ? "unlabeled" : label.Trim();
        }

        public void Configure(PlayerCharacter capturePlayer)
        {
            player = capturePlayer;
            combatController = player != null ? player.CombatController : null;
            previousAttack = null;
            previousElapsedSeconds = 0f;
            cumulativePlannedLunge = 0f;
            cumulativeActualPlanarDistance = 0f;
            previousPosition = player != null ? player.transform.position : Vector3.zero;
            hitboxOpenedFrame = -1;
            hitExecutedFrame = -1;
            lastLoggedFrame = -1;
            ResetCsv();
        }

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<PlayerCharacter>();
                combatController = player != null ? player.CombatController : null;
                previousPosition = transform.position;
            }
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            combatController = player.CombatController;
            AttackDefinitionSO attack = combatController != null ? combatController.CurrentAttackDefinition : null;
            float elapsedSeconds = combatController != null ? combatController.CurrentAttackElapsedSeconds : 0f;

            if (attack != previousAttack || elapsedSeconds < previousElapsedSeconds)
            {
                previousAttack = attack;
                previousElapsedSeconds = 0f;
                cumulativePlannedLunge = 0f;
                cumulativeActualPlanarDistance = 0f;
                hitboxOpenedFrame = -1;
                hitExecutedFrame = -1;
                previousPosition = player.transform.position;
            }

            bool hitboxOpen = ResolveHitboxBool(HitboxWindowField);
            bool hitExecuted = ResolveHitboxBool(HitboxExecutedField);

            if (hitboxOpen && hitboxOpenedFrame < 0)
            {
                hitboxOpenedFrame = Time.frameCount;
            }

            if (hitExecuted && hitExecutedFrame < 0)
            {
                hitExecutedFrame = Time.frameCount;
            }

            float plannedDelta = ResolvePlannedLungeDelta(attack, previousElapsedSeconds, elapsedSeconds);
            cumulativePlannedLunge += plannedDelta;
            cumulativeActualPlanarDistance += ResolveActualPlanarDelta();
            previousElapsedSeconds = elapsedSeconds;
            previousPosition = player.transform.position;
            WriteCsvLine(attack, elapsedSeconds, plannedDelta, hitboxOpen, hitExecuted);
        }

        private void OnGUI()
        {
            EnsureStyles();

            IReadOnlyList<string> lines = BuildLines();
            Rect panelRect = new Rect(Mathf.Max(8f, Screen.width - 560f), 16f, 544f, 178f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            float y = panelRect.y + 8f;
            for (int i = 0; i < lines.Count; i++)
            {
                GUI.Label(new Rect(panelRect.x + 10f, y, panelRect.width - 20f, 18f), lines[i], labelStyle);
                y += 18f;
            }
        }

        private IReadOnlyList<string> BuildLines()
        {
            List<string> lines = new List<string>(9);
            AttackDefinitionSO attack = combatController != null ? combatController.CurrentAttackDefinition : null;
            float elapsedSeconds = combatController != null ? combatController.CurrentAttackElapsedSeconds : 0f;
            float durationSeconds = combatController != null ? combatController.CurrentAttackDurationSeconds : 0f;
            bool hitboxOpen = ResolveHitboxBool(HitboxWindowField);
            bool hitExecuted = ResolveHitboxBool(HitboxExecutedField);
            string attackId = attack != null ? attack.AttackId : "None";
            string phase = attack != null ? ResolvePhase(attack, elapsedSeconds, durationSeconds) : "Idle";
            float plannedDelta = attack != null
                ? PlayerCombatRuntimeUtility.ResolveAttackForwardMovementDelta(attack, previousElapsedSeconds, elapsedSeconds)
                : 0f;

            lines.Add($"<b>TY_NEW lunge evidence</b>  case: {currentCaseLabel}");
            lines.Add($"frame {Time.frameCount}  atk {attackId}  {phase} {elapsedSeconds:0.000}/{durationSeconds:0.000}s");
            lines.Add(attack != null
                ? $"hit window {attack.StartupSeconds:0.000}-{attack.StartupSeconds + attack.ActiveSeconds:0.000}s  mode {attack.HitboxActivationMode}"
                : "hit window --");
            lines.Add($"lunge/frame planned {plannedDelta:0.000}m  cumulative {cumulativePlannedLunge:0.000}m / target {(attack != null ? attack.ForwardMovement : 0f):0.000}m");
            lines.Add($"actual planar cumulative {cumulativeActualPlanarDistance:0.000}m  pos {FormatVector(player != null ? player.transform.position : Vector3.zero)}");
            lines.Add($"hitbox open {hitboxOpen}  executed {hitExecuted}  openFrame {FormatFrame(hitboxOpenedFrame)}  hitFrame {FormatFrame(hitExecutedFrame)}");
            lines.Add($"lock target {(player != null && player.LockOnTargetSelector != null && player.LockOnTargetSelector.HasTarget ? "yes" : "no")}  grounded {(player != null && player.Motor != null && player.Motor.IsGrounded ? "yes" : "no")}");
            return lines;
        }

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }

            if (panelStyle == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.025f, 0.88f));
                texture.Apply();
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = texture;
            }
        }

        private float ResolveActualPlanarDelta()
        {
            if (player == null)
            {
                return 0f;
            }

            Vector3 delta = player.transform.position - previousPosition;
            delta.y = 0f;
            return delta.magnitude;
        }

        private bool ResolveHitboxBool(FieldInfo field)
        {
            HitboxController hitbox = combatController != null ? combatController.HitboxController : null;

            if (hitbox == null || field == null)
            {
                return false;
            }

            object value = field.GetValue(hitbox);
            return value is bool boolean && boolean;
        }

        private static float ResolvePlannedLungeDelta(AttackDefinitionSO attack, float previousElapsedSeconds, float elapsedSeconds)
        {
            return attack != null
                ? PlayerCombatRuntimeUtility.ResolveAttackForwardMovementDelta(attack, previousElapsedSeconds, elapsedSeconds)
                : 0f;
        }

        private static string ResolvePhase(AttackDefinitionSO attack, float elapsedSeconds, float durationSeconds)
        {
            float hitStartSeconds = attack.StartupSeconds;
            float hitEndSeconds = hitStartSeconds + attack.ActiveSeconds;

            if (elapsedSeconds < hitStartSeconds)
            {
                return "Startup";
            }

            if (elapsedSeconds < hitEndSeconds)
            {
                return "Active";
            }

            return elapsedSeconds < durationSeconds ? "Recovery" : "Done";
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.00},{value.y:0.00},{value.z:0.00}";
        }

        private static string FormatFrame(int frame)
        {
            return frame >= 0 ? frame.ToString() : "--";
        }

        private void ResetCsv()
        {
            try
            {
                File.WriteAllText(
                    CsvPath,
                    "frame,case,attack,phase,elapsed,total,planned_delta,planned_cumulative,actual_cumulative,position_x,position_y,position_z,hitbox_open,hit_executed,hitbox_open_frame,hit_executed_frame\n");
            }
            catch (IOException)
            {
            }
        }

        private void WriteCsvLine(
            AttackDefinitionSO attack,
            float elapsedSeconds,
            float plannedDelta,
            bool hitboxOpen,
            bool hitExecuted)
        {
            if (Time.frameCount == lastLoggedFrame)
            {
                return;
            }

            lastLoggedFrame = Time.frameCount;
            string attackId = attack != null ? attack.AttackId : "None";
            float duration = combatController != null ? combatController.CurrentAttackDurationSeconds : 0f;
            string phase = attack != null ? ResolvePhase(attack, elapsedSeconds, duration) : "Idle";
            Vector3 position = player != null ? player.transform.position : Vector3.zero;

            try
            {
                File.AppendAllText(
                    CsvPath,
                    $"{Time.frameCount},\"{currentCaseLabel}\",{attackId},{phase},{elapsedSeconds:0.0000},{duration:0.0000},{plannedDelta:0.0000},{cumulativePlannedLunge:0.0000},{cumulativeActualPlanarDistance:0.0000},{position.x:0.0000},{position.y:0.0000},{position.z:0.0000},{hitboxOpen},{hitExecuted},{FormatFrame(hitboxOpenedFrame)},{FormatFrame(hitExecutedFrame)}\n");
            }
            catch (IOException)
            {
            }
        }
    }
}
#endif
