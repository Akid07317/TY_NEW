using System;
using System.IO;
using System.Linq;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Multiplayer;
using CampusRPG.Server;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CampusRPG.Editor
{
    public static class DedicatedServerBuildUtility
    {
        public const string ServerBuildRoot = "Builds/DedicatedServer";
        public const string LinuxOutputPath = ServerBuildRoot + "/Linux/TYServer.x86_64";
        public const string MacLocalServerOutputPath = ServerBuildRoot + "/MacLocal/TYServer.app";
        public const string ServerBootScenePath = "Assets/_Game/Scenes/ServerBoot.unity";
        public const string NetworkPlayerPrefabPath = "Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerAvatar.prefab";
        public const string FormalNetworkPlayerPrefabPath = "Assets/_Game/Resources/Multiplayer/PF_NetworkPlayerCombatTest.prefab";
        public const string NetworkEnemyPrefabPath = "Assets/_Game/Resources/Multiplayer/PF_NetworkEnemyAvatar.prefab";
        public const string FormalNetworkPlayerPrefabResourcePath = "Multiplayer/PF_NetworkPlayerCombatTest";
        public const string ServerBootNavMeshGroundName = "ServerNavMeshGround";
        private const string CombatTestPlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string CombatTestEnemyMeleePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab";

        private static readonly string[] ServerScenePaths =
        {
            ServerBootScenePath
        };

        private static readonly string[] LocalPreviewOnlyAssetRoots =
        {
            "Assets/Kevin Iglesias",
            "Assets/DoubleL",
            "Assets/ithappy",
            "Assets/JC_LP_MedievalCharacters_LITE",
            "Assets/Free medieval weapons",
            "Assets/GhostSamurai_Animset",
            "Assets/MYFG-Weapon Pack Lite",
            "Assets/Polytope Studio",
            "Assets/LocalPreviewTools",
            "Assets/_Game/Animations/Characters/CombatTest/LocalPreview"
        };

        [MenuItem("CampusRPG/Build/Dedicated Server/Create Or Repair ServerBoot Scene")]
        public static void CreateOrRepairServerBootScene()
        {
            CreateOrRepairNetworkPlayerPrefab();
            CreateOrRepairFormalNetworkPlayerPrefab();
            CreateOrRepairNetworkEnemyPrefab();

            string sceneDirectory = Path.GetDirectoryName(ServerBootScenePath);

            if (!string.IsNullOrEmpty(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject serverRuntime = new GameObject("ServerRuntime");
            ServerRuntimeBootstrap bootstrap = serverRuntime.AddComponent<ServerRuntimeBootstrap>();
            CreateOrRepairServerBootNavMeshGround();
            GameObject networkPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerPrefabPath);

            if (networkPlayerPrefab != null)
            {
                SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
                serializedBootstrap.FindProperty("networkPlayerPrefab").objectReferenceValue = networkPlayerPrefab;
                serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ServerBootScenePath);
            bool navMeshBuilt = RebuildServerBootSceneNavMesh(scene);
            EditorSceneManager.SaveScene(scene, ServerBootScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!navMeshBuilt)
            {
                Debug.LogWarning("ServerBoot scene was generated, but no baked NavMesh data was produced.");
            }

            Debug.Log($"Dedicated server boot scene saved at {ServerBootScenePath}.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Create Or Repair Network Player Prefab")]
        public static void CreateOrRepairNetworkPlayerPrefab()
        {
            string prefabDirectory = Path.GetDirectoryName(NetworkPlayerPrefabPath);

            if (!string.IsNullOrEmpty(prefabDirectory))
            {
                Directory.CreateDirectory(prefabDirectory);
            }

            GameObject root = new GameObject("PF_NetworkPlayerAvatar");
            root.AddComponent<NetworkObject>();
            NetworkPlayerAvatar avatar = root.AddComponent<NetworkPlayerAvatar>();
            string avatarTypeName = avatar.GetType().Name;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual_Capsule";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);

            Collider visualCollider = visual.GetComponent<Collider>();

            if (visualCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(visualCollider);
            }

            PrefabUtility.SaveAsPrefabAsset(root, NetworkPlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(NetworkPlayerPrefabPath);
            AssetDatabase.Refresh();
            ValidateNetworkPlayerPrefab();

            Debug.Log($"P1.5 network player prefab saved at {NetworkPlayerPrefabPath}: {avatarTypeName}.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Create Or Repair Formal Network Player Prefab")]
        public static void CreateOrRepairFormalNetworkPlayerPrefab()
        {
            string prefabDirectory = Path.GetDirectoryName(FormalNetworkPlayerPrefabPath);

            if (!string.IsNullOrEmpty(prefabDirectory))
            {
                Directory.CreateDirectory(prefabDirectory);
            }

            GameObject combatPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatTestPlayerPrefabPath);

            if (combatPlayerPrefab == null)
            {
                throw new FileNotFoundException(
                    $"CombatTest player prefab does not exist: {CombatTestPlayerPrefabPath}",
                    CombatTestPlayerPrefabPath);
            }

            GameObject root = new GameObject("PF_NetworkPlayerCombatTest");
            root.AddComponent<NetworkObject>();
            NetworkPlayerAvatar avatar = root.AddComponent<NetworkPlayerAvatar>();
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(combatPlayerPrefab);

            try
            {
                if (player == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to instantiate CombatTest player prefab: {CombatTestPlayerPrefabPath}");
                }

                PrefabUtility.UnpackPrefabInstance(player, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                player.name = "FormalPlayer_CombatTest";
                player.transform.SetParent(root.transform, false);
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                player.transform.localScale = Vector3.one;

                Animator animator = player.GetComponent<Animator>();
                CombatImportedPlayerVisualUtility.RemoveImportedVisual(player, animator);
                CombatProxyVisualUtility.Apply(player, CombatProxyVisualKind.Player);

                NetworkPlayerDeathStateBridge deathStateBridge = GetOrAddComponent<NetworkPlayerDeathStateBridge>(player);
                NetworkPlayerPresentationBridge presentationBridge =
                    GetOrAddComponent<NetworkPlayerPresentationBridge>(player);
                HealthComponent health = player.GetComponent<HealthComponent>();
                PlayerStateMachine stateMachine = player.GetComponent<PlayerStateMachine>();
                deathStateBridge.Configure(avatar, health, stateMachine);
                presentationBridge.Configure(avatar, player.GetComponent<PlayerCharacter>(), health, stateMachine);
                EditorUtility.SetDirty(deathStateBridge);
                EditorUtility.SetDirty(presentationBridge);

                PrefabUtility.SaveAsPrefabAsset(root, FormalNetworkPlayerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(FormalNetworkPlayerPrefabPath);
            AssetDatabase.Refresh();
            ValidateFormalNetworkPlayerPrefab();

            Debug.Log($"P5 formal network player prefab saved at {FormalNetworkPlayerPrefabPath}.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Create Or Repair Network Enemy Prefab")]
        public static void CreateOrRepairNetworkEnemyPrefab()
        {
            string prefabDirectory = Path.GetDirectoryName(NetworkEnemyPrefabPath);

            if (!string.IsNullOrEmpty(prefabDirectory))
            {
                Directory.CreateDirectory(prefabDirectory);
            }

            GameObject root = new GameObject("PF_NetworkEnemyAvatar");
            root.AddComponent<NetworkObject>();
            NetworkEnemyAvatar enemy = root.AddComponent<NetworkEnemyAvatar>();
            string enemyTypeName = enemy.GetType().Name;
            GameObject combatEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatTestEnemyMeleePrefabPath);

            if (combatEnemyPrefab == null)
            {
                throw new FileNotFoundException(
                    $"CombatTest melee enemy prefab does not exist: {CombatTestEnemyMeleePrefabPath}",
                    CombatTestEnemyMeleePrefabPath);
            }

            try
            {
                GameObject formalEnemy = (GameObject)PrefabUtility.InstantiatePrefab(combatEnemyPrefab);

                if (formalEnemy == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to instantiate CombatTest melee enemy prefab: {CombatTestEnemyMeleePrefabPath}");
                }

                PrefabUtility.UnpackPrefabInstance(formalEnemy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                formalEnemy.name = "FormalEnemy_Melee_CombatTest";
                formalEnemy.transform.SetParent(root.transform, false);
                formalEnemy.transform.localPosition = Vector3.zero;
                formalEnemy.transform.localRotation = Quaternion.identity;
                formalEnemy.transform.localScale = Vector3.one;

                Animator animator = formalEnemy.GetComponent<Animator>();
                CombatImportedEnemyVisualUtility.RemoveImportedVisual(formalEnemy, animator);

                EnemyCombatAnimationRelay importedAnimationRelay =
                    formalEnemy.GetComponent<EnemyCombatAnimationRelay>();

                if (importedAnimationRelay != null)
                {
                    UnityEngine.Object.DestroyImmediate(importedAnimationRelay);
                }

                if (animator != null)
                {
                    UnityEngine.Object.DestroyImmediate(animator);
                }

                CombatProxyVisualUtility.Apply(formalEnemy, CombatProxyVisualKind.EnemyMelee);

                EnemyBrain enemyBrain = formalEnemy.GetComponent<EnemyBrain>();
                EnemyStateMachine stateMachine = formalEnemy.GetComponent<EnemyStateMachine>();
                HealthComponent health = formalEnemy.GetComponent<HealthComponent>();
                EnemySensing sensing = formalEnemy.GetComponent<EnemySensing>();
                EnemyMotor motor = formalEnemy.GetComponent<EnemyMotor>();
                EnemyAttackController attackController = formalEnemy.GetComponent<EnemyAttackController>();
                NavMeshAgent navMeshAgent = formalEnemy.GetComponent<NavMeshAgent>();
                NetworkEnemyPresentationBridge presentationBridge =
                    GetOrAddComponent<NetworkEnemyPresentationBridge>(formalEnemy);

                presentationBridge.Configure(
                    enemy,
                    enemyBrain,
                    stateMachine,
                    health,
                    sensing,
                    motor,
                    attackController,
                    navMeshAgent);
                EditorUtility.SetDirty(presentationBridge);

                PrefabUtility.SaveAsPrefabAsset(root, NetworkEnemyPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(NetworkEnemyPrefabPath);
            AssetDatabase.Refresh();
            ValidateNetworkEnemyPrefab();

            Debug.Log($"P6.2 formal network enemy prefab saved at {NetworkEnemyPrefabPath}: {enemyTypeName}.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Validate Linux Dedicated Server Build Inputs")]
        public static void ValidateLinuxDedicatedServerBuildInputs()
        {
            ValidateBuildInputs();
            Debug.Log("Dedicated server build inputs are valid for Linux Server.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Run Smoke Verification")]
        public static void RunDedicatedServerSmokeVerification()
        {
            BuildPlayerOptions options = CreateBuildOptions();

            Require(options.scenes.Length == 1, "Expected exactly one server scene.");
            Require(
                options.scenes[0] == ServerBootScenePath,
                $"Expected server scene {ServerBootScenePath}, got {options.scenes[0]}.");
            Require(options.target == BuildTarget.StandaloneLinux64, $"Expected Linux target, got {options.target}.");
            Require(
                options.subtarget == (int)StandaloneBuildSubtarget.Server,
                $"Expected Server subtarget, got {options.subtarget}.");
            Require(options.options == BuildOptions.None, $"Expected no build options, got {options.options}.");
            Require(
                NormalizePath(options.locationPathName) == LinuxOutputPath,
                $"Expected output {LinuxOutputPath}, got {options.locationPathName}.");

            ServerRuntimeSettings parsedSettings = ServerRuntimeBootstrap.CreateSettingsFromCommandLine(
                new[]
                {
                    "TYServer.x86_64",
                    "-port=70000",
                    "--bind-address",
                    "localhost",
                    "--max-players=999",
                    "--room-id",
                    "p1-smoke-room",
                    "--disable-gameplay-server",
                    "--network-port=70000",
                    "--network-bind-address",
                    "localhost",
                    "--network-player-prefab",
                    "Multiplayer/TestAvatar",
                    "--disable-network-server",
                    "--health-port=70000",
                    "--health-bind-address",
                    "localhost",
                    "--disable-health-server",
                    "--target-fps",
                    "0",
                    "--tick-rate=999",
                    "--log-interval=-5",
                    "--quit-after-seconds",
                    "3"
                });

            Require(parsedSettings.Port == 65535, $"Expected clamped port 65535, got {parsedSettings.Port}.");
            Require(
                !parsedSettings.GameplayServerEnabled,
                "Expected gameplay server to be disabled by command line.");
            Require(
                parsedSettings.BindAddress == "localhost",
                $"Expected gameplay bind address localhost, got {parsedSettings.BindAddress}.");
            Require(parsedSettings.MaxPlayers == 256, $"Expected max players 256, got {parsedSettings.MaxPlayers}.");
            Require(parsedSettings.RoomId == "p1-smoke-room", $"Expected room id p1-smoke-room, got {parsedSettings.RoomId}.");
            Require(!parsedSettings.NetworkServerEnabled, "Expected NGO network server to be disabled.");
            Require(
                parsedSettings.NetworkBindAddress == "localhost",
                $"Expected NGO network bind address localhost, got {parsedSettings.NetworkBindAddress}.");
            Require(
                parsedSettings.NetworkPort == 65535,
                $"Expected clamped NGO network port 65535, got {parsedSettings.NetworkPort}.");
            Require(
                parsedSettings.NetworkPlayerPrefabResourcePath == "Multiplayer/TestAvatar",
                $"Expected NGO network player prefab path Multiplayer/TestAvatar, got {parsedSettings.NetworkPlayerPrefabResourcePath}.");
            Require(
                !parsedSettings.HealthServerEnabled,
                "Expected health server to be disabled by command line.");
            Require(
                parsedSettings.HealthBindAddress == "localhost",
                $"Expected health bind address localhost, got {parsedSettings.HealthBindAddress}.");
            Require(
                parsedSettings.HealthPort == 65535,
                $"Expected clamped health port 65535, got {parsedSettings.HealthPort}.");
            Require(
                parsedSettings.TargetFrameRate == 1,
                $"Expected clamped target frame rate 1, got {parsedSettings.TargetFrameRate}.");
            Require(parsedSettings.TickRate == 120, $"Expected clamped tick rate 120, got {parsedSettings.TickRate}.");
            Require(
                parsedSettings.LogIntervalSeconds == 0,
                $"Expected clamped log interval 0, got {parsedSettings.LogIntervalSeconds}.");
            Require(
                parsedSettings.QuitAfterSeconds == 3,
                $"Expected quit-after seconds 3, got {parsedSettings.QuitAfterSeconds}.");

            ServerRuntimeSettings defaultSettings = ServerRuntimeBootstrap.CreateDefaultSettings();
            Require(defaultSettings.GameplayServerEnabled, "Expected gameplay server to be enabled by default.");
            Require(
                defaultSettings.BindAddress == ServerRuntimeBootstrap.DefaultBindAddress,
                "Expected default gameplay bind address to match bootstrap default.");
            Require(
                defaultSettings.MaxPlayers == ServerRuntimeBootstrap.DefaultMaxPlayers,
                "Expected default max players to match bootstrap default.");
            Require(
                defaultSettings.RoomId == ServerRuntimeBootstrap.DefaultRoomId,
                "Expected default room id to match bootstrap default.");
            Require(defaultSettings.NetworkServerEnabled, "Expected NGO network server to be enabled by default.");
            Require(
                defaultSettings.NetworkBindAddress == ServerRuntimeBootstrap.DefaultNetworkBindAddress,
                "Expected default NGO bind address to match bootstrap default.");
            Require(
                defaultSettings.NetworkPort == ServerRuntimeBootstrap.DefaultNetworkPort,
                "Expected default NGO network port to match bootstrap default.");
            Require(
                defaultSettings.NetworkPlayerPrefabResourcePath == ServerRuntimeBootstrap.DefaultNetworkPlayerPrefabResourcePath,
                "Expected default NGO player prefab resource path to match bootstrap default.");
            Require(defaultSettings.HealthServerEnabled, "Expected health server to be enabled by default.");
            Require(
                defaultSettings.HealthBindAddress == ServerRuntimeBootstrap.DefaultHealthBindAddress,
                "Expected default health bind address to match bootstrap default.");
            Require(
                defaultSettings.HealthPort == ServerRuntimeBootstrap.DefaultHealthPort,
                "Expected default health port to match bootstrap default.");

            string healthResponse = ServerHealthService.FormatHealthResponse(
                new ServerHealthSnapshot(
                    ServerHealthService.DefaultServiceName,
                    "ok",
                    defaultSettings.Port,
                    defaultSettings.GameplayServerEnabled,
                    defaultSettings.BindAddress,
                    defaultSettings.Port,
                    defaultSettings.RoomId,
                    defaultSettings.MaxPlayers,
                    3,
                    1,
                    2,
                    1,
                    4,
                    true,
                    true,
                    true,
                    true,
                    false,
                    defaultSettings.NetworkBindAddress,
                    MultiplayerNetworkSessionSettings.DefaultConnectAddress,
                    defaultSettings.NetworkPort,
                    defaultSettings.MaxPlayers,
                    2,
                    2,
                    defaultSettings.HealthServerEnabled,
                    defaultSettings.HealthBindAddress,
                    defaultSettings.HealthPort,
                    defaultSettings.TargetFrameRate,
                    defaultSettings.TickRate,
                    12.5,
                    30,
                    64,
                    2,
                    1));

            Require(healthResponse.Contains("status=ok"), "Expected health response to include status=ok.");
            Require(
                healthResponse.Contains("healthPort=7778"),
                "Expected health response to include healthPort=7778.");
            Require(
                healthResponse.Contains("connectionsAccepted=2"),
                "Expected health response to include connection counters.");
            Require(
                healthResponse.Contains("gameplayPort=7777"),
                "Expected health response to include gameplay port.");
            Require(
                healthResponse.Contains("gameActivePlayers=1"),
                "Expected health response to include game player counters.");
            Require(
                healthResponse.Contains("networkEnabled=true"),
                "Expected health response to include NGO network enabled state.");
            Require(
                healthResponse.Contains("networkPort=7777"),
                "Expected health response to include NGO network port.");
            Require(
                healthResponse.Contains("networkConnectedClients=2"),
                "Expected health response to include NGO connected client counters.");

            string welcome = ServerGameConnectionService.FormatWelcome(7, defaultSettings.RoomId, defaultSettings.MaxPlayers);
            Require(welcome.Contains("protocol=1"), "Expected game welcome to include protocol version.");
            Require(welcome.Contains("room=combat-test"), "Expected game welcome to include default room.");

            string joined = ServerGameConnectionService.FormatJoined(
                new PlayerSessionSnapshot(7, 7, "SmokeBot", defaultSettings.RoomId, true),
                1,
                defaultSettings.MaxPlayers);
            Require(joined.Contains("JOINED"), "Expected game join response.");
            Require(joined.Contains("playerName=SmokeBot"), "Expected game join response to include player name.");

            EditorSceneManager.OpenScene(ServerBootScenePath);

            ServerRuntimeBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<ServerRuntimeBootstrap>();
            UnityEngine.Camera[] cameras = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>();
            AudioListener[] audioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>();

            Require(bootstraps.Length == 1, $"Expected one ServerRuntimeBootstrap, got {bootstraps.Length}.");
            Require(cameras.Length == 0, $"Expected no cameras in ServerBoot, got {cameras.Length}.");
            Require(audioListeners.Length == 0, $"Expected no audio listeners in ServerBoot, got {audioListeners.Length}.");
            ValidateNetworkPlayerPrefab();
            ValidateNetworkEnemyPrefab();

            string[] dependencies = AssetDatabase.GetDependencies(ServerBootScenePath, true);
            Require(
                !dependencies.Contains("Assets/_Game/Scripts/Runtime/Input/InputReader.cs"),
                "ServerBoot depends on InputReader.cs.");

            Debug.Log("Dedicated server smoke verification passed.");
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Build Linux Dedicated Server")]
        public static void BuildLinuxDedicatedServer()
        {
            Build();
        }

        public static BuildPlayerOptions CreateBuildOptions()
        {
            ValidateBuildInputs();

            return new BuildPlayerOptions
            {
                scenes = GetServerScenePaths(),
                locationPathName = LinuxOutputPath,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };
        }

        [MenuItem("CampusRPG/Build/Dedicated Server/Build macOS Local Server Player")]
        public static void BuildMacOSLocalServerPlayer()
        {
            BuildMacOSLocalServer();
        }

        public static BuildPlayerOptions CreateMacOSLocalServerBuildOptions()
        {
            ValidateBuildInputs();

            return new BuildPlayerOptions
            {
                scenes = GetServerScenePaths(),
                locationPathName = MacLocalServerOutputPath,
                target = BuildTarget.StandaloneOSX,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            };
        }

        public static BuildReport BuildMacOSLocalServer()
        {
            BuildPlayerOptions options = CreateMacOSLocalServerBuildOptions();
            string outputDirectory = Path.GetDirectoryName(options.locationPathName);

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"macOS local server build failed: {report.summary.result}");
            }

            return report;
        }

        public static BuildReport Build()
        {
            BuildPlayerOptions options = CreateBuildOptions();
            string outputDirectory = Path.GetDirectoryName(options.locationPathName);

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Dedicated server build failed: {report.summary.result}");
            }

            return report;
        }

        public static string[] GetServerScenePaths()
        {
            return ServerScenePaths.ToArray();
        }

        public static void ValidateBuildInputs()
        {
            foreach (string scenePath in ServerScenePaths)
            {
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    throw new InvalidOperationException("Dedicated server build contains an empty scene path.");
                }

                if (!File.Exists(scenePath))
                {
                    throw new FileNotFoundException(
                        $"Dedicated server scene does not exist: {scenePath}",
                        scenePath);
                }

                string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);

                if (string.IsNullOrWhiteSpace(sceneGuid))
                {
                    throw new InvalidOperationException($"Dedicated server scene is not imported: {scenePath}");
                }
            }

            string[] activeLocalPreviewRoots = LocalPreviewOnlyAssetRoots
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();

            foreach (string scenePath in ServerScenePaths)
            {
                string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);

                foreach (string dependency in dependencies)
                {
                    string matchingRoot = activeLocalPreviewRoots.FirstOrDefault(
                        root => IsUnderRoot(dependency, root));

                    if (matchingRoot != null)
                    {
                        throw new InvalidOperationException(
                            $"{scenePath} depends on local-preview-only asset {dependency} under {matchingRoot}.");
                    }
                }
            }

            ValidateNetworkPlayerPrefab();
            ValidateFormalNetworkPlayerPrefab();
            ValidateNetworkEnemyPrefab();
            ValidateServerBootSceneNavMesh();
        }

        public static void ValidateServerBootSceneNavMesh()
        {
            if (!File.Exists(ServerBootScenePath))
            {
                throw new FileNotFoundException(
                    $"Dedicated server scene does not exist: {ServerBootScenePath}",
                    ServerBootScenePath);
            }

            string sceneYaml = File.ReadAllText(ServerBootScenePath);
            Require(
                sceneYaml.Contains(ServerBootNavMeshGroundName),
                $"ServerBoot scene is missing {ServerBootNavMeshGroundName}.");
            Require(
                !sceneYaml.Contains("m_NavMeshData: {fileID: 0}"),
                "ServerBoot scene is missing baked NavMesh data.");
        }

        public static void ValidateNetworkPlayerPrefab()
        {
            if (!File.Exists(NetworkPlayerPrefabPath))
            {
                throw new FileNotFoundException(
                    $"P1.5 network player prefab does not exist: {NetworkPlayerPrefabPath}",
                    NetworkPlayerPrefabPath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkPlayerPrefabPath);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"P1.5 network player prefab is not imported: {NetworkPlayerPrefabPath}");
            }

            Require(
                prefab.GetComponent<NetworkObject>() != null,
                "P1.5 network player prefab is missing NetworkObject.");
            Require(
                prefab.GetComponent<NetworkPlayerAvatar>() != null,
                "P1.5 network player prefab is missing NetworkPlayerAvatar.");
        }

        public static void ValidateNetworkEnemyPrefab()
        {
            if (!File.Exists(NetworkEnemyPrefabPath))
            {
                throw new FileNotFoundException(
                    $"P6.0 network enemy prefab does not exist: {NetworkEnemyPrefabPath}",
                    NetworkEnemyPrefabPath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkEnemyPrefabPath);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"P6.0 network enemy prefab is not imported: {NetworkEnemyPrefabPath}");
            }

            Require(
                prefab.GetComponent<NetworkObject>() != null,
                "P6.0 network enemy prefab is missing NetworkObject.");
            Require(
                prefab.GetComponent<NetworkEnemyAvatar>() != null,
                "P6.0 network enemy prefab is missing NetworkEnemyAvatar.");

            NetworkEnemyAvatar avatar = prefab.GetComponent<NetworkEnemyAvatar>();
            EnemyBrain enemyBrain = prefab.GetComponentInChildren<EnemyBrain>(true);
            EnemyStateMachine stateMachine = enemyBrain != null ? enemyBrain.GetComponent<EnemyStateMachine>() : null;
            HealthComponent health = enemyBrain != null ? enemyBrain.GetComponent<HealthComponent>() : null;
            EnemySensing sensing = enemyBrain != null ? enemyBrain.GetComponent<EnemySensing>() : null;
            EnemyMotor motor = enemyBrain != null ? enemyBrain.GetComponent<EnemyMotor>() : null;
            EnemyAttackController attackController = enemyBrain != null ? enemyBrain.GetComponent<EnemyAttackController>() : null;
            NavMeshAgent navMeshAgent = enemyBrain != null ? enemyBrain.GetComponent<NavMeshAgent>() : null;
            NetworkEnemyPresentationBridge presentationBridge =
                enemyBrain != null ? enemyBrain.GetComponent<NetworkEnemyPresentationBridge>() : null;

            Require(enemyBrain != null, "P6.2 network enemy prefab is missing formal EnemyBrain child.");
            Require(stateMachine != null, "P6.2 network enemy prefab is missing formal EnemyStateMachine.");
            Require(health != null, "P6.2 network enemy prefab is missing formal HealthComponent.");
            Require(sensing != null, "P6.2 network enemy prefab is missing formal EnemySensing.");
            Require(motor != null, "P6.2 network enemy prefab is missing formal EnemyMotor.");
            Require(attackController != null, "P6.2 network enemy prefab is missing formal EnemyAttackController.");
            Require(navMeshAgent != null, "P6.2 network enemy prefab is missing formal NavMeshAgent.");
            Require(presentationBridge != null, "P6.2 network enemy prefab is missing NetworkEnemyPresentationBridge.");

            SerializedObject serializedBridge = new SerializedObject(presentationBridge);
            Require(
                serializedBridge.FindProperty("avatar").objectReferenceValue == avatar,
                "P6.2 formal network enemy bridge is not wired to the root NetworkEnemyAvatar.");
            Require(
                serializedBridge.FindProperty("enemyBrain").objectReferenceValue == enemyBrain,
                "P6.2 formal network enemy bridge is not wired to the formal EnemyBrain.");
            Require(
                serializedBridge.FindProperty("stateMachine").objectReferenceValue == stateMachine,
                "P6.2 formal network enemy bridge is not wired to the formal EnemyStateMachine.");
            Require(
                serializedBridge.FindProperty("health").objectReferenceValue == health,
                "P6.2 formal network enemy bridge is not wired to the formal HealthComponent.");
            Require(
                serializedBridge.FindProperty("sensing").objectReferenceValue == sensing,
                "P6.2 formal network enemy bridge is not wired to the formal EnemySensing.");
            Require(
                serializedBridge.FindProperty("motor").objectReferenceValue == motor,
                "P6.2 formal network enemy bridge is not wired to the formal EnemyMotor.");
            Require(
                serializedBridge.FindProperty("attackController").objectReferenceValue == attackController,
                "P6.2 formal network enemy bridge is not wired to the formal EnemyAttackController.");
            Require(
                serializedBridge.FindProperty("navMeshAgent").objectReferenceValue == navMeshAgent,
                "P6.2 formal network enemy bridge is not wired to the formal NavMeshAgent.");

            string[] activeLocalPreviewRoots = LocalPreviewOnlyAssetRoots
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();
            string[] dependencies = AssetDatabase.GetDependencies(NetworkEnemyPrefabPath, true);

            foreach (string dependency in dependencies)
            {
                string matchingRoot = activeLocalPreviewRoots.FirstOrDefault(
                    root => IsUnderRoot(dependency, root));

                if (matchingRoot != null)
                {
                    throw new InvalidOperationException(
                        $"{NetworkEnemyPrefabPath} depends on local-preview-only asset {dependency} under {matchingRoot}.");
                }
            }
        }

        public static void ValidateFormalNetworkPlayerPrefab()
        {
            if (!File.Exists(FormalNetworkPlayerPrefabPath))
            {
                throw new FileNotFoundException(
                    $"P5 formal network player prefab does not exist: {FormalNetworkPlayerPrefabPath}",
                    FormalNetworkPlayerPrefabPath);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FormalNetworkPlayerPrefabPath);

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"P5 formal network player prefab is not imported: {FormalNetworkPlayerPrefabPath}");
            }

            NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
            NetworkPlayerAvatar avatar = prefab.GetComponent<NetworkPlayerAvatar>();
            PlayerCharacter player = prefab.GetComponentInChildren<PlayerCharacter>(true);
            HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
            PlayerStateMachine stateMachine = player != null ? player.GetComponent<PlayerStateMachine>() : null;
            NetworkPlayerDeathStateBridge deathStateBridge =
                player != null ? player.GetComponent<NetworkPlayerDeathStateBridge>() : null;
            NetworkPlayerPresentationBridge presentationBridge =
                player != null ? player.GetComponent<NetworkPlayerPresentationBridge>() : null;

            Require(networkObject != null, "P5 formal network player prefab is missing NetworkObject.");
            Require(avatar != null, "P5 formal network player prefab is missing NetworkPlayerAvatar.");
            Require(player != null, "P5 formal network player prefab is missing formal PlayerCharacter child.");
            Require(health != null, "P5 formal network player prefab is missing formal HealthComponent.");
            Require(stateMachine != null, "P5 formal network player prefab is missing formal PlayerStateMachine.");
            Require(deathStateBridge != null, "P5 formal network player prefab is missing NetworkPlayerDeathStateBridge.");
            Require(presentationBridge != null, "P5.5 formal network player prefab is missing NetworkPlayerPresentationBridge.");

            SerializedObject serializedBridge = new SerializedObject(deathStateBridge);
            Require(
                serializedBridge.FindProperty("avatar").objectReferenceValue == avatar,
                "P5 formal network player death bridge is not wired to the root NetworkPlayerAvatar.");
            Require(
                serializedBridge.FindProperty("health").objectReferenceValue == health,
                "P5 formal network player death bridge is not wired to the formal HealthComponent.");
            Require(
                serializedBridge.FindProperty("stateMachine").objectReferenceValue == stateMachine,
                "P5 formal network player death bridge is not wired to the formal PlayerStateMachine.");

            SerializedObject serializedPresentationBridge = new SerializedObject(presentationBridge);
            Require(
                serializedPresentationBridge.FindProperty("avatar").objectReferenceValue == avatar,
                "P5.5 formal network player presentation bridge is not wired to the root NetworkPlayerAvatar.");
            Require(
                serializedPresentationBridge.FindProperty("player").objectReferenceValue == player,
                "P5.5 formal network player presentation bridge is not wired to the formal PlayerCharacter.");
            Require(
                serializedPresentationBridge.FindProperty("health").objectReferenceValue == health,
                "P5.5 formal network player presentation bridge is not wired to the formal HealthComponent.");
            Require(
                serializedPresentationBridge.FindProperty("stateMachine").objectReferenceValue == stateMachine,
                "P5.5 formal network player presentation bridge is not wired to the formal PlayerStateMachine.");

            string[] activeLocalPreviewRoots = LocalPreviewOnlyAssetRoots
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();
            string[] dependencies = AssetDatabase.GetDependencies(FormalNetworkPlayerPrefabPath, true);

            foreach (string dependency in dependencies)
            {
                string matchingRoot = activeLocalPreviewRoots.FirstOrDefault(
                    root => IsUnderRoot(dependency, root));

                if (matchingRoot != null)
                {
                    throw new InvalidOperationException(
                        $"{FormalNetworkPlayerPrefabPath} depends on local-preview-only asset {dependency} under {matchingRoot}.");
                }
            }
        }

        private static void CreateOrRepairServerBootNavMeshGround()
        {
            GameObject ground = GameObject.Find(ServerBootNavMeshGroundName);

            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = ServerBootNavMeshGroundName;
            }

            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);
            StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(ground);
            GameObjectUtility.SetStaticEditorFlags(
                ground,
                currentFlags | StaticEditorFlags.NavigationStatic);
            EditorUtility.SetDirty(ground);
        }

        private static bool RebuildServerBootSceneNavMesh(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            CreateOrRepairServerBootNavMeshGround();
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);

            if (!File.Exists(scene.path))
            {
                return false;
            }

            EditorSceneManager.SaveScene(scene, scene.path);
            string sceneYaml = File.ReadAllText(scene.path);
            return !string.IsNullOrWhiteSpace(sceneYaml) && !sceneYaml.Contains("m_NavMeshData: {fileID: 0}");
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static bool IsUnderRoot(string assetPath, string rootPath)
        {
            return assetPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Dedicated server smoke verification failed: " + message);
            }
        }
    }
}
