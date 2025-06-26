using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Management;
using System.Runtime.Versioning;
using System.IO;

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
            Locked,
            Unlocked,
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
        private bool useWmiMethod = true; // WMI 방법 우선 사용
        
        public SyncChecker()
        {
            // WMI 방법이 사용 가능한지 확인
            if (!IsWmiMethodAvailable())
            {
                useWmiMethod = false;
                Logger.Info("WMI 방법을 사용할 수 없습니다. nvidia-smi 방법으로 대체합니다.");
            }
            
            // nvidia-smi가 사용 가능한지 확인 (백업용)
            if (!IsNvidiaSmiAvailable())
            {
                throw new InvalidOperationException("nvidia-smi를 찾을 수 없습니다. NVIDIA 드라이버가 설치되어 있는지 확인하세요.");
            }
        }
        
        public SyncStatus GetSyncStatus()
        {
            try
            {
                SyncStatus newStatus;
                
                if (useWmiMethod)
                {
                    // WMI 방법으로 Sync 상태 확인 (우선순위 1)
                    newStatus = GetSyncStatusFromWmi();
                }
                else
                {
                    // nvidia-smi 방법으로 Sync 상태 확인 (우선순위 2)
                    newStatus = GetSyncStatusFromNvidiaSmi();
                }
                
                lock (lockObject)
                {
                    // 상태 변경 시에만 업데이트
                    if (newStatus != lastStatus)
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
                        return SyncStatus.Unlocked;
                    }
                    
                    foreach (ManagementObject obj in collection)
                    {
                        try
                        {
                            // displaySyncState 값만 확인
                            int displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                            int id = Convert.ToInt32(obj["id"]);
                            
                            Logger.Debug($"Sync 디바이스: ID={id}, State={displaySyncState}");
                            
                            // displaySyncState 값으로 동기화 상태 판단
                            // 0 = UnSynced (동기화되지 않음) - 빨강
                            // 1 = Slave (슬레이브 모드 - 동기화됨) - 노랑
                            // 2 = Master (마스터 모드 - 동기화됨) - 초록
                            if (displaySyncState == 2)
                            {
                                Logger.Info($"디바이스 {id}가 마스터 상태입니다. (State: {displaySyncState})");
                                return SyncStatus.Locked; // 초록색
                            }
                            else if (displaySyncState == 1)
                            {
                                Logger.Info($"디바이스 {id}가 슬레이브 상태입니다. (State: {displaySyncState})");
                                return SyncStatus.Unknown; // 노란색
                            }
                            else if (displaySyncState == 0)
                            {
                                Logger.Info($"디바이스 {id}가 동기화되지 않은 상태입니다. (State: {displaySyncState})");
                                return SyncStatus.Error; // 빨간색
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Sync 디바이스 정보 추출 중 오류: {ex.Message}");
                        }
                    }
                    
                    // 모든 디바이스가 확인되었지만 명확한 상태가 없는 경우
                    return SyncStatus.Unknown; // 노란색
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"WMI Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error; // 빨간색
            }
        }
        
        private SyncStatus GetSyncStatusFromNvidiaSmi()
        {
            try
            {
                // nvidia-smi로 GPU 정보 가져오기
                var gpuInfo = GetNvidiaSmiInfo();
                
                // Sync 관련 정보 검색
                bool hasSyncInfo = CheckSyncInfo(gpuInfo);
                
                return hasSyncInfo ? SyncStatus.Locked : SyncStatus.Unlocked;
            }
            catch (Exception ex)
            {
                Logger.Error($"nvidia-smi Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error;
            }

        }
        
        private bool IsNvidiaSmiAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit(3000); // 3초 타임아웃
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
        
        private string GetNvidiaSmiInfo()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "-q", // 상세 정보 출력
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000); // 5초 타임아웃
            
            return output;
        }
        
        private bool CheckSyncInfo(string gpuInfo)
        {
            // 1. Quadro Sync 카드 확인
            bool hasQuadroSync = false;
            string[] quadroSyncKeywords = {
                "Quadro Sync",
                "Frame Lock",
                "Genlock",
                "Synchronization"
            };
            
            foreach (string keyword in quadroSyncKeywords)
            {
                if (gpuInfo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    hasQuadroSync = true;
                    break;
                }
            }
            
            // Quadro Sync 카드가 없으면 Unlocked
            if (!hasQuadroSync)
            {
                return false;
            }
            
            // 2. 실제 Sync Lock 상태 확인
            // nvidia-smi에서 더 구체적인 Sync 정보 찾기
            string[] syncLockKeywords = {
                "Frame Lock: Enabled",
                "Sync: Active",
                "Lock: Enabled",
                "Synchronization: Active"
            };
            
            foreach (string keyword in syncLockKeywords)
            {
                if (gpuInfo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true; // Sync Locked
                }
            }
            
            // 3. Master/Slave 상태 확인
            string[] masterSlaveKeywords = {
                "Master",
                "Slave",
                "Sync Master",
                "Sync Slave"
            };
            
            foreach (string keyword in masterSlaveKeywords)
            {
                if (gpuInfo.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    // Master/Slave 정보가 있으면 Sync 가능한 상태로 간주
                    return true;
                }
            }
            
            // 4. Quadro GPU 확인 (마지막 수단)
            if (gpuInfo.Contains("Quadro", StringComparison.OrdinalIgnoreCase))
            {
                // Quadro GPU가 있지만 Sync 정보가 불분명한 경우
                // 실제 Sync 상태를 알 수 없으므로 Unknown으로 처리
                return false; // Unlocked로 처리
            }
            
            return false; // Sync 정보 없음
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
                result.IsSynced = (status == SyncStatus.Locked);
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
        
        public void Dispose()
        {
            // nvidia-smi는 별도 정리 작업이 필요 없음
        }
    }

    public class Logger
    {
        private static readonly object lockObject = new object();
        private static readonly string logDirectory = "logs";
        private static readonly int maxFileSizeMB = 10;
        private static LogLevel currentLogLevel = LogLevel.INFO;
        
        static Logger()
        {
            // 로그 디렉토리 생성
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
                    
                    // 파일에 로그 저장
                    string logFile = Path.Combine(logDirectory, $"syncguard_{DateTime.Now:yyyy-MM-dd}.txt");
                    
                    // 파일 크기 확인 및 로테이션
                    if (File.Exists(logFile))
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.Length > maxFileSizeMB * 1024 * 1024)
                        {
                            string backupFile = Path.Combine(logDirectory, $"syncguard_{DateTime.Now:yyyy-MM-dd}_{DateTime.Now:HHmmss}.txt");
                            File.Move(logFile, backupFile);
                        }
                    }
                    
                    File.AppendAllText(logFile, logEntry + Environment.NewLine);
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
