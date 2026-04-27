using CampusRPG.Combat;
using UnityEngine;

namespace CampusRPG.AI
{
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyVisualPresentationRelay : MonoBehaviour
    {
        [SerializeField] private EnemyBrain enemyBrain;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform accentTransform;
        [SerializeField] private float poseSmoothing = 18f;
        [SerializeField] private float accentSmoothing = 22f;

        private Vector3 defaultVisualLocalPosition;
        private Quaternion defaultVisualLocalRotation = Quaternion.identity;
        private Vector3 defaultVisualLocalScale = Vector3.one;
        private Quaternion defaultAccentLocalRotation = Quaternion.identity;
        private Vector3 defaultAccentLocalScale = Vector3.one;
        private Vector3 lastWorldPosition;
        private float locomotionCycle;
        private bool defaultsCaptured;

        private void Awake()
        {
            EnsureReferences();
            CaptureDefaults();
            lastWorldPosition = transform.position;
        }

        private void OnEnable()
        {
            EnsureReferences();
            CaptureDefaults();
            lastWorldPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (!EnsureReferences())
            {
                return;
            }

            if (!defaultsCaptured)
            {
                CaptureDefaults();
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float moveSpeedNormalized = ResolveMoveSpeedNormalized(deltaTime);
            locomotionCycle += deltaTime * Mathf.Lerp(1.5f, 5.4f, moveSpeedNormalized);

            EnemyAttackPresentationPhase attackPhase = ResolveAttackPhase(
                out float attackProgress,
                out EnemyTargetResponseType targetResponse);
            EnemyArchetypeType archetypeType = enemyBrain != null && enemyBrain.Archetype != null
                ? enemyBrain.Archetype.ArchetypeType
                : EnemyArchetypeType.Melee;
            string currentStateName = stateMachine != null ? stateMachine.CurrentStateName : string.Empty;
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                archetypeType,
                currentStateName,
                moveSpeedNormalized,
                locomotionCycle,
                attackPhase,
                attackProgress,
                targetResponse);

            float poseLerp = ResolveLerpFactor(poseSmoothing, deltaTime);
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                defaultVisualLocalPosition + pose.RootLocalOffset,
                poseLerp);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                defaultVisualLocalRotation * Quaternion.Euler(pose.RootLocalEulerAngles),
                poseLerp);
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                Vector3.Scale(defaultVisualLocalScale, pose.RootLocalScale),
                poseLerp);

            if (accentTransform == null)
            {
                return;
            }

            float accentLerp = ResolveLerpFactor(accentSmoothing, deltaTime);
            accentTransform.localRotation = Quaternion.Slerp(
                accentTransform.localRotation,
                defaultAccentLocalRotation * Quaternion.Euler(pose.AccentLocalEulerAngles),
                accentLerp);
            accentTransform.localScale = Vector3.Lerp(
                accentTransform.localScale,
                Vector3.Scale(defaultAccentLocalScale, pose.AccentLocalScale),
                accentLerp);
        }

        private void OnDisable()
        {
            RestoreImmediate();
        }

        public static Transform FindDefaultVisualRoot(Transform actorRoot)
        {
            return actorRoot != null ? actorRoot.Find("CombatProxyVisualRoot") : null;
        }

        public static Transform FindDefaultAccentTransform(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            string[] candidateNames =
            {
                "MeleeBlade",
                "Staff",
                "FocusOrb",
                "MobileTail",
                "Blade",
                "Guard"
            };

            for (int i = 0; i < candidateNames.Length; i++)
            {
                Transform candidate = root.Find(candidateNames[i]);

                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool EnsureReferences()
        {
            if (enemyBrain == null)
            {
                enemyBrain = GetComponent<EnemyBrain>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }

            if (visualRoot == null)
            {
                visualRoot = FindDefaultVisualRoot(transform);
            }

            if (accentTransform == null)
            {
                accentTransform = FindDefaultAccentTransform(visualRoot);
            }

            return enemyBrain != null && stateMachine != null && visualRoot != null;
        }

        private void CaptureDefaults()
        {
            if (visualRoot == null)
            {
                defaultsCaptured = false;
                return;
            }

            defaultVisualLocalPosition = visualRoot.localPosition;
            defaultVisualLocalRotation = visualRoot.localRotation;
            defaultVisualLocalScale = visualRoot.localScale;

            if (accentTransform != null)
            {
                defaultAccentLocalRotation = accentTransform.localRotation;
                defaultAccentLocalScale = accentTransform.localScale;
            }

            defaultsCaptured = true;
        }

        private float ResolveMoveSpeedNormalized(float deltaTime)
        {
            Vector3 planarDelta = transform.position - lastWorldPosition;
            planarDelta.y = 0f;
            lastWorldPosition = transform.position;

            float speed = planarDelta.magnitude / deltaTime;
            float baselineMoveSpeed = enemyBrain != null && enemyBrain.Archetype != null
                ? Mathf.Max(0.01f, enemyBrain.Archetype.MoveSpeed)
                : 3.5f;
            return Mathf.Clamp01(speed / baselineMoveSpeed);
        }

        private EnemyAttackPresentationPhase ResolveAttackPhase(
            out float attackProgress,
            out EnemyTargetResponseType targetResponse)
        {
            attackProgress = 0f;
            targetResponse = EnemyTargetResponseType.None;

            if (stateMachine == null || stateMachine.CurrentState is not EnemyAttackState attackState)
            {
                return EnemyAttackPresentationPhase.None;
            }

            attackProgress = attackState.PresentationProgress;
            targetResponse = ResolveTargetResponse(attackState.CurrentAttackDefinition);
            return attackState.PresentationPhase;
        }

        private static EnemyTargetResponseType ResolveTargetResponse(AttackDefinitionSO attackDefinition)
        {
            if (attackDefinition == null)
            {
                return EnemyTargetResponseType.None;
            }

            if (attackDefinition.EnemyTargetResponse != EnemyTargetResponseType.None)
            {
                return attackDefinition.EnemyTargetResponse;
            }

            return attackDefinition.BreaksGuard
                ? EnemyTargetResponseType.GuardBreak
                : EnemyTargetResponseType.None;
        }

        private void RestoreImmediate()
        {
            if (!defaultsCaptured || visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = defaultVisualLocalPosition;
            visualRoot.localRotation = defaultVisualLocalRotation;
            visualRoot.localScale = defaultVisualLocalScale;

            if (accentTransform == null)
            {
                return;
            }

            accentTransform.localRotation = defaultAccentLocalRotation;
            accentTransform.localScale = defaultAccentLocalScale;
        }

        private static float ResolveLerpFactor(float smoothing, float deltaTime)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0f, smoothing) * deltaTime);
        }
    }
}
