using CampusRPG.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRPG.UI
{
    public readonly struct MainMenuPlan
    {
        public MainMenuPlan(
            string statusLine,
            string objectiveHeading,
            string objectiveBody,
            string primaryActionLabel,
            string secondaryActionLabel,
            bool showSecondaryAction)
        {
            StatusLine = statusLine ?? string.Empty;
            ObjectiveHeading = objectiveHeading ?? string.Empty;
            ObjectiveBody = objectiveBody ?? string.Empty;
            PrimaryActionLabel = primaryActionLabel ?? string.Empty;
            SecondaryActionLabel = secondaryActionLabel ?? string.Empty;
            ShowSecondaryAction = showSecondaryAction;
        }

        public string StatusLine { get; }

        public string ObjectiveHeading { get; }

        public string ObjectiveBody { get; }

        public string PrimaryActionLabel { get; }

        public string SecondaryActionLabel { get; }

        public bool ShowSecondaryAction { get; }

        public string ShortcutHintLine => ShowSecondaryAction
            ? $"Enter: {PrimaryActionLabel}   R: {SecondaryActionLabel}   Esc: Quit"
            : $"Enter: {PrimaryActionLabel}   Esc: Quit";
    }

    public static class MainMenuPlanner
    {
        public static MainMenuPlan Build(ChapterSaveData saveData)
        {
            if (saveData == null || saveData.chapterId != Chapter01Ids.Chapter)
            {
                return new MainMenuPlan(
                    "No auto-save found. Start from CP01 / Entrance Tutorial.",
                    "Start Fresh",
                    "Finish the tutorial encounter, activate CP01, then push deeper into the sealed campus.",
                    "Start Chapter 01",
                    string.Empty,
                    false);
            }

            if (saveData.chapterCompleted)
            {
                return new MainMenuPlan(
                    "Latest auto-save: Chapter complete. Load back in to review the ending card, or restart from CP01.",
                    "Chapter Complete",
                    "The Ritual Core is already secured. Load back in to review the ending card, or restart from CP01.",
                    "Review Chapter Complete",
                    "Restart Chapter 01",
                    true);
            }

            (string objectiveHeading, string objectiveBody) = BuildObjectivePreview(saveData);

            return new MainMenuPlan(
                $"Latest auto-save: {DescribeProgress(saveData)}. Continue from the saved checkpoint, or restart from CP01.",
                objectiveHeading,
                objectiveBody,
                "Continue Chapter 01",
                "Restart Chapter 01",
                true);
        }

        private static string DescribeProgress(ChapterSaveData saveData)
        {
            if (!string.IsNullOrWhiteSpace(saveData.checkpointId))
            {
                switch (saveData.checkpointId)
                {
                    case Chapter01Ids.Checkpoints.Start:
                        return "CP01 / Entrance Tutorial";
                    case Chapter01Ids.Checkpoints.Courtyard:
                        return "CP02 / Outdoor Courtyard";
                    case Chapter01Ids.Checkpoints.Interior:
                        return "CP03 / School Interior";
                }
            }

            switch (saveData.currentAreaId)
            {
                case Chapter01Ids.Areas.Entrance:
                    return "Area01 / Entrance Tutorial";
                case Chapter01Ids.Areas.Courtyard:
                    return "Area02 / Outdoor Courtyard";
                case Chapter01Ids.Areas.Interior:
                    return "Area03 / School Interior";
                case Chapter01Ids.Areas.Boss:
                    return "Area04 / Boss Arena";
                default:
                    return "the latest checkpoint";
            }
        }

        private static (string Heading, string Body) BuildObjectivePreview(ChapterSaveData saveData)
        {
            bool gatekeeperCleared = ContainsProgressFlag(saveData.clearedEncounterIds, Chapter01Ids.Encounters.Gatekeeper);
            bool hasGateSigil = ContainsProgressFlag(saveData.keyItemIds, Chapter01Ids.KeyItems.GateSigil);
            string areaId = ResolveEffectiveAreaId(saveData);

            if (gatekeeperCleared)
            {
                return (
                    "Ritual Core Ahead",
                    "The gatekeeper is down. Walk forward and pick up the Ritual Core to finish the chapter.");
            }

            if (hasGateSigil && areaId != Chapter01Ids.Areas.Boss)
            {
                return (
                    "Boss Gate Open",
                    "The Gate Sigil unlocked the boss route. Push forward and challenge the gatekeeper.");
            }

            switch (areaId)
            {
                case Chapter01Ids.Areas.Courtyard:
                    return (
                        "Outdoor Courtyard",
                        "Win the courtyard skirmish, then push into the school interior.");
                case Chapter01Ids.Areas.Interior:
                    return (
                        "School Interior",
                        "Clear the sealed room and recover the Gate Sigil.");
                case Chapter01Ids.Areas.Boss:
                    return (
                        "Boss Arena",
                        "Defeat the Campus Gatekeeper and secure the Ritual Core.");
                case Chapter01Ids.Areas.Entrance:
                    return (
                        "Entrance Tutorial",
                        "Finish the tutorial encounter and activate CP01.");
                default:
                    return (
                        "Next Objective",
                        "Push forward and secure the next checkpoint.");
            }
        }

        private static string ResolveEffectiveAreaId(ChapterSaveData saveData)
        {
            if (!string.IsNullOrWhiteSpace(saveData.currentAreaId))
            {
                return saveData.currentAreaId;
            }

            switch (saveData.checkpointId)
            {
                case Chapter01Ids.Checkpoints.Start:
                    return Chapter01Ids.Areas.Entrance;
                case Chapter01Ids.Checkpoints.Courtyard:
                    return Chapter01Ids.Areas.Courtyard;
                case Chapter01Ids.Checkpoints.Interior:
                    return Chapter01Ids.Areas.Interior;
                default:
                    return string.Empty;
            }
        }

        private static bool ContainsProgressFlag(string[] values, string expectedValue)
        {
            if (values == null || string.IsNullOrWhiteSpace(expectedValue))
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == expectedValue)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SaveService))]
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string ControlHintLine = "WASD Move   Mouse Look   Tab Lock On   LMB/RMB Attack   Left Ctrl Block   Left Shift Dodge   F Interact";

        [SerializeField] private SaveService saveService;
        [SerializeField] private string chapterSceneName = "Chapter01_Combined";
        [SerializeField] private string menuTitle = "Campus Chapter 01";
        [SerializeField] private string menuSubtitle = "Fight through the sealed campus, defeat the gatekeeper, and secure the Ritual Core.";

        private MainMenuPlan currentPlan;
        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusStyle;
        private GUIStyle objectiveHeadingStyle;
        private GUIStyle objectiveBodyStyle;
        private GUIStyle hintStyle;
        private GUIStyle buttonStyle;
        private GUIStyle secondaryButtonStyle;
        private Texture2D overlayTexture;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D secondaryButtonTexture;

        public string CurrentStatusLine => currentPlan.StatusLine;

        public string CurrentPrimaryActionLabel => currentPlan.PrimaryActionLabel;

        public string CurrentSecondaryActionLabel => currentPlan.SecondaryActionLabel;

        public bool ShowsSecondaryAction => currentPlan.ShowSecondaryAction;

        private void Awake()
        {
            ResolveReferences();
            RefreshPlan();
            ApplyMenuState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshPlan();
            ApplyMenuState();
        }

        private void OnDestroy()
        {
            DestroyTexture(ref overlayTexture);
            DestroyTexture(ref panelTexture);
            DestroyTexture(ref buttonTexture);
            DestroyTexture(ref secondaryButtonTexture);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ExecutePrimaryAction();
                return;
            }

            if (currentPlan.ShowSecondaryAction && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                ExecuteSecondaryAction();
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                QuitApplication();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            Rect panelRect = new Rect(
                (Screen.width * 0.5f) - 310f,
                (Screen.height * 0.5f) - 225f,
                620f,
                450f);

            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 30f, panelRect.width - 68f, 42f), menuTitle, titleStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 80f, panelRect.width - 68f, 44f), menuSubtitle, subtitleStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 136f, panelRect.width - 68f, 44f), currentPlan.StatusLine, statusStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 188f, panelRect.width - 68f, 24f), currentPlan.ObjectiveHeading, objectiveHeadingStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 216f, panelRect.width - 68f, 44f), currentPlan.ObjectiveBody, objectiveBodyStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 276f, panelRect.width - 68f, 34f), ControlHintLine, hintStyle);
            GUI.Label(new Rect(panelRect.x + 34f, panelRect.y + 314f, panelRect.width - 68f, 20f), currentPlan.ShortcutHintLine, hintStyle);

            Rect primaryButtonRect = new Rect(panelRect.x + 34f, panelRect.y + 348f, panelRect.width - 68f, 44f);
            if (GUI.Button(primaryButtonRect, currentPlan.PrimaryActionLabel, buttonStyle))
            {
                ExecutePrimaryAction();
                GUIUtility.ExitGUI();
            }

            if (currentPlan.ShowSecondaryAction)
            {
                Rect secondaryButtonRect = new Rect(panelRect.x + 34f, panelRect.y + 400f, panelRect.width - 68f, 34f);
                if (GUI.Button(secondaryButtonRect, currentPlan.SecondaryActionLabel, secondaryButtonStyle))
                {
                    ExecuteSecondaryAction();
                    GUIUtility.ExitGUI();
                }
            }

            Rect quitButtonRect = new Rect(panelRect.x + panelRect.width - 114f, panelRect.y + panelRect.height - 34f, 80f, 22f);
            if (GUI.Button(quitButtonRect, "Quit"))
            {
                QuitApplication();
                GUIUtility.ExitGUI();
            }
        }

        private void ResolveReferences()
        {
            if (saveService == null)
            {
                saveService = GetComponent<SaveService>();
            }
        }

        private void RefreshPlan()
        {
            currentPlan = MainMenuPlanner.Build(TryLoadChapterSave());
        }

        private ChapterSaveData TryLoadChapterSave()
        {
            if (saveService == null || !saveService.TryLoad(out ChapterSaveData data) || data == null)
            {
                return null;
            }

            return data.chapterId == Chapter01Ids.Chapter ? data : null;
        }

        private void ExecutePrimaryAction()
        {
            LoadChapterScene(clearSaveFirst: false);
        }

        private void ExecuteSecondaryAction()
        {
            if (!currentPlan.ShowSecondaryAction)
            {
                return;
            }

            LoadChapterScene(clearSaveFirst: true);
        }

        private void LoadChapterScene(bool clearSaveFirst)
        {
            if (string.IsNullOrWhiteSpace(chapterSceneName))
            {
                Debug.LogWarning("MainMenuView could not load the chapter scene because chapterSceneName is empty.");
                return;
            }

            if (clearSaveFirst && saveService != null)
            {
                saveService.DeleteSave();
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(chapterSceneName, LoadSceneMode.Single);
        }

        private void ApplyMenuState()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            Debug.Log("MainMenuView requested quit while running in the editor.");
#else
            Application.Quit();
#endif
        }

        private void EnsureStyles()
        {
            if (overlayStyle == null)
            {
                overlayTexture = CreateTexture(new Color(0.05f, 0.06f, 0.1f, 1f));
                overlayStyle = new GUIStyle(GUI.skin.box);
                overlayStyle.normal.background = overlayTexture;
                overlayStyle.border = new RectOffset();
            }

            if (panelStyle == null)
            {
                panelTexture = CreateTexture(new Color(0.11f, 0.12f, 0.16f, 0.94f));
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
                panelStyle.border = new RectOffset(10, 10, 10, 10);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 28;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.normal.textColor = new Color(0.96f, 0.94f, 0.88f);
                titleStyle.wordWrap = true;
            }

            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle(GUI.skin.label);
                subtitleStyle.fontSize = 15;
                subtitleStyle.normal.textColor = new Color(0.8f, 0.82f, 0.88f);
                subtitleStyle.wordWrap = true;
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.fontSize = 13;
                statusStyle.normal.textColor = new Color(0.96f, 0.75f, 0.56f);
                statusStyle.wordWrap = true;
            }

            if (objectiveHeadingStyle == null)
            {
                objectiveHeadingStyle = new GUIStyle(GUI.skin.label);
                objectiveHeadingStyle.fontSize = 18;
                objectiveHeadingStyle.fontStyle = FontStyle.Bold;
                objectiveHeadingStyle.normal.textColor = new Color(0.95f, 0.9f, 0.72f);
                objectiveHeadingStyle.wordWrap = true;
            }

            if (objectiveBodyStyle == null)
            {
                objectiveBodyStyle = new GUIStyle(GUI.skin.label);
                objectiveBodyStyle.fontSize = 13;
                objectiveBodyStyle.normal.textColor = new Color(0.88f, 0.9f, 0.93f);
                objectiveBodyStyle.wordWrap = true;
            }

            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label);
                hintStyle.fontSize = 12;
                hintStyle.normal.textColor = new Color(0.67f, 0.73f, 0.8f);
                hintStyle.wordWrap = true;
            }

            if (buttonStyle == null)
            {
                buttonTexture = CreateTexture(new Color(0.29f, 0.41f, 0.58f, 1f));
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.normal.background = buttonTexture;
                buttonStyle.hover.background = buttonTexture;
                buttonStyle.active.background = buttonTexture;
                buttonStyle.fontSize = 15;
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.normal.textColor = Color.white;
                buttonStyle.fixedHeight = 44f;
            }

            if (secondaryButtonStyle == null)
            {
                secondaryButtonTexture = CreateTexture(new Color(0.24f, 0.24f, 0.28f, 1f));
                secondaryButtonStyle = new GUIStyle(GUI.skin.button);
                secondaryButtonStyle.normal.background = secondaryButtonTexture;
                secondaryButtonStyle.hover.background = secondaryButtonTexture;
                secondaryButtonStyle.active.background = secondaryButtonTexture;
                secondaryButtonStyle.fontSize = 12;
                secondaryButtonStyle.normal.textColor = new Color(0.87f, 0.87f, 0.9f);
                secondaryButtonStyle.fixedHeight = 34f;
            }
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            Object.Destroy(texture);
            texture = null;
        }
    }
}
