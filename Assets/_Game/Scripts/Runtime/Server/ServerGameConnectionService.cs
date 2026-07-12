using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CampusRPG.Server
{
    public sealed class ServerGameConnectionService : IDisposable
    {
        public const string DefaultProtocolName = "TY_NEW_GAME";
        public const int DefaultProtocolVersion = 1;
        public const string DefaultRoomId = "combat-test";
        public const int DefaultMaxPlayers = 16;
        private const string FallbackBindAddress = "0.0.0.0";
        private const int MaxCommandLength = 512;
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly object lifecycleLock = new object();
        private readonly object clientLock = new object();
        private readonly object joinLock = new object();
        private readonly string bindAddress;
        private readonly int port;
        private readonly string roomId;
        private readonly int maxPlayers;
        private readonly List<TcpClient> activeClients = new List<TcpClient>();
        private TcpListener listener;
        private Thread listenerThread;
        private volatile bool isRunning;
        private long nextConnectionId;
        private long connectionsAccepted;
        private long playersJoined;
        private long messagesReceived;
        private int activeConnections;
        private int activePlayers;

        public ServerGameConnectionService(string bindAddress, int port, string roomId, int maxPlayers)
        {
            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "Gameplay port must be between 0 and 65535.");
            }

            this.bindAddress = string.IsNullOrWhiteSpace(bindAddress)
                ? FallbackBindAddress
                : bindAddress.Trim();
            this.port = port;
            this.roomId = NormalizeRoomId(roomId, DefaultRoomId);
            this.maxPlayers = Math.Max(1, maxPlayers);
        }

        public long ConnectionsAccepted => Interlocked.Read(ref connectionsAccepted);
        public long PlayersJoined => Interlocked.Read(ref playersJoined);
        public long MessagesReceived => Interlocked.Read(ref messagesReceived);
        public int ActiveConnections => Volatile.Read(ref activeConnections);
        public int ActivePlayers => Volatile.Read(ref activePlayers);

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
                    Name = "TY_NEW Game Connections"
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

            CloseActiveClients();

            if (threadToJoin != null && threadToJoin.IsAlive)
            {
                threadToJoin.Join(250);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public ServerGameSnapshot CreateSnapshot(bool enabled)
        {
            return new ServerGameSnapshot(
                enabled,
                bindAddress,
                BoundPort,
                roomId,
                maxPlayers,
                ConnectionsAccepted,
                ActiveConnections,
                PlayersJoined,
                ActivePlayers,
                MessagesReceived);
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

            throw new FormatException($"Gameplay bind address is not a valid IP address: {bindAddress}");
        }

        public static string FormatWelcome(long connectionId, string roomId, int maxPlayers)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} protocol={1} connectionId={2} room={3} maxPlayers={4}",
                DefaultProtocolName,
                DefaultProtocolVersion,
                connectionId,
                SanitizeToken(roomId),
                maxPlayers);
        }

        public static string FormatJoined(PlayerSessionSnapshot session, int activePlayers, int maxPlayers)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "JOINED connectionId={0} playerId={1} playerName={2} room={3} players={4} maxPlayers={5}",
                session.ConnectionId,
                session.PlayerId,
                SanitizeToken(session.PlayerName),
                SanitizeToken(session.RoomId),
                activePlayers,
                maxPlayers);
        }

        public static string FormatPong(PlayerSessionSnapshot session, long serverTimeMilliseconds)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "PONG connectionId={0} playerId={1} joined={2} serverTimeMs={3}",
                session.ConnectionId,
                session.PlayerId,
                session.Joined ? "true" : "false",
                serverTimeMilliseconds);
        }

        public static string FormatRoomState(ServerGameSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "ROOM room={0} players={1} maxPlayers={2} activeConnections={3} connectionsAccepted={4} playersJoined={5} messagesReceived={6}",
                SanitizeToken(snapshot.RoomId),
                snapshot.ActivePlayers,
                snapshot.MaxPlayers,
                snapshot.ActiveConnections,
                snapshot.ConnectionsAccepted,
                snapshot.PlayersJoined,
                snapshot.MessagesReceived);
        }

        public static string FormatError(string code, string message)
        {
            return "ERR code=" + SanitizeToken(code) + " message=" + SanitizeToken(message);
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

            RegisterClient(client);
            Interlocked.Increment(ref connectionsAccepted);
            Interlocked.Increment(ref activeConnections);

            long connectionId = Interlocked.Increment(ref nextConnectionId);
            PlayerSession session = new PlayerSession(connectionId, roomId);

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    client.NoDelay = true;
                    writer.NewLine = "\n";
                    writer.AutoFlush = true;
                    writer.WriteLine(FormatWelcome(session.ConnectionId, roomId, maxPlayers));

                    while (isRunning)
                    {
                        string line = reader.ReadLine();

                        if (line == null)
                        {
                            break;
                        }

                        if (line.Length > MaxCommandLength)
                        {
                            writer.WriteLine(FormatError("line_too_long", "Command length exceeds limit."));
                            continue;
                        }

                        Interlocked.Increment(ref messagesReceived);
                        string response = HandleCommand(session, line);
                        writer.WriteLine(response);

                        if (IsQuitCommand(line))
                        {
                            break;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Client disconnects are expected while probing the game port.
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                if (session.Joined)
                {
                    Interlocked.Decrement(ref activePlayers);
                }

                Interlocked.Decrement(ref activeConnections);
                UnregisterClient(client);
            }
        }

        private string HandleCommand(PlayerSession session, string line)
        {
            string command = ReadCommandName(line);

            if (string.IsNullOrWhiteSpace(command))
            {
                return FormatError("empty_command", "Command is empty.");
            }

            switch (command.ToUpperInvariant())
            {
                case "HELLO":
                case "JOIN":
                    return HandleJoin(session, line);
                case "PING":
                    return FormatPong(session.ToSnapshot(), GetUnixTimeMilliseconds());
                case "STATE":
                    return FormatRoomState(CreateSnapshot(true));
                case "QUIT":
                    return "BYE connectionId=" + session.ConnectionId;
                default:
                    return FormatError("unknown_command", command);
            }
        }

        private string HandleJoin(PlayerSession session, string line)
        {
            lock (joinLock)
            {
                if (!session.Joined && ActivePlayers >= maxPlayers)
                {
                    return FormatError("room_full", "Room is full.");
                }

                if (!session.Joined)
                {
                    session.Joined = true;
                    session.PlayerName = ReadTokenValue(line, "playerName", ReadTokenValue(line, "name", session.PlayerName));
                    Interlocked.Increment(ref playersJoined);
                    Interlocked.Increment(ref activePlayers);
                }

                return FormatJoined(session.ToSnapshot(), ActivePlayers, maxPlayers);
            }
        }

        private void RegisterClient(TcpClient client)
        {
            lock (clientLock)
            {
                activeClients.Add(client);
            }
        }

        private void UnregisterClient(TcpClient client)
        {
            lock (clientLock)
            {
                activeClients.Remove(client);
            }
        }

        private void CloseActiveClients()
        {
            TcpClient[] clients;

            lock (clientLock)
            {
                clients = activeClients.ToArray();
                activeClients.Clear();
            }

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].Close();
            }
        }

        private static bool IsQuitCommand(string line)
        {
            return string.Equals(ReadCommandName(line), "QUIT", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadCommandName(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            string trimmed = line.Trim();
            int separatorIndex = trimmed.IndexOf(' ');
            return separatorIndex < 0 ? trimmed : trimmed.Substring(0, separatorIndex);
        }

        private static string ReadTokenValue(string line, string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string prefix = key + "=";

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return SanitizeToken(parts[i].Substring(prefix.Length));
                }
            }

            return fallback;
        }

        private static long GetUnixTimeMilliseconds()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        private static string NormalizeRoomId(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return SanitizeToken(value);
        }

        private static string SanitizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Trim()
                .Replace(' ', '_')
                .Replace('\t', '_')
                .Replace('\n', '_')
                .Replace('\r', '_');
        }

        private sealed class PlayerSession
        {
            public PlayerSession(long connectionId, string roomId)
            {
                ConnectionId = connectionId;
                PlayerId = connectionId;
                RoomId = roomId;
                PlayerName = "player-" + connectionId.ToString(CultureInfo.InvariantCulture);
            }

            public long ConnectionId { get; }
            public long PlayerId { get; }
            public string RoomId { get; }
            public string PlayerName { get; set; }
            public bool Joined { get; set; }

            public PlayerSessionSnapshot ToSnapshot()
            {
                return new PlayerSessionSnapshot(ConnectionId, PlayerId, PlayerName, RoomId, Joined);
            }
        }
    }

    public struct PlayerSessionSnapshot
    {
        public PlayerSessionSnapshot(long connectionId, long playerId, string playerName, string roomId, bool joined)
        {
            ConnectionId = connectionId;
            PlayerId = playerId;
            PlayerName = playerName;
            RoomId = roomId;
            Joined = joined;
        }

        public long ConnectionId { get; }
        public long PlayerId { get; }
        public string PlayerName { get; }
        public string RoomId { get; }
        public bool Joined { get; }
    }

    public struct ServerGameSnapshot
    {
        public ServerGameSnapshot(
            bool enabled,
            string bindAddress,
            int port,
            string roomId,
            int maxPlayers,
            long connectionsAccepted,
            int activeConnections,
            long playersJoined,
            int activePlayers,
            long messagesReceived)
        {
            Enabled = enabled;
            BindAddress = bindAddress;
            Port = port;
            RoomId = roomId;
            MaxPlayers = maxPlayers;
            ConnectionsAccepted = connectionsAccepted;
            ActiveConnections = activeConnections;
            PlayersJoined = playersJoined;
            ActivePlayers = activePlayers;
            MessagesReceived = messagesReceived;
        }

        public bool Enabled { get; }
        public string BindAddress { get; }
        public int Port { get; }
        public string RoomId { get; }
        public int MaxPlayers { get; }
        public long ConnectionsAccepted { get; }
        public int ActiveConnections { get; }
        public long PlayersJoined { get; }
        public int ActivePlayers { get; }
        public long MessagesReceived { get; }
    }
}
