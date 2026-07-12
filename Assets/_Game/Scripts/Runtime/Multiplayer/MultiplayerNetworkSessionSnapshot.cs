namespace CampusRPG.Multiplayer
{
    public readonly struct MultiplayerNetworkSessionSnapshot
    {
        public MultiplayerNetworkSessionSnapshot(
            bool enabled,
            bool started,
            bool listening,
            bool isServer,
            bool isClient,
            string listenAddress,
            string connectAddress,
            int port,
            int maxPlayers,
            int connectedClients,
            int spawnedPlayers)
        {
            Enabled = enabled;
            Started = started;
            Listening = listening;
            IsServer = isServer;
            IsClient = isClient;
            ListenAddress = listenAddress;
            ConnectAddress = connectAddress;
            Port = port;
            MaxPlayers = maxPlayers;
            ConnectedClients = connectedClients;
            SpawnedPlayers = spawnedPlayers;
        }

        public bool Enabled { get; }
        public bool Started { get; }
        public bool Listening { get; }
        public bool IsServer { get; }
        public bool IsClient { get; }
        public string ListenAddress { get; }
        public string ConnectAddress { get; }
        public int Port { get; }
        public int MaxPlayers { get; }
        public int ConnectedClients { get; }
        public int SpawnedPlayers { get; }
    }
}
