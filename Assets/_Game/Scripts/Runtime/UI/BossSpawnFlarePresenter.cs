using CampusRPG.AI;
using UnityEngine;

namespace CampusRPG.UI
{
    [DisallowMultipleComponent]
    public sealed class BossSpawnFlarePresenter : MonoBehaviour
    {
        [SerializeField] private EnemyBrain bossEnemy;
        [SerializeField] private BossTelegraphStyleSO telegraphStyle;
        [SerializeField] private float groundOffset = 0.08f;
        [SerializeField] private float flareDurationSeconds = 0.95f;
        [SerializeField] private float maxHeight = 3.8f;
        [SerializeField] private float maxRadius = 1.2f;
        [SerializeField] private Color flareColor = new Color(1f, 0.73f, 0.28f, 1f);

        private GameObject flareVisual;
        private Renderer flareRenderer;
        private Material flareMaterial;
        private GameObject currentVisualTemplate;
        private Material currentMaterialTemplate;
        private bool wasBossActiveLastFrame;
        private float remainingTime;
        private Vector3 currentBasePosition;

        public bool IsVisible => flareVisual != null && flareVisual.activeSelf;

        public float RemainingVisibleSeconds => remainingTime;

        public Vector3 CurrentBasePosition => currentBasePosition;

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
            HideFlare();
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            BossTelegraphVisualUtility.DestroyVisualAndMaterial(
                ref flareVisual,
                ref flareRenderer,
                ref currentVisualTemplate,
                ref flareMaterial,
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

            if (!wasBossActiveLastFrame)
            {
                ShowFlare();
            }

            wasBossActiveLastFrame = true;

            if (!IsVisible)
            {
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - Mathf.Max(0f, deltaTime));

            if (remainingTime <= 0f)
            {
                HideFlare();
                return;
            }

            UpdateVisual();
        }

        private void ShowFlare()
        {
            ApplyPlan(BossSpawnFlarePlanner.CreateActivationPlan(
                bossEnemy,
                groundOffset,
                flareDurationSeconds,
                maxHeight,
                maxRadius));
            flareVisual.SetActive(true);
            ApplyFlareMaterial();
        }

        private void HideFlare()
        {
            remainingTime = 0f;

            if (flareVisual != null)
            {
                flareVisual.SetActive(false);
            }
        }

        private void ResetRuntimeState()
        {
            HideFlare();
            wasBossActiveLastFrame = false;
            currentBasePosition = Vector3.zero;
        }

        private void UpdateVisual()
        {
            ApplyPlan(BossSpawnFlarePlanner.BuildRuntimePlan(
                bossEnemy,
                groundOffset,
                flareDurationSeconds,
                maxHeight,
                maxRadius,
                remainingTime));
        }

        private void ApplyPlan(BossSpawnFlarePlan plan)
        {
            remainingTime = plan.RemainingTime;
            currentBasePosition = plan.BasePosition;

            if (flareVisual == null)
            {
                return;
            }

            flareVisual.transform.position = plan.VisualPosition;
            flareVisual.transform.localScale = plan.VisualScale;
        }

        private void EnsureVisual()
        {
            BossTelegraphVisualUtility.EnsureVisual(
                transform,
                "BossSpawnFlareVisual",
                ResolveFlareVisualTemplate(),
                ref flareVisual,
                ref flareRenderer,
                ref currentVisualTemplate,
                ref flareMaterial,
                ref currentMaterialTemplate);
        }

        private void ApplyFlareMaterial()
        {
            BossTelegraphVisualUtility.ApplyRuntimeMaterial(
                flareRenderer,
                ResolveFlareMaterialTemplate(),
                ResolveFlareColor(),
                ref flareMaterial,
                ref currentMaterialTemplate);
        }

        private GameObject ResolveFlareVisualTemplate()
        {
            return telegraphStyle != null ? telegraphStyle.SpawnFlareVisualPrefab : null;
        }

        private Material ResolveFlareMaterialTemplate()
        {
            return telegraphStyle != null ? telegraphStyle.SpawnFlareMaterial : null;
        }

        private Color ResolveFlareColor()
        {
            return telegraphStyle != null ? telegraphStyle.SpawnFlareColor : flareColor;
        }

    }
}
