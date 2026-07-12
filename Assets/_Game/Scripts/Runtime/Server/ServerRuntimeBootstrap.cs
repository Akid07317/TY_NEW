using System;
using CampusRPG.Multiplayer;
using UnityEngine;

namespace CampusRPG.Server
{
    [DisallowMultipleComponent]
    public sealed class ServerRuntimeBootstrap : MonoBehaviour
    {
        public const int DefaultPort = 7777;
        public const string DefaultBindAddress = "0.0.0.0";
        public const int DefaultHealthPort = 7778;
        public const string DefaultHealthBindAddress = "0.0.0.0";
        public const int DefaultNetworkPort = MultiplayerNetworkSessionSettings.DefaultNetworkPort;
        public const string DefaultNetworkBindAddress = MultiplayerNetworkSessionSettings.DefaultListenAddress;
        public const string DefaultNetworkPlayerPrefabResourcePath = MultiplayerNetworkSessionSettings.DefaultPlayerPrefabResourcePath;
        public const int DefaultTargetFrameRate = 30;
        public const int DefaultTickRate = 30;
        public const int DefaultLogIntervalSeconds = 10;
        public const int DefaultMaxPlayers = ServerGameConnectionService.DefaultMaxPlayers;
        public const string DefaultRoomId = ServerGameConnectionService.DefaultRoomId;
        public const int DefaultNetworkEnemyCount = MultiplayerNetworkSessionSettings.DefaultNetworkEnemyCount;
        public const int DefaultNetworkEnemyServerTickDeathDelaySeconds =
            NetworkEnemyAvatar.DefaultServerEnemyGameplayTickDeathDelaySeconds;
        public const int DefaultNetworkEnemyServerTickDamage = NetworkEnemyAvatar.DefaultServerEnemyAttackDamage;

        [SerializeField] private int port = DefaultPort;
        [SerializeField] private bool gameplayServerEnabled = true;
        [SerializeField] private string bindAddress = DefaultBindAddress;
        [SerializeField] private int maxPlayers = DefaultMaxPlayers;
        [SerializeField] private string roomId = DefaultRoomId;
        [SerializeField] private bool networkServerEnabled = true;
        [SerializeField] private string networkBindAddress = DefaultNetworkBindAddress;
        [SerializeField] private int networkPort = DefaultNetworkPort;
        [SerializeField] private string networkPlayerPrefabResourcePath = DefaultNetworkPlayerPrefabResourcePath;
        [SerializeField] private GameObject networkPlayerPrefab;
        [SerializeField] private bool healthServerEnabled = true;
        [SerializeField] private string healthBindAddress = DefaultHealthBindAddress;
        [SerializeField] private int healthPort = DefaultHealthPort;
        [SerializeField] private int targetFrameRate = DefaultTargetFrameRate;
        [SerializeField] private int tickRate = DefaultTickRate;
        [SerializeField] private int logIntervalSeconds = DefaultLogIntervalSeconds;
        [SerializeField] private int networkEnemyCount = DefaultNetworkEnemyCount;
        [SerializeField] private int networkEnemyServerTickDeathDelaySeconds = DefaultNetworkEnemyServerTickDeathDelaySeconds;
        [SerializeField] private int networkEnemyServerTickDamage = DefaultNetworkEnemyServerTickDamage;
        [SerializeField] private int quitAfterSeconds = 0;

        private readonly object healthResponseLock = new object();
        private ServerRuntimeSettings activeSettings;
        private MultiplayerNetworkSessionService networkSessionService;
        private ServerGameConnectionService gameService;
        private ServerHealthService healthService;
        private string cachedHealthResponse;
        private float nextHeartbeatTime;
        private float startupTime;

        public ServerRuntimeSettings ActiveSettings => activeSettings;

        private void Awake()
        {
            startupTime = Time.realtimeSinceStartup;
            activeSettings = CreateSettingsFromCommandLine(
                Environment.GetCommandLineArgs(),
                new ServerRuntimeSettings(
                    port,
                    gameplayServerEnabled,
                    bindAddress,
                    maxPlayers,
                    roomId,
                    networkServerEnabled,
                    networkBindAddress,
                    networkPort,
                    networkPlayerPrefabResourcePath,
                    healthServerEnabled,
                    healthBindAddress,
                    healthPort,
                    targetFrameRate,
                    tickRate,
                    logIntervalSeconds,
                    networkEnemyCount,
                    networkEnemyServerTickDeathDelaySeconds,
                    networkEnemyServerTickDamage,
                    quitAfterSeconds));

            ApplySettings(activeSettings);
            StartGameService(activeSettings);
            RefreshHealthResponse(activeSettings);
            StartHealthService(activeSettings);
            LogStartup(activeSettings);
            nextHeartbeatTime = Time.realtimeSinceStartup + activeSettings.LogIntervalSeconds;
        }

        private void Start()
        {
            StartNetworkService(activeSettings);
            RefreshHealthResponse(activeSettings);
        }

        private void Update()
        {
            RefreshHealthResponse(activeSettings);

            if (activeSettings.LogIntervalSeconds > 0 && Time.realtimeSinceStartup >= nextHeartbeatTime)
            {
                LogHeartbeat(activeSettings);
                nextHeartbeatTime = Time.realtimeSinceStartup + activeSettings.LogIntervalSeconds;
            }

            if (activeSettings.QuitAfterSeconds > 0
                && Time.realtimeSinceStartup - startupTime >= activeSettings.QuitAfterSeconds)
            {
                Debug.Log("[ServerRuntime] QuitAfterSeconds reached; shutting down.");
                Application.Quit(0);
            }
        }

        private void OnApplicationQuit()
        {
            StopNetworkService();
            StopGameService();
            StopHealthService();
        }

        private void OnDestroy()
        {
            StopNetworkService();
            StopGameService();
            StopHealthService();
        }

        public static ServerRuntimeSettings CreateDefaultSettings()
        {
            return new ServerRuntimeSettings(
                DefaultPort,
                true,
                DefaultBindAddress,
                DefaultMaxPlayers,
                DefaultRoomId,
                true,
                DefaultNetworkBindAddress,
                DefaultNetworkPort,
                DefaultNetworkPlayerPrefabResourcePath,
                true,
                DefaultHealthBindAddress,
                DefaultHealthPort,
                DefaultTargetFrameRate,
                DefaultTickRate,
                DefaultLogIntervalSeconds,
                DefaultNetworkEnemyCount,
                DefaultNetworkEnemyServerTickDeathDelaySeconds,
                DefaultNetworkEnemyServerTickDamage,
                0);
        }

        public static ServerRuntimeSettings CreateSettingsFromCommandLine(string[] args)
        {
            return CreateSettingsFromCommandLine(args, CreateDefaultSettings());
        }

        public static ServerRuntimeSettings CreateSettingsFromCommandLine(
            string[] args,
            ServerRuntimeSettings defaults)
        {
            int resolvedPort = ReadIntArgument(args, defaults.Port, "-port", "--port", "-serverPort", "--server-port");
            bool resolvedGameplayServerEnabled = ReadBoolArgument(
                args,
                defaults.GameplayServerEnabled,
                "-gameplayServerEnabled",
                "--gameplay-server-enabled");
            string resolvedBindAddress = ReadStringArgument(
                args,
                defaults.BindAddress,
                "-bindAddress",
                "--bind-address",
                "-gameBindAddress",
                "--game-bind-address");
            int resolvedMaxPlayers = ReadIntArgument(
                args,
                defaults.MaxPlayers,
                "-maxPlayers",
                "--max-players");
            string resolvedRoomId = ReadStringArgument(
                args,
                defaults.RoomId,
                "-roomId",
                "--room-id");
            bool resolvedNetworkServerEnabled = ReadBoolArgument(
                args,
                defaults.NetworkServerEnabled,
                "-networkServerEnabled",
                "--network-server-enabled");
            string resolvedNetworkBindAddress = ReadStringArgument(
                args,
                defaults.NetworkBindAddress,
                "-networkBindAddress",
                "--network-bind-address",
                "-networkListenAddress",
                "--network-listen-address");
            int resolvedNetworkPort = ReadIntArgument(
                args,
                defaults.NetworkPort,
                "-networkPort",
                "--network-port",
                "-multiplayerPort",
                "--multiplayer-port");
            string resolvedNetworkPlayerPrefabResourcePath = ReadStringArgument(
                args,
                defaults.NetworkPlayerPrefabResourcePath,
                "-networkPlayerPrefab",
                "--network-player-prefab");
            bool resolvedHealthServerEnabled = ReadBoolArgument(
                args,
                defaults.HealthServerEnabled,
                "-healthServerEnabled",
                "--health-server-enabled");
            string resolvedHealthBindAddress = ReadStringArgument(
                args,
                defaults.HealthBindAddress,
                "-healthBindAddress",
                "--health-bind-address");
            int resolvedHealthPort = ReadIntArgument(
                args,
                defaults.HealthPort,
                "-healthPort",
                "--health-port");
            int resolvedTargetFrameRate = ReadIntArgument(
                args,
                defaults.TargetFrameRate,
                "-targetFrameRate",
                "--target-frame-rate",
                "--target-fps");
            int resolvedTickRate = ReadIntArgument(args, defaults.TickRate, "-tickRate", "--tick-rate");
            int resolvedLogIntervalSeconds = ReadIntArgument(
                args,
                defaults.LogIntervalSeconds,
                "-logInterval",
                "--log-interval");
            int resolvedNetworkEnemyCount = ReadIntArgument(
                args,
                defaults.NetworkEnemyCount,
                "-networkEnemyCount",
                "--network-enemy-count");
            int resolvedNetworkEnemyServerTickDeathDelaySeconds = ReadIntArgument(
                args,
                defaults.NetworkEnemyServerTickDeathDelaySeconds,
                "-networkEnemyServerTickDeathDelaySeconds",
                "--network-enemy-server-tick-death-delay-seconds");
            int resolvedNetworkEnemyServerTickDamage = ReadIntArgument(
                args,
                defaults.NetworkEnemyServerTickDamage,
                "-networkEnemyServerTickDamage",
                "--network-enemy-server-tick-damage");
            int resolvedQuitAfterSeconds = ReadIntArgument(
                args,
                defaults.QuitAfterSeconds,
                "-quitAfterSeconds",
                "--quit-after-seconds");

            if (HasFlag(args, "-disableHealthServer", "--disable-health-server"))
            {
                resolvedHealthServerEnabled = false;
            }

            if (HasFlag(args, "-enableHealthServer", "--enable-health-server"))
            {
                resolvedHealthServerEnabled = true;
            }

            if (HasFlag(args, "-disableGameplayServer", "--disable-gameplay-server"))
            {
                resolvedGameplayServerEnabled = false;
            }

            if (HasFlag(args, "-enableGameplayServer", "--enable-gameplay-server"))
            {
                resolvedGameplayServerEnabled = true;
            }

            if (HasFlag(args, "-disableNetworkServer", "--disable-network-server"))
            {
                resolvedNetworkServerEnabled = false;
            }

            if (HasFlag(args, "-enableNetworkServer", "--enable-network-server"))
            {
                resolvedNetworkServerEnabled = true;
            }

            return new ServerRuntimeSettings(
                Clamp(resolvedPort, 1, 65535),
                resolvedGameplayServerEnabled,
                NormalizeBindAddress(resolvedBindAddress, defaults.BindAddress, DefaultBindAddress),
                Clamp(resolvedMaxPlayers, 1, 256),
                NormalizeRoomId(resolvedRoomId, defaults.RoomId),
                resolvedNetworkServerEnabled,
                NormalizeBindAddress(resolvedNetworkBindAddress, defaults.NetworkBindAddress, DefaultNetworkBindAddress),
                Clamp(resolvedNetworkPort, 1, 65535),
                NormalizeResourcePath(resolvedNetworkPlayerPrefabResourcePath, defaults.NetworkPlayerPrefabResourcePath),
                resolvedHealthServerEnabled,
                NormalizeBindAddress(resolvedHealthBindAddress, defaults.HealthBindAddress, DefaultHealthBindAddress),
                Clamp(resolvedHealthPort, 1, 65535),
                Clamp(resolvedTargetFrameRate, 1, 240),
                Clamp(resolvedTickRate, 1, 120),
                Clamp(resolvedLogIntervalSeconds, 0, 300),
                Clamp(resolvedNetworkEnemyCount, 0, 16),
                Clamp(resolvedNetworkEnemyServerTickDeathDelaySeconds, 0, 86400),
                Clamp(resolvedNetworkEnemyServerTickDamage, 1, 10000),
                Clamp(resolvedQuitAfterSeconds, 0, 86400));
        }

        private static void ApplySettings(ServerRuntimeSettings settings)
        {
            Application.runInBackground = true;
            Application.targetFrameRate = settings.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            Time.fixedDeltaTime = 1f / settings.TickRate;
        }

        private static void LogStartup(ServerRuntimeSettings settings)
        {
            Debug.Log(
                "[ServerRuntime] Startup"
                + $" product={Application.productName}"
                + $" version={Application.version}"
                + $" unity={Application.unityVersion}"
                + $" platform={Application.platform}"
                + $" batchMode={Application.isBatchMode}"
                + $" port={settings.Port}"
                + $" gameplayServerEnabled={settings.GameplayServerEnabled}"
                + $" bindAddress={settings.BindAddress}"
                + $" maxPlayers={settings.MaxPlayers}"
                + $" roomId={settings.RoomId}"
                + $" networkServerEnabled={settings.NetworkServerEnabled}"
                + $" networkBindAddress={settings.NetworkBindAddress}"
                + $" networkPort={settings.NetworkPort}"
                + $" networkPlayerPrefabResourcePath={settings.NetworkPlayerPrefabResourcePath}"
                + $" healthServerEnabled={settings.HealthServerEnabled}"
                + $" healthBindAddress={settings.HealthBindAddress}"
                + $" healthPort={settings.HealthPort}"
                + $" targetFrameRate={settings.TargetFrameRate}"
                + $" tickRate={settings.TickRate}"
                + $" fixedDeltaTime={Time.fixedDeltaTime:0.0000}"
                + $" logIntervalSeconds={settings.LogIntervalSeconds}"
                + $" networkEnemyCount={settings.NetworkEnemyCount}"
                + $" networkEnemyServerTickDeathDelaySeconds={settings.NetworkEnemyServerTickDeathDelaySeconds}"
                + $" networkEnemyServerTickDamage={settings.NetworkEnemyServerTickDamage}"
                + $" quitAfterSeconds={settings.QuitAfterSeconds}");
        }

        private void LogHeartbeat(ServerRuntimeSettings settings)
        {
            long managedMemoryMb = GC.GetTotalMemory(false) / (1024L * 1024L);
            long connectionsAccepted = healthService != null ? healthService.ConnectionsAccepted : 0L;
            int activeConnections = healthService != null ? healthService.ActiveConnections : 0;
            ServerGameSnapshot gameSnapshot = CreateGameSnapshot(settings);
            MultiplayerNetworkSessionSnapshot networkSnapshot = CreateNetworkSnapshot(settings);

            Debug.Log(
                "[ServerRuntime] Heartbeat"
                + $" uptimeSeconds={Time.realtimeSinceStartup:0.0}"
                + $" frame={Time.frameCount}"
                + $" managedMemoryMb={managedMemoryMb}"
                + $" port={settings.Port}"
                + $" healthPort={settings.HealthPort}"
                + $" connectionsAccepted={connectionsAccepted}"
                + $" activeConnections={activeConnections}"
                + $" gameConnectionsAccepted={gameSnapshot.ConnectionsAccepted}"
                + $" gameActiveConnections={gameSnapshot.ActiveConnections}"
                + $" gamePlayers={gameSnapshot.ActivePlayers}"
                + $" gameMessagesReceived={gameSnapshot.MessagesReceived}"
                + $" networkStarted={networkSnapshot.Started}"
                + $" networkListening={networkSnapshot.Listening}"
                + $" networkConnectedClients={networkSnapshot.ConnectedClients}"
                + $" networkSpawnedPlayers={networkSnapshot.SpawnedPlayers}"
                + $" targetFrameRate={settings.TargetFrameRate}"
                + $" tickRate={settings.TickRate}");
        }

        private void StartNetworkService(ServerRuntimeSettings settings)
        {
            if (!settings.NetworkServerEnabled)
            {
                Debug.Log("[ServerRuntime] NGO network server disabled.");
                return;
            }

            if (networkSessionService != null)
            {
                return;
            }

            MultiplayerNetworkSessionService sessionService = null;

            try
            {
                sessionService = new MultiplayerNetworkSessionService();
                MultiplayerNetworkSessionService.ConfigureServerEnemyAttackSmoke(
                    ShouldEnableNetworkEnemyAttackSmoke(Environment.GetCommandLineArgs()));
                MultiplayerNetworkSessionService.ConfigureServerFormalEnemyAttackSmoke(
                    ShouldEnableNetworkEnemyFormalAttackSmoke(Environment.GetCommandLineArgs()));
                MultiplayerNetworkSessionService.ConfigureServerBrainEnemyAttackSmoke(
                    ShouldEnableNetworkEnemyBrainAttackSmoke(Environment.GetCommandLineArgs()));
                MultiplayerNetworkSessionService.ConfigureServerBrainEnemyChaseAttackSmoke(
                    ShouldEnableNetworkEnemyBrainChaseAttackSmoke(Environment.GetCommandLineArgs()));
                MultiplayerNetworkSessionService.ConfigureServerEnemyGameplayTickDeathDelay(
                    settings.NetworkEnemyServerTickDeathDelaySeconds);
                MultiplayerNetworkSessionService.ConfigureServerEnemyGameplayTickDamage(
                    settings.NetworkEnemyServerTickDamage);
                MultiplayerNetworkSessionService.ConfigureServerEnemyGameplayTick(
                    ShouldEnableNetworkEnemyGameplayTick(Environment.GetCommandLineArgs()));
                bool started = sessionService.StartServer(
                    new MultiplayerNetworkSessionSettings(
                        settings.NetworkBindAddress,
                        MultiplayerNetworkSessionSettings.DefaultConnectAddress,
                        settings.NetworkPort,
                        settings.MaxPlayers,
                        settings.NetworkPlayerPrefabResourcePath,
                        ResolveNetworkPlayerPrefabOverride(settings.NetworkPlayerPrefabResourcePath),
                        MultiplayerNetworkSessionSettings.DefaultEnemyPrefabResourcePath,
                        null,
                        settings.NetworkEnemyCount));

                if (started)
                {
                    networkSessionService = sessionService;
                    return;
                }

                sessionService.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogError("[ServerRuntime] Failed to start NGO network server: " + exception);
                sessionService?.Dispose();
                networkSessionService = null;
            }
        }

        public static bool ShouldEnableNetworkEnemyAttackSmoke(string[] args)
        {
            return HasFlag(
                args,
                "-enableNetworkEnemyAttackSmoke",
                "--enable-network-enemy-attack-smoke",
                "--network-enemy-attack-smoke");
        }

        public static bool ShouldEnableNetworkEnemyFormalAttackSmoke(string[] args)
        {
            return HasFlag(
                args,
                "-enableNetworkEnemyFormalAttackSmoke",
                "--enable-network-enemy-formal-attack-smoke",
                "--network-enemy-formal-attack-smoke");
        }

        public static bool ShouldEnableNetworkEnemyBrainAttackSmoke(string[] args)
        {
            return HasFlag(
                args,
                "-enableNetworkEnemyBrainAttackSmoke",
                "--enable-network-enemy-brain-attack-smoke",
                "--network-enemy-brain-attack-smoke");
        }

        public static bool ShouldEnableNetworkEnemyBrainChaseAttackSmoke(string[] args)
        {
            return HasFlag(
                args,
                "-enableNetworkEnemyBrainChaseAttackSmoke",
                "--enable-network-enemy-brain-chase-attack-smoke",
                "--network-enemy-brain-chase-attack-smoke");
        }

        public static bool ShouldEnableNetworkEnemyGameplayTick(string[] args)
        {
            return HasFlag(
                args,
                "-enableNetworkEnemyGameplayTick",
                "--enable-network-enemy-server-tick",
                "--network-enemy-server-tick");
        }

        public static bool ShouldUseSerializedNetworkPlayerPrefab(
            string activeResourcePath,
            string serializedResourcePath,
            GameObject serializedPrefab)
        {
            if (serializedPrefab == null)
            {
                return false;
            }

            string activePath = NormalizeResourcePath(activeResourcePath, DefaultNetworkPlayerPrefabResourcePath);
            string scenePath = NormalizeResourcePath(serializedResourcePath, DefaultNetworkPlayerPrefabResourcePath);
            return string.Equals(activePath, scenePath, StringComparison.Ordinal);
        }

        private GameObject ResolveNetworkPlayerPrefabOverride(string activeResourcePath)
        {
            if (ShouldUseSerializedNetworkPlayerPrefab(
                    activeResourcePath,
                    networkPlayerPrefabResourcePath,
                    networkPlayerPrefab))
            {
                return networkPlayerPrefab;
            }

            if (networkPlayerPrefab != null)
            {
                Debug.Log(
                    "[ServerRuntime] Network player prefab command-line path overrides scene prefab reference"
                    + $" scenePath={NormalizeResourcePath(networkPlayerPrefabResourcePath, DefaultNetworkPlayerPrefabResourcePath)}"
                    + $" activePath={NormalizeResourcePath(activeResourcePath, DefaultNetworkPlayerPrefabResourcePath)}");
            }

            return null;
        }

        private void StopNetworkService()
        {
            if (networkSessionService == null)
            {
                return;
            }

            networkSessionService.Dispose();
            networkSessionService = null;
        }

        private void StartGameService(ServerRuntimeSettings settings)
        {
            if (!settings.GameplayServerEnabled)
            {
                Debug.Log("[ServerRuntime] Gameplay service disabled.");
                return;
            }

            gameService = new ServerGameConnectionService(
                settings.BindAddress,
                settings.Port,
                settings.RoomId,
                settings.MaxPlayers);
            gameService.Start();

            Debug.Log(
                "[ServerRuntime] Gameplay service listening"
                + $" bindAddress={settings.BindAddress}"
                + $" port={gameService.BoundPort}"
                + $" roomId={settings.RoomId}"
                + $" maxPlayers={settings.MaxPlayers}");
        }

        private void StopGameService()
        {
            if (gameService == null)
            {
                return;
            }

            gameService.Dispose();
            gameService = null;
        }

        private void StartHealthService(ServerRuntimeSettings settings)
        {
            if (!settings.HealthServerEnabled)
            {
                Debug.Log("[ServerRuntime] Health service disabled.");
                return;
            }

            healthService = new ServerHealthService(
                settings.HealthBindAddress,
                settings.HealthPort,
                GetCachedHealthResponse);
            healthService.Start();

            Debug.Log(
                "[ServerRuntime] Health service listening"
                + $" bindAddress={settings.HealthBindAddress}"
                + $" port={healthService.BoundPort}");
        }

        private void StopHealthService()
        {
            if (healthService == null)
            {
                return;
            }

            healthService.Dispose();
            healthService = null;
        }

        private void RefreshHealthResponse(ServerRuntimeSettings settings)
        {
            long connectionsAccepted = healthService != null ? healthService.ConnectionsAccepted : 0L;
            int activeConnections = healthService != null ? healthService.ActiveConnections : 0;
            long managedMemoryMb = GC.GetTotalMemory(false) / (1024L * 1024L);
            ServerGameSnapshot gameSnapshot = CreateGameSnapshot(settings);
            MultiplayerNetworkSessionSnapshot networkSnapshot = CreateNetworkSnapshot(settings);
            ServerHealthSnapshot snapshot = new ServerHealthSnapshot(
                ServerHealthService.DefaultServiceName,
                "ok",
                settings.Port,
                gameSnapshot.Enabled,
                gameSnapshot.BindAddress,
                gameSnapshot.Port,
                gameSnapshot.RoomId,
                gameSnapshot.MaxPlayers,
                gameSnapshot.ConnectionsAccepted,
                gameSnapshot.ActiveConnections,
                gameSnapshot.PlayersJoined,
                gameSnapshot.ActivePlayers,
                gameSnapshot.MessagesReceived,
                settings.NetworkServerEnabled,
                networkSnapshot.Started,
                networkSnapshot.Listening,
                networkSnapshot.IsServer,
                networkSnapshot.IsClient,
                networkSnapshot.ListenAddress,
                networkSnapshot.ConnectAddress,
                networkSnapshot.Port,
                networkSnapshot.MaxPlayers,
                networkSnapshot.ConnectedClients,
                networkSnapshot.SpawnedPlayers,
                settings.HealthServerEnabled,
                settings.HealthBindAddress,
                settings.HealthPort,
                settings.TargetFrameRate,
                settings.TickRate,
                Time.realtimeSinceStartup - startupTime,
                Time.frameCount,
                managedMemoryMb,
                connectionsAccepted,
                activeConnections);

            lock (healthResponseLock)
            {
                cachedHealthResponse = ServerHealthService.FormatHealthResponse(snapshot);
            }
        }

        private ServerGameSnapshot CreateGameSnapshot(ServerRuntimeSettings settings)
        {
            if (gameService != null)
            {
                return gameService.CreateSnapshot(settings.GameplayServerEnabled);
            }

            return new ServerGameSnapshot(
                settings.GameplayServerEnabled,
                settings.BindAddress,
                settings.Port,
                settings.RoomId,
                settings.MaxPlayers,
                0L,
                0,
                0L,
                0,
                0L);
        }

        private MultiplayerNetworkSessionSnapshot CreateNetworkSnapshot(ServerRuntimeSettings settings)
        {
            if (networkSessionService != null)
            {
                return networkSessionService.CreateSnapshot(settings.NetworkServerEnabled);
            }

            return new MultiplayerNetworkSessionSnapshot(
                settings.NetworkServerEnabled,
                false,
                false,
                false,
                false,
                settings.NetworkBindAddress,
                MultiplayerNetworkSessionSettings.DefaultConnectAddress,
                settings.NetworkPort,
                settings.MaxPlayers,
                0,
                0);
        }

        private string GetCachedHealthResponse()
        {
            lock (healthResponseLock)
            {
                return cachedHealthResponse;
            }
        }

        private static int ReadIntArgument(string[] args, int fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int splitValue))
                        {
                            return splitValue;
                        }

                        continue;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(arg.Substring(prefix.Length), out int inlineValue))
                    {
                        return inlineValue;
                    }
                }
            }

            return fallback;
        }

        private static bool ReadBoolArgument(string[] args, bool fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && TryParseBool(args[i + 1], out bool splitValue))
                        {
                            return splitValue;
                        }

                        return true;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && TryParseBool(arg.Substring(prefix.Length), out bool inlineValue))
                    {
                        return inlineValue;
                    }
                }
            }

            return fallback;
        }

        private static string ReadStringArgument(string[] args, string fallback, params string[] names)
        {
            if (args == null || names == null)
            {
                return fallback;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    string name = names[nameIndex];

                    if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                        {
                            return args[i + 1];
                        }

                        continue;
                    }

                    string prefix = name + "=";

                    if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return arg.Substring(prefix.Length);
                    }
                }
            }

            return fallback;
        }

        private static bool HasFlag(string[] args, params string[] names)
        {
            if (args == null || names == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                for (int nameIndex = 0; nameIndex < names.Length; nameIndex++)
                {
                    if (arg.Equals(names[nameIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = false;
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    result = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    result = false;
                    return true;
                default:
                    return bool.TryParse(value, out result);
            }
        }

        private static string NormalizeBindAddress(string value, string fallback, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.IsNullOrWhiteSpace(fallback) ? defaultValue : fallback.Trim();
            }

            return value.Trim();
        }

        private static string NormalizeRoomId(string value, string fallback)
        {
            string resolved = string.IsNullOrWhiteSpace(value) ? fallback : value;

            if (string.IsNullOrWhiteSpace(resolved))
            {
                resolved = DefaultRoomId;
            }

            return resolved.Trim().Replace(' ', '_').Replace('\t', '_').Replace('\n', '_').Replace('\r', '_');
        }

        private static string NormalizeResourcePath(string value, string fallback)
        {
            string resolved = string.IsNullOrWhiteSpace(value) ? fallback : value;

            if (string.IsNullOrWhiteSpace(resolved))
            {
                resolved = DefaultNetworkPlayerPrefabResourcePath;
            }

            return resolved.Trim().Replace('\\', '/');
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public struct ServerRuntimeSettings : IEquatable<ServerRuntimeSettings>
    {
        public ServerRuntimeSettings(
            int port,
            bool gameplayServerEnabled,
            string bindAddress,
            int maxPlayers,
            string roomId,
            bool networkServerEnabled,
            string networkBindAddress,
            int networkPort,
            string networkPlayerPrefabResourcePath,
            bool healthServerEnabled,
            string healthBindAddress,
            int healthPort,
            int targetFrameRate,
            int tickRate,
            int logIntervalSeconds,
            int networkEnemyCount,
            int networkEnemyServerTickDeathDelaySeconds,
            int networkEnemyServerTickDamage,
            int quitAfterSeconds)
        {
            Port = port;
            GameplayServerEnabled = gameplayServerEnabled;
            BindAddress = bindAddress;
            MaxPlayers = maxPlayers;
            RoomId = roomId;
            NetworkServerEnabled = networkServerEnabled;
            NetworkBindAddress = networkBindAddress;
            NetworkPort = networkPort;
            NetworkPlayerPrefabResourcePath = networkPlayerPrefabResourcePath;
            HealthServerEnabled = healthServerEnabled;
            HealthBindAddress = healthBindAddress;
            HealthPort = healthPort;
            TargetFrameRate = targetFrameRate;
            TickRate = tickRate;
            LogIntervalSeconds = logIntervalSeconds;
            NetworkEnemyCount = networkEnemyCount;
            NetworkEnemyServerTickDeathDelaySeconds = networkEnemyServerTickDeathDelaySeconds;
            NetworkEnemyServerTickDamage = networkEnemyServerTickDamage;
            QuitAfterSeconds = quitAfterSeconds;
        }

        public int Port { get; }
        public bool GameplayServerEnabled { get; }
        public string BindAddress { get; }
        public int MaxPlayers { get; }
        public string RoomId { get; }
        public bool NetworkServerEnabled { get; }
        public string NetworkBindAddress { get; }
        public int NetworkPort { get; }
        public string NetworkPlayerPrefabResourcePath { get; }
        public bool HealthServerEnabled { get; }
        public string HealthBindAddress { get; }
        public int HealthPort { get; }
        public int TargetFrameRate { get; }
        public int TickRate { get; }
        public int LogIntervalSeconds { get; }
        public int NetworkEnemyCount { get; }
        public int NetworkEnemyServerTickDeathDelaySeconds { get; }
        public int NetworkEnemyServerTickDamage { get; }
        public int QuitAfterSeconds { get; }

        public bool Equals(ServerRuntimeSettings other)
        {
            return Port == other.Port
                && GameplayServerEnabled == other.GameplayServerEnabled
                && string.Equals(BindAddress, other.BindAddress, StringComparison.Ordinal)
                && MaxPlayers == other.MaxPlayers
                && string.Equals(RoomId, other.RoomId, StringComparison.Ordinal)
                && NetworkServerEnabled == other.NetworkServerEnabled
                && string.Equals(NetworkBindAddress, other.NetworkBindAddress, StringComparison.Ordinal)
                && NetworkPort == other.NetworkPort
                && string.Equals(NetworkPlayerPrefabResourcePath, other.NetworkPlayerPrefabResourcePath, StringComparison.Ordinal)
                && HealthServerEnabled == other.HealthServerEnabled
                && string.Equals(HealthBindAddress, other.HealthBindAddress, StringComparison.Ordinal)
                && HealthPort == other.HealthPort
                && TargetFrameRate == other.TargetFrameRate
                && TickRate == other.TickRate
                && LogIntervalSeconds == other.LogIntervalSeconds
                && NetworkEnemyCount == other.NetworkEnemyCount
                && NetworkEnemyServerTickDeathDelaySeconds == other.NetworkEnemyServerTickDeathDelaySeconds
                && NetworkEnemyServerTickDamage == other.NetworkEnemyServerTickDamage
                && QuitAfterSeconds == other.QuitAfterSeconds;
        }

        public override bool Equals(object obj)
        {
            return obj is ServerRuntimeSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Port;
                hashCode = (hashCode * 397) ^ GameplayServerEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (BindAddress != null ? BindAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MaxPlayers;
                hashCode = (hashCode * 397) ^ (RoomId != null ? RoomId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NetworkServerEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (NetworkBindAddress != null ? NetworkBindAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NetworkPort;
                hashCode = (hashCode * 397) ^ (NetworkPlayerPrefabResourcePath != null ? NetworkPlayerPrefabResourcePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ HealthServerEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (HealthBindAddress != null ? HealthBindAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ HealthPort;
                hashCode = (hashCode * 397) ^ TargetFrameRate;
                hashCode = (hashCode * 397) ^ TickRate;
                hashCode = (hashCode * 397) ^ LogIntervalSeconds;
                hashCode = (hashCode * 397) ^ NetworkEnemyCount;
                hashCode = (hashCode * 397) ^ NetworkEnemyServerTickDeathDelaySeconds;
                hashCode = (hashCode * 397) ^ NetworkEnemyServerTickDamage;
                hashCode = (hashCode * 397) ^ QuitAfterSeconds;
                return hashCode;
            }
        }
    }
}
