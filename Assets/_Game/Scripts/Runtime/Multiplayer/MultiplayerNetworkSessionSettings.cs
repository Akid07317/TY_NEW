using System;
using UnityEngine;

namespace CampusRPG.Multiplayer
{
    public readonly struct MultiplayerNetworkSessionSettings : IEquatable<MultiplayerNetworkSessionSettings>
    {
        public const int DefaultNetworkPort = 7777;
        public const string DefaultListenAddress = "0.0.0.0";
        public const string DefaultConnectAddress = "127.0.0.1";
        public const int DefaultMaxPlayers = 16;
        public const int DefaultNetworkEnemyCount = 1;
        public const string DefaultPlayerPrefabResourcePath = "Multiplayer/PF_NetworkPlayerAvatar";
        public const string DefaultEnemyPrefabResourcePath = "Multiplayer/PF_NetworkEnemyAvatar";

        public MultiplayerNetworkSessionSettings(
            string listenAddress,
            string connectAddress,
            int port,
            int maxPlayers,
            string playerPrefabResourcePath,
            GameObject playerPrefab,
            string enemyPrefabResourcePath = DefaultEnemyPrefabResourcePath,
            GameObject enemyPrefab = null,
            int networkEnemyCount = DefaultNetworkEnemyCount)
        {
            ListenAddress = string.IsNullOrWhiteSpace(listenAddress)
                ? DefaultListenAddress
                : listenAddress.Trim();
            ConnectAddress = string.IsNullOrWhiteSpace(connectAddress)
                ? DefaultConnectAddress
                : connectAddress.Trim();
            Port = MultiplayerCommandLineUtility.Clamp(port, 1, 65535);
            MaxPlayers = MultiplayerCommandLineUtility.Clamp(maxPlayers, 1, 256);
            PlayerPrefabResourcePath = string.IsNullOrWhiteSpace(playerPrefabResourcePath)
                ? DefaultPlayerPrefabResourcePath
                : playerPrefabResourcePath.Trim();
            PlayerPrefab = playerPrefab;
            EnemyPrefabResourcePath = string.IsNullOrWhiteSpace(enemyPrefabResourcePath)
                ? DefaultEnemyPrefabResourcePath
                : enemyPrefabResourcePath.Trim();
            EnemyPrefab = enemyPrefab;
            NetworkEnemyCount = MultiplayerCommandLineUtility.Clamp(networkEnemyCount, 0, 16);
        }

        public string ListenAddress { get; }
        public string ConnectAddress { get; }
        public int Port { get; }
        public int MaxPlayers { get; }
        public string PlayerPrefabResourcePath { get; }
        public GameObject PlayerPrefab { get; }
        public string EnemyPrefabResourcePath { get; }
        public GameObject EnemyPrefab { get; }
        public int NetworkEnemyCount { get; }

        public static MultiplayerNetworkSessionSettings CreateDefaultServerSettings()
        {
            return new MultiplayerNetworkSessionSettings(
                DefaultListenAddress,
                DefaultConnectAddress,
                DefaultNetworkPort,
                DefaultMaxPlayers,
                DefaultPlayerPrefabResourcePath,
                null,
                DefaultEnemyPrefabResourcePath,
                null,
                DefaultNetworkEnemyCount);
        }

        public static MultiplayerNetworkSessionSettings CreateDefaultClientSettings()
        {
            return new MultiplayerNetworkSessionSettings(
                DefaultListenAddress,
                DefaultConnectAddress,
                DefaultNetworkPort,
                DefaultMaxPlayers,
                DefaultPlayerPrefabResourcePath,
                null,
                DefaultEnemyPrefabResourcePath,
                null,
                DefaultNetworkEnemyCount);
        }

        public bool Equals(MultiplayerNetworkSessionSettings other)
        {
            return string.Equals(ListenAddress, other.ListenAddress, StringComparison.Ordinal)
                && string.Equals(ConnectAddress, other.ConnectAddress, StringComparison.Ordinal)
                && Port == other.Port
                && MaxPlayers == other.MaxPlayers
                && string.Equals(PlayerPrefabResourcePath, other.PlayerPrefabResourcePath, StringComparison.Ordinal)
                && Equals(PlayerPrefab, other.PlayerPrefab)
                && string.Equals(EnemyPrefabResourcePath, other.EnemyPrefabResourcePath, StringComparison.Ordinal)
                && Equals(EnemyPrefab, other.EnemyPrefab)
                && NetworkEnemyCount == other.NetworkEnemyCount;
        }

        public override bool Equals(object obj)
        {
            return obj is MultiplayerNetworkSessionSettings other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ListenAddress != null ? ListenAddress.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ConnectAddress != null ? ConnectAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Port;
                hashCode = (hashCode * 397) ^ MaxPlayers;
                hashCode = (hashCode * 397) ^ (PlayerPrefabResourcePath != null ? PlayerPrefabResourcePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PlayerPrefab != null ? PlayerPrefab.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (EnemyPrefabResourcePath != null ? EnemyPrefabResourcePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (EnemyPrefab != null ? EnemyPrefab.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ NetworkEnemyCount;
                return hashCode;
            }
        }
    }
}
