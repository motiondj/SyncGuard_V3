using System;
using System.Windows.Forms;
using SyncGuard.Core;
using System.Runtime.Versioning;
using System.Threading;

namespace SyncGuard.Tray;

[SupportedOSPlatform("windows")]
static class Program
{
    private static Mutex? mutex = null;
    private const string MutexName = "Global\\SyncGuard_V3_SingleInstance";
    
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        bool createdNew;
        mutex = new Mutex(true, MutexName, out createdNew);
        
        if (!createdNew)
        {
            // 이미 실행 중인 경우
            MessageBox.Show("SyncGuard가 이미 실행 중입니다.", "중복 실행 방지", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        try
        {
            // 간단한 초기화 로그만 파일에 기록
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SyncGuard 시작됨";
                System.IO.File.AppendAllText("syncguard_log.txt", logMessage + Environment.NewLine, System.Text.Encoding.UTF8);
            }
            catch
            {
                // 로그 파일 기록 실패 시 무시
            }
            
            // 트레이 앱 바로 시작
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        finally
        {
            // Mutex 해제
            mutex?.ReleaseMutex();
            mutex?.Dispose();
        }
    }    
    
    // AllocConsole 함수 제거 - 더 이상 사용하지 않음
}