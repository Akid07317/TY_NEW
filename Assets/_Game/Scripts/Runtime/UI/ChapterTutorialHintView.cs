using CampusRPG.Composition;
using CampusRPG.Input;
using CampusRPG.Save;
using UnityEngine;

namespace CampusRPG.UI
{
    public readonly struct ChapterTutorialHintPlan
    {
        public ChapterTutorialHintPlan(string title, string body, bool isVisible)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            IsVisible = isVisible;
        }

        public string Title { get; }

        public string Body { get; }

        public bool IsVisible { get; }

        public static ChapterTutorialHintPlan Hidden => new ChapterTutorialHintPlan(string.Empty, string.Empty, false);
    }

    public static class ChapterTutorialHintPlanner
    {
        public static ChapterTutorialHintPlan Build(
            string currentAreaId,
            bool tutorialEncounterCleared,
            bool chapterCompleted,
            bool hasMovementInput,
            bool hasLockedOn,
            bool hasAttacked,
            bool hasDefended)
        {
            if (chapterCompleted || tutorialEncounterCleared)
            {
                return ChapterTutorialHintPlan.Hidden;
            }

            bool inEntrance = string.IsNullOrWhiteSpace(currentAreaId)
                || currentAreaId == Chapter01Ids.Areas.Entrance;

            if (!inEntrance)
            {
                return ChapterTutorialHintPlan.Hidden;
            }

            if (!hasMovementInput)
            {
                return new ChapterTutorialHintPlan(
                    "Get Moving",
                    "Use WASD to move and line up your camera before the first clash.",
                    true);
            }

            if (!hasLockedOn)
            {
                return new ChapterTutorialHintPlan(
                    "Lock On",
                    "Press Tab to lock onto the nearest enemy before you commit.",
                    true);
            }

            if (!hasAttacked)
            {
                return new ChapterTutorialHintPlan(
                    "Open the Fight",
                    "Use LMB for quick hits or RMB for a heavier strike.",
                    true);
            }

            if (!hasDefended)
            {
                return new ChapterTutorialHintPlan(
                    "Stay Safe",
                    "Hold Left Ctrl to block or tap Left Shift to dodge through pressure.",
                    true);
            }

            return new ChapterTutorialHintPlan(
                "Finish the Drill",
                "Clear the tutorial enemies, then step through the opened gate.",
                true);
        }
    }

    [DisallowMultipleComponent]
    public sealed class ChapterTutorialHintView : MonoBehaviour
    {
        [SerializeField] private ChapterProgressService chapterProgressService;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private string panelTitle = "Tutorial Hint";

        private bool isSubscribed;
        private bool hasMovementInput;
        private bool hasLockedOn;
        private bool hasAttacked;
        private bool hasDefended;
        private ChapterTutorialHintPlan currentPlan;
        private GUIStyle panelStyle;
        private GUIStyle panelTitleStyle;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private Texture2D panelTexture;

        public bool IsVisible => currentPlan.IsVisible;

        public string CurrentTitle => currentPlan.Title;

        public string CurrentBody => currentPlan.Body;

        private void Awake()
        {
            ResolveReferences();
            RefreshPlan();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshPlan();
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
            if (inputReader == null)
            {
                return;
            }

            if (!hasMovementInput && inputReader.MoveValue.sqrMagnitude > 0.01f)
            {
                MarkMovementInputSeen();
            }

            if (!hasDefended && inputReader.IsBlockHeld)
            {
                MarkDefenseSeen();
            }
        }

        private void OnGUI()
        {
            if (!currentPlan.IsVisible)
            {
                return;
            }

            EnsureStyles();

            const float width = 372f;
            const float height = 118f;
            Rect panelRect = new Rect(Screen.width - width - 18f, 18f, width, height);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            float textX = panelRect.x + 18f;
            GUI.Label(new Rect(textX, panelRect.y + 12f, panelRect.width - 36f, 20f), panelTitle, panelTitleStyle);
            GUI.Label(new Rect(textX, panelRect.y + 34f, panelRect.width - 36f, 24f), currentPlan.Title, titleStyle);
            GUI.Label(new Rect(textX, panelRect.y + 62f, panelRect.width - 36f, 44f), currentPlan.Body, bodyStyle);
        }

        private void HandleProgressChanged()
        {
            RefreshPlan();
        }

        private void HandleLockOnPressed()
        {
            MarkLockOnSeen();
        }

        private void HandleLightAttackPressed()
        {
            MarkAttackSeen();
        }

        private void HandleHeavyAttackPressed()
        {
            MarkAttackSeen();
        }

        private void HandleDodgePressed()
        {
            MarkDefenseSeen();
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged += HandleProgressChanged;
            }

            if (inputReader != null)
            {
                inputReader.LockOnPressed += HandleLockOnPressed;
                inputReader.LightAttackPressed += HandleLightAttackPressed;
                inputReader.HeavyAttackPressed += HandleHeavyAttackPressed;
                inputReader.DodgePressed += HandleDodgePressed;
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (chapterProgressService != null)
            {
                chapterProgressService.ProgressChanged -= HandleProgressChanged;
            }

            if (inputReader != null)
            {
                inputReader.LockOnPressed -= HandleLockOnPressed;
                inputReader.LightAttackPressed -= HandleLightAttackPressed;
                inputReader.HeavyAttackPressed -= HandleHeavyAttackPressed;
                inputReader.DodgePressed -= HandleDodgePressed;
            }

            isSubscribed = false;
        }

        private void ResolveReferences()
        {
            chapterProgressService = SceneRuntimeReferenceUtility.ResolveChapterProgressService(chapterProgressService);
            inputReader = SceneRuntimeReferenceUtility.ResolveInputReader(inputReader);
        }

        private void RefreshPlan()
        {
            if (chapterProgressService == null)
            {
                currentPlan = ChapterTutorialHintPlan.Hidden;
                return;
            }

            currentPlan = ChapterTutorialHintPlanner.Build(
                chapterProgressService.CurrentAreaId,
                chapterProgressService.IsEncounterCleared(Chapter01Ids.Encounters.EntranceTutorial),
                chapterProgressService.IsChapterCompleted,
                hasMovementInput,
                hasLockedOn,
                hasAttacked,
                hasDefended);
        }

        private void MarkMovementInputSeen()
        {
            if (hasMovementInput)
            {
                return;
            }

            hasMovementInput = true;
            RefreshPlan();
        }

        private void MarkLockOnSeen()
        {
            if (hasLockedOn)
            {
                return;
            }

            hasLockedOn = true;
            RefreshPlan();
        }

        private void MarkAttackSeen()
        {
            if (hasAttacked)
            {
                return;
            }

            hasAttacked = true;
            RefreshPlan();
        }

        private void MarkDefenseSeen()
        {
            if (hasDefended)
            {
                return;
            }

            hasDefended = true;
            RefreshPlan();
        }

        private void EnsureStyles()
        {
            if (panelStyle == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.08f, 0.11f, 0.13f, 0.95f));
                panelTexture.Apply();

                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(8, 8, 8, 8);
            }

            if (panelTitleStyle == null)
            {
                panelTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                panelTitleStyle.normal.textColor = new Color(0.77f, 0.89f, 0.92f);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                titleStyle.normal.textColor = new Color(0.98f, 0.88f, 0.57f);
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft
                };
                bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
