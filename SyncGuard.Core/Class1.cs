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
            Synced,         // 동기화됨 (설정값과 타이밍 서버 일치)
            Free,           // 동기화 안됨 (displaySyncState: 0)
            ConfigConflict  // 설정 충돌 (설정값과 타이밍 서버 불일치)
        }

        // 사용자 설정 역할
        public enum SyncRole
        {
            Master,
            Slave
        }

        // 디스플레이 동기화 진단 결과
        public class DisplaySyncDiagnosis
        {
            public bool IsSynced { get; set; } = false;
            public string TimingServer { get; set; } = "";
            public string SelectedDisplay { get; set; } = "";
            public string DiagnosisMessage { get; set; } = "";
            public string RawData { get; set; } = "";
            public SyncRole UserRole { get; set; } = SyncRole.Slave;
            public bool IsConfigConflict { get; set; } = false;
        }
        
        private SyncStatus lastStatus = SyncStatus.Unknown;
        private readonly object lockObject = new object();
        private ManagementEventWatcher? eventWatcher;
        private NvApiWrapper? nvApiWrapper;
        private bool isDisposed = false;
        private SyncRole userRole = SyncRole.Slave; // 기본값: Slave
        
        // 이벤트: Sync 상태 변경 시 발생
        public event EventHandler<SyncStatus>? SyncStatusChanged;
        
        public SyncChecker()
        {
            WriteLogAndConsole("=== SyncChecker 초기화 시작 (Master/Slave 설정 기능 추가) ===");
            
            // 프로그램 시작 시 설정을 Slave로 리셋
            ResetUserRoleOnStartup();
            
            // 사용자 설정 로드
            LoadUserRole();
            
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

        // 프로그램 시작 시 설정 리셋
        private void ResetUserRoleOnStartup()
        {
            try
            {
                string configPath = "syncguard_config.txt";
                if (File.Exists(configPath))
                {
                    // 기존 설정 파일 삭제
                    File.Delete(configPath);
                    WriteLogAndConsole("프로그램 시작 시 기존 설정 파일 삭제됨");
                }
                
                // 기본값(Slave)으로 설정
                userRole = SyncRole.Slave;
                SaveUserRole();
                WriteLogAndConsole("프로그램 시작 시 설정을 Slave로 리셋");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"프로그램 시작 시 설정 리셋 실패: {ex.Message}");
            }
        }

        // 사용자 역할 설정
        public void SetUserRole(SyncRole role)
        {
            userRole = role;
            SaveUserRole();
            WriteLogAndConsole($"사용자 역할 설정: {role}");
            
            // 설정 변경 시 상태 재확인
            RefreshSyncStatus();
        }

        // 현재 사용자 역할 가져오기
        public SyncRole GetUserRole()
        {
            return userRole;
        }

        // 사용자 설정 저장
        private void SaveUserRole()
        {
            try
            {
                string configPath = "syncguard_config.txt";
                File.WriteAllText(configPath, userRole.ToString());
                WriteLogAndConsole($"사용자 설정 저장: {userRole}");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"사용자 설정 저장 실패: {ex.Message}");
            }
        }

        // 사용자 설정 로드
        private void LoadUserRole()
        {
            try
            {
                string configPath = "syncguard_config.txt";
                if (File.Exists(configPath))
                {
                    string roleText = File.ReadAllText(configPath).Trim();
                    if (Enum.TryParse<SyncRole>(roleText, out SyncRole role))
                    {
                        userRole = role;
                        WriteLogAndConsole($"사용자 설정 로드: {userRole}");
                    }
                    else
                    {
                        WriteLogAndConsole($"잘못된 사용자 설정: {roleText}, 기본값(Slave) 사용");
                    }
                }
                else
                {
                    WriteLogAndConsole("사용자 설정 파일 없음, 기본값(Slave) 사용");
                }
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"사용자 설정 로드 실패: {ex.Message}, 기본값(Slave) 사용");
            }
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
                    
                    // 모든 인스턴스 분석
                    int instanceIndex = 0;
                    foreach (ManagementObject obj in collection)
                    {
                        instanceIndex++;
                        WriteLogAndConsole($"=== SyncTopology 인스턴스 {instanceIndex} 분석 ===");
                        
                        // 모든 속성 정보 수집 (디버깅용)
                        var allProperties = new System.Text.StringBuilder();
                        foreach (PropertyData prop in obj.Properties)
                        {
                            allProperties.AppendLine($"{prop.Name}: {prop.Value}");
                        }
                        diagnosis.RawData = allProperties.ToString();
                        
                        WriteLogAndConsole("=== SyncTopology 속성 분석 ===");
                        WriteLogAndConsole(diagnosis.RawData);
                        
                        // WMI에서 사용 가능한 모든 속성 확인
                        WriteLogAndConsole("=== 사용 가능한 모든 속성 ===");
                        foreach (var prop in obj.Properties)
                        {
                            WriteLogAndConsole($"속성: {prop.Name} = {prop.Value}");
                        }
                        WriteLogAndConsole("=== 속성 확인 완료 ===");
                        
                        // 추가 디버깅: name, uname, ver 속성 상세 분석
                        WriteLogAndConsole("=== 상세 속성 분석 ===");
                        try
                        {
                            var nameValue = obj["name"];
                            WriteLogAndConsole($"name 속성 타입: {nameValue?.GetType()}, 값: '{nameValue}'");
                            
                            var unameValue = obj["uname"];
                            WriteLogAndConsole($"uname 속성 타입: {unameValue?.GetType()}, 값: '{unameValue}'");
                            
                            var verValue = obj["ver"];
                            WriteLogAndConsole($"ver 속성 타입: {verValue?.GetType()}, 값: '{verValue}'");
                            
                            // ver가 ManagementBaseObject인 경우 내부 속성 확인
                            if (verValue is ManagementBaseObject verObj)
                            {
                                WriteLogAndConsole("=== ver 객체 내부 속성 ===");
                                foreach (var verProp in verObj.Properties)
                                {
                                    WriteLogAndConsole($"  ver.{verProp.Name} = {verProp.Value}");
                                }
                                
                                // orderedValue 분석
                                try
                                {
                                    var orderedValue = verObj["orderedValue"];
                                    WriteLogAndConsole($"ver.orderedValue 분석: {orderedValue} (타입: {orderedValue?.GetType()})");
                                    
                                    // orderedValue로 Master/Slave 구분 가능성 확인
                                    if (orderedValue != null)
                                    {
                                        long orderVal = Convert.ToInt64(orderedValue);
                                        WriteLogAndConsole($"orderedValue 숫자값: {orderVal}");
                                        
                                        // orderedValue가 Master/Slave 구분에 사용될 수 있는지 확인
                                        // 일반적으로 낮은 값이 Master일 가능성
                                        if (orderVal == 16777216)
                                        {
                                            WriteLogAndConsole("orderedValue 16777216 → 이 시스템이 Master일 가능성");
                                        }
                                        else
                                        {
                                            WriteLogAndConsole($"orderedValue {orderVal} → 이 시스템이 Slave일 가능성");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WriteLogAndConsole($"orderedValue 분석 중 오류: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLogAndConsole($"상세 속성 분석 중 오류: {ex.Message}");
                        }
                        WriteLogAndConsole("=== 상세 속성 분석 완료 ===");
                        
                        // 1. displaySyncState를 우선 확인
                        int displaySyncState = 0;
                        try
                        {
                            displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                        }
                        catch { }
                        
                        WriteLogAndConsole($"displaySyncState: {displaySyncState}");
                        WriteLogAndConsole($"사용자 설정 역할: {userRole}");
                        
                        // 2. isDisplayMasterable로 실제 Master/Slave 판단 (임시로 유지)
                        bool isActuallyMaster = false;
                        try
                        {
                            isActuallyMaster = Convert.ToBoolean(obj["isDisplayMasterable"]);
                            WriteLogAndConsole($"isDisplayMasterable: {isActuallyMaster} → 이 시스템이 {(isActuallyMaster ? "Master" : "Slave")}입니다");
                        }
                        catch (Exception ex)
                        {
                            WriteLogAndConsole($"isDisplayMasterable 속성 읽기 실패: {ex.Message}");
                            isActuallyMaster = false;
                        }
                        
                        // 3. ver.orderedValue로 Master/Slave 판단 시도
                        bool isMasterByOrderedValue = false;
                        try
                        {
                            if (obj["ver"] is ManagementBaseObject verObj)
                            {
                                var orderedValue = verObj["orderedValue"];
                                if (orderedValue != null)
                                {
                                    long orderVal = Convert.ToInt64(orderedValue);
                                    // 16777216이 Master를 나타낸다고 가정
                                    isMasterByOrderedValue = (orderVal == 16777216);
                                    WriteLogAndConsole($"ver.orderedValue 기반 판단: {orderVal} → {(isMasterByOrderedValue ? "Master" : "Slave")}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteLogAndConsole($"ver.orderedValue 분석 실패: {ex.Message}");
                        }
                        
                        // 4. 최종 Master/Slave 판단 (orderedValue 우선, 없으면 isDisplayMasterable 사용)
                        bool finalIsMaster = isMasterByOrderedValue;
                        if (!isMasterByOrderedValue) // orderedValue 분석이 실패한 경우
                        {
                            finalIsMaster = isActuallyMaster;
                            WriteLogAndConsole("orderedValue 분석 실패로 isDisplayMasterable 사용");
                        }
                        
                        WriteLogAndConsole($"최종 Master/Slave 판단: {(finalIsMaster ? "Master" : "Slave")}");
                        
                        // 5. Master 케이스 (finalIsMaster: True)
                        if (finalIsMaster)
                        {
                            WriteLogAndConsole("Master 케이스: 디스플레이 선택 여부 확인 필요");
                            
                            // 타이밍 서버 정보 설정
                            diagnosis.TimingServer = "ThisSystem";
                            WriteLogAndConsole($"타이밍 서버: {diagnosis.TimingServer}");
                            
                            // Master일 때도 displaySyncState 확인
                            if (displaySyncState == 0)
                            {
                                // 디스플레이 선택 안됨 - Master는 동기화 안됨으로 처리
                                diagnosis.IsSynced = false;
                                diagnosis.DiagnosisMessage = "Master이지만 동기화 꺼짐: 동기화되지 않음";
                                
                                diagnosis.UserRole = userRole;
                                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                                return diagnosis;
                            }
                            else if (displaySyncState == 1)
                            {
                                // 디스플레이 선택됨 - 사용자 설정과 비교
                                if (userRole == SyncRole.Master)
                                {
                                    // 설정: Master, 실제: Master → 일치
                                    diagnosis.IsSynced = true;
                                    diagnosis.DiagnosisMessage = "설정: Master, 실제: Master → 동기화됨";
                                }
                                else
                                {
                                    // 설정: Slave, 실제: Master → 불일치
                                    diagnosis.IsSynced = false;
                                    diagnosis.IsConfigConflict = true;
                                    diagnosis.DiagnosisMessage = "설정: Slave, 실제: Master → 설정 충돌";
                                }
                                
                                diagnosis.UserRole = userRole;
                                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                                return diagnosis;
                            }
                        }
                        
                        // 6. Slave 케이스 (finalIsMaster: False)
                        else
                        {
                            WriteLogAndConsole("Slave 케이스: 디스플레이 선택 여부 확인 필요");
                            
                            // 타이밍 서버 정보 설정
                            diagnosis.TimingServer = "OtherSystem";
                            WriteLogAndConsole($"타이밍 서버: {diagnosis.TimingServer}");
                            
                            // Slave일 때는 displaySyncState 확인
                            if (displaySyncState == 0)
                            {
                                // 디스플레이 선택 안됨 - Slave도 동기화 안됨으로 처리
                                diagnosis.IsSynced = false;
                                diagnosis.DiagnosisMessage = "Slave이지만 동기화 꺼짐: 동기화되지 않음";
                                
                                diagnosis.UserRole = userRole;
                                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                                return diagnosis;
                            }
                            else if (displaySyncState == 1)
                            {
                                // 디스플레이 선택됨 - 사용자 설정과 비교
                                if (userRole == SyncRole.Slave)
                                {
                                    // 설정: Slave, 실제: Slave → 일치
                                    diagnosis.IsSynced = true;
                                    diagnosis.DiagnosisMessage = "설정: Slave, 실제: Slave → 동기화됨";
                                }
                                else
                                {
                                    // 설정: Master, 실제: Slave → 불일치
                                    diagnosis.IsSynced = false;
                                    diagnosis.IsConfigConflict = true;
                                    diagnosis.DiagnosisMessage = "설정: Master, 실제: Slave → 설정 충돌";
                                }
                                
                                diagnosis.UserRole = userRole;
                                WriteLogAndConsole($"진단 결과: {diagnosis.DiagnosisMessage}");
                                WriteLogAndConsole("=== 디스플레이 동기화 진단 완료 ===");
                                return diagnosis;
                            }
                        }
                        
                        WriteLogAndConsole($"인스턴스 {instanceIndex} 분석 완료");
                        WriteLogAndConsole("---");
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
                
                SyncStatus currentStatus;
                
                if (diagnosis.IsConfigConflict)
                {
                    currentStatus = SyncStatus.ConfigConflict;
                }
                else if (diagnosis.IsSynced)
                {
                    currentStatus = SyncStatus.Synced;
                }
                else
                {
                    currentStatus = SyncStatus.Free;
                }
                
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

        // 다른 NVIDIA WMI 클래스 탐색
        public void ExploreNvidiaWmiClasses()
        {
            try
            {
                WriteLogAndConsole("=== NVIDIA WMI 클래스 탐색 시작 ===");
                
                string nvidiaNamespace = "root\\CIMV2\\NV";
                var scope = new ManagementScope(nvidiaNamespace);
                scope.Connect();
                
                // 모든 클래스 조회
                var query = new SelectQuery("SELECT * FROM meta_class");
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    var collection = searcher.Get();
                    WriteLogAndConsole($"발견된 클래스 수: {collection.Count}");
                    
                    foreach (ManagementClass cls in collection)
                    {
                        WriteLogAndConsole($"클래스: {cls.ClassPath.ClassName}");
                        
                        // 각 클래스의 속성 확인
                        foreach (PropertyData prop in cls.Properties)
                        {
                            WriteLogAndConsole($"  속성: {prop.Name} (타입: {prop.Type})");
                        }
                        WriteLogAndConsole("---");
                    }
                }
                
                WriteLogAndConsole("=== NVIDIA WMI 클래스 탐색 완료 ===");
            }
            catch (Exception ex)
            {
                WriteLogAndConsole($"NVIDIA WMI 클래스 탐색 실패: {ex.Message}");
            }
        }
    }
}
