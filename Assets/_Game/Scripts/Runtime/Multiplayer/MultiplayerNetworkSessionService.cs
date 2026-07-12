using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    public sealed class MultiplayerNetworkSessionService : IDisposable
    {
        private const string NetworkManagerObjectName = "P1_5_NetworkManager";
        private const ushort ProtocolVersion = 15;

        private NetworkManager networkManager;
        private UnityTransport transport;
        private GameObject managerObject;
        private MultiplayerNetworkSessionSettings activeSettings;
        private readonly List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
        private bool ownsManagerObject;
        private bool started;

        public static void ConfigureServerEnemyAttackSmoke(bool enabled)
        {
            NetworkEnemyAvatar.ConfigureServerEnemyAttackSmoke(enabled);
        }

        public static void ConfigureServerFormalEnemyAttackSmoke(bool enabled)
        {
            NetworkEnemyAvatar.ConfigureServerFormalEnemyAttackSmoke(enabled);
        }

        public static void ConfigureServerBrainEnemyAttackSmoke(bool enabled)
        {
            NetworkEnemyAvatar.ConfigureServerBrainEnemyAttackSmoke(enabled);
        }

        public static void ConfigureServerBrainEnemyChaseAttackSmoke(bool enabled)
        {
            NetworkEnemyAvatar.ConfigureServerBrainEnemyChaseAttackSmoke(enabled);
        }

        public static void ConfigureServerEnemyGameplayTick(bool enabled)
        {
            NetworkEnemyAvatar.ConfigureServerEnemyGameplayTick(enabled);
        }

        public static void ConfigureServerEnemyGameplayTickDeathDelay(float delaySeconds)
        {
            NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDeathDelay(delaySeconds);
        }

        public static void ConfigureServerEnemyGameplayTickDamage(int damage)
        {
            NetworkEnemyAvatar.ConfigureServerEnemyGameplayTickDamage(damage);
        }

        public MultiplayerNetworkSessionSnapshot CreateSnapshot(bool enabled)
        {
            int connectedClients = 0;

            if (networkManager != null)
            {
                if (networkManager.IsServer)
                {
                    connectedClients = networkManager.ConnectedClientsIds.Count;
                }
                else if (networkManager.IsConnectedClient)
                {
                    connectedClients = 1;
                }
            }

            return new MultiplayerNetworkSessionSnapshot(
                enabled,
                started,
                networkManager != null && networkManager.IsListening,
                networkManager != null && networkManager.IsServer,
                networkManager != null && networkManager.IsClient,
                activeSettings.ListenAddress,
                activeSettings.ConnectAddress,
                activeSettings.Port,
                activeSettings.MaxPlayers,
                connectedClients,
                NetworkPlayerAvatar.ActiveAvatarCount);
        }

        public bool StartServer(MultiplayerNetworkSessionSettings settings)
        {
            Configure(settings, settings.ListenAddress);
            started = networkManager.StartServer();

            if (!started)
            {
                Debug.LogError(
                    "[MultiplayerNetwork] Failed to start NGO server"
                    + $" listenAddress={settings.ListenAddress}"
                    + $" port={settings.Port}");
            }
            else
            {
                SpawnServerEnemy(settings);
                Debug.Log(
                    "[MultiplayerNetwork] NGO server listening"
                    + $" listenAddress={settings.ListenAddress}"
                    + $" port={settings.Port}"
                    + $" maxPlayers={settings.MaxPlayers}");
            }

            return started;
        }

        public bool StartClient(MultiplayerNetworkSessionSettings settings)
        {
            Configure(settings, settings.ConnectAddress);
            started = networkManager.StartClient();

            if (!started)
            {
                Debug.LogError(
                    "[MultiplayerNetwork] Failed to start NGO client"
                    + $" connectAddress={settings.ConnectAddress}"
                    + $" port={settings.Port}");
            }
            else
            {
                Debug.Log(
                    "[MultiplayerNetwork] NGO client connecting"
                    + $" connectAddress={settings.ConnectAddress}"
                    + $" port={settings.Port}");
            }

            return started;
        }

        public void Dispose()
        {
            DespawnServerEnemies();

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            if (ownsManagerObject && managerObject != null)
            {
                UnityEngine.Object.Destroy(managerObject);
            }

            networkManager = null;
            transport = null;
            managerObject = null;
            ownsManagerObject = false;
            started = false;
        }

        private void Configure(MultiplayerNetworkSessionSettings settings, string targetAddress)
        {
            activeSettings = settings;
            ResolveNetworkManager();
            ResolveTransport();

            GameObject playerPrefab = ResolvePlayerPrefab(settings);
            GameObject enemyPrefab = ResolveEnemyPrefab(settings);

            if (playerPrefab == null)
            {
                throw new InvalidOperationException(
                    "P1.5 network player prefab is missing. Expected Resources path: "
                    + settings.PlayerPrefabResourcePath);
            }

            if (enemyPrefab == null)
            {
                throw new InvalidOperationException(
                    "P6.0 network enemy prefab is missing. Expected Resources path: "
                    + settings.EnemyPrefabResourcePath);
            }

            transport.SetConnectionData(
                targetAddress,
                (ushort)settings.Port,
                settings.ListenAddress);

            NetworkConfig networkConfig = new NetworkConfig
            {
                ProtocolVersion = ProtocolVersion,
                PlayerPrefab = playerPrefab,
                NetworkTransport = transport,
                EnableSceneManagement = false,
                ConnectionApproval = false
            };
            networkConfig.Prefabs.Add(new NetworkPrefab { Prefab = enemyPrefab });
            networkManager.NetworkConfig = networkConfig;
        }

        private void ResolveNetworkManager()
        {
            networkManager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();

            if (networkManager != null)
            {
                managerObject = networkManager.gameObject;
                ownsManagerObject = false;
                UnityEngine.Object.DontDestroyOnLoad(managerObject);
                return;
            }

            managerObject = new GameObject(NetworkManagerObjectName);
            UnityEngine.Object.DontDestroyOnLoad(managerObject);
            networkManager = managerObject.AddComponent<NetworkManager>();
            ownsManagerObject = true;
        }

        private void ResolveTransport()
        {
            transport = managerObject.GetComponent<UnityTransport>();

            if (transport == null)
            {
                transport = managerObject.AddComponent<UnityTransport>();
            }
        }

        private static GameObject ResolvePlayerPrefab(MultiplayerNetworkSessionSettings settings)
        {
            if (settings.PlayerPrefab != null)
            {
                return settings.PlayerPrefab;
            }

            return Resources.Load<GameObject>(settings.PlayerPrefabResourcePath);
        }

        private static GameObject ResolveEnemyPrefab(MultiplayerNetworkSessionSettings settings)
        {
            if (settings.EnemyPrefab != null)
            {
                return settings.EnemyPrefab;
            }

            return Resources.Load<GameObject>(settings.EnemyPrefabResourcePath);
        }

        private void SpawnServerEnemy(MultiplayerNetworkSessionSettings settings)
        {
            if (networkManager == null || !networkManager.IsServer || spawnedEnemies.Count > 0)
            {
                return;
            }

            GameObject enemyPrefab = ResolveEnemyPrefab(settings);
            if (enemyPrefab == null || settings.NetworkEnemyCount <= 0)
            {
                return;
            }

            for (int i = 0; i < settings.NetworkEnemyCount; i++)
            {
                GameObject enemyInstance = UnityEngine.Object.Instantiate(enemyPrefab);
                NetworkObject spawnedEnemy = enemyInstance.GetComponent<NetworkObject>();

                if (spawnedEnemy == null)
                {
                    UnityEngine.Object.Destroy(enemyInstance);
                    throw new InvalidOperationException(
                        "P6.0 network enemy prefab is missing NetworkObject: "
                        + settings.EnemyPrefabResourcePath);
                }

                NetworkEnemyAvatar enemyAvatar = enemyInstance.GetComponent<NetworkEnemyAvatar>();
                Vector3 spawnPosition = NetworkEnemyAvatar.BuildServerSpawnPosition(i);

                if (enemyAvatar != null)
                {
                    enemyAvatar.ConfigureServerSpawn(i + 1, spawnPosition);
                }

                spawnedEnemy.Spawn();
                spawnedEnemies.Add(spawnedEnemy);
                Debug.Log(
                    "[MultiplayerNetwork] Spawned server network enemy"
                    + $" prefab={settings.EnemyPrefabResourcePath}"
                    + $" enemyId={i + 1}"
                    + $" spawnPosition={FormatVector3(spawnPosition)}"
                    + $" networkObjectId={spawnedEnemy.NetworkObjectId}");
            }
        }

        private void DespawnServerEnemies()
        {
            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                NetworkObject spawnedEnemy = spawnedEnemies[i];

                if (spawnedEnemy == null)
                {
                    continue;
                }

                if (spawnedEnemy.IsSpawned)
                {
                    spawnedEnemy.Despawn(true);
                }
                else
                {
                    UnityEngine.Object.Destroy(spawnedEnemy.gameObject);
                }
            }

            spawnedEnemies.Clear();
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"{value.x:0.00},{value.y:0.00},{value.z:0.00}";
        }
    }
}
