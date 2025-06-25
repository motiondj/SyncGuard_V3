using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Management;

namespace SyncGuard.Core
{
    public class SyncChecker : IDisposable
    {
        public enum SyncStatus
        {
            Unknown,
            Locked,
            Unlocked,
            Error
        }
        
        private SyncStatus lastStatus = SyncStatus.Unknown;
        private readonly object lockObject = new object();
        private bool useWmiMethod = true; // WMI 방법 우선 사용
        
        public SyncChecker()
        {
            // WMI 방법이 사용 가능한지 확인
            if (!IsWmiMethodAvailable())
            {
                useWmiMethod = false;
                Console.WriteLine("WMI 방법을 사용할 수 없습니다. nvidia-smi 방법으로 대체합니다.");
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
                    }
                    
                    return lastStatus;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error;
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
                        Console.WriteLine("SyncTopology WMI 클래스에서 Sync 디바이스를 찾을 수 없습니다.");
                        return SyncStatus.Unlocked;
                    }
                    
                    bool hasValidSyncDevice = false;
                    bool isActuallySynced = false;
                    
                    foreach (ManagementObject obj in collection)
                    {
                        try
                        {
                            // Sync 디바이스 정보 추출
                            int displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
                            string uname = obj["uname"]?.ToString() ?? "";
                            bool isDisplayMasterable = Convert.ToBoolean(obj["isDisplayMasterable"]);
                            
                            Console.WriteLine($"Sync 디바이스: ID={obj["id"]}, State={displaySyncState}, Name={uname}, Masterable={isDisplayMasterable}");
                            
                            // 유효한 Sync 디바이스인지 확인
                            if (!string.IsNullOrEmpty(uname) && uname != "invalid")
                            {
                                hasValidSyncDevice = true;
                                
                                // isGPUSynced() 메서드로 실제 Sync 상태 확인
                                try
                                {
                                    var result = obj.InvokeMethod("isGPUSynced", null);
                                    bool isSynced = Convert.ToBoolean(((ManagementBaseObject)result)["ReturnValue"]);
                                    Console.WriteLine($"isGPUSynced() 결과: {isSynced}");
                                    
                                    if (isSynced)
                                    {
                                        isActuallySynced = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"isGPUSynced() 호출 중 오류: {ex.Message}");
                                    // isGPUSynced() 호출 실패 시 동기화 안됨으로 처리
                                    // displaySyncState는 Sync 디바이스 인식 여부만 나타내므로 동기화 상태 판단에 사용하지 않음
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Sync 디바이스 정보 추출 중 오류: {ex.Message}");
                        }
                    }
                    
                    // Sync 상태 판단
                    if (!hasValidSyncDevice)
                    {
                        return SyncStatus.Unlocked; // 유효한 Sync 디바이스 없음
                    }
                    else if (isActuallySynced)
                    {
                        return SyncStatus.Locked; // 실제로 Sync 활성화됨
                    }
                    else
                    {
                        return SyncStatus.Unlocked; // Sync 디바이스는 있지만 동기화 안됨
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WMI Sync 상태 확인 중 오류: {ex.Message}");
                return SyncStatus.Error;
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
                Console.WriteLine($"nvidia-smi Sync 상태 확인 중 오류: {ex.Message}");
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
        
        public void Dispose()
        {
            // nvidia-smi는 별도 정리 작업이 필요 없음
        }
    }
}
