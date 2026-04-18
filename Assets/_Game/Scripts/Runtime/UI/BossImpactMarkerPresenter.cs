using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossImpactMarkerPresenter : MonoBehaviour
    {
        public enum MarkerShape
        {
            None = 0,
            ImpactCircle = 1,
            AttackLane = 2
        }

        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private float groundOffset = 0.09f;
        [SerializeField] private float minimumLifetimeSeconds = 0.24f;
        [SerializeField] private float pulseAmplitude = 0.08f;
        [SerializeField] private float pulseSpeed = 8f;
        [SerializeField] private Color markerColor = new Color(0.98f, 0.28f, 0.2f, 1f);

        private GameObject markerVisual;
        private Renderer markerRenderer;
        private Material markerMaterial;
        private GameObject currentVisualTemplate;
        private Material currentMaterialTemplate;
        private string lastStateName = string.Empty;
        private float visibleTimer;
        private float pulseTime;
        private float currentRadius;
        private float currentLength;
        private Vector3 currentPosition;
        private Vector3 currentDirection = Vector3.forward;
        private MarkerShape currentShape;

        public bool IsVisible => markerVisual != null && markerVisual.activeSelf;

        public MarkerShape CurrentShape => currentShape;

        public float CurrentRadius => currentRadius;

        public float CurrentLength => currentLength;

        public Vector3 CurrentPosition => currentPosition;

        public Vector3 CurrentDirection => currentDirection;

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
            HideMarker();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            BossTelegraphVisualUtility.DestroyVisualAndMaterial(
                ref markerVisual,
                ref markerRenderer,
                ref currentVisualTemplate,
                ref markerMaterial,
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
                if (currentStateName == nameof(EnemyAttackState))
                {
                    ShowMarker();
                }

                lastStateName = currentStateName;
            }

            if (!IsVisible)
            {
                return;
            }

            visibleTimer = Mathf.Max(0f, visibleTimer - Mathf.Max(0f, deltaTime));

            if (visibleTimer <= 0f)
            {
                HideMarker();
                return;
            }

            pulseTime += Mathf.Max(0f, deltaTime);
            ApplyVisualScale(1f + Mathf.Sin(pulseTime * pulseSpeed) * pulseAmplitude);
        }

        private void ShowMarker()
        {
            EnsureVisual();

            BossImpactMarkerPlan plan = BossImpactMarkerPlanner.Build(
                bossEnemy,
                groundOffset,
                minimumLifetimeSeconds);
            currentShape = plan.Shape;
            currentDirection = plan.Direction;
            currentRadius = plan.Radius;
            currentLength = plan.Length;
            currentPosition = plan.Position;
            visibleTimer = plan.LifetimeSeconds;
            pulseTime = 0f;
            ApplyMarkerMaterial();
            markerVisual.SetActive(true);
            ApplyVisualScale(1f);
        }

        private void HideMarker()
        {
            currentShape = MarkerShape.None;
            currentRadius = 0f;
            currentLength = 0f;
            visibleTimer = 0f;
            pulseTime = 0f;
            currentDirection = Vector3.forward;

            if (markerVisual != null)
            {
                markerVisual.SetActive(false);
            }
        }

        private void ResetRuntimeState()
        {
            HideMarker();
            currentPosition = Vector3.zero;
            lastStateName = string.Empty;
        }

        private void ApplyVisualScale(float scaleMultiplier)
        {
            if (markerVisual == null)
            {
                return;
            }

            markerVisual.transform.position = currentPosition;

            if (currentShape == MarkerShape.AttackLane)
            {
                float width = Mathf.Max(0.3f, currentRadius * 2f * scaleMultiplier);
                markerVisual.transform.rotation = Quaternion.LookRotation(currentDirection, Vector3.up);
                markerVisual.transform.localScale = new Vector3(width, 0.025f, Mathf.Max(width, currentLength));
                return;
            }

            float diameter = Mathf.Max(0.3f, currentRadius * 2f * scaleMultiplier);
            markerVisual.transform.rotation = Quaternion.identity;
            markerVisual.transform.localScale = new Vector3(diameter, 0.025f, diameter);
        }

        private void ApplyMarkerMaterial()
        {
            BossTelegraphVisualUtility.ApplyRuntimeMaterial(
                markerRenderer,
                ResolveMarkerMaterialTemplate(),
                ResolveMarkerColor(),
                ref markerMaterial,
                ref currentMaterialTemplate);
        }

        private Color ResolveMarkerColor()
        {
            return telegraphStyle != null ? telegraphStyle.ImpactMarkerColor : markerColor;
        }

        private Material ResolveMarkerMaterialTemplate()
        {
            return telegraphStyle != null ? telegraphStyle.ImpactMarkerMaterial : null;
        }

        private GameObject ResolveMarkerVisualTemplate()
        {
            return telegraphStyle != null ? telegraphStyle.ImpactMarkerVisualPrefab : null;
        }

        private void EnsureVisual()
        {
            BossTelegraphVisualUtility.EnsureVisual(
                transform,
                "BossImpactMarkerVisual",
                ResolveMarkerVisualTemplate(),
                ref markerVisual,
                ref markerRenderer,
                ref currentVisualTemplate,
                ref markerMaterial,
                ref currentMaterialTemplate);
        }
    }
}
