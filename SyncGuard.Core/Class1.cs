using System;
using System.Text.RegularExpressions;
using System.Management;
using System.Runtime.Versioning;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncGuard.Core
{
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARNING,
        ERROR
    }
    
    [SupportedOSPlatform("windows")]
    public class SyncChecker : IDisposable
    {
        public enum SyncStatus
        {
            Unknown,
            Master,
            Slave,
            Error
        }
        
        public enum SyncRole
        {
            Master,
            Slave
        }
        
        // Sync 상태 변경 이벤트 추가
        public event EventHandler<SyncStatus>? SyncStatusChanged;
        
        private SyncStatus lastStatus = SyncStatus.Unknown;
        private SyncRole currentRole = SyncRole.Slave; // 기본값은 Slave
        private readonly object lockObject = new object();
        private ManagementObject? syncTopology;
        private bool isClientEnabled = false;
        private string targetServerIp = "192.168.1.100";
        private int targetServerPort = 8080;
        
        public SyncChecker()
        {
            // WMI 방법이 사용 가능한지 확인
            if (!IsWmiMethodAvailable())
            {
                throw new InvalidOperationException("WMI SyncTopology를 찾을 수 없습니다. NVIDIA 드라이버가 설치되어 있는지 확인하세요.");
            }
            
            InitializeSyncTopology();
        }
        
        private void InitializeSyncTopology()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\WMI");
                var query = new SelectQuery("SELECT * FROM SyncTopology");
                var searcher = new ManagementObjectSearcher(scope, query);
                var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    syncTopology = obj;
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SyncTopology 초기화 실패: {ex.Message}");
            }
        }
        
        public SyncStatus GetSyncStatus()
        {
            try
            {
                SyncStatus newStatus = GetSyncStatusFromWmi();
                
                lock (lockObject)
                {
                    // 초기 상태이거나 상태 변경 시에만 업데이트
                    if (lastStatus == SyncStatus.Unknown || newStatus != lastStatus)
                    {
                        lastStatus = newStatus;
                        // 상태 변경 이벤트 발생
                        SyncStatusChanged?.Invoke(this, newStatus);
                    }
                    
                    return lastStatus;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error;
            }
        }
        
        public SyncRole GetUserRole()
        {
            lock (lockObject)
            {
                return currentRole;
            }
        }
        
        public void SetUserRole(SyncRole role)
        {
            lock (lockObject)
            {
                currentRole = role;
                Logger.Info($"사용자 역할이 {role}로 설정되었습니다.");
            }
        }
        
        private bool IsWmiMethodAvailable()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2\\NV", "SELECT * FROM SyncTopology"))
                {
                    var collection = searcher.Get();
                    return collection.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }
        
        private SyncStatus GetSyncStatusFromWmi()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2\\NV", "SELECT * FROM SyncTopology"))
                {
                    var collection = searcher.Get();
                    
                    if (collection.Count == 0)
                    {
                        Logger.Warning("SyncTopology WMI 클래스에서 Sync 디바이스를 찾을 수 없습니다.");
                        return SyncStatus.Slave;
                    }

                    bool foundMaster = false;
                    bool foundSlave = false;
                    bool foundError = false;

                    foreach (ManagementObject obj in collection)
                    {
                        try
                        {
                            // displaySyncState 값 확인
                            int displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                            int id = Convert.ToInt32(obj["id"]);
                            bool isDisplayMasterable = Convert.ToBoolean(obj["isDisplayMasterable"]);
                            
                            Logger.Debug($"Sync 디바이스: ID={id}, State={displaySyncState}, Masterable={isDisplayMasterable}");
                            
                            // displaySyncState 값으로 동기화 설정 상태 판단
                            // 0 = UnSynced (동기화 설정 안됨) - 빨강
                            // 1 = Slave (슬레이브 모드 - 동기화 설정됨) - 노랑
                            // 2 = Master (마스터 모드 - 동기화 설정됨) - 초록
                            if (displaySyncState == 2)
                            {
                                Logger.Info($"디바이스 {id}가 마스터 상태입니다. (State: {displaySyncState})");
                                foundMaster = true;
                            }
                            else if (displaySyncState == 1)
                            {
                                Logger.Info($"디바이스 {id}가 슬레이브 상태입니다. (State: {displaySyncState})");
                                foundSlave = true;
                            }
                            else if (displaySyncState == 0)
                            {
                                Logger.Info($"디바이스 {id}가 동기화되지 않은 상태입니다. (State: {displaySyncState})");
                                foundError = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Sync 디바이스 정보 추출 중 오류: {ex.Message}");
                        }
                    }
                    
                    // 우선순위: Master > Slave > Error > Unknown
                    if (foundMaster)
                    {
                        Logger.Info("마스터 디바이스가 발견되어 Master 상태로 설정합니다.");
                        return SyncStatus.Master;
                    }
                    else if (foundSlave)
                    {
                        Logger.Info("슬레이브 디바이스가 발견되어 Slave 상태로 설정합니다.");
                        return SyncStatus.Slave;
                    }
                    else if (foundError)
                    {
                        Logger.Info("동기화되지 않은 디바이스가 발견되어 Error 상태로 설정합니다.");
                        return SyncStatus.Error;
                    }
                    else
                    {
                        Logger.Warning("모든 디바이스의 상태를 확인했지만 명확한 상태가 없습니다.");
                        return SyncStatus.Unknown;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"WMI Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error; // 빨간색
            }
        }
        
        // 진단 결과를 위한 클래스
        public class DiagnosisResult
        {
            public bool IsSynced { get; set; }
            public string TimingServer { get; set; } = "";
            public string SelectedDisplay { get; set; } = "";
            public string DiagnosisMessage { get; set; } = "";
            public string RawData { get; set; } = "";
        }
        
        public DiagnosisResult DiagnoseDisplaySync()
        {
            var result = new DiagnosisResult();
            
            try
            {
                var status = GetSyncStatus();
                result.IsSynced = (status == SyncStatus.Master);
                result.DiagnosisMessage = $"Sync 상태: {status}";
                
                // WMI에서 상세 정보 수집
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2\\NV", "SELECT * FROM SyncTopology"))
                {
                    var collection = searcher.Get();
                    result.RawData = $"SyncTopology 항목 수: {collection.Count}\n";
                    
                    foreach (ManagementObject obj in collection)
                    {
                        int id = Convert.ToInt32(obj["id"]);
                        int displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                        string uname = obj["uname"]?.ToString() ?? "";
                        bool isDisplayMasterable = Convert.ToBoolean(obj["isDisplayMasterable"]);
                        
                        result.RawData += $"ID: {id}, State: {displaySyncState}, Name: {uname}, Masterable: {isDisplayMasterable}\n";
                        
                        if (displaySyncState == 2) // Master
                        {
                            result.TimingServer = $"Master Device {id}";
                        }
                        else if (displaySyncState == 1) // Slave
                        {
                            result.SelectedDisplay = $"Slave Device {id}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.DiagnosisMessage = $"진단 중 오류: {ex.Message}";
                result.RawData = ex.ToString();
            }
            
            return result;
        }
        
        public void RefreshSyncStatus()
        {
            // 상태를 강제로 새로고침
            Logger.Info("Sync 상태를 새로고침합니다...");
            GetSyncStatus(); // 상태를 다시 확인
        }
        
        public void ExploreNvidiaWmiClasses()
        {
            Logger.Info("=== NVIDIA WMI 클래스 탐색 ===");
            
            try
            {
                // 여러 네임스페이스에서 WMI 클래스 탐색
                string[] namespaces = { "root\\CIMV2", "root\\CIMV2\\NV", "root\\WMI" };
                
                foreach (string ns in namespaces)
                {
                    Logger.Info($"\n--- {ns} 네임스페이스 ---");
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher(ns, "SELECT * FROM Meta_Class WHERE __Class LIKE '%Sync%' OR __Class LIKE '%Display%' OR __Class LIKE '%GPU%'"))
                        {
                            var collection = searcher.Get();
                            Logger.Info($"관련 클래스 수: {collection.Count}");
                            
                            foreach (ManagementObject obj in collection)
                            {
                                string className = obj["__Class"]?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(className))
                                {
                                    Logger.Info($"  - {className}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"  오류: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"WMI 클래스 탐색 중 오류: {ex.Message}");
            }
        }
        
        // 외부 전송용 상태 변환
        private string GetExternalStatus()
        {
            lock (lockObject)
            {
                // 현재 PC의 IP 주소 가져오기
                string localIp = GetLocalIpAddress();
                
                string status = lastStatus switch
                {
                    SyncStatus.Master => "state2",  // State 2: Master
                    SyncStatus.Slave => "state1", // State 1: Slave
                    SyncStatus.Error => "state0",    // State 0: UnSynced
                    SyncStatus.Unknown => "state0",  // Unknown도 state0으로 처리
                    _ => "state0"
                };
                
                return $"{localIp}_{status}";
            }
        }
        
        // 현재 PC의 IP 주소 가져오기
        private string GetLocalIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"로컬 IP 주소 가져오기 실패: {ex.Message}");
            }
            
            return "unknown";
        }

        // 현재 상태 가져오기 (스레드 안전)
        public SyncStatus GetCurrentStatus()
        {
            lock (lockObject)
            {
                return lastStatus;
            }
        }
        
        // TCP 클라이언트 시작
        public void StartTcpClient(string ip, int port)
        {
            if (isClientEnabled)
            {
                Logger.Warning("TCP 클라이언트가 이미 실행 중입니다.");
                return;
            }

            try
            {
                targetServerIp = ip;
                targetServerPort = port;
                isClientEnabled = true;

                Logger.Info($"TCP 클라이언트 시작됨 - {ip}:{port}");
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 시작 실패: {ex.Message}");
            }
        }

        // TCP 클라이언트 중지
        public void StopTcpClient()
        {
            if (!isClientEnabled)
                return;

            try
            {
                isClientEnabled = false;
                Logger.Info("TCP 클라이언트 중지됨");
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 중지 실패: {ex.Message}");
            }
        }

        // 상태를 서버로 전송
        public async Task SendStatusToServer()
        {
            if (!isClientEnabled)
                return;

            try
            {
                using var client = new TcpClient();
                client.ReceiveTimeout = 5000;  // 5초 타임아웃
                client.SendTimeout = 5000;     // 5초 타임아웃
                
                Logger.Info($"TCP 서버에 연결 시도: {targetServerIp}:{targetServerPort}");
                await client.ConnectAsync(targetServerIp, targetServerPort);
                Logger.Info("TCP 서버 연결 성공");
                
                using var stream = client.GetStream();
                var status = GetExternalStatus();
                var message = status + "\r\n";  // 개행 문자 추가
                var data = Encoding.UTF8.GetBytes(message);
                
                Logger.Info($"메시지 전송 시작: '{status}' ({data.Length} bytes)");
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();  // 스트림 플러시
                
                Logger.Info($"상태 전송 완료: {status} -> {targetServerIp}:{targetServerPort}");
                
                // 연결을 잠시 유지하여 서버가 메시지를 받을 시간 제공
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Logger.Error($"상태 전송 실패: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            StopTcpClient();
            syncTopology?.Dispose();
        }
    }

    public class Logger
    {
        private static readonly object lockObject = new object();
        private static readonly string logDirectory = ".";
        private static readonly int maxFileSizeMB = 10;
        private static LogLevel currentLogLevel = LogLevel.INFO;
        
        static Logger()
        {
            // 로그 디렉토리 생성 (루트 디렉토리 사용)
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
        
        public static void SetLogLevel(LogLevel level)
        {
            currentLogLevel = level;
        }
        
        public static void Log(LogLevel level, string message)
        {
            if (level < currentLogLevel) return;
            
            lock (lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logEntry = $"[{timestamp}] [{level}] {message}";
                    
                    // 콘솔 출력
                    Console.WriteLine(logEntry);
                    
                    // 파일에 로그 저장 (루트 디렉토리)
                    string logFile = Path.Combine(logDirectory, "syncguard_log.txt");
                    
                    // 파일 크기 확인 및 로테이션
                    if (File.Exists(logFile))
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.Length > maxFileSizeMB * 1024 * 1024)
                        {
                            string backupFile = Path.Combine(logDirectory, $"syncguard_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                            File.Move(logFile, backupFile);
                        }
                    }
                    
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 로그 쓰기 실패: {ex.Message}");
                }
            }
        }
        
        public static void Debug(string message) => Log(LogLevel.DEBUG, message);
        public static void Info(string message) => Log(LogLevel.INFO, message);
        public static void Warning(string message) => Log(LogLevel.WARNING, message);
        public static void Error(string message) => Log(LogLevel.ERROR, message);
    }
}
