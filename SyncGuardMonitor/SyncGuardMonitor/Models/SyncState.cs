using System.Drawing;

namespace SyncGuardMonitor.Models
{
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