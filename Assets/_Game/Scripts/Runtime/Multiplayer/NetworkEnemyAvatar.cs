using CampusRPG.AI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class NetworkEnemyAvatar : NetworkBehaviour
    {
        private const int DefaultEnemyId = 1;
        private const int DefaultMaxHealth = 50;
        private const float DefaultServerSmokeDeathDelaySeconds = 4f;
        private const float DefaultServerBrainSmokeDeathDelaySeconds = 7f;
        private const float DefaultServerBrainChaseSmokeDeathDelaySeconds = 9f;
        public const int DefaultServerEnemyGameplayTickDeathDelaySeconds = 9;
        private const float DefaultServerEnemyAttackDelaySeconds = 2f;
        private const float DefaultServerEnemyAttackRange = 4f;
        public const int DefaultServerEnemyAttackDamage = 25;
        private const string ServerGameplayTickFallbackAttackId = "ServerGameplayTickFallback";
        private static readonly Vector3 FormalAttackSmokeSpawnPosition = new Vector3(-1f, 0f, 1.25f);
        private static readonly Vector3 FormalBrainChaseSmokeSpawnPosition = new Vector3(-1f, 0f, 5f);
        public const int NoAttackPresentationCode = 0;
        public const int LightAttackPresentationCode = 1;
        private static int activeEnemyCount;
        private static bool serverEnemyAttackSmokeEnabled;
        private static bool serverFormalEnemyAttackSmokeEnabled;
        private static bool serverBrainEnemyAttackSmokeEnabled;
        private static bool serverBrainEnemyChaseAttackSmokeEnabled;
        private static bool serverEnemyGameplayTickEnabled;
        private static float serverEnemyAttackSmokeDelaySeconds = DefaultServerEnemyAttackDelaySeconds;
        private static float serverEnemyGameplayTickDeathDelaySeconds = DefaultServerEnemyGameplayTickDeathDelaySeconds;
        private static int serverEnemyGameplayTickDamage = DefaultServerEnemyAttackDamage;

        private readonly NetworkVariable<int> replicatedEnemyId = new NetworkVariable<int>(
            DefaultEnemyId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<Vector3> replicatedPosition = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> replicatedYaw = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> replicatedHealth = new NetworkVariable<int>(
            DefaultMaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> replicatedIsDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<uint> replicatedAttackPresentationSequence = new NetworkVariable<uint>(
            0u,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> replicatedAttackPresentationCode = new NetworkVariable<int>(
            NoAttackPresentationCode,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool serverSmokeDeathArmed;
        private float serverSmokeDeathAt;
        private bool serverEnemyAttackArmed;
        private bool serverEnemyAttackApplied;
        private float serverEnemyAttackAt;
        private bool serverBrainEnemyAttackDriverEnabled;
        private bool serverEnemyGameplayTickActive;
        private bool serverBrainEnemyAttackWaitingLogged;
        private float serverBrainEnemyAttackNextStatusAt;
        private ulong? serverEnemyLastAttackTargetOwner;
        private bool serverEnemyLastAttackTargetDead;
        private int configuredEnemyId = DefaultEnemyId;
        private Vector3 configuredSpawnPosition;
        private bool hasConfiguredSpawnPosition;
        private EnemyAttackController subscribedBrainAttackController;

        public static int ActiveEnemyCount => activeEnemyCount;

        public int EnemyId => replicatedEnemyId.Value;

        public int CurrentHealth => replicatedHealth.Value;

        public bool IsDead => replicatedIsDead.Value;

        public uint AttackPresentationSequence => replicatedAttackPresentationSequence.Value;

        public int AttackPresentationCode => replicatedAttackPresentationCode.Value;

        public string CurrentAttackPresentationId =>
            ResolveAttackPresentationId(replicatedAttackPresentationCode.Value);

        public static bool ServerEnemyAttackSmokeEnabled => serverEnemyAttackSmokeEnabled;

        public static bool ServerFormalEnemyAttackSmokeEnabled => serverFormalEnemyAttackSmokeEnabled;

        public static bool ServerBrainEnemyAttackSmokeEnabled => serverBrainEnemyAttackSmokeEnabled;

        public static bool ServerBrainEnemyChaseAttackSmokeEnabled => serverBrainEnemyChaseAttackSmokeEnabled;

        public static bool ServerEnemyGameplayTickEnabled => serverEnemyGameplayTickEnabled;

        public static float ServerEnemyAttackSmokeDelaySeconds => serverEnemyAttackSmokeDelaySeconds;

        public static float ServerEnemyGameplayTickDeathDelaySeconds => serverEnemyGameplayTickDeathDelaySeconds;

        public static int ServerEnemyAttackDamage => DefaultServerEnemyAttackDamage;

        public static int ServerEnemyGameplayTickDamage => serverEnemyGameplayTickDamage;

        public static float ServerEnemyAttackRange => DefaultServerEnemyAttackRange;

        public bool ShouldAllowServerFormalEnemyDriver =>
            IsServer
            && !replicatedIsDead.Value
            && (serverEnemyGameplayTickEnabled
                ? serverEnemyGameplayTickActive
                : !serverBrainEnemyAttackSmokeEnabled || serverBrainEnemyAttackDriverEnabled);

        public static void ConfigureServerEnemyAttackSmoke(bool enabled)
        {
            ConfigureServerEnemyAttackSmoke(enabled, DefaultServerEnemyAttackDelaySeconds);
        }

        public static void ConfigureServerEnemyAttackSmoke(bool enabled, float delaySeconds)
        {
            serverEnemyAttackSmokeEnabled = enabled;
            serverEnemyAttackSmokeDelaySeconds = Mathf.Max(0f, delaySeconds);

            if (!enabled)
            {
                serverFormalEnemyAttackSmokeEnabled = false;
                serverBrainEnemyAttackSmokeEnabled = false;
                serverBrainEnemyChaseAttackSmokeEnabled = false;
            }
        }

        public static void ConfigureServerFormalEnemyAttackSmoke(bool enabled)
        {
            serverFormalEnemyAttackSmokeEnabled = enabled;

            if (enabled)
            {
                ConfigureServerEnemyAttackSmoke(true);
            }
        }

        public static void ConfigureServerBrainEnemyAttackSmoke(bool enabled)
        {
            serverBrainEnemyAttackSmokeEnabled = enabled;

            if (enabled)
            {
                ConfigureServerEnemyAttackSmoke(true);
                serverFormalEnemyAttackSmokeEnabled = true;
            }
        }

        public static void ConfigureServerBrainEnemyChaseAttackSmoke(bool enabled)
        {
            serverBrainEnemyChaseAttackSmokeEnabled = enabled;

            if (enabled)
            {
                ConfigureServerBrainEnemyAttackSmoke(true);
            }
        }

        public static void ConfigureServerEnemyGameplayTick(bool enabled)
        {
            serverEnemyGameplayTickEnabled = enabled;
        }

        public static void ConfigureServerEnemyGameplayTickDeathDelay(float delaySeconds)
        {
            serverEnemyGameplayTickDeathDelaySeconds = Mathf.Max(0f, delaySeconds);
        }

        public static void ConfigureServerEnemyGameplayTickDamage(int damage)
        {
            serverEnemyGameplayTickDamage = Mathf.Max(1, damage);
        }

        public override void OnNetworkSpawn()
        {
            activeEnemyCount++;

            if (IsServer)
            {
                ResetServerEnemyState();
            }

            ApplyReplicatedPose();
            gameObject.name = $"NetworkEnemy_{EnemyId}";
        }

        public override void OnNetworkDespawn()
        {
            activeEnemyCount = Mathf.Max(0, activeEnemyCount - 1);
            UnsubscribeServerBrainAttackCommitted();
        }

        private void OnDisable()
        {
            UnsubscribeServerBrainAttackCommitted();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                TickServerSmokeLifecycle();
            }

            ApplyReplicatedPose();
        }

        public static Vector3 BuildSpawnPosition()
        {
            return BuildSpawnPosition(0);
        }

        public static Vector3 BuildSpawnPosition(int spawnIndex)
        {
            int index = Mathf.Max(0, spawnIndex);
            float x = index % 2 == 0 ? 0f : 2f;
            float z = 3f + 2f * (index / 2);
            return new Vector3(x, 0f, z);
        }

        public static Vector3 BuildFormalAttackSmokeSpawnPosition()
        {
            return BuildFormalAttackSmokeSpawnPosition(0);
        }

        public static Vector3 BuildFormalAttackSmokeSpawnPosition(int spawnIndex)
        {
            int index = Mathf.Max(0, spawnIndex);
            float x = index % 2 == 0 ? FormalAttackSmokeSpawnPosition.x : 1f;
            float z = FormalAttackSmokeSpawnPosition.z + 2f * (index / 2);
            return new Vector3(x, 0f, z);
        }

        public static Vector3 BuildFormalBrainChaseSmokeSpawnPosition()
        {
            return BuildFormalBrainChaseSmokeSpawnPosition(0);
        }

        public static Vector3 BuildFormalBrainChaseSmokeSpawnPosition(int spawnIndex)
        {
            int index = Mathf.Max(0, spawnIndex);
            float x = index % 2 == 0 ? FormalBrainChaseSmokeSpawnPosition.x : 1f;
            float z = FormalBrainChaseSmokeSpawnPosition.z + 2f * (index / 2);
            return new Vector3(x, 0f, z);
        }

        public static Vector3 BuildServerSpawnPosition(int spawnIndex)
        {
            if (serverEnemyGameplayTickEnabled || serverBrainEnemyChaseAttackSmokeEnabled)
            {
                return BuildFormalBrainChaseSmokeSpawnPosition(spawnIndex);
            }

            return serverFormalEnemyAttackSmokeEnabled || serverBrainEnemyAttackSmokeEnabled
                ? BuildFormalAttackSmokeSpawnPosition(spawnIndex)
                : BuildSpawnPosition(spawnIndex);
        }

        public void ConfigureServerSpawn(int enemyId, Vector3 spawnPosition)
        {
            configuredEnemyId = Mathf.Max(1, enemyId);
            configuredSpawnPosition = spawnPosition;
            hasConfiguredSpawnPosition = true;
        }

        public static bool ResolveServerDeathState(int health)
        {
            return health <= 0;
        }

        public static bool ShouldAcceptServerBrainAttackCommit(
            bool isServerGameplayTickCommit,
            bool isBrainSmokeCommit,
            bool hasAppliedNetworkEnemyAttack,
            bool enemyIsDead)
        {
            if (enemyIsDead || (!isServerGameplayTickCommit && !isBrainSmokeCommit))
            {
                return false;
            }

            return isServerGameplayTickCommit || !hasAppliedNetworkEnemyAttack;
        }

        private void TickServerSmokeLifecycle()
        {
            int connectedClients = NetworkManager != null && NetworkManager.IsServer
                ? NetworkManager.ConnectedClientsIds.Count
                : 0;

            if (connectedClients <= 0)
            {
                ResetServerEnemyState();
                return;
            }

            if (connectedClients < 2 || replicatedIsDead.Value)
            {
                serverEnemyGameplayTickActive = false;
                return;
            }

            if (serverEnemyGameplayTickEnabled)
            {
                TickServerEnemyGameplayTick(connectedClients);
            }
            else
            {
                TickServerEnemyAttackSmoke(connectedClients);
            }

            if (!serverSmokeDeathArmed)
            {
                float deathDelaySeconds = serverEnemyGameplayTickEnabled
                    ? serverEnemyGameplayTickDeathDelaySeconds
                    : serverBrainEnemyChaseAttackSmokeEnabled
                    ? DefaultServerBrainChaseSmokeDeathDelaySeconds
                    : serverBrainEnemyAttackSmokeEnabled
                        ? DefaultServerBrainSmokeDeathDelaySeconds
                        : DefaultServerSmokeDeathDelaySeconds;
                serverSmokeDeathArmed = true;
                serverSmokeDeathAt = Time.unscaledTime + deathDelaySeconds;
                Debug.Log(
                    "[MultiplayerEnemy] Armed smoke enemy death"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" connectedClients={connectedClients}"
                    + $" delaySeconds={deathDelaySeconds:0.0}");
                return;
            }

            if (Time.unscaledTime < serverSmokeDeathAt)
            {
                return;
            }

            int previousHealth = replicatedHealth.Value;
            replicatedHealth.Value = 0;
            replicatedIsDead.Value = true;
            serverSmokeDeathArmed = false;

            Debug.Log(
                "[MultiplayerEnemy] Smoke enemy death applied"
                + $" enemyId={replicatedEnemyId.Value}"
                + $" health={previousHealth}->0"
                + $" enemyDead={replicatedIsDead.Value}");
        }

        private void TickServerEnemyAttackSmoke(int connectedClients)
        {
            if (!serverEnemyAttackSmokeEnabled || serverEnemyAttackApplied)
            {
                return;
            }

            if (serverBrainEnemyAttackSmokeEnabled)
            {
                TickServerBrainEnemyAttackSmoke(connectedClients);
                return;
            }

            if (!serverEnemyAttackArmed)
            {
                serverEnemyAttackArmed = true;
                serverEnemyAttackAt = Time.unscaledTime + serverEnemyAttackSmokeDelaySeconds;
                Debug.Log(
                    "[MultiplayerEnemy] Armed smoke enemy attack"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" connectedClients={connectedClients}"
                    + $" delaySeconds={serverEnemyAttackSmokeDelaySeconds:0.0}");
                return;
            }

            if (Time.unscaledTime < serverEnemyAttackAt)
            {
                return;
            }

            if (!NetworkPlayerAvatar.TryFindServerEnemyAttackTarget(
                    replicatedPosition.Value,
                    DefaultServerEnemyAttackRange,
                    out NetworkPlayerAvatar target))
            {
                Debug.Log(
                    "[MultiplayerEnemy] Smoke enemy attack waiting for target"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" range={DefaultServerEnemyAttackRange:0.00}");
                return;
            }

            bool applied;
            int previousHealth;
            int nextHealth;
            bool targetDead;

            if (serverFormalEnemyAttackSmokeEnabled)
            {
                applied = TryApplyServerFormalEnemyAttack(
                    target,
                    out previousHealth,
                    out nextHealth,
                    out targetDead);
            }
            else
            {
                applied = target.ApplyServerEnemyDamage(
                    DefaultServerEnemyAttackDamage,
                    out previousHealth,
                    out nextHealth,
                    out targetDead);
            }

            if (!applied)
            {
                return;
            }

            PublishServerAttackPresentation();
            serverEnemyAttackApplied = true;

            Debug.Log(
                (serverFormalEnemyAttackSmokeEnabled
                    ? "[MultiplayerEnemy] Formal smoke enemy attack applied"
                    : "[MultiplayerEnemy] Smoke enemy attack applied")
                + $" enemyId={replicatedEnemyId.Value}"
                + $" targetOwner={target.OwnerClientId}"
                + $" damage={DefaultServerEnemyAttackDamage}"
                + $" health={previousHealth}->{nextHealth}"
                + $" targetDead={targetDead}"
                + $" enemyPosition={FormatVector3(replicatedPosition.Value)}"
                + $" targetPosition={FormatVector3(target.transform.position)}");
        }

        private void ResetServerEnemyState()
        {
            replicatedEnemyId.Value = configuredEnemyId;
            replicatedPosition.Value = hasConfiguredSpawnPosition
                ? configuredSpawnPosition
                : BuildServerSpawnPosition(0);
            replicatedYaw.Value = 180f;
            replicatedHealth.Value = DefaultMaxHealth;
            replicatedIsDead.Value = false;
            replicatedAttackPresentationSequence.Value = 0u;
            replicatedAttackPresentationCode.Value = NoAttackPresentationCode;
            serverSmokeDeathArmed = false;
            serverEnemyAttackArmed = false;
            serverEnemyAttackApplied = false;
            serverBrainEnemyAttackDriverEnabled = !serverBrainEnemyAttackSmokeEnabled;
            serverEnemyGameplayTickActive = false;
            serverBrainEnemyAttackWaitingLogged = false;
            serverBrainEnemyAttackNextStatusAt = 0f;
            serverEnemyLastAttackTargetOwner = null;
            serverEnemyLastAttackTargetDead = false;
        }

        private void TickServerEnemyGameplayTick(int connectedClients)
        {
            EnsureServerBrainAttackSubscription();

            if (!serverEnemyAttackArmed)
            {
                serverEnemyAttackArmed = true;
                serverEnemyAttackAt = Time.unscaledTime + serverEnemyAttackSmokeDelaySeconds;
                serverBrainEnemyAttackDriverEnabled = false;
                serverEnemyGameplayTickActive = false;
                serverBrainEnemyAttackWaitingLogged = false;
                Debug.Log(
                    "[MultiplayerEnemy] Armed server enemy gameplay tick"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" connectedClients={connectedClients}"
                    + $" delaySeconds={serverEnemyAttackSmokeDelaySeconds:0.0}"
                    + $" spawnPosition={FormatVector3(replicatedPosition.Value)}");
                return;
            }

            if (Time.unscaledTime < serverEnemyAttackAt)
            {
                serverEnemyGameplayTickActive = false;
                return;
            }

            serverEnemyGameplayTickActive = true;
            serverBrainEnemyAttackDriverEnabled = true;
            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
            LogServerBrainAttackStatus("[MultiplayerEnemy] Server tick enemy status", true);
            TryCommitServerBrainAttackFromFormalState();

            if (subscribedBrainAttackController == null && !serverBrainEnemyAttackWaitingLogged)
            {
                serverBrainEnemyAttackWaitingLogged = true;
                Debug.Log(
                    "[MultiplayerEnemy] Server tick enemy waiting for formal driver"
                    + $" enemyId={replicatedEnemyId.Value}");
            }
        }

        private void TickServerBrainEnemyAttackSmoke(int connectedClients)
        {
            EnsureServerBrainAttackSubscription();

            if (!serverEnemyAttackArmed)
            {
                serverEnemyAttackArmed = true;
                serverEnemyAttackAt = Time.unscaledTime + serverEnemyAttackSmokeDelaySeconds;
                serverBrainEnemyAttackDriverEnabled = false;
                serverBrainEnemyAttackWaitingLogged = false;
                Debug.Log(
                    (serverBrainEnemyChaseAttackSmokeEnabled
                        ? "[MultiplayerEnemy] Armed brain chase smoke enemy attack"
                        : "[MultiplayerEnemy] Armed brain smoke enemy attack")
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" connectedClients={connectedClients}"
                    + $" delaySeconds={serverEnemyAttackSmokeDelaySeconds:0.0}"
                    + $" spawnPosition={FormatVector3(replicatedPosition.Value)}");
                return;
            }

            if (Time.unscaledTime < serverEnemyAttackAt)
            {
                serverBrainEnemyAttackDriverEnabled = false;
                return;
            }

            serverBrainEnemyAttackDriverEnabled = true;
            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
            LogServerBrainAttackStatus("[MultiplayerEnemy] Brain smoke enemy attack status", false);
            TryCommitServerBrainAttackFromFormalState();

            if (subscribedBrainAttackController == null && !serverBrainEnemyAttackWaitingLogged)
            {
                serverBrainEnemyAttackWaitingLogged = true;
                Debug.Log(
                    "[MultiplayerEnemy] Brain smoke enemy attack waiting for formal driver"
                    + $" enemyId={replicatedEnemyId.Value}");
            }
        }

        private void TryCommitServerBrainAttackFromFormalState()
        {
            bool allowRepeatedServerGameplayTick = serverEnemyGameplayTickEnabled && serverEnemyGameplayTickActive;

            if ((!allowRepeatedServerGameplayTick && serverEnemyAttackApplied)
                || !TryResolveFormalEnemyAttackDriver(
                    out EnemyBrain enemyBrain,
                    out EnemyAttackController attackController)
                || enemyBrain.Archetype == null
                || enemyBrain.CurrentTarget == null
                || enemyBrain.StateMachine == null)
            {
                return;
            }

            if (allowRepeatedServerGameplayTick)
            {
                TryFaceServerBrainAttackTarget(enemyBrain.transform, enemyBrain.CurrentTarget);
            }

            attackController.Tick(Time.deltaTime);
            bool isAttackState = enemyBrain.StateMachine.CurrentState is EnemyAttackState
                || enemyBrain.StateMachine.CurrentStateName == nameof(EnemyAttackState);

            if (!isAttackState)
            {
                if (allowRepeatedServerGameplayTick)
                {
                    TryApplyServerGameplayTickFallbackAttack(enemyBrain, attackController);
                }

                return;
            }

            bool committed = attackController.TryAttack(enemyBrain.CurrentTarget, enemyBrain.Archetype);

            if (!committed
                && allowRepeatedServerGameplayTick
                && TryApplyServerGameplayTickFallbackAttack(enemyBrain, attackController))
            {
                return;
            }

            if (!committed && Time.unscaledTime >= serverBrainEnemyAttackNextStatusAt)
            {
                Debug.Log(
                    "[MultiplayerEnemy] Brain smoke enemy attack waiting for controller commit"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" target={enemyBrain.CurrentTarget.name}"
                    + $" state={enemyBrain.StateMachine.CurrentStateName}");
            }
        }

        private void LogServerBrainAttackStatus(string logPrefix, bool serverTick)
        {
            if (Time.unscaledTime < serverBrainEnemyAttackNextStatusAt
                || !TryResolveFormalEnemyAttackDriver(
                    out EnemyBrain enemyBrain,
                    out EnemyAttackController attackController))
            {
                return;
            }

            bool driverEnabled = enemyBrain.enabled
                && (enemyBrain.Sensing == null || enemyBrain.Sensing.enabled)
                && attackController.enabled;
            NavMeshAgent navMeshAgent = enemyBrain.GetComponent<NavMeshAgent>();
            bool navMeshAgentEnabled = navMeshAgent != null && navMeshAgent.enabled;
            bool navMeshReady = navMeshAgentEnabled && navMeshAgent.isOnNavMesh;

            if (!driverEnabled)
            {
                return;
            }

            serverBrainEnemyAttackNextStatusAt = Time.unscaledTime + 0.5f;
            Transform sensedTarget = enemyBrain.Sensing != null && enemyBrain.Archetype != null
                ? enemyBrain.Sensing.FindTarget(enemyBrain.transform.position, enemyBrain.Archetype.AggroDistance)
                : null;
            Transform currentTarget = enemyBrain.CurrentTarget;
            NetworkPlayerAvatar networkTarget = null;
            Transform formalTarget = null;
            float formalTargetDistance = -1f;
            float currentTargetDistance = currentTarget != null
                ? Vector3.Distance(enemyBrain.transform.position, currentTarget.position)
                : -1f;
            bool canAttackCurrentTarget = currentTarget != null
                && enemyBrain.Archetype != null
                && attackController.CanAttack(enemyBrain.Archetype.AttackCooldown);
            bool currentTargetHasClearShot = currentTarget != null
                && enemyBrain.Archetype != null
                && attackController.HasAttackClearShotForTarget(currentTarget, enemyBrain.Archetype);

            if (NetworkPlayerAvatar.TryFindServerEnemyAttackTarget(
                    replicatedPosition.Value,
                    DefaultServerEnemyAttackRange,
                    out networkTarget)
                && networkTarget.TryResolveFormalPlayerTarget(out formalTarget))
            {
                formalTargetDistance = Vector3.Distance(enemyBrain.transform.position, formalTarget.position);
            }

            Debug.Log(
                logPrefix
                + $" enemyId={replicatedEnemyId.Value}"
                + $" state={enemyBrain.StateMachine?.CurrentStateName ?? string.Empty}"
                + $" currentTarget={currentTarget?.name ?? "<none>"}"
                + $" currentTargetDistance={currentTargetDistance:0.00}"
                + $" canAttackCurrentTarget={canAttackCurrentTarget}"
                + $" currentTargetHasClearShot={currentTargetHasClearShot}"
                + $" sensedTarget={sensedTarget?.name ?? "<none>"}"
                + $" networkTargetOwner={(networkTarget != null ? networkTarget.OwnerClientId.ToString() : "<none>")}"
                + $" formalTarget={formalTarget?.name ?? "<none>"}"
                + $" formalTargetDistance={formalTargetDistance:0.00}"
                + $" brainEnabled={enemyBrain.enabled}"
                + $" sensingEnabled={(enemyBrain.Sensing != null && enemyBrain.Sensing.enabled)}"
                + $" attackControllerEnabled={attackController.enabled}"
                + $" navMeshAgentEnabled={navMeshAgentEnabled}"
                + $" navMeshReady={navMeshReady}"
                + $" serverTick={serverTick}");
        }

        public static bool TryFaceServerBrainAttackTarget(Transform enemyTransform, Transform target)
        {
            if (enemyTransform == null || target == null)
            {
                return false;
            }

            Vector3 flatDirection = target.position - enemyTransform.position;
            flatDirection.y = 0f;

            if (flatDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            enemyTransform.rotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            return true;
        }

        private bool TryApplyServerGameplayTickFallbackAttack(
            EnemyBrain enemyBrain,
            EnemyAttackController attackController)
        {
            if (enemyBrain == null
                || attackController == null
                || enemyBrain.Archetype == null
                || enemyBrain.CurrentTarget == null
                || !attackController.CanAttack(enemyBrain.Archetype.AttackCooldown)
                || !IsFormalTargetInsideServerGameplayTickRange(enemyBrain, attackController))
            {
                return false;
            }

            NetworkPlayerAvatar target = ResolveServerBrainAttackTarget(
                enemyBrain.CurrentTarget,
                preferRetainedServerTickTarget: true);
            if (!ShouldAcceptServerGameplayTickFallbackTarget(
                    target != null,
                    target != null && !target.IsDead && target.CurrentHealth > 0))
            {
                return false;
            }

            attackController.RegisterServerAuthoritativeCommit(enemyBrain.CurrentTarget, enemyBrain.Archetype);

            Debug.Log(
                "[MultiplayerEnemy] Server tick enemy attack committed"
                + $" enemyId={replicatedEnemyId.Value}"
                + $" targetOwner={target.OwnerClientId}"
                + $" targetHealth={target.CurrentHealth}"
                + $" targetDead={target.IsDead}"
                + " formalDamage=0"
                + $" attackId={ServerGameplayTickFallbackAttackId}");

            int damage = ServerEnemyGameplayTickDamage;
            bool applied = target.ApplyServerEnemyDamage(
                damage,
                out int previousHealth,
                out int nextHealth,
                out bool targetDead);

            if (!applied)
            {
                Debug.Log(
                    "[MultiplayerEnemy] Server tick enemy attack skipped network damage"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" targetOwner={target.OwnerClientId}"
                    + $" targetHealth={target.CurrentHealth}"
                    + $" targetDead={target.IsDead}");
                return false;
            }

            PublishServerAttackPresentation();
            serverEnemyAttackApplied = true;

            LogServerTickTargetSelection(target, targetDead, isServerGameplayTickCommit: true);

            Debug.Log(
                "[MultiplayerEnemy] Server tick enemy attack applied"
                + $" enemyId={replicatedEnemyId.Value}"
                + $" targetOwner={target.OwnerClientId}"
                + $" damage={damage}"
                + " formalDamage=0"
                + $" attackId={ServerGameplayTickFallbackAttackId}"
                + $" health={previousHealth}->{nextHealth}"
                + $" targetDead={targetDead}"
                + $" enemyPosition={FormatVector3(replicatedPosition.Value)}"
                + $" targetPosition={FormatVector3(target.transform.position)}"
                + $" formalTargetPosition={FormatVector3(enemyBrain.CurrentTarget.position)}");
            return true;
        }

        public static bool ShouldAcceptServerGameplayTickFallbackTarget(
            bool hasNetworkTarget,
            bool targetAlive)
        {
            return hasNetworkTarget && targetAlive;
        }

        public static bool ShouldPreferRetainedServerTickTarget(
            bool isServerGameplayTickCommit,
            bool hasRetainedTarget,
            bool retainedTargetDead)
        {
            return isServerGameplayTickCommit && hasRetainedTarget && !retainedTargetDead;
        }

        private static bool IsFormalTargetInsideServerGameplayTickRange(
            EnemyBrain enemyBrain,
            EnemyAttackController attackController)
        {
            if (enemyBrain == null
                || attackController == null
                || enemyBrain.Archetype == null
                || enemyBrain.CurrentTarget == null)
            {
                return false;
            }

            float attackRange = attackController.GetAttackRangeForTarget(enemyBrain.CurrentTarget, enemyBrain.Archetype);
            Vector3 flatOffset = enemyBrain.CurrentTarget.position - enemyBrain.transform.position;
            flatOffset.y = 0f;
            float paddedRange = attackRange + 0.08f;
            return flatOffset.sqrMagnitude <= paddedRange * paddedRange;
        }

        private bool EnsureServerBrainAttackSubscription()
        {
            if (!TryResolveFormalEnemyAttackDriver(
                    out EnemyBrain _,
                    out EnemyAttackController attackController))
            {
                return false;
            }

            if (subscribedBrainAttackController == attackController)
            {
                return true;
            }

            UnsubscribeServerBrainAttackCommitted();
            subscribedBrainAttackController = attackController;
            subscribedBrainAttackController.AttackCommitted += HandleServerBrainAttackCommitted;
            return true;
        }

        private void UnsubscribeServerBrainAttackCommitted()
        {
            if (subscribedBrainAttackController == null)
            {
                return;
            }

            subscribedBrainAttackController.AttackCommitted -= HandleServerBrainAttackCommitted;
            subscribedBrainAttackController = null;
        }

        private void HandleServerBrainAttackCommitted(EnemyAttackCommit commit)
        {
            bool isServerGameplayTickCommit = serverEnemyGameplayTickEnabled && serverEnemyGameplayTickActive;
            bool isBrainSmokeCommit = serverBrainEnemyAttackSmokeEnabled
                && serverEnemyAttackArmed
                && Time.unscaledTime >= serverEnemyAttackAt;

            if (!IsServer
                || !ShouldAcceptServerBrainAttackCommit(
                    isServerGameplayTickCommit,
                    isBrainSmokeCommit,
                    serverEnemyAttackApplied,
                    replicatedIsDead.Value))
            {
                return;
            }

            NetworkPlayerAvatar target = ResolveServerBrainAttackTarget(
                commit.Target,
                preferRetainedServerTickTarget: isServerGameplayTickCommit);
            if (target == null)
            {
                Debug.Log(
                    (isServerGameplayTickCommit
                        ? "[MultiplayerEnemy] Server tick enemy attack ignored non-network target"
                        : "[MultiplayerEnemy] Brain smoke enemy attack ignored non-network target")
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" formalTarget={commit.Target?.name ?? "<null>"}");
                return;
            }

            Debug.Log(
                (isServerGameplayTickCommit
                    ? "[MultiplayerEnemy] Server tick enemy attack committed"
                    : "[MultiplayerEnemy] Brain smoke enemy attack committed")
                + $" enemyId={replicatedEnemyId.Value}"
                + $" targetOwner={target.OwnerClientId}"
                + $" targetHealth={target.CurrentHealth}"
                + $" targetDead={target.IsDead}"
                + $" formalDamage={commit.Damage:0.##}"
                + $" attackId={commit.Attack?.AttackId ?? string.Empty}");

            int damage = isServerGameplayTickCommit
                ? ServerEnemyGameplayTickDamage
                : DefaultServerEnemyAttackDamage;
            bool applied = target.ApplyServerEnemyDamage(
                damage,
                out int previousHealth,
                out int nextHealth,
                out bool targetDead);

            if (!applied)
            {
                Debug.Log(
                    (isServerGameplayTickCommit
                        ? "[MultiplayerEnemy] Server tick enemy attack skipped network damage"
                        : "[MultiplayerEnemy] Brain smoke enemy attack skipped network damage")
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" targetOwner={target.OwnerClientId}"
                    + $" targetHealth={target.CurrentHealth}"
                    + $" targetDead={target.IsDead}");
                return;
            }

            PublishServerAttackPresentation();
            serverEnemyAttackApplied = true;

            LogServerTickTargetSelection(target, targetDead, isServerGameplayTickCommit);

            Debug.Log(
                (isServerGameplayTickCommit
                    ? "[MultiplayerEnemy] Server tick enemy attack applied"
                    : "[MultiplayerEnemy] Brain smoke enemy attack applied")
                + $" enemyId={replicatedEnemyId.Value}"
                + $" targetOwner={target.OwnerClientId}"
                + $" damage={damage}"
                + $" formalDamage={commit.Damage:0.##}"
                + $" attackId={commit.Attack?.AttackId ?? string.Empty}"
                + $" health={previousHealth}->{nextHealth}"
                + $" targetDead={targetDead}"
                + $" enemyPosition={FormatVector3(replicatedPosition.Value)}"
                + $" targetPosition={FormatVector3(target.transform.position)}"
                + $" formalTargetPosition={FormatVector3(commit.Target.position)}");
        }

        private void LogServerTickTargetSelection(
            NetworkPlayerAvatar target,
            bool targetDead,
            bool isServerGameplayTickCommit)
        {
            if (!isServerGameplayTickCommit || target == null)
            {
                return;
            }

            if (!serverEnemyLastAttackTargetOwner.HasValue)
            {
                Debug.Log(
                    "[MultiplayerEnemy] Server tick enemy target acquired"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" targetOwner={target.OwnerClientId}"
                    + $" targetDead={targetDead}");
            }
            else if (serverEnemyLastAttackTargetOwner.Value != target.OwnerClientId)
            {
                Debug.Log(
                    "[MultiplayerEnemy] Server tick enemy target switched"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" previousTargetOwner={serverEnemyLastAttackTargetOwner.Value}"
                    + $" previousTargetDead={serverEnemyLastAttackTargetDead}"
                    + $" nextTargetOwner={target.OwnerClientId}"
                    + $" nextTargetDead={targetDead}");
            }

            serverEnemyLastAttackTargetOwner = target.OwnerClientId;
            serverEnemyLastAttackTargetDead = targetDead;
        }

        private NetworkPlayerAvatar ResolveServerBrainAttackTarget(
            Transform formalTarget,
            bool preferRetainedServerTickTarget)
        {
            if (ShouldPreferRetainedServerTickTarget(
                    preferRetainedServerTickTarget,
                    serverEnemyLastAttackTargetOwner.HasValue,
                    serverEnemyLastAttackTargetDead)
                && NetworkPlayerAvatar.TryFindLiveServerAvatarByOwner(
                    serverEnemyLastAttackTargetOwner.Value,
                    out NetworkPlayerAvatar retainedTarget))
            {
                return retainedTarget;
            }

            return ResolveServerBrainAttackTarget(formalTarget);
        }

        private static NetworkPlayerAvatar ResolveServerBrainAttackTarget(Transform formalTarget)
        {
            return formalTarget != null
                ? formalTarget.GetComponentInParent<NetworkPlayerAvatar>()
                : null;
        }

        private bool TryApplyServerFormalEnemyAttack(
            NetworkPlayerAvatar target,
            out int previousHealth,
            out int nextHealth,
            out bool targetDead)
        {
            previousHealth = 0;
            nextHealth = 0;
            targetDead = false;

            if (target == null
                || !TryResolveFormalEnemyAttackDriver(
                    out EnemyBrain enemyBrain,
                    out EnemyAttackController attackController)
                || enemyBrain.Archetype == null
                || !target.TryResolveFormalPlayerTarget(out Transform formalTarget))
            {
                Debug.Log(
                    "[MultiplayerEnemy] Formal smoke enemy attack waiting for formal driver"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" hasTarget={target != null}");
                return false;
            }

            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
            enemyBrain.SetTarget(formalTarget);
            attackController.Tick(Time.deltaTime);

            bool formalAttackCommitted = attackController.TryAttack(formalTarget, enemyBrain.Archetype);
            if (!formalAttackCommitted)
            {
                Debug.Log(
                    "[MultiplayerEnemy] Formal smoke enemy attack waiting for committed TryAttack"
                    + $" enemyId={replicatedEnemyId.Value}"
                    + $" targetOwner={target.OwnerClientId}"
                    + $" enemyPosition={FormatVector3(transform.position)}"
                    + $" targetPosition={FormatVector3(formalTarget.position)}");
                return false;
            }

            return target.ApplyServerEnemyDamage(
                DefaultServerEnemyAttackDamage,
                out previousHealth,
                out nextHealth,
                out targetDead);
        }

        public void CommitServerFormalDriverPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            bool allowFormalDriverPoseCommit = serverEnemyGameplayTickActive
                || serverBrainEnemyAttackSmokeEnabled && serverBrainEnemyAttackDriverEnabled;

            if (!IsServer
                || replicatedIsDead.Value
                || !allowFormalDriverPoseCommit
                || !IsFinite(worldPosition))
            {
                return;
            }

            Vector3 nextPosition = worldPosition;
            nextPosition.y = replicatedPosition.Value.y;
            float nextYaw = worldRotation.eulerAngles.y;

            if (Vector3.Distance(replicatedPosition.Value, nextPosition) <= 0.001f
                && Mathf.Abs(Mathf.DeltaAngle(replicatedYaw.Value, nextYaw)) <= 0.1f)
            {
                return;
            }

            replicatedPosition.Value = nextPosition;
            replicatedYaw.Value = nextYaw;
            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
        }

        private bool TryResolveFormalEnemyAttackDriver(
            out EnemyBrain enemyBrain,
            out EnemyAttackController attackController)
        {
            enemyBrain = GetComponentInChildren<EnemyBrain>(true);
            attackController = enemyBrain != null ? enemyBrain.AttackController : null;

            if (attackController == null)
            {
                attackController = GetComponentInChildren<EnemyAttackController>(true);
            }

            return enemyBrain != null && attackController != null;
        }

        private void ApplyReplicatedPose()
        {
            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
        }

        public static int ResolveAttackPresentationCode(string attackId)
        {
            return string.Equals(
                attackId,
                NetworkServerAttackProfile.Light01AttackId,
                System.StringComparison.OrdinalIgnoreCase)
                ? LightAttackPresentationCode
                : NoAttackPresentationCode;
        }

        public static string ResolveAttackPresentationId(int attackPresentationCode)
        {
            return attackPresentationCode == LightAttackPresentationCode
                ? NetworkServerAttackProfile.Light01AttackId
                : string.Empty;
        }

        private void PublishServerAttackPresentation()
        {
            replicatedAttackPresentationCode.Value = LightAttackPresentationCode;
            replicatedAttackPresentationSequence.Value = replicatedAttackPresentationSequence.Value == uint.MaxValue
                ? 1u
                : replicatedAttackPresentationSequence.Value + 1u;
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"{value.x:F2},{value.y:F2},{value.z:F2}";
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsNaN(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.x)
                && !float.IsInfinity(value.y)
                && !float.IsInfinity(value.z);
        }
    }
}
