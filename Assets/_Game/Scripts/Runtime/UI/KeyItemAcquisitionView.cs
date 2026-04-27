using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct KeyItemAcquisitionPlan
    {
        public KeyItemAcquisitionPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static KeyItemAcquisitionPlan Hidden => new KeyItemAcquisitionPlan(string.Empty, string.Empty, false);
    }

    public static class KeyItemAcquisitionPlanner
    {
        public static KeyItemAcquisitionPlan Build(string keyItemId, bool chapterCompleted)
        {
            if (chapterCompleted && keyItemId == Chapter01Ids.KeyItems.RitualCore)
            {
                return KeyItemAcquisitionPlan.Hidden;
            }

            return Build(keyItemId);
        }

        public static KeyItemAcquisitionPlan Build(string keyItemId)
        {
            switch (keyItemId)
            {
                case Chapter01Ids.KeyItems.GateSigil:
                    return new KeyItemAcquisitionPlan(
                        "Gate Sigil Recovered",
                        "The boss gate is open. Push forward into the gatekeeper arena.",
                        true);
                case Chapter01Ids.KeyItems.SideRouteCache:
                    return new KeyItemAcquisitionPlan(
                        "Side Route Cache Recovered",
                        "Optional cache secured. Use the shortcut return or push toward the boss gate.",
                        true);
                case Chapter01Ids.KeyItems.RitualCore:
                    return new KeyItemAcquisitionPlan(
                        "Ritual Core Recovered",
                        "Chapter target secured. This chapter is now marked complete.",
                        true);
                default:
                    return string.IsNullOrWhiteSpace(keyItemId)
                        ? KeyItemAcquisitionPlan.Hidden
                        : new KeyItemAcquisitionPlan(
                            "Key Item Recovered",
                            keyItemId,
                            true);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class KeyItemAcquisitionView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private float visibleDurationSeconds = 2.2f;

        private bool isSubscribed;
        private bool isVisible;
        private float visibleTimer;
        private string currentTitle = string.Empty;
        private string currentBody = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public bool IsVisible => isVisible;

        public string CurrentTitle => currentTitle;

        public string CurrentBody => currentBody;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            SyncVisibilityForProgressState();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
                panelTexture = null;
            }
        }

        private void Update()
        {
            if (!isVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Time.unscaledDeltaTime);
            isVisible = visibleTimer > 0f;
        }

        private void OnGUI()
        {
            if (!isVisible)
            {
                return;
            }

            EnsureStyles();

            const float width = 368f;
            const float height = 92f;
            Rect panelRect = new Rect(Screen.width - width - 18f, 148f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 32f), currentBody, bodyStyle);
        }

        private void HandleKeyItemAcquired(string keyItemId)
        {
            Show(KeyItemAcquisitionPlanner.Build(keyItemId, chapterProgressService != null && chapterProgressService.IsChapterCompleted));
        }

        private void HandleProgressChanged()
        {
            SyncVisibilityForProgressState();
        }

        private void Subscribe()
        {
            if (isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.KeyItemAcquired += HandleKeyItemAcquired;
            chapterProgressService.ProgressChanged += HandleProgressChanged;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.KeyItemAcquired -= HandleKeyItemAcquired;
            chapterProgressService.ProgressChanged -= HandleProgressChanged;
            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void Show(KeyItemAcquisitionPlan plan)
        {
            if (!plan.IsVisible)
            {
                Hide();
                return;
            }

            currentTitle = plan.Title;
            currentBody = plan.Body;
            visibleTimer = Mathf.Max(0.1f, visibleDurationSeconds);
            isVisible = true;
        }

        private void SyncVisibilityForProgressState()
        {
            if (chapterProgressService != null && chapterProgressService.IsChapterCompleted)
            {
                Hide();
            }
        }

        private void Hide()
        {
            visibleTimer = 0f;
            isVisible = false;
            currentTitle = string.Empty;
            currentBody = string.Empty;
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.13f, 0.1f, 0.08f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.98f, 0.86f, 0.57f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
