using System;
using System.Windows.Forms;
using SyncGuard.Core;

namespace SyncGuard.Tray;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 콘솔 창 표시
        AllocConsole();
        
        // 새로운 진단 로직 테스트
        Console.WriteLine("=== 새로운 디스플레이 동기화 진단 테스트 시작 ===");
        
        try
        {
            var syncChecker = new SyncChecker();
            Console.WriteLine("SyncChecker 생성 성공");
            
            // 상세 진단 실행
            Console.WriteLine("\n=== 상세 진단 실행 ===");
            var diagnosis = syncChecker.DiagnoseDisplaySync();
            
            Console.WriteLine($"\n=== 진단 결과 ===");
            Console.WriteLine($"동기화 상태: {(diagnosis.IsSynced ? "Synced (Locked)" : "Free (Unlocked)")}");
            Console.WriteLine($"타이밍 서버: {diagnosis.TimingServer}");
            Console.WriteLine($"선택된 디스플레이: {diagnosis.SelectedDisplay}");
            Console.WriteLine($"진단 메시지: {diagnosis.DiagnosisMessage}");
            
            Console.WriteLine($"\n=== 원시 데이터 ===");
            Console.WriteLine(diagnosis.RawData);
            
            // 일반 상태 확인
            var status = syncChecker.GetSyncStatus();
            Console.WriteLine($"\n일반 Sync 상태: {status}");
            
            // 5초간 모니터링
            Console.WriteLine("\n=== 5초간 모니터링 ===");
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(1000);
                status = syncChecker.GetSyncStatus();
                Console.WriteLine($"Sync 상태 [{i+1}]: {status}");
            }
            
            // 수동 리프레시 테스트
            Console.WriteLine("\n=== 수동 리프레시 테스트 ===");
            Console.WriteLine("리프레시를 실행합니다...");
            syncChecker.RefreshSyncStatus();
            
            // 리프레시 후 상태 확인
            var refreshDiagnosis = syncChecker.DiagnoseDisplaySync();
            Console.WriteLine($"\n=== 리프레시 후 진단 결과 ===");
            Console.WriteLine($"동기화 상태: {(refreshDiagnosis.IsSynced ? "Synced (Locked)" : "Free (Unlocked)")}");
            Console.WriteLine($"타이밍 서버: {refreshDiagnosis.TimingServer}");
            Console.WriteLine($"선택된 디스플레이: {refreshDiagnosis.SelectedDisplay}");
            Console.WriteLine($"진단 메시지: {refreshDiagnosis.DiagnosisMessage}");
            
            syncChecker.Dispose();
            Console.WriteLine("SyncChecker 정리 완료");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"진단 테스트 중 오류: {ex.Message}");
            Console.WriteLine($"스택 트레이스: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n=== 진단 테스트 완료 ===");
        Console.WriteLine("아무 키나 누르면 트레이 앱이 시작됩니다...");
        Console.ReadKey();
        
        // 트레이 앱 시작
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool AllocConsole();
}