using CampusRPG.Composition;
using CampusRPG.Interaction;
using CampusRPG.Save;
using UnityEngine;
using UnityEngine.Serialization;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossArenaStatusPresenter : MonoBehaviour
    {
        [SerializeField] private EncounterController bossEncounter;
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private string sealedTitle = "Arena Sealed";
        [SerializeField] private string sealedBody = "Gatekeeper has locked the arena behind you.";
        [SerializeField] private string clearedTitle = "Gatekeeper Down";
        [SerializeField] private string clearedBody = "Walk forward and pick up the Ritual Core to finish the chapter.";
        [FormerlySerializedAs("visibleDurationSeconds")]
        [SerializeField] private float sealedVisibleDurationSeconds = 1.9f;
        [SerializeField] private float clearedVisibleDurationSeconds = 1.05f;

        private bool hasInitialized;
        private bool isVisible;
        private bool lastIsActive;
        private bool lastIsCleared;
        private float visibleTimer;
        private float currentVisibleDurationSeconds;
        private string currentTitle = string.Empty;
        private string currentBody = string.Empty;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public bool IsVisible => isVisible;

        public float RemainingVisibleSeconds => visibleTimer;

        public string CurrentTitle => currentTitle;

        public string CurrentBody => currentBody;

        public float CurrentAlpha => isVisible ? GetCurrentAlpha() : 0f;

        public void Configure(EncounterController configuredBossEncounter)
        {
            bossEncounter = configuredBossEncounter;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
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
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * CurrentAlpha);

            const float width = 460f;
            const float height = 86f;
            Rect panelRect = new Rect((Screen.width - width) * 0.5f, 164f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 12f, panelRect.width - 36f, 28f), currentTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 42f, panelRect.width - 36f, 28f), currentBody, bodyStyle);

            GUI.color = previousColor;
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
                panelTexture = null;
            }
        }

        private void Tick(float deltaTime)
        {
            ResolveReferences();

            if (chapterProgressService != null && chapterProgressService.IsChapterCompleted)
            {
                ResetPresentation();
                return;
            }

            if (bossEncounter == null)
            {
                ResetPresentation();
                return;
            }

            bool isActive = bossEncounter.IsActive;
            bool isCleared = bossEncounter.IsCleared;

            if (!hasInitialized)
            {
                hasInitialized = true;
                lastIsActive = isActive;
                lastIsCleared = isCleared;
            }
            else
            {
                if (!lastIsActive && isActive && !isCleared)
                {
                    ShowMessage(sealedTitle, sealedBody, sealedVisibleDurationSeconds);
                }
                else if (!lastIsCleared && isCleared)
                {
                    ShowMessage(clearedTitle, clearedBody, clearedVisibleDurationSeconds);
                }

                lastIsActive = isActive;
                lastIsCleared = isCleared;
            }

            if (!isVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));
            isVisible = visibleTimer > 0f;
        }

        private void ShowMessage(string title, string body, float visibleDurationSeconds)
        {
            currentTitle = title;
            currentBody = body;
            currentVisibleDurationSeconds = Mathf.Max(0.1f, visibleDurationSeconds);
            visibleTimer = currentVisibleDurationSeconds;
            isVisible = true;
        }

        private void ResetPresentation()
        {
            hasInitialized = false;
            isVisible = false;
            lastIsActive = false;
            lastIsCleared = false;
            visibleTimer = 0f;
            currentVisibleDurationSeconds = 0f;
            currentTitle = string.Empty;
            currentBody = string.Empty;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.1f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = new Color(0.97f, 0.84f, 0.43f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }

        private float GetCurrentAlpha()
        {
            if (currentVisibleDurationSeconds <= Mathf.Epsilon)
            {
                return 0f;
            }

            float fadeWindow = Mathf.Min(0.18f, currentVisibleDurationSeconds * 0.5f);

            if (fadeWindow <= Mathf.Epsilon)
            {
                return 1f;
            }

            float fadeIn = Mathf.Clamp01((currentVisibleDurationSeconds - visibleTimer) / fadeWindow);
            float fadeOut = Mathf.Clamp01(visibleTimer / fadeWindow);
            return Mathf.Min(fadeIn, fadeOut);
        }
    }
}
