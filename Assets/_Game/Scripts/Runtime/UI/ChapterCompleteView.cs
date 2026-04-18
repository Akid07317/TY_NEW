using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct ChapterCompletePlan
    {
        public ChapterCompletePlan(
            string title,
            string body,
            string resultLine,
            string rewardLine,
            string saveStateLine,
            bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            ResultLine = resultLine ?? string.Empty;
            RewardLine = rewardLine ?? string.Empty;
            SaveStateLine = saveStateLine ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public string ResultLine { get; }

        public string RewardLine { get; }

        public string SaveStateLine { get; }

        public bool IsVisible { get; }

        public static ChapterCompletePlan Hidden => new ChapterCompletePlan(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            false);
    }

    public static class ChapterCompletePlanner
    {
        public static ChapterCompletePlan Build(ChapterProgressService chapterProgressService)
        {
            if (chapterProgressService == null || !chapterProgressService.IsChapterCompleted)
            {
                return ChapterCompletePlan.Hidden;
            }

            string chapterId = chapterProgressService.Progression != null
                ? chapterProgressService.Progression.ChapterId
                : string.Empty;
            bool gatekeeperCleared = chapterProgressService.IsEncounterCleared(Chapter01Ids.Encounters.Gatekeeper);
            bool ritualCoreRecovered = chapterProgressService.HasKeyItem(Chapter01Ids.KeyItems.RitualCore);
            string title = chapterId == Chapter01Ids.Chapter
                ? "Chapter 01 Cleared"
                : "Chapter Complete";
            string body = ritualCoreRecovered
                ? "The gatekeeper is down and the Ritual Core is secure."
                : "The final chapter objective is secure.";
            string resultLine = gatekeeperCleared
                ? "Result: Campus Gatekeeper defeated."
                : "Result: Final route resolved.";
            string rewardLine = ritualCoreRecovered
                ? "Reward: Ritual Core recovered."
                : "Reward: Chapter completion item secured.";
            string saveStateLine = !string.IsNullOrWhiteSpace(chapterId)
                ? $"Save state: {chapterId} auto-save updated."
                : "Save state: chapter progress updated.";

            return new ChapterCompletePlan(
                title,
                body,
                resultLine,
                rewardLine,
                saveStateLine,
                true);
        }
    }

    [DisallowMultipleComponent]
    public sealed class ChapterCompleteView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private float completionRevealDelaySeconds = 0.35f;
        [SerializeField] private float completionFadeInDurationSeconds = 0.18f;
        [SerializeField] private float backdropMaxAlpha = 0.28f;

        private bool isSubscribed;
        private bool isVisible;
        private ChapterCompletePlan currentPlan = ChapterCompletePlan.Hidden;
        private float pendingRevealDelaySeconds;
        private float fadeInRemainingSeconds;
        private float currentRevealAlpha;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle detailStyle;
        private Texture2D backdropTexture;
        private Texture2D panelBackground;

        public bool IsVisible => isVisible;

        public string CurrentTitle => currentPlan.Title;

        public string CurrentBody => currentPlan.Body;

        public string CurrentResultLine => currentPlan.ResultLine;

        public string CurrentRewardLine => currentPlan.RewardLine;

        public string CurrentSaveStateLine => currentPlan.SaveStateLine;

        public bool IsRevealPending => pendingRevealDelaySeconds > 0f && currentPlan.IsVisible && !isVisible;

        public float RemainingRevealDelaySeconds => pendingRevealDelaySeconds;

        public bool IsFadeInActive => isVisible && fadeInRemainingSeconds > 0f && currentRevealAlpha < 1f;

        public float CurrentRevealAlpha => isVisible ? currentRevealAlpha : 0f;

        public float CurrentBackdropAlpha => isVisible ? Mathf.Clamp01(currentRevealAlpha) * Mathf.Clamp01(backdropMaxAlpha) : 0f;

        private void Awake()
        {
            ResolveReferences();
            SyncVisibleState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            SyncVisibleState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (backdropTexture != null)
            {
                Destroy(backdropTexture);
                backdropTexture = null;
            }

            if (panelBackground != null)
            {
                Destroy(panelBackground);
                panelBackground = null;
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, previousColor.a * CurrentBackdropAlpha);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backdropTexture, ScaleMode.StretchToFill);
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * CurrentRevealAlpha);

            const float panelWidth = 460f;
            const float panelHeight = 192f;
            float panelRevealOffset = Mathf.Lerp(14f, 0f, CurrentRevealAlpha);
            Rect panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                Mathf.Max(48f, Screen.height * 0.18f) + panelRevealOffset,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, GUIContent.none, panelStyle);

            float textX = panelRect.x + 24f;
            GUI.Label(new Rect(textX, panelRect.y + 18f, panelRect.width - 48f, 36f), currentPlan.Title, titleStyle);
            GUI.Label(new Rect(textX, panelRect.y + 58f, panelRect.width - 48f, 36f), currentPlan.Body, bodyStyle);
            GUI.Label(new Rect(textX, panelRect.y + 102f, panelRect.width - 48f, 24f), currentPlan.ResultLine, detailStyle);
            GUI.Label(new Rect(textX, panelRect.y + 128f, panelRect.width - 48f, 24f), currentPlan.RewardLine, detailStyle);
            GUI.Label(new Rect(textX, panelRect.y + 154f, panelRect.width - 48f, 24f), currentPlan.SaveStateLine, detailStyle);

            GUI.color = previousColor;
        }

        private void HandleProgressChanged()
        {
            SyncVisibleState(preservePendingRevealDelay: true);
        }

        private void HandleChapterCompleted()
        {
            BeginRevealDelay();
        }

        private void Subscribe()
        {
            if (isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.ProgressChanged += HandleProgressChanged;
            chapterProgressService.ChapterCompleted += HandleChapterCompleted;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.ProgressChanged -= HandleProgressChanged;
            chapterProgressService.ChapterCompleted -= HandleChapterCompleted;
            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void SyncVisibleState(bool preservePendingRevealDelay = false)
        {
            currentPlan = ChapterCompletePlanner.Build(chapterProgressService);

            if (!currentPlan.IsVisible)
            {
                HideImmediately();
                return;
            }

            if (preservePendingRevealDelay && pendingRevealDelaySeconds > 0f)
            {
                currentRevealAlpha = 0f;
                isVisible = false;
                return;
            }

            ShowImmediately();
        }

        private void BeginRevealDelay()
        {
            currentPlan = ChapterCompletePlanner.Build(chapterProgressService);

            if (!currentPlan.IsVisible)
            {
                HideImmediately();
                return;
            }

            pendingRevealDelaySeconds = Mathf.Max(0f, completionRevealDelaySeconds);

            if (pendingRevealDelaySeconds > 0f)
            {
                fadeInRemainingSeconds = 0f;
                currentRevealAlpha = 0f;
                isVisible = false;
                return;
            }

            BeginFadeIn();
        }

        private void Tick(float deltaTime)
        {
            float remainingDeltaTime = Mathf.Max(0f, deltaTime);

            if (pendingRevealDelaySeconds > 0f)
            {
                float consumedDelayTime = Mathf.Min(pendingRevealDelaySeconds, remainingDeltaTime);
                pendingRevealDelaySeconds = Mathf.Max(0f, pendingRevealDelaySeconds - remainingDeltaTime);
                remainingDeltaTime = Mathf.Max(0f, remainingDeltaTime - consumedDelayTime);

                if (pendingRevealDelaySeconds <= 0f && currentPlan.IsVisible)
                {
                    BeginFadeIn();
                }
            }

            if (!isVisible)
            {
                return;
            }

            TickFadeIn(remainingDeltaTime);
        }

        private void HideImmediately()
        {
            pendingRevealDelaySeconds = 0f;
            fadeInRemainingSeconds = 0f;
            currentRevealAlpha = 0f;
            isVisible = false;
        }

        private void ShowImmediately()
        {
            pendingRevealDelaySeconds = 0f;
            fadeInRemainingSeconds = 0f;
            currentRevealAlpha = currentPlan.IsVisible ? 1f : 0f;
            isVisible = currentPlan.IsVisible;
        }

        private void BeginFadeIn()
        {
            pendingRevealDelaySeconds = 0f;
            fadeInRemainingSeconds = Mathf.Max(0f, completionFadeInDurationSeconds);
            currentRevealAlpha = fadeInRemainingSeconds <= 0f ? 1f : 0f;
            isVisible = currentPlan.IsVisible;
        }

        private void TickFadeIn(float deltaTime)
        {
            if (fadeInRemainingSeconds <= 0f)
            {
                currentRevealAlpha = currentPlan.IsVisible ? 1f : 0f;
                return;
            }

            float duration = Mathf.Max(0.0001f, completionFadeInDurationSeconds);
            fadeInRemainingSeconds = Mathf.Max(0f, fadeInRemainingSeconds - Mathf.Max(0f, deltaTime));
            currentRevealAlpha = 1f - (fadeInRemainingSeconds / duration);
        }

        private void EnsureStyles()
        {
            if (backdropTexture == null)
            {
                backdropTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                backdropTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
                backdropTexture.Apply();
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
                panelBackground = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelBackground.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.14f, 0.92f));
                panelBackground.Apply();
                panelStyle.normal.background = panelBackground;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.96f, 0.9f, 0.65f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                bodyStyle.normal.textColor = Color.white;
            }

            if (detailStyle == null)
            {
                detailStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                detailStyle.normal.textColor = new Color(0.84f, 0.88f, 0.92f);
            }
        }
    }
}
