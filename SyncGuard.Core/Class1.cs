using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Management;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace SyncGuard.Core
{
    public class SyncChecker : IDisposable
    {
        public enum SyncStatus
        {
            Unknown,
            Synced,    // Locked - 타이밍 서버 지정됨
            Free       // Unlocked - 타이밍 서버 미지정
        }

        // 디스플레이 동기화 진단 결과
        public class DisplaySyncDiagnosis
        {
            public bool IsSynced { get; set; } = false;
            public string TimingServer { get; set; } = "";
            public string SelectedDisplay { get; set; } = "";
            public string DiagnosisMessage { get; set; } = "";
            public string RawData { get; set; } = "";
        }
        
        private SyncStatus lastStatus = SyncStatus.Unknown;
        private readonly object lockObject = new object();
        private ManagementEventWatcher? eventWatcher;
        private NvApiWrapper? nvApiWrapper;
        private bool isDisposed = false;
        
        // 이벤트: Sync 상태 변경 시 발생
        public event EventHandler<SyncStatus>? SyncStatusChanged;
        
        public SyncChecker()
        {
            WriteLogAndConsole("=== SyncChecker 초기화 시작 (새로운 진단 로직) ===");
            
            // Quadro Sync II NVAPI 래퍼 초기화
            try
            {
                nvApiWrapper = new NvApiWrapper();
                WriteLogAndConsole("Quadro Sync II NVAPI 래퍼 초기화 완료");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"Quadro Sync II NVAPI 래퍼 초기화 실패: {ex.Message}");
                nvApiWrapper = null;
            }
            
            // WMI 이벤트 모니터링 시작
            StartWmiEventMonitoring();
            
            WriteLogAndConsole("=== SyncChecker 초기화 완료 ===");
        }

        // 새로운 진단 로직: 타이밍 서버와 디스플레이 선택 상태 확인
        public DisplaySyncDiagnosis DiagnoseDisplaySync()
        {
            var diagnosis = new DisplaySyncDiagnosis();
            
            try
            {
                WriteLogAndConsole("=== 디스플레이 동기화 진단 시작 ===");
                
                string nvidiaNamespace = "root\\CIMV2\\NV";
                var scope = new ManagementScope(nvidiaNamespace);
                scope.Connect();
                var query = new SelectQuery("SELECT * FROM SyncTopology");
                
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    var collection = searcher.Get();
                    
                    if (collection.Count == 0)
                    {
                        diagnosis.DiagnosisMessage = "SyncTopology에서 정보를 찾을 수 없습니다.";
                        diagnosis.RawData = "SyncTopology 인스턴스 없음";
                        WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                        return diagnosis;
                    }

                    WriteLogAndConsole($"SyncTopology 인스턴스 {collection.Count}개 발견");
                    
                    foreach (ManagementObject obj in collection)
                    {
                        // 모든 속성 정보 수집 (디버깅용)
                        var allProperties = new System.Text.StringBuilder();
                        foreach (PropertyData prop in obj.Properties)
                        {
                            allProperties.AppendLine($"{prop.Name}: {prop.Value}");
                        }
                        diagnosis.RawData = allProperties.ToString();
                        
                        WriteLogAndConsole("=== SyncTopology 속성 분석 ===");
                        WriteLogAndConsole(diagnosis.RawData);
                        
                        // 1. displaySyncState를 우선 확인 (가장 신뢰할 수 있는 지표)
                        int displaySyncState = 0;
                        try
                        {
                            displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                        }
                        catch { }
                        
                        WriteLogAndConsole($"displaySyncState: {displaySyncState}");
                        
                        // displaySyncState가 1이면 동기화됨, 0이면 동기화 안됨
                        if (displaySyncState == 1)
                        {
                            diagnosis.IsSynced = true;
                            diagnosis.DiagnosisMessage = "displaySyncState=1: 동기화됨";
                            WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                            WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                            return diagnosis;
                        }
                        else if (displaySyncState == 0)
                        {
                            diagnosis.DiagnosisMessage = "displaySyncState=0: 동기화되지 않음";
                            WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                            return diagnosis;
                        }
                        
                        // 2. displaySyncState가 예상값이 아닌 경우, 기존 로직으로 fallback
                        // 타이밍 서버 확인
                        string? timingServer = obj["timingServer"]?.ToString();
                        diagnosis.TimingServer = timingServer ?? "미지정";
                        
                        WriteLogAndConsole($"타이밍 서버: {diagnosis.TimingServer}");
                        
                        if (string.IsNullOrEmpty(timingServer))
                        {
                            diagnosis.DiagnosisMessage = "타이밍 서버가 지정되지 않았습니다.";
                            WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                            return diagnosis;
                        }
                        
                        // 2. 디스플레이 선택 상태 확인
                        var displayList = obj["displayList"];
                        string selectedDisplayInfo = "";
                        
                        if (displayList != null)
                        {
                            var displays = displayList as System.Collections.IEnumerable;
                            if (displays != null)
                            {
                                bool hasSelectedDisplay = false;
                                var displayDetails = new System.Text.StringBuilder();
                                
                                foreach (var disp in displays)
                                {
                                    var dispObj = disp as ManagementBaseObject;
                                    if (dispObj == null) continue;
                                    
                                    string name = dispObj["name"]?.ToString() ?? "unknown";
                                    bool isActive = false;
                                    bool isSelected = false;
                                    
                                    try
                                    {
                                        isActive = Convert.ToBoolean(dispObj["active"]);
                                        isSelected = Convert.ToBoolean(dispObj["selected"]);
                                    }
                                    catch { }
                                    
                                    string freq = dispObj["refreshRate"]?.ToString() ?? "?";
                                    string res = dispObj["resolution"]?.ToString() ?? "?";
                                    
                                    displayDetails.AppendLine($"  - {name}: 활성={isActive}, 선택={isSelected}, 주사율={freq}, 해상도={res}");
                                    
                                    if (isSelected)
                                    {
                                        hasSelectedDisplay = true;
                                        selectedDisplayInfo = name;
                                    }
                                }
                                
                                diagnosis.SelectedDisplay = selectedDisplayInfo;
                                WriteLogAndConsole("=== 디스플레이 정보 ===");
                                WriteLogAndConsole(displayDetails.ToString());
                                
                                if (!hasSelectedDisplay)
                                {
                                    diagnosis.DiagnosisMessage = "타이밍 서버는 지정되었지만, 선택된 디스플레이가 없습니다.";
                                    WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                    return diagnosis;
                                }
                            }
                        }
                        else
                        {
                            // displayList가 없는 경우, 다른 속성에서 디스플레이 정보 찾기
                            var selectedDisplay = obj["selectedDisplay"]?.ToString();
                            diagnosis.SelectedDisplay = selectedDisplay ?? "미지정";
                            
                            WriteLogAndConsole($"선택된 디스플레이: {diagnosis.SelectedDisplay}");
                            
                            if (string.IsNullOrEmpty(selectedDisplay))
                            {
                                diagnosis.DiagnosisMessage = "타이밍 서버는 지정되었지만, 선택된 디스플레이 정보가 없습니다.";
                                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                return diagnosis;
                            }
                        }
                        
                        // 3. 모든 조건이 만족되면 Synced 상태
                        diagnosis.IsSynced = true;
                        diagnosis.DiagnosisMessage = $"동기화 설정 완료 - 타이밍 서버: {diagnosis.TimingServer}, 선택된 디스플레이: {diagnosis.SelectedDisplay}";
                        
                        WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                        WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                        
                        return diagnosis;
                    }
                }
                
                diagnosis.DiagnosisMessage = "SyncTopology 정보 파싱 실패";
                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                return diagnosis;
            }
            catch (Exception ex)
            {
                diagnosis.DiagnosisMessage = $"WMI 오류: {ex.Message}";
                WriteLogAndConsole($"진단 오류: {diagnosis.DiagnosisMessage}");
                return diagnosis;
            }
        }
        
        public SyncStatus GetSyncStatus()
        {
            try
            {
                // 새로운 진단 로직으로 상태 확인
                var diagnosis = DiagnoseDisplaySync();
                
                SyncStatus currentStatus = diagnosis.IsSynced ? SyncStatus.Synced : SyncStatus.Free;
                
                // NVAPI 보조 정보 (GPU/드라이버 정보)
                if (nvApiWrapper != null)
                {
                    var nvApiStatus = nvApiWrapper.GetSyncStatus();
                    WriteLogAndConsole($"NVAPI 보조 정보: {nvApiStatus}");
                }
                
                lock (lockObject)
                {
                    if (currentStatus != lastStatus)
                    {
                        lastStatus = currentStatus;
                        WriteLogAndConsole($"Sync 상태 변경: {currentStatus} ({diagnosis.DiagnosisMessage})");
                        SyncStatusChanged?.Invoke(this, currentStatus);
                    }
                    
                    return lastStatus;
                }
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Unknown;
            }
        }
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows WMI API 사용")]
        private void StartWmiEventMonitoring()
        {
            try
            {
                // 다른 이벤트 쿼리 방식 시도
                var query = new WqlEventQuery(
                    "SELECT * FROM __InstanceModificationEvent " +
                    "WHERE TargetInstance ISA 'SyncTopology' AND " +
                    "TargetInstance.Namespace = 'root\\CIMV2\\NV'"
                );
                
                var scope = new ManagementScope("root\\CIMV2\\NV");
                scope.Options.EnablePrivileges = true; // 권한 추가
                eventWatcher = new ManagementEventWatcher(scope, query);
                eventWatcher.EventArrived += OnSyncTopologyChanged;
                eventWatcher.Start();
                
                WriteLogAndConsole("WMI 이벤트 모니터링이 시작되었습니다.");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"WMI 이벤트 모니터링 시작 실패: {ex.Message}");
                // 이벤트 모니터링 실패 시 폴링 방식으로 대체
                StartPollingMode();
            }
        }
        
        // 폴링 방식으로 대체 (이벤트 모니터링 실패 시)
        private void StartPollingMode()
        {
            WriteLogAndConsole("폴링 방식으로 Sync 상태 모니터링을 시작합니다.");
            // 폴링은 GetSyncStatus() 호출 시 자동으로 처리됨
        }
        
        // 수동 리프레시 기능
        public void RefreshSyncStatus()
        {
            WriteLogAndConsole("=== 수동 리프레시 시작 ===");
            
            try
            {
                // WMI Scope 재연결
                var scope = new ManagementScope("root\\CIMV2\\NV");
                scope.Connect();
                
                // SyncTopology 다시 조회
                var diagnosis = DiagnoseDisplaySync();
                var newStatus = diagnosis.IsSynced ? SyncStatus.Synced : SyncStatus.Free;
                
                lock (lockObject)
                {
                    if (newStatus != lastStatus)
                    {
                        lastStatus = newStatus;
                        WriteLogAndConsole($"리프레시 후 Sync 상태 변경: {newStatus} ({diagnosis.DiagnosisMessage})");
                        SyncStatusChanged?.Invoke(this, newStatus);
                    }
                    else
                    {
                        WriteLogAndConsole($"리프레시 후 Sync 상태 유지: {newStatus} ({diagnosis.DiagnosisMessage})");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"수동 리프레시 중 오류: {ex.Message}");
            }
        }
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows WMI API 사용")]
        private void OnSyncTopologyChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                WriteLogAndConsole("=== SyncTopology 변경 감지됨 ===");
                
                // 변경된 인스턴스 정보 출력
                var targetInstance = e.NewEvent["TargetInstance"] as ManagementBaseObject;
                if (targetInstance != null)
                {
                    WriteLogAndConsole("변경된 SyncTopology 정보:");
                    foreach (PropertyData prop in targetInstance.Properties)
                    {
                        WriteLogAndConsole($"  {prop.Name}: {prop.Value}");
                    }
                }
                
                // 상태 재확인
                var newStatus = GetSyncStatus();
                WriteLogAndConsole($"변경 후 Sync 상태: {newStatus}");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"SyncTopology 변경 처리 중 오류: {ex.Message}");
            }
        }
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows WMI API 사용")]
        public void Dispose()
        {
            if (isDisposed) return;
            
            try
            {
                WriteLogAndConsole("=== SyncChecker 정리 시작 ===");
                
                if (eventWatcher != null)
                {
                    eventWatcher.Stop();
                    eventWatcher.Dispose();
                    eventWatcher = null;
                    WriteLogAndConsole("WMI 이벤트 모니터링 중지됨");
                }
                
                if (nvApiWrapper != null)
                {
                    nvApiWrapper.Dispose();
                    nvApiWrapper = null;
                    WriteLogAndConsole("NVAPI 래퍼 정리됨");
                }
                
                WriteLogAndConsole("=== SyncChecker 정리 완료 ===");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"SyncChecker 정리 중 오류: {ex.Message}");
            }
            finally
            {
                isDisposed = true;
            }
        }
        
        private void WriteLogAndConsole(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logMessage = $"[{timestamp}] {message}";
            
            Console.WriteLine(logMessage);
            
            try
            {
                File.AppendAllText("syncguard_log.txt", logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 파일 쓰기 실패: {ex.Message}");
            }
        }
    }
}
