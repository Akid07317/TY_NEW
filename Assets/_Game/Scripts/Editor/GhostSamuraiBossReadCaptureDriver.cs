#if UNITY_EDITOR
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Editor;
using CampusRPG.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CampusRPG.EditorTools
{
    [InitializeOnLoad]
    public static class GhostSamuraiBossReadCaptureDriverMenu
    {
        private const string PendingKey = "TY_NEW.BossReadCapture.Pending";
        private const string HideHudKey = "TY_NEW.BossReadCapture.HideHud";
        private const string FocusSceneViewKey = "TY_NEW.BossReadCapture.FocusSceneView";
        private const string ScenarioKey = "TY_NEW.BossReadCapture.Scenario";
        private const string LastRequestKey = "TY_NEW.BossReadCapture.LastRequest";
        private const string RequestPath = "/tmp/TY_NEW_boss_read_capture_driver.request";
        private const string BossObjectName = "Boss_Gatekeeper";
        private const string CaptureKeyboardName = "TY_NEW_BossCaptureKeyboard";
        private const string GateSlamAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper.asset";
        private const string SkyHookAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_SkyHook.asset";
        private const string PursuitSlamAttackPath = "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_RollCatcher.asset";
        private const float AirborneCaptureHeightOffset = 1.15f;
        private const float AirborneCaptureVerticalVelocity = 0.75f;
        private const float DodgeStartupOpenProgress = 0.65f;
        private const float DodgeStartupCloseProgress = 0.90f;
        private const float TowardBossDotThreshold = 0.70f;
        private const float InputScenarioTimeoutSeconds = 2f;

        private static readonly CaptureStep[] CaptureSteps =
        {
            new CaptureStep(0.60f, CaptureCommand.SkyHook, "Sky Hook / Anti-Air", 4.75f, true),
            new CaptureStep(4.10f, CaptureCommand.PursuitSlam, "Pursuit Slam / Roll Catch", 4.10f, false),
            new CaptureStep(7.60f, CaptureCommand.GateSlam, "Gate Slam / Guard Break", 2.15f, false)
        };

        private static readonly InputCaptureStep[] GateSlamGuardInputSteps =
        {
            new InputCaptureStep(0.20f, InputCaptureCommand.PressGuard, "Press <Keyboard>/leftCtrl"),
            new InputCaptureStep(0.60f, InputCaptureCommand.TriggerGateSlam, "Trigger Gate Slam while guard is active"),
            new InputCaptureStep(1.12f, InputCaptureCommand.ReleaseGuard, "Release <Keyboard>/leftCtrl"),
            new InputCaptureStep(1.30f, InputCaptureCommand.RecordResult, "Record Gate Slam guard result")
        };

        private static readonly InputCaptureStep[] GateSlamDodgeInputSteps =
        {
            new InputCaptureStep(0.60f, InputCaptureCommand.TriggerGateSlam, "Trigger Gate Slam before dodge"),
            new InputCaptureStep(0.80f, InputCaptureCommand.PressDodge, "Press <Keyboard>/leftShift"),
            new InputCaptureStep(0.86f, InputCaptureCommand.ReleaseDodge, "Release <Keyboard>/leftShift"),
            new InputCaptureStep(1.30f, InputCaptureCommand.RecordResult, "Record Gate Slam dodge result")
        };

        private static bool driverActive;
        private static bool driverHideHud;
        private static bool driverFocusSceneView;
        private static CaptureScenario driverScenario;
        private static PlayerCharacter player;
        private static PlayerStateMachine playerStateMachine;
        private static LockOnTargetSelector lockOnTargetSelector;
        private static EnemyBrain bossBrain;
        private static EnemyStateMachine bossStateMachine;
        private static EnemyAttackController bossAttackController;
        private static EnemyArchetypeSO originalBossArchetype;
        private static EnemyArchetypeSO runtimeBossArchetype;
        private static Vector3 initialBossPosition;
        private static Quaternion initialBossRotation;
        private static double driverStartTime;
        private static int nextStepIndex;
        private static float inputScenarioInitialHealth;
        private static float inputScenarioInitialAgility;
        private static Keyboard captureKeyboard;
        private static Key injectedDodgeMovementKey;
        private static float injectedDodgeAlignment;
        private static bool sawAttackCommitted;
        private static bool sawBlockState;
        private static bool sawGuardStartup;
        private static bool sawActiveGuard;
        private static bool sawDodgeState;
        private static bool sawGroundDodge;
        private static bool sawDodgeInvulnerability;
        private static bool sawDodgeFollowUpWindow;
        private static bool sawSuccessfulDodge;
        private static bool sawGuardBreak;

        static GhostSamuraiBossReadCaptureDriverMenu()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.update -= HandleEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= CleanupBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeAssemblyReload;

            if (IsBatchOrTestRun())
            {
                return;
            }

            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
            EditorApplication.update += HandleEditorUpdate;
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Boss Read Capture Driver/Debug HUD")]
        public static void StartDebugHudDriver()
        {
            StartDriver(CaptureScenario.ReadSequence, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Boss Read Capture Driver/Clean HUD")]
        public static void StartCleanHudDriver()
        {
            StartDriver(CaptureScenario.ReadSequence, hideHud: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Boss Read Capture Driver/Scene View")]
        public static void StartSceneViewDriver()
        {
            StartDriver(CaptureScenario.ReadSequence, hideHud: false, focusSceneView: true);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Boss Input Capture Driver/Gate Slam Guard/Debug HUD")]
        public static void StartGateSlamGuardInputDriver()
        {
            StartDriver(CaptureScenario.GateSlamGuardInput, hideHud: false);
        }

        [MenuItem("CampusRPG/Setup/Local Preview/Start Boss Input Capture Driver/Gate Slam Dodge/Debug HUD")]
        public static void StartGateSlamDodgeInputDriver()
        {
            StartDriver(CaptureScenario.GateSlamDodgeInput, hideHud: false);
        }

        private static void StartDriver(
            CaptureScenario scenario,
            bool hideHud,
            bool focusSceneView = false)
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
            if (IsBatchOrTestRun() || !System.IO.File.Exists(RequestPath))
            {
                return;
            }

            string request;

            try
            {
                request = System.IO.File.ReadAllText(RequestPath).Trim();
            }
            catch (System.IO.IOException)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(request)
                || request == SessionState.GetString(LastRequestKey, string.Empty))
            {
                return;
            }

            SessionState.SetString(LastRequestKey, request);
            ParseRequest(request, out bool hideHud, out bool focusSceneView);
            StartDriver(ParseScenario(request), hideHud, focusSceneView);
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
                CleanupRuntimeArchetype();
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
            driverScenario = (CaptureScenario)SessionState.GetInt(
                ScenarioKey,
                (int)CaptureScenario.ReadSequence);
            PrepareSceneForCapture();
        }

        private static void PrepareSceneForCapture()
        {
            CleanupInputCaptureRuntime();
            player = Object.FindObjectOfType<PlayerCharacter>();
            playerStateMachine = player != null ? player.StateMachine : null;
            lockOnTargetSelector = player != null ? player.LockOnTargetSelector : null;
            bossBrain = FindBossBrain();

            if (bossBrain != null && !bossBrain.gameObject.activeInHierarchy)
            {
                EncounterController encounter = bossBrain.GetComponentInParent<EncounterController>(true);
                encounter?.ActivateEncounter();
            }

            if (bossBrain != null
                && runtimeBossArchetype != null
                && originalBossArchetype != null
                && bossBrain.Archetype == runtimeBossArchetype)
            {
                SetPrivateField(bossBrain, "archetype", originalBossArchetype);
            }

            bossStateMachine = bossBrain != null ? bossBrain.StateMachine : null;
            bossAttackController = bossBrain != null ? bossBrain.AttackController : null;
            originalBossArchetype = bossBrain != null ? bossBrain.Archetype : null;

            if (player == null
                || playerStateMachine == null
                || bossBrain == null
                || bossStateMachine == null
                || bossAttackController == null
                || originalBossArchetype == null)
            {
                Debug.LogWarning(
                    "[TY_NEW BossReadDriver] Could not find PlayerCharacter/Boss_Gatekeeper runtime wiring. " +
                    "Open BossTest or a Gatekeeper scene first.");
                driverActive = false;
                return;
            }

            initialBossPosition = bossBrain.transform.position;
            initialBossRotation = bossBrain.transform.rotation;
            DisableNonBossEnemyAI();
            EnsureBossRuntimePreview();

            bool isInputScenario = driverScenario != CaptureScenario.ReadSequence;
            ResetBossReadPose(
                driverScenario == CaptureScenario.GateSlamDodgeInput
                    ? 4.10f
                    : isInputScenario
                        ? 2.15f
                        : CaptureSteps[0].PlayerDistance,
                isInputScenario ? false : CaptureSteps[0].StartAirborne);

            if (isInputScenario)
            {
                bossBrain.enabled = false;
                bossAttackController.AttackCommitted -= HandleBossAttackCommitted;
                bossAttackController.AttackCommitted += HandleBossAttackCommitted;
            }

            ApplyHudVisibility();
            FocusSceneViewForCapture();
            driverStartTime = EditorApplication.timeSinceStartup;
            nextStepIndex = 0;
            ResetInputScenarioTelemetry();
            driverActive = true;
            Debug.Log(
                $"[TY_NEW BossReadDriver] Started {driverScenario} capture " +
                $"{(driverHideHud ? "clean-HUD" : "debug-HUD")}.");
        }

        private static void TickDriver()
        {
            if (!driverActive
                || !EditorApplication.isPlaying
                || bossStateMachine == null)
            {
                return;
            }

            float elapsedSeconds = (float)(EditorApplication.timeSinceStartup - driverStartTime);

            if (driverScenario == CaptureScenario.ReadSequence)
            {
                TickReadSequence(elapsedSeconds);
                return;
            }

            ObserveInputScenarioTelemetry();

            if (HasCompletedInputScenarioOutcome())
            {
                RecordInputScenarioResult();
                return;
            }

            InputCaptureStep[] inputSteps = ResolveInputCaptureSteps();

            if (nextStepIndex >= inputSteps.Length)
            {
                return;
            }

            InputCaptureStep inputStep = inputSteps[nextStepIndex];

            if (elapsedSeconds < inputStep.TimeSeconds)
            {
                return;
            }

            if (!IsInputCaptureStepReady(inputStep.Command))
            {
                if (elapsedSeconds >= InputScenarioTimeoutSeconds)
                {
                    RecordInputScenarioResult();
                }

                return;
            }

            if (!ExecuteInputCaptureStep(inputStep.Command))
            {
                return;
            }

            Debug.Log(
                $"[TY_NEW BossInputDriver] {inputStep.Label} at {elapsedSeconds:0.00}s.");
            nextStepIndex++;
        }

        private static void TickReadSequence(float elapsedSeconds)
        {
            if (nextStepIndex >= CaptureSteps.Length)
            {
                return;
            }

            CaptureStep step = CaptureSteps[nextStepIndex];

            if (elapsedSeconds < step.TimeSeconds)
            {
                return;
            }

            ResetBossReadPose(step.PlayerDistance, step.StartAirborne);
            TriggerBossAttack(step.Command);
            Debug.Log($"[TY_NEW BossReadDriver] Triggered {step.Label} at {elapsedSeconds:0.00}s.");
            nextStepIndex++;
        }

        private static InputCaptureStep[] ResolveInputCaptureSteps()
        {
            return driverScenario == CaptureScenario.GateSlamGuardInput
                ? GateSlamGuardInputSteps
                : GateSlamDodgeInputSteps;
        }

        private static bool IsInputCaptureStepReady(InputCaptureCommand command)
        {
            if (command == InputCaptureCommand.PressDodge)
            {
                if (bossStateMachine?.CurrentState is not EnemyAttackState attackState)
                {
                    return false;
                }

                AttackDefinitionSO attack = attackState.CurrentAttackDefinition;
                return attack != null
                    && attack.AttackId == "Enemy_Gatekeeper"
                    && attackState.PresentationPhase == EnemyAttackPresentationPhase.Startup
                    && attackState.PresentationProgress >= DodgeStartupOpenProgress
                    && attackState.PresentationProgress <= DodgeStartupCloseProgress;
            }

            return command != InputCaptureCommand.ReleaseDodge || sawGroundDodge;
        }

        private static bool ExecuteInputCaptureStep(InputCaptureCommand command)
        {
            switch (command)
            {
                case InputCaptureCommand.PressGuard:
                    return QueueCaptureKeyboardState(Key.LeftCtrl);
                case InputCaptureCommand.ReleaseGuard:
                    return QueueCaptureKeyboardState();
                case InputCaptureCommand.PressDodge:
                    if (!TryResolveTowardBossKey(out injectedDodgeMovementKey, out injectedDodgeAlignment))
                    {
                        Debug.LogWarning(
                            "[TY_NEW BossInputDriver] Could not resolve a locked-on, camera-relative " +
                            "WASD direction toward the boss.");
                        return false;
                    }

                    return QueueCaptureKeyboardState(injectedDodgeMovementKey, Key.LeftShift);
                case InputCaptureCommand.ReleaseDodge:
                    return QueueCaptureKeyboardState();
                case InputCaptureCommand.TriggerGateSlam:
                    if (bossBrain != null)
                    {
                        bossBrain.enabled = true;
                    }

                    TriggerBossAttack(CaptureCommand.GateSlam);
                    return true;
                case InputCaptureCommand.RecordResult:
                    RecordInputScenarioResult();
                    return true;
                default:
                    return false;
            }
        }

        private static bool QueueCaptureKeyboardState(params Key[] pressedKeys)
        {
            EnsureCaptureKeyboard();
            InputSystem.QueueStateEvent(captureKeyboard, new KeyboardState(pressedKeys));
            Debug.Log(
                $"[TY_NEW BossInputDriver] Queued {captureKeyboard.name} state=" +
                $"{(pressedKeys.Length == 0 ? "<released>" : string.Join("+", pressedKeys))}.");
            return true;
        }

        private static void EnsureCaptureKeyboard()
        {
            if (captureKeyboard != null && captureKeyboard.added)
            {
                return;
            }

            captureKeyboard = InputSystem.AddDevice<Keyboard>(CaptureKeyboardName);
        }

        private static void CleanupInputCaptureRuntime()
        {
            if (bossAttackController != null)
            {
                bossAttackController.AttackCommitted -= HandleBossAttackCommitted;
            }

            if (captureKeyboard != null && captureKeyboard.added)
            {
                InputSystem.RemoveDevice(captureKeyboard);
            }

            captureKeyboard = null;
            injectedDodgeMovementKey = Key.None;
            injectedDodgeAlignment = 0f;
        }

        private static void CleanupBeforeAssemblyReload()
        {
            CleanupInputCaptureRuntime();
        }

        private static bool TryResolveTowardBossKey(out Key resolvedKey, out float alignment)
        {
            resolvedKey = Key.None;
            alignment = float.NegativeInfinity;

            if (player == null
                || bossBrain == null
                || player.CameraTransform == null
                || lockOnTargetSelector == null
                || lockOnTargetSelector.CurrentTarget == null)
            {
                return false;
            }

            EnemyBrain lockedEnemy = lockOnTargetSelector.CurrentTarget.GetComponentInParent<EnemyBrain>();

            if (lockedEnemy != bossBrain)
            {
                return false;
            }

            Vector3 towardBoss = bossBrain.transform.position - player.transform.position;
            towardBoss.y = 0f;

            if (towardBoss.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            towardBoss.Normalize();
            ConsiderTowardBossKey(Key.W, Vector2.up, towardBoss, ref resolvedKey, ref alignment);
            ConsiderTowardBossKey(Key.S, Vector2.down, towardBoss, ref resolvedKey, ref alignment);
            ConsiderTowardBossKey(Key.A, Vector2.left, towardBoss, ref resolvedKey, ref alignment);
            ConsiderTowardBossKey(Key.D, Vector2.right, towardBoss, ref resolvedKey, ref alignment);
            return resolvedKey != Key.None && alignment >= TowardBossDotThreshold;
        }

        private static void ConsiderTowardBossKey(
            Key key,
            Vector2 moveInput,
            Vector3 towardBoss,
            ref Key bestKey,
            ref float bestDot)
        {
            Vector3 worldDirection = PlayerMovementRuntimeUtility.BuildCameraRelativeMoveDirection(
                moveInput,
                player != null ? player.CameraTransform : null);
            float dot = Vector3.Dot(worldDirection, towardBoss);

            if (dot <= bestDot)
            {
                return;
            }

            bestDot = dot;
            bestKey = key;
        }

        private static void HandleBossAttackCommitted(EnemyAttackCommit commit)
        {
            if (player != null && commit.Target == player.transform)
            {
                sawAttackCommitted = true;
            }
        }

        private static void ResetInputScenarioTelemetry()
        {
            inputScenarioInitialHealth = player?.Health != null ? player.Health.CurrentValue : 0f;
            inputScenarioInitialAgility = player?.Gauges != null ? player.Gauges.AgilityGauge : 0f;
            injectedDodgeMovementKey = Key.None;
            injectedDodgeAlignment = 0f;
            sawAttackCommitted = false;
            sawBlockState = false;
            sawGuardStartup = false;
            sawActiveGuard = false;
            sawDodgeState = false;
            sawGroundDodge = false;
            sawDodgeInvulnerability = false;
            sawDodgeFollowUpWindow = false;
            sawSuccessfulDodge = false;
            sawGuardBreak = false;
        }

        private static void ObserveInputScenarioTelemetry()
        {
            if (playerStateMachine == null)
            {
                return;
            }

            sawBlockState |= playerStateMachine.IsBlocking;
            sawGuardStartup |= playerStateMachine.IsBlocking && !playerStateMachine.HasActiveGuard;
            sawActiveGuard |= playerStateMachine.HasActiveGuard;
            sawGuardBreak |= playerStateMachine.CurrentHitReactionType == PlayerHitReactionType.GuardBreak;

            if (playerStateMachine.CurrentState is PlayerDodgeState dodgeState)
            {
                sawDodgeState = true;
                sawGroundDodge |= dodgeState.ActionType == PlayerEvasiveActionType.GroundDodge;
                sawDodgeInvulnerability |= dodgeState.IsInvulnerable;
            }

            sawDodgeFollowUpWindow |= player?.CombatController != null
                && player.CombatController.HasDodgeFollowUpWindow;
            sawSuccessfulDodge |= player?.Gauges != null
                && player.Gauges.AgilityGauge > inputScenarioInitialAgility + Mathf.Epsilon;
        }

        private static bool HasCompletedInputScenarioOutcome()
        {
            return sawAttackCommitted
                && (driverScenario == CaptureScenario.GateSlamGuardInput
                    ? sawGuardBreak
                    : sawSuccessfulDodge);
        }

        private static void RecordInputScenarioResult()
        {
            ObserveInputScenarioTelemetry();
            float finalHealth = player?.Health != null ? player.Health.CurrentValue : 0f;
            float finalAgility = player?.Gauges != null ? player.Gauges.AgilityGauge : 0f;
            float expectedAgilityGain = player?.CombatController?.Balance != null
                ? player.CombatController.Balance.DodgeAgilityGaugeGain
                : 0f;
            bool registeredExpectedDodgeGain = expectedAgilityGain > 0f
                && finalAgility >= inputScenarioInitialAgility + expectedAgilityGain - 0.01f;
            bool passed = driverScenario == CaptureScenario.GateSlamGuardInput
                ? sawAttackCommitted
                    && sawBlockState
                    && sawGuardStartup
                    && sawActiveGuard
                    && sawGuardBreak
                    && finalHealth < inputScenarioInitialHealth
                : sawAttackCommitted
                    && sawDodgeState
                    && sawGroundDodge
                    && sawDodgeInvulnerability
                    && registeredExpectedDodgeGain
                    && Mathf.Approximately(finalHealth, inputScenarioInitialHealth);

            Debug.Log(
                $"[TY_NEW BossInputDriver] RESULT scenario={driverScenario} " +
                $"device={CaptureKeyboardName} " +
                $"input={(driverScenario == CaptureScenario.GateSlamGuardInput ? "<Keyboard>/leftCtrl" : $"<Keyboard>/{injectedDodgeMovementKey}+leftShift")} " +
                $"dodgeAlignment={injectedDodgeAlignment:0.00} " +
                $"initialHP={inputScenarioInitialHealth:0.##} finalHP={finalHealth:0.##} " +
                $"initialAgility={inputScenarioInitialAgility:0.##} finalAgility={finalAgility:0.##} " +
                $"attackCommitted={sawAttackCommitted} block={sawBlockState} " +
                $"guardStartup={sawGuardStartup} activeGuard={sawActiveGuard} " +
                $"dodge={sawDodgeState} groundDodge={sawGroundDodge} " +
                $"invulnerable={sawDodgeInvulnerability} successfulDodge={sawSuccessfulDodge} " +
                $"dodgeFollowUp={sawDodgeFollowUpWindow} guardBreak={sawGuardBreak} " +
                $"outcome={(passed ? "PASS" : "FAIL")}.");

            CleanupInputCaptureRuntime();

            if (bossBrain != null)
            {
                bossBrain.enabled = false;
            }

            driverActive = false;
            EditorApplication.isPaused = true;
        }

        private static EnemyBrain FindBossBrain()
        {
            foreach (EnemyBrain candidate in Object.FindObjectsByType<EnemyBrain>(FindObjectsInactive.Include))
            {
                if (candidate == null)
                {
                    continue;
                }

                if (string.Equals(candidate.gameObject.name, BossObjectName, System.StringComparison.Ordinal))
                {
                    return candidate;
                }

                if (candidate.Archetype != null && candidate.Archetype.ArchetypeType == EnemyArchetypeType.Boss)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void DisableNonBossEnemyAI()
        {
            foreach (EnemyBrain brain in Object.FindObjectsOfType<EnemyBrain>())
            {
                if (brain == null || brain == bossBrain)
                {
                    continue;
                }

                brain.enabled = false;
            }

            foreach (EnemyStateMachine stateMachine in Object.FindObjectsOfType<EnemyStateMachine>())
            {
                if (stateMachine == null || stateMachine == bossStateMachine)
                {
                    continue;
                }

                stateMachine.enabled = false;
            }

            foreach (EnemyAttackController attackController in Object.FindObjectsOfType<EnemyAttackController>())
            {
                if (attackController == null || attackController == bossAttackController)
                {
                    continue;
                }

                attackController.enabled = false;
            }

            foreach (NavMeshAgent agent in Object.FindObjectsOfType<NavMeshAgent>())
            {
                if (agent == null || agent.gameObject == bossBrain?.gameObject)
                {
                    continue;
                }

                if (agent.enabled)
                {
                    agent.ResetPath();
                    agent.enabled = false;
                }
            }
        }

        private static void EnsureBossRuntimePreview()
        {
            if (bossBrain == null)
            {
                return;
            }

            RuntimeAnimatorController controller =
                CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);

            if (controller == null)
            {
                Debug.LogWarning("[TY_NEW BossReadDriver] Imported enemy preview controller is not available.");
                return;
            }

            Animator rootAnimator = bossBrain.GetComponent<Animator>();
            CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                bossBrain.gameObject,
                CombatProxyVisualKind.EnemyMelee,
                rootAnimator);

            Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(bossBrain.gameObject);

            if (importedAnimator == null)
            {
                Debug.LogWarning("[TY_NEW BossReadDriver] Imported enemy preview animator was not created for the boss.");
                return;
            }

            importedAnimator.enabled = true;
            importedAnimator.runtimeAnimatorController = controller;
            importedAnimator.applyRootMotion = false;
            importedAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            importedAnimator.updateMode = AnimatorUpdateMode.Normal;
            importedAnimator.Rebind();

            EnemyVisualPresentationRelay presentationRelay = bossBrain.GetComponent<EnemyVisualPresentationRelay>();

            if (presentationRelay != null)
            {
                presentationRelay.enabled = false;
            }

            EnemyCombatAnimationRelay importedRelay = bossBrain.GetComponent<EnemyCombatAnimationRelay>();

            if (importedRelay == null)
            {
                importedRelay = bossBrain.gameObject.AddComponent<EnemyCombatAnimationRelay>();
            }

            importedRelay.enabled = true;
        }

        private static void ResetBossReadPose(float playerDistance, bool startAirborne)
        {
            if (player == null || playerStateMachine == null || bossBrain == null || bossStateMachine == null)
            {
                return;
            }

            Vector3 bossForward = initialBossRotation * Vector3.forward;
            bossForward.y = 0f;

            if (bossForward.sqrMagnitude <= Mathf.Epsilon)
            {
                bossForward = Vector3.forward;
            }

            bossForward.Normalize();
            Vector3 playerPosition = initialBossPosition + bossForward * Mathf.Max(1.8f, playerDistance);

            if (startAirborne)
            {
                playerPosition += Vector3.up * AirborneCaptureHeightOffset;
            }

            Quaternion playerRotation = Quaternion.LookRotation(-bossForward, Vector3.up);
            bossBrain.transform.SetPositionAndRotation(initialBossPosition, initialBossRotation);
            bossBrain.Health?.RestoreFull();
            bossBrain.Motor?.Stop();
            bossAttackController.ResetRuntimeState();
            bossBrain.ClearTarget();
            bossStateMachine.SwitchToIdle();

            player.Motor?.WarpTo(playerPosition, playerRotation);
            player.Motor?.ResetMotion();

            if (startAirborne)
            {
                player.Motor?.ApplyActionVerticalVelocity(AirborneCaptureVerticalVelocity, onlyIfHigher: false);
            }

            player.Health?.RestoreFull();
            player.Mana?.RestoreFull();
            player.Gauges?.ResetAll();
            player.CombatController?.ResetRuntimeState();
            lockOnTargetSelector?.ResetRuntimeState();
            playerStateMachine.SwitchToLocomotion();

            Physics.SyncTransforms();
            ApplyBossLockTarget();
        }

        private static void ApplyBossLockTarget()
        {
            if (player == null || bossBrain == null)
            {
                return;
            }

            bossBrain.SetTarget(player.transform);

            if (lockOnTargetSelector == null)
            {
                return;
            }

            MethodInfo method = typeof(LockOnTargetSelector).GetMethod(
                "SetCurrentTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (method != null)
            {
                method.Invoke(lockOnTargetSelector, new object[] { bossBrain.transform });
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

        private static void FocusSceneViewForCapture()
        {
            if (!driverFocusSceneView || SceneView.lastActiveSceneView == null || player == null || bossBrain == null)
            {
                return;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            Vector3 target = Vector3.Lerp(player.transform.position, bossBrain.transform.position, 0.5f);
            sceneView.pivot = target + Vector3.up * 0.9f;
            sceneView.rotation = Quaternion.Euler(18f, 4f, 0f);
            sceneView.size = 6.6f;
            sceneView.Repaint();
        }

        private static void TriggerBossAttack(CaptureCommand command)
        {
            if (bossBrain == null || bossStateMachine == null || originalBossArchetype == null)
            {
                return;
            }

            AttackDefinitionSO attack = LoadAttack(command);

            if (attack == null)
            {
                Debug.LogWarning($"[TY_NEW BossReadDriver] Missing attack asset for {command}.");
                return;
            }

            if (runtimeBossArchetype == null)
            {
                runtimeBossArchetype = Object.Instantiate(originalBossArchetype);
                runtimeBossArchetype.name = originalBossArchetype.name + "_TY_NEW_RuntimePreview";
                runtimeBossArchetype.hideFlags = HideFlags.DontSave;
            }

            SetPrivateField(runtimeBossArchetype, "attacks", new[] { attack });
            SetPrivateField(bossBrain, "archetype", runtimeBossArchetype);
            bossBrain.Motor?.SetMoveSpeed(runtimeBossArchetype.MoveSpeed);
            bossAttackController.ResetRuntimeState();
            bossBrain.SetTarget(player != null ? player.transform : null);
            bossStateMachine.SwitchToAttack();
        }

        private static AttackDefinitionSO LoadAttack(CaptureCommand command)
        {
            string assetPath = command switch
            {
                CaptureCommand.SkyHook => SkyHookAttackPath,
                CaptureCommand.PursuitSlam => PursuitSlamAttackPath,
                _ => GateSlamAttackPath
            };

            return AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(assetPath);
        }

        private static void CleanupRuntimeArchetype()
        {
            driverActive = false;
            nextStepIndex = 0;
            CleanupInputCaptureRuntime();

            if (bossBrain != null)
            {
                bossBrain.enabled = true;
            }

            if (bossBrain != null && originalBossArchetype != null)
            {
                SetPrivateField(bossBrain, "archetype", originalBossArchetype);
            }

            if (runtimeBossArchetype != null)
            {
                Object.DestroyImmediate(runtimeBossArchetype);
                runtimeBossArchetype = null;
            }
        }

        private static void ParseRequest(string request, out bool hideHud, out bool focusSceneView)
        {
            string normalized = request.Trim().ToLowerInvariant();
            hideHud = normalized.Contains("clean");
            focusSceneView = normalized.Contains("scene");
        }

        private static CaptureScenario ParseScenario(string request)
        {
            string normalized = request.Trim().ToLowerInvariant();

            if (normalized.Contains("guard-input"))
            {
                return CaptureScenario.GateSlamGuardInput;
            }

            return normalized.Contains("dodge-input")
                ? CaptureScenario.GateSlamDodgeInput
                : CaptureScenario.ReadSequence;
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
                CaptureCommand command,
                string label,
                float playerDistance,
                bool startAirborne)
            {
                TimeSeconds = timeSeconds;
                Command = command;
                Label = label;
                PlayerDistance = playerDistance;
                StartAirborne = startAirborne;
            }

            public float TimeSeconds { get; }

            public CaptureCommand Command { get; }

            public string Label { get; }

            public float PlayerDistance { get; }

            public bool StartAirborne { get; }
        }

        private readonly struct InputCaptureStep
        {
            public InputCaptureStep(float timeSeconds, InputCaptureCommand command, string label)
            {
                TimeSeconds = timeSeconds;
                Command = command;
                Label = label;
            }

            public float TimeSeconds { get; }

            public InputCaptureCommand Command { get; }

            public string Label { get; }
        }

        private enum CaptureScenario
        {
            ReadSequence = 0,
            GateSlamGuardInput = 1,
            GateSlamDodgeInput = 2
        }

        private enum CaptureCommand
        {
            SkyHook = 0,
            PursuitSlam = 1,
            GateSlam = 2
        }

        private enum InputCaptureCommand
        {
            PressGuard = 0,
            ReleaseGuard = 1,
            PressDodge = 2,
            ReleaseDodge = 3,
            TriggerGateSlam = 4,
            RecordResult = 5
        }
    }
}
#endif
