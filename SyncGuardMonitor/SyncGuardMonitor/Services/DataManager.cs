using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyncGuardMonitor.Models;

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
        private readonly System.Threading.Timer cleanupTimer;
        
        // 통계
        private long totalMessagesProcessed = 0;
        private long totalStateChanges = 0;
        private DateTime startTime;
        
        // 설정
        private int inactiveTimeoutSeconds = 30;
        
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
            cleanupTimer = new System.Threading.Timer(CleanupInactiveClients, null, 
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
        /// 락 없이 특정 클라이언트 조회 (내부 전용)
        /// </summary>
        private ClientInfo? GetClientNoLock(string ipAddress)
        {
            clients.TryGetValue(ipAddress, out var client);
            return client;
        }
        
        /// <summary>
        /// 모든 클라이언트 조회 (락 한 번만)
        /// </summary>
        public List<ClientInfo> GetAllClientsList()
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
        /// 기존 GetAllClients는 IEnumerable에서 List로 반환 타입 변경
        /// </summary>
        public IEnumerable<ClientInfo> GetAllClients() => GetAllClientsList();
        
        /// <summary>
        /// 기존 GetActiveClients도 List로 반환
        /// </summary>
        public List<ClientInfo> GetActiveClientsList()
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
        public IEnumerable<ClientInfo> GetActiveClients() => GetActiveClientsList();
        
        /// <summary>
        /// 특정 클라이언트 조회
        /// </summary>
        public ClientInfo? GetClient(string ipAddress)
        {
            dataLock.EnterReadLock();
            try
            {
                return GetClientNoLock(ipAddress);
            }
            finally
            {
                dataLock.ExitReadLock();
            }
        }
        
        /// <summary>
        /// 락 없이 상태별 클라이언트 수 조회 (내부 전용)
        /// </summary>
        private Dictionary<SyncState, int> GetStateDistributionNoLock()
        {
            return clients.Values
                .Where(c => c.IsActive)
                .GroupBy(c => c.CurrentState)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        public Dictionary<SyncState, int> GetStateDistribution()
        {
            dataLock.EnterReadLock();
            try
            {
                return GetStateDistributionNoLock();
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
                    StateDistribution = GetStateDistributionNoLock(),
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
            System.Diagnostics.Debug.WriteLine($"[DataManager] {message}");
        private void LogInfo(string message) => 
            System.Diagnostics.Debug.WriteLine($"[DataManager] {message}");
        
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
    
    // 이벤트 아규먼트 클래스들
    public class ClientUpdateEventArgs : EventArgs
    {
        public ClientInfo Client { get; set; } = new ClientInfo();
        public bool IsNewClient { get; set; }
        public SyncMessage Message { get; set; } = new SyncMessage();
    }
    
    public class StateChangeEventArgs : EventArgs
    {
        public ClientInfo Client { get; set; } = new ClientInfo();
        public SyncState PreviousState { get; set; }
        public SyncState NewState { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ClientEventArgs : EventArgs
    {
        public ClientInfo Client { get; set; } = new ClientInfo();
    }
    
    public class DataStatisticsEventArgs : EventArgs
    {
        public DataStatistics Statistics { get; set; } = new DataStatistics();
    }
} 