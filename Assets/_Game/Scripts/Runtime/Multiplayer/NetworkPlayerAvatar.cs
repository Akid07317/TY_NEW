using System.Collections.Generic;
using CampusRPG.Character;
using CampusRPG.Combat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusRPG.Multiplayer
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerAvatar : NetworkBehaviour
    {
        private const float InputSendIntervalSeconds = 1f / 30f;
        private const float ServerInputHoldSeconds = 0.25f;
        private const float DefaultSmokeAttackIntervalSeconds = 0.75f;
        private const int MaxSmokeAttackCount = 16;
        private const int DefaultMaxHealth = 100;
        public const int NoAttackPresentationCode = 0;
        public const int LightAttackPresentationCode = 1;
        private static int activeAvatarCount;
        private static readonly List<NetworkPlayerAvatar> serverAvatars = new List<NetworkPlayerAvatar>();
        private static Vector2 smokeMoveInput;
        private static float smokeMoveStartedAt;
        private static float smokeMoveDelaySeconds;
        private static float smokeMoveDurationSeconds;
        private static string smokeAttackId = NetworkServerAttackProfile.Light01AttackId;
        private static int smokeAttackDamageRequest;
        private static float smokeAttackDelaySeconds = 3f;
        private static int smokeAttackCount = 1;
        private static float smokeAttackIntervalSeconds = DefaultSmokeAttackIntervalSeconds;

        [SerializeField] private float moveSpeed = 4f;

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

        private float nextInputSendTime;
        private uint inputSequence;
        private uint smokeAttackSequence;
        private int smokeAttacksSent;
        private Vector2 serverMoveInput;
        private uint serverLastInputSequence;
        private uint serverLastAttackSequence;
        private float serverLastInputTime;
        private float nextServerAttackTime;
        private float smokeAttackSendAt;

        public static int ActiveAvatarCount => activeAvatarCount;

        public static Vector2 SmokeMoveInput => smokeMoveInput;

        public static int SmokeAttackDamageRequest => smokeAttackDamageRequest;

        public static string SmokeAttackId => smokeAttackId;

        public static int SmokeAttackCount => smokeAttackCount;

        public static float SmokeAttackIntervalSeconds => smokeAttackIntervalSeconds;

        public int CurrentHealth => replicatedHealth.Value;

        public bool IsDead => replicatedIsDead.Value;

        public uint AttackPresentationSequence => replicatedAttackPresentationSequence.Value;

        public int AttackPresentationCode => replicatedAttackPresentationCode.Value;

        public string CurrentAttackPresentationId =>
            ResolveAttackPresentationId(replicatedAttackPresentationCode.Value);

        public bool TryResolveFormalPlayerTarget(out Transform target)
        {
            PlayerCharacter player = GetComponentInChildren<PlayerCharacter>(true);
            target = player != null ? player.transform : transform;
            return target != null;
        }

        public static bool TryFindServerEnemyAttackTarget(
            Vector3 enemyPosition,
            float maxRange,
            out NetworkPlayerAvatar target)
        {
            target = null;
            float clampedRange = Mathf.Max(0f, maxRange);
            float bestDistance = float.MaxValue;
            ulong bestOwnerClientId = ulong.MaxValue;

            for (int i = 0; i < serverAvatars.Count; i++)
            {
                NetworkPlayerAvatar candidate = serverAvatars[i];

                if (candidate == null
                    || !candidate.IsSpawned
                    || candidate.replicatedIsDead.Value
                    || candidate.replicatedHealth.Value <= 0)
                {
                    continue;
                }

                float distance = Vector3.Distance(enemyPosition, candidate.replicatedPosition.Value);
                if (distance > clampedRange)
                {
                    continue;
                }

                if (distance < bestDistance
                    || (Mathf.Approximately(distance, bestDistance)
                        && candidate.OwnerClientId < bestOwnerClientId))
                {
                    target = candidate;
                    bestDistance = distance;
                    bestOwnerClientId = candidate.OwnerClientId;
                }
            }

            return target != null;
        }

        public static bool TryFindLiveServerAvatarByOwner(
            ulong ownerClientId,
            out NetworkPlayerAvatar target)
        {
            target = null;

            for (int i = 0; i < serverAvatars.Count; i++)
            {
                NetworkPlayerAvatar candidate = serverAvatars[i];

                if (candidate == null
                    || !candidate.IsSpawned
                    || candidate.OwnerClientId != ownerClientId
                    || candidate.replicatedIsDead.Value
                    || candidate.replicatedHealth.Value <= 0)
                {
                    continue;
                }

                target = candidate;
                return true;
            }

            return false;
        }

        public static void ConfigureSmokeMoveInput(Vector2 move)
        {
            ConfigureSmokeMoveInput(move, 0f, 0f);
        }

        public static void ConfigureSmokeMoveInput(Vector2 move, float durationSeconds)
        {
            ConfigureSmokeMoveInput(move, 0f, durationSeconds);
        }

        public static void ConfigureSmokeMoveInput(Vector2 move, float delaySeconds, float durationSeconds)
        {
            smokeMoveInput = Vector2.ClampMagnitude(move, 1f);
            smokeMoveDelaySeconds = Mathf.Max(0f, delaySeconds);
            smokeMoveDurationSeconds = Mathf.Max(0f, durationSeconds);
            smokeMoveStartedAt = Time.unscaledTime;
        }

        public static void ConfigureSmokeAttackRequest(int requestedDamage, float delaySeconds)
        {
            ConfigureSmokeAttackRequest(NetworkServerAttackProfile.Light01AttackId, requestedDamage, delaySeconds);
        }

        public static void ConfigureSmokeAttackRequest(string attackId, int requestedDamage, float delaySeconds)
        {
            ConfigureSmokeAttackRequest(
                attackId,
                requestedDamage,
                delaySeconds,
                1,
                DefaultSmokeAttackIntervalSeconds);
        }

        public static void ConfigureSmokeAttackRequest(
            string attackId,
            int requestedDamage,
            float delaySeconds,
            int attackCount,
            float intervalSeconds)
        {
            smokeAttackDamageRequest = Mathf.Max(0, requestedDamage);
            smokeAttackId = string.IsNullOrWhiteSpace(attackId)
                ? NetworkServerAttackProfile.Light01AttackId
                : attackId;
            smokeAttackDelaySeconds = Mathf.Max(0f, delaySeconds);
            smokeAttackCount = Mathf.Clamp(attackCount, 0, MaxSmokeAttackCount);
            smokeAttackIntervalSeconds = Mathf.Max(0.05f, intervalSeconds);
        }

        public override void OnNetworkSpawn()
        {
            activeAvatarCount++;
            smokeAttacksSent = 0;
            smokeAttackSendAt = Time.unscaledTime + smokeAttackDelaySeconds;

            if (IsOwner && IsClient && !IsServer)
            {
                smokeMoveStartedAt = Time.unscaledTime;
            }

            if (IsServer)
            {
                RegisterServerAvatar(this);
                replicatedPosition.Value = BuildSpawnPosition(OwnerClientId);
                replicatedYaw.Value = BuildSpawnYaw(OwnerClientId);
                replicatedHealth.Value = DefaultMaxHealth;
                replicatedIsDead.Value = false;
                replicatedAttackPresentationSequence.Value = 0u;
                replicatedAttackPresentationCode.Value = NoAttackPresentationCode;
                SyncAuthoritativeFormalHealth(replicatedHealth.Value, replicatedIsDead.Value);
                serverMoveInput = Vector2.zero;
                serverLastInputSequence = 0;
                serverLastAttackSequence = 0;
                serverLastInputTime = Time.unscaledTime;
                nextServerAttackTime = 0f;
            }

            ApplyReplicatedPose();
            gameObject.name = $"NetworkPlayer_{OwnerClientId}";
        }

        public override void OnNetworkDespawn()
        {
            activeAvatarCount = Mathf.Max(0, activeAvatarCount - 1);

            if (IsServer)
            {
                serverAvatars.Remove(this);
            }
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                TickAuthoritativeMovement(Time.deltaTime);
            }

            if (IsOwner && IsClient && !IsServer)
            {
                SendOwnerInputIfReady();
                SendSmokeAttackIfReady();
            }

            ApplyReplicatedPose();
        }

        public static Vector3 BuildSpawnPosition(ulong ownerClientId)
        {
            int pairIndex = ownerClientId == 0 ? 0 : (int)((ownerClientId - 1) / 2);
            float x = ownerClientId % 2 == 0 ? 1f : -1f;
            return new Vector3(x, 0f, pairIndex * 2f);
        }

        public static float BuildSpawnYaw(ulong ownerClientId)
        {
            return ownerClientId % 2 == 0 ? -90f : 90f;
        }

        private void SendOwnerInputIfReady()
        {
            if (replicatedIsDead.Value)
            {
                return;
            }

            if (Time.unscaledTime < nextInputSendTime)
            {
                return;
            }

            Vector2 move = ReadMoveInput();
            inputSequence++;
            SubmitMoveInputServerRpc(Vector2.ClampMagnitude(move, 1f), inputSequence, Time.unscaledTime);
            nextInputSendTime = Time.unscaledTime + InputSendIntervalSeconds;
        }

        private void SendSmokeAttackIfReady()
        {
            if (smokeAttackDamageRequest <= 0 || smokeAttacksSent >= smokeAttackCount)
            {
                return;
            }

            if (Time.unscaledTime < smokeAttackSendAt)
            {
                return;
            }

            smokeAttackSequence++;
            SubmitAttackIntentServerRpc(smokeAttackSequence, smokeAttackId, Time.unscaledTime);
            smokeAttacksSent++;
            smokeAttackSendAt = Time.unscaledTime + smokeAttackIntervalSeconds;
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 move = ReadSmokeMoveInput();

            if (keyboard == null)
            {
                return move;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                move.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                move.y -= 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                move.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                move.x += 1f;
            }

            return Vector2.ClampMagnitude(move, 1f);
        }

        private static Vector2 ReadSmokeMoveInput()
        {
            float smokeMoveElapsed = Time.unscaledTime - smokeMoveStartedAt;

            if (smokeMoveElapsed < smokeMoveDelaySeconds)
            {
                return Vector2.zero;
            }

            if (smokeMoveDurationSeconds > 0f
                && smokeMoveElapsed - smokeMoveDelaySeconds > smokeMoveDurationSeconds)
            {
                return Vector2.zero;
            }

            return smokeMoveInput;
        }

        private void TickAuthoritativeMovement(float deltaTime)
        {
            if (replicatedIsDead.Value)
            {
                serverMoveInput = Vector2.zero;
                return;
            }

            Vector2 clampedMove = ClampMoveInput(serverMoveInput);

            if (Time.unscaledTime - serverLastInputTime > ServerInputHoldSeconds)
            {
                clampedMove = Vector2.zero;
            }

            if (clampedMove.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 delta = new Vector3(clampedMove.x, 0f, clampedMove.y) * (moveSpeed * deltaTime);
            replicatedPosition.Value += delta;
            replicatedYaw.Value = CalculateYaw(clampedMove);
        }

        [ServerRpc]
        private void SubmitMoveInputServerRpc(Vector2 move, uint sequence, float clientTime)
        {
            if (sequence <= serverLastInputSequence)
            {
                return;
            }

            if (replicatedIsDead.Value)
            {
                serverLastInputSequence = sequence;
                return;
            }

            serverLastInputSequence = sequence;
            serverMoveInput = ClampMoveInput(move);
            serverLastInputTime = Time.unscaledTime;
        }

        [ServerRpc]
        private void SubmitAttackIntentServerRpc(uint sequence, string attackId, float clientTime)
        {
            if (sequence <= serverLastAttackSequence)
            {
                Debug.Log(
                    "[MultiplayerCombat] Ignored stale attack intent"
                    + $" owner={OwnerClientId}"
                    + $" sequence={sequence}"
                    + $" lastSequence={serverLastAttackSequence}");
                return;
            }

            if (replicatedIsDead.Value)
            {
                serverLastAttackSequence = sequence;
                Debug.Log(
                    "[MultiplayerCombat] Ignored attack intent from dead avatar"
                    + $" owner={OwnerClientId}"
                    + $" sequence={sequence}");
                return;
            }

            string requestedAttackId = attackId ?? string.Empty;
            if (!NetworkServerAttackProfile.TryResolve(requestedAttackId, out NetworkServerAttackProfile attackProfile))
            {
                serverLastAttackSequence = sequence;
                Debug.Log(
                    "[MultiplayerCombat] Ignored unknown attack intent"
                    + $" owner={OwnerClientId}"
                    + $" sequence={sequence}"
                    + $" attackId={requestedAttackId}");
                return;
            }

            if (Time.unscaledTime < nextServerAttackTime)
            {
                Debug.Log(
                    "[MultiplayerCombat] Ignored attack intent during cooldown"
                    + $" owner={OwnerClientId}"
                    + $" sequence={sequence}");
                return;
            }

            serverLastAttackSequence = sequence;
            nextServerAttackTime = Time.unscaledTime + attackProfile.CooldownSeconds;
            PublishServerAttackPresentation(attackProfile);

            NetworkPlayerAvatar target = FindAttackTarget(attackProfile);
            if (target == null)
            {
                Debug.Log(
                    "[MultiplayerCombat] Attack intent missed"
                    + $" owner={OwnerClientId}"
                    + $" sequence={sequence}"
                    + $" attackId={attackProfile.AttackId}"
                    + $" attackerPosition={FormatVector3(replicatedPosition.Value)}"
                    + $" attackerYaw={replicatedYaw.Value:F1}"
                    + $" serverAvatarCount={serverAvatars.Count}");
                return;
            }

            int previousHealth = target.replicatedHealth.Value;
            int nextHealth = Mathf.Max(0, previousHealth - attackProfile.Damage);
            target.replicatedHealth.Value = nextHealth;
            target.replicatedIsDead.Value = ResolveServerDeathState(nextHealth);
            target.SyncAuthoritativeFormalHealth(nextHealth, target.replicatedIsDead.Value);
            Debug.Log(
                "[MultiplayerCombat] Attack intent hit"
                + $" owner={OwnerClientId}"
                + $" targetOwner={target.OwnerClientId}"
                + $" sequence={sequence}"
                + $" attackId={attackProfile.AttackId}"
                + $" damage={attackProfile.Damage}"
                + $" health={previousHealth}->{nextHealth}"
                + $" targetDead={target.replicatedIsDead.Value}"
                + $" attackerPosition={FormatVector3(replicatedPosition.Value)}"
                + $" targetPosition={FormatVector3(target.replicatedPosition.Value)}");
        }

        private void ApplyReplicatedPose()
        {
            transform.SetPositionAndRotation(
                replicatedPosition.Value,
                Quaternion.Euler(0f, replicatedYaw.Value, 0f));
        }

        public static Vector2 ClampMoveInput(Vector2 move)
        {
            return Vector2.ClampMagnitude(move, 1f);
        }

        public static int ResolveServerAttackDamage(int requestedDamage)
        {
            return ResolveServerAttackDamage(NetworkServerAttackProfile.Light01AttackId, requestedDamage);
        }

        public static int ResolveServerAttackDamage(string attackId, int requestedDamage)
        {
            return NetworkServerAttackProfile.ResolveServerDamage(attackId, requestedDamage);
        }

        public static bool ResolveServerDeathState(int health)
        {
            return health <= 0;
        }

        public bool ApplyServerEnemyDamage(
            int damage,
            out int previousHealth,
            out int nextHealth,
            out bool nextDead)
        {
            previousHealth = replicatedHealth.Value;
            nextHealth = previousHealth;
            nextDead = replicatedIsDead.Value;

            if (!IsServer
                || !IsSpawned
                || damage <= 0
                || replicatedIsDead.Value
                || replicatedHealth.Value <= 0)
            {
                return false;
            }

            nextHealth = Mathf.Max(0, previousHealth - damage);
            nextDead = ResolveServerDeathState(nextHealth);
            bool changed = nextHealth != previousHealth || nextDead != replicatedIsDead.Value;
            replicatedHealth.Value = nextHealth;
            replicatedIsDead.Value = nextDead;
            SyncAuthoritativeFormalHealth(nextHealth, nextDead);
            return changed;
        }

        public static bool ApplyAuthoritativeFormalHealth(
            HealthComponent health,
            int authoritativeHealth,
            bool authoritativeDead)
        {
            if (health == null)
            {
                return false;
            }

            float nextHealth = authoritativeDead ? 0f : Mathf.Max(0, authoritativeHealth);
            if (Mathf.Approximately(health.CurrentValue, nextHealth))
            {
                return false;
            }

            health.SetCurrent(nextHealth);
            return true;
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

        public static bool TryResolveAttackPresentationRequest(
            int attackPresentationCode,
            out PlayerAttackRequest request)
        {
            if (attackPresentationCode == LightAttackPresentationCode)
            {
                request = PlayerAttackRequest.Light;
                return true;
            }

            request = default;
            return false;
        }

        public static bool IsTargetInsideAttackArc(
            Vector3 attackerPosition,
            float attackerYaw,
            Vector3 targetPosition)
        {
            return IsTargetInsideAttackArc(attackerPosition, attackerYaw, targetPosition, NetworkServerAttackProfile.Light01);
        }

        public static bool IsTargetInsideAttackArc(
            Vector3 attackerPosition,
            float attackerYaw,
            Vector3 targetPosition,
            NetworkServerAttackProfile attackProfile)
        {
            Vector3 offset = targetPosition - attackerPosition;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance > attackProfile.Range)
            {
                return false;
            }

            if (distance <= 0.001f)
            {
                return true;
            }

            Vector3 forward = Quaternion.Euler(0f, attackerYaw, 0f) * Vector3.forward;
            float angle = Vector3.Angle(forward, offset.normalized);
            return angle <= attackProfile.HalfAngleDegrees;
        }

        private static float CalculateYaw(Vector2 move)
        {
            return Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg;
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"{value.x:F2},{value.y:F2},{value.z:F2}";
        }

        private void PublishServerAttackPresentation(NetworkServerAttackProfile attackProfile)
        {
            int attackPresentationCode = ResolveAttackPresentationCode(attackProfile.AttackId);
            if (attackPresentationCode == NoAttackPresentationCode)
            {
                return;
            }

            replicatedAttackPresentationCode.Value = attackPresentationCode;
            replicatedAttackPresentationSequence.Value = replicatedAttackPresentationSequence.Value == uint.MaxValue
                ? 1u
                : replicatedAttackPresentationSequence.Value + 1u;
        }

        private bool SyncAuthoritativeFormalHealth(int authoritativeHealth, bool authoritativeDead)
        {
            HealthComponent health = GetComponentInChildren<HealthComponent>(true);
            return ApplyAuthoritativeFormalHealth(health, authoritativeHealth, authoritativeDead);
        }

        private static void RegisterServerAvatar(NetworkPlayerAvatar avatar)
        {
            if (serverAvatars.Contains(avatar))
            {
                return;
            }

            serverAvatars.Add(avatar);
        }

        private NetworkPlayerAvatar FindAttackTarget(NetworkServerAttackProfile attackProfile)
        {
            NetworkPlayerAvatar bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < serverAvatars.Count; i++)
            {
                NetworkPlayerAvatar candidate = serverAvatars[i];

                if (candidate == null
                    || candidate == this
                    || !candidate.IsSpawned
                    || candidate.replicatedIsDead.Value
                    || candidate.replicatedHealth.Value <= 0)
                {
                    continue;
                }

                if (!IsTargetInsideAttackArc(
                    replicatedPosition.Value,
                    replicatedYaw.Value,
                    candidate.replicatedPosition.Value,
                    attackProfile))
                {
                    continue;
                }

                float distance = Vector3.Distance(replicatedPosition.Value, candidate.replicatedPosition.Value);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }
    }
}
