using System.Collections.Generic;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CampusRPG.UI
{
    public readonly struct CombatDebugHudLayout
    {
        public CombatDebugHudLayout(
            float lineX,
            float topY,
            float lineWidth,
            float lineHeight,
            float lineAdvance,
            float maxY)
        {
            LineX = lineX;
            TopY = topY;
            LineWidth = lineWidth;
            LineHeight = lineHeight;
            LineAdvance = lineAdvance;
            MaxY = maxY;
        }

        public float LineX { get; }

        public float TopY { get; }

        public float LineWidth { get; }

        public float LineHeight { get; }

        public float LineAdvance { get; }

        public float MaxY { get; }

        public int MaxVisibleLineCount
        {
            get
            {
                float usableHeight = MaxY - TopY - LineHeight;
                return usableHeight < 0f ? 0 : Mathf.FloorToInt(usableHeight / LineAdvance) + 1;
            }
        }

        public bool CanDrawLine(float y)
        {
            return y + LineHeight <= MaxY;
        }

        public int GetContentLineCount(int totalLineCount)
        {
            int visibleLineCount = Mathf.Min(Mathf.Max(0, totalLineCount), MaxVisibleLineCount);
            bool needsOverflowLine = totalLineCount > visibleLineCount;
            return needsOverflowLine && visibleLineCount > 0 ? visibleLineCount - 1 : visibleLineCount;
        }

        public int GetHiddenLineCount(int totalLineCount)
        {
            return Mathf.Max(0, totalLineCount - GetContentLineCount(totalLineCount));
        }

        public Rect BuildLineRect(float y)
        {
            return new Rect(LineX, y, LineWidth, LineHeight);
        }
    }

    public static class CombatDebugHudLayoutUtility
    {
        private const float LeftMargin = 16f;
        private const float RightMargin = 16f;
        private const float TopMargin = 16f;
        private const float MaxLineWidth = 420f;
        private const float LineHeight = 22f;
        private const float LineAdvance = 20f;
        private const float BottomHudGap = 12f;
        private const float PanelPaddingX = 8f;
        private const float PanelPaddingY = 6f;
        private const float CollapsedPanelWidth = 190f;

        public static CombatDebugHudLayout Build(float screenWidth, float screenHeight)
        {
            float availableWidth = Mathf.Max(1f, screenWidth - LeftMargin - RightMargin);
            float lineWidth = Mathf.Min(MaxLineWidth, Mathf.Max(1f, availableWidth - PanelPaddingX * 2f));
            float lineX = screenWidth >= lineWidth + LeftMargin + RightMargin
                ? LeftMargin
                : Mathf.Max(0f, screenWidth - lineWidth);
            SwordArtHudLayout swordArtLayout = SwordArtHudLayoutUtility.Build(screenWidth, screenHeight);
            float maxY = Mathf.Max(TopMargin, swordArtLayout.PanelRect.yMin - BottomHudGap - PanelPaddingY);

            return new CombatDebugHudLayout(
                lineX,
                TopMargin,
                lineWidth,
                LineHeight,
                LineAdvance,
                maxY);
        }

        public static string BuildOverflowLine(int hiddenLineCount)
        {
            return hiddenLineCount > 0 ? $"+{hiddenLineCount} debug lines hidden" : string.Empty;
        }

        public static Rect BuildPanelRect(CombatDebugHudLayout layout, int totalLineCount)
        {
            int visibleLineCount = Mathf.Min(Mathf.Max(0, totalLineCount), layout.MaxVisibleLineCount);

            if (visibleLineCount <= 0)
            {
                return Rect.zero;
            }

            float contentHeight = layout.LineHeight + layout.LineAdvance * (visibleLineCount - 1);
            return new Rect(
                Mathf.Max(0f, layout.LineX - PanelPaddingX),
                Mathf.Max(0f, layout.TopY - PanelPaddingY),
                layout.LineWidth + PanelPaddingX * 2f,
                contentHeight + PanelPaddingY * 2f);
        }

        public static Rect BuildCollapsedPanelRect(float screenWidth)
        {
            float width = Mathf.Min(CollapsedPanelWidth, Mathf.Max(1f, screenWidth - LeftMargin - RightMargin));
            float x = screenWidth >= width + LeftMargin + RightMargin
                ? LeftMargin
                : Mathf.Max(0f, screenWidth - width);

            return new Rect(
                x,
                Mathf.Max(0f, TopMargin - PanelPaddingY),
                width,
                LineHeight + PanelPaddingY * 2f);
        }

        public static Rect BuildCollapsedLabelRect(Rect panelRect)
        {
            return new Rect(
                panelRect.x + PanelPaddingX,
                panelRect.y + PanelPaddingY,
                Mathf.Max(1f, panelRect.width - PanelPaddingX * 2f),
                LineHeight);
        }
    }

    public static class CombatDebugHudInputUtility
    {
        public const string ToggleShortcutLabel = "F1/`: Hide Debug HUD";
        public const string CollapsedHintLabel = "Debug HUD hidden (F1/`)";

        public static bool ShouldToggleDebugPanel(Keyboard keyboard)
        {
            return keyboard != null
                && (WasPressedThisFrame(keyboard.f1Key) || WasPressedThisFrame(keyboard.backquoteKey));
        }

        private static bool WasPressedThisFrame(ButtonControl control)
        {
            return control != null && control.wasPressedThisFrame;
        }
    }

    public static class CombatDebugHudAnimatorClipUtility
    {
        private const string DefaultClipLineLabel = "Anim Clip";

        public static string BuildAnimatorClipLine(Animator animator, int layerIndex = 0)
        {
            return BuildAnimatorClipLine(DefaultClipLineLabel, animator, layerIndex);
        }

        public static string BuildAnimatorClipLine(string lineLabel, Animator animator, int layerIndex = 0)
        {
            if (animator == null
                || !animator.isActiveAndEnabled
                || layerIndex < 0
                || layerIndex >= animator.layerCount)
            {
                return string.Empty;
            }

            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);

            if (clipInfos == null || clipInfos.Length == 0)
            {
                return string.Empty;
            }

            AnimatorClipInfo bestClipInfo = clipInfos[0];

            for (int i = 1; i < clipInfos.Length; i++)
            {
                if (clipInfos[i].weight > bestClipInfo.weight)
                {
                    bestClipInfo = clipInfos[i];
                }
            }

            if (bestClipInfo.clip == null)
            {
                return string.Empty;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
            return BuildAnimatorClipLine(
                lineLabel,
                bestClipInfo.clip.name,
                stateInfo.normalizedTime,
                bestClipInfo.weight,
                animator.IsInTransition(layerIndex));
        }

        public static string BuildAnimatorClipLine(
            string clipName,
            float normalizedTime,
            float weight,
            bool isInTransition = false)
        {
            return BuildAnimatorClipLine(DefaultClipLineLabel, clipName, normalizedTime, weight, isInTransition);
        }

        public static string BuildAnimatorClipLine(
            string lineLabel,
            string clipName,
            float normalizedTime,
            float weight,
            bool isInTransition = false)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return string.Empty;
            }

            string label = string.IsNullOrWhiteSpace(lineLabel) ? DefaultClipLineLabel : lineLabel.Trim();
            string transitionSuffix = isInTransition ? " blend" : string.Empty;
            return $"{label}: {ShortenClipName(clipName)} @ {normalizedTime:0.00}x w{Mathf.Clamp01(weight):0.00}{transitionSuffix}";
        }

        public static string ShortenClipName(string clipName)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return string.Empty;
            }

            string result = clipName.Trim();

            if (result.StartsWith("AN_Player_", System.StringComparison.Ordinal))
            {
                result = result.Substring("AN_Player_".Length);
            }
            else if (result.StartsWith("AN_Enemy_", System.StringComparison.Ordinal))
            {
                result = result.Substring("AN_Enemy_".Length);
            }

            if (result.EndsWith("_CombatTest", System.StringComparison.Ordinal))
            {
                result = result.Substring(0, result.Length - "_CombatTest".Length);
            }

            return result;
        }
    }

    public sealed class CombatDebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField] private LockOnTargetSelector lockOnTargetSelector;
        [SerializeField] private SwordArtHudPresenter swordArtHudPresenter;
        [SerializeField] private bool showControlHelp = true;
        [SerializeField] private bool ensureSwordArtHud = true;
        [SerializeField] private bool showDebugPanel = true;

        private GUIStyle labelStyle;
        private Texture2D panelBackground;
        private CombatDebugHudLayout layout;
        private Animator playerAnimator;

        private void OnDestroy()
        {
            if (panelBackground != null)
            {
                Destroy(panelBackground);
                panelBackground = null;
            }
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureSwordArtHudPresenter();
        }

        private void Update()
        {
            if (CombatDebugHudInputUtility.ShouldToggleDebugPanel(Keyboard.current))
            {
                showDebugPanel = !showDebugPanel;
            }
        }

        private void OnGUI()
        {
            ResolveReferences();
            EnsureSwordArtHudPresenter();
            EnsureHudResources();

            if (!showDebugPanel)
            {
                DrawCollapsedHint();
                return;
            }

            if (playerCharacter == null)
            {
                return;
            }

            layout = CombatDebugHudLayoutUtility.Build(Screen.width, Screen.height);
            List<string> lines = new List<string>();
            PlayerStateMachine stateMachine = playerCharacter.StateMachine;
            string stateLine = $"<b>State</b>: {stateMachine?.CurrentState?.GetType().Name ?? "None"}";

            string animatorClipLine = CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(playerAnimator);
            Transform target = lockOnTargetSelector != null ? lockOnTargetSelector.CurrentTarget : null;
            bool shouldDeferPlayerAnimatorLine = target != null && HasTargetAttackTimingLine(target);
            PlayerCombatController combatController = playerCharacter.CombatController;
            string attackTimingLine = CombatDebugHudAttackTimingUtility.BuildAttackTimingLine(combatController, compact: true);
            bool hasAttackTimingLine = !string.IsNullOrWhiteSpace(attackTimingLine);
            bool shouldDeferStateLine = hasAttackTimingLine && shouldDeferPlayerAnimatorLine;
            string actionFeedbackLine = CombatDebugHudActionFeedbackUtility.BuildPlayerActionFeedbackLine(
                stateMachine,
                combatController);
            string actionAudioFeedbackLine = CombatDebugHudActionFeedbackUtility.BuildActionAudioFeedbackLine(
                ProceduralAudioUtility.LastActionCueDecision,
                Time.unscaledTime);

            if (!shouldDeferStateLine)
            {
                lines.Add(stateLine);
            }

            if (!shouldDeferPlayerAnimatorLine && !string.IsNullOrWhiteSpace(animatorClipLine))
            {
                lines.Add(animatorClipLine);
            }

            if (stateMachine != null && stateMachine.CurrentHitReactionType == PlayerHitReactionType.GuardBreak)
            {
                lines.Add("<b>Hit Reaction</b>: Guard Break");
            }

            if (hasAttackTimingLine)
            {
                lines.Add(attackTimingLine);
            }
            else if (!string.IsNullOrWhiteSpace(actionFeedbackLine))
            {
                lines.Add(actionFeedbackLine);
            }

            if (target != null)
            {
                AppendTargetReadLines(lines, target);
            }

            if (shouldDeferStateLine)
            {
                lines.Add(stateLine);
            }

            if (shouldDeferPlayerAnimatorLine && !string.IsNullOrWhiteSpace(animatorClipLine))
            {
                lines.Add(animatorClipLine);
            }

            if (hasAttackTimingLine && !string.IsNullOrWhiteSpace(actionFeedbackLine))
            {
                lines.Add(actionFeedbackLine);
            }

            if (!string.IsNullOrWhiteSpace(actionAudioFeedbackLine))
            {
                lines.Add(actionAudioFeedbackLine);
            }

            lines.Add($"HP: {playerCharacter.Health?.CurrentValue:0}/{playerCharacter.Health?.MaxValue:0}");
            lines.Add($"MP: {playerCharacter.Mana?.CurrentValue:0}/{playerCharacter.Mana?.MaxValue:0}");
            lines.Add($"Counter: {playerCharacter.Gauges?.CounterGauge:0}");
            lines.Add($"Agility: {playerCharacter.Gauges?.AgilityGauge:0}");

            if (playerCharacter.SkillController != null)
            {
                lines.Add(CombatDebugHudSkillStatusUtility.BuildSkillLine("Q", playerCharacter.SkillController, 0));
                lines.Add(CombatDebugHudSkillStatusUtility.BuildSkillLine("E", playerCharacter.SkillController, 1));
            }

            if (combatController != null && combatController.HasCurrentSwordArt)
            {
                lines.Add(
                    $"Sword Art Active: {combatController.CurrentSwordArt.DisplayName} -> {combatController.CurrentSwordArtAttack.DisplayName}");
            }
            else if (combatController != null && combatController.HasSwordArtPreview)
            {
                lines.Add(
                    $"Sword Art Preview: {combatController.PreviewSwordArt.DisplayName} -> {combatController.PreviewSwordArtAttack.DisplayName}");
            }

            if (combatController != null
                && combatController.TryGetBufferedSwordArtCancelWindowStatus(
                    out SwordArtDefinitionSO bufferedSwordArt,
                    out _,
                    out bool isCancelOpen,
                    out float secondsUntilCancelOpen))
            {
                string cancelStatus = isCancelOpen ? "open" : $"opens in {secondsUntilCancelOpen:0.00}s";
                lines.Add($"Sword Art Cancel: {bufferedSwordArt.DisplayName} {cancelStatus}");
            }

            if (target == null)
            {
                lines.Add("Lock Target: None");
            }

            if (showControlHelp)
            {
                lines.Add(string.Empty);
                lines.Add("LMB: Light  RMB: Heavy  Shift: Dodge  Ctrl: Block");
                lines.Add("Q/E: Skills  Tab: LockOn  Space: Jump");
                lines.Add(CombatDebugHudInputUtility.ToggleShortcutLabel);
            }

            DrawLines(lines);
        }

        private void EnsureHudResources()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    richText = true
                };
            }

            if (panelBackground == null)
            {
                panelBackground = CreatePanelBackground(new Color(0.03f, 0.04f, 0.05f, 0.86f));
            }
        }

        private void DrawCollapsedHint()
        {
            Rect panelRect = CombatDebugHudLayoutUtility.BuildCollapsedPanelRect(Screen.width);

            if (panelRect.width > 0f && panelRect.height > 0f)
            {
                GUI.DrawTexture(panelRect, panelBackground, ScaleMode.StretchToFill, true);
            }

            GUI.Label(
                CombatDebugHudLayoutUtility.BuildCollapsedLabelRect(panelRect),
                $"<b>{CombatDebugHudInputUtility.CollapsedHintLabel}</b>",
                labelStyle);
        }

        private void DrawLines(IReadOnlyList<string> lines)
        {
            Rect panelRect = CombatDebugHudLayoutUtility.BuildPanelRect(layout, lines.Count);

            if (panelRect.width > 0f && panelRect.height > 0f)
            {
                GUI.DrawTexture(panelRect, panelBackground, ScaleMode.StretchToFill, true);
            }

            float y = layout.TopY;
            int contentLineCount = layout.GetContentLineCount(lines.Count);
            int hiddenLineCount = layout.GetHiddenLineCount(lines.Count);

            for (int i = 0; i < contentLineCount; i++)
            {
                DrawLine(ref y, lines[i]);
            }

            string overflowLine = CombatDebugHudLayoutUtility.BuildOverflowLine(hiddenLineCount);

            if (!string.IsNullOrWhiteSpace(overflowLine))
            {
                DrawLine(ref y, overflowLine);
            }
        }

        private void DrawLine(ref float y, string text)
        {
            if (!layout.CanDrawLine(y))
            {
                return;
            }

            GUI.Label(layout.BuildLineRect(y), text, labelStyle);
            y += layout.LineAdvance;
        }

        private static void AppendTargetReadLines(ICollection<string> lines, Transform target)
        {
            EnemyBrain enemyBrain = target.GetComponentInParent<EnemyBrain>();
            Animator targetAnimator = ResolveTargetAnimator(target, enemyBrain);
            string targetAnimatorClipLine = CombatDebugHudAnimatorClipUtility.BuildAnimatorClipLine(
                "Target Anim",
                targetAnimator);

            if (!string.IsNullOrWhiteSpace(targetAnimatorClipLine))
            {
                lines.Add(targetAnimatorClipLine);
            }

            string bossResponseLine = CombatDebugHudActionFeedbackUtility.BuildBossResponseFeedbackLine(
                enemyBrain,
                compact: true);

            string targetAttackTimingLine = BuildTargetAttackTimingLine(enemyBrain);

            if (!string.IsNullOrWhiteSpace(targetAttackTimingLine))
            {
                lines.Add(targetAttackTimingLine);
            }

            if (!string.IsNullOrWhiteSpace(bossResponseLine))
            {
                lines.Add(bossResponseLine);
            }

            lines.Add($"Lock Target: {target.name}");

            HealthComponent targetHealth = target.GetComponentInParent<HealthComponent>();

            if (targetHealth != null)
            {
                lines.Add($"Target HP: {targetHealth.CurrentValue:0}/{targetHealth.MaxValue:0}");
            }
        }

        private static Texture2D CreatePanelBackground(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void ResolveReferences()
        {
            playerCharacter = SceneRuntimeReferenceUtility.ResolvePlayerCharacter(playerCharacter);

            if (playerCharacter == null)
            {
                return;
            }

            lockOnTargetSelector = SceneRuntimeReferenceUtility.ResolveLockOnTargetSelector(lockOnTargetSelector, playerCharacter);
            playerAnimator = playerCharacter.GetComponent<Animator>();

            if (playerAnimator == null)
            {
                playerAnimator = playerCharacter.GetComponentInChildren<Animator>();
            }
        }

        private void EnsureSwordArtHudPresenter()
        {
            if (!ensureSwordArtHud || playerCharacter == null)
            {
                return;
            }

            if (swordArtHudPresenter == null)
            {
                swordArtHudPresenter = Object.FindFirstObjectByType<SwordArtHudPresenter>();
            }

            if (swordArtHudPresenter == null)
            {
                GameObject hudObject = new GameObject("SwordArtHUD");
                swordArtHudPresenter = hudObject.AddComponent<SwordArtHudPresenter>();
            }

            swordArtHudPresenter.Configure(playerCharacter);
        }

        private static Animator ResolveTargetAnimator(Transform target, EnemyBrain enemyBrain)
        {
            if (target == null)
            {
                return null;
            }

            Animator animator = target.GetComponentInParent<Animator>();

            if (animator != null)
            {
                return animator;
            }

            if (enemyBrain != null)
            {
                animator = enemyBrain.GetComponent<Animator>();

                if (animator != null)
                {
                    return animator;
                }

                return enemyBrain.GetComponentInChildren<Animator>();
            }

            return target.GetComponentInChildren<Animator>();
        }

        private static bool HasTargetAttackTimingLine(Transform target)
        {
            EnemyBrain enemyBrain = target != null ? target.GetComponentInParent<EnemyBrain>() : null;
            return !string.IsNullOrWhiteSpace(BuildTargetAttackTimingLine(enemyBrain));
        }

        private static string BuildTargetAttackTimingLine(EnemyBrain enemyBrain)
        {
            if (enemyBrain == null
                || enemyBrain.StateMachine == null
                || enemyBrain.StateMachine.CurrentState is not EnemyAttackState attackState)
            {
                return string.Empty;
            }

            return CombatDebugHudAttackTimingUtility.BuildTargetAttackTimingLine(
                attackState.CurrentAttackDefinition,
                attackState.PresentationPhase,
                attackState.PresentationProgress,
                compact: true);
        }
    }
}
