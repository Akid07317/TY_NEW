using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossGroundTelegraphPresenter : MonoBehaviour
    {
        public enum TelegraphShape
        {
            None = 0,
            GroundCircle = 1,
            AttackLane = 2
        }

        public enum TelegraphMode
        {
            None = 0,
            Engage = 1,
            Attack = 2
        }

        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private float groundOffset = 0.08f;
        [SerializeField] private float engagePulseSpeed = 3.2f;
        [SerializeField] private float attackPulseSpeed = 6.4f;
        [SerializeField] private float pulseAmplitude = 0.06f;
        [SerializeField] private Color engageColor = new Color(0.96f, 0.7f, 0.22f, 1f);
        [SerializeField] private Color attackColor = new Color(0.92f, 0.18f, 0.16f, 1f);

        private GameObject telegraphVisual;
        private Renderer telegraphRenderer;
        private Material telegraphMaterial;
        private GameObject currentVisualTemplate;
        private Material currentMaterialTemplate;
        private string lastStateName = string.Empty;
        private float pulseTime;
        private float currentRadius;
        private float currentLength;
        private Vector3 currentPosition;
        private Vector3 currentDirection = Vector3.forward;
        private TelegraphShape currentShape;
        private TelegraphMode currentMode;

        public bool IsVisible => telegraphVisual != null && telegraphVisual.activeSelf;

        public float CurrentRadius => currentRadius;

        public float CurrentLength => currentLength;

        public Vector3 CurrentPosition => currentPosition;

        public Vector3 CurrentDirection => currentDirection;

        public TelegraphShape CurrentShape => currentShape;

        public TelegraphMode CurrentMode => currentMode;

        public void Configure(EnemyBrain configuredBossEnemy, BossTelegraphStyleSO configuredTelegraphStyle = null)
        {
            bossEnemy = configuredBossEnemy;

            if (configuredTelegraphStyle != null || telegraphStyle == null)
            {
                telegraphStyle = configuredTelegraphStyle;
            }
        }

        private void Awake()
        {
            EnsureVisual();
            HideTelegraph();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            BossTelegraphVisualUtility.DestroyVisualAndMaterial(
                ref telegraphVisual,
                ref telegraphRenderer,
                ref currentVisualTemplate,
                ref telegraphMaterial,
                ref currentMaterialTemplate);
        }

        private void Tick(float deltaTime)
        {
            if (!BossPresentationRules.IsBossEligible(bossEnemy))
            {
                ResetRuntimeState();
                return;
            }

            EnsureVisual();

            string currentStateName = bossEnemy.StateMachine != null ? bossEnemy.StateMachine.CurrentStateName : string.Empty;

            if (currentStateName != lastStateName)
            {
                if (currentStateName == nameof(EnemyEngageState))
                {
                    ShowTelegraph(TelegraphMode.Engage, engageColor);
                }
                else if (currentStateName == nameof(EnemyAttackState))
                {
                    ShowTelegraph(TelegraphMode.Attack, attackColor);
                }
                else
                {
                    HideTelegraph();
                }

                lastStateName = currentStateName;
            }

            if (!IsVisible)
            {
                return;
            }

            pulseTime += Mathf.Max(0f, deltaTime);
            float pulseSpeed = currentMode == TelegraphMode.Attack ? attackPulseSpeed : engagePulseSpeed;
            float scaleMultiplier = 1f + Mathf.Sin(pulseTime * pulseSpeed) * pulseAmplitude;
            currentDirection = BossGroundTelegraphPlanner.ResolveDirection(bossEnemy, transform, currentShape);
            currentPosition = BossGroundTelegraphPlanner.ResolvePosition(
                bossEnemy,
                transform,
                groundOffset,
                currentShape,
                currentDirection,
                currentLength);
            ApplyVisualScale(scaleMultiplier);
        }

        private void ShowTelegraph(TelegraphMode mode, Color fallbackColor)
        {
            EnsureVisual();

            BossGroundTelegraphPlan plan = BossGroundTelegraphPlanner.Build(
                bossEnemy,
                transform,
                groundOffset,
                mode);
            currentMode = mode;
            currentShape = plan.Shape;
            currentRadius = plan.Radius;
            currentLength = plan.Length;
            currentDirection = plan.Direction;
            currentPosition = plan.Position;
            pulseTime = 0f;
            ApplyTelegraphMaterial(mode, fallbackColor);
            telegraphVisual.SetActive(true);
            ApplyVisualScale(1f);
        }

        private void HideTelegraph()
        {
            currentMode = TelegraphMode.None;
            currentShape = TelegraphShape.None;
            currentRadius = 0f;
            currentLength = 0f;
            pulseTime = 0f;
            currentPosition = Vector3.zero;
            currentDirection = Vector3.forward;

            if (telegraphVisual != null)
            {
                telegraphVisual.SetActive(false);
            }
        }

        private void ResetRuntimeState()
        {
            HideTelegraph();
            lastStateName = string.Empty;
        }

        private void ApplyVisualScale(float scaleMultiplier)
        {
            if (telegraphVisual == null)
            {
                return;
            }

            telegraphVisual.transform.position = currentPosition;

            if (currentShape == TelegraphShape.AttackLane)
            {
                float width = Mathf.Max(0.6f, currentRadius * 2f * scaleMultiplier);
                telegraphVisual.transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
                telegraphVisual.transform.localScale = new Vector3(width, 0.03f, Mathf.Max(width, currentLength));
                return;
            }

            float diameter = Mathf.Max(0.6f, currentRadius * 2f * scaleMultiplier);
            telegraphVisual.transform.rotation = Quaternion.identity;
            telegraphVisual.transform.localScale = new Vector3(diameter, 0.03f, diameter);
        }

        private void ApplyTelegraphMaterial(TelegraphMode mode, Color fallbackColor)
        {
            BossTelegraphVisualUtility.ApplyRuntimeMaterial(
                telegraphRenderer,
                ResolveTelegraphMaterialTemplate(mode),
                ResolveTelegraphColor(mode, fallbackColor),
                ref telegraphMaterial,
                ref currentMaterialTemplate);
        }

        private Color ResolveTelegraphColor(TelegraphMode mode, Color fallbackColor)
        {
            if (telegraphStyle == null)
            {
                return fallbackColor;
            }

            return mode == TelegraphMode.Engage
                ? telegraphStyle.EngageTelegraphColor
                : telegraphStyle.AttackTelegraphColor;
        }

        private Material ResolveTelegraphMaterialTemplate(TelegraphMode mode)
        {
            if (telegraphStyle == null)
            {
                return null;
            }

            return mode == TelegraphMode.Engage
                ? telegraphStyle.EngageTelegraphMaterial
                : telegraphStyle.AttackTelegraphMaterial;
        }

        private GameObject ResolveTelegraphVisualTemplate()
        {
            return telegraphStyle != null ? telegraphStyle.GroundTelegraphVisualPrefab : null;
        }

        private void EnsureVisual()
        {
            BossTelegraphVisualUtility.EnsureVisual(
                transform,
                "BossGroundTelegraphVisual",
                ResolveTelegraphVisualTemplate(),
                ref telegraphVisual,
                ref telegraphRenderer,
                ref currentVisualTemplate,
                ref telegraphMaterial,
                ref currentMaterialTemplate);
        }
    }
}
