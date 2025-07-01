using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncGuardMonitor.Core
{
    /// <summary>
    /// SyncGuard Monitor 전용 TCP 서버
    /// SyncGuard와 완전히 독립적으로 동작
    /// </summary>
    public class TcpServer : IDisposable
    {
        // 상수 정의
        private const int BUFFER_SIZE = 4096;
        private const int MAX_PENDING_CONNECTIONS = 100;
        private const int READ_TIMEOUT_MS = 5000;
        
        // 멤버 변수
        private TcpListener? listener;
        private CancellationTokenSource? cancellationTokenSource;
        private readonly ConcurrentDictionary<string, ClientConnection> activeConnections;
        private readonly SemaphoreSlim connectionSemaphore;
        private bool isRunning = false;
        private int port;
        
        // 통계
        private long totalConnections = 0;
        private long totalMessages = 0;
        private long totalBytes = 0;
        private DateTime startTime;
        
        // 이벤트
        public event EventHandler<MessageReceivedEventArgs>? MessageReceived;
        public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
        public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
        public event EventHandler<ServerStatusEventArgs>? ServerStatusChanged;
        public event EventHandler<ServerErrorEventArgs>? ServerError;
        
        // 프로퍼티
        public bool IsRunning => isRunning;
        public int Port => port;
        public int ActiveConnectionCount => activeConnections.Count;
        public ServerStatistics Statistics => GetStatistics();
        
        public TcpServer()
        {
            activeConnections = new ConcurrentDictionary<string, ClientConnection>();
            connectionSemaphore = new SemaphoreSlim(MAX_PENDING_CONNECTIONS);
        }
        
        /// <summary>
        /// 서버 시작
        /// </summary>
        public async Task StartAsync(int listenPort)
        {
            if (isRunning)
                throw new InvalidOperationException("서버가 이미 실행 중입니다.");
            
            try
            {
                port = listenPort;
                cancellationTokenSource = new CancellationTokenSource();
                listener = new TcpListener(IPAddress.Any, port);
                
                // Socket 옵션 설정
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                
                listener.Start(MAX_PENDING_CONNECTIONS);
                
                isRunning = true;
                startTime = DateTime.Now;
                
                // 서버 상태 변경 이벤트
                ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs 
                { 
                    IsRunning = true, 
                    Port = port,
                    StartTime = startTime
                });
                
                // 클라이언트 수락 시작 (백그라운드에서 실행)
                _ = Task.Run(() => AcceptClientsAsync(cancellationTokenSource.Token));
                
                // 정리 작업 시작 (백그라운드에서 실행)
                _ = Task.Run(() => CleanupInactiveConnectionsAsync(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                isRunning = false;
                LogError($"서버 시작 실패: {ex.Message}");
                ServerError?.Invoke(this, new ServerErrorEventArgs { Error = ex });
                throw;
            }
        }
        
        /// <summary>
        /// 서버 중지
        /// </summary>
        public async Task StopAsync()
        {
            if (!isRunning)
                return;
            
            try
            {
                isRunning = false;
                cancellationTokenSource?.Cancel();
                
                // 모든 연결 종료
                var tasks = new List<Task>();
                foreach (var connection in activeConnections.Values)
                {
                    tasks.Add(DisconnectClientAsync(connection));
                }
                
                await Task.WhenAll(tasks);
                
                listener?.Stop();
                
                ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs 
                { 
                    IsRunning = false,
                    Port = port
                });
            }
            catch (Exception ex)
            {
                LogError($"서버 중지 중 오류: {ex.Message}");
                ServerError?.Invoke(this, new ServerErrorEventArgs { Error = ex });
            }
        }
        
        /// <summary>
        /// 클라이언트 연결 수락
        /// </summary>
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && isRunning)
            {
                try
                {
                    // 연결 수 제한
                    await connectionSemaphore.WaitAsync(cancellationToken);
                    
                    var tcpClient = await listener!.AcceptTcpClientAsync();
                    
                    if (!isRunning)
                    {
                        tcpClient.Close();
                        connectionSemaphore.Release();
                        break;
                    }
                    
                    // 클라이언트 정보 생성
                    var clientEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                    var clientId = clientEndPoint?.ToString() ?? $"Unknown_{Guid.NewGuid()}";
                    
                    var connection = new ClientConnection
                    {
                        Id = clientId,
                        TcpClient = tcpClient,
                        IpAddress = clientEndPoint?.Address.ToString() ?? "Unknown",
                        Port = clientEndPoint?.Port ?? 0,
                        ConnectedTime = DateTime.Now,
                        Stream = tcpClient.GetStream()
                    };
                    
                    // 연결 추가
                    if (activeConnections.TryAdd(clientId, connection))
                    {
                        Interlocked.Increment(ref totalConnections);
                        
                        LogInfo($"클라이언트 연결됨: {clientId}");
                        
                        // 연결 이벤트
                        ClientConnected?.Invoke(this, new ClientConnectionEventArgs
                        {
                            ClientId = clientId,
                            IpAddress = connection.IpAddress,
                            Port = connection.Port,
                            Timestamp = connection.ConnectedTime
                        });
                        
                        // 클라이언트 처리 시작
                        await HandleClientAsync(connection, cancellationToken);
                    }
                    else
                    {
                        tcpClient.Close();
                        connectionSemaphore.Release();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 서버 중지 시 정상
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        LogError($"클라이언트 수락 오류: {ex.Message}");
                        ServerError?.Invoke(this, new ServerErrorEventArgs { Error = ex });
                    }
                }
            }
        }
        
        /// <summary>
        /// 클라이언트 처리
        /// </summary>
        private async Task HandleClientAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            var buffer = new byte[BUFFER_SIZE];
            var messageBuilder = new StringBuilder();
            
            try
            {
                connection.Stream.ReadTimeout = READ_TIMEOUT_MS;
                
                while (!cancellationToken.IsCancellationRequested && 
                       connection.TcpClient.Connected)
                {
                    int bytesRead = await connection.Stream.ReadAsync(
                        buffer, 0, buffer.Length, cancellationToken);
                    
                    if (bytesRead == 0)
                    {
                        // 연결 종료
                        break;
                    }
                    
                    // 통계 업데이트
                    Interlocked.Add(ref totalBytes, bytesRead);
                    connection.LastActivityTime = DateTime.Now;
                    connection.BytesReceived += bytesRead;
                    
                    // 메시지 조합
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    
                    // 완전한 메시지 처리
                    ProcessCompleteMessages(connection, messageBuilder);
                }
            }
            catch (IOException ioEx)
            {
                LogDebug($"클라이언트 {connection.Id} 연결 종료: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                LogError($"클라이언트 처리 오류 [{connection.Id}]: {ex.Message}");
                ServerError?.Invoke(this, new ServerErrorEventArgs 
                { 
                    Error = ex,
                    ClientId = connection.Id 
                });
            }
            finally
            {
                await DisconnectClientAsync(connection);
            }
        }
        
        /// <summary>
        /// 완전한 메시지 처리
        /// </summary>
        private void ProcessCompleteMessages(ClientConnection connection, StringBuilder messageBuilder)
        {
            string messages = messageBuilder.ToString();
            int lastIndex = messages.LastIndexOf("\r\n");
            
            if (lastIndex >= 0)
            {
                // 완전한 메시지들 추출
                string completeMessages = messages.Substring(0, lastIndex);
                
                // 버퍼 정리
                messageBuilder.Clear();
                if (lastIndex + 2 < messages.Length)
                {
                    messageBuilder.Append(messages.Substring(lastIndex + 2));
                }
                
                // 각 메시지 처리
                var messageArray = completeMessages.Split(
                    new[] { "\r\n" }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var message in messageArray)
                {
                    try
                    {
                        Interlocked.Increment(ref totalMessages);
                        connection.MessagesReceived++;
                        
                        // 메시지 수신 이벤트
                        MessageReceived?.Invoke(this, new MessageReceivedEventArgs
                        {
                            ClientId = connection.Id,
                            IpAddress = connection.IpAddress,
                            Message = message,
                            ReceivedTime = DateTime.Now
                        });
                        
                        LogDebug($"메시지 수신 [{connection.Id}]: {message}");
                    }
                    catch (Exception ex)
                    {
                        LogError($"메시지 처리 오류: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 클라이언트 연결 해제
        /// </summary>
        private Task DisconnectClientAsync(ClientConnection connection)
        {
            try
            {
                if (activeConnections.TryRemove(connection.Id, out _))
                {
                    connection.Stream?.Close();
                    connection.TcpClient?.Close();
                    connectionSemaphore.Release();
                    LogInfo($"클라이언트 연결 해제: {connection.Id}");
                    // 연결 해제 이벤트
                    ClientDisconnected?.Invoke(this, new ClientConnectionEventArgs
                    {
                        ClientId = connection.Id,
                        IpAddress = connection.IpAddress,
                        Port = connection.Port,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                LogError($"연결 해제 중 오류: {ex.Message}");
            }
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 비활성 연결 정리
        /// </summary>
        private async Task CleanupInactiveConnectionsAsync(CancellationToken cancellationToken)
        {
            var cleanupInterval = TimeSpan.FromSeconds(30);
            var inactiveTimeout = TimeSpan.FromMinutes(5);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(cleanupInterval, cancellationToken);
                    
                    var now = DateTime.Now;
                    var inactiveConnections = activeConnections.Values
                        .Where(c => now - c.LastActivityTime > inactiveTimeout)
                        .ToList();
                    
                    foreach (var connection in inactiveConnections)
                    {
                        LogInfo($"비활성 연결 정리: {connection.Id}");
                        await DisconnectClientAsync(connection);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"연결 정리 중 오류: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 서버 통계 조회
        /// </summary>
        private ServerStatistics GetStatistics()
        {
            var uptime = isRunning ? DateTime.Now - startTime : TimeSpan.Zero;
            
            return new ServerStatistics
            {
                IsRunning = isRunning,
                Port = port,
                StartTime = startTime,
                Uptime = uptime,
                TotalConnections = totalConnections,
                ActiveConnections = activeConnections.Count,
                TotalMessages = totalMessages,
                TotalBytes = totalBytes,
                MessagesPerSecond = uptime.TotalSeconds > 0 ? 
                    totalMessages / uptime.TotalSeconds : 0,
                BytesPerSecond = uptime.TotalSeconds > 0 ? 
                    totalBytes / uptime.TotalSeconds : 0
            };
        }
        
        // 로깅 메서드
        private void LogDebug(string message) => 
            System.Diagnostics.Debug.WriteLine($"[TcpServer] {message}");
        private void LogInfo(string message) => 
            System.Diagnostics.Debug.WriteLine($"[TcpServer] {message}");
        private void LogError(string message) => 
            System.Diagnostics.Debug.WriteLine($"[TcpServer] ERROR: {message}");
        
        public void Dispose()
        {
            StopAsync().Wait(5000);
            cancellationTokenSource?.Dispose();
            connectionSemaphore?.Dispose();
        }
    }
    
    /// <summary>
    /// 클라이언트 연결 정보
    /// </summary>
    internal class ClientConnection
    {
        public string Id { get; set; } = "";
        public TcpClient TcpClient { get; set; } = null!;
        public NetworkStream Stream { get; set; } = null!;
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public long BytesReceived { get; set; }
        public long MessagesReceived { get; set; }
    }
    
    /// <summary>
    /// 서버 통계
    /// </summary>
    public class ServerStatistics
    {
        public bool IsRunning { get; set; }
        public int Port { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public long TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public long TotalMessages { get; set; }
        public long TotalBytes { get; set; }
        public double MessagesPerSecond { get; set; }
        public double BytesPerSecond { get; set; }
    }
    
    // 이벤트 아규먼트 클래스들
    public class MessageReceivedEventArgs : EventArgs
    {
        public string ClientId { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime ReceivedTime { get; set; }
    }
    
    public class ClientConnectionEventArgs : EventArgs
    {
        public string ClientId { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ServerStatusEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public int Port { get; set; }
        public DateTime StartTime { get; set; }
    }
    
    public class ServerErrorEventArgs : EventArgs
    {
        public Exception Error { get; set; } = null!;
        public string? ClientId { get; set; }
    }
} 