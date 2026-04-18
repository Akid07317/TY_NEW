using CampusRPG.Composition;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct EncounterClearPlan
    {
        public EncounterClearPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static EncounterClearPlan Hidden => new EncounterClearPlan(string.Empty, string.Empty, false);
    }

    public static class EncounterClearPlanner
    {
        public static EncounterClearPlan Build(string encounterId)
        {
            switch (encounterId)
            {
                case Chapter01Ids.Encounters.EntranceTutorial:
                    return new EncounterClearPlan(
                        "Training Complete",
                        "The route forward is open. Activate CP01 before you leave the entrance.",
                        true);
                case Chapter01Ids.Encounters.Courtyard:
                    return new EncounterClearPlan(
                        "Courtyard Secured",
                        "The school interior is open. Push inside and keep the pressure on.",
                        true);
                case Chapter01Ids.Encounters.Interior:
                    return new EncounterClearPlan(
                        "Seal Broken",
                        "The room is clear. Recover the Gate Sigil and head for the boss gate.",
                        true);
                case Chapter01Ids.Encounters.Gatekeeper:
                    return EncounterClearPlan.Hidden;
                default:
                    return string.IsNullOrWhiteSpace(encounterId)
                        ? EncounterClearPlan.Hidden
                        : new EncounterClearPlan(
                            "Encounter Cleared",
                            encounterId,
                            true);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class EncounterClearView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private float visibleDurationSeconds = 1.9f;

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

            const float width = 390f;
            const float height = 86f;
            Rect panelRect = new Rect(18f, Screen.height - height - 24f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 14f, panelRect.width - 36f, 24f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 26f), currentBody, bodyStyle);
        }

        private void HandleEncounterCleared(string encounterId)
        {
            Show(EncounterClearPlanner.Build(encounterId));
        }

        private void Subscribe()
        {
            if (isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.EncounterCleared += HandleEncounterCleared;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || chapterProgressService == null)
            {
                return;
            }

            chapterProgressService.EncounterCleared -= HandleEncounterCleared;
            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void Show(EncounterClearPlan plan)
        {
            if (!plan.IsVisible)
            {
                return;
            }

            currentTitle = plan.Title;
            currentBody = plan.Body;
            visibleTimer = Mathf.Max(0.1f, visibleDurationSeconds);
            isVisible = true;
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.07f, 0.1f, 0.12f, 0.95f));
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
                titleStyle.normal.textColor = new Color(0.72f, 0.94f, 0.78f);
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
