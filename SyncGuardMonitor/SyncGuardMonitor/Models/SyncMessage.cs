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
                
                // 로깅 서비스가 있으면 사용
                System.Diagnostics.Debug.WriteLine($"메시지 파싱 실패 [{senderId}]: {ex.Message} - Raw: {rawMessage}");
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
} 