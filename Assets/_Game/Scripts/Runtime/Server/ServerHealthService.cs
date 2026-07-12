using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CampusRPG.Server
{
    public sealed class ServerHealthService : IDisposable
    {
        public const string DefaultServiceName = "TY_NEW_SERVER";
        private const string FallbackBindAddress = "0.0.0.0";

        private readonly object lifecycleLock = new object();
        private readonly string bindAddress;
        private readonly int port;
        private readonly Func<string> responseProvider;
        private TcpListener listener;
        private Thread listenerThread;
        private volatile bool isRunning;
        private long connectionsAccepted;
        private int activeConnections;

        public ServerHealthService(string bindAddress, int port, Func<string> responseProvider)
        {
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Health port must be between 0 and 65535.");
            }

            this.bindAddress = string.IsNullOrWhiteSpace(bindAddress)
                ? FallbackBindAddress
                : bindAddress.Trim();
            this.port = port;
            this.responseProvider = responseProvider ?? throw new ArgumentNullException(nameof(responseProvider));
        }

        public long ConnectionsAccepted => Interlocked.Read(ref connectionsAccepted);
        public int ActiveConnections => Volatile.Read(ref activeConnections);

        public int BoundPort
        {
            get
            {
                TcpListener activeListener = listener;

                if (activeListener != null && activeListener.LocalEndpoint is IPEndPoint endpoint)
                {
                    return endpoint.Port;
                }

                return port;
            }
        }

        public void Start()
        {
            lock (lifecycleLock)
            {
                if (isRunning)
                {
                    return;
                }

                listener = new TcpListener(ParseBindAddress(bindAddress), port);
                listener.Start();
                isRunning = true;
                listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "TY_NEW Server Health"
                };
                listenerThread.Start();
            }
        }

        public void Stop()
        {
            TcpListener listenerToStop;
            Thread threadToJoin;

            lock (lifecycleLock)
            {
                if (!isRunning && listener == null)
                {
                    return;
                }

                isRunning = false;
                listenerToStop = listener;
                threadToJoin = listenerThread;
                listener = null;
                listenerThread = null;
            }

            if (listenerToStop != null)
            {
                listenerToStop.Stop();
            }

            if (threadToJoin != null && threadToJoin.IsAlive)
            {
                threadToJoin.Join(250);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public static IPAddress ParseBindAddress(string bindAddress)
        {
            if (string.IsNullOrWhiteSpace(bindAddress)
                || bindAddress.Equals("*", StringComparison.Ordinal)
                || bindAddress.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Any;
            }

            if (bindAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Loopback;
            }

            if (IPAddress.TryParse(bindAddress.Trim(), out IPAddress address))
            {
                return address;
            }

            throw new FormatException($"Health bind address is not a valid IP address: {bindAddress}");
        }

        public static string FormatHealthResponse(ServerHealthSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} status={1} uptimeSeconds={2:0.0} frame={3} managedMemoryMb={4} port={5} healthEnabled={6} healthBindAddress={7} healthPort={8} targetFrameRate={9} tickRate={10} connectionsAccepted={11} activeConnections={12}",
                SanitizeToken(snapshot.ServiceName),
                SanitizeToken(snapshot.Status),
                snapshot.UptimeSeconds,
                snapshot.Frame,
                snapshot.ManagedMemoryMb,
                snapshot.Port,
                snapshot.HealthServerEnabled ? "true" : "false",
                SanitizeToken(snapshot.HealthBindAddress),
                snapshot.HealthPort,
                snapshot.TargetFrameRate,
                snapshot.TickRate,
                snapshot.ConnectionsAccepted,
                snapshot.ActiveConnections)
                + string.Format(
                    CultureInfo.InvariantCulture,
                    " gameplayEnabled={0} gameplayBindAddress={1} gameplayPort={2} room={3} maxPlayers={4} gameConnectionsAccepted={5} gameActiveConnections={6} gamePlayersJoined={7} gameActivePlayers={8} gameMessagesReceived={9}",
                    snapshot.GameplayServerEnabled ? "true" : "false",
                    SanitizeToken(snapshot.GameplayBindAddress),
                    snapshot.GameplayPort,
                    SanitizeToken(snapshot.RoomId),
                    snapshot.MaxPlayers,
                    snapshot.GameConnectionsAccepted,
                    snapshot.GameActiveConnections,
                    snapshot.GamePlayersJoined,
                    snapshot.GameActivePlayers,
                    snapshot.GameMessagesReceived)
                + string.Format(
                    CultureInfo.InvariantCulture,
                    " networkEnabled={0} networkStarted={1} networkListening={2} networkIsServer={3} networkIsClient={4} networkListenAddress={5} networkConnectAddress={6} networkPort={7} networkMaxPlayers={8} networkConnectedClients={9} networkSpawnedPlayers={10}",
                    snapshot.NetworkServerEnabled ? "true" : "false",
                    snapshot.NetworkStarted ? "true" : "false",
                    snapshot.NetworkListening ? "true" : "false",
                    snapshot.NetworkIsServer ? "true" : "false",
                    snapshot.NetworkIsClient ? "true" : "false",
                    SanitizeToken(snapshot.NetworkListenAddress),
                    SanitizeToken(snapshot.NetworkConnectAddress),
                    snapshot.NetworkPort,
                    snapshot.NetworkMaxPlayers,
                    snapshot.NetworkConnectedClients,
                    snapshot.NetworkSpawnedPlayers);
        }

        private void ListenLoop()
        {
            TcpListener activeListener = listener;

            while (isRunning && activeListener != null)
            {
                try
                {
                    TcpClient client = activeListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    if (isRunning)
                    {
                        continue;
                    }

                    break;
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = state as TcpClient;

            if (client == null)
            {
                return;
            }

            Interlocked.Increment(ref connectionsAccepted);
            Interlocked.Increment(ref activeConnections);

            try
            {
                using (client)
                {
                    client.SendTimeout = 1000;
                    string response = EnsureTrailingNewline(GetResponse());
                    byte[] payload = Encoding.UTF8.GetBytes(response);
                    NetworkStream stream = client.GetStream();
                    stream.Write(payload, 0, payload.Length);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activeConnections);
            }
        }

        private string GetResponse()
        {
            try
            {
                string response = responseProvider();
                return string.IsNullOrWhiteSpace(response)
                    ? DefaultServiceName + " status=unavailable"
                    : response;
            }
            catch (Exception)
            {
                return DefaultServiceName + " status=error error=response_provider_failed";
            }
        }

        private static string EnsureTrailingNewline(string value)
        {
            return value.EndsWith("\n", StringComparison.Ordinal) ? value : value + "\n";
        }

        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim().Replace(' ', '_').Replace('\t', '_').Replace('\n', '_').Replace('\r', '_');
        }
    }

    public struct ServerHealthSnapshot
    {
        public ServerHealthSnapshot(
            string serviceName,
            string status,
            int port,
            bool gameplayServerEnabled,
            string gameplayBindAddress,
            int gameplayPort,
            string roomId,
            int maxPlayers,
            long gameConnectionsAccepted,
            int gameActiveConnections,
            long gamePlayersJoined,
            int gameActivePlayers,
            long gameMessagesReceived,
            bool networkServerEnabled,
            bool networkStarted,
            bool networkListening,
            bool networkIsServer,
            bool networkIsClient,
            string networkListenAddress,
            string networkConnectAddress,
            int networkPort,
            int networkMaxPlayers,
            int networkConnectedClients,
            int networkSpawnedPlayers,
            bool healthServerEnabled,
            string healthBindAddress,
            int healthPort,
            int targetFrameRate,
            int tickRate,
            double uptimeSeconds,
            int frame,
            long managedMemoryMb,
            long connectionsAccepted,
            int activeConnections)
        {
            ServiceName = serviceName;
            Status = status;
            Port = port;
            GameplayServerEnabled = gameplayServerEnabled;
            GameplayBindAddress = gameplayBindAddress;
            GameplayPort = gameplayPort;
            RoomId = roomId;
            MaxPlayers = maxPlayers;
            GameConnectionsAccepted = gameConnectionsAccepted;
            GameActiveConnections = gameActiveConnections;
            GamePlayersJoined = gamePlayersJoined;
            GameActivePlayers = gameActivePlayers;
            GameMessagesReceived = gameMessagesReceived;
            NetworkServerEnabled = networkServerEnabled;
            NetworkStarted = networkStarted;
            NetworkListening = networkListening;
            NetworkIsServer = networkIsServer;
            NetworkIsClient = networkIsClient;
            NetworkListenAddress = networkListenAddress;
            NetworkConnectAddress = networkConnectAddress;
            NetworkPort = networkPort;
            NetworkMaxPlayers = networkMaxPlayers;
            NetworkConnectedClients = networkConnectedClients;
            NetworkSpawnedPlayers = networkSpawnedPlayers;
            HealthServerEnabled = healthServerEnabled;
            HealthBindAddress = healthBindAddress;
            HealthPort = healthPort;
            TargetFrameRate = targetFrameRate;
            TickRate = tickRate;
            UptimeSeconds = uptimeSeconds;
            Frame = frame;
            ManagedMemoryMb = managedMemoryMb;
            ConnectionsAccepted = connectionsAccepted;
            ActiveConnections = activeConnections;
        }

        public string ServiceName { get; }
        public string Status { get; }
        public int Port { get; }
        public bool GameplayServerEnabled { get; }
        public string GameplayBindAddress { get; }
        public int GameplayPort { get; }
        public string RoomId { get; }
        public int MaxPlayers { get; }
        public long GameConnectionsAccepted { get; }
        public int GameActiveConnections { get; }
        public long GamePlayersJoined { get; }
        public int GameActivePlayers { get; }
        public long GameMessagesReceived { get; }
        public bool NetworkServerEnabled { get; }
        public bool NetworkStarted { get; }
        public bool NetworkListening { get; }
        public bool NetworkIsServer { get; }
        public bool NetworkIsClient { get; }
        public string NetworkListenAddress { get; }
        public string NetworkConnectAddress { get; }
        public int NetworkPort { get; }
        public int NetworkMaxPlayers { get; }
        public int NetworkConnectedClients { get; }
        public int NetworkSpawnedPlayers { get; }
        public bool HealthServerEnabled { get; }
        public string HealthBindAddress { get; }
        public int HealthPort { get; }
        public int TargetFrameRate { get; }
        public int TickRate { get; }
        public double UptimeSeconds { get; }
        public int Frame { get; }
        public long ManagedMemoryMb { get; }
        public long ConnectionsAccepted { get; }
        public int ActiveConnections { get; }
    }
}
