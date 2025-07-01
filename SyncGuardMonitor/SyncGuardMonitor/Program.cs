using System;
using System.Threading;
using System.Windows.Forms;

namespace SyncGuardMonitor
{
    /// <summary>
    /// SyncGuard Monitor - 독립 실행 프로그램
    /// </summary>
    internal static class Program
    {
        private static Mutex? mutex;
        
        [STAThread]
        static void Main()
        {
            // 단일 인스턴스 확인
            const string mutexName = "Global\\SyncGuardMonitor_SingleInstance";
            mutex = new Mutex(true, mutexName, out bool createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show(
                    "SyncGuard Monitor가 이미 실행 중입니다.", 
                    "중복 실행", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
                return;
            }
            
            try
            {
                // 애플리케이션 설정
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                
                // 전역 예외 처리
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                // 로깅 초기화
                System.Diagnostics.Debug.WriteLine("=== SyncGuard Monitor 시작 ===");
                System.Diagnostics.Debug.WriteLine($"버전: {Application.ProductVersion}");
                System.Diagnostics.Debug.WriteLine($"실행 경로: {Application.StartupPath}");
                System.Diagnostics.Debug.WriteLine($"OS: {Environment.OSVersion}");
                
                // 메인 폼 실행
                Application.Run(new MainForm());
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== SyncGuard Monitor 종료 ===");
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
        }
        
        private static void Application_ThreadException(object sender, 
            ThreadExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"처리되지 않은 예외: {e.Exception}");
            
            MessageBox.Show(
                $"예기치 않은 오류가 발생했습니다:\n\n{e.Exception.Message}", 
                "오류", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
        
        private static void CurrentDomain_UnhandledException(object sender, 
            UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Debug.WriteLine($"도메인 예외: {ex?.Message ?? "Unknown"}");
        }
    }
}