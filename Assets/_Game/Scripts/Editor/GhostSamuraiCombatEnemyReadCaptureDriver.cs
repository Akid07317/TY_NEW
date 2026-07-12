#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CampusRPG.EditorTools
{
    [InitializeOnLoad]
    public static class GhostSamuraiCombatEnemyReadCaptureDriverMenu
    {
        private const string PendingKey = "TY_NEW.EnemyReadCapture.Pending";
        private const string HideHudKey = "TY_NEW.EnemyReadCapture.HideHud";
        private const string FocusSceneViewKey = "TY_NEW.EnemyReadCapture.FocusSceneView";
        private const string ScenarioKey = "TY_NEW.EnemyReadCapture.Scenario";
        private const string LastRequestKey = "TY_NEW.EnemyReadCapture.LastRequest";
        private const string RequestPath = "/tmp/TY_NEW_enemy_read_capture_driver.request";
        private const string RangedAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Ranged.asset";
        private const float GuardInputTimeoutSeconds = 2f;
        private const float GuardInputEvidenceTolerance = 0.01f;
        private const string GuardInputAttackId = "Enemy_Melee";

        private static readonly CaptureStep[] DefaultCaptureSteps =
        {
            new CaptureStep(0.60f, CombatProxyVisualKind.EnemyMelee, CaptureCommand.Default, "EnemyMelee / Guard Swing", 1.95f),
            new CaptureStep(4.10f, CombatProxyVisualKind.EnemyMobile, CaptureCommand.Default, "EnemyMobile / Feint Dash", 2.15f),
            new CaptureStep(7.70f, CombatProxyVisualKind.EnemyRanged, CaptureCommand.Default, "EnemyRanged / Arc Bolt", 5.20f)
        };

        private static readonly CaptureStep[] RangedVariantCaptureSteps =
        {
            new CaptureStep(
                0.60f,
                CombatProxyVisualKind.EnemyRanged,
                CaptureCommand.RangedAntiAir,
                "EnemyRanged / Anti-Air Shot",
                4.75f,
                PlayerPrepAction.None,
                true),
            new CaptureStep(
                4.10f,
                CombatProxyVisualKind.EnemyRanged,
                CaptureCommand.RangedChaseRoll,
                "EnemyRanged / Chase Roll Shot",
                4.20f,
                PlayerPrepAction.CombatRoll),
            new CaptureStep(
                7.70f,
                CombatProxyVisualKind.EnemyRanged,
                CaptureCommand.RangedGuardBreak,
                "EnemyRanged / Guard Break Shot",
                3.60f,
                PlayerPrepAction.Block)
        };

        private static readonly CaptureStep[] GuardInputTargetCaptureSteps =
        {
            new CaptureStep(
                0f,
                CombatProxyVisualKind.EnemyMelee,
                CaptureCommand.Default,
                "EnemyMelee / Guard Swing / Guard Input Validation",
                1.50f)
        };

        private static readonly GuardInputCaptureStep[] GuardInputCaptureSteps =
        {
            new GuardInputCaptureStep(0.10f, GuardInputCaptureCommand.PressStartupGuard, "Press <Keyboard>/leftCtrl for startup"),
            new GuardInputCaptureStep(0.10f, GuardInputCaptureCommand.TriggerStartupAttack, "Direct Guard Swing during guard startup"),
            new GuardInputCaptureStep(0.20f, GuardInputCaptureCommand.ReleaseStartupGuard, "Release <Keyboard>/leftCtrl after startup hit"),
            new GuardInputCaptureStep(0.35f, GuardInputCaptureCommand.ResetForActiveGuard, "Reset for active guard"),
            new GuardInputCaptureStep(0.45f, GuardInputCaptureCommand.PressActiveGuard, "Press <Keyboard>/leftCtrl for active guard"),
            new GuardInputCaptureStep(0.45f, GuardInputCaptureCommand.TriggerActiveGuardAttack, "Trigger full Guard Swing after active guard"),
            new GuardInputCaptureStep(0.95f, GuardInputCaptureCommand.ReleaseActiveGuard, "Release <Keyboard>/leftCtrl after successful block"),
            new GuardInputCaptureStep(1.10f, GuardInputCaptureCommand.RecordResult, "Record two-beat guard result")
        };

        private static readonly Dictionary<CombatProxyVisualKind, EnemyBrain> EnemyBrains =
            new Dictionary<CombatProxyVisualKind, EnemyBrain>();
        private static readonly Dictionary<CombatProxyVisualKind, Vector3> InitialPositions =
            new Dictionary<CombatProxyVisualKind, Vector3>();
        private static readonly Dictionary<CombatProxyVisualKind, Quaternion> InitialRotations =
            new Dictionary<CombatProxyVisualKind, Quaternion>();
        private static readonly Dictionary<CombatProxyVisualKind, EnemyArchetypeSO> OriginalArchetypes =
            new Dictionary<CombatProxyVisualKind, EnemyArchetypeSO>();
        private static readonly Dictionary<CaptureCommand, AttackDefinitionSO> RuntimeRangedAttackOverrides =
            new Dictionary<CaptureCommand, AttackDefinitionSO>();

        private static bool driverActive;
        private static bool driverHideHud;
        private static bool driverFocusSceneView;
        private static CaptureScenario driverScenario;
        private static PlayerCharacter player;
        private static PlayerStateMachine playerStateMachine;
        private static LockOnTargetSelector lockOnTargetSelector;
        private static double driverStartTime;
        private static int nextStepIndex;
        private static EnemyArchetypeSO runtimeRangedArchetype;
        private static Keyboard guardInputKeyboard;
        private static EnemyBrain guardInputEnemyBrain;
        private static bool guardInputEnemyWasEnabled;
        private static EnemyAttackController guardInputAttackController;
        private static Transform guardInputExpectedTarget;
        private static EnemyArchetypeSO guardInputExpectedArchetype;
        private static bool guardInputActivePhase;
        private static bool guardInputSawStartupState;
        private static bool guardInputSawActivePhaseStartupState;
        private static bool guardInputSawActiveGuard;
        private static bool guardInputSawBlockStun;
        private static bool guardInputStartupAttackTriggered;
        private static bool guardInputStartupTryAttackSucceeded;
        private static bool guardInputStartupAttackCommitted;
        private static bool guardInputStartupCommitMatched;
        private static bool guardInputStartupCounterWindowAtCommit;
        private static bool guardInputStartupBeatPassed;
        private static bool guardInputActiveAttackTriggered;
        private static bool guardInputActiveAttackCommitted;
        private static bool guardInputActiveCommitMatched;
        private static bool guardInputActiveCounterWindowAtCommit;
        private static bool guardInputActiveBeatPassed;
        private static float guardInputStartupInitialHealth;
        private static float guardInputStartupFinalHealth;
        private static float guardInputStartupInitialCounter;
        private static float guardInputStartupFinalCounter;
        private static float guardInputStartupCommittedDamage;
        private static float guardInputActiveInitialHealth;
        private static float guardInputActiveFinalHealth;
        private static float guardInputActiveInitialCounter;
        private static float guardInputActiveFinalCounter;
        private static float guardInputActiveCommittedDamage;
        private static float guardInputExpectedCounterGain;

        static GhostSamuraiCombatEnemyReadCaptureDriverMenu()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.update -= HandleEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= HandleBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += HandleBeforeAssemblyReload;

            if (IsBatchOrTestRun())
            {
                return;
            }

            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.update += HandleEditorUpdate;
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Debug HUD")]
        public static void StartDebugHudDriver()
        {
            StartDriver(CaptureScenario.Default, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Clean HUD")]
        public static void StartCleanHudDriver()
        {
            StartDriver(CaptureScenario.Default, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Scene View")]
        public static void StartSceneViewDriver()
        {
            StartDriver(CaptureScenario.Default, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Ranged Variants/Debug HUD")]
        public static void StartRangedVariantDebugHudDriver()
        {
            StartDriver(CaptureScenario.RangedVariants, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Ranged Variants/Clean HUD")]
        public static void StartRangedVariantCleanHudDriver()
        {
            StartDriver(CaptureScenario.RangedVariants, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Ranged Variants/Scene View")]
        public static void StartRangedVariantSceneViewDriver()
        {
            StartDriver(CaptureScenario.RangedVariants, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start CombatTest Enemy Read Capture Driver/Guard Input Validation/Debug HUD")]
        public static void StartGuardInputValidationDriver()
        {
            EditorApplication.isPaused = false;
            StartDriver(CaptureScenario.GuardInputValidation, hideHud: false);
        }

        private static void StartDriver(CaptureScenario scenario, bool hideHud, bool focusSceneView = false)
        {
            if (IsBatchOrTestRun())
            {
                return;
            }

            if (scenario == CaptureScenario.GuardInputValidation)
            {
                EditorApplication.isPaused = false;
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
            if (IsBatchOrTestRun() || !File.Exists(RequestPath))
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

            if (string.IsNullOrWhiteSpace(request)
                || request == SessionState.GetString(LastRequestKey, string.Empty))
            {
                return;
            }

            SessionState.SetString(LastRequestKey, request);
            ParseRequest(request, out CaptureScenario scenario, out bool hideHud, out bool focusSceneView);
            StartDriver(scenario, hideHud, focusSceneView);
        }

        private static void HandlePlayModeChanged(PlayModeStateChange change)
        {
            if (!IsBatchOrTestRun()
                && change == PlayModeStateChange.EnteredPlayMode
                && SessionState.GetBool(PendingKey, false))
            {
                AttachDriver();
            }

            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                CleanupDriverState();
            }
        }

        private static void HandleBeforeAssemblyReload()
        {
            CleanupDriverState();
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
            driverScenario = (CaptureScenario)SessionState.GetInt(ScenarioKey, (int)CaptureScenario.Default);
            PrepareSceneForCapture();
        }

        private static void PrepareSceneForCapture()
        {
            CleanupGuardInputResources();
            player = Object.FindObjectOfType<PlayerCharacter>();
            playerStateMachine = player != null ? player.StateMachine : null;
            lockOnTargetSelector = player != null ? player.LockOnTargetSelector : null;
            EnemyBrains.Clear();
            InitialPositions.Clear();
            InitialRotations.Clear();
            OriginalArchetypes.Clear();

            foreach (EnemyBrain candidate in Object.FindObjectsOfType<EnemyBrain>())
            {
                if (candidate == null || !TryResolveKind(candidate, out CombatProxyVisualKind kind))
                {
                    continue;
                }

                if (EnemyBrains.ContainsKey(kind))
                {
                    continue;
                }

                EnemyBrains.Add(kind, candidate);
                InitialPositions.Add(kind, candidate.transform.position);
                InitialRotations.Add(kind, candidate.transform.rotation);
                OriginalArchetypes.Add(kind, candidate.Archetype);
            }

            if (player == null
                || playerStateMachine == null
                || !EnemyBrains.ContainsKey(CombatProxyVisualKind.EnemyMelee)
                || !EnemyBrains.ContainsKey(CombatProxyVisualKind.EnemyMobile)
                || !EnemyBrains.ContainsKey(CombatProxyVisualKind.EnemyRanged))
            {
                Debug.LogWarning(
                    "[TY_NEW EnemyReadDriver] Could not find CombatTest player or melee/mobile/ranged enemies. " +
                    "Open CombatTest with the standard three enemy spawns first.");

                if (driverScenario == CaptureScenario.GuardInputValidation)
                {
                    EditorApplication.isPaused = true;
                }

                CleanupDriverState();
                return;
            }

            EnsureRuntimePreview(CombatProxyVisualKind.EnemyMelee);
            EnsureRuntimePreview(CombatProxyVisualKind.EnemyMobile);
            EnsureRuntimePreview(CombatProxyVisualKind.EnemyRanged);
            ApplyHudVisibility();
            CaptureStep[] steps = ResolveCaptureSteps();
            ResetEnemyReadPose(steps[0]);
            driverStartTime = EditorApplication.timeSinceStartup;
            nextStepIndex = 0;
            if (driverScenario == CaptureScenario.GuardInputValidation)
            {
                if (!PrepareGuardInputScenario())
                {
                    CleanupDriverState();
                    return;
                }
            }

            driverActive = true;
            Debug.Log(
                $"[TY_NEW EnemyReadDriver] Started CombatTest enemy read capture " +
                $"scenario={driverScenario} {(driverHideHud ? "clean-HUD" : "debug-HUD")}.");
        }

        private static void TickDriver()
        {
            if (!driverActive
                || !EditorApplication.isPlaying)
            {
                return;
            }

            if (driverScenario == CaptureScenario.GuardInputValidation)
            {
                TickGuardInputScenario();
                return;
            }

            if (nextStepIndex >= ResolveCaptureSteps().Length)
            {
                return;
            }

            float elapsedSeconds = (float)(EditorApplication.timeSinceStartup - driverStartTime);
            CaptureStep step = ResolveCaptureSteps()[nextStepIndex];

            if (elapsedSeconds < step.TimeSeconds)
            {
                return;
            }

            ResetEnemyReadPose(step);
            TriggerEnemyAttack(step);
            Debug.Log($"[TY_NEW EnemyReadDriver] Triggered {step.Label} at {elapsedSeconds:0.00}s.");
            nextStepIndex++;
        }

        private static bool PrepareGuardInputScenario()
        {
            if (player?.InputReader == null
                || player.Health == null
                || player.Gauges == null
                || player.CombatController == null
                || !EnemyBrains.TryGetValue(CombatProxyVisualKind.EnemyMelee, out EnemyBrain meleeEnemy)
                || meleeEnemy == null
                || meleeEnemy.AttackController == null
                || meleeEnemy.Archetype == null)
            {
                Debug.LogError(
                    "[TY_NEW EnemyGuardInputDriver] FAIL missing player input/health/gauges/combat or EnemyMelee attack wiring.");
                EditorApplication.isPaused = true;
                return false;
            }

            ResetGuardInputTelemetry();
            guardInputEnemyBrain = meleeEnemy;
            guardInputEnemyWasEnabled = meleeEnemy.enabled;
            meleeEnemy.enabled = false;
            guardInputAttackController = meleeEnemy.AttackController;
            guardInputExpectedTarget = player.transform;
            guardInputExpectedArchetype = meleeEnemy.Archetype;
            guardInputAttackController.AttackCommitted -= HandleGuardInputAttackCommitted;
            guardInputAttackController.AttackCommitted += HandleGuardInputAttackCommitted;
            guardInputStartupInitialHealth = player.Health.CurrentValue;
            guardInputStartupInitialCounter = player.Gauges.CounterGauge;
            guardInputExpectedCounterGain = player.CombatController.Balance != null
                ? player.CombatController.Balance.GuardCounterGaugeGain
                : 0f;
            return true;
        }

        private static void TickGuardInputScenario()
        {
            float elapsedSeconds = (float)(EditorApplication.timeSinceStartup - driverStartTime);

            try
            {
                UpdateGuardInputObservations();

                if (elapsedSeconds >= GuardInputTimeoutSeconds)
                {
                    CompleteGuardInputScenario(
                        passed: false,
                        reason:
                            $"timeout at {elapsedSeconds:0.00}s step={nextStepIndex} " +
                            $"startupState={guardInputSawStartupState} startupCommit={guardInputStartupAttackCommitted} " +
                            $"activeGuard={guardInputSawActiveGuard} activeCommit={guardInputActiveAttackCommitted}");
                    return;
                }

                if (nextStepIndex >= GuardInputCaptureSteps.Length)
                {
                    return;
                }

                GuardInputCaptureStep step = GuardInputCaptureSteps[nextStepIndex];

                if (elapsedSeconds < step.MinimumElapsedSeconds || !TryExecuteGuardInputStep(step.Command))
                {
                    return;
                }

                if (!driverActive)
                {
                    nextStepIndex++;
                    return;
                }

                Debug.Log(
                    $"[TY_NEW EnemyGuardInputDriver] Step {nextStepIndex + 1}/{GuardInputCaptureSteps.Length} " +
                    $"{step.Label} at {elapsedSeconds:0.00}s.");
                nextStepIndex++;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                CompleteGuardInputScenario(false, $"exception {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static bool TryExecuteGuardInputStep(GuardInputCaptureCommand command)
        {
            switch (command)
            {
                case GuardInputCaptureCommand.PressStartupGuard:
                    QueueGuardKeyboardState(guardHeld: true);
                    return true;

                case GuardInputCaptureCommand.TriggerStartupAttack:
                    if (!guardInputSawStartupState)
                    {
                        return false;
                    }

                    guardInputStartupAttackTriggered = true;
                    guardInputStartupTryAttackSucceeded = guardInputAttackController != null
                        && EnemyBrains.TryGetValue(CombatProxyVisualKind.EnemyMelee, out EnemyBrain startupEnemy)
                        && startupEnemy != null
                        && guardInputAttackController.TryAttack(player.transform, startupEnemy.Archetype);

                    if (!guardInputStartupTryAttackSucceeded || !guardInputStartupAttackCommitted)
                    {
                        CompleteGuardInputScenario(
                            false,
                            "startup direct Guard Swing was rejected or did not emit AttackCommitted");
                        return false;
                    }

                    return true;

                case GuardInputCaptureCommand.ReleaseStartupGuard:
                    if (!guardInputStartupAttackCommitted)
                    {
                        return false;
                    }

                    if (!HasStartupGuardFailureEvidence())
                    {
                        CompleteGuardInputScenario(false, "startup beat did not prove HP loss with unchanged Counter and no CounterWindow");
                        return false;
                    }

                    guardInputStartupBeatPassed = true;
                    QueueGuardKeyboardState(guardHeld: false);
                    return true;

                case GuardInputCaptureCommand.ResetForActiveGuard:
                    if (!guardInputStartupBeatPassed
                        || player?.InputReader == null
                        || player.InputReader.IsBlockHeld
                        || playerStateMachine == null
                        || playerStateMachine.IsBlocking)
                    {
                        return false;
                    }

                    ResetEnemyReadPose(GuardInputTargetCaptureSteps[0]);
                    guardInputActivePhase = true;
                    guardInputActiveInitialHealth = player.Health.CurrentValue;
                    guardInputActiveInitialCounter = player.Gauges.CounterGauge;
                    return true;

                case GuardInputCaptureCommand.PressActiveGuard:
                    QueueGuardKeyboardState(guardHeld: true);
                    return true;

                case GuardInputCaptureCommand.TriggerActiveGuardAttack:
                    if (!guardInputStartupBeatPassed
                        || !guardInputSawActivePhaseStartupState
                        || !guardInputSawActiveGuard)
                    {
                        return false;
                    }

                    guardInputActiveAttackTriggered = true;

                    if (guardInputEnemyBrain != null)
                    {
                        guardInputEnemyBrain.enabled = true;
                    }

                    TriggerEnemyAttack(GuardInputTargetCaptureSteps[0]);
                    return true;

                case GuardInputCaptureCommand.ReleaseActiveGuard:
                    if (!guardInputActiveAttackCommitted)
                    {
                        return false;
                    }

                    if (!HasActiveGuardBlockEvidence())
                    {
                        CompleteGuardInputScenario(false, "active beat did not prove zero HP loss, Counter gain, and CounterWindow");
                        return false;
                    }

                    guardInputActiveBeatPassed = true;
                    QueueGuardKeyboardState(guardHeld: false);
                    return true;

                case GuardInputCaptureCommand.RecordResult:
                    if (!guardInputStartupBeatPassed
                        || !guardInputActiveBeatPassed
                        || !guardInputActiveAttackCommitted
                        || player?.InputReader == null
                        || player.InputReader.IsBlockHeld
                        || playerStateMachine == null
                        || playerStateMachine.IsBlocking)
                    {
                        return false;
                    }

                    RecordGuardInputResult();
                    return true;

                default:
                    return false;
            }
        }

        private static void UpdateGuardInputObservations()
        {
            if (player?.InputReader == null || playerStateMachine == null)
            {
                return;
            }

            bool isHeld = player.InputReader.IsBlockHeld;
            bool isStartup = isHeld && playerStateMachine.IsBlocking && !playerStateMachine.HasActiveGuard;

            if (guardInputActivePhase)
            {
                guardInputSawActivePhaseStartupState |= isStartup;
                guardInputSawActiveGuard |= isHeld && playerStateMachine.HasActiveGuard;

                if (playerStateMachine.CurrentState is PlayerBlockState blockState && blockState.IsInBlockStun)
                {
                    guardInputSawBlockStun = true;
                }

                return;
            }

            guardInputSawStartupState |= isStartup;
        }

        private static void HandleGuardInputAttackCommitted(EnemyAttackCommit commit)
        {
            if (!IsExpectedGuardInputCommit(
                commit,
                guardInputExpectedTarget,
                guardInputExpectedArchetype))
            {
                Debug.LogWarning(
                    "[TY_NEW EnemyGuardInputDriver] Ignored AttackCommitted that did not match " +
                    "target=Player, archetype=EnemyMelee, attack=Enemy_Melee, damage>0.");
                return;
            }

            float currentHealth = player?.Health != null ? player.Health.CurrentValue : float.NaN;
            float currentCounter = player?.Gauges != null ? player.Gauges.CounterGauge : float.NaN;
            bool hasCounterWindow = player?.CombatController != null && player.CombatController.HasCounterWindow;

            if (guardInputActivePhase)
            {
                if (!guardInputActiveAttackTriggered || guardInputActiveAttackCommitted)
                {
                    Debug.LogWarning(
                        "[TY_NEW EnemyGuardInputDriver] Ignored out-of-order or duplicate active-beat AttackCommitted.");
                    return;
                }

                guardInputActiveAttackCommitted = true;
                guardInputActiveCommitMatched = true;
                guardInputActiveCommittedDamage = commit.Damage;
                guardInputActiveFinalHealth = currentHealth;
                guardInputActiveFinalCounter = currentCounter;
                guardInputActiveCounterWindowAtCommit = hasCounterWindow;

                if (playerStateMachine?.CurrentState is PlayerBlockState blockState && blockState.IsInBlockStun)
                {
                    guardInputSawBlockStun = true;
                }

                return;
            }

            if (!guardInputStartupAttackTriggered || guardInputStartupAttackCommitted)
            {
                Debug.LogWarning(
                    "[TY_NEW EnemyGuardInputDriver] Ignored out-of-order or duplicate startup-beat AttackCommitted.");
                return;
            }

            guardInputStartupAttackCommitted = true;
            guardInputStartupCommitMatched = true;
            guardInputStartupCommittedDamage = commit.Damage;
            guardInputStartupFinalHealth = currentHealth;
            guardInputStartupFinalCounter = currentCounter;
            guardInputStartupCounterWindowAtCommit = hasCounterWindow;
        }

        private static void RecordGuardInputResult()
        {
            bool startupPassed = guardInputStartupBeatPassed && HasStartupGuardFailureEvidence();
            bool activePassed = guardInputActiveBeatPassed && HasActiveGuardBlockEvidence();
            bool inputReleased = player?.InputReader != null
                && !player.InputReader.IsBlockHeld
                && playerStateMachine != null
                && !playerStateMachine.IsBlocking;
            bool passed = startupPassed && activePassed && inputReleased;
            string result = passed ? "PASS" : "FAIL";
            string message =
                $"[TY_NEW EnemyGuardInputDriver] {result} two-beat ordinary Guard input " +
                $"input=<Keyboard>/leftCtrl " +
                $"startup=(state={guardInputSawStartupState} try={guardInputStartupTryAttackSucceeded} " +
                $"commit={guardInputStartupAttackCommitted}/{guardInputStartupCommitMatched} " +
                $"hp={guardInputStartupInitialHealth:0.##}->{guardInputStartupFinalHealth:0.##} " +
                $"damage={guardInputStartupCommittedDamage:0.##} counter={guardInputStartupInitialCounter:0.##}->{guardInputStartupFinalCounter:0.##} " +
                $"counterWindow={guardInputStartupCounterWindowAtCommit}) " +
                $"active=(startup={guardInputSawActivePhaseStartupState} guard={guardInputSawActiveGuard} " +
                $"trigger={guardInputActiveAttackTriggered} commit={guardInputActiveAttackCommitted}/{guardInputActiveCommitMatched} " +
                $"hp={guardInputActiveInitialHealth:0.##}->{guardInputActiveFinalHealth:0.##} " +
                $"damage={guardInputActiveCommittedDamage:0.##} counter={guardInputActiveInitialCounter:0.##}->{guardInputActiveFinalCounter:0.##} " +
                $"expectedGain={guardInputExpectedCounterGain:0.##} counterWindow={guardInputActiveCounterWindowAtCommit} " +
                $"blockStun={guardInputSawBlockStun}) released={inputReleased}.";

            if (passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            CompleteGuardInputScenario(passed, "criteria recorded", resultAlreadyLogged: true);
        }

        private static bool HasStartupGuardFailureEvidence()
        {
            return guardInputSawStartupState
                && guardInputStartupAttackTriggered
                && guardInputStartupTryAttackSucceeded
                && guardInputStartupAttackCommitted
                && guardInputStartupCommitMatched
                && MatchesStartupGuardFailureVitals(
                    guardInputStartupInitialHealth,
                    guardInputStartupFinalHealth,
                    guardInputStartupCommittedDamage,
                    guardInputStartupInitialCounter,
                    guardInputStartupFinalCounter,
                    guardInputStartupCounterWindowAtCommit);
        }

        private static bool HasActiveGuardBlockEvidence()
        {
            return guardInputSawActivePhaseStartupState
                && guardInputSawActiveGuard
                && guardInputActiveAttackTriggered
                && guardInputActiveAttackCommitted
                && guardInputActiveCommitMatched
                && guardInputActiveCommittedDamage > 0f
                && MatchesActiveGuardBlockVitals(
                    guardInputActiveInitialHealth,
                    guardInputActiveFinalHealth,
                    guardInputActiveInitialCounter,
                    guardInputActiveFinalCounter,
                    guardInputExpectedCounterGain,
                    guardInputActiveCounterWindowAtCommit);
        }

        private static bool MatchesStartupGuardFailureVitals(
            float initialHealth,
            float finalHealth,
            float committedDamage,
            float initialCounter,
            float finalCounter,
            bool hasCounterWindow)
        {
            bool healthDecreased = finalHealth < initialHealth - GuardInputEvidenceTolerance;
            bool damageMatched = committedDamage > 0f
                && Mathf.Abs(
                    finalHealth - Mathf.Max(0f, initialHealth - committedDamage)) <= GuardInputEvidenceTolerance;
            bool counterUnchanged = Mathf.Abs(finalCounter - initialCounter) <= GuardInputEvidenceTolerance;
            return healthDecreased && damageMatched && counterUnchanged && !hasCounterWindow;
        }

        private static bool MatchesActiveGuardBlockVitals(
            float initialHealth,
            float finalHealth,
            float initialCounter,
            float finalCounter,
            float expectedCounterGain,
            bool hasCounterWindow)
        {
            bool healthUnchanged = Mathf.Abs(finalHealth - initialHealth) <= GuardInputEvidenceTolerance;
            bool counterMatched = expectedCounterGain > 0f
                && Mathf.Abs(
                    finalCounter - (initialCounter + expectedCounterGain)) <= GuardInputEvidenceTolerance;
            return healthUnchanged && counterMatched && hasCounterWindow;
        }

        private static bool IsExpectedGuardInputCommit(
            EnemyAttackCommit commit,
            Transform expectedTarget,
            EnemyArchetypeSO expectedArchetype)
        {
            return expectedTarget != null
                && expectedArchetype != null
                && commit.Target == expectedTarget
                && commit.Archetype == expectedArchetype
                && commit.Attack != null
                && string.Equals(
                    commit.Attack.AttackId,
                    GuardInputAttackId,
                    System.StringComparison.Ordinal)
                && commit.Damage > 0f;
        }

        private static void QueueGuardKeyboardState(bool guardHeld)
        {
            if (guardInputKeyboard == null || !guardInputKeyboard.added)
            {
                guardInputKeyboard = InputSystem.AddDevice<Keyboard>("TY_NEW_EnemyReadCaptureKeyboard");
            }

            KeyboardState state = guardHeld
                ? new KeyboardState(Key.LeftCtrl)
                : new KeyboardState();
            InputSystem.QueueStateEvent(guardInputKeyboard, state);
        }

        private static void CompleteGuardInputScenario(
            bool passed,
            string reason,
            bool resultAlreadyLogged = false)
        {
            if (!resultAlreadyLogged)
            {
                string message = $"[TY_NEW EnemyGuardInputDriver] {(passed ? "PASS" : "FAIL")} {reason}.";

                if (passed)
                {
                    Debug.Log(message);
                }
                else
                {
                    Debug.LogError(message);
                }
            }

            CleanupGuardInputResources();

            if (!passed && playerStateMachine != null)
            {
                playerStateMachine.SwitchToLocomotion();
            }

            driverActive = false;
            EditorApplication.isPaused = true;
        }

        private static void CleanupGuardInputResources()
        {
            if (guardInputAttackController != null)
            {
                guardInputAttackController.AttackCommitted -= HandleGuardInputAttackCommitted;
                guardInputAttackController = null;
            }

            guardInputExpectedTarget = null;
            guardInputExpectedArchetype = null;

            if (guardInputEnemyBrain != null)
            {
                try
                {
                    guardInputEnemyBrain.enabled = guardInputEnemyWasEnabled;
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning(
                        $"[TY_NEW EnemyGuardInputDriver] EnemyBrain restoration warning: {exception.Message}");
                }
                finally
                {
                    guardInputEnemyBrain = null;
                }
            }

            guardInputEnemyWasEnabled = false;

            if (guardInputKeyboard != null)
            {
                try
                {
                    if (guardInputKeyboard.added)
                    {
                        InputSystem.QueueStateEvent(guardInputKeyboard, new KeyboardState());
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning(
                        $"[TY_NEW EnemyGuardInputDriver] Virtual keyboard release warning: {exception.Message}");
                }

                try
                {
                    if (guardInputKeyboard.added)
                    {
                        InputSystem.RemoveDevice(guardInputKeyboard);
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning(
                        $"[TY_NEW EnemyGuardInputDriver] Virtual keyboard removal warning: {exception.Message}");
                }
                finally
                {
                    guardInputKeyboard = null;
                }
            }
        }

        private static void ResetGuardInputTelemetry()
        {
            guardInputActivePhase = false;
            guardInputSawStartupState = false;
            guardInputSawActivePhaseStartupState = false;
            guardInputSawActiveGuard = false;
            guardInputSawBlockStun = false;
            guardInputStartupAttackTriggered = false;
            guardInputStartupTryAttackSucceeded = false;
            guardInputStartupAttackCommitted = false;
            guardInputStartupCommitMatched = false;
            guardInputStartupCounterWindowAtCommit = false;
            guardInputStartupBeatPassed = false;
            guardInputActiveAttackTriggered = false;
            guardInputActiveAttackCommitted = false;
            guardInputActiveCommitMatched = false;
            guardInputActiveCounterWindowAtCommit = false;
            guardInputActiveBeatPassed = false;
            guardInputStartupInitialHealth = 0f;
            guardInputStartupFinalHealth = 0f;
            guardInputStartupInitialCounter = 0f;
            guardInputStartupFinalCounter = 0f;
            guardInputStartupCommittedDamage = 0f;
            guardInputActiveInitialHealth = 0f;
            guardInputActiveFinalHealth = 0f;
            guardInputActiveInitialCounter = 0f;
            guardInputActiveFinalCounter = 0f;
            guardInputActiveCommittedDamage = 0f;
            guardInputExpectedCounterGain = 0f;
        }

        private static bool TryResolveKind(EnemyBrain brain, out CombatProxyVisualKind kind)
        {
            kind = CombatProxyVisualKind.EnemyMelee;

            if (brain == null || brain.Archetype == null)
            {
                return false;
            }

            switch (brain.Archetype.ArchetypeType)
            {
                case EnemyArchetypeType.Mobile:
                    kind = CombatProxyVisualKind.EnemyMobile;
                    return true;
                case EnemyArchetypeType.Ranged:
                    kind = CombatProxyVisualKind.EnemyRanged;
                    return true;
                case EnemyArchetypeType.Melee:
                    kind = CombatProxyVisualKind.EnemyMelee;
                    return true;
                default:
                    return false;
            }
        }

        private static void EnsureRuntimePreview(CombatProxyVisualKind kind)
        {
            if (!EnemyBrains.TryGetValue(kind, out EnemyBrain brain) || brain == null)
            {
                return;
            }

            RuntimeAnimatorController controller =
                CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(kind);

            if (controller == null)
            {
                Debug.LogWarning($"[TY_NEW EnemyReadDriver] Imported enemy preview controller is not available for {kind}.");
                return;
            }

            Animator rootAnimator = brain.GetComponent<Animator>();
            CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(brain.gameObject, kind, rootAnimator);

            Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(brain.gameObject);

            if (importedAnimator == null)
            {
                Debug.LogWarning($"[TY_NEW EnemyReadDriver] Imported preview animator was not created for {kind}.");
                return;
            }

            importedAnimator.enabled = true;
            importedAnimator.runtimeAnimatorController = controller;
            importedAnimator.applyRootMotion = false;
            importedAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            importedAnimator.updateMode = AnimatorUpdateMode.Normal;
            importedAnimator.Rebind();

            EnemyVisualPresentationRelay presentationRelay = brain.GetComponent<EnemyVisualPresentationRelay>();

            if (presentationRelay != null)
            {
                presentationRelay.enabled = false;
            }

            EnemyCombatAnimationRelay importedRelay = brain.GetComponent<EnemyCombatAnimationRelay>();

            if (importedRelay == null)
            {
                importedRelay = brain.gameObject.AddComponent<EnemyCombatAnimationRelay>();
            }

            importedRelay.enabled = true;
        }

        private static void ResetEnemyReadPose(CaptureStep step)
        {
            if (player == null || playerStateMachine == null || !EnemyBrains.TryGetValue(step.Kind, out EnemyBrain activeEnemy))
            {
                return;
            }

            foreach (KeyValuePair<CombatProxyVisualKind, EnemyBrain> pair in EnemyBrains)
            {
                EnemyBrain enemy = pair.Value;

                if (enemy == null)
                {
                    continue;
                }

                enemy.transform.SetPositionAndRotation(
                    InitialPositions[pair.Key],
                    InitialRotations[pair.Key]);
                enemy.Health?.RestoreFull();
                enemy.Motor?.Stop();
                enemy.ClearTarget();
                enemy.StateMachine?.SwitchToIdle();
                enemy.AttackController?.ResetRuntimeState();

                NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

                if (agent != null && agent.enabled)
                {
                    agent.ResetPath();
                }

                if (pair.Key != step.Kind)
                {
                    if (agent != null && agent.enabled)
                    {
                        agent.enabled = false;
                    }

                    if (enemy.AttackController != null)
                    {
                        enemy.AttackController.enabled = false;
                    }
                }
                else
                {
                    if (agent != null && !agent.enabled)
                    {
                        agent.enabled = true;
                    }

                    if (enemy.AttackController != null)
                    {
                        enemy.AttackController.enabled = true;
                    }
                }
            }

            Vector3 enemyForward = InitialRotations[step.Kind] * Vector3.forward;
            enemyForward.y = 0f;

            if (enemyForward.sqrMagnitude <= Mathf.Epsilon)
            {
                enemyForward = Vector3.forward;
            }

            enemyForward.Normalize();
            float minimumPlayerDistance = driverScenario == CaptureScenario.GuardInputValidation ? 1.5f : 1.8f;
            Vector3 playerPosition = InitialPositions[step.Kind]
                + enemyForward * Mathf.Max(minimumPlayerDistance, step.PlayerDistance);

            if (step.StartAirborne)
            {
                playerPosition += Vector3.up * 1.15f;
            }

            Quaternion playerRotation = Quaternion.LookRotation(-enemyForward, Vector3.up);

            player.Motor?.WarpTo(playerPosition, playerRotation);
            player.Motor?.ResetMotion();

            if (step.StartAirborne)
            {
                player.Motor?.ApplyActionVerticalVelocity(0.75f, onlyIfHigher: false);
            }

            player.Health?.RestoreFull();
            player.Mana?.RestoreFull();
            player.Gauges?.ResetAll();
            player.CombatController?.ResetRuntimeState();
            lockOnTargetSelector?.ResetRuntimeState();
            playerStateMachine.SwitchToLocomotion();
            ApplyPlayerPrepAction(step);

            Physics.SyncTransforms();
            ApplyEnemyLockTarget(activeEnemy);
            FocusSceneViewForCapture(activeEnemy);
        }

        private static void ApplyPlayerPrepAction(CaptureStep step)
        {
            if (playerStateMachine == null)
            {
                return;
            }

            switch (step.PrepAction)
            {
                case PlayerPrepAction.CombatRoll:
                    playerStateMachine.SwitchToDodge(PlayerEvasiveActionType.CombatRoll);
                    break;
                case PlayerPrepAction.Block:
                    playerStateMachine.SwitchToBlock();
                    break;
            }
        }

        private static void ApplyEnemyLockTarget(EnemyBrain activeEnemy)
        {
            if (player == null || activeEnemy == null)
            {
                return;
            }

            activeEnemy.SetTarget(player.transform);

            if (lockOnTargetSelector == null)
            {
                return;
            }

            MethodInfo method = typeof(LockOnTargetSelector).GetMethod(
                "SetCurrentTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method != null)
            {
                method.Invoke(lockOnTargetSelector, new object[] { activeEnemy.transform });
                return;
            }

            lockOnTargetSelector.AcquireTarget();
        }

        private static void ApplyHudVisibility()
        {
            FieldInfo field = typeof(CampusRPG.UI.CombatDebugHUD).GetField(
                "showDebugPanel",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                return;
            }

            foreach (CampusRPG.UI.CombatDebugHUD hud in Object.FindObjectsOfType<CampusRPG.UI.CombatDebugHUD>(includeInactive: true))
            {
                field.SetValue(hud, !driverHideHud);
            }
        }

        private static void FocusSceneViewForCapture(EnemyBrain activeEnemy)
        {
            if (!driverFocusSceneView || SceneView.lastActiveSceneView == null || player == null || activeEnemy == null)
            {
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            Vector3 target = Vector3.Lerp(player.transform.position, activeEnemy.transform.position, 0.5f);
            sceneView.pivot = target + Vector3.up * 0.9f;
            sceneView.rotation = Quaternion.Euler(18f, 4f, 0f);
            sceneView.size = 5.8f;
            sceneView.Repaint();
        }

        private static void TriggerEnemyAttack(CaptureStep step)
        {
            if (!EnemyBrains.TryGetValue(step.Kind, out EnemyBrain activeEnemy)
                || activeEnemy == null
                || activeEnemy.StateMachine == null)
            {
                return;
            }

            ApplyRuntimeAttackOverride(activeEnemy, step);
            activeEnemy.AttackController?.ResetRuntimeState();
            activeEnemy.SetTarget(player != null ? player.transform : null);
            activeEnemy.StateMachine.SwitchToAttack();
        }

        private static void ApplyRuntimeAttackOverride(EnemyBrain activeEnemy, CaptureStep step)
        {
            if (activeEnemy == null)
            {
                return;
            }

            if (step.Kind != CombatProxyVisualKind.EnemyRanged || step.Command == CaptureCommand.Default)
            {
                RestoreOriginalArchetype(step.Kind);
                return;
            }

            if (!OriginalArchetypes.TryGetValue(CombatProxyVisualKind.EnemyRanged, out EnemyArchetypeSO originalArchetype)
                || originalArchetype == null)
            {
                return;
            }

            AttackDefinitionSO attack = ResolveRuntimeRangedAttack(step.Command);

            if (attack == null)
            {
                RestoreOriginalArchetype(step.Kind);
                return;
            }

            if (runtimeRangedArchetype == null)
            {
                runtimeRangedArchetype = Object.Instantiate(originalArchetype);
                runtimeRangedArchetype.name = originalArchetype.name + "_TY_NEW_RuntimePreview";
                runtimeRangedArchetype.hideFlags = HideFlags.DontSave;
            }

            SetPrivateField(runtimeRangedArchetype, "attacks", new[] { attack });
            SetPrivateField(activeEnemy, "archetype", runtimeRangedArchetype);
            activeEnemy.Motor?.SetMoveSpeed(runtimeRangedArchetype.MoveSpeed);
        }

        private static AttackDefinitionSO ResolveRuntimeRangedAttack(CaptureCommand command)
        {
            if (command == CaptureCommand.Default)
            {
                return null;
            }

            if (RuntimeRangedAttackOverrides.TryGetValue(command, out AttackDefinitionSO cachedAttack) && cachedAttack != null)
            {
                return cachedAttack;
            }

            AttackDefinitionSO baseAttack = AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(RangedAttackPath);

            if (baseAttack == null)
            {
                Debug.LogWarning("[TY_NEW EnemyReadDriver] Missing base ranged attack asset.");
                return null;
            }

            AttackDefinitionSO runtimeAttack = Object.Instantiate(baseAttack);
            runtimeAttack.hideFlags = HideFlags.DontSave;

            switch (command)
            {
                case CaptureCommand.RangedAntiAir:
                    runtimeAttack.name = "SO_Attack_Enemy_Ranged_AntiAir_TY_NEW_RuntimePreview";
                    SetPrivateField(runtimeAttack, "attackId", "Enemy_Ranged_AntiAir_Preview");
                    SetPrivateField(runtimeAttack, "displayName", "Sky Thread");
                    SetPrivateField(runtimeAttack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);
                    SetPrivateField(runtimeAttack, "breaksGuard", false);
                    break;
                case CaptureCommand.RangedChaseRoll:
                    runtimeAttack.name = "SO_Attack_Enemy_Ranged_ChaseRoll_TY_NEW_RuntimePreview";
                    SetPrivateField(runtimeAttack, "attackId", "Enemy_Ranged_ChaseRoll_Preview");
                    SetPrivateField(runtimeAttack, "displayName", "Slip Shot");
                    SetPrivateField(runtimeAttack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);
                    SetPrivateField(runtimeAttack, "breaksGuard", false);
                    break;
                case CaptureCommand.RangedGuardBreak:
                    runtimeAttack.name = "SO_Attack_Enemy_Ranged_GuardBreak_TY_NEW_RuntimePreview";
                    SetPrivateField(runtimeAttack, "attackId", "Enemy_Ranged_GuardBreak_Preview");
                    SetPrivateField(runtimeAttack, "displayName", "Sunder Draw");
                    SetPrivateField(runtimeAttack, "enemyTargetResponse", EnemyTargetResponseType.GuardBreak);
                    SetPrivateField(runtimeAttack, "breaksGuard", true);
                    break;
            }

            RuntimeRangedAttackOverrides[command] = runtimeAttack;
            return runtimeAttack;
        }

        private static void RestoreOriginalArchetype(CombatProxyVisualKind kind)
        {
            if (!EnemyBrains.TryGetValue(kind, out EnemyBrain brain)
                || brain == null
                || !OriginalArchetypes.TryGetValue(kind, out EnemyArchetypeSO originalArchetype)
                || originalArchetype == null)
            {
                return;
            }

            SetPrivateField(brain, "archetype", originalArchetype);
            brain.Motor?.SetMoveSpeed(originalArchetype.MoveSpeed);
        }

        private static CaptureStep[] ResolveCaptureSteps()
        {
            switch (driverScenario)
            {
                case CaptureScenario.RangedVariants:
                    return RangedVariantCaptureSteps;
                case CaptureScenario.GuardInputValidation:
                    return GuardInputTargetCaptureSteps;
                default:
                    return DefaultCaptureSteps;
            }
        }

        private static void ParseRequest(string request, out CaptureScenario scenario, out bool hideHud, out bool focusSceneView)
        {
            string normalized = request.Trim().ToLowerInvariant();
            if (normalized.Contains("guard-input") || normalized.Contains("guardinput"))
            {
                scenario = CaptureScenario.GuardInputValidation;
            }
            else
            {
                scenario = normalized.Contains("ranged")
                    ? CaptureScenario.RangedVariants
                    : CaptureScenario.Default;
            }

            hideHud = normalized.Contains("clean");
            focusSceneView = normalized.Contains("scene");
        }

        private static void CleanupDriverState()
        {
            CleanupGuardInputResources();

            foreach (CombatProxyVisualKind kind in OriginalArchetypes.Keys)
            {
                RestoreOriginalArchetype(kind);
            }

            foreach (AttackDefinitionSO runtimeAttack in RuntimeRangedAttackOverrides.Values)
            {
                if (runtimeAttack != null)
                {
                    Object.DestroyImmediate(runtimeAttack);
                }
            }

            RuntimeRangedAttackOverrides.Clear();

            if (runtimeRangedArchetype != null)
            {
                Object.DestroyImmediate(runtimeRangedArchetype);
                runtimeRangedArchetype = null;
            }

            driverActive = false;
            nextStepIndex = 0;
            EnemyBrains.Clear();
            InitialPositions.Clear();
            InitialRotations.Clear();
            OriginalArchetypes.Clear();
            ResetGuardInputTelemetry();
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            if (instance == null)
            {
                return;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }

        private readonly struct CaptureStep
        {
            public CaptureStep(
                float timeSeconds,
                CombatProxyVisualKind kind,
                CaptureCommand command,
                string label,
                float playerDistance,
                PlayerPrepAction prepAction = PlayerPrepAction.None,
                bool startAirborne = false)
            {
                TimeSeconds = timeSeconds;
                Kind = kind;
                Command = command;
                Label = label;
                PlayerDistance = playerDistance;
                PrepAction = prepAction;
                StartAirborne = startAirborne;
            }

            public float TimeSeconds { get; }

            public CombatProxyVisualKind Kind { get; }

            public CaptureCommand Command { get; }

            public string Label { get; }

            public float PlayerDistance { get; }

            public PlayerPrepAction PrepAction { get; }

            public bool StartAirborne { get; }
        }

        private readonly struct GuardInputCaptureStep
        {
            public GuardInputCaptureStep(
                float minimumElapsedSeconds,
                GuardInputCaptureCommand command,
                string label)
            {
                MinimumElapsedSeconds = minimumElapsedSeconds;
                Command = command;
                Label = label;
            }

            public float MinimumElapsedSeconds { get; }

            public GuardInputCaptureCommand Command { get; }

            public string Label { get; }
        }

        private enum CaptureScenario
        {
            Default = 0,
            RangedVariants = 1,
            GuardInputValidation = 2
        }

        private enum CaptureCommand
        {
            Default = 0,
            RangedAntiAir = 1,
            RangedChaseRoll = 2,
            RangedGuardBreak = 3
        }

        private enum GuardInputCaptureCommand
        {
            PressStartupGuard = 0,
            TriggerStartupAttack = 1,
            ReleaseStartupGuard = 2,
            ResetForActiveGuard = 3,
            PressActiveGuard = 4,
            TriggerActiveGuardAttack = 5,
            ReleaseActiveGuard = 6,
            RecordResult = 7
        }

        private enum PlayerPrepAction
        {
            None = 0,
            CombatRoll = 1,
            Block = 2
        }
    }
}
#endif
