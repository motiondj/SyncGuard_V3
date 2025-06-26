using System;
using System.Windows.Forms;
using SyncGuard.Core;
using System.Runtime.Versioning;

namespace SyncGuard.Tray;

[SupportedOSPlatform("windows")]
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 콘솔 창 숨기기 (백그라운드 실행)
        // AllocConsole(); // 제거 - 콘솔 창을 표시하지 않음
        
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
    
    // AllocConsole 함수 제거 - 더 이상 사용하지 않음
}