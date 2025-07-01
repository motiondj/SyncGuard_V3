# 📚 SyncGuard Monitor - 독립 소프트웨어 상세 개발 문서

## 🎯 프로젝트 정의

### ⚠️ **중요: 이것은 SyncGuard와 완전히 별개의 독립 소프트웨어입니다!**

**SyncGuard Monitor**는 SyncGuard 클라이언트들이 전송하는 TCP 메시지를 수신하여 모니터링하는 **독립적인 서버 애플리케이션**입니다. SyncGuard의 일부가 아니며, 별도로 개발/배포/운영되는 완전히 독립된 프로그램입니다.

### 📋 프로젝트 기본 정보
- **프로젝트명**: SyncGuard Monitor (독립 소프트웨어)
- **버전**: 1.0.0
- **개발 언어**: C# (.NET 6.0)
- **UI 프레임워크**: Windows Forms
- **타겟 OS**: Windows 10/11 (64bit)
- **라이선스**: MIT License (별도 라이선스)
- **저작권**: SyncGuard Monitor Team (SyncGuard Team과 별개)

---

## 📁 독립 프로젝트 구조

### 완전히 새로운 솔루션 구조
```
C:\Projects\SyncGuardMonitor\              # SyncGuard와 다른 별도 폴더
├── SyncGuardMonitor.sln                   # 독립 솔루션 파일
├── README.md                              # 프로젝트 설명
├── LICENSE                                # MIT 라이선스
├── .gitignore                             # Git 설정
├── docs/                                  # 문서
│   ├── user-manual.md                    # 사용자 매뉴얼
│   ├── developer-guide.md                # 개발자 가이드
│   └── api-reference.md                  # API 문서
├── src/                                   # 소스 코드
│   └── SyncGuardMonitor/                 # 메인 프로젝트
│       ├── SyncGuardMonitor.csproj       # 프로젝트 파일
│       ├── Program.cs                    # 진입점
│       ├── Forms/                        # UI 폼
│       │   ├── MainForm.cs              # 메인 화면
│       │   ├── MainForm.Designer.cs     # 디자이너 코드
│       │   ├── MainForm.resx            # 리소스
│       │   ├── SettingsForm.cs          # 설정 화면
│       │   ├── AboutForm.cs             # 정보 화면
│       │   └── StatisticsForm.cs        # 통계 화면
│       ├── Core/                         # 핵심 로직
│       │   ├── TcpServer.cs             # TCP 서버
│       │   ├── MessageProcessor.cs       # 메시지 처리
│       │   ├── ConnectionManager.cs      # 연결 관리
│       │   └── PerformanceMonitor.cs    # 성능 모니터링
│       ├── Models/                       # 데이터 모델
│       │   ├── ClientInfo.cs            # 클라이언트 정보
│       │   ├── SyncMessage.cs           # 메시지 구조
│       │   ├── SyncState.cs             # 상태 열거형
│       │   ├── StateHistory.cs          # 상태 이력
│       │   └── Statistics.cs            # 통계 정보
│       ├── Services/                     # 서비스 계층
│       │   ├── DataManager.cs           # 데이터 관리
│       │   ├── ConfigService.cs         # 설정 관리
│       │   ├── LoggingService.cs        # 로깅 서비스
│       │   └── ExportService.cs         # 내보내기 서비스
│       ├── UI/                           # UI 컴포넌트
│       │   ├── UIUpdateManager.cs       # UI 업데이트
│       │   ├── GridManager.cs           # 그리드 관리
│       │   ├── ChartManager.cs          # 차트 관리
│       │   └── NotificationManager.cs   # 알림 관리
│       ├── Utils/                        # 유틸리티
│       │   ├── NetworkUtils.cs          # 네트워크 유틸
│       │   ├── ValidationUtils.cs       # 검증 유틸
│       │   ├── FormatUtils.cs           # 포맷 유틸
│       │   └── Extensions.cs            # 확장 메서드
│       └── Resources/                    # 리소스
│           ├── Icons/                    # 아이콘
│           │   ├── app.ico              # 앱 아이콘
│           │   ├── master.png           # Master 상태
│           │   ├── slave.png            # Slave 상태
│           │   └── error.png            # Error 상태
│           └── Sounds/                   # 알림음
│               └── alert.wav            # 알림 소리
├── tests/                                # 테스트 프로젝트
│   └── SyncGuardMonitor.Tests/
│       ├── SyncGuardMonitor.Tests.csproj
│       ├── Core/                        # 핵심 로직 테스트
│       ├── Models/                      # 모델 테스트
│       └── Services/                    # 서비스 테스트
└── build/                               # 빌드 관련
    ├── build.ps1                        # 빌드 스크립트
    ├── publish.ps1                      # 배포 스크립트
    └── installer/                       # 설치 프로그램
        └── setup.iss                    # Inno Setup 스크립트
```

---

## 🏗️ 상세 아키텍처 설계

### 계층 구조
```
┌─────────────────────────────────────────────────────┐
│                   Presentation Layer                │
│  (MainForm, SettingsForm, GridManager, Charts)     │
├─────────────────────────────────────────────────────┤
│                   Business Logic Layer              │
│  (DataManager, MessageProcessor, Statistics)        │
├─────────────────────────────────────────────────────┤
│                   Service Layer                     │
│  (ConfigService, LoggingService, ExportService)    │
├─────────────────────────────────────────────────────┤
│                   Network Layer                     │
│  (TcpServer, ConnectionManager, MessageParser)      │
├─────────────────────────────────────────────────────┤
│                   Data Layer                        │
│  (Models, DTOs, Repositories)                       │
└─────────────────────────────────────────────────────┘
```

### 데이터 흐름
```
SyncGuard Client → TCP Message → SyncGuard Monitor
     ↓                              ↓
[IP_state] ─────────────────→ [TcpServer]
                                    ↓
                              [MessageProcessor]
                                    ↓
                              [DataManager]
                                    ↓
                              [UI Update]
                                    ↓
                              [User Display]
```

---

## 💻 상세 화면 설계

### 메인 화면 (MainForm) - 1200x800px
```
┌──────────────────────────────────────────────────────────────────────┐
│ SyncGuard Monitor v1.0 - 독립 모니터링 소프트웨어     [─] [□] [X]  │
├──────────────────────────────────────────────────────────────────────┤
│ 파일(F)  편집(E)  보기(V)  도구(T)  창(W)  도움말(H)               │
├──────────────────────────────────────────────────────────────────────┤
│ ┌────────────────────────────────────────────────────────────────┐  │
│ │ [▶ 시작] [■ 중지] [⚙ 설정] [📊 통계] [💾 내보내기] [🔄 새로고침] │  │
│ └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│ ┌──────────────────── 서버 상태 ────────────────────────────────┐  │
│ │ 상태: ● 실행중  | 포트: 8080 | 연결: 5 | 수신: 1,234 msg/s   │  │
│ │ 시작 시간: 2025-01-01 09:00:00 | 실행 시간: 02:34:56         │  │
│ └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│ ┌──────────────────── 클라이언트 목록 ──────────────────────────┐  │
│ │ ┌─────────────────────────────────────────────────────────┐   │  │
│ │ │ IP 주소↓    | 상태  | 최근수신  | 지속시간 | 메시지 | ● │   │  │
│ │ ├─────────────────────────────────────────────────────────┤   │  │
│ │ │ 192.168.0.201 | Master | 14:23:45 | 00:05:23 | 1,234  | ● │   │  │
│ │ │ 192.168.0.202 | Slave  | 14:23:44 | 00:05:22 | 1,233  | ● │   │  │
│ │ │ 192.168.0.203 | Error  | 14:23:20 | 00:00:00 | 1,200  | ● │   │  │
│ │ │ 192.168.0.204 | Slave  | 14:23:43 | 00:04:21 | 1,100  | ● │   │  │
│ │ │ 192.168.0.205 | Master | 14:23:42 | 00:03:20 | 1,050  | ● │   │  │
│ │ └─────────────────────────────────────────────────────────┘   │  │
│ └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│ ┌─ 실시간 차트 ─┬─ 상태 분포 ─┬─ 메시지 통계 ─────────────────┐  │
│ │ [실시간 그래프] │ Master: 40%  │ 총 메시지: 123,456            │  │
│ │                │ Slave:  40%  │ 초당 평균: 5.2 msg/s          │  │
│ │                │ Error:  20%  │ 피크 시간: 14:15:30           │  │
│ └────────────────┴──────────────┴────────────────────────────────┘  │
│                                                                      │
│ ┌──────────────────── 실시간 로그 ───────────────────────────────┐  │
│ │ [14:23:45] 💚 192.168.0.201 상태 변경: Slave → Master          │  │
│ │ [14:23:44] 🔵 192.168.0.202 연결됨                             │  │
│ │ [14:23:43] 📨 192.168.0.204 메시지 수신: state1                │  │
│ │ [14:23:42] 🔴 192.168.0.203 상태 변경: Master → Error         │  │
│ └────────────────────────────────────────────────────────────────┘  │
│ 준비 | 서버 실행중 | CPU: 2% | 메모리: 45MB | 네트워크: 12KB/s    │
└──────────────────────────────────────────────────────────────────────┘
```

### 설정 화면 (SettingsForm) - 600x500px
```
┌─────────────────── 설정 ───────────────────┐
│ ┌─ 일반 ─┬─ 네트워크 ─┬─ 알림 ─┬─ 고급 ─┐│
│ │                                          ││
│ │ [네트워크 설정]                          ││
│ │ TCP 포트: [8080    ] (1-65535)          ││
│ │ ☑ 자동 시작                             ││
│ │ ☑ 로컬 연결만 허용                      ││
│ │                                          ││
│ │ [성능 설정]                              ││
│ │ 최대 동시 연결: [100   ] 개             ││
│ │ 메시지 버퍼 크기: [1024  ] KB           ││
│ │ 비활성 타임아웃: [30    ] 초            ││
│ │                                          ││
│ │ [데이터 관리]                            ││
│ │ 히스토리 보관: [100   ] 개              ││
│ │ ☑ 자동 정리 활성화                      ││
│ │                                          ││
│ └──────────────────────────────────────────┘│
│ [적용] [저장] [취소]                         │
└──────────────────────────────────────────────┘
```

---

## 🔧 핵심 클래스 상세 구현

### 1. TCP 서버 클래스 (완전 독립적)

```csharp
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
                
                LogInfo($"TCP 서버가 포트 {port}에서 시작되었습니다.");
                
                // 서버 상태 변경 이벤트
                ServerStatusChanged?.Invoke(this, new ServerStatusEventArgs 
                { 
                    IsRunning = true, 
                    Port = port,
                    StartTime = startTime
                });
                
                // 클라이언트 수락 시작
                _ = Task.Run(() => AcceptClientsAsync(cancellationTokenSource.Token));
                
                // 정리 작업 시작
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
                LogInfo("서버를 중지하는 중...");
                
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
                
                LogInfo("서버가 중지되었습니다.");
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
                        _ = Task.Run(() => HandleClientAsync(connection, cancellationToken));
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
        private async Task DisconnectClientAsync(ClientConnection connection)
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
            LoggingService.Instance.Debug($"[TcpServer] {message}");
        private void LogInfo(string message) => 
            LoggingService.Instance.Info($"[TcpServer] {message}");
        private void LogError(string message) => 
            LoggingService.Instance.Error($"[TcpServer] {message}");
        
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
}
```

### 2. 메시지 파서 (독립적인 구현)

```csharp
using System;
using System.Text.RegularExpressions;

namespace SyncGuardMonitor.Models
{
    /// <summary>
    /// SyncGuard 메시지 파서
    /// 형식: IP_state (예: 192.168.0.201_state2)
    /// </summary>
    public class SyncMessage
    {
        // 정규식 패턴
        private static readonly Regex MessagePattern = 
            new Regex(@"^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})_state(\d)$", 
                RegexOptions.Compiled);
        
        // 프로퍼티
        public string RawData { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public SyncState State { get; set; } = SyncState.Unknown;
        public DateTime ReceivedTime { get; set; }
        public string SenderId { get; set; } = "";
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 메시지 파싱
        /// </summary>
        public static SyncMessage Parse(string rawMessage, string senderId)
        {
            var message = new SyncMessage
            {
                RawData = rawMessage,
                SenderId = senderId,
                ReceivedTime = DateTime.Now
            };
            
            try
            {
                if (string.IsNullOrWhiteSpace(rawMessage))
                {
                    throw new ArgumentException("메시지가 비어있습니다.");
                }
                
                // 공백 제거
                rawMessage = rawMessage.Trim();
                
                // 정규식 매칭
                var match = MessagePattern.Match(rawMessage);
                if (!match.Success)
                {
                    throw new FormatException($"잘못된 메시지 형식: {rawMessage}");
                }
                
                // IP 주소 추출
                message.IpAddress = match.Groups[1].Value;
                
                // IP 주소 유효성 검증
                if (!IsValidIpAddress(message.IpAddress))
                {
                    throw new FormatException($"잘못된 IP 주소: {message.IpAddress}");
                }
                
                // State 값 추출
                if (int.TryParse(match.Groups[2].Value, out int stateValue))
                {
                    message.State = stateValue switch
                    {
                        0 => SyncState.Error,
                        1 => SyncState.Slave,
                        2 => SyncState.Master,
                        _ => SyncState.Unknown
                    };
                }
                else
                {
                    throw new FormatException($"잘못된 상태 값: {match.Groups[2].Value}");
                }
                
                message.IsValid = true;
            }
            catch (Exception ex)
            {
                message.IsValid = false;
                message.ErrorMessage = ex.Message;
                message.State = SyncState.Unknown;
                
                LoggingService.Instance.Warning(
                    $"메시지 파싱 실패 [{senderId}]: {ex.Message} - Raw: {rawMessage}");
            }
            
            return message;
        }
        
        /// <summary>
        /// IP 주소 유효성 검증
        /// </summary>
        private static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;
            
            string[] octets = ipAddress.Split('.');
            if (octets.Length != 4)
                return false;
            
            foreach (string octet in octets)
            {
                if (!int.TryParse(octet, out int value))
                    return false;
                    
                if (value < 0 || value > 255)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 메시지를 문자열로 변환
        /// </summary>
        public override string ToString()
        {
            return IsValid ? 
                $"{IpAddress}_{State.ToStateString()}" : 
                $"Invalid: {ErrorMessage}";
        }
    }
    
    /// <summary>
    /// Sync 상태 열거형
    /// </summary>
    public enum SyncState
    {
        Unknown = -1,
        Error = 0,      // state0 - 동기화 오류 또는 미설정
        Slave = 1,      // state1 - Slave 모드
        Master = 2      // state2 - Master 모드
    }
    
    /// <summary>
    /// SyncState 확장 메서드
    /// </summary>
    public static class SyncStateExtensions
    {
        public static string ToStateString(this SyncState state)
        {
            return $"state{(int)state}";
        }
        
        public static string ToDisplayString(this SyncState state)
        {
            return state switch
            {
                SyncState.Master => "Master",
                SyncState.Slave => "Slave",
                SyncState.Error => "Error",
                SyncState.Unknown => "Unknown",
                _ => "Unknown"
            };
        }
        
        public static Color ToColor(this SyncState state)
        {
            return state switch
            {
                SyncState.Master => Color.Green,
                SyncState.Slave => Color.Orange,
                SyncState.Error => Color.Red,
                SyncState.Unknown => Color.Gray,
                _ => Color.Gray
            };
        }
    }
}
```

### 3. 클라이언트 정보 모델

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncGuardMonitor.Models
{
    /// <summary>
    /// 모니터링 중인 클라이언트 정보
    /// </summary>
    public class ClientInfo
    {
        // 기본 정보
        public string ClientId { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        
        // 상태 정보
        public SyncState CurrentState { get; set; } = SyncState.Unknown;
        public SyncState PreviousState { get; set; } = SyncState.Unknown;
        public DateTime FirstSeen { get; set; }
        public DateTime LastReceived { get; set; }
        public DateTime StateChangedTime { get; set; }
        public TimeSpan StateDuration => DateTime.Now - StateChangedTime;
        
        // 통계
        public long TotalMessages { get; set; }
        public long TotalBytes { get; set; }
        public double MessagesPerSecond { get; set; }
        public int StateChangeCount { get; set; }
        
        // 연결 정보
        public bool IsConnected { get; set; }
        public bool IsActive => (DateTime.Now - LastReceived).TotalSeconds < 30;
        public TimeSpan InactiveDuration => IsActive ? TimeSpan.Zero : DateTime.Now - LastReceived;
        
        // 이력
        public List<StateHistory> History { get; set; } = new List<StateHistory>();
        public Queue<MessageLog> RecentMessages { get; set; } = new Queue<MessageLog>();
        
        // 알림 설정
        public bool EnableNotifications { get; set; } = true;
        public SyncState? NotifyOnState { get; set; }
        
        /// <summary>
        /// 상태 업데이트
        /// </summary>
        public void UpdateState(SyncState newState)
        {
            if (CurrentState != newState)
            {
                PreviousState = CurrentState;
                CurrentState = newState;
                StateChangedTime = DateTime.Now;
                StateChangeCount++;
                
                // 히스토리 추가
                History.Add(new StateHistory
                {
                    Timestamp = DateTime.Now,
                    FromState = PreviousState,
                    ToState = newState,
                    Duration = StateDuration
                });
                
                // 히스토리 크기 제한 (최대 1000개)
                if (History.Count > 1000)
                {
                    History.RemoveAt(0);
                }
            }
            
            LastReceived = DateTime.Now;
            TotalMessages++;
        }
        
        /// <summary>
        /// 메시지 로그 추가
        /// </summary>
        public void AddMessageLog(string message)
        {
            RecentMessages.Enqueue(new MessageLog
            {
                Timestamp = DateTime.Now,
                Message = message
            });
            
            // 최근 100개만 유지
            while (RecentMessages.Count > 100)
            {
                RecentMessages.Dequeue();
            }
        }
        
        /// <summary>
        /// 상태 통계 조회
        /// </summary>
        public Dictionary<SyncState, StateStatistics> GetStateStatistics()
        {
            var stats = new Dictionary<SyncState, StateStatistics>();
            
            // 현재 상태 포함
            if (History.Any())
            {
                var groups = History.GroupBy(h => h.ToState);
                
                foreach (var group in groups)
                {
                    var state = group.Key;
                    stats[state] = new StateStatistics
                    {
                        State = state,
                        Count = group.Count(),
                        TotalDuration = group.Sum(h => h.Duration.TotalSeconds),
                        AverageDuration = group.Average(h => h.Duration.TotalSeconds),
                        LastOccurrence = group.Max(h => h.Timestamp)
                    };
                }
            }
            
            // 현재 상태 추가/업데이트
            if (!stats.ContainsKey(CurrentState))
            {
                stats[CurrentState] = new StateStatistics { State = CurrentState };
            }
            
            stats[CurrentState].Count++;
            stats[CurrentState].TotalDuration += StateDuration.TotalSeconds;
            stats[CurrentState].LastOccurrence = DateTime.Now;
            
            return stats;
        }
        
        /// <summary>
        /// 클라이언트 정보 요약
        /// </summary>
        public ClientSummary GetSummary()
        {
            return new ClientSummary
            {
                IpAddress = IpAddress,
                DisplayName = DisplayName,
                CurrentState = CurrentState,
                IsActive = IsActive,
                Uptime = DateTime.Now - FirstSeen,
                TotalMessages = TotalMessages,
                StateChangeCount = StateChangeCount,
                LastStateChange = StateChangedTime,
                StateDuration = StateDuration
            };
        }
    }
    
    /// <summary>
    /// 상태 변경 이력
    /// </summary>
    public class StateHistory
    {
        public DateTime Timestamp { get; set; }
        public SyncState FromState { get; set; }
        public SyncState ToState { get; set; }
        public TimeSpan Duration { get; set; }
        public string? Notes { get; set; }
        
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] " +
                   $"{FromState.ToDisplayString()} → {ToState.ToDisplayString()} " +
                   $"(Duration: {Duration:hh\\:mm\\:ss})";
        }
    }
    
    /// <summary>
    /// 메시지 로그
    /// </summary>
    public class MessageLog
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
    }
    
    /// <summary>
    /// 상태별 통계
    /// </summary>
    public class StateStatistics
    {
        public SyncState State { get; set; }
        public int Count { get; set; }
        public double TotalDuration { get; set; }
        public double AverageDuration { get; set; }
        public DateTime LastOccurrence { get; set; }
    }
    
    /// <summary>
    /// 클라이언트 요약 정보
    /// </summary>
    public class ClientSummary
    {
        public string IpAddress { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public SyncState CurrentState { get; set; }
        public bool IsActive { get; set; }
        public TimeSpan Uptime { get; set; }
        public long TotalMessages { get; set; }
        public int StateChangeCount { get; set; }
        public DateTime LastStateChange { get; set; }
        public TimeSpan StateDuration { get; set; }
    }
}
```

### 4. 데이터 관리자

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncGuardMonitor.Services
{
    /// <summary>
    /// 클라이언트 데이터 중앙 관리자
    /// </summary>
    public class DataManager : IDisposable
    {
        // 싱글톤 인스턴스
        private static readonly Lazy<DataManager> instance = 
            new Lazy<DataManager>(() => new DataManager());
        public static DataManager Instance => instance.Value;
        
        // 데이터 저장소
        private readonly ConcurrentDictionary<string, ClientInfo> clients;
        private readonly ReaderWriterLockSlim dataLock;
        private readonly Timer cleanupTimer;
        
        // 통계
        private long totalMessagesProcessed = 0;
        private long totalStateChanges = 0;
        private DateTime startTime;
        
        // 설정
        private int inactiveTimeoutSeconds = 30;
        private int maxHistoryPerClient = 1000;
        
        // 이벤트
        public event EventHandler<ClientUpdateEventArgs>? ClientUpdated;
        public event EventHandler<StateChangeEventArgs>? StateChanged;
        public event EventHandler<ClientEventArgs>? ClientAdded;
        public event EventHandler<ClientEventArgs>? ClientRemoved;
        public event EventHandler<DataStatisticsEventArgs>? StatisticsUpdated;
        
        private DataManager()
        {
            clients = new ConcurrentDictionary<string, ClientInfo>();
            dataLock = new ReaderWriterLockSlim();
            startTime = DateTime.Now;
            
            // 정리 타이머 (30초마다)
            cleanupTimer = new Timer(CleanupInactiveClients, null, 
                TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }
        
        /// <summary>
        /// 클라이언트 메시지 처리
        /// </summary>
        public void ProcessMessage(SyncMessage message, string senderId)
        {
            if (message == null || !message.IsValid)
                return;
            
            dataLock.EnterWriteLock();
            try
            {
                var isNewClient = false;
                var previousState = SyncState.Unknown;
                
                // 클라이언트 조회 또는 생성
                var client = clients.AddOrUpdate(message.IpAddress,
                    // 새 클라이언트 생성
                    (ip) =>
                    {
                        isNewClient = true;
                        return new ClientInfo
                        {
                            ClientId = Guid.NewGuid().ToString(),
                            IpAddress = ip,
                            DisplayName = $"Client_{ip}",
                            FirstSeen = DateTime.Now,
                            LastReceived = DateTime.Now,
                            StateChangedTime = DateTime.Now,
                            CurrentState = message.State,
                            IsConnected = true
                        };
                    },
                    // 기존 클라이언트 업데이트
                    (ip, existing) =>
                    {
                        previousState = existing.CurrentState;
                        existing.UpdateState(message.State);
                        existing.AddMessageLog(message.RawData);
                        existing.IsConnected = true;
                        return existing;
                    });
                
                // 통계 업데이트
                Interlocked.Increment(ref totalMessagesProcessed);
                
                // 이벤트 발생
                if (isNewClient)
                {
                    LogInfo($"새 클라이언트 추가: {client.IpAddress}");
                    ClientAdded?.Invoke(this, new ClientEventArgs { Client = client });
                }
                
                ClientUpdated?.Invoke(this, new ClientUpdateEventArgs
                {
                    Client = client,
                    IsNewClient = isNewClient,
                    Message = message
                });
                
                // 상태 변경 확인
                if (!isNewClient && previousState != message.State)
                {
                    Interlocked.Increment(ref totalStateChanges);
                    
                    LogInfo($"상태 변경 감지: {client.IpAddress} - " +
                           $"{previousState.ToDisplayString()} → {message.State.ToDisplayString()}");
                    
                    StateChanged?.Invoke(this, new StateChangeEventArgs
                    {
                        Client = client,
                        PreviousState = previousState,
                        NewState = message.State,
                        Timestamp = DateTime.Now
                    });
                }
                
                // 주기적 통계 업데이트
                if (totalMessagesProcessed % 100 == 0)
                {
                    UpdateStatistics();
                }
            }
            finally
            {
                dataLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 모든 클라이언트 조회
        /// </summary>
        public IEnumerable<ClientInfo> GetAllClients()
        {
            dataLock.EnterReadLock();
            try
            {
                return clients.Values.OrderBy(c => c.IpAddress).ToList();
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 활성 클라이언트만 조회
        /// </summary>
        public IEnumerable<ClientInfo> GetActiveClients()
        {
            dataLock.EnterReadLock();
            try
            {
                return clients.Values
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.IpAddress)
                    .ToList();
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 특정 클라이언트 조회
        /// </summary>
        public ClientInfo? GetClient(string ipAddress)
        {
            dataLock.EnterReadLock();
            try
            {
                clients.TryGetValue(ipAddress, out var client);
                return client;
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 상태별 클라이언트 수 조회
        /// </summary>
        public Dictionary<SyncState, int> GetStateDistribution()
        {
            dataLock.EnterReadLock();
            try
            {
                return clients.Values
                    .Where(c => c.IsActive)
                    .GroupBy(c => c.CurrentState)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 전체 통계 조회
        /// </summary>
        public DataStatistics GetStatistics()
        {
            dataLock.EnterReadLock();
            try
            {
                var allClients = clients.Values.ToList();
                var activeClients = allClients.Where(c => c.IsActive).ToList();
                
                return new DataStatistics
                {
                    TotalClients = allClients.Count,
                    ActiveClients = activeClients.Count,
                    InactiveClients = allClients.Count - activeClients.Count,
                    TotalMessages = totalMessagesProcessed,
                    TotalStateChanges = totalStateChanges,
                    Uptime = DateTime.Now - startTime,
                    StateDistribution = GetStateDistribution(),
                    MessagesPerSecond = CalculateMessagesPerSecond(),
                    LastUpdateTime = DateTime.Now
                };
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 클라이언트 제거
        /// </summary>
        public bool RemoveClient(string ipAddress)
        {
            dataLock.EnterWriteLock();
            try
            {
                if (clients.TryRemove(ipAddress, out var client))
                {
                    LogInfo($"클라이언트 제거: {ipAddress}");
                    ClientRemoved?.Invoke(this, new ClientEventArgs { Client = client });
                    return true;
                }
                return false;
            }
            finally
            {
                dataLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 모든 데이터 초기화
        /// </summary>
        public void ClearAll()
        {
            dataLock.EnterWriteLock();
            try
            {
                clients.Clear();
                totalMessagesProcessed = 0;
                totalStateChanges = 0;
                startTime = DateTime.Now;
                
                LogInfo("모든 클라이언트 데이터가 초기화되었습니다.");
            }
            finally
            {
                dataLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 비활성 클라이언트 정리
        /// </summary>
        private void CleanupInactiveClients(object? state)
        {
            dataLock.EnterWriteLock();
            try
            {
                var cutoffTime = DateTime.Now.AddSeconds(-inactiveTimeoutSeconds);
                var inactiveClients = clients.Values
                    .Where(c => c.LastReceived < cutoffTime && c.IsConnected)
                    .ToList();
                
                foreach (var client in inactiveClients)
                {
                    client.IsConnected = false;
                    LogDebug($"클라이언트 비활성 처리: {client.IpAddress}");
                }
            }
            finally
            {
                dataLock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// 초당 메시지 수 계산
        /// </summary>
        private double CalculateMessagesPerSecond()
        {
            var uptime = (DateTime.Now - startTime).TotalSeconds;
            return uptime > 0 ? totalMessagesProcessed / uptime : 0;
        }
        
        /// <summary>
        /// 통계 업데이트
        /// </summary>
        private void UpdateStatistics()
        {
            var stats = GetStatistics();
            StatisticsUpdated?.Invoke(this, new DataStatisticsEventArgs { Statistics = stats });
        }
        
        // 로깅
        private void LogDebug(string message) => 
            LoggingService.Instance.Debug($"[DataManager] {message}");
        private void LogInfo(string message) => 
            LoggingService.Instance.Info($"[DataManager] {message}");
        
        public void Dispose()
        {
            cleanupTimer?.Dispose();
            dataLock?.Dispose();
        }
    }
    
    /// <summary>
    /// 데이터 통계
    /// </summary>
    public class DataStatistics
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public long TotalMessages { get; set; }
        public long TotalStateChanges { get; set; }
        public TimeSpan Uptime { get; set; }
        public Dictionary<SyncState, int> StateDistribution { get; set; } = new();
        public double MessagesPerSecond { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}
```

### 5. UI 업데이트 관리자

```csharp
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SyncGuardMonitor.UI
{
    /// <summary>
    /// UI 업데이트 중앙 관리자
    /// </summary>
    public class UIUpdateManager
    {
        private readonly DataGridView clientGrid;
        private readonly RichTextBox logBox;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly Label statsLabel;
        private readonly System.Windows.Forms.Timer refreshTimer;
        
        // UI 설정
        private readonly object uiLock = new object();
        private readonly Queue<LogEntry> logQueue = new Queue<LogEntry>();
        private readonly Dictionary<string, DataGridViewRow> rowCache = new();
        private readonly Dictionary<SyncState, Bitmap> iconCache = new();
        
        // 통계
        private int uiUpdateCount = 0;
        private DateTime lastUpdateTime = DateTime.Now;
        
        public UIUpdateManager(
            DataGridView grid, 
            RichTextBox log, 
            ToolStripStatusLabel status,
            Label stats)
        {
            clientGrid = grid;
            logBox = log;
            statusLabel = status;
            statsLabel = stats;
            
            InitializeGrid();
            InitializeIcons();
            
            // UI 새로고침 타이머 (100ms)
            refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }
        
        /// <summary>
        /// 그리드 초기화
        /// </summary>
        private void InitializeGrid()
        {
            clientGrid.AutoGenerateColumns = false;
            clientGrid.AllowUserToAddRows = false;
            clientGrid.AllowUserToDeleteRows = false;
            clientGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            clientGrid.MultiSelect = false;
            clientGrid.RowHeadersVisible = false;
            clientGrid.BackgroundColor = Color.White;
            clientGrid.BorderStyle = BorderStyle.None;
            clientGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            clientGrid.GridColor = Color.LightGray;
            clientGrid.DefaultCellStyle.SelectionBackColor = Color.LightBlue;
            clientGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
            
            // 컬럼 정의
            var columns = new[]
            {
                new DataGridViewTextBoxColumn
                {
                    Name = "colIP",
                    HeaderText = "IP 주소",
                    Width = 120,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colName",
                    HeaderText = "이름",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colState",
                    HeaderText = "상태",
                    Width = 80,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colLastReceived",
                    HeaderText = "마지막 수신",
                    Width = 150,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colDuration",
                    HeaderText = "지속시간",
                    Width = 100,
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn
                {
                    Name = "colMessages",
                    HeaderText = "메시지",
                    Width = 80,
                    ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle 
                    { 
                        Alignment = DataGridViewContentAlignment.MiddleRight 
                    }
                },
                new DataGridViewImageColumn
                {
                    Name = "colStatus",
                    HeaderText = "●",
                    Width = 30,
                    ImageLayout = DataGridViewImageCellLayout.Zoom
                }
            };
            
            clientGrid.Columns.AddRange(columns);
            
            // 더블 버퍼링 활성화
            EnableDoubleBuffering(clientGrid);
        }
        
        /// <summary>
        /// 아이콘 초기화
        /// </summary>
        private void InitializeIcons()
        {
            iconCache[SyncState.Master] = CreateStatusIcon(Color.Green);
            iconCache[SyncState.Slave] = CreateStatusIcon(Color.Orange);
            iconCache[SyncState.Error] = CreateStatusIcon(Color.Red);
            iconCache[SyncState.Unknown] = CreateStatusIcon(Color.Gray);
        }
        
        /// <summary>
        /// 클라이언트 업데이트
        /// </summary>
        public void UpdateClient(ClientInfo client)
        {
            if (clientGrid.InvokeRequired)
            {
                clientGrid.BeginInvoke(new Action(() => UpdateClient(client)));
                return;
            }
            
            lock (uiLock)
            {
                try
                {
                    DataGridViewRow row;
                    
                    // 캐시에서 행 찾기
                    if (!rowCache.TryGetValue(client.IpAddress, out row))
                    {
                        // 새 행 생성
                        int index = clientGrid.Rows.Add();
                        row = clientGrid.Rows[index];
                        rowCache[client.IpAddress] = row;
                        
                        // 추가 애니메이션
                        AnimateNewRow(row);
                    }
                    
                    // 데이터 업데이트
                    row.Cells["colIP"].Value = client.IpAddress;
                    row.Cells["colName"].Value = client.DisplayName;
                    row.Cells["colState"].Value = client.CurrentState.ToDisplayString();
                    row.Cells["colLastReceived"].Value = client.LastReceived.ToString("HH:mm:ss");
                    row.Cells["colDuration"].Value = FormatDuration(client.StateDuration);
                    row.Cells["colMessages"].Value = client.TotalMessages.ToString("N0");
                    row.Cells["colStatus"].Value = iconCache[client.CurrentState];
                    
                    // 행 스타일 업데이트
                    UpdateRowStyle(row, client);
                    
                    // 상태 변경 애니메이션
                    if (client.PreviousState != client.CurrentState && 
                        client.PreviousState != SyncState.Unknown)
                    {
                        AnimateStateChange(row);
                    }
                    
                    uiUpdateCount++;
                }
                catch (Exception ex)
                {
                    LogError($"UI 업데이트 오류: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 행 스타일 업데이트
        /// </summary>
        private void UpdateRowStyle(DataGridViewRow row, ClientInfo client)
        {
            // 배경색
            Color backColor = client.CurrentState switch
            {
                SyncState.Master => Color.FromArgb(240, 255, 240),  // 연한 초록
                SyncState.Slave => Color.FromArgb(255, 250, 240),   // 연한 주황
                SyncState.Error => Color.FromArgb(255, 240, 240),   // 연한 빨강
                _ => Color.White
            };
            
            // 비활성 상태
            if (!client.IsActive)
            {
                backColor = Color.FromArgb(245, 245, 245);  // 연한 회색
                row.DefaultCellStyle.ForeColor = Color.Gray;
                row.DefaultCellStyle.Font = new Font(clientGrid.Font, FontStyle.Italic);
            }
            else
            {
                row.DefaultCellStyle.ForeColor = Color.Black;
                row.DefaultCellStyle.Font = clientGrid.Font;
            }
            
            foreach (DataGridViewCell cell in row.Cells)
            {
                cell.Style.BackColor = backColor;
            }
        }
        
        /// <summary>
        /// 로그 추가
        /// </summary>
        public void AddLog(string message, LogLevel level = LogLevel.Info, string? category = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Level = level,
                Category = category ?? "System"
            };
            
            lock (logQueue)
            {
                logQueue.Enqueue(entry);
                
                // 큐 크기 제한
                while (logQueue.Count > 100)
                {
                    logQueue.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// 상태바 업데이트
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (statusLabel.GetCurrentParent()?.InvokeRequired == true)
            {
                statusLabel.GetCurrentParent().BeginInvoke(
                    new Action(() => UpdateStatus(message)));
                return;
            }
            
            statusLabel.Text = message;
        }
        
        /// <summary>
        /// 통계 레이블 업데이트
        /// </summary>
        public void UpdateStatistics(DataStatistics stats)
        {
            if (statsLabel.InvokeRequired)
            {
                statsLabel.BeginInvoke(new Action(() => UpdateStatistics(stats)));
                return;
            }
            
            statsLabel.Text = $"클라이언트: {stats.ActiveClients}/{stats.TotalClients} | " +
                             $"메시지: {stats.TotalMessages:N0} ({stats.MessagesPerSecond:F1}/s) | " +
                             $"Master: {stats.StateDistribution.GetValueOrDefault(SyncState.Master)} | " +
                             $"Slave: {stats.StateDistribution.GetValueOrDefault(SyncState.Slave)} | " +
                             $"Error: {stats.StateDistribution.GetValueOrDefault(SyncState.Error)}";
        }
        
        /// <summary>
        /// 타이머 틱 이벤트
        /// </summary>
        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // 로그 처리
            ProcessLogQueue();
            
            // 지속시간 업데이트
            UpdateDurations();
            
            // 성능 모니터링
            if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
            {
                var ups = uiUpdateCount;
                uiUpdateCount = 0;
                lastUpdateTime = DateTime.Now;
                
                UpdateStatus($"준비 | UI 업데이트: {ups}/s");
            }
        }
        
        /// <summary>
        /// 로그 큐 처리
        /// </summary>
        private void ProcessLogQueue()
        {
            lock (logQueue)
            {
                while (logQueue.Count > 0)
                {
                    var entry = logQueue.Dequeue();
                    AppendLogEntry(entry);
                }
            }
        }
        
        /// <summary>
        /// 로그 엔트리 추가
        /// </summary>
        private void AppendLogEntry(LogEntry entry)
        {
            if (logBox.InvokeRequired)
            {
                logBox.BeginInvoke(new Action(() => AppendLogEntry(entry)));
                return;
            }
            
            try
            {
                // 아이콘
                string icon = entry.Level switch
                {
                    LogLevel.Debug => "🔍",
                    LogLevel.Info => "ℹ️",
                    LogLevel.Success => "✅",
                    LogLevel.Warning => "⚠️",
                    LogLevel.Error => "❌",
                    _ => "📝"
                };
                
                // 색상
                Color color = entry.Level switch
                {
                    LogLevel.Debug => Color.Gray,
                    LogLevel.Info => Color.Black,
                    LogLevel.Success => Color.Green,
                    LogLevel.Warning => Color.Orange,
                    LogLevel.Error => Color.Red,
                    _ => Color.Black
                };
                
                // 텍스트 추가
                string logText = $"[{entry.Timestamp:HH:mm:ss}] {icon} {entry.Message}\n";
                
                logBox.SelectionStart = logBox.TextLength;
                logBox.SelectionLength = 0;
                logBox.SelectionColor = color;
                logBox.AppendText(logText);
                
                // 최대 라인 수 유지
                if (logBox.Lines.Length > 1000)
                {
                    var lines = logBox.Lines.Skip(100).ToArray();
                    logBox.Lines = lines;
                }
                
                // 스크롤
                logBox.SelectionStart = logBox.TextLength;
                logBox.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 추가 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 지속시간 업데이트
        /// </summary>
        private void UpdateDurations()
        {
            if (clientGrid.InvokeRequired)
            {
                clientGrid.BeginInvoke(new Action(() => UpdateDurations()));
                return;
            }
            
            foreach (DataGridViewRow row in clientGrid.Rows)
            {
                var ip = row.Cells["colIP"].Value?.ToString();
                if (string.IsNullOrEmpty(ip))
                    continue;
                
                var client = DataManager.Instance.GetClient(ip);
                if (client != null)
                {
                    row.Cells["colDuration"].Value = FormatDuration(client.StateDuration);
                }
            }
        }
        
        /// <summary>
        /// 애니메이션 효과
        /// </summary>
        private async void AnimateNewRow(DataGridViewRow row)
        {
            var originalColor = row.DefaultCellStyle.BackColor;
            row.DefaultCellStyle.BackColor = Color.LightBlue;
            clientGrid.Refresh();
            
            await Task.Delay(500);
            
            row.DefaultCellStyle.BackColor = originalColor;
            clientGrid.Refresh();
        }
        
        private async void AnimateStateChange(DataGridViewRow row)
        {
            for (int i = 0; i < 3; i++)
            {
                row.DefaultCellStyle.BackColor = Color.White;
                clientGrid.Refresh();
                await Task.Delay(100);
                
                UpdateRowStyle(row, DataManager.Instance.GetClient(
                    row.Cells["colIP"].Value?.ToString() ?? "")!);
                clientGrid.Refresh();
                await Task.Delay(100);
            }
        }
        
        /// <summary>
        /// 상태 아이콘 생성
        /// </summary>
        private Bitmap CreateStatusIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
                
                using (var pen = new Pen(Color.FromArgb(64, Color.Black), 1))
                {
                    g.DrawEllipse(pen, 2, 2, 12, 12);
                }
            }
            
            return bitmap;
        }
        
        /// <summary>
        /// 더블 버퍼링 활성화
        /// </summary>
        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
        
        /// <summary>
        /// 시간 포맷
        /// </summary>
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration:hh\\:mm\\:ss}";
            else
                return duration.ToString(@"hh\:mm\:ss");
        }
        
        /// <summary>
        /// 정리
        /// </summary>
        public void Dispose()
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            
            foreach (var icon in iconCache.Values)
            {
                icon?.Dispose();
            }
            
            iconCache.Clear();
            rowCache.Clear();
        }
    }
    
    /// <summary>
    /// 로그 엔트리
    /// </summary>
    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = "";
        public LogLevel Level { get; set; }
        public string Category { get; set; } = "";
    }
}
```

---

## 📊 통계 및 차트 구현

### 실시간 차트 컴포넌트

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace SyncGuardMonitor.UI
{
    /// <summary>
    /// 실시간 상태 차트
    /// </summary>
    public class RealtimeChart : UserControl
    {
        private readonly Queue<DataPoint> dataPoints = new();
        private readonly Timer updateTimer;
        private readonly int maxPoints = 60; // 60초 데이터
        
        public RealtimeChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw, true);
            
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            
            updateTimer = new Timer { Interval = 1000 };
            updateTimer.Tick += (s, e) => UpdateChart();
            updateTimer.Start();
        }
        
        private void UpdateChart()
        {
            var stats = DataManager.Instance.GetStateDistribution();
            
            dataPoints.Enqueue(new DataPoint
            {
                Timestamp = DateTime.Now,
                MasterCount = stats.GetValueOrDefault(SyncState.Master),
                SlaveCount = stats.GetValueOrDefault(SyncState.Slave),
                ErrorCount = stats.GetValueOrDefault(SyncState.Error)
            });
            
            while (dataPoints.Count > maxPoints)
            {
                dataPoints.Dequeue();
            }
            
            Invalidate();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (dataPoints.Count < 2)
                return;
            
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // 그리드 그리기
            DrawGrid(g);
            
            // 데이터 라인 그리기
            DrawDataLines(g);
            
            // 범례 그리기
            DrawLegend(g);
        }
        
        private void DrawGrid(Graphics g)
        {
            using (var pen = new Pen(Color.LightGray, 1))
            {
                // 가로선
                for (int i = 0; i <= 10; i++)
                {
                    float y = Height * i / 10f;
                    g.DrawLine(pen, 0, y, Width, y);
                }
                
                // 세로선
                for (int i = 0; i <= 6; i++)
                {
                    float x = Width * i / 6f;
                    g.DrawLine(pen, x, 0, x, Height);
                }
            }
        }
        
        private void DrawDataLines(Graphics g)
        {
            var points = dataPoints.ToArray();
            float xStep = (float)Width / (maxPoints - 1);
            
            // Master 라인 (초록)
            DrawLine(g, points, p => p.MasterCount, Color.Green);
            
            // Slave 라인 (주황)
            DrawLine(g, points, p => p.SlaveCount, Color.Orange);
            
            // Error 라인 (빨강)
            DrawLine(g, points, p => p.ErrorCount, Color.Red);
        }
        
        private void DrawLine(Graphics g, DataPoint[] points, 
            Func<DataPoint, int> valueSelector, Color color)
        {
            var maxValue = points.Max(p => 
                p.MasterCount + p.SlaveCount + p.ErrorCount);
            
            if (maxValue == 0) maxValue = 1;
            
            var linePoints = new List<PointF>();
            
            for (int i = 0; i < points.Length; i++)
            {
                float x = i * Width / (float)(maxPoints - 1);
                float y = Height - (valueSelector(points[i]) * Height / (float)maxValue);
                linePoints.Add(new PointF(x, y));
            }
            
            if (linePoints.Count > 1)
            {
                using (var pen = new Pen(color, 2))
                {
                    g.DrawLines(pen, linePoints.ToArray());
                }
            }
        }
        
        private void DrawLegend(Graphics g)
        {
            var font = new Font("Segoe UI", 9);
            var x = Width - 100;
            var y = 10;
            
            // Master
            using (var brush = new SolidBrush(Color.Green))
            {
                g.FillRectangle(brush, x, y, 10, 10);
                g.DrawString("Master", font, Brushes.Black, x + 15, y - 2);
            }
            
            // Slave
            y += 20;
            using (var brush = new SolidBrush(Color.Orange))
            {
                g.FillRectangle(brush, x, y, 10, 10);
                g.DrawString("Slave", font, Brushes.Black, x + 15, y - 2);
            }
            
            // Error
            y += 20;
            using (var brush = new SolidBrush(Color.Red))
            {
                g.FillRectangle(brush, x, y, 10, 10);
                g.DrawString("Error", font, Brushes.Black, x + 15, y - 2);
            }
        }
        
        private class DataPoint
        {
            public DateTime Timestamp { get; set; }
            public int MasterCount { get; set; }
            public int SlaveCount { get; set; }
            public int ErrorCount { get; set; }
        }
    }
}
```

---

## 🔌 독립 실행 및 배포

### 프로그램 진입점 (Program.cs)

```csharp
using System;
using System.Threading;
using System.Windows.Forms;

namespace SyncGuardMonitor
{
    /// <summary>
    /// SyncGuard Monitor - 독립 실행 프로그램
    /// </summary>
    internal static class Program
    {
        private static Mutex? mutex;
        
        [STAThread]
        static void Main()
        {
            // 단일 인스턴스 확인
            const string mutexName = "Global\\SyncGuardMonitor_SingleInstance";
            mutex = new Mutex(true, mutexName, out bool createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show(
                    "SyncGuard Monitor가 이미 실행 중입니다.", 
                    "중복 실행", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }
            
            try
            {
                // 애플리케이션 설정
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                
                // 전역 예외 처리
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                // 로깅 초기화
                LoggingService.Instance.Info("=== SyncGuard Monitor 시작 ===");
                LoggingService.Instance.Info($"버전: {Application.ProductVersion}");
                LoggingService.Instance.Info($"실행 경로: {Application.StartupPath}");
                LoggingService.Instance.Info($"OS: {Environment.OSVersion}");
                
                // 메인 폼 실행
                Application.Run(new MainForm());
            }
            finally
            {
                LoggingService.Instance.Info("=== SyncGuard Monitor 종료 ===");
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
        }
        
        private static void Application_ThreadException(object sender, 
            ThreadExceptionEventArgs e)
        {
            LoggingService.Instance.Error($"처리되지 않은 예외: {e.Exception}");
            
            MessageBox.Show(
                $"예기치 않은 오류가 발생했습니다:\n\n{e.Exception.Message}", 
                "오류", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
        
        private static void CurrentDomain_UnhandledException(object sender, 
            UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            LoggingService.Instance.Error($"도메인 예외: {ex?.Message ?? "Unknown"}");
        }
    }
}
```

### 빌드 및 배포 스크립트

```powershell
# build.ps1 - SyncGuard Monitor 빌드 스크립트

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SingleFile = $true,
    [switch]$SelfContained = $true
)

Write-Host "=== SyncGuard Monitor 빌드 시작 ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "SingleFile: $SingleFile"
Write-Host "SelfContained: $SelfContained"

# 프로젝트 경로
$projectPath = "$PSScriptRoot\src\SyncGuardMonitor\SyncGuardMonitor.csproj"
$outputPath = "$PSScriptRoot\build\output"

# 기존 빌드 정리
Write-Host "`n정리 중..." -ForegroundColor Yellow
dotnet clean $projectPath -c $Configuration

# 빌드
Write-Host "`n빌드 중..." -ForegroundColor Yellow
dotnet build $projectPath -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "빌드 실패!" -ForegroundColor Red
    exit 1
}

# 발행
Write-Host "`n발행 중..." -ForegroundColor Yellow
$publishArgs = @(
    "publish"
    $projectPath
    "-c", $Configuration
    "-r", $Runtime
    "-o", $outputPath
)

if ($SingleFile) {
    $publishArgs += "-p:PublishSingleFile=true"
    $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

if ($SelfContained) {
    $publishArgs += "--self-contained"
    $publishArgs += "-p:PublishTrimmed=true"
}

& dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "발행 실패!" -ForegroundColor Red
    exit 1
}

# 버전 정보
$versionInfo = (Get-Item "$outputPath\SyncGuardMonitor.exe").VersionInfo
Write-Host "`n=== 빌드 완료 ===" -ForegroundColor Green
Write-Host "출력 경로: $outputPath"
Write-Host "파일 버전: $($versionInfo.FileVersion)"
Write-Host "제품 버전: $($versionInfo.ProductVersion)"

# 파일 크기
$exeSize = (Get-Item "$outputPath\SyncGuardMonitor.exe").Length / 1MB
Write-Host "실행 파일 크기: $([math]::Round($exeSize, 2)) MB"
```

---

## 📋 프로젝트 파일 (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 기본 설정 -->
    <OutputType>WinExe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- 애플리케이션 정보 -->
    <AssemblyName>SyncGuardMonitor</AssemblyName>
    <RootNamespace>SyncGuardMonitor</RootNamespace>
    <ApplicationIcon>Resources\Icons\app.ico</ApplicationIcon>
    
    <!-- 버전 정보 -->
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <FileVersion>1.0.0.0</FileVersion>
    <ProductVersion>1.0.0</ProductVersion>
    
    <!-- 회사 정보 -->
    <Company>SyncGuard Monitor Team</Company>
    <Product>SyncGuard Monitor</Product>
    <Copyright>Copyright © 2025 SyncGuard Monitor Team</Copyright>
    <Description>TCP 기반 SyncGuard 상태 모니터링 독립 소프트웨어</Description>
    
    <!-- 빌드 옵션 -->
    <PlatformTarget>x64</PlatformTarget>
    <DebugType>embedded</DebugType>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishReadyToRun>true</PublishReadyToRun>
    <PublishTrimmed>true</PublishTrimmed>
    
    <!-- 코드 분석 -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <!-- NuGet 패키지 -->
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="7.0.0" />
  </ItemGroup>

  <!-- 리소스 파일 -->
  <ItemGroup>
    <EmbeddedResource Include="Resources\**\*.*" />
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <!-- 컴파일 시 경고 무시 -->
  <PropertyGroup>
    <NoWarn>CA1416</NoWarn> <!-- Windows 전용 API 경고 -->
  </PropertyGroup>

</Project>
```

---

## 🚀 설치 프로그램 (Inno Setup)

```pascal
; SyncGuard Monitor 설치 스크립트
; Inno Setup 6.2.0 이상 필요

#define MyAppName "SyncGuard Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SyncGuard Monitor Team"
#define MyAppURL "https://github.com/syncguardmonitor"
#define MyAppExeName "SyncGuardMonitor.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\build\installer
OutputBaseFilename=SyncGuardMonitor_Setup_{#MyAppVersion}
SetupIconFile=..\..\src\SyncGuardMonitor\Resources\Icons\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright=Copyright (C) 2025 {#MyAppPublisher}

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "autostart"; Description: "윈도우 시작 시 자동 실행"; GroupDescription: "추가 옵션:"; Flags: unchecked

[Files]
Source: "..\..\build\output\SyncGuardMonitor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\build\output\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\SyncGuardMonitor"
Type: filesandordirs; Name: "{app}\logs"

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // .NET 6.0 런타임 확인
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\Microsoft.WindowsDesktop.App') then
  begin
    if MsgBox('.NET 6.0 Desktop Runtime이 설치되어 있지 않습니다. 지금 다운로드하시겠습니까?', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/6.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;
```

---

## 📝 사용자 매뉴얼

### 1. 시작하기

**SyncGuard Monitor**는 SyncGuard 클라이언트들의 상태를 중앙에서 모니터링하는 독립적인 소프트웨어입니다.

#### 시스템 요구사항
- Windows 10/11 (64-bit)
- .NET 6.0 Desktop Runtime
- 최소 RAM: 2GB
- 디스크 공간: 100MB

#### 설치 방법
1. `SyncGuardMonitor_Setup_1.0.0.exe` 실행
2. 설치 마법사 지시에 따라 진행
3. 설치 완료 후 바탕화면 아이콘 또는 시작 메뉴에서 실행

### 2. 기본 사용법

#### 서버 시작
1. 프로그램 실행 후 상단의 **[▶ 시작]** 버튼 클릭
2. 기본 포트(8080)로 서버가 시작됨
3. SyncGuard 클라이언트에서 이 IP와 포트로 연결 설정

#### 포트 변경
1. **[⚙ 설정]** 버튼 클릭
2. "네트워크 설정" 탭에서 포트 번호 변경
3. 저장 후 서버 재시작

### 3. 화면 구성

#### 클라이언트 목록
- **IP 주소**: 클라이언트의 IP
- **상태**: Master(초록), Slave(노랑), Error(빨강)
- **마지막 수신**: 최근 메시지 수신 시간
- **지속시간**: 현재 상태 유지 시간
- **메시지**: 총 수신 메시지 수

#### 실시간 차트
- 시간대별 상태 변화 추이
- 상태별 클라이언트 수 분포

#### 로그 창
- 모든 이벤트 실시간 표시
- 색상으로 구분된 로그 레벨

### 4. 고급 기능

#### 데이터 내보내기
1. 메뉴 → 파일 → 데이터 내보내기
2. CSV 형식으로 저장
3. Excel에서 열어 분석 가능

#### 알림 설정
1. 특정 클라이언트 우클릭
2. "알림 설정" 선택
3. 상태 변경 시 알림 받기

---

## 🔒 보안 및 네트워크

### 방화벽 설정

```powershell
# Windows 방화벽 규칙 추가
New-NetFirewallRule -DisplayName "SyncGuard Monitor TCP Server" `
                    -Direction Inbound `
                    -Protocol TCP `
                    -LocalPort 8080 `
                    -Action Allow `
                    -Profile Domain,Private
```

### 네트워크 보안 권장사항
1. **로컬 네트워크 전용**: 인터넷 노출 금지
2. **포트 변경**: 기본 포트(8080) 대신 다른 포트 사용
3. **IP 화이트리스트**: 특정 IP만 허용 설정
4. **정기 로그 검토**: 비정상 접속 시도 확인

---

## 🧪 테스트 시나리오

### 1. 기본 기능 테스트

```csharp
[TestClass]
public class BasicFunctionalityTests
{
    [TestMethod]
    public async Task Server_Should_Start_And_Stop()
    {
        // Arrange
        var server = new TcpServer();
        
        // Act & Assert - Start
        await server.StartAsync(18080);
        Assert.IsTrue(server.IsRunning);
        Assert.AreEqual(18080, server.Port);
        
        // Act & Assert - Stop
        await server.StopAsync();
        Assert.IsFalse(server.IsRunning);
    }
    
    [TestMethod]
    public void Message_Should_Parse_Correctly()
    {
        // Arrange
        var rawMessage = "192.168.0.201_state2";
        
        // Act
        var message = SyncMessage.Parse(rawMessage, "test-client");
        
        // Assert
        Assert.IsTrue(message.IsValid);
        Assert.AreEqual("192.168.0.201", message.IpAddress);
        Assert.AreEqual(SyncState.Master, message.State);
    }
}
```

### 2. 부하 테스트

```csharp
[TestClass]
public class LoadTests
{
    [TestMethod]
    public async Task Server_Should_Handle_Multiple_Clients()
    {
        // Arrange
        var server = new TcpServer();
        await server.StartAsync(18081);
        var messageCount = 0;
        
        server.MessageReceived += (s, e) => 
            Interlocked.Increment(ref messageCount);
        
        // Act - 10개 클라이언트 동시 연결
        var tasks = new List<Task>();
        for (int i = 1; i <= 10; i++)
        {
            int clientNum = i;
            tasks.Add(Task.Run(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync("localhost", 18081);
                
                var message = $"192.168.0.{200 + clientNum}_state1\r\n";
                var data = Encoding.UTF8.GetBytes(message);
                
                // 각 클라이언트가 100개 메시지 전송
                for (int j = 0; j < 100; j++)
                {
                    await client.GetStream().WriteAsync(data);
                    await Task.Delay(10);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        await Task.Delay(1000); // 처리 대기
        
        // Assert
        Assert.AreEqual(1000, messageCount); // 10 clients * 100 messages
        
        await server.StopAsync();
    }
}
```

### 3. 시뮬레이터 (테스트용)

```csharp
/// <summary>
/// SyncGuard 클라이언트 시뮬레이터
/// </summary>
public class ClientSimulator
{
    private readonly string serverIp;
    private readonly int serverPort;
    private readonly string clientIp;
    private TcpClient? client;
    private readonly Random random = new Random();
    private bool isRunning = false;
    
    public ClientSimulator(string serverIp, int serverPort, string clientIp)
    {
        this.serverIp = serverIp;
        this.serverPort = serverPort;
        this.clientIp = clientIp;
    }
    
    public async Task StartAsync()
    {
        isRunning = true;
        client = new TcpClient();
        await client.ConnectAsync(serverIp, serverPort);
        
        // 초기 상태
        var currentState = (SyncState)random.Next(0, 3);
        
        while (isRunning && client.Connected)
        {
            // 상태 변경 확률 (10%)
            if (random.Next(100) < 10)
            {
                currentState = (SyncState)random.Next(0, 3);
            }
            
            // 메시지 전송
            var message = $"{clientIp}_state{(int)currentState}\r\n";
            var data = Encoding.UTF8.GetBytes(message);
            
            await client.GetStream().WriteAsync(data);
            
            // 1-5초 간격
            await Task.Delay(random.Next(1000, 5000));
        }
    }
    
    public void Stop()
    {
        isRunning = false;
        client?.Close();
    }
}

// 시뮬레이터 실행 예제
public static async Task RunSimulation()
{
    var simulators = new List<ClientSimulator>();
    
    // 5개 가상 클라이언트 생성
    for (int i = 1; i <= 5; i++)
    {
        var simulator = new ClientSimulator(
            "localhost", 
            8080, 
            $"192.168.0.{200 + i}"
        );
        
        simulators.Add(simulator);
        _ = Task.Run(() => simulator.StartAsync());
    }
    
    // 1분간 실행
    await Task.Delay(60000);
    
    // 모든 시뮬레이터 중지
    simulators.ForEach(s => s.Stop());
}
```

---

## 🛠️ 문제 해결

### 자주 발생하는 문제

#### 1. 서버 시작 실패
**증상**: "포트가 이미 사용 중입니다" 오류
**해결**:
```cmd
# 포트 사용 프로세스 확인
netstat -ano | findstr :8080

# 프로세스 종료
taskkill /PID [프로세스ID] /F
```

#### 2. 클라이언트 연결 안됨
**증상**: SyncGuard에서 연결 실패
**해결**:
- Windows 방화벽에서 포트 허용
- 바이러스 백신 예외 추가
- 네트워크 연결 상태 확인

#### 3. 메모리 사용량 증가
**증상**: 장시간 실행 시 메모리 증가
**해결**:
- 설정에서 히스토리 보관 개수 줄이기
- 주기적으로 비활성 클라이언트 정리

### 로그 파일 위치
```
%LOCALAPPDATA%\SyncGuardMonitor\Logs\
└── monitor_YYYYMMDD.log
```

---

## 📊 성능 최적화

### 권장 설정값

| 항목 | 기본값 | 최대 성능 | 안정성 우선 |
|------|--------|-----------|-------------|
| 최대 동시 연결 | 100 | 500 | 50 |
| 메시지 버퍼 | 4KB | 16KB | 2KB |
| 비활성 타임아웃 | 30초 | 10초 | 60초 |
| 히스토리 보관 | 1000개 | 100개 | 5000개 |
| UI 새로고침 | 100ms | 500ms | 50ms |

### 대규모 모니터링 (100+ 클라이언트)
1. **전용 서버** 사용 권장
2. **SSD** 사용 (로그 쓰기 성능)
3. **RAM 8GB** 이상
4. **기가비트 네트워크**

---

## 🔄 업데이트 및 유지보수

### 자동 업데이트 확인

```csharp
public class UpdateChecker
{
    private const string UPDATE_URL = "https://api.github.com/repos/syncguardmonitor/releases/latest";
    
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "SyncGuardMonitor");
            
            var response = await client.GetStringAsync(UPDATE_URL);
            var json = JsonDocument.Parse(response);
            
            var latestVersion = json.RootElement.GetProperty("tag_name").GetString();
            var downloadUrl = json.RootElement
                .GetProperty("assets")[0]
                .GetProperty("browser_download_url")
                .GetString();
            
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString();
            
            if (IsNewerVersion(latestVersion, currentVersion))
            {
                return new UpdateInfo
                {
                    Version = latestVersion,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = json.RootElement
                        .GetProperty("body").GetString()
                };
            }
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error($"업데이트 확인 실패: {ex.Message}");
        }
        
        return null;
    }
}
```

---

## 📄 라이선스 및 저작권

### MIT License

```
MIT License

Copyright (c) 2025 SyncGuard Monitor Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🤝 기여 가이드라인

### 개발 참여 방법
1. 프로젝트 Fork
2. Feature 브랜치 생성 (`git checkout -b feature/AmazingFeature`)
3. 변경사항 커밋 (`git commit -m 'Add some AmazingFeature'`)
4. 브랜치 Push (`git push origin feature/AmazingFeature`)
5. Pull Request 생성

### 코딩 규칙
- C# 코딩 컨벤션 준수
- 모든 public 메서드에 XML 주석
- 단위 테스트 작성
- 코드 리뷰 필수

---

## 📞 지원 및 문의

### 기술 지원
- **GitHub Issues**: https://github.com/syncguardmonitor/issues
- **이메일**: support@syncguardmonitor.com
- **문서**: https://docs.syncguardmonitor.com

### 커뮤니티
- **Discord**: https://discord.gg/syncguardmonitor
- **포럼**: https://forum.syncguardmonitor.com

---

## 🎯 로드맵

### v1.1 (2025 Q2)
- [ ] 웹 대시보드 추가
- [ ] 실시간 알림 (이메일/Slack)
- [ ] 다국어 지원 (영어, 한국어, 일본어)

### v2.0 (2025 Q3)
- [ ] 양방향 통신 (명령 전송)
- [ ] 데이터베이스 연동 (PostgreSQL/MySQL)
- [ ] REST API 제공
- [ ] Docker 컨테이너 지원

### v3.0 (2025 Q4)
- [ ] 클라우드 모니터링 서비스
- [ ] 모바일 앱 (iOS/Android)
- [ ] AI 기반 이상 패턴 감지
- [ ] 엔터프라이즈 기능

---

## 🏁 결론

**SyncGuard Monitor**는 SyncGuard 시스템을 위한 완전히 독립적인 모니터링 솔루션입니다. 
이 문서는 개발부터 배포, 운영까지 모든 과정을 상세히 다루고 있습니다.

### 핵심 특징
- ✅ **완전 독립 소프트웨어**: SyncGuard와 별개로 개발/배포
- ✅ **실시간 모니터링**: TCP 기반 실시간 상태 수신
- ✅ **확장 가능**: 플러그인 및 API 지원
- ✅ **엔터프라이즈 준비**: 대규모 배포 지원

### 다음 단계
1. 소스 코드 다운로드
2. 개발 환경 설정
3. 빌드 및 테스트
4. 커스터마이징
5. 배포

**이제 SyncGuard Monitor를 활용하여 효율적인 모니터링 시스템을 구축하세요!**

---

*문서 버전: 1.0.0*  
*최종 수정: 2025-01-01*  
*작성자: SyncGuard Monitor Team*