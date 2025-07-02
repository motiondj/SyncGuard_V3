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
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;

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
        
        // 🔥 최적화를 위한 새로운 멤버 변수들
        private TcpClient? persistentClient;
        private NetworkStream? persistentStream;
        private readonly SemaphoreSlim connectionSemaphore = new(1, 1);
        private DateTime lastConnectionTime = DateTime.MinValue;
        private DateTime lastSuccessfulSend = DateTime.MinValue;
        private int reconnectAttempts = 0;
        private readonly CancellationTokenSource cancellationTokenSource = new();
        
        // 🔥 성능 모니터링용
        private long totalMessagesSent = 0;
        private long totalBytesSent = 0;
        private long connectionCount = 0;
        private long reconnectCount = 0;
        
        // 🔥 캐싱용
        private string? cachedLocalIp;
        private readonly Dictionary<(string ip, SyncStatus status), string> statusMessageCache = new();
        
        // 🔥 로그 집계용
        private readonly Dictionary<string, int> logAggregator = new();
        private DateTime lastLogFlush = DateTime.Now;
        
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
        
        // 🔥 연결 상태 확인 (최적화)
        private bool IsConnected()
        {
            try
            {
                if (persistentClient == null || !persistentClient.Connected)
                    return false;
                
                // 실제 연결 상태 테스트 (non-blocking)
                if (persistentClient.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (persistentClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        return false; // 연결 끊김
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        // 🔥 연결 확보 (최적화된 버전)
        private async Task<bool> EnsureConnectionAsync()
        {
            // 이미 연결되어 있으면 바로 반환
            if (IsConnected())
                return true;
            
            await connectionSemaphore.WaitAsync();
            try
            {
                // Double-check after acquiring semaphore
                if (IsConnected())
                    return true;
                
                // 재연결 간격 제한 (지수 백오프)
                var timeSinceLastAttempt = DateTime.Now - lastConnectionTime;
                var waitTime = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, reconnectAttempts), 30));
                
                if (timeSinceLastAttempt < waitTime)
                {
                    LogAggregated("connection_skip", $"재연결 대기 중 (다음 시도까지 {(waitTime - timeSinceLastAttempt).TotalSeconds:F0}초)", LogLevel.DEBUG);
                    return false;
                }
                
                // 기존 연결 정리
                if (persistentClient != null)
                {
                    try
                    {
                        persistentStream?.Close();
                        persistentClient.Close();
                    }
                    catch { }
                    finally
                    {
                        persistentStream = null;
                        persistentClient = null;
                    }
                }
                
                // 새 연결 시도
                Logger.Info($"TCP 서버 연결 시도: {targetServerIp}:{targetServerPort}");
                
                persistentClient = new TcpClient
                {
                    ReceiveTimeout = 5000,
                    SendTimeout = 5000,
                    NoDelay = true, // Nagle 알고리즘 비활성화 (지연 감소)
                };
                
                // 연결 (타임아웃 포함)
                var connectTask = persistentClient.ConnectAsync(targetServerIp, targetServerPort);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    throw new TimeoutException("연결 시간 초과");
                }
                
                await connectTask; // 예외 확인
                
                persistentStream = persistentClient.GetStream();
                
                // 성공
                lastConnectionTime = DateTime.Now;
                connectionCount++;
                
                if (reconnectAttempts > 0)
                {
                    reconnectCount++;
                    Logger.Info($"TCP 서버 재연결 성공 (시도 {reconnectAttempts}회 후)");
                }
                else
                {
                    Logger.Info($"TCP 서버 연결 성공 (지속 연결 모드)");
                }
                
                reconnectAttempts = 0;
                return true;
            }
            catch (Exception ex)
            {
                reconnectAttempts++;
                lastConnectionTime = DateTime.Now;
                
                var errorMsg = ex switch
                {
                    SocketException se => $"소켓 오류: {se.SocketErrorCode}",
                    TimeoutException => "연결 시간 초과",
                    _ => ex.Message
                };
                
                Logger.Error($"TCP 연결 실패 (시도 {reconnectAttempts}회): {errorMsg}");
                
                persistentStream = null;
                persistentClient = null;
                
                return false;
            }
            finally
            {
                connectionSemaphore.Release();
            }
        }
        
        // 🔥 최적화된 전송 메서드
        public async Task SendStatusToServer()
        {
            if (!isClientEnabled)
                return;
            
            try
            {
                // 연결 확인/재연결
                if (!await EnsureConnectionAsync())
                {
                    return; // 연결 실패 시 조용히 스킵
                }
                
                // 메시지 준비 (캐싱된 메서드 사용)
                var status = GetExternalStatusCached();
                var message = status + "\r\n";
                var data = Encoding.UTF8.GetBytes(message);
                
                // 전송
                await persistentStream!.WriteAsync(data, 0, data.Length, cancellationTokenSource.Token);
                await persistentStream.FlushAsync();
                
                // 통계 업데이트
                Interlocked.Increment(ref totalMessagesSent);
                Interlocked.Add(ref totalBytesSent, data.Length);
                lastSuccessfulSend = DateTime.Now;
                
                // 로그 (집계)
                LogAggregated("send_success", $"상태 전송: {status}", LogLevel.DEBUG);
                
                // 주기적으로 통계 출력 (1000번마다)
                if (totalMessagesSent % 1000 == 0)
                {
                    PrintStatistics();
                }
            }
            catch (Exception ex)
            {
                LogAggregated("send_error", $"전송 실패: {ex.Message}", LogLevel.ERROR);
                
                // 연결 리셋 (다음 전송 시 재연결)
                await connectionSemaphore.WaitAsync();
                try
                {
                    persistentStream?.Close();
                    persistentClient?.Close();
                    persistentStream = null;
                    persistentClient = null;
                }
                catch { }
                finally
                {
                    connectionSemaphore.Release();
                }
            }
        }
        
        // 🔥 캐싱된 외부 상태 가져오기
        private string GetExternalStatusCached()
        {
            lock (lockObject)
            {
                // IP 주소 캐싱
                cachedLocalIp ??= GetLocalIpAddress();
                
                var key = (cachedLocalIp, lastStatus);
                
                if (!statusMessageCache.TryGetValue(key, out var cached))
                {
                    string status = lastStatus switch
                    {
                        SyncStatus.Master => "state2",
                        SyncStatus.Slave => "state1",
                        SyncStatus.Error => "state0",
                        SyncStatus.Unknown => "state0",
                        _ => "state0"
                    };
                    
                    cached = $"{cachedLocalIp}_{status}";
                    statusMessageCache[key] = cached;
                    
                    // 캐시 크기 제한
                    if (statusMessageCache.Count > 20)
                    {
                        statusMessageCache.Clear();
                        statusMessageCache[key] = cached;
                    }
                }
                
                return cached;
            }
        }
        
        // 🔥 집계된 로깅
        private void LogAggregated(string key, string message, LogLevel level)
        {
            lock (logAggregator)
            {
                if (!logAggregator.ContainsKey(key))
                {
                    logAggregator[key] = 0;
                }
                
                logAggregator[key]++;
                
                // 1분마다 또는 처음 발생 시 로그 출력
                var now = DateTime.Now;
                if ((now - lastLogFlush).TotalMinutes >= 1 || logAggregator[key] == 1)
                {
                    FlushAggregatedLogs();
                    lastLogFlush = now;
                }
            }
        }
        
        // 🔥 집계된 로그 출력
        private void FlushAggregatedLogs()
        {
            lock (logAggregator)
            {
                foreach (var kvp in logAggregator)
                {
                    if (kvp.Value > 1)
                    {
                        Logger.Debug($"[집계] {kvp.Key}: {kvp.Value}회 발생");
                    }
                }
                
                logAggregator.Clear();
            }
        }
        
        // 🔥 통계 출력
        private void PrintStatistics()
        {
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var messagesPerSecond = totalMessagesSent / uptime.TotalSeconds;
            var bytesPerSecond = totalBytesSent / uptime.TotalSeconds;
            var connectionEfficiency = connectionCount > 0 ? (1.0 - (double)reconnectCount / connectionCount) * 100 : 100;
            
            Logger.Info($"[통계] 실행시간: {uptime:hh\\:mm\\:ss}, " +
                       $"전송: {totalMessagesSent:N0}개 ({messagesPerSecond:F1}/초), " +
                       $"처리량: {bytesPerSecond:F0}B/s, " +
                       $"연결효율: {connectionEfficiency:F1}%");
        }
        
        // TCP 클라이언트 시작 (수정됨)
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
                
                // 캐시 초기화
                cachedLocalIp = null;
                statusMessageCache.Clear();

                Logger.Info($"TCP 클라이언트 시작됨 - {ip}:{port} (최적화 모드)");
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 시작 실패: {ex.Message}");
            }
        }

        // TCP 클라이언트 중지 (수정됨)
        public void StopTcpClient()
        {
            if (!isClientEnabled)
                return;

            try
            {
                isClientEnabled = false;
                cancellationTokenSource.Cancel();
                
                // 연결 정리
                connectionSemaphore.Wait();
                try
                {
                    persistentStream?.Close();
                    persistentClient?.Close();
                }
                catch { }
                finally
                {
                    persistentStream = null;
                    persistentClient = null;
                    connectionSemaphore.Release();
                }
                
                // 최종 통계 출력
                PrintStatistics();
                FlushAggregatedLogs();
                
                Logger.Info("TCP 클라이언트 중지됨");
            }
            catch (Exception ex)
            {
                Logger.Error($"TCP 클라이언트 중지 실패: {ex.Message}");
            }
        }
        
        // 🔥 성능 통계 가져오기
        public (long messages, long bytes, double messagesPerSec, double connectionEfficiency) GetPerformanceStats()
        {
            var uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var messagesPerSec = uptime.TotalSeconds > 0 ? totalMessagesSent / uptime.TotalSeconds : 0;
            var efficiency = connectionCount > 0 ? (1.0 - (double)reconnectCount / connectionCount) : 1.0;
            
            return (totalMessagesSent, totalBytesSent, messagesPerSec, efficiency);
        }
        
        public void Dispose()
        {
            StopTcpClient();
            cancellationTokenSource?.Dispose();
            connectionSemaphore?.Dispose();
            syncTopology?.Dispose();
        }
    }

    // 🔥 최적화된 Logger 클래스
    public class Logger
    {
        private static readonly object lockObject = new object();
        // %LOCALAPPDATA%\SyncGuard\logs 폴더 사용
        private static readonly string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "SyncGuard", 
            "logs");
        private static readonly int maxFileSizeMB = 10;
        private static LogLevel currentLogLevel = GetConfiguredLogLevel();
        
        // 🔥 로그 레벨 설정 읽기
        private static LogLevel GetConfiguredLogLevel()
        {
            try
            {
                // 환경 변수에서 읽기
                var envLogLevel = Environment.GetEnvironmentVariable("SYNCGUARD_LOG_LEVEL");
                if (!string.IsNullOrEmpty(envLogLevel) && Enum.TryParse<LogLevel>(envLogLevel, out var level))
                {
                    return level;
                }
                
                // 디버그 빌드에서는 DEBUG, 릴리즈에서는 INFO
                #if DEBUG
                    return LogLevel.DEBUG;
                #else
                    return LogLevel.INFO;
                #endif
            }
            catch
            {
                return LogLevel.INFO;
            }
        }
        
        static Logger()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }
        
        private static string GetApplicationDirectory()
        {
            try
            {
                return AppContext.BaseDirectory;
            }
            catch
            {
                return ".";
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
                    
                    // 콘솔 출력 (디버그 모드에서만)
                    #if DEBUG
                    Console.WriteLine(logEntry);
                    #endif
                    
                    // 파일에 로그 저장
                    string logFile = Path.Combine(logDirectory, "syncguard_log.txt");
                    
                    // 파일 크기 확인 및 로테이션
                    if (File.Exists(logFile))
                    {
                        var fileInfo = new FileInfo(logFile);
                        if (fileInfo.Length > maxFileSizeMB * 1024 * 1024)
                        {
                            string backupFile = Path.Combine(logDirectory, $"syncguard_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                            File.Move(logFile, backupFile);
                            
                            // 오래된 백업 파일 삭제 (7일 이상)
                            CleanOldLogs();
                        }
                    }
                    
                    File.AppendAllText(logFile, logEntry + Environment.NewLine, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    Console.WriteLine($"[ERROR] 로그 쓰기 실패: {ex.Message}");
                    #endif
                }
            }
        }
        
        // 🔥 오래된 로그 파일 정리
        private static void CleanOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(logDirectory, "syncguard_log_*.txt");
                var cutoffDate = DateTime.Now.AddDays(-7);
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }
        
        public static void Debug(string message) => Log(LogLevel.DEBUG, message);
        public static void Info(string message) => Log(LogLevel.INFO, message);
        public static void Warning(string message) => Log(LogLevel.WARNING, message);
        public static void Error(string message) => Log(LogLevel.ERROR, message);
    }

    public class ConfigManager
    {
        // %APPDATA%\SyncGuard 폴더 사용
        private static readonly string configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "SyncGuard");
        private static readonly string configFile = Path.Combine(configDirectory, "syncguard_config.json");
        private static readonly object lockObject = new object();
        
        // 더 이상 사용하지 않음 - 필요시 제거
        private static string GetApplicationDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        
        static ConfigManager()
        {
            // 설정 디렉토리 생성
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
        }
        
        public static void SaveConfig(string serverIP, int serverPort, int transmissionInterval = 1000, bool enableExternalSend = false)
        {
            lock (lockObject)
            {
                try
                {
                    Logger.Info($"설정 저장 시작 - configDirectory: {configDirectory}");
                    Logger.Info($"설정 저장 시작 - configFile: {configFile}");
                    
                    // 설정 디렉토리가 없으면 생성
                    if (!Directory.Exists(configDirectory))
                    {
                        Directory.CreateDirectory(configDirectory);
                        Logger.Info($"설정 디렉토리 생성: {configDirectory}");
                    }
                    
                    // 기존 설정 파일 백업
                    if (File.Exists(configFile))
                    {
                        string backupFile = configFile + ".bak";
                        File.Copy(configFile, backupFile, true);
                        Logger.Info($"기존 설정 파일 백업: {backupFile}");
                    }
                    
                    var config = new
                    {
                        ServerIP = serverIP,
                        ServerPort = serverPort,
                        TransmissionInterval = transmissionInterval,
                        EnableExternalSend = enableExternalSend,
                        LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    Logger.Info($"JSON 설정 내용: {json}");
                    
                    File.WriteAllText(configFile, json, System.Text.Encoding.UTF8);
                    Logger.Info($"설정 저장 완료: {serverIP}:{serverPort}, Interval={transmissionInterval}ms, ExternalSend={enableExternalSend} -> {configFile}");
                    
                    // 파일이 실제로 생성되었는지 확인
                    if (File.Exists(configFile))
                    {
                        var fileInfo = new FileInfo(configFile);
                        Logger.Info($"설정 파일 생성 확인: {configFile}, 크기: {fileInfo.Length} bytes, 수정시간: {fileInfo.LastWriteTime}");
                    }
                    else
                    {
                        Logger.Error($"설정 파일이 생성되지 않았습니다: {configFile}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"설정 저장 실패: {ex.Message}");
                    Logger.Error($"스택 트레이스: {ex.StackTrace}");
                    throw; // 상위로 예외 전파
                }
            }
        }
        
        public static (string serverIP, int serverPort, int transmissionInterval, bool enableExternalSend) LoadConfig()
        {
            lock (lockObject)
            {
                try
                {
                    Logger.Info($"설정 로드 시작 - 설정 파일 경로: {configFile}");
                    
                    // 🔥 레거시 설정 파일 마이그레이션 체크
                    if (TryMigrateLegacyConfig())
                    {
                        Logger.Info("레거시 설정 파일을 JSON 형식으로 마이그레이션했습니다.");
                    }
                    
                    if (File.Exists(configFile))
                    {
                        Logger.Info($"설정 파일 발견: {configFile}");
                        string json = File.ReadAllText(configFile, System.Text.Encoding.UTF8);
                        Logger.Info($"설정 파일 내용: {json}");
                        
                        // JSON을 JsonDocument로 파싱
                        using var document = JsonDocument.Parse(json);
                        var root = document.RootElement;
                        
                        if (root.ValueKind == JsonValueKind.Null || root.ValueKind == JsonValueKind.Undefined)
                        {
                            Logger.Warning("설정 파일이 비어있거나 잘못된 형식입니다. 기본값을 사용합니다.");
                            return ("127.0.0.1", 8080, 1000, false);
                        }
                        
                        string serverIP = "127.0.0.1";
                        int serverPort = 8080;
                        
                        try
                        {
                            if (root.TryGetProperty("ServerIP", out var serverIPElement))
                            {
                                serverIP = serverIPElement.GetString() ?? "127.0.0.1";
                            }
                            
                            if (root.TryGetProperty("ServerPort", out var serverPortElement))
                            {
                                serverPort = serverPortElement.GetInt32();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"ServerIP/ServerPort 파싱 실패: {ex.Message}, 기본값 사용");
                        }
                        
                        // JSON 파싱 후 유효성 검사
                        if (string.IsNullOrWhiteSpace(serverIP) || !IPAddress.TryParse(serverIP, out _))
                        {
                            Logger.Warning($"잘못된 IP 주소: {serverIP}, 기본값 사용");
                            serverIP = "127.0.0.1";
                        }

                        if (serverPort < 1 || serverPort > 65535)
                        {
                            Logger.Warning($"잘못된 포트 번호: {serverPort}, 기본값 사용");
                            serverPort = 8080;
                        }
                        
                        // TransmissionInterval이 있으면 사용, 없으면 기본값 1000ms
                        int transmissionInterval = 1000;
                        try
                        {
                            if (root.TryGetProperty("TransmissionInterval", out System.Text.Json.JsonElement intervalProperty))
                            {
                                transmissionInterval = intervalProperty.GetInt32();
                                Logger.Info($"TransmissionInterval 로드: {transmissionInterval}ms");
                            }
                            else
                            {
                                Logger.Warning("TransmissionInterval 필드가 없어 기본값 1000ms를 사용합니다.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"TransmissionInterval 읽기 실패: {ex.Message}, 기본값 1000ms 사용");
                            transmissionInterval = 1000;
                        }
                        
                        if (transmissionInterval < 100 || transmissionInterval > 3600000) // 0.1초 ~ 1시간
                        {
                            Logger.Warning($"잘못된 전송 간격: {transmissionInterval}ms, 기본값 사용");
                            transmissionInterval = 1000;
                        }
                        
                        // EnableExternalSend가 있으면 사용, 없으면 기본값 false
                        bool enableExternalSend = false;
                        try
                        {
                            if (root.TryGetProperty("EnableExternalSend", out System.Text.Json.JsonElement enableProperty))
                            {
                                enableExternalSend = enableProperty.GetBoolean();
                                Logger.Info($"EnableExternalSend 로드: {enableExternalSend}");
                            }
                            else
                            {
                                Logger.Warning("EnableExternalSend 필드가 없어 기본값 false를 사용합니다.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"EnableExternalSend 읽기 실패: {ex.Message}, 기본값 false 사용");
                            enableExternalSend = false;
                        }
                        
                        Logger.Info($"설정 로드 완료: {serverIP}:{serverPort}, Interval={transmissionInterval}ms, ExternalSend={enableExternalSend}");
                        return (serverIP, serverPort, transmissionInterval, enableExternalSend);
                    }
                    else
                    {
                        Logger.Warning($"설정 파일이 존재하지 않습니다: {configFile}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"설정 로드 실패: {ex.Message}");
                }
                
                // 기본값 반환
                Logger.Info("기본 설정 사용: 127.0.0.1:8080, Interval=1000ms, ExternalSend=false");
                return ("127.0.0.1", 8080, 1000, false);
            }
        }
        
        // 🔥 레거시 설정 파일을 JSON 형식으로 마이그레이션
        private static bool TryMigrateLegacyConfig()
        {
            try
            {
                // 여러 위치에서 레거시 설정 파일 확인
                string[] possibleLegacyPaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "syncguard_config.txt"),
                    Path.Combine(AppContext.BaseDirectory, "config", "syncguard_config.txt"),
                    Path.Combine(Environment.CurrentDirectory, "syncguard_config.txt"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SyncGuard", "syncguard_config.txt"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SyncGuard", "syncguard_config.txt")
                };
                
                foreach (string legacyPath in possibleLegacyPaths)
                {
                    if (File.Exists(legacyPath))
                    {
                        Logger.Info($"레거시 설정 파일 발견: {legacyPath}");
                        
                        // 레거시 파일 읽기
                        string[] lines = File.ReadAllLines(legacyPath, System.Text.Encoding.UTF8);
                        
                        string serverIP = "127.0.0.1";
                        int serverPort = 8080;
                        int transmissionInterval = 1000;
                        bool enableExternalSend = false;
                        
                        // 레거시 형식 파싱 (IP:Port 형식)
                        foreach (string line in lines)
                        {
                            string trimmedLine = line.Trim();
                            if (!string.IsNullOrEmpty(trimmedLine) && trimmedLine.Contains(":"))
                            {
                                string[] parts = trimmedLine.Split(':');
                                if (parts.Length >= 2)
                                {
                                    if (IPAddress.TryParse(parts[0], out _))
                                    {
                                        serverIP = parts[0];
                                        if (int.TryParse(parts[1], out int port))
                                        {
                                            serverPort = port;
                                        }
                                    }
                                }
                            }
                        }
                        
                        // JSON 형식으로 저장
                        SaveConfig(serverIP, serverPort, transmissionInterval, enableExternalSend);
                        
                        // 레거시 파일 백업 후 삭제
                        string backupFile = Path.Combine(GetApplicationDirectory(), $"syncguard_config_backup_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        File.Move(legacyPath, backupFile);
                        
                        Logger.Info($"레거시 설정 마이그레이션 완료: {serverIP}:{serverPort} -> {backupFile}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"레거시 설정 마이그레이션 실패: {ex.Message}");
            }
            
            return false;
        }
    }
}
