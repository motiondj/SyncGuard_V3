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