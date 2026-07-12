using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Editor;
using CampusRPG.Multiplayer;
using CampusRPG.Server;
using Unity.Netcode;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRPG.Tests.EditMode
{
    public sealed class DedicatedServerBuildUtilityTests
    {
        [Test]
        public void DedicatedServerBuildOptions_UseLinuxServerSubtargetAndServerBootScene()
        {
            BuildPlayerOptions options = DedicatedServerBuildUtility.CreateBuildOptions();
            BuildPlayerOptions macLocalOptions = DedicatedServerBuildUtility.CreateMacOSLocalServerBuildOptions();

            CollectionAssert.AreEqual(
                new[] { DedicatedServerBuildUtility.ServerBootScenePath },
                options.scenes);
            Assert.AreEqual(BuildTarget.StandaloneLinux64, options.target);
            Assert.AreEqual((int)StandaloneBuildSubtarget.Server, options.subtarget);
            Assert.AreEqual(BuildOptions.None, options.options);
            Assert.AreEqual(
                DedicatedServerBuildUtility.LinuxOutputPath,
                NormalizePath(options.locationPathName));
            Assert.IsTrue(
                NormalizePath(options.locationPathName).StartsWith(
                    DedicatedServerBuildUtility.ServerBuildRoot + "/",
                    System.StringComparison.Ordinal));
            CollectionAssert.AreEqual(
                new[] { DedicatedServerBuildUtility.ServerBootScenePath },
                macLocalOptions.scenes);
            Assert.AreEqual(BuildTarget.StandaloneOSX, macLocalOptions.target);
            Assert.AreEqual((int)StandaloneBuildSubtarget.Player, macLocalOptions.subtarget);
            Assert.AreEqual(
                DedicatedServerBuildUtility.MacLocalServerOutputPath,
                NormalizePath(macLocalOptions.locationPathName));
        }

        [Test]
        public void ServerRuntimeBootstrap_CommandLineSettingsUseDefaultsAndClampValues()
        {
            ServerRuntimeSettings defaults = ServerRuntimeBootstrap.CreateDefaultSettings();
            ServerRuntimeSettings parsed = ServerRuntimeBootstrap.CreateSettingsFromCommandLine(
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
                    "--enable-network-enemy-attack-smoke",
                    "--enable-network-enemy-formal-attack-smoke",
                    "--enable-network-enemy-brain-attack-smoke",
                    "--enable-network-enemy-server-tick",
                    "--disable-network-server",
                    "--health-port=70000",
                    "--health-bind-address",
                    "localhost",
                    "--disable-health-server",
                    "--target-fps",
                    "0",
                    "--tick-rate=999",
                    "--log-interval=-5",
                    "--network-enemy-count=999999",
                    "--network-enemy-server-tick-death-delay-seconds=999999",
                    "--network-enemy-server-tick-damage=999999",
                    "--quit-after-seconds",
                    "3"
                });

            Assert.AreEqual(defaults.Port, ServerRuntimeBootstrap.DefaultPort);
            Assert.IsTrue(defaults.GameplayServerEnabled);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultBindAddress, defaults.BindAddress);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultMaxPlayers, defaults.MaxPlayers);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultRoomId, defaults.RoomId);
            Assert.IsTrue(defaults.NetworkServerEnabled);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultNetworkBindAddress, defaults.NetworkBindAddress);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultNetworkPort, defaults.NetworkPort);
            Assert.AreEqual(
                ServerRuntimeBootstrap.DefaultNetworkPlayerPrefabResourcePath,
                defaults.NetworkPlayerPrefabResourcePath);
            Assert.AreEqual(
                MultiplayerNetworkSessionSettings.DefaultEnemyPrefabResourcePath,
                MultiplayerNetworkSessionSettings.CreateDefaultServerSettings().EnemyPrefabResourcePath);
            Assert.AreEqual(
                MultiplayerNetworkSessionSettings.DefaultNetworkEnemyCount,
                MultiplayerNetworkSessionSettings.CreateDefaultServerSettings().NetworkEnemyCount);
            Assert.IsTrue(defaults.HealthServerEnabled);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultHealthBindAddress, defaults.HealthBindAddress);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultHealthPort, defaults.HealthPort);
            Assert.AreEqual(ServerRuntimeBootstrap.DefaultNetworkEnemyCount, defaults.NetworkEnemyCount);
            Assert.AreEqual(
                ServerRuntimeBootstrap.DefaultNetworkEnemyServerTickDeathDelaySeconds,
                defaults.NetworkEnemyServerTickDeathDelaySeconds);
            Assert.AreEqual(
                ServerRuntimeBootstrap.DefaultNetworkEnemyServerTickDamage,
                defaults.NetworkEnemyServerTickDamage);
            Assert.AreEqual(65535, parsed.Port);
            Assert.IsFalse(parsed.GameplayServerEnabled);
            Assert.AreEqual("localhost", parsed.BindAddress);
            Assert.AreEqual(256, parsed.MaxPlayers);
            Assert.AreEqual("p1-smoke-room", parsed.RoomId);
            Assert.IsFalse(parsed.NetworkServerEnabled);
            Assert.AreEqual("localhost", parsed.NetworkBindAddress);
            Assert.AreEqual(65535, parsed.NetworkPort);
            Assert.AreEqual("Multiplayer/TestAvatar", parsed.NetworkPlayerPrefabResourcePath);
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyAttackSmoke(new[]
            {
                "TYServer.x86_64",
                "--enable-network-enemy-attack-smoke"
            }));
            Assert.IsFalse(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyAttackSmoke(new[]
            {
                "TYServer.x86_64"
            }));
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyFormalAttackSmoke(new[]
            {
                "TYServer.x86_64",
                "--enable-network-enemy-formal-attack-smoke"
            }));
            Assert.IsFalse(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyFormalAttackSmoke(new[]
            {
                "TYServer.x86_64"
            }));
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyBrainAttackSmoke(new[]
            {
                "TYServer.x86_64",
                "--enable-network-enemy-brain-attack-smoke"
            }));
            Assert.IsFalse(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyBrainAttackSmoke(new[]
            {
                "TYServer.x86_64"
            }));
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyBrainChaseAttackSmoke(new[]
            {
                "TYServer.x86_64",
                "--enable-network-enemy-brain-chase-attack-smoke"
            }));
            Assert.IsFalse(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyBrainChaseAttackSmoke(new[]
            {
                "TYServer.x86_64"
            }));
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyGameplayTick(new[]
            {
                "TYServer.x86_64",
                "--enable-network-enemy-server-tick"
            }));
            Assert.IsTrue(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyGameplayTick(new[]
            {
                "TYServer.x86_64",
                "--network-enemy-server-tick"
            }));
            Assert.IsFalse(ServerRuntimeBootstrap.ShouldEnableNetworkEnemyGameplayTick(new[]
            {
                "TYServer.x86_64"
            }));
            Assert.IsFalse(parsed.HealthServerEnabled);
            Assert.AreEqual("localhost", parsed.HealthBindAddress);
            Assert.AreEqual(65535, parsed.HealthPort);
            Assert.AreEqual(1, parsed.TargetFrameRate);
            Assert.AreEqual(120, parsed.TickRate);
            Assert.AreEqual(0, parsed.LogIntervalSeconds);
            Assert.AreEqual(16, parsed.NetworkEnemyCount);
            Assert.AreEqual(86400, parsed.NetworkEnemyServerTickDeathDelaySeconds);
            Assert.AreEqual(10000, parsed.NetworkEnemyServerTickDamage);
            Assert.AreEqual(3, parsed.QuitAfterSeconds);
        }

        [Test]
        public void ServerRuntimeBootstrap_CommandLinePrefabPathOverridesSerializedScenePrefab()
        {
            GameObject defaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.NetworkPlayerPrefabPath);

            Assert.IsNotNull(defaultPrefab);
            Assert.IsTrue(
                ServerRuntimeBootstrap.ShouldUseSerializedNetworkPlayerPrefab(
                    ServerRuntimeBootstrap.DefaultNetworkPlayerPrefabResourcePath,
                    ServerRuntimeBootstrap.DefaultNetworkPlayerPrefabResourcePath,
                    defaultPrefab));
            Assert.IsFalse(
                ServerRuntimeBootstrap.ShouldUseSerializedNetworkPlayerPrefab(
                    DedicatedServerBuildUtility.FormalNetworkPlayerPrefabResourcePath,
                    ServerRuntimeBootstrap.DefaultNetworkPlayerPrefabResourcePath,
                    defaultPrefab));
            Assert.IsFalse(
                ServerRuntimeBootstrap.ShouldUseSerializedNetworkPlayerPrefab(
                    DedicatedServerBuildUtility.FormalNetworkPlayerPrefabResourcePath,
                    DedicatedServerBuildUtility.FormalNetworkPlayerPrefabResourcePath,
                    null));
        }

        [Test]
        public void ServerHealthService_FormatsProbeFriendlyStatusLine()
        {
            string response = ServerHealthService.FormatHealthResponse(
                new ServerHealthSnapshot(
                    ServerHealthService.DefaultServiceName,
                    "ok",
                    7777,
                    true,
                    "0.0.0.0",
                    7777,
                    "combat-test",
                    16,
                    5,
                    2,
                    3,
                    1,
                    8,
                    true,
                    true,
                    true,
                    true,
                    false,
                    "0.0.0.0",
                    "127.0.0.1",
                    7777,
                    16,
                    2,
                    2,
                    true,
                    "0.0.0.0",
                    7778,
                    30,
                    30,
                    12.5,
                    42,
                    64,
                    3,
                    1));

            StringAssert.StartsWith(ServerHealthService.DefaultServiceName, response);
            StringAssert.Contains("status=ok", response);
            StringAssert.Contains("port=7777", response);
            StringAssert.Contains("healthEnabled=true", response);
            StringAssert.Contains("healthBindAddress=0.0.0.0", response);
            StringAssert.Contains("healthPort=7778", response);
            StringAssert.Contains("connectionsAccepted=3", response);
            StringAssert.Contains("activeConnections=1", response);
            StringAssert.Contains("gameplayEnabled=true", response);
            StringAssert.Contains("gameplayPort=7777", response);
            StringAssert.Contains("room=combat-test", response);
            StringAssert.Contains("gameConnectionsAccepted=5", response);
            StringAssert.Contains("gameActivePlayers=1", response);
            StringAssert.Contains("gameMessagesReceived=8", response);
            StringAssert.Contains("networkEnabled=true", response);
            StringAssert.Contains("networkStarted=true", response);
            StringAssert.Contains("networkListening=true", response);
            StringAssert.Contains("networkPort=7777", response);
            StringAssert.Contains("networkConnectedClients=2", response);
            StringAssert.Contains("networkSpawnedPlayers=2", response);
        }

        [Test]
        public void ServerGameConnectionService_FormatsProbeFriendlyProtocolLines()
        {
            string welcome = ServerGameConnectionService.FormatWelcome(12, "combat-test", 16);
            string joined = ServerGameConnectionService.FormatJoined(
                new PlayerSessionSnapshot(12, 12, "Player One", "combat-test", true),
                1,
                16);
            string pong = ServerGameConnectionService.FormatPong(
                new PlayerSessionSnapshot(12, 12, "PlayerOne", "combat-test", true),
                123456);
            string room = ServerGameConnectionService.FormatRoomState(
                new ServerGameSnapshot(true, "0.0.0.0", 7777, "combat-test", 16, 2, 1, 1, 1, 3));

            StringAssert.StartsWith(ServerGameConnectionService.DefaultProtocolName, welcome);
            StringAssert.Contains("protocol=1", welcome);
            StringAssert.Contains("room=combat-test", welcome);
            StringAssert.Contains("JOINED", joined);
            StringAssert.Contains("playerName=Player_One", joined);
            StringAssert.Contains("PONG", pong);
            StringAssert.Contains("joined=true", pong);
            StringAssert.Contains("ROOM", room);
            StringAssert.Contains("players=1", room);
        }

        [Test]
        public void ServerGameConnectionService_AcceptsHelloPingAndStateOverTcp()
        {
            using (ServerGameConnectionService service = new ServerGameConnectionService("127.0.0.1", 0, "editmode-room", 2))
            {
                service.Start();

                using (TcpClient client = new TcpClient())
                {
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;
                    client.Connect(IPAddress.Loopback, service.BoundPort);

                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        writer.NewLine = "\n";
                        writer.AutoFlush = true;

                        string welcome = reader.ReadLine();
                        StringAssert.Contains("TY_NEW_GAME", welcome);
                        StringAssert.Contains("room=editmode-room", welcome);

                        writer.WriteLine("HELLO playerName=EditModeBot");
                        string joined = reader.ReadLine();
                        StringAssert.Contains("JOINED", joined);
                        StringAssert.Contains("playerName=EditModeBot", joined);
                        StringAssert.Contains("players=1", joined);

                        writer.WriteLine("PING");
                        string pong = reader.ReadLine();
                        StringAssert.Contains("PONG", pong);
                        StringAssert.Contains("joined=true", pong);

                        writer.WriteLine("STATE");
                        string state = reader.ReadLine();
                        StringAssert.Contains("ROOM", state);
                        StringAssert.Contains("players=1", state);

                        writer.WriteLine("QUIT");
                        string bye = reader.ReadLine();
                        StringAssert.Contains("BYE", bye);
                    }
                }
            }
        }

        [Test]
        public void ServerHealthService_ParseBindAddressHandlesDeploymentAliases()
        {
            Assert.AreEqual(IPAddress.Any, ServerHealthService.ParseBindAddress("0.0.0.0"));
            Assert.AreEqual(IPAddress.Any, ServerHealthService.ParseBindAddress("*"));
            Assert.AreEqual(IPAddress.Loopback, ServerHealthService.ParseBindAddress("localhost"));
            Assert.Throws<FormatException>(() => ServerHealthService.ParseBindAddress("not-an-ip-address"));
            Assert.AreEqual(IPAddress.Any, ServerGameConnectionService.ParseBindAddress("0.0.0.0"));
            Assert.AreEqual(IPAddress.Any, ServerGameConnectionService.ParseBindAddress("*"));
            Assert.AreEqual(IPAddress.Loopback, ServerGameConnectionService.ParseBindAddress("localhost"));
            Assert.Throws<FormatException>(() => ServerGameConnectionService.ParseBindAddress("not-an-ip-address"));
        }

        [Test]
        public void ServerBootScene_HasOnlyServerRuntimeBootstrapNoClientCameraAudioOrInput()
        {
            EditorSceneManager.OpenScene(DedicatedServerBuildUtility.ServerBootScenePath);

            ServerRuntimeBootstrap[] bootstraps = UnityEngine.Object.FindObjectsByType<ServerRuntimeBootstrap>();
            UnityEngine.Camera[] cameras = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>();
            AudioListener[] audioListeners = UnityEngine.Object.FindObjectsByType<AudioListener>();

            Assert.AreEqual(1, bootstraps.Length);
            Assert.AreEqual(0, cameras.Length);
            Assert.AreEqual(0, audioListeners.Length);

            string[] dependencies = AssetDatabase.GetDependencies(
                DedicatedServerBuildUtility.ServerBootScenePath,
                true);

            CollectionAssert.DoesNotContain(dependencies, "Assets/_Game/Scripts/Runtime/Input/InputReader.cs");
        }

        [Test]
        public void ServerBootScene_ContainsBakedNavMeshForEnemyChaseSmoke()
        {
            EditorSceneManager.OpenScene(DedicatedServerBuildUtility.ServerBootScenePath);
            DedicatedServerBuildUtility.ValidateServerBootSceneNavMesh();

            GameObject ground = GameObject.Find(DedicatedServerBuildUtility.ServerBootNavMeshGroundName);

            Assert.IsNotNull(ground);
            Assert.AreEqual(Vector3.zero, ground.transform.position);
            Assert.AreEqual(new Vector3(2f, 1f, 2f), ground.transform.localScale);
            Assert.IsTrue(
                (GameObjectUtility.GetStaticEditorFlags(ground) & StaticEditorFlags.NavigationStatic) != 0);

            string sceneYaml = File.ReadAllText(DedicatedServerBuildUtility.ServerBootScenePath);
            StringAssert.DoesNotContain("m_NavMeshData: {fileID: 0}", sceneYaml);
        }

        [Test]
        public void DedicatedServerNetworkPlayerPrefab_HasNgoPlayerComponents()
        {
            DedicatedServerBuildUtility.ValidateNetworkPlayerPrefab();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.NetworkPlayerPrefabPath);

            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<NetworkObject>());
            Assert.IsNotNull(prefab.GetComponent<NetworkPlayerAvatar>());
        }

        [Test]
        public void DedicatedServerNetworkEnemyPrefab_HasNgoEnemyComponents()
        {
            DedicatedServerBuildUtility.ValidateNetworkEnemyPrefab();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.NetworkEnemyPrefabPath);

            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<NetworkObject>());
            Assert.IsNotNull(prefab.GetComponent<NetworkEnemyAvatar>());
            Assert.IsNotNull(prefab.GetComponentInChildren<EnemyBrain>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<NetworkEnemyPresentationBridge>(true));
            Assert.IsNotNull(prefab.GetComponentInChildren<NavMeshAgent>(true));
        }

        [Test]
        public void FormalNetworkEnemyPrefab_EmbedsCombatTestEnemyAndSuppressesClientDriver()
        {
            DedicatedServerBuildUtility.ValidateNetworkEnemyPrefab();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.NetworkEnemyPrefabPath);

            Assert.IsNotNull(prefab);

            NetworkEnemyAvatar avatar = prefab.GetComponent<NetworkEnemyAvatar>();
            EnemyBrain enemyBrain = prefab.GetComponentInChildren<EnemyBrain>(true);
            EnemyStateMachine stateMachine = enemyBrain != null ? enemyBrain.GetComponent<EnemyStateMachine>() : null;
            HealthComponent health = enemyBrain != null ? enemyBrain.GetComponent<HealthComponent>() : null;
            EnemySensing sensing = enemyBrain != null ? enemyBrain.GetComponent<EnemySensing>() : null;
            EnemyMotor motor = enemyBrain != null ? enemyBrain.GetComponent<EnemyMotor>() : null;
            EnemyAttackController attackController =
                enemyBrain != null ? enemyBrain.GetComponent<EnemyAttackController>() : null;
            NavMeshAgent navMeshAgent = enemyBrain != null ? enemyBrain.GetComponent<NavMeshAgent>() : null;
            NetworkEnemyPresentationBridge presentationBridge =
                enemyBrain != null ? enemyBrain.GetComponent<NetworkEnemyPresentationBridge>() : null;

            Assert.IsNotNull(avatar);
            Assert.IsNotNull(enemyBrain);
            Assert.IsNotNull(stateMachine);
            Assert.IsNotNull(health);
            Assert.IsNotNull(sensing);
            Assert.IsNotNull(motor);
            Assert.IsNotNull(attackController);
            Assert.IsNotNull(navMeshAgent);
            Assert.IsNotNull(presentationBridge);
            Assert.AreSame(prefab.transform, enemyBrain.transform.parent);

            SerializedObject serializedBridge = new SerializedObject(presentationBridge);
            Assert.AreSame(avatar, serializedBridge.FindProperty("avatar").objectReferenceValue);
            Assert.AreSame(enemyBrain, serializedBridge.FindProperty("enemyBrain").objectReferenceValue);
            Assert.AreSame(stateMachine, serializedBridge.FindProperty("stateMachine").objectReferenceValue);
            Assert.AreSame(health, serializedBridge.FindProperty("health").objectReferenceValue);
            Assert.AreSame(sensing, serializedBridge.FindProperty("sensing").objectReferenceValue);
            Assert.AreSame(motor, serializedBridge.FindProperty("motor").objectReferenceValue);
            Assert.AreSame(attackController, serializedBridge.FindProperty("attackController").objectReferenceValue);
            Assert.AreSame(navMeshAgent, serializedBridge.FindProperty("navMeshAgent").objectReferenceValue);

            CollectionAssert.IsEmpty(
                AssetDatabase.GetDependencies(DedicatedServerBuildUtility.NetworkEnemyPrefabPath, true)
                    .Where(IsLocalPreviewOnlyDependency),
                "P6.2 formal network enemy prefab must stay safe for server Resources packaging.");

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            try
            {
                EnemyBrain instanceBrain = instance.GetComponentInChildren<EnemyBrain>(true);
                EnemyStateMachine instanceStateMachine = instanceBrain.GetComponent<EnemyStateMachine>();
                HealthComponent instanceHealth = instanceBrain.GetComponent<HealthComponent>();
                EnemySensing instanceSensing = instanceBrain.GetComponent<EnemySensing>();
                EnemyMotor instanceMotor = instanceBrain.GetComponent<EnemyMotor>();
                EnemyAttackController instanceAttackController = instanceBrain.GetComponent<EnemyAttackController>();
                NavMeshAgent instanceNavMeshAgent = instanceBrain.GetComponent<NavMeshAgent>();
                NetworkEnemyPresentationBridge instanceBridge =
                    instanceBrain.GetComponent<NetworkEnemyPresentationBridge>();

                instanceStateMachine.Initialize(instanceBrain);
                Assert.IsInstanceOf<EnemyIdleGuardState>(instanceStateMachine.CurrentState);

                Assert.IsTrue(instanceBridge.ApplyEnemyDriverAuthority(false));
                Assert.IsFalse(instanceBrain.enabled);
                Assert.IsFalse(instanceSensing.enabled);
                Assert.IsFalse(instanceMotor.enabled);
                Assert.IsFalse(instanceAttackController.enabled);
                Assert.IsFalse(instanceNavMeshAgent.enabled);
                Assert.IsTrue(instanceBridge.LocalEnemyDriverSuppressed);

                bool mirroredHealth = instanceBridge.ApplyAuthoritativeState(50, false);

                Assert.IsTrue(mirroredHealth);
                Assert.AreEqual(50f, instanceHealth.CurrentValue, 0.0001f);
                Assert.IsFalse(instanceHealth.IsDead);

                Assert.AreEqual(
                    NetworkEnemyAvatar.LightAttackPresentationCode,
                    NetworkEnemyAvatar.ResolveAttackPresentationCode(NetworkServerAttackProfile.Light01AttackId));
                Assert.AreEqual(
                    NetworkServerAttackProfile.Light01AttackId,
                    NetworkEnemyAvatar.ResolveAttackPresentationId(NetworkEnemyAvatar.LightAttackPresentationCode));
                Assert.IsFalse(instanceBridge.ApplyAuthoritativeAttackPresentation(
                    1u,
                    NetworkEnemyAvatar.NoAttackPresentationCode,
                    false));
                Assert.IsFalse(instanceBridge.HasObservedFormalAttackPresentation);

                bool appliedAttack = instanceBridge.ApplyAuthoritativeAttackPresentation(
                    2u,
                    NetworkEnemyAvatar.LightAttackPresentationCode,
                    false);

                Assert.IsTrue(appliedAttack);
                Assert.IsInstanceOf<EnemyAttackState>(instanceStateMachine.CurrentState);
                Assert.IsTrue(instanceBridge.IsFormalAttackStateActive);
                Assert.IsTrue(instanceBridge.HasObservedFormalAttackPresentation);
                Assert.IsFalse(instanceBridge.ApplyAuthoritativeAttackPresentation(
                    2u,
                    NetworkEnemyAvatar.LightAttackPresentationCode,
                    false));

                bool appliedDeath = instanceBridge.ApplyAuthoritativeState(0, true);

                Assert.IsTrue(appliedDeath);
                Assert.AreEqual(0f, instanceHealth.CurrentValue, 0.0001f);
                Assert.IsTrue(instanceHealth.IsDead);
                Assert.IsInstanceOf<EnemyDeathState>(instanceStateMachine.CurrentState);
                Assert.IsTrue(instanceBridge.IsFormalDeathStateActive);

                Assert.IsFalse(instanceBridge.ApplyEnemyDriverAuthority(true));
                Assert.IsTrue(instanceBrain.enabled);
                Assert.IsTrue(instanceSensing.enabled);
                Assert.IsTrue(instanceMotor.enabled);
                Assert.IsTrue(instanceAttackController.enabled);
                Assert.IsTrue(instanceNavMeshAgent.enabled);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void FormalNetworkPlayerPrefab_EmbedsCombatTestPlayerAndDeathBridge()
        {
            DedicatedServerBuildUtility.ValidateFormalNetworkPlayerPrefab();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.FormalNetworkPlayerPrefabPath);

            Assert.IsNotNull(prefab);

            NetworkPlayerAvatar avatar = prefab.GetComponent<NetworkPlayerAvatar>();
            PlayerCharacter player = prefab.GetComponentInChildren<PlayerCharacter>(true);
            HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
            PlayerStateMachine stateMachine = player != null ? player.GetComponent<PlayerStateMachine>() : null;
            NetworkPlayerDeathStateBridge deathStateBridge =
                player != null ? player.GetComponent<NetworkPlayerDeathStateBridge>() : null;
            NetworkPlayerPresentationBridge presentationBridge =
                player != null ? player.GetComponent<NetworkPlayerPresentationBridge>() : null;

            Assert.IsNotNull(prefab.GetComponent<NetworkObject>());
            Assert.IsNotNull(avatar);
            Assert.IsNotNull(player);
            Assert.IsNotNull(health);
            Assert.IsNotNull(stateMachine);
            Assert.IsNotNull(deathStateBridge);
            Assert.IsNotNull(presentationBridge);
            Assert.AreSame(prefab.transform, player.transform.parent);

            SerializedObject serializedBridge = new SerializedObject(deathStateBridge);
            Assert.AreSame(avatar, serializedBridge.FindProperty("avatar").objectReferenceValue);
            Assert.AreSame(health, serializedBridge.FindProperty("health").objectReferenceValue);
            Assert.AreSame(stateMachine, serializedBridge.FindProperty("stateMachine").objectReferenceValue);

            SerializedObject serializedPresentationBridge = new SerializedObject(presentationBridge);
            Assert.AreSame(avatar, serializedPresentationBridge.FindProperty("avatar").objectReferenceValue);
            Assert.AreSame(player, serializedPresentationBridge.FindProperty("player").objectReferenceValue);
            Assert.AreSame(health, serializedPresentationBridge.FindProperty("health").objectReferenceValue);
            Assert.AreSame(stateMachine, serializedPresentationBridge.FindProperty("stateMachine").objectReferenceValue);

            CollectionAssert.IsEmpty(
                AssetDatabase.GetDependencies(DedicatedServerBuildUtility.FormalNetworkPlayerPrefabPath, true)
                    .Where(IsLocalPreviewOnlyDependency),
                "P5 formal network player prefab must stay safe for server Resources packaging.");
        }

        [Test]
        public void FormalNetworkPlayerPrefab_PresentationBridgeDrivesFormalPlayerState()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DedicatedServerBuildUtility.FormalNetworkPlayerPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            try
            {
                PlayerCharacter player = instance.GetComponentInChildren<PlayerCharacter>(true);
                HealthComponent health = player.GetComponent<HealthComponent>();
                PlayerStateMachine stateMachine = player.GetComponent<PlayerStateMachine>();
                NetworkPlayerPresentationBridge presentationBridge =
                    player.GetComponent<NetworkPlayerPresentationBridge>();

                stateMachine.Initialize(player);
                instance.transform.SetPositionAndRotation(new Vector3(3f, 0f, 5f), Quaternion.Euler(0f, 90f, 0f));
                player.transform.localPosition = new Vector3(2f, 0f, 1f);
                player.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

                NetworkPlayerPresentationBridge.ConstrainPresentationTransform(player.transform);

                Assert.AreEqual(Vector3.zero, player.transform.localPosition);
                Assert.AreEqual(Quaternion.identity, player.transform.localRotation);
                Assert.AreEqual(new Vector3(3f, 0f, 5f), player.transform.position);
                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);

                Assert.AreEqual(
                    NetworkPlayerAvatar.LightAttackPresentationCode,
                    NetworkPlayerAvatar.ResolveAttackPresentationCode(NetworkServerAttackProfile.Light01AttackId));
                Assert.AreEqual(
                    NetworkServerAttackProfile.Light01AttackId,
                    NetworkPlayerAvatar.ResolveAttackPresentationId(NetworkPlayerAvatar.LightAttackPresentationCode));
                Assert.IsTrue(NetworkPlayerAvatar.TryResolveAttackPresentationRequest(
                    NetworkPlayerAvatar.LightAttackPresentationCode,
                    out PlayerAttackRequest request));
                Assert.AreEqual(PlayerAttackRequest.Light, request);

                bool ignoredAttack = presentationBridge.ApplyAuthoritativeAttackPresentation(
                    1u,
                    NetworkPlayerAvatar.NoAttackPresentationCode,
                    false);

                Assert.IsFalse(ignoredAttack);
                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsFalse(presentationBridge.HasObservedFormalAttackPresentation);

                bool appliedAttack = presentationBridge.ApplyAuthoritativeAttackPresentation(
                    2u,
                    NetworkPlayerAvatar.LightAttackPresentationCode,
                    false);

                Assert.IsTrue(appliedAttack);
                Assert.IsInstanceOf<PlayerAttackState>(stateMachine.CurrentState);
                Assert.IsTrue(presentationBridge.IsFormalAttackStateActive);
                Assert.IsTrue(presentationBridge.HasObservedFormalAttackPresentation);

                stateMachine.SwitchToLocomotion();

                bool mirroredHealth = NetworkPlayerPresentationBridge.ApplyAuthoritativePresentationState(
                    health,
                    stateMachine,
                    instance.transform.position,
                    instance,
                    75,
                    false);

                Assert.IsTrue(mirroredHealth);
                Assert.AreEqual(75f, health.CurrentValue, 0.0001f);
                Assert.IsFalse(health.IsDead);
                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);
                Assert.IsFalse(presentationBridge.HasObservedFormalHitReaction);

                bool appliedHit = presentationBridge.ApplyAuthoritativeState(50, false, true);

                Assert.IsTrue(appliedHit);
                Assert.AreEqual(50f, health.CurrentValue, 0.0001f);
                Assert.IsFalse(health.IsDead);
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);
                Assert.IsTrue(presentationBridge.IsFormalHitStateActive);
                Assert.IsTrue(presentationBridge.HasObservedFormalHitReaction);

                bool suppressedAttackDuringHit = presentationBridge.ApplyAuthoritativeAttackPresentation(
                    3u,
                    NetworkPlayerAvatar.LightAttackPresentationCode,
                    false);

                Assert.IsFalse(suppressedAttackDuringHit);
                Assert.IsInstanceOf<PlayerHitState>(stateMachine.CurrentState);

                bool applied = presentationBridge.ApplyAuthoritativeState(0, true);

                Assert.IsTrue(applied);
                Assert.IsTrue(health.IsDead);
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);
                Assert.IsTrue(presentationBridge.IsFormalDeathStateActive);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void NetworkPlayerAvatar_BuildSpawnPositionPairsConsecutiveClients()
        {
            Assert.AreEqual(new Vector3(1f, 0f, 0f), NetworkPlayerAvatar.BuildSpawnPosition(0));
            Assert.AreEqual(new Vector3(-1f, 0f, 0f), NetworkPlayerAvatar.BuildSpawnPosition(1));
            Assert.AreEqual(new Vector3(1f, 0f, 0f), NetworkPlayerAvatar.BuildSpawnPosition(2));
            Assert.AreEqual(new Vector3(-1f, 0f, 2f), NetworkPlayerAvatar.BuildSpawnPosition(3));
            Assert.AreEqual(new Vector3(1f, 0f, 2f), NetworkPlayerAvatar.BuildSpawnPosition(4));
        }

        [Test]
        public void NetworkPlayerAvatar_BuildSpawnYawFacesPairedClientsTowardEachOther()
        {
            Assert.AreEqual(90f, NetworkPlayerAvatar.BuildSpawnYaw(1));
            Assert.AreEqual(-90f, NetworkPlayerAvatar.BuildSpawnYaw(2));
            Assert.AreEqual(90f, NetworkPlayerAvatar.BuildSpawnYaw(3));
            Assert.AreEqual(-90f, NetworkPlayerAvatar.BuildSpawnYaw(4));
        }

        [Test]
        public void MultiplayerCommandLineUtility_ReadsInvariantFloatArguments()
        {
            string[] args =
            {
                "TY_NEW",
                "--smoke-move-x",
                "0.5",
                "--smoke-move-y=-0.25"
            };

            Assert.AreEqual(
                0.5f,
                MultiplayerCommandLineUtility.ReadFloatArgument(args, 0f, "--smoke-move-x"),
                0.0001f);
            Assert.AreEqual(
                -0.25f,
                MultiplayerCommandLineUtility.ReadFloatArgument(args, 0f, "--smoke-move-y"),
                0.0001f);
            Assert.AreEqual(
                0.75f,
                MultiplayerCommandLineUtility.ReadFloatArgument(args, 0.75f, "--missing-float"),
                0.0001f);
        }

        [Test]
        public void NetworkPlayerAvatar_ConfigureSmokeMoveInputClampsMagnitude()
        {
            try
            {
                NetworkPlayerAvatar.ConfigureSmokeMoveInput(new Vector2(3f, 4f));

                Assert.AreEqual(1f, NetworkPlayerAvatar.SmokeMoveInput.magnitude, 0.0001f);
                Assert.AreEqual(0.6f, NetworkPlayerAvatar.SmokeMoveInput.x, 0.0001f);
                Assert.AreEqual(0.8f, NetworkPlayerAvatar.SmokeMoveInput.y, 0.0001f);
            }
            finally
            {
                NetworkPlayerAvatar.ConfigureSmokeMoveInput(Vector2.zero);
            }
        }

        [Test]
        public void NetworkPlayerAvatar_ClampMoveInputClampsMagnitude()
        {
            Vector2 clamped = NetworkPlayerAvatar.ClampMoveInput(new Vector2(6f, 8f));

            Assert.AreEqual(1f, clamped.magnitude, 0.0001f);
            Assert.AreEqual(0.6f, clamped.x, 0.0001f);
            Assert.AreEqual(0.8f, clamped.y, 0.0001f);
        }

        [Test]
        public void NetworkPlayerAvatar_ResolveServerAttackDamageIgnoresClientMagnitude()
        {
            Assert.AreEqual(0, NetworkPlayerAvatar.ResolveServerAttackDamage(-10));
            Assert.AreEqual(25, NetworkPlayerAvatar.ResolveServerAttackDamage(12));
            Assert.AreEqual(25, NetworkPlayerAvatar.ResolveServerAttackDamage(9999));
            Assert.AreEqual(0, NetworkPlayerAvatar.ResolveServerAttackDamage("Unknown_Attack", 9999));
        }

        [Test]
        public void NetworkPlayerAvatar_ResolveServerDeathStateUsesZeroHealthThreshold()
        {
            Assert.IsFalse(NetworkPlayerAvatar.ResolveServerDeathState(100));
            Assert.IsFalse(NetworkPlayerAvatar.ResolveServerDeathState(1));
            Assert.IsTrue(NetworkPlayerAvatar.ResolveServerDeathState(0));
            Assert.IsTrue(NetworkPlayerAvatar.ResolveServerDeathState(-5));
        }

        [Test]
        public void NetworkPlayerAvatar_ApplyAuthoritativeFormalHealthMirrorsServerDeathForAiTargeting()
        {
            GameObject targetObject = null;

            try
            {
                targetObject = new GameObject("P6_15_FormalNetworkPlayerTarget");
                HealthComponent health = targetObject.AddComponent<HealthComponent>();
                health.SetMax(100f, refillCurrent: true);

                Assert.IsTrue(NetworkPlayerAvatar.ApplyAuthoritativeFormalHealth(
                    health,
                    75,
                    authoritativeDead: false));
                Assert.AreEqual(75f, health.CurrentValue, 0.0001f);
                Assert.IsFalse(health.IsDead);

                Assert.IsTrue(NetworkPlayerAvatar.ApplyAuthoritativeFormalHealth(
                    health,
                    0,
                    authoritativeDead: true));
                Assert.AreEqual(0f, health.CurrentValue, 0.0001f);
                Assert.IsTrue(health.IsDead);

                Assert.IsFalse(NetworkPlayerAvatar.ApplyAuthoritativeFormalHealth(
                    health,
                    0,
                    authoritativeDead: true));
                Assert.IsFalse(NetworkPlayerAvatar.ApplyAuthoritativeFormalHealth(
                    null,
                    100,
                    authoritativeDead: false));
            }
            finally
            {
                if (targetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(targetObject);
                }
            }
        }

        [Test]
        public void NetworkEnemyAvatar_UsesServerOwnedSpawnAndDeathFacts()
        {
            Assert.AreEqual(new Vector3(0f, 0f, 3f), NetworkEnemyAvatar.BuildSpawnPosition());
            Assert.AreEqual(new Vector3(2f, 0f, 3f), NetworkEnemyAvatar.BuildSpawnPosition(1));
            Assert.AreEqual(new Vector3(-1f, 0f, 1.25f), NetworkEnemyAvatar.BuildFormalAttackSmokeSpawnPosition());
            Assert.AreEqual(new Vector3(1f, 0f, 1.25f), NetworkEnemyAvatar.BuildFormalAttackSmokeSpawnPosition(1));
            Assert.AreEqual(new Vector3(-1f, 0f, 5f), NetworkEnemyAvatar.BuildFormalBrainChaseSmokeSpawnPosition());
            Assert.AreEqual(new Vector3(1f, 0f, 5f), NetworkEnemyAvatar.BuildFormalBrainChaseSmokeSpawnPosition(1));
            Assert.IsFalse(NetworkEnemyAvatar.ResolveServerDeathState(50));
            Assert.IsFalse(NetworkEnemyAvatar.ResolveServerDeathState(1));
            Assert.IsTrue(NetworkEnemyAvatar.ResolveServerDeathState(0));
            Assert.IsTrue(NetworkEnemyAvatar.ResolveServerDeathState(-5));
            Assert.IsTrue(NetworkEnemyAvatar.ShouldAcceptServerBrainAttackCommit(
                isServerGameplayTickCommit: true,
                isBrainSmokeCommit: false,
                hasAppliedNetworkEnemyAttack: true,
                enemyIsDead: false));
            Assert.IsTrue(NetworkEnemyAvatar.ShouldAcceptServerBrainAttackCommit(
                isServerGameplayTickCommit: false,
                isBrainSmokeCommit: true,
                hasAppliedNetworkEnemyAttack: false,
                enemyIsDead: false));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldAcceptServerBrainAttackCommit(
                isServerGameplayTickCommit: false,
                isBrainSmokeCommit: true,
                hasAppliedNetworkEnemyAttack: true,
                enemyIsDead: false));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldAcceptServerBrainAttackCommit(
                isServerGameplayTickCommit: true,
                isBrainSmokeCommit: false,
                hasAppliedNetworkEnemyAttack: false,
                enemyIsDead: true));
            Assert.IsTrue(NetworkEnemyAvatar.ShouldAcceptServerGameplayTickFallbackTarget(
                hasNetworkTarget: true,
                targetAlive: true));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldAcceptServerGameplayTickFallbackTarget(
                hasNetworkTarget: false,
                targetAlive: true));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldAcceptServerGameplayTickFallbackTarget(
                hasNetworkTarget: true,
                targetAlive: false));
            Assert.IsTrue(NetworkEnemyAvatar.ShouldPreferRetainedServerTickTarget(
                isServerGameplayTickCommit: true,
                hasRetainedTarget: true,
                retainedTargetDead: false));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldPreferRetainedServerTickTarget(
                isServerGameplayTickCommit: false,
                hasRetainedTarget: true,
                retainedTargetDead: false));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldPreferRetainedServerTickTarget(
                isServerGameplayTickCommit: true,
                hasRetainedTarget: false,
                retainedTargetDead: false));
            Assert.IsFalse(NetworkEnemyAvatar.ShouldPreferRetainedServerTickTarget(
                isServerGameplayTickCommit: true,
                hasRetainedTarget: true,
                retainedTargetDead: true));
            Assert.AreEqual(25, NetworkEnemyAvatar.ServerEnemyAttackDamage);
            Assert.AreEqual(25, NetworkEnemyAvatar.ServerEnemyGameplayTickDamage);
            Assert.AreEqual(4f, NetworkEnemyAvatar.ServerEnemyAttackRange, 0.0001f);

            try
            {
                NetworkEnemyAvatar.ConfigureServerEnemyAttackSmoke(true, 1.25f);

                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);
                Assert.AreEqual(1.25f, NetworkEnemyAvatar.ServerEnemyAttackSmokeDelaySeconds, 0.0001f);

                NetworkEnemyAvatar.ConfigureServerFormalEnemyAttackSmoke(true);

                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerFormalEnemyAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerBrainEnemyAttackSmokeEnabled);

                NetworkEnemyAvatar.ConfigureServerBrainEnemyAttackSmoke(true);

                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerFormalEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerBrainEnemyAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerBrainEnemyChaseAttackSmokeEnabled);

                NetworkEnemyAvatar.ConfigureServerBrainEnemyChaseAttackSmoke(true);

                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerFormalEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerBrainEnemyAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerBrainEnemyChaseAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerEnemyGameplayTickEnabled);

                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTick(true);

                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyGameplayTickEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);

                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDeathDelay(24f);

                Assert.AreEqual(24f, NetworkEnemyAvatar.ServerEnemyGameplayTickDeathDelaySeconds, 0.0001f);

                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDamage(10);

                Assert.AreEqual(10, NetworkEnemyAvatar.ServerEnemyGameplayTickDamage);

                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDamage(0);

                Assert.AreEqual(1, NetworkEnemyAvatar.ServerEnemyGameplayTickDamage);

                NetworkEnemyAvatar.ConfigureServerEnemyAttackSmoke(false);

                Assert.IsFalse(NetworkEnemyAvatar.ServerEnemyAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerFormalEnemyAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerBrainEnemyAttackSmokeEnabled);
                Assert.IsFalse(NetworkEnemyAvatar.ServerBrainEnemyChaseAttackSmokeEnabled);
                Assert.IsTrue(NetworkEnemyAvatar.ServerEnemyGameplayTickEnabled);

                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTick(false);

                Assert.IsFalse(NetworkEnemyAvatar.ServerEnemyGameplayTickEnabled);
                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDeathDelay(
                    NetworkEnemyAvatar.DefaultServerEnemyGameplayTickDeathDelaySeconds);
                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDamage(
                    NetworkEnemyAvatar.DefaultServerEnemyAttackDamage);
            }
            finally
            {
                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDeathDelay(
                    NetworkEnemyAvatar.DefaultServerEnemyGameplayTickDeathDelaySeconds);
                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDamage(
                    NetworkEnemyAvatar.DefaultServerEnemyAttackDamage);
                NetworkEnemyAvatar.ConfigureServerEnemyGameplayTick(false);
                NetworkEnemyAvatar.ConfigureServerBrainEnemyChaseAttackSmoke(false);
                NetworkEnemyAvatar.ConfigureServerBrainEnemyAttackSmoke(false);
                NetworkEnemyAvatar.ConfigureServerFormalEnemyAttackSmoke(false);
                NetworkEnemyAvatar.ConfigureServerEnemyAttackSmoke(false);
            }
        }

        [Test]
        public void NetworkEnemyAvatar_FacesServerBrainAttackTargetBeforeTickCommit()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;

            try
            {
                enemyObject = new GameObject("P6_22_ServerTickEnemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;

                targetObject = new GameObject("P6_22_ServerTickTarget");
                targetObject.transform.position = new Vector3(1f, 0f, 0f);

                Assert.IsTrue(NetworkEnemyAvatar.TryFaceServerBrainAttackTarget(enemyObject.transform, targetObject.transform));
                Assert.AreEqual(1f, enemyObject.transform.forward.x, 0.0001f);
                Assert.AreEqual(0f, enemyObject.transform.forward.z, 0.0001f);

                targetObject.transform.position = enemyObject.transform.position;

                Assert.IsFalse(NetworkEnemyAvatar.TryFaceServerBrainAttackTarget(enemyObject.transform, targetObject.transform));
            }
            finally
            {
                if (enemyObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemyObject);
                }

                if (targetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(targetObject);
                }
            }
        }

        [Test]
        public void EnemyAttackController_ServerAuthoritativeCommitConsumesCooldown()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("P6_22_ServerAuthoritativeEnemy");
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("P6_22_ServerAuthoritativeTarget");
                targetObject.transform.position = new Vector3(0f, 0f, 1f);

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                InvokeAwake(attackController);

                Assert.IsTrue(attackController.CanAttack(archetype.AttackCooldown));

                attackController.RegisterServerAuthoritativeCommit(targetObject.transform, archetype);

                Assert.IsFalse(attackController.CanAttack(archetype.AttackCooldown));

                attackController.Tick(archetype.AttackCooldown);

                Assert.IsTrue(attackController.CanAttack(archetype.AttackCooldown));
            }
            finally
            {
                if (archetype != null)
                {
                    UnityEngine.Object.DestroyImmediate(archetype);
                }

                if (enemyObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemyObject);
                }

                if (targetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(targetObject);
                }
            }
        }

        [Test]
        public void EnemyAttackController_RaisesCommittedAttackOnSuccessfulTryAttack()
        {
            GameObject enemyObject = null;
            GameObject targetObject = null;
            EnemyArchetypeSO archetype = null;

            try
            {
                enemyObject = new GameObject("P6_6_ServerBrainEnemy");
                enemyObject.transform.position = Vector3.zero;
                enemyObject.transform.rotation = Quaternion.identity;
                enemyObject.AddComponent<BoxCollider>();
                EnemyAttackController attackController = enemyObject.AddComponent<EnemyAttackController>();

                targetObject = new GameObject("P6_6_FormalNetworkPlayerTarget");
                targetObject.transform.position = new Vector3(0f, 0f, 1f);
                targetObject.AddComponent<BoxCollider>();
                HealthComponent targetHealth = targetObject.AddComponent<HealthComponent>();
                DamageableReceiver targetReceiver = targetObject.AddComponent<DamageableReceiver>();
                targetObject.AddComponent<PlayerCharacter>();

                archetype = ScriptableObject.CreateInstance<EnemyArchetypeSO>();
                InvokeAwake(attackController);
                InvokeAwake(targetReceiver);
                Physics.SyncTransforms();

                EnemyAttackCommit observedCommit = default;
                int committedCount = 0;
                attackController.AttackCommitted += commit =>
                {
                    committedCount++;
                    observedCommit = commit;
                };

                Assert.IsTrue(attackController.TryAttack(targetObject.transform, archetype));
                Assert.AreEqual(1, committedCount);
                Assert.AreSame(targetObject.transform, observedCommit.Target);
                Assert.AreSame(archetype, observedCommit.Archetype);
                Assert.IsNull(observedCommit.Attack);
                Assert.AreEqual(10f, observedCommit.Damage, 0.0001f);
                Assert.AreEqual(90f, targetHealth.CurrentValue, 0.0001f);
            }
            finally
            {
                if (archetype != null)
                {
                    UnityEngine.Object.DestroyImmediate(archetype);
                }

                if (targetObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(targetObject);
                }

                if (enemyObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(enemyObject);
                }
            }
        }

        [Test]
        public void NetworkPlayerDeathStateBridge_AppliesAuthoritativeDeathToCombatPlayerState()
        {
            GameObject gameObject = new GameObject("NetworkCombatPlayer");

            try
            {
                HealthComponent health = gameObject.AddComponent<HealthComponent>();
                PlayerStateMachine stateMachine = gameObject.AddComponent<PlayerStateMachine>();
                PlayerCharacter player = gameObject.AddComponent<PlayerCharacter>();

                stateMachine.Initialize(player);

                Assert.IsFalse(health.IsDead);
                Assert.IsInstanceOf<PlayerLocomotionState>(stateMachine.CurrentState);

                bool applied = NetworkPlayerDeathStateBridge.ApplyAuthoritativeDeath(
                    health,
                    stateMachine,
                    Vector3.zero,
                    gameObject);

                Assert.IsTrue(applied);
                Assert.AreEqual(0f, health.CurrentValue, 0.0001f);
                Assert.IsTrue(health.IsDead);
                Assert.IsInstanceOf<PlayerDeathState>(stateMachine.CurrentState);
                Assert.IsFalse(
                    NetworkPlayerDeathStateBridge.ApplyAuthoritativeDeath(
                        health,
                        stateMachine,
                        Vector3.zero,
                        gameObject));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CombatTestPlayerPrefab_HasNetworkDeathStateBridgeWiredToFormalPlayerState()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab");

            Assert.IsNotNull(prefab);

            NetworkPlayerDeathStateBridge deathStateBridge = prefab.GetComponent<NetworkPlayerDeathStateBridge>();

            Assert.IsNotNull(deathStateBridge);

            SerializedObject serializedBridge = new SerializedObject(deathStateBridge);
            Assert.AreSame(
                prefab.GetComponent<HealthComponent>(),
                serializedBridge.FindProperty("health").objectReferenceValue);
            Assert.AreSame(
                prefab.GetComponent<PlayerStateMachine>(),
                serializedBridge.FindProperty("stateMachine").objectReferenceValue);
        }

        [Test]
        public void NetworkServerAttackProfile_ResolvesLight01FromServerWhitelist()
        {
            Assert.IsTrue(NetworkServerAttackProfile.TryResolve("Light_01", out NetworkServerAttackProfile profile));
            Assert.AreEqual("Light_01", profile.AttackId);
            Assert.AreEqual(25, profile.Damage);
            Assert.AreEqual(2.25f, profile.Range, 0.0001f);
            Assert.AreEqual(100f, profile.HalfAngleDegrees, 0.0001f);
            Assert.AreEqual(0.4f, profile.CooldownSeconds, 0.0001f);
            Assert.IsFalse(NetworkServerAttackProfile.TryResolve("Client_Only_OneShot", out _));
        }

        [Test]
        public void NetworkPlayerAvatar_ConfigureSmokeAttackRequestStoresAttackIdAndClampsMagnitude()
        {
            try
            {
                NetworkPlayerAvatar.ConfigureSmokeAttackRequest(-5, 1f);
                Assert.AreEqual(0, NetworkPlayerAvatar.SmokeAttackDamageRequest);
                Assert.AreEqual("Light_01", NetworkPlayerAvatar.SmokeAttackId);

                NetworkPlayerAvatar.ConfigureSmokeAttackRequest("Light_01", 9999, 1f);
                Assert.AreEqual(9999, NetworkPlayerAvatar.SmokeAttackDamageRequest);
                Assert.AreEqual("Light_01", NetworkPlayerAvatar.SmokeAttackId);
            }
            finally
            {
                NetworkPlayerAvatar.ConfigureSmokeAttackRequest(0, 0f);
            }
        }

        [Test]
        public void NetworkPlayerAvatar_ConfigureSmokeAttackRequestStoresRepeatSettings()
        {
            try
            {
                NetworkPlayerAvatar.ConfigureSmokeAttackRequest("Light_01", 9999, 1f, 99, -1f);

                Assert.AreEqual(16, NetworkPlayerAvatar.SmokeAttackCount);
                Assert.AreEqual(0.05f, NetworkPlayerAvatar.SmokeAttackIntervalSeconds, 0.0001f);
            }
            finally
            {
                NetworkPlayerAvatar.ConfigureSmokeAttackRequest(0, 0f);
            }
        }

        [Test]
        public void NetworkPlayerAvatar_IsTargetInsideAttackArcUsesRangeAndFacing()
        {
            Vector3 attacker = Vector3.zero;

            Assert.IsTrue(NetworkPlayerAvatar.IsTargetInsideAttackArc(attacker, 90f, new Vector3(2f, 0f, 0f)));
            Assert.IsFalse(NetworkPlayerAvatar.IsTargetInsideAttackArc(attacker, 90f, new Vector3(-2f, 0f, 0f)));
            Assert.IsFalse(NetworkPlayerAvatar.IsTargetInsideAttackArc(attacker, 90f, new Vector3(4f, 0f, 0f)));
            Assert.IsFalse(
                NetworkPlayerAvatar.IsTargetInsideAttackArc(
                    attacker,
                    90f,
                    new Vector3(2f, 0f, 0f),
                    new NetworkServerAttackProfile("Tiny", 1, 1f, 100f, 0.1f)));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void InvokeAwake(Component component)
        {
            MethodInfo awakeMethod = component.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awakeMethod?.Invoke(component, null);
        }

        private static bool IsLocalPreviewOnlyDependency(string assetPath)
        {
            return assetPath.StartsWith("Assets/Kevin Iglesias/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/DoubleL/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/ithappy/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/JC_LP_MedievalCharacters_LITE/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/Free medieval weapons/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/GhostSamurai_Animset/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/MYFG-Weapon Pack Lite/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/Polytope Studio/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith("Assets/LocalPreviewTools/", StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(
                    "Assets/_Game/Animations/Characters/CombatTest/LocalPreview/",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
