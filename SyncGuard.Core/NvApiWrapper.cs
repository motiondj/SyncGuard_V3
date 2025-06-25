using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SyncGuard.Core
{
    /// <summary>
    /// Quadro Sync II를 위한 NVAPI P/Invoke 래퍼
    /// G-Sync가 아닌 Quadro Sync II 동기화 상태를 확인
    /// </summary>
    public class NvApiWrapper : IDisposable
    {
        #region NVAPI 상수 및 구조체

        // NVAPI 상태 코드
        private const int NVAPI_OK = 0;
        private const int NVAPI_ERROR = -1;

        // Quadro Sync 관련 상수
        private const int NV_DISPLAY_DRIVER_VERSION_VER = 0x00010000;
        private const int NV_GPU_PHYSICAL_FRAME_BUFFER_SIZE_VER = 0x00010000;

        // Display Sync 구조체
        [StructLayout(LayoutKind.Sequential)]
        private struct NV_DISPLAY_DRIVER_VERSION
        {
            public uint version;
            public uint driverVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] buildBranchString;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NV_GPU_PHYSICAL_FRAME_BUFFER_SIZE
        {
            public uint version;
            public uint frameBufferSize;
        }

        #endregion

        #region NVAPI 함수 포인터

        // Quadro Sync II 관련 NVAPI 함수 델리게이트
        private delegate int NvAPI_Initialize_t();
        private delegate int NvAPI_Unload_t();
        private delegate int NvAPI_GetErrorMessage_t(int status, [Out] StringBuilder message);
        private delegate int NvAPI_SYS_GetDriverAndBranchVersion_t(ref uint driverVersion, [Out] StringBuilder buildBranchString);
        private delegate int NvAPI_GPU_GetPhysicalFrameBufferSize_t(IntPtr gpuHandle, ref NV_GPU_PHYSICAL_FRAME_BUFFER_SIZE frameBufferSize);
        private delegate int NvAPI_EnumPhysicalGPUs_t([Out] IntPtr[] gpuHandles, ref uint gpuCount);
        private delegate int NvAPI_GPU_GetFullName_t(IntPtr gpuHandle, [Out] StringBuilder name);

        // 함수 포인터들
        private NvAPI_Initialize_t? _NvAPI_Initialize;
        private NvAPI_Unload_t? _NvAPI_Unload;
        private NvAPI_GetErrorMessage_t? _NvAPI_GetErrorMessage;
        private NvAPI_SYS_GetDriverAndBranchVersion_t? _NvAPI_SYS_GetDriverAndBranchVersion;
        private NvAPI_GPU_GetPhysicalFrameBufferSize_t? _NvAPI_GPU_GetPhysicalFrameBufferSize;
        private NvAPI_EnumPhysicalGPUs_t? _NvAPI_EnumPhysicalGPUs;
        private NvAPI_GPU_GetFullName_t? _NvAPI_GPU_GetFullName;

        #endregion

        #region 멤버 변수

        private IntPtr _hNvApi = IntPtr.Zero;
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private static readonly object _lockObject = new object();

        #endregion

        #region 생성자 및 소멸자

        public NvApiWrapper()
        {
            InitializeNvApi();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    UnloadNvApi();
                }
                _isDisposed = true;
            }
        }

        #endregion

        #region 초기화 및 해제

        private void InitializeNvApi()
        {
            lock (_lockObject)
            {
                try
                {
                    WriteLog("=== Quadro Sync II NVAPI 초기화 시작 ===");

                    // nvapi64.dll 로드
                    _hNvApi = LoadLibrary("nvapi64.dll");
                    if (_hNvApi == IntPtr.Zero)
                    {
                        WriteLog("nvapi64.dll 로드 실패");
                        return;
                    }

                    WriteLog("nvapi64.dll 로드 성공");

                    // 함수 포인터 바인딩
                    if (!BindFunctionPointers())
                    {
                        WriteLog("함수 포인터 바인딩 실패");
                        return;
                    }

                    // NVAPI 초기화
                    if (_NvAPI_Initialize != null)
                    {
                        int result = _NvAPI_Initialize();
                        if (result == NVAPI_OK)
                        {
                            _isInitialized = true;
                            WriteLog("Quadro Sync II NVAPI 초기화 성공");
                        }
                        else
                        {
                            string errorMsg = GetErrorMessage(result);
                            WriteLog($"Quadro Sync II NVAPI 초기화 실패: {errorMsg}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Quadro Sync II NVAPI 초기화 중 예외: {ex.Message}");
                }
            }
        }

        private void UnloadNvApi()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_isInitialized && _NvAPI_Unload != null)
                    {
                        int result = _NvAPI_Unload();
                        if (result != NVAPI_OK)
                        {
                            string errorMsg = GetErrorMessage(result);
                            WriteLog($"Quadro Sync II NVAPI 언로드 실패: {errorMsg}");
                        }
                        else
                        {
                            WriteLog("Quadro Sync II NVAPI 언로드 성공");
                        }
                        _isInitialized = false;
                    }

                    if (_hNvApi != IntPtr.Zero)
                    {
                        FreeLibrary(_hNvApi);
                        _hNvApi = IntPtr.Zero;
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Quadro Sync II NVAPI 언로드 중 예외: {ex.Message}");
                }
            }
        }

        private bool BindFunctionPointers()
        {
            try
            {
                _NvAPI_Initialize = GetFunctionPointer<NvAPI_Initialize_t>("NvAPI_Initialize");
                _NvAPI_Unload = GetFunctionPointer<NvAPI_Unload_t>("NvAPI_Unload");
                _NvAPI_GetErrorMessage = GetFunctionPointer<NvAPI_GetErrorMessage_t>("NvAPI_GetErrorMessage");
                _NvAPI_SYS_GetDriverAndBranchVersion = GetFunctionPointer<NvAPI_SYS_GetDriverAndBranchVersion_t>("NvAPI_SYS_GetDriverAndBranchVersion");
                _NvAPI_GPU_GetPhysicalFrameBufferSize = GetFunctionPointer<NvAPI_GPU_GetPhysicalFrameBufferSize_t>("NvAPI_GPU_GetPhysicalFrameBufferSize");
                _NvAPI_EnumPhysicalGPUs = GetFunctionPointer<NvAPI_EnumPhysicalGPUs_t>("NvAPI_EnumPhysicalGPUs");
                _NvAPI_GPU_GetFullName = GetFunctionPointer<NvAPI_GPU_GetFullName_t>("NvAPI_GPU_GetFullName");

                return _NvAPI_Initialize != null && _NvAPI_Unload != null && _NvAPI_GetErrorMessage != null;
            }
            catch (Exception ex)
            {
                WriteLog($"함수 포인터 바인딩 중 예외: {ex.Message}");
                return false;
            }
        }

        private T? GetFunctionPointer<T>(string functionName) where T : Delegate
        {
            IntPtr functionPtr = GetProcAddress(_hNvApi, functionName);
            if (functionPtr != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
            }
            WriteLog($"함수 {functionName} 찾을 수 없음");
            return null;
        }

        #endregion

        #region Quadro Sync II 상태 확인

        public SyncChecker.SyncStatus GetSyncStatus()
        {
            if (!_isInitialized)
            {
                WriteLog("Quadro Sync II NVAPI가 초기화되지 않음");
                return SyncChecker.SyncStatus.Unknown;
            }

            try
            {
                WriteLog("=== Quadro Sync II 상태 확인 시작 ===");

                // 드라이버 정보 조회
                var driverInfo = GetDriverInfo();
                if (driverInfo != null)
                {
                    WriteLog($"드라이버 버전: {driverInfo.Value.version}, 브랜치: {driverInfo.Value.branch}");
                }

                // GPU 정보 조회
                var gpuInfo = GetGpuInfo();
                if (gpuInfo != null)
                {
                    WriteLog($"GPU: {gpuInfo.Value.name}, 메모리: {gpuInfo.Value.memorySize}MB");
                }

                // Quadro Sync II 상태는 WMI와 병행으로 확인
                // NVAPI로는 기본적인 GPU/드라이버 정보만 확인 가능
                WriteLog("Quadro Sync II 상태는 WMI와 병행 확인 필요");
                
                return SyncChecker.SyncStatus.Unknown; // WMI에서 실제 Sync 상태 확인
            }
            catch (Exception ex)
            {
                WriteLog($"Quadro Sync II 상태 확인 중 예외: {ex.Message}");
                return SyncChecker.SyncStatus.Unknown;
            }
        }

        private (uint version, string branch)? GetDriverInfo()
        {
            if (_NvAPI_SYS_GetDriverAndBranchVersion == null)
            {
                WriteLog("NvAPI_SYS_GetDriverAndBranchVersion 함수가 없음");
                return null;
            }

            try
            {
                uint driverVersion = 0;
                var buildBranchString = new StringBuilder(256);

                int result = _NvAPI_SYS_GetDriverAndBranchVersion(ref driverVersion, buildBranchString);
                if (result == NVAPI_OK)
                {
                    return (driverVersion, buildBranchString.ToString());
                }
                else
                {
                    string errorMsg = GetErrorMessage(result);
                    WriteLog($"드라이버 정보 조회 실패: {errorMsg}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"드라이버 정보 조회 중 예외: {ex.Message}");
                return null;
            }
        }

        private (string name, uint memorySize)? GetGpuInfo()
        {
            if (_NvAPI_EnumPhysicalGPUs == null || _NvAPI_GPU_GetFullName == null || _NvAPI_GPU_GetPhysicalFrameBufferSize == null)
            {
                WriteLog("GPU 정보 조회 함수가 없음");
                return null;
            }

            try
            {
                IntPtr[] gpuHandles = new IntPtr[4]; // 최대 4개 GPU
                uint gpuCount = 0;

                int result = _NvAPI_EnumPhysicalGPUs(gpuHandles, ref gpuCount);
                if (result != NVAPI_OK)
                {
                    string errorMsg = GetErrorMessage(result);
                    WriteLog($"GPU 열거 실패: {errorMsg}");
                    return null;
                }

                WriteLog($"GPU {gpuCount}개 발견");

                if (gpuCount > 0)
                {
                    // 첫 번째 GPU 정보 조회
                    var gpuName = new StringBuilder(256);
                    result = _NvAPI_GPU_GetFullName(gpuHandles[0], gpuName);
                    if (result != NVAPI_OK)
                    {
                        string errorMsg = GetErrorMessage(result);
                        WriteLog($"GPU 이름 조회 실패: {errorMsg}");
                        return null;
                    }

                    var frameBufferSize = new NV_GPU_PHYSICAL_FRAME_BUFFER_SIZE
                    {
                        version = NV_GPU_PHYSICAL_FRAME_BUFFER_SIZE_VER
                    };

                    result = _NvAPI_GPU_GetPhysicalFrameBufferSize(gpuHandles[0], ref frameBufferSize);
                    if (result != NVAPI_OK)
                    {
                        string errorMsg = GetErrorMessage(result);
                        WriteLog($"GPU 메모리 크기 조회 실패: {errorMsg}");
                        return (gpuName.ToString(), 0);
                    }

                    uint memorySizeMB = frameBufferSize.frameBufferSize / (1024 * 1024);
                    return (gpuName.ToString(), memorySizeMB);
                }

                return null;
            }
            catch (Exception ex)
            {
                WriteLog($"GPU 정보 조회 중 예외: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 유틸리티 메서드

        private string GetErrorMessage(int status)
        {
            if (_NvAPI_GetErrorMessage == null)
            {
                return "NvAPI_GetErrorMessage 함수가 없음";
            }

            try
            {
                var message = new StringBuilder(256);
                int result = _NvAPI_GetErrorMessage(status, message);
                if (result == NVAPI_OK)
                {
                    return message.ToString();
                }
                return $"Unknown error: {status}";
            }
            catch
            {
                return $"Unknown error: {status}";
            }
        }

        private void WriteLog(string message)
        {
            string logMessage = $"[QuadroSyncII] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            File.AppendAllText("syncguard_log.txt", logMessage + Environment.NewLine);
        }

        #endregion

        #region P/Invoke 선언

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        #endregion
    }
} 